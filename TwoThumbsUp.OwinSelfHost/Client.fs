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
                printfn "submitting vote (client)"
                let! success =
                    data |> Map.map (fun name getSelection -> getSelection ())
                    |> AppState.Api.submitVote votingRoomName
                setResultInfo ("success: " + string success) }
            |> Async.Start)

        Div [
            optionsDiv
            submitButton ]
    
    let form_viewVote votingRoomName =
        let tableDiv = Div [Attr.Class "table table-hover"]
        
        let render (votingRoomData: Map<string, Map<Vote, int>>) =
            let table =
                Table
                    [ yield TR [ yield TD [ Text "" ]
                                 for vote in Vote.values -> TD [ Text (Vote.toStrMap.[vote]) ] ]
                      for (option, voteTallies) in Map.toList votingRoomData do
                          yield TR [ yield TD [ Align "right" ]
                                           -< [B [ Text option ] ]
                                     for (vote, tally) in Map.toList voteTallies do
                                         yield TD [ Align "center"]
                                               -< [Text (string tally) ] ] ]
                (*Table [
                    TR [
                        yield TD []
                        for v in Vote.values do
                            yield TD [Text (Vote.toStrMap.[v])]]
                ]*)
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
                printfn "waiting for change..."
                let! votingRoomData = AppState.Api.pollChange votingRoomName
                match votingRoomData with
                | Some(votingRoomData) ->
                    printfn "Incoming vote"
                    render votingRoomData
                | None ->
                    setResultInfo "Vote does not exist"
                    voteExists <- false }
        |> Async.Start
        tableDiv