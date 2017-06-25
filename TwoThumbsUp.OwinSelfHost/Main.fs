namespace TwoThumbsUp
open System
open TwoThumbsUp.Routing
open WebSharper.Html.Server
open WebSharper
open WebSharper.Sitelets

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

    let NotFoundPage endPoint =
        Content.WithTemplate Templating.MainTemplate {
            browserTitle = "Page not found - TwoThumbsUp"
            title = "Page not found"
            content =
               [Span [Class "text-sub-heading"]
                -< [Text "The requested URL "
                    B [Text (link endPoint)]
                    Text " does not exist."]
                ] }

    let CreateVotePage votingRoomName endPoint =
        Content.WithTemplate Templating.MainTemplate
          { browserTitle = "TwoThumbsUp - Create voting room"
            title = "Create a voting room"
            content = [Div [ClientSide <@ Client.form_createVote votingRoomName @>]] }
    
    let IndexPage = CreateVotePage ""

    let ManageVotePage votingRoomName endPoint =
        let url = "/vote/" + votingRoomName
        Content.WithTemplate Templating.TemplateSubmitVote
          ([Text "Manage "; ahref url url],
           [])
    
    let VotePage votingRoomName endPoint = async {
        let! votingRoom = AppState.postMessageAndReply votingRoomName RetrieveState
        match votingRoom with
        | Some(votingRoom) ->
            // WebSharper templating performs escaping
            return!
                Content.WithTemplate Templating.MainTemplate
                  { browserTitle = "Vote! - TwoThumbsUp"; title = "Vote in /vote/" + votingRoomName
                    content = [Div [ClientSide <@ Client.form_submitVote votingRoomName votingRoom @>]] }
        | None -> return! CreateVotePage votingRoomName endPoint }

    let ViewVotePage votingRoomName endPoint =
        Content.WithTemplate Templating.TemplateSubmitVote
          ([Text ("Viewing: " + votingRoomName)],
           [Div [ClientSide <@ Client.form_viewVote votingRoomName @>]])

    let Controller =
        { Handle = fun action ->
            try
                let makePage =
                    match action with
                    | NotFound url -> NotFoundPage
                    | Index -> IndexPage
                    | Vote roomName -> VotePage roomName
                    | ManageVote roomName -> ManageVotePage roomName
                    | ViewVote roomName -> ViewVotePage roomName
                
                Content.FromContext (fun ctx -> makePage action)
                |> Content.FromAsync
            with e ->
                printfn "500 internal server error: %A" (e.ToString ())
                reraise () }
    
    let MainSitelet =
      { Router = Router.New route (fun x -> Some(new Uri(link x, UriKind.Relative)))
        Controller = Controller }

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
        WebSharper.Warp.RunAndWaitForInput (Site.MainSitelet, urls = urls, debug = true)