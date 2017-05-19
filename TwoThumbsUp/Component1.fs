namespace TwoThumbsUp
open System
open System.Threading.Tasks
open System.Web
open System.Web.Http
open WebSharper
open WebSharper.Sitelets
open WebSharper.Owin
open global.Owin
open Microsoft.Owin
open Microsoft.Owin.Extensions
open Microsoft.Owin.StaticFiles
open Microsoft.Owin.FileSystems

type EndPoint = | [<EndPoint "/index">] Index

module Site =
    module Pages =
        let _ = 42
    
    let Main : Sitelet<EndPoint> =
        printfn "got here"
        Sitelet.Infer (fun context -> function
            | Index -> Content.Text ("Hello world from WebSharper", Text.Encoding.ASCII)
        )

type Website() =
    interface IWebsite<EndPoint> with
        member this.Sitelet = Site.Main
        member this.Actions = [ Index ]


type Startup =
    
    static member Configuration (app: IAppBuilder) =
        let webRoot = HttpRuntime.AppDomainAppPath
        //app.UseStaticFiles(StaticFileOptions(FileSystem = PhysicalFileSystem rootDirectory))
           //.UseSitelet(rootDirectory, Site.Main) |> ignore
        
        //app.Use (fun context next ->
            //context.Response.ContentType <- "text/plain"
            //let task = context.Response.WriteAsync "Hello world from middleware"
            //Task.WaitAll [|task; next.Invoke ()|]) |> ignore
        let options = WebSharperOptions(ServerRootDirectory = webRoot).WithSitelet(Site.Main)
        app.UseWebSharper options |> ignore
        app.Run (fun context ->
            context.Response.ContentType <- "text/plain"
            context.Response.WriteAsync "Hello world from OWIN")
        
        //app.Run (fun context -> upcast Task.FromResult<Object>(null))
        app.UseStageMarker PipelineStage.MapHandler |> ignore
        

type Global() =
    inherit HttpApplication()
    member private this.Application_Start () =
        printfn "application_start"

[<assembly: OwinStartup(typeof<Startup>)>]
[<assembly: Website(typeof<Website>)>]
do ()

(*
type HttpRouteDefaults = { Controller : string; Id : obj }
 
type Global() =
    inherit System.Web.HttpApplication()
    member this.Application_Start (sender : obj) (e : EventArgs) =
        GlobalConfiguration.Configuration.Routes.MapHttpRoute(
            "DefaultAPI",
            "{controller}/{id}",
            { Controller = "Home"; Id = RouteParameter.Optional }) |> ignore

*)