namespace TwoThumbsUp

open WebSharper
open WebSharper.Html.Client
open WebSharper.JavaScript

[<JavaScript>]
module Client =
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
                    do! inputs |> List.map (fun x -> x.Value) |> List.rev
                        |> AppState.Api.createVotingRoom votingRoomNameInput.Value
                    JS.Window.Location.Pathname <- "/vote/" + JS.EncodeURIComponent votingRoomNameInput.Value
                    return () }
                |> Async.Start ) ]
    
    let form_submitVote votingRoom =
        Div
           [let (Voting(votes)) = votingRoom
            for kv in votes do
                let option, votes = kv.Key, kv.Value
                yield Div [Text option]]