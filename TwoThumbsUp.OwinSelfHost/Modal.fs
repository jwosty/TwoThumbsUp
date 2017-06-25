namespace TwoThumbsUp
open System
open TwoThumbsUp.HelperFunctions
open TwoThumbsUp.HelperFunctions.Client
open WebSharper
open WebSharper.Html.Client
open WebSharper.JavaScript
open WebSharper.JQuery

type Modal<'a> =
  { header: seq<Pagelet>; body: seq<Pagelet>; footer: seq<Pagelet>
    addCloseHandlers: ('a -> unit) -> unit; defaultCloseHandler: unit -> 'a }

[<JavaScript>]
module Modal =
    let [<Inline "$el.modal('show')">] private modalShow (el: JQuery) = X<unit>
    let [<Inline "$el.modal('hide')">] private modalHide (el: JQuery) = X<unit>
    
    let create defaultCloseHandler addCloseHandler
            (title: #seq<Pagelet>) (footer: #seq<Pagelet>) (body: #seq<Pagelet>) =
      { header = title; body = body; footer = footer
        addCloseHandlers = addCloseHandler; defaultCloseHandler = defaultCloseHandler }

    let confirm title (confirmClass, confirmLabel) (cancelClass, cancelLabel) (body: #seq<Pagelet>) =
        let confirmButton = Button [Type "button"; Class confirmClass] -< [Text confirmLabel]
        let cancelButton = Button [Type "button"; Class cancelClass] -< [Text cancelLabel]
        let addCloseHandlers closeModal =
            confirmButton |> OnClick (fun x e -> closeModal true)
            cancelButton |> OnClick (fun x e -> closeModal false)
        create (fun _ -> false) addCloseHandlers
            [H4 [Text title]]
            [confirmButton; cancelButton]
            body

    let show modal =
        let cssId = "label-" + guidToString (Guid.NewGuid ())

        let html_modal =
            Div [Class "modal fade"; TabIndex "-1"; Role "dialog"; AriaLabelledBy cssId; NewAttr "aria-hidden" "true"]
            -< [Div [Class "modal-dialog"; Style "margin-top: 100px;"]
                -< [Div [Class "modal-content"]
                    -< [Div [Class "container-fixed"]
                        -< [Div [Class "modal-header"]
                            -< [Button [Type "button"; Class "close"; DataDismiss "modal"; AriaLabel "Close"]
                                -< [Span [NewAttr "aria-hidden" "true"; Text "×"]]]
                            -< modal.header
                            Div [Class "modal-body"] -< modal.body
                            Div [Class "modal-footer"] -< modal.footer
                            ]
                        ]
                    ]
                ]
        
        html_modal.Render ()
        let modalDom = JQuery html_modal.Dom
        modalShow modalDom

        Async.FromContinuations (fun (cont, err, cancel) ->
            let mutable hasCompleted = false
            modal.addCloseHandlers (fun result ->
                hasCompleted <- true
                modalHide modalDom
                cont result)
            modalDom.On("hide.bs.modal", fun x e ->
                if not hasCompleted then
                    hasCompleted <- true
                    cont (modal.defaultCloseHandler ())) |> ignore
            modalDom.On("hidden.bs.modal", fun x e -> html_modal.Remove ()).Ignore)