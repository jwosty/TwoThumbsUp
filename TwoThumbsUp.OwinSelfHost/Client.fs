namespace TwoThumbsUp

open WebSharper
open WebSharper.Html.Client
open WebSharper.JavaScript

[<JavaScript>]
module Client =
    let x = 42

    let form_createVote () =
        let optionsDiv = Div []
        let i = ref 0
        let inputs = ref []
        let addNewInput () =
            let newInput = Input [ Attr.Type "text"; Attr.Name ("option" + string !i) ]
            optionsDiv.Append newInput
            optionsDiv.Append (Br [])
            inputs := newInput :: !inputs
        addNewInput ()
        // TODO: Make this a proper HTML form with an action POST page
        Div [  Span [ Text "twothumbsup.com/vote/" ]
               Input [ Attr.Type "text"; Attr.Name "" ]
               Button [ Text "+" ]
               |>! OnClick (fun x e -> addNewInput ())
               Br []
               optionsDiv
               Input [ Attr.Type "Button"; Attr.Value "Submit"] ]