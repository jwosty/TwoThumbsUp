#load "AppState.fs"
open TwoThumbsUp

let votingRoom = new VotingRoomAgent()


votingRoom.PostAndReply (fun replyChannel ->
    AddOption "option a")
|> Async.StartImmediate

votingRoom.Post (AddOption "option a")
votingRoom.Post (RemoveOption "option a")
votingRoom.Post (AddVote ("option a", TwoThumbsDown, 2))
votingRoom.PostAndReply Retrieve |> Async.RunSynchronously