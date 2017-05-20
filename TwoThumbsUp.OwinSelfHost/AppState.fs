namespace TwoThumbsUp
open WebSharper
open WebSharper.Sitelets
open System.Collections.Generic
open System.Web

module Dictionary =
    let tryGetValue key (dict: Dictionary<_,_>) =
        match dict.TryGetValue key with
        | true, value -> Some(value)
        | false, _ -> None

[<JavaScript>]
type Vote =
    | TwoThumbsDown | OneThumbDown | OneThumbUp | TwoThumbsUp

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<JavaScript>]
module Vote =
    let values = [ TwoThumbsDown; OneThumbDown; OneThumbUp; TwoThumbsUp ]
    
    let fromStrMap =
        Map.ofList [ "Two thumbs down", TwoThumbsDown; "One thumb down", OneThumbDown
                     "One thumb up", OneThumbUp; "Two thumbs up", TwoThumbsUp ]
    
    let toStrMap = Map.toList fromStrMap |> List.map (fun (s, v) -> v, s) |> Map.ofList

[<JavaScript>]
type VotingRoomState =
    | Voting of votes: Map<string, Map<Vote, int>>

type AppState = { activeVotingRooms: Dictionary<string, VotingRoomState> }

module AppState =
    // TODO: Agents?
    let _lock = new System.Object()
    
    let state = { activeVotingRooms = new Dictionary<_, _>() }

    module Api =
        [<Rpc>]
        let createVotingRoom votingRoomName options = async { lock _lock (fun () ->
            let options =
                    List.filter (not << System.String.IsNullOrWhiteSpace) options
                    |> List.map (fun o -> o, Vote.values |> List.map (fun value -> value, 0) |> Map.ofList) |> Map.ofList
            state.activeVotingRooms.Add (votingRoomName, Voting(options))) }
        
        [<Rpc>]
        let tryGetVotingRoom votingRoomName = async { return lock _lock (fun () ->
            Dictionary.tryGetValue votingRoomName state.activeVotingRooms ) }
        
        [<Rpc>]
        /// Adds a vote to a given session, returning whether or not the operation was successful
        let submitVote votingRoomName (votes: Map<string, Vote>) = async {
            return lock _lock (fun () ->
                match Dictionary.tryGetValue votingRoomName state.activeVotingRooms with
                | Some(Voting(voteCounts)) ->
                    let voteCounts' =
                        voteCounts |> Map.map (fun option votesInfo ->
                            votesInfo |> Map.map (fun vote voteCount ->
                                if votes.[option] = vote then voteCount + 1
                                else voteCount))
                    state.activeVotingRooms.[votingRoomName] <- Voting(voteCounts')
                    true
                | None -> false )}