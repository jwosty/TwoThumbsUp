namespace TwoThumbsUp
open System
open WebSharper.Html.Server
open WebSharper
open WebSharper.Sitelets

type EndPoint = | [<EndPoint "GET /">] Index

module Templating =
    open System.Web

    type Page = { title: string; bodyTitle: string; body: Element list;  }

    let MainTemplate =
        Content.Template<Page>("~/Main.html")
               .With("title", fun x -> x.title)
               .With("bodyTitle", fun x -> x.bodyTitle)
               .With("body", fun x -> x.body)
    
    let Main context endPoint title bodyTitle body =
        Content.WithTemplate MainTemplate { title = title; bodyTitle = bodyTitle; body = body }

module Site =
    let IndexPage context =
        Templating.Main context EndPoint.Index "Create vote" "Create vote" [
            I [Text "Fancy stuff here"]
        ]

    let Main : Sitelet<EndPoint> =
        Sitelet.Infer (fun context endPoint ->
            try
                match endPoint with
                | Index -> IndexPage context
            with e ->
                System.Console.Error.WriteLine ("Error while serving page:\n" + e.Message)
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