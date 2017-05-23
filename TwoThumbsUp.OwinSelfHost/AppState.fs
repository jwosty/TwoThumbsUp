namespace TwoThumbsUp
open System
open System.Collections.Generic
open System.Web

#if INTERACTIVE
// Just so that we don't have to reference WebSharper when testing things in interactive
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

type VotingRoomMessage =
    | AddOption of optionName: string
    | RemoveOption of optionName: string
    | AddVote of optionName:string * vote:Vote * count:int
    | Retrieve of AsyncReplyChannel<VotingRoomState>

type VotingRoomAgent() =
    let initialTally = Vote.values |> List.map (fun voteKind -> voteKind, 0) |> Map.ofList
    
    let agent = MailboxProcessor.Start (fun inbox ->
        let rec messageLoop state = async {
            let! message = inbox.Receive ()
            
            let state =
                match message with
                | AddOption optionName ->
                    { state with optionVotes = Map.add optionName initialTally state.optionVotes }
                | RemoveOption optionName ->
                    { state with
                        optionVotes = state.optionVotes |> Map.filter (fun optionName' tallies ->
                            optionName' = optionName) }
                | AddVote (optionName, voteKind, count) ->
                    let optionVotes = state.optionVotes |> Map.map (fun optionName' tallies ->
                        if optionName' = optionName then
                            tallies |> Map.map (fun voteKind' tally ->
                            if voteKind = voteKind' then tally + count else tally)
                        else tallies)
                    { state with optionVotes = optionVotes }
                | Retrieve replyChannel ->
                    replyChannel.Reply state
                    state
            
            return! messageLoop state }
        messageLoop { optionVotes = Map.empty })
    
    member this.Post message = agent.Post message
    member this.PostAndReply f = agent.PostAndAsyncReply f



        

//[<JavaScript>]
///// NOTE: Don't try to RPC this whole structure over the wire -- Event is making deserialization fail
//type VotingRoomState(optionVotes: Map<string, Map<Vote, int>>) =
    //let onChange = new Event<VotingRoomState>()
    //let mutable optionVotes = optionVotes

    //member this.OnChange = onChange.Publish

    //member this.OptionVotes
        //with get () = optionVotes
        //and set newValue =
            //optionVotes <- newValue
            //onChange.Trigger this

//type AppState = { activeVotingRooms: Dictionary<string, VotingRoomState> }

(*
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

*)