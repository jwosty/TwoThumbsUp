namespace TwoThumbsUp
open NUnit.Framework
open FsUnit

module VotingRoomTests =
    let retrieveState (a: VotingRoomAgent) = a.PostAndReply RetrieveState |> Async.RunSynchronously

    [<Test>]
    let ``New VotingRoomAgent should have an empty Voting state`` () =
        let agent = new VotingRoomAgent()
        retrieveState agent |> should equal (Voting Map.empty)
    
    [<Test>]
    let ``Posting one AddOption message should add one option`` () =
        let agent = new VotingRoomAgent()
        agent.Post (AddOption "option a")
        let (Voting optionVotes) = retrieveState agent
        optionVotes |> Map.count |> should equal 1
        optionVotes |> Map.tryFind "option a" |> should not' (equal None)
    
    [<Test>]
    let ``Posting two AddOption messages should add two options`` () =
        let agent = new VotingRoomAgent()
        agent.Post (AddOption "option a")
        agent.Post (AddOption "option b")
        let (Voting optionVotes) = retrieveState agent
        optionVotes |> Map.count |> should equal 2
        optionVotes |> Map.tryFind "option a" |> should not' (equal None)
        optionVotes |> Map.tryFind "option b" |> should not' (equal None)
    
    [<Test>]
    let ``Posting AddOptions with no options should not add options`` () =
        let agent = new VotingRoomAgent()
        agent.Post (AddOptions [])
        retrieveState agent |> should equal (Voting (Map.ofList []))
    
    [<Test>]
    let ``Posting AddOptions with two options should add two options`` () =
        let agent = new VotingRoomAgent()
        agent.Post (AddOptions ["option a"; "option b"])
        let (Voting optionVotes) = retrieveState agent
        optionVotes |> Map.count |> should equal 2
        optionVotes |> Map.tryFind "option a" |> should not' (equal None)
        optionVotes |> Map.tryFind "option b" |> should not' (equal None)
    
    [<Test>]
    let ``Posting RemoveOption should remove an option`` () =
        let agent = new VotingRoomAgent()
        agent.Post (AddOptions ["option a"; "option b"])
        agent.Post (RemoveOption "option a")
        let (Voting optionVotes) = retrieveState agent
        optionVotes |> Map.count |> should equal 1
        optionVotes |> Map.tryFind "option b" |> should equal None

    [<Test>]
    let ``Posting a SubmitVote should increase the appropriate vote tallies`` () =
        let agent = new VotingRoomAgent()
        agent.Post (AddOptions ["option a"; "option b"])
        let votes = Map.ofList ["option a", TwoThumbsUp; "option b", OneThumbDown]
        agent.Post (SubmitVote votes)
        let (Voting optionVotes) = retrieveState agent
        let oa = Map.find "option a" optionVotes
        let ob = Map.find "option b" optionVotes
        
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
        let (Voting optionVotes) = retrieveState agent
        let oa = Map.find "option a" optionVotes
        
        oa |> Map.find TwoThumbsUp |> should equal 0
        oa |> Map.find OneThumbUp |> should equal 2
        oa |> Map.find OneThumbDown |> should equal 1
        oa |> Map.find TwoThumbsDown |> should equal 0