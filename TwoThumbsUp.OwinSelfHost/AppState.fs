namespace TwoThumbsUp
open WebSharper
open WebSharper.Sitelets
open System
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
/// NOTE: Don't try to RPC this whole structure over the wire -- Event is making deserialization fail
type VotingRoomState(optionVotes: Map<string, Map<Vote, int>>) =
    let onChange = new Event<VotingRoomState>()
    let mutable optionVotes = optionVotes

    member this.OnChange = onChange.Publish

    member this.OptionVotes
        with get () = optionVotes
        and set newValue =
            optionVotes <- newValue
            onChange.Trigger this

type AppState = { activeVotingRooms: Dictionary<string, VotingRoomState> }

module AppState =
    // TODO: Agents?
    let _lock = new System.Object()
    
    let state = { activeVotingRooms = new Dictionary<_, _>() }

    module Api =
        let tryGetVotingRoom votingRoomName = lock _lock (fun () ->
            Dictionary.tryGetValue votingRoomName state.activeVotingRooms)
        
        [<Rpc>]
        let tryGetVotingRoomData votingRoomName = async {
            return tryGetVotingRoom votingRoomName
                   |> Option.map (fun x -> x.OptionVotes) }
        
        type TryCreateVotingRoomResult = | Success | InvalidName | InvalidOptions | NameTaken

        [<Rpc>]
        let tryCreateVotingRoom votingRoomName options = async {
            if System.String.IsNullOrWhiteSpace votingRoomName then
                return InvalidName
            else
                let options = List.filter (not << System.String.IsNullOrWhiteSpace) options
                let optionVotes = options |> List.map (fun o -> o, Vote.values |> List.map (fun value -> value, 0) |> Map.ofList) |> Map.ofList
                return lock _lock (fun () ->
                    if state.activeVotingRooms.ContainsKey votingRoomName then
                        NameTaken
                    elif options.Length = 0 then
                        InvalidOptions
                    else
                        state.activeVotingRooms.Add (votingRoomName, new VotingRoomState(optionVotes))
                        Success) }
        
        [<Rpc>]
        /// Adds a vote to a given session, returning whether or not the operation was successful
        let submitVote votingRoomName (votes: Map<string, Vote>) = async {
            return lock _lock (fun () ->
                match Dictionary.tryGetValue votingRoomName state.activeVotingRooms with
                | Some(votingRoom) ->
                    let optionVotes =
                        votingRoom.OptionVotes |> Map.map (fun option votesInfo ->
                            votesInfo |> Map.map (fun vote voteCount ->
                                if votes.[option] = vote then voteCount + 1
                                else voteCount))
                    state.activeVotingRooms.[votingRoomName].OptionVotes <- optionVotes
                    true
                | None -> false )}
        
        [<Rpc>]
        let pollChange votingRoomName = async {
            match tryGetVotingRoom votingRoomName with
            | Some(votingRoom) ->
                let! votingRoom = Async.AwaitEvent votingRoom.OnChange
                return Some(votingRoom.OptionVotes)
            | None -> return None }