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
        agent.Post (AddOption "option a")
        let state = (retrieveState agent).optionVotes
        state |> Map.count |> should equal 1
        state |> Map.tryFind "option a" |> should not' (equal None)
    
    [<Test>]
    let ``Posting two AddOption messages should add two options`` () =
        let agent = new VotingRoomAgent()
        agent.Post (AddOption "option a")
        agent.Post (AddOption "option b")
        let state = (retrieveState agent).optionVotes
        state |> Map.count |> should equal 2
        state |> Map.tryFind "option a" |> should not' (equal None)
        state |> Map.tryFind "option b" |> should not' (equal None)
    
    [<Test>]
    let ``Posting AddOptions with no options should not add options`` () =
        let agent = new VotingRoomAgent()
        agent.Post (AddOptions [])
        (retrieveState agent).optionVotes
        |> should equal (Map.ofList [])
    
    [<Test>]
    let ``Posting AddOptions with two options should add two options`` () =
        let agent = new VotingRoomAgent()
        agent.Post (AddOptions ["option a"; "option b"])
        let state = (retrieveState agent).optionVotes
        state |> Map.count |> should equal 2
        state |> Map.tryFind "option a" |> should not' (equal None)
        state |> Map.tryFind "option b" |> should not' (equal None)
    
    [<Test>]
    let ``Posting RemoveOption should remove an option`` () =
        let agent = new VotingRoomAgent()
        agent.Post (AddOptions ["option a"; "option b"])
        agent.Post (RemoveOption "option a")
        let state = (retrieveState agent).optionVotes
        state |> Map.count |> should equal 1
        state |> Map.tryFind "option b" |> should equal None

    [<Test>]
    let ``Posting a SubmitVote should increase the appropriate vote tallies`` () =
        let agent = new VotingRoomAgent()
        agent.Post (AddOptions ["option a"; "option b"])
        let votes = Map.ofList ["option a", TwoThumbsUp; "option b", OneThumbDown]
        agent.Post (SubmitVote votes)
        let state = (retrieveState agent).optionVotes
        let oa = Map.find "option a" state
        let ob = Map.find "option b" state
        
        oa |> Map.find TwoThumbsUp |> should equal 1
        oa |> Map.find OneThumbDown |> should equal 0
        ob |> Map.find TwoThumbsUp |> should equal 0
        ob |> Map.find OneThumbDown |> should equal 1
    
    [<Test>]
    let ``Posting many SubmitVote s should increase the approprate vote tallies`` () =
        let agent = new VotingRoomAgent()
        agent.Post (AddOptions ["option a"])
        for vote in [OneThumbUp; OneThumbDown; OneThumbUp] do
            agent.Post (SubmitVote(Map.ofList ["option a", vote]))
        let oa = (retrieveState agent).optionVotes |> Map.find "option a"
        
        oa |> Map.find TwoThumbsUp |> should equal 0
        oa |> Map.find OneThumbUp |> should equal 2
        oa |> Map.find OneThumbDown |> should equal 1
        oa |> Map.find TwoThumbsDown |> should equal 0