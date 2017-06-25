namespace TwoThumbsUp
open System
open WebSharper.Html.Server
open WebSharper
open WebSharper.Sitelets

type EndPoint =
    | [<EndPoint "GET /">] Index
    | [<EndPoint "GET /manage">] ManageVote of escapedVotingRoomString: string
    | [<EndPoint "GET /vote">] Vote of escapedVotingRoomName: string
    | [<EndPoint "GET /view">] ViewVote of escapedVotingRoomName: string

module Templating =
    open System.Web

    type MainTemplateData =
      { browserTitle: string
        title: string
        content: Element list }

    let MainTemplate =
        Content.Template<_>("~/MainTemplate.xml")
               .With("browserTitle", fun data -> data.browserTitle)
               .With("title", fun data -> data.title)
               .With("content", fun data -> data.content)

    let TemplateSubmitVote =
        Content.Template<_>("~/SubmitVote.html")
               .With("title", fun (title: Element list, _) -> title)
               .With("client-scripts", fun (_, clientCode: Element list) -> clientCode)

module Site =
    let ahref href text = A [ HRef href ] -< [Text text]

    let IndexPage defaultVotingRoomName =
        Content.WithTemplate Templating.MainTemplate
          { browserTitle = "TwoThumbsUp - Create voting room"
            title = "Create a voting room"
            content =
               [Form [Attr.Action "/404"]
                -< [Div [Class "row"]
                    -< [Div [Class "col-xs-3"]
                        -< [Div [Class "input-group"]
                            -< [Span [Class "input-group-addon"; Id "url-addon"] -< [Text "twothumbsup.com/vote/"]
                                Input [Type "text"; Class "form-control"; Id "input-url"; PlaceHolder "url"; Value defaultVotingRoomName;
                                       AutoFocus "autofocus"; AutoComplete "off"; NewAttr "auto-capitalize" "none"]]
                            ]
                        ]
                    Br []
                    Div [Class "row"]
                    -< [Div [Class "col-xs-5"]
                        -< [Input [Type "submit"; Class "btn btn-default btn-xs"; Id "add-option"; Value "+"]]
                        ]
                    Br []
                    Div [ClientSide <@ Client.form_createVote defaultVotingRoomName @>]
                    Div [Class "row"]
                    -< [Div [Class "col-xs-5"]
                        -< [Button [Type "button"; Class "btn btn-default"; Id "create-vote-room"] -< [Text "Create"]]]
                    ]
                ] }
    
    let ManageVotePage votingRoomName =
        let url = "/vote/" + votingRoomName
        Content.WithTemplate Templating.TemplateSubmitVote
          ([Text "Manage "; ahref url url],
           [])
    
    let VotePage votingRoomName = async {
        let! votingRoom = AppState.postMessageAndReply votingRoomName RetrieveState
        match votingRoom with
        | Some(votingRoom) ->
            // WebSharper templating automatically performs escaping here, so it's safe
            // to just stitch strings together in this case
            return! Content.WithTemplate Templating.TemplateSubmitVote
                       ([Text ("Vote in /vote/" + votingRoomName)],
                        [Div [ClientSide <@ Client.form_submitVote votingRoomName votingRoom @>]])
        | None -> return! IndexPage votingRoomName }

    let ViewVotePage votingRoomName =
        Content.WithTemplate Templating.TemplateSubmitVote
          ([Text ("Viewing: " + votingRoomName)],
           [Div [ClientSide <@ Client.form_viewVote votingRoomName @>]])

    let Main : Sitelet<EndPoint> =
        Sitelet.Infer (fun context endPoint ->
            try
                match endPoint with
                | Index -> IndexPage ""
                | ManageVote(escapedVotingRoomName) -> ManageVotePage (Uri.UnescapeDataString escapedVotingRoomName)
                | Vote(escapedVotingRoomName) -> VotePage (Uri.UnescapeDataString escapedVotingRoomName)
                | ViewVote(escapedVotingRoomName) -> ViewVotePage (Uri.UnescapeDataString escapedVotingRoomName)
            with e ->
                System.Console.Error.WriteLine ("Error while serving page:" + e.Message)
                raise e)

open System.Net.NetworkInformation
open System.Net.Sockets

module Main =
    [<EntryPoint>]
    let main args =
        let hosts =
            if args.Length > 0 then
                Array.toList args
            else
                let localIp =
                    NetworkInterface.GetAllNetworkInterfaces ()
                    |> Seq.tryPick (fun netInterface ->
                        match netInterface.NetworkInterfaceType with
                        | NetworkInterfaceType.Wireless80211 | NetworkInterfaceType.Ethernet ->
                            netInterface.GetIPProperties().UnicastAddresses
                            |> Seq.tryPick (fun addrInfo ->
                                if addrInfo.Address.AddressFamily = AddressFamily.InterNetwork then Some(string addrInfo.Address) else None)
                        | _ -> None)
                List.choose id [localIp; Some "localhost"] |> List.map (fun host -> host + ":8080")
        let urls = hosts |> List.map (fun host -> "http://" + host)
        WebSharper.Warp.RunAndWaitForInput (Site.Main, urls = urls, debug = true)