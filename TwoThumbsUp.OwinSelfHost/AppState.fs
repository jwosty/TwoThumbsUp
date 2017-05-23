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
        Map.ofList [ "Two thumbs down", TwoThumbsDown; "One thumb down", OneThumbDown
                     "One thumb up", OneThumbUp; "Two thumbs up", TwoThumbsUp ]
    
    let toStrMap = Map.toList fromStrMap |> List.map (fun (s, v) -> v, s) |> Map.ofList

[<JavaScript>]
type VotingRoomState = { optionVotes: Map<string, Map<Vote, int>> }

[<JavaScript>]
type JSSafeVotingRoomMessage =
    | AddOption of string
    | AddOptions of string list
    | RemoveOption of string
    | SubmitVote of Map<string, Vote>

type VotingRoomMessage =
    | JSSafe of JSSafeVotingRoomMessage
    | RetrieveState of AsyncReplyChannel<VotingRoomState>

type VotingRoomAgent() =
    let initialTally = Vote.values |> List.map (fun voteKind -> voteKind, 0) |> Map.ofList
    
    let agent = MailboxProcessor.Start (fun inbox ->
        let rec messageLoop state = async {
            let! message = inbox.Receive ()
            
            let state =
                match message with
                | JSSafe(AddOption optionName) ->
                    { state with optionVotes = Map.add optionName initialTally state.optionVotes }
                | JSSafe(AddOptions optionNames) ->
                    { state with
                        optionVotes =
                            optionNames |> List.fold (fun optionVotes optionName ->
                                Map.add optionName initialTally optionVotes) state.optionVotes}
                | JSSafe(RemoveOption optionName) ->
                    { state with
                        optionVotes = state.optionVotes |> Map.filter (fun optionName' tallies ->
                            optionName' = optionName) }
                | JSSafe(SubmitVote votes) ->
                    let optionVotes =
                        state.optionVotes |> Map.map (fun option votesInfo ->
                            votesInfo |> Map.map (fun vote tally ->
                                if votes.[option] = vote then tally + 1 else tally))
                    { state with optionVotes = optionVotes }
                | RetrieveState replyChannel ->
                    replyChannel.Reply state
                    state
            
            return! messageLoop state }
        messageLoop { optionVotes = Map.empty })
    
    member this.Post message = agent.Post message
    member this.PostAndReply f = agent.PostAndAsyncReply f

module AppState =
    // TODO: Look into wrapping the whole app state in an agent. This would make message passing to the individual
    // voting rooms a little tricky, so this is good enough for now -- better than before
    let private _lock = new Object()
    let private votingRooms = new Dictionary<string, VotingRoomAgent>()

    let tryGetVotingRoomAgent votingRoomName = lock _lock (fun () ->
        Dictionary.tryGetValue votingRoomName votingRooms)
    
    let createVotingRoom votingRoomName = lock _lock (fun () ->
        if not (votingRooms.ContainsKey votingRoomName) then
            votingRooms.[votingRoomName] <- new VotingRoomAgent())
    
    let destroyVotingRoom votingRoomName = lock _lock (fun () ->
        votingRooms.Remove votingRoomName)
    
    let postMessage votingRoomName message =
        tryGetVotingRoomAgent votingRoomName |> Option.iter (fun votingRoom -> votingRoom.Post message)
    
    let postMessageAndReply votingRoomName message = async {
        match tryGetVotingRoomAgent votingRoomName with
        | Some votingRoom ->
            let! result = votingRoom.PostAndReply message
            return Some(result)
        | None -> return None }
    
    module Api =
        [<Rpc>]
        let createVotingRoom votingRoomName = async {
            printfn "createVotingRoom: %A" votingRoomName
            createVotingRoom votingRoomName
            printfn "createVotingRoom success" }
        [<Rpc>]
        let postMessage votingRoomName message = async {
            printfn "postMessage: %A" message
            postMessage votingRoomName (JSSafe(message))
            printfn "postMessage success" }
        
        [<Rpc>]
        let tryRetrieveVotingRoomState votingRoomName = async {
            printfn "tryRetrieveVotingRoomState: %s" votingRoomName
            let! result = postMessageAndReply votingRoomName RetrieveState
            printfn "tryRetrieveVotingRoomState success: %A" result
            return result }