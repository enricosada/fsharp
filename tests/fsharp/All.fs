module All

open System
open System.IO
open System.Diagnostics
open NUnit.Framework

type CmdResult = Success | ErrorLevel of int

type CmdArguments = {
    RedirectOutput: (string -> unit) option;
    RedirectError: (string -> unit) option;
    RedirectInput: (StreamWriter -> unit) option;
}

let exec' cmdArgs workDir envs path arguments =
    //TODO gestione errore
    let processInfo = new ProcessStartInfo(path, arguments)
    processInfo.CreateNoWindow <- true
    processInfo.UseShellExecute <- false

    let p = new Process()
    p.StartInfo <- processInfo

    p.StartInfo.WorkingDirectory <- workDir

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

    p.Start() |> ignore
    
    cmdArgs.RedirectOutput |> Option.iter (fun _ -> p.BeginOutputReadLine())
    cmdArgs.RedirectError |> Option.iter (fun _ -> p.BeginErrorReadLine())

    cmdArgs.RedirectInput
    |> Option.iter (fun input ->
        let inputWriter = p.StandardInput
        input inputWriter
    )

    p.WaitForExit()

    // Read the streams

    let exitCode = p.ExitCode
    p.Close()

    match exitCode with
    | 0 -> Success
    | err -> ErrorLevel err

let whereCommand cmd =
    let result = ref None
    let lastLine = function null -> () | l -> result := Some l

    let cmdArgs = {
        RedirectOutput = Some lastLine;
        RedirectError = None;
        RedirectInput = None;
    }
    
    match exec' cmdArgs (Path.GetTempPath()) Map.empty "cmd.exe" (sprintf "/c where %s" cmd) with
    | ErrorLevel _ -> None
    | OK -> !result
    

let fileExists path = if path |> File.Exists then Some path else None

type BuildResult = OK | Error of int
type RunResult = OK | Error of (int * string) | Skipped of string


let echo = printfn


type TestConfig = {
    EnvironmentVariables: Map<string,string>
    ALINK: string;
    CORDIR: string;
    CORSDK: string;
    CSC: string;
    csc_flags: string;
    FSC: string;
    fsc_flags: string;
    FSCBinPath: string;
    FSCOREDLL20PATH: string;
    FSCOREDLLPATH: string;
    FSCOREDLLPORTABLEPATH: string;
    FSCOREDLLNETCOREPATH: string;
    FSCOREDLLNETCORE78PATH: string;
    FSCOREDLLNETCORE259PATH: string;
    FSDATATPPATH: string;
    FSDIFF: string;
    FSI: string;
    fsi_flags: string;
    GACUTIL: string;
    ILDASM: string;
    INSTALL_SKU: INSTALL_SKU option;
    MSBUILDTOOLSPATH: string option;
    NGEN: string;
    PEVERIFY: string;
    RESGEN: string;
}
and INSTALL_SKU = Clean | DesktopExpress | WebExpress | Ultimate

let private env key = 
    match Environment.GetEnvironmentVariable(key) with
    | null -> None
    | "" -> None
    | x -> Some x

type Commands = {
    echo_tofile: string -> string -> unit;
    copy_y: string list -> string -> unit;
    type_append_tofile: string list -> string -> unit;
    fsc: string -> string list -> CmdResult;
    fsc_flags: string;
    peverify: string -> CmdResult;
    clix: string -> string -> CmdResult;
    fsi: string -> string list -> CmdResult;
    fsiIn: string -> string list -> CmdResult;
    fsi_flags: string;
    csc: string -> string list -> CmdResult;
    csc_flags: string;
    FsharpCoreDllPath: string;
}
    
let withFileGuard path f =
    //  if exist test.ok (del /f /q test.ok)
    path |> fileExists |> Option.iter File.Delete
    //  %CLIX% "%FSI%" %fsi_flags% < %sources% && (
    //  dir test.ok > NUL 2>&1 ) || (
    //  @echo FSI_STDIN failed;
    //  set ERRORMSG=%ERRORMSG% FSI_STDIN failed;
    //  )
    match f () with
    | ErrorLevel err -> Error (err, sprintf "exit code %i" err)
    | Success ->
        match path |> fileExists with
        | Some _ -> OK
        | None -> Error (0, sprintf "exit code 0 but %s file doesn't exists" (Path.GetFileName(path)))

type Permutation = FSI_FILE | FSI_STDIN | FSI_STDIN_OPT | FSI_STDIN_GUI | FSC_BASIC | FSC_BASIC_64 | FSC_HW | FSC_O3 | GENERATED_SIGNATURE | EMPTY_SIGNATURE | EMPTY_SIGNATURE_OPT | FSC_OPT_MINUS_DEBUG | FSC_OPT_PLUS_DEBUG | FRENCH | SPANISH | AS_DLL | WRAPPER_NAMESPACE | WRAPPER_NAMESPACE_OPT
