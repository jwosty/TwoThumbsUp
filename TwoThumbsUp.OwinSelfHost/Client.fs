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

        JQuery("#add-option") |> on "click" (fun x e ->
            printfn "+"
            addNewInput ()) |> ignore
        
        JQuery("#submit-vote-session") |> on "click" (fun x e ->
            async {
                let votingSessionName = JQuery("#input-url").Prop("value")
                let! result =
                    inputs |> List.map (fun x -> x.Value) |> List.rev
                    |> AppState.Api.tryCreateVotingRoom votingSessionName
                match result with
                | AppState.Api.Success ->
                    setResultInfo ""
                    JS.Window.Location.Pathname <- "/vote/" + JS.EncodeURIComponent votingSessionName
                | AppState.Api.NameTaken -> setResultInfo "Name already taken"
                | AppState.Api.InvalidOptions -> setResultInfo "At least one option must be added"
                | AppState.Api.InvalidName -> setResultInfo "Voting room name cannot be empty" }
            |> Async.Start ) |> ignore

        addNewInput ()
        Div []
    
    let form_submitVote votingRoom =
        Div
           [let (Voting(votes)) = votingRoom
            for kv in votes do
                let option, votes = kv.Key, kv.Value
                yield Div [Text option]]