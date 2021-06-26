open System
open System.IO
open System.Net
open System.Text.RegularExpressions
open System.Threading
open FSharp.Data

type RFCIndexEntry = { id: int; title: string; url: string }

[<Literal>]
let rfcEntrySample = """
{
    "id": 123,
    "title": "Title",
    "url": "http://www.example.com"
}
"""

type RFCEntryJSON = JsonProvider<rfcEntrySample, RootName="entry">

type DownloadUpdateMessage =
    | Ok of string
    | Fail of string
    | Done

let downloadStatusAgent =
    MailboxProcessor<DownloadUpdateMessage>.Start
        (fun inbox ->
            let rec loop () =
                async {
                    let! msg = inbox.Receive()

                    match msg with
                    | Ok (x) -> printfn "Downloaded %s" x
                    | Fail (x) -> printfn "Failed to download %s" x
                    | Done -> return ()

                    return! loop ()
                }

            loop ())


let downloadFile (downloadLocation: string) (entry: RFCIndexEntry) =
    async {
        let url = entry.url
        let! cancelToken = Async.CancellationToken

        try
            let request =
                WebRequest.Create(url) :?> HttpWebRequest

            request.AllowAutoRedirect <- true
            request.MaximumAutomaticRedirections <- 4
            request.Timeout <- 5000

            let! response = request.AsyncGetResponse()
            let responsePath = response.ResponseUri.AbsolutePath

            let filePath =
                Path.Join(downloadLocation, responsePath + ".html")

            let fileInfo = FileInfo(filePath)
            fileInfo.Directory.Create()

            use stream = response.GetResponseStream()

            use fstream =
                new FileStream(fileInfo.FullName, FileMode.Create)

            stream.CopyTo(fstream)

            downloadStatusAgent.Post(Ok(url.ToString()))
            return Some({ entry with url = responsePath })
        with
        | :? OperationCanceledException -> return None
        | error ->
            downloadStatusAgent.Post(Fail(url.ToString()))
            return None
    }

let writeIndex filePath index =
    let itemValues =
        (index
         |> List.toArray
         |> Array.map
             (fun x ->
                 RFCEntryJSON
                     .Entry(
                         id = x.id,
                         title = x.title,
                         url = x.url
                     )
                     .JsonValue)
         |> JsonValue.Array)
            .ToString(JsonSaveOptions.DisableFormatting)

    File.WriteAllText(filePath, itemValues)

let parseIndex () =
    async {
        let url =
            Uri("https://tools.ietf.org/rfc/mini-index")

        let! loadedDocument = HtmlDocument.AsyncLoad(url.ToString())

        let mapToRFCEntry =
            fun (link: HtmlNode) ->
                let av = link.AttributeValue

                { id = int (av ("id"))
                  title = av ("title")
                  url = (Uri(url, (av "href"))).ToString() }

        return
            loadedDocument.CssSelect("a.s")
            |> List.map mapToRFCEntry
    }

[<EntryPoint>]
let main argv =
    match argv |> Array.toList with
    | indexPath :: downloadPath :: _ ->
        printfn "Index path = %s, Download path = %s" indexPath downloadPath

        use cancellationTokenSource = new CancellationTokenSource()

        Console.CancelKeyPress.Add
            (fun _ ->
                printfn "Cancelling..."

                try
                    cancellationTokenSource.Cancel()
                with _ -> ())

        let downloadTasks =
            parseIndex ()
            |> Async.RunSynchronously
            |> List.take 100
            |> List.map (downloadFile downloadPath)
            |> (fun ts -> Async.Parallel(ts, 5))


        try
            Async.RunSynchronously(downloadTasks, cancellationToken = cancellationTokenSource.Token)
            |> Array.choose id
            |> List.ofArray
            |> (writeIndex indexPath)
            |> ignore
        with _ -> ()

        downloadStatusAgent.Post(Done)

        0
    | _ ->
        printfn "Invalid input recieved"
        1
