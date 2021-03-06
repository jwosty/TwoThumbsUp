﻿namespace TwoThumbsUp

open WebSharper
open WebSharper.Html
open WebSharper.Html.Client
open WebSharper.JavaScript
open WebSharper.JQuery

[<JavaScript>]
module Client =
    let RadioGroup children = Tags.NewTag "radioGroup" children
    
    let setResultInfo message = JQuery.Of("#resultInfo").Text message |> ignore

    let on (e: string) f (x: JQuery) = x.On(e, f) |> ignore

    let inputButton children = Input [Attr.Type "button"] -< children

    /// Creates a <form> tag containing radio buttons, and a function to retrieve which value is selected when called
    let makeRadioGroup radioButtonGroupName data =
        let html =
            let mutable isFirst = true
            Form [
                for (value, radioButtonName) in data ->
                    let result =
                        Div [Attr.Class "radio"]
                        -< [Label
                               [Input
                                   [yield Type "radio"
                                    yield Name radioButtonGroupName
                                    yield Value radioButtonName
                                    if isFirst then yield Checked "" ]]
                            -< [Span [Text radioButtonName]]]
                    isFirst <- false
                    result]
        html, (fun () ->
            let selection = JQuery.Of(sprintf "input[name=\"%s\"]:checked" radioButtonGroupName).Val() :?> string
            data |> List.find (fun (value, radioButtonName) -> selection = radioButtonName) |> fst)

    let makeTable (rows: (Pagelet list * _ list) list) =
        Table
           [for i in 0 .. rows.Length - 1 ->
                let (colAttrs, col) = rows.[i]
                TR colAttrs
                 -< [for j in 0 .. col.Length - 1 ->
                        let tag = if i = 0 || j = 0 then TH else TD
                        let str, attributes = col.[j]
                        tag (Text str :: attributes) ]]
    
    let form_createVote votingRoomName =
        let mutable optionInputs = []
        let optionInputsDiv = Div []

        let addNewInput () =
            let newInput =
                Input [Attr.Class "optionInput"]
                   -< [Attr.Type "text"; Attr.Class "form-control"
                       Attr.Name ("option" + string optionInputs.Length)
                       Attr.AutoComplete "off" ]
            optionInputsDiv.Append (Div [Attr.Class "row"] -< [Div [Attr.Class "col-sm-5"] -< [newInput]])
            optionInputsDiv.Append (Br [])
            optionInputs <- newInput :: optionInputs

        let createVotingRoom votingRoomName =
            async {
                let options =
                    optionInputs |> List.rev |> List.map (fun x -> x.Value)
                    |> List.filter (not << System.String.IsNullOrWhiteSpace)
                if options.Length = 0 then
                    setResultInfo "No options added"
                else
                    let! result = Api.tryCreateVotingRoom votingRoomName
                    match result with
                    | AppState.NameTaken -> setResultInfo "Name already taken"
                    | AppState.InvalidName -> setResultInfo "Name required"
                    | AppState.Success ->
                        do! Api.addOptions votingRoomName options
                        JS.Window.Location.Pathname <- "/vote/" + JS.EncodeURIComponent votingRoomName }

        let input_url =
            Input [Type "text"; Class "form-control"; Id "input-url"; PlaceHolder "url"; Value votingRoomName;
                   AutoFocus "autofocus"; AutoComplete "off"; NewAttr "auto-capitalize" "none"]

        addNewInput ()
        Form [Attr.Action "/404"]
        -< [Div [Class "row"]
               -< [Div [Class "col-xs-3"]
                   -< [Div [Class "input-group"]
                       -< [Span [Class "input-group-addon"; Id "url-addon"] -< [Text "twothumbsup.com/vote/"]
                           input_url]
                       ]
                   ]
            Br []
            Div [Class "row"]
            -< [Div [Class "col-xs-5"]
                -< [Input [Type "submit"; Class "btn btn-default btn-xs"; Value "+"]
                    |>! OnClick (fun x e ->
                        e.Event.PreventDefault ()
                        addNewInput ())]
                ]
            Br []
            optionInputsDiv
            Div [Class "row"]
            -< [Div [Class "col-xs-5"]
                -< [Button [Type "button"; Class "btn btn-default"; Text "Create"]
                    |>! OnClick (fun x e ->
                        e.Event.PreventDefault ()
                        Async.Start (createVotingRoom input_url.Value))
                    ]
                ]
            ]
    
    let form_brainstorm votingRoomName =
        let input_optionName = Input [Type "text"; Class "form-control"; PlaceHolder "Type your option here"]

        Form [Attr.Action "/404"]
        -< [
            Div [Class "row"]
            -< [Div [Class "col-xs-6"] -< [input_optionName]
                Div [Class "col-xs-6"]
                -< [Button [Type "submit"; Text "Add idea"]
                    |>! OnClick (fun x e ->
                        e.Event.PreventDefault ()
                        Async.Start <| async {
                            do! Api.addOption votingRoomName input_optionName.Value
                            input_optionName.Value <- "" } )
                    ]
                ]
            ]

    let form_submitVote votingRoomName votingRoom =
        match votingRoom with
        | Voting optionVotes ->
            let optionsDiv = Div []
            let submitButton = Button [ Attr.Class "btn btn-default"; Text "Cast vote" ]

            for kv in optionVotes do
                printfn "%s" kv.Key

            let data =
                // Used to create a unique name for each radio group. The name itself doesn't matter to anything except the
                // internals of makeRadioGroup
                let mutable i = 0
                optionVotes |> Map.map (fun optionName optionVote ->
                    // make a radio group for each option that exists
                    let radioGroup, getSelection = makeRadioGroup ("option" + string i) (Map.toList Vote.toStrMap |> List.rev)
                    let element =
                        Div [Attr.Class "row"]
                        -< [Div [Attr.Class "col-xs-5"; Text optionName]
                            -< [radioGroup]]
                    optionsDiv.Append element
                    i <- i + 1
                    getSelection)
        
            submitButton |> OnClick (fun x e ->
                data |> Map.map (fun name getSelection -> getSelection ())
                |> Api.submitVote votingRoomName
                |> Async.Start)

            Div [
                optionsDiv
                submitButton ]
    
    let form_viewVote votingRoomName =
        let tableDiv = Div []
        
        let render votingRoom =
            match votingRoom with
            | Voting optionVotes ->
                let voteResultData : (Pagelet list * (string * Pagelet list) list) list =
                  [ yield [],
                          [ yield "Option", []
                            for vote in Vote.values -> Vote.toStrMap.[vote], [] ]
                    for (option, voteTallies) in Map.toList optionVotes do
                        let hasVeto = voteTallies |> Map.exists (fun vote tally -> vote = TwoThumbsDown && tally > 0 )
                        yield [if hasVeto then yield Attr.Class "danger"],
                              [ yield option, []
                                for (vote, tally) in Map.toList voteTallies do
                                    yield string tally, []]]
                let table = makeTable voteResultData -< [Attr.Class "table table-bordered"]
                tableDiv.Clear ()
                tableDiv.Append table

        // Initial update
        async {
            let! votingRoom = Api.tryRetrieveVotingRoomState votingRoomName
            match votingRoom with
            | Some(votingRoom) -> render votingRoom
            | None -> setResultInfo "Voting room does not exist"
        } |> Async.Start
        
        // Start an event loop to listen for incoming votes
        async {
            let mutable voteExists = true
            while voteExists do
                let! votingRoom = Api.pollStateChange votingRoomName
                match votingRoom with
                | Some(votingRoom) -> render votingRoom
                | None ->
                    setResultInfo "Vote does not exist"
                    voteExists <- false }
        |> Async.Start

        Div [Class "row"]
        -< [Div [Class "col-xs-12"]
            -< [tableDiv]
            Div [Class "col-xs-12"]
            -< [A [HRef "/"; Class "btn btn-danger"; Text "Delete voting room"]
                |>! OnClick (fun x e ->
                    e.Event.PreventDefault ()
                    Async.Start <| async {
                        let! _ = Api.deleteVotingRoom votingRoomName
                        JS.Window.Location.Href <- "/" })
                ]
            ]