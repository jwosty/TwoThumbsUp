#load "VotingRoom.fs"
open TwoThumbsUp

AppState.tryCreateVotingRoom "ice-cream"
let iceCreamAgent = AppState.tryGetVotingRoomAgent "ice-cream" |> Option.get

iceCreamAgent.Post (JSSafe(AddOption "McDonalds"))

iceCreamAgent.PostAndReply RetrieveState |> Async.RunSynchronously