namespace TwoThumbsUp

open WebSharper
open WebSharper.Html.Client
open WebSharper.JavaScript

[<JavaScript>]
module Client =
    let x = 42

    let form_createVote () =
        // TODO: Make bracket style consistent.... Idk what looks best
        let optionsDiv = Div []
        let mutable inputs = []
        let submitButton = Input [Attr.Type "button"; Attr.Value "Submit & view"; Attr.TabIndex "0"]
        let votingRoomInput = Input [Attr.Type "text"; Attr.Name ""
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
          [ Span [Text "twothumbsup.com/vote/"]; votingRoomInput
            Br []
            Button [Text "+"] |>! OnClick (fun x e -> addNewInput ())
            Br []
            optionsDiv
            submitButton
            |>! OnClick (fun x e ->
                async {
                    do! inputs |> List.map (fun x -> x.Value) |> AppState.Api.createVotingRoom votingRoomInput.Value
                    JS.Window.Location.Pathname <- "/vote/" + votingRoomInput.Value
                    return () }
                |> Async.Start ) ]