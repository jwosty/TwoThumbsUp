namespace TwoThumbsUp

open WebSharper
open WebSharper.Html.Client
open WebSharper.JavaScript
open WebSharper.JQuery

[<JavaScript>]
module Client =
    let setResultInfo message = JQuery.Of("#resultInfo").Text message |> ignore

    let form_createVote defaultVotingRoomName =
        // TODO: Make bracket style consistent.... Idk what looks best
        let optionsDiv = Div []
        let mutable inputs = []
        let submitButton = Input [Attr.Type "button"; Attr.Value "Submit & view"; Attr.TabIndex "0"]
        let votingRoomNameInput = Input [Attr.Type "text"; Attr.Value defaultVotingRoomName
                                         Attr.AutoComplete "off"; Attr.TabIndex "1"]
        
        let addNewInput () =
            let newInput = Input [Attr.Type "text"
                                  Attr.Name ("option" + string inputs.Length)
                                  Attr.AutoComplete "off" ]
            optionsDiv.Append newInput
            optionsDiv.Append (Br [])
            inputs <- newInput :: inputs
        
        addNewInput ()
        
        Div
          [ Span [Text "twothumbsup.com/vote/"]; votingRoomNameInput
            Br []
            Button [Text "+"] |>! OnClick (fun x e -> addNewInput ())
            Br []
            optionsDiv
            submitButton
            |>! OnClick (fun x e ->
                async {
                    let! result =
                        inputs |> List.map (fun x -> x.Value) |> List.rev
                        |> AppState.Api.tryCreateVotingRoom votingRoomNameInput.Value
                    match result with
                    | AppState.Api.Success ->
                        setResultInfo ""
                        JS.Window.Location.Pathname <- "/vote/" + JS.EncodeURIComponent votingRoomNameInput.Value
                    | AppState.Api.NameTaken -> setResultInfo "Name already taken"
                    | AppState.Api.InvalidOptions -> setResultInfo "At least one option must be added"
                    | AppState.Api.InvalidName -> setResultInfo "Voting room name cannot be empty"
                    return () }
                |> Async.Start ) ]
    
    let form_submitVote votingRoom =
        Div
           [let (Voting(votes)) = votingRoom
            for kv in votes do
                let option, votes = kv.Key, kv.Value
                yield Div [Text option]]