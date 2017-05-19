namespace TwoThumbsUp
open System
open WebSharper.Html.Server
open WebSharper
open WebSharper.Sitelets

type EndPoint = | [<EndPoint "GET /">] Index

module Templating =
    open System.Web

    type Page = { Title: string; Body: Element list }

    let MainTemplate =
        Content.Template<Page>("~/Main.html")
               .With("title", fun x -> x.Title)
               .With("body", fun x -> x.Body)
    
    let Main context endPoint title body =
        Content.WithTemplate MainTemplate { Title = title; Body = body }

module Site =
    let IndexPage context =
        Templating.Main context EndPoint.Index "Hello WebSharper" [
            H2 [Text "Welcome to Hello WebSharper."]
            I [Text "Fancy stuff here"]
        ]

    let Main : Sitelet<EndPoint> =
        Sitelet.Infer (fun context endPoint ->
            try
                match endPoint with
                | Index -> IndexPage context
            with e ->
                System.Console.Error.WriteLine ("Error while serving page:\n" + e.Message)
                //|> Async.AwaitTask |> Async.Start
                raise e)

module Main =
    let rec loop () = loop ()

    [<EntryPoint>]
    let main args =
        WebSharper.Warp.RunAndWaitForInput Site.Main |> ignore
        loop ()