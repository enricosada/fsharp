module PlatformHelpers

type PROCESSOR_ARCHITECTURE = X86 | IA64 | AMD64 | Unknown of string
    with override this.ToString() = match this with X86 -> "x86" | IA64 -> "IA64" | AMD64 -> "AMD64" | Unknown arc -> arc

let parseProcessorArchitecture (s: string) =
    match s.ToUpper() with
    | "X86" -> X86
    | "IA64" -> IA64
    | "AMD64" -> AMD64
    | arc -> Unknown s


//  %~i	    -   expands %i removing any surrounding quotes (")
//  %~fi	-   expands %i to a fully qualified path name
//  %~di	-   expands %i to a drive letter only
//  %~pi	-   expands %i to a path only
//  %~ni	-   expands %i to a file name only
//  %~xi	-   expands %i to a file extension only
//  %~si	-   expanded path contains short names only

open System.IO

let inline (/) a b = Path.Combine(a,b)

let fileExists path = if path |> File.Exists then Some path else None

type CmdResult = Success | ErrorLevel of int

type CmdArguments = {
    RedirectOutput: (string -> unit) option;
    RedirectError: (string -> unit) option;
    RedirectInput: (StreamWriter -> unit) option;
}

type FilePath = string

open System.Diagnostics

let exec' cmdArgs (workDir: FilePath) envs (path: FilePath) arguments =
    //TODO gestione errore
    let processInfo = new ProcessStartInfo(path, arguments)
    processInfo.CreateNoWindow <- true
    processInfo.UseShellExecute <- false
    processInfo.WorkingDirectory <- workDir

    let p = new Process()
    p.EnableRaisingEvents <- true
    p.StartInfo <- processInfo

    cmdArgs.RedirectOutput
    |> Option.map (fun f -> (fun (ea: DataReceivedEventArgs) -> ea.Data |> f)) 
    |> Option.iter (fun newOut ->
        processInfo.RedirectStandardOutput <- true
        p.OutputDataReceived.Add newOut
    )

    cmdArgs.RedirectError 
    |> Option.map (fun f -> (fun (ea: DataReceivedEventArgs) -> ea.Data |> f)) 
    |> Option.iter (fun newErr ->
        processInfo.RedirectStandardError <- true
        p.ErrorDataReceived.Add newErr
    )

    cmdArgs.RedirectInput
    |> Option.iter (fun _ -> p.StartInfo.RedirectStandardInput <- true)

    let exitedAsync (proc: Process) =
        let tcs = new System.Threading.Tasks.TaskCompletionSource<int>();
        p.Exited.Add (fun s -> 
            tcs.TrySetResult(proc.ExitCode) |> ignore
            proc.Dispose())
        tcs.Task

    p.Start() |> ignore
    
    cmdArgs.RedirectOutput |> Option.iter (fun _ -> p.BeginOutputReadLine())
    cmdArgs.RedirectError |> Option.iter (fun _ -> p.BeginErrorReadLine())

    cmdArgs.RedirectInput
    |> Option.iter (fun input ->
        let pipeInput = async {
            let inputWriter = p.StandardInput
            input inputWriter
            do! inputWriter.FlushAsync () |> Async.AwaitIAsyncResult |> Async.Ignore
            inputWriter.Close ()
        }
        pipeInput |> Async.Start
    )

    let exitCode = p |> exitedAsync |> Async.AwaitTask |> Async.RunSynchronously

    match exitCode with
    | 0 -> Success
    | err -> ErrorLevel err

let log format = Printf.ksprintf (fun s -> printfn "%s" s) format
