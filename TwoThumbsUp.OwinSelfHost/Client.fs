namespace TwoThumbsUp

open WebSharper
open WebSharper.Html.Client
open WebSharper.JavaScript
open WebSharper.JQuery

[<JavaScript>]
module Client =
    let setResultInfo message = JQuery.Of("#resultInfo").Text message |> ignore

    let on (e: string) f (x: JQuery) = x.On(e, f)

    let inputButton children = Input [Attr.Type "button"] -< children

    let form_createVote (defaultVotingRoomName: string) =
        let mutable inputs = []

        let addNewInput () =
            let newInput =
                Input [Attr.Class "optionInput"]
                   -< [Attr.Type "text"; Attr.Class "form-control"
                       Attr.Name ("option" + string inputs.Length)
                       Attr.AutoComplete "off" ]
            (Div [Attr.Class "row"] -< [Div [Attr.Class "col-xs-5"] -< [newInput]]).AppendTo "option-inputs"
            (Br []).AppendTo "option-inputs"
            inputs <- newInput :: inputs

        JQuery.Of "#add-option" |> on "click" (fun x e ->
            printfn "+"
            addNewInput ()) |> ignore
        JQuery.Of "#submit-vote-session" |> on "click" (fun x e -> printfn "clicked") |> ignore
        Div []

    let form_createVoteOld defaultVotingRoomName =
        // TODO: Make bracket style consistent.... Idk what looks best
        // also TODO: Refactor some of this out into the template HTML. We don't really need to generate _all_ of this with JS
        let optionsDiv = Div []
        let mutable inputs = []
        let submitButton = Input [Attr.Type "button"; Attr.Class "btn btn-default"; Attr.Value "Submit & view"; Attr.TabIndex "0"]
        let votingRoomNameInput =
            Input [Attr.Type "text"; Attr.Class "form-control"; Attr.Id "input-url"
                   NewAttr "aria-describedby" "url-addon"

                   Attr.AutoComplete "off"; NewAttr "autocapitalize" "none"
                   Attr.Value defaultVotingRoomName]
        
        let addNewInput () =
            let newInput =
                Input [Attr.Class "optionInput"]
                   -< [Attr.Type "text"; Attr.Class "form-control"
                       Attr.Name ("option" + string inputs.Length)
                       Attr.AutoComplete "off" ]
            optionsDiv.Append (Div [newInput])
            optionsDiv.Append (Br [])
            inputs <- newInput :: inputs
        
        addNewInput ()
        
        Div
          [ Div [Attr.Class "row"]
            -< [Div [Attr.Class "col-xs-3"]
               -< [Div [Attr.Class "input-group"]
                   -< [Span [Attr.Class"input-group-addon"; Attr.Id "url-addon"] -< [Text "twothumbsup.com/vote/"]]
                   -< [votingRoomNameInput]]]
            -< [Div [Attr.Class "col-xs-9"]]
            Div [Attr.Class "row"]
            -< [Div [Attr.Class "col-xs-5"]
                -< [Br []; Br []
                    Div [inputButton [Attr.Class "btn btn-default btn-xs"; Attr.Value "+"] |>! OnClick (fun x e -> addNewInput ())]
                    Br []
                    optionsDiv
                    Div [Attr.Class "option-inputs"]
                     -< [submitButton |>! OnClick (fun x e ->
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
                                | AppState.Api.InvalidName -> setResultInfo "Voting room name cannot be empty" }
                            |> Async.Start )]]]]
    
    let form_submitVote votingRoom =
        Div
           [let (Voting(votes)) = votingRoom
            for kv in votes do
                let option, votes = kv.Key, kv.Value
                yield Div [Text option]]