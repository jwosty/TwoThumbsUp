module TwoThumbsUp.HelperFunctions
open System
open WebSharper

[<JavaScript>]
let guidToString (guid: Guid) = (string guid).Replace ("-", "")

module Client =
    open WebSharper.Html.Client
    [<AutoOpen; JavaScript>]
    module Attr =
        let Role = NewAttr "role"
        let DataDismiss = NewAttr "data-dismiss"
        let AriaLabel = NewAttr "aria-label"
        let AriaLabelledBy = NewAttr "aria-labelledby"