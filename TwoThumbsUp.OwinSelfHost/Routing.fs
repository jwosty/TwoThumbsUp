namespace TwoThumbsUp
open System
open WebSharper
open WebSharper.Sitelets
open WebSharper.Sitelets.UrlHelpers

module Routing =
    [<JavaScript>]
    type EndPoint =
        | Index
        | ManageVote of votingRoomName: string
        | Brainstorm of votingRoomName: string
        | Vote of votingRoomName: string
        | ViewVote of votingRoomName: string
        | NotFound of string

    let route request =
        let action =
            match request with
            | GET (_, SPLIT_BY '/' []) -> Some Index
            | GET (_, SPLIT_BY '/' ["manage"; roomName]) -> Some (ManageVote roomName)
            | GET (_, SPLIT_BY '/' ["brainstorm"; roomName]) -> Some (Brainstorm roomName)
            | GET (_, SPLIT_BY '/' ["vote"; roomName]) -> Some (Vote roomName)
            | GET (_, SPLIT_BY '/' ["vote"; roomName; "view"]) -> Some (ViewVote roomName)
            | _ -> None
        match action with
        | Some endPoint -> Some endPoint
        | None -> Some (NotFound request.Uri.AbsolutePath)

    [<JavaScript>]
    let link = function
        | Index -> "/"
        | NotFound badPath -> badPath
        | ManageVote roomName -> sprintf "/manage/%s" roomName
        | Brainstorm roomName -> sprintf "/brainstorm/%s" roomName
        | Vote roomName -> sprintf "/vote/%s" roomName
        | ViewVote roomName -> sprintf "/vote/%s/view" roomName