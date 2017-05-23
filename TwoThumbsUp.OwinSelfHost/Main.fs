namespace TwoThumbsUp
open System
open WebSharper.Html.Server
open WebSharper
open WebSharper.Sitelets

type EndPoint =
    | [<EndPoint "GET /">] Index
    | [<EndPoint "GET /vote">] Vote of escapedVotingRoomName: string
    | [<EndPoint "GET /view">] ViewVote of escapedVotingRoomName: string

module Templating =
    open System.Web

    let TemplateCreateVote =
        Content.Template<_>("~/CreateVote.html")
               .With("vote-name", fun (voteName: string, _) -> voteName)
               .With("client-scripts", fun (_, clientCode: Element list) -> clientCode)

    let TemplateSubmitVote =
        Content.Template<_>("~/SubmitVote.html")
               .With("title", fun (title: string, _) -> title)
               .With("client-scripts", fun (_, clientCode: Element list) -> clientCode)

module Site =
    let IndexPage defaultVotingRoomName =
        Content.WithTemplate Templating.TemplateCreateVote
            (defaultVotingRoomName, [Div [ClientSide <@ Client.form_createVote defaultVotingRoomName @> ]])
    
    let VotePage votingRoomName = async {
        let! votingRoom = AppState.postMessageAndReply votingRoomName RetrieveState
        match votingRoom with
        | Some(votingRoom) ->
            // WebSharper templating automatically performs escaping here, so it's safe
            // to just stitch strings together in this case
            return! Content.WithTemplate Templating.TemplateSubmitVote
                        ("Voting: " + votingRoomName,
                        [Div [ClientSide <@ Client.form_submitVote votingRoomName votingRoom @>]])
        | None -> return! IndexPage votingRoomName }

    let ViewVotePage votingRoomName =
        Content.WithTemplate Templating.TemplateSubmitVote
            ("Viewing: " + votingRoomName, [Div [ClientSide <@ Client.form_viewVote votingRoomName @>]])

    let Main : Sitelet<EndPoint> =
        Sitelet.Infer (fun context endPoint ->
            try
                match endPoint with
                | Index -> IndexPage ""
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
        let localIp =
            NetworkInterface.GetAllNetworkInterfaces ()
            |> Seq.tryPick (fun netInterface ->
                match netInterface.NetworkInterfaceType with
                | NetworkInterfaceType.Wireless80211 | NetworkInterfaceType.Ethernet ->
                    netInterface.GetIPProperties().UnicastAddresses
                    |> Seq.tryPick (fun addrInfo ->
                        if addrInfo.Address.AddressFamily = AddressFamily.InterNetwork then Some(string addrInfo.Address) else None)
                | _ -> None)
        let urls = List.choose id [localIp; Some "localhost"] |> List.map (fun host -> "http://" + host + ":8080")
        WebSharper.Warp.RunAndWaitForInput (Site.Main, urls = urls, debug = true)