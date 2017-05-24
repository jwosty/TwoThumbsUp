namespace TwoThumbsUp
open NUnit.Framework
open FsUnit

module VotingRoomTests =
    let retrieveState (a: VotingRoomAgent) = a.PostAndReply RetrieveState |> Async.RunSynchronously

    [<Test>]
    let ``New VotingRoomAgent should have an empty state`` () =
        let agent = new VotingRoomAgent()
        (retrieveState agent).optionVotes
        |> should equal Map.empty
    
    [<Test>]
    let ``Posting one AddOption message should add one option`` () =
        let agent = new VotingRoomAgent()
        agent.Post (JSSafe(AddOption "option a"))
        let state = (retrieveState agent).optionVotes
        state |> Map.count |> should equal 1
        state |> Map.tryFind "option a" |> should not' (equal None)
    
    [<Test>]
    let ``Posting two AddOption messages should add two options`` () =
        let agent = new VotingRoomAgent()
        agent.Post (JSSafe(AddOption "option a"))
        agent.Post (JSSafe(AddOption "option b"))
        let state = (retrieveState agent).optionVotes
        state |> Map.count |> should equal 2
        state |> Map.tryFind "option a" |> should not' (equal None)
        state |> Map.tryFind "option b" |> should not' (equal None)
    
    [<Test>]
    let ``Posting AddOptions with no options should not add options`` () =
        let agent = new VotingRoomAgent()
        agent.Post (JSSafe(AddOptions []))
        (retrieveState agent).optionVotes
        |> should equal (Map.ofList [])
    
    [<Test>]
    let ``Posting AddOptions with two options should add two options`` () =
        let agent = new VotingRoomAgent()
        agent.Post (JSSafe(AddOptions ["option a"; "option b"]))
        let state = (retrieveState agent).optionVotes
        state |> Map.count |> should equal 2
        state |> Map.tryFind "option a" |> should not' (equal None)
        state |> Map.tryFind "option b" |> should not' (equal None)
    
    [<Test>]
    let ``Posting a RemoveOption message should remove a message`` () =
        let agent = new VotingRoomAgent()
        agent.Post (JSSafe(AddOptions ["option a"; "option b"]))
        agent.Post (JSSafe(RemoveOption "option a"))
        let state = (retrieveState agent).optionVotes
        state |> Map.count |> should equal 1
        state |> Map.tryFind "option b" |> should equal None