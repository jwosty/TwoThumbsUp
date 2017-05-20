namespace TwoThumbsUp

open WebSharper
open WebSharper.Html.Client
open WebSharper.JavaScript

[<JavaScript>]
module Client =
    let x = 42

    let form_createVote () =
        let optionsDiv = Div []
        let mutable i = 0
        let mutable optionInputs = []
        let addNewInput () =
            let newInput = Input [ Attr.Type "text"; Attr.Name ("option" + string i) ]
            optionsDiv.Append newInput
            optionsDiv.Append (Br [])
            optionInputs <- newInput :: optionInputs
            i <- i + 1
        addNewInput ()
        let sessionNameInput = Input [ Attr.Type "text"; Attr.Name ""]
        Div [Span [ Text "twothumbsup.com/vote/" ]; sessionNameInput
             Button [ Text "+" ]
             |>! OnClick (fun x e -> addNewInput ())
             Br []
             optionsDiv
             Input [ Attr.Type "Button"; Attr.Value "Submit"]
             |>! OnClick (fun x e ->
                optionInputs |> List.map (fun x -> x.Value)
                |> AppState.Api.createVoteSession sessionNameInput.Value
                |> Async.Start) ]