namespace TwoThumbsUp

open WebSharper
open WebSharper.Html
open WebSharper.Html.Client
open WebSharper.JavaScript
open WebSharper.JQuery

[<JavaScript>]
module Client =
    let RadioGroup children = Tags.NewTag "radioGroup" children
    
    let setResultInfo message = JQuery.Of("#resultInfo").Text message |> ignore

    let on (e: string) f (x: JQuery) = x.On(e, f)

    let inputButton children = Input [Attr.Type "button"] -< children

    /// Creates a <form> tag containing radio buttons, and a function to retrieve which value is selected when called
    let makeRadioGroup radioButtonGroupName data =
        let html =
            let mutable isFirst = true
            Form [
                for (value, radioButtonName) in data ->
                    isFirst <- false
                    Div [Attr.Class "radio"]
                     -< [Label [
                            Input [
                                yield Type "radio"
                                yield Name radioButtonGroupName
                                yield Value radioButtonName
                                if isFirst then yield Checked "" ]]
                         -< [Span [Text radioButtonName]]]]
        html, (fun () ->
            let selection = JQuery.Of(sprintf "input[name=%s]:checked" radioButtonGroupName).Val() :?> string
            data |> List.find (fun (value, radioButtonName) -> selection = radioButtonName) |> fst)

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
    
    let form_submitVote votingRoomName (votingRoom: VotingRoomState) =
        let optionsDiv = Div []
        let submitButton = Button [ Attr.Class "btn btn-default"; Text "Cast vote" ]

        let data = votingRoom.OptionVotes |> Map.map (fun optionName optionVote ->
            // make a radio group for each option that exists
            let radioGroup, getSelection = makeRadioGroup optionName (Map.toList Vote.toStrMap |> List.rev)
            let element =
                Div [Attr.Class "row"]
                -< [Div [Attr.Class "col-xs-5"; Text optionName]
                    -< [radioGroup]]
            optionsDiv.Append element
            getSelection)
        
        submitButton |> OnClick (fun x e ->
            async {
                let! success =
                    data |> Map.map (fun name getSelection -> getSelection ())
                    |> AppState.Api.submitVote votingRoomName
                setResultInfo ("success: " + string success) }
            |> Async.Start)

        Div [
            optionsDiv
            submitButton ]
    
    let makeTable (rows: string list list) =
        Table
           [for i in 0 .. rows.Length - 1 ->
                let col = rows.[i]
                TR
                   [for j in 0 .. col.Length - 1 ->
                        let tag = if i = 0 || j = 0 then TH else TD
                        tag [Text col.[j]]]]

    
    let form_viewVote votingRoomName =
        let tableDiv = Div []
        
        let render (votingRoomData: Map<string, Map<Vote, int>>) =
            let voteResultData =
                [ yield [ yield "Option"
                          for vote in Vote.values -> Vote.toStrMap.[vote] ]
                  for (option, voteTallies) in Map.toList votingRoomData do
                      yield [ yield option
                              for (vote, tally) in Map.toList voteTallies do
                                     yield string tally ] ]
            let table = makeTable voteResultData -< [Attr.Class "table table-striped table-hover table-bordered"]
            tableDiv.Clear ()
            tableDiv.Append table

        async {
            let! votingRoomData = AppState.Api.tryGetVotingRoomData votingRoomName
            match votingRoomData with
            | Some(votingRoomData) -> render votingRoomData
            | None -> setResultInfo "Vote does not exist"
        } |> Async.Start

        async {
            let mutable voteExists = true
            while voteExists do
                let! votingRoomData = AppState.Api.pollChange votingRoomName
                match votingRoomData with
                | Some(votingRoomData) ->
                    render votingRoomData
                | None ->
                    setResultInfo "Vote does not exist"
                    voteExists <- false }
        |> Async.Start
        tableDiv