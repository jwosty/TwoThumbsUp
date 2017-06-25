namespace TwoThumbsUp
open System
open System.Collections.Generic
open System.Web

#if INTERACTIVE
type JavaScriptAttribute() = inherit Attribute()
type RpcAttribute() = inherit Attribute()
#else
open WebSharper
open WebSharper.Sitelets
#endif

module Dictionary =
    let tryGetValue key (dict: Dictionary<_,_>) =
        match dict.TryGetValue key with
        | true, value -> Some(value)
        | false, _ -> None

[<JavaScript>]
type Vote = | TwoThumbsDown | OneThumbDown | OneThumbUp | TwoThumbsUp

[<JavaScript>]
module Vote =
    let values = [ TwoThumbsDown; OneThumbDown; OneThumbUp; TwoThumbsUp ]
    
    let fromStrMap =
        Map.ofList ["Two thumbs down", TwoThumbsDown; "One thumb down", OneThumbDown
                    "One thumb up", OneThumbUp; "Two thumbs up", TwoThumbsUp]
    
    let toStrMap = Map.toList fromStrMap |> List.map (fun (s, v) -> v, s) |> Map.ofList

type VotingRoomState = Voting of Map<string, Map<Vote, int>>

[<JavaScript>]
type VotingRoomMessage =
    | AddOption of string
    | AddOptions of string list
    | RemoveOption of string
    | SubmitVote of Map<string, Vote>
    | RetrieveState of AsyncReplyChannel<VotingRoomState>

type VotingRoomAgent() =
    let initialTally = Vote.values |> List.map (fun voteKind -> voteKind, 0) |> Map.ofList
    
    let onStateChanged = new Event<_>()
    
    let agent = MailboxProcessor.Start (fun inbox ->
        let rec messageLoop state = async {
            let! message = inbox.Receive ()

            let state' =
                match state with
                | Voting optionVotes ->
                    match message with
                    | AddOption optionName ->
                        Some(Voting (Map.add optionName initialTally optionVotes))
                    | AddOptions optionNames ->
                        let optionVotes =
                            optionNames |> List.fold (fun optionVotes optionName ->
                                Map.add optionName initialTally optionVotes) optionVotes
                        Some (Voting optionVotes)
                    | RemoveOption optionName ->
                        let optionVotes =
                            optionVotes |> Map.filter (fun optionName' tallies ->
                            optionName' = optionName)
                        Some(Voting optionVotes)
                    | SubmitVote votes ->
                        let optionVotes =
                            optionVotes |> Map.map (fun option votesInfo ->
                                votesInfo |> Map.map (fun vote tally ->
                                    if votes.[option] = vote then tally + 1 else tally))
                        Some(Voting optionVotes )
                    | RetrieveState replyChannel ->
                        replyChannel.Reply state
                        None
            match state' with
            | Some(state') ->
                onStateChanged.Trigger state'
                return! messageLoop state'
            | None -> return! messageLoop state }
        messageLoop (Voting Map.empty))
    
    member this.Post message = agent.Post message
    member this.PostAndReply f = agent.PostAndAsyncReply f
    member this.OnStateChanged = onStateChanged.Publish

module AppState =
    // TODO: Look into wrapping the whole app state in an agent. This would make message passing to the individual
    // voting rooms a little tricky, so this is good enough for now -- better than before
    let private _lock = new Object()
    let private votingRooms = new Dictionary<string, VotingRoomAgent>()

    let tryGetVotingRoomAgent votingRoomName = lock _lock (fun () ->
        Dictionary.tryGetValue votingRoomName votingRooms)

    let votingRoomExists votingRoomName = lock _lock (fun () ->
        votingRooms.ContainsKey votingRoomName)

    [<JavaScript>]
    type TryCreateVotingRoomResult = | Success | InvalidName | NameTaken
    
    /// Creates a voting room, returning whether or it succeeded (will only fail if the room already exists)
    let tryCreateVotingRoom votingRoomName =
        if String.IsNullOrWhiteSpace votingRoomName then
            InvalidName
        else
            lock _lock (fun () ->
                if not (votingRooms.ContainsKey votingRoomName) then
                    votingRooms.[votingRoomName] <- new VotingRoomAgent()
                    Success
                else NameTaken)
    
    let deleteVotingRoom votingRoomName = lock _lock (fun () ->
        votingRooms.Remove votingRoomName)
    
    let postMessage votingRoomName message =
        let agent =
            match tryGetVotingRoomAgent votingRoomName with
            | Some agent -> Some agent
            | None ->
                tryCreateVotingRoom votingRoomName |> ignore
                tryGetVotingRoomAgent votingRoomName
        tryGetVotingRoomAgent votingRoomName |> Option.iter (fun votingRoom -> votingRoom.Post message)

    let postMessageAndReply votingRoomName message = async {
        match tryGetVotingRoomAgent votingRoomName with
        | Some votingRoom ->
            let! result = votingRoom.PostAndReply message
            return Some(result)
        | None -> return None }
    
module Api =
    let [<Rpc>] tryCreateVotingRoom votingRoomName = async {
        return AppState.tryCreateVotingRoom votingRoomName }
    
    let [<Rpc>] addOption votingRoomName option = async {
        AppState.postMessage votingRoomName (AddOption option) }

    let [<Rpc>] addOptions votingRoomName options = async {
        AppState.postMessage votingRoomName (AddOptions options) }

    let [<Rpc>] submitVote votingRoomName votes = async {
        AppState.postMessage votingRoomName (SubmitVote votes) }

    let [<Rpc>] tryRetrieveVotingRoomState votingRoomName =
        AppState.postMessageAndReply votingRoomName RetrieveState
    
    let [<Rpc>] pollStateChange votingRoomName = async {
        match AppState.tryGetVotingRoomAgent votingRoomName with
        | Some(agent) ->
            let! state = Async.AwaitEvent agent.OnStateChanged
            return Some(state)
        | None -> return None }

    let [<Rpc>] deleteVotingRoom votingRoomName = async {
        return AppState.deleteVotingRoom votingRoomName }