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
    WorkingDirectory: string;
}

let exec' cmdArgs path arguments =
    let path = Path.GetFullPath(path)
    printfn "%s" (sprintf "%s %s" path arguments)
    let processInfo = new ProcessStartInfo(path, arguments)
    processInfo.CreateNoWindow <- true
    processInfo.UseShellExecute <- false

    let p = new Process()
    p.StartInfo <- processInfo

    p.StartInfo.WorkingDirectory <- cmdArgs.WorkingDirectory

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

let cmdExePath = lazy (
    let systemRoot = System.Environment.GetEnvironmentVariable("SystemRoot")
    Path.Combine(systemRoot, "system32", "cmd.exe")
)

let exec_bat_in workDir path =
    let cmdArgs = {
        WorkingDirectory = workDir;
        RedirectOutput = Some (printfn "%s");
        RedirectError = Some (printfn "%s");
        RedirectInput = None;
    }
    exec' cmdArgs (cmdExePath.Value) (sprintf "/c %s" path)

let exec_bat_in' workDir path input =
    let cmdArgs = {
        WorkingDirectory = workDir;
        RedirectOutput = Some (printfn "%s");
        RedirectError = Some (printfn "%s");
        RedirectInput = Some input;
    }
    exec' cmdArgs (cmdExePath.Value) (sprintf "/c %s" path)

let exec_bat path = exec_bat_in (Path.GetDirectoryName(path)) path

let whereCommand cmd =
    let result = ref None
    let lastLine = function null -> () | l -> result := Some l

    let cmdArgs = {
        WorkingDirectory = Path.GetTempPath();
        RedirectOutput = Some lastLine;
        RedirectError = None;
        RedirectInput = None;
    }
    
    match exec' cmdArgs (cmdExePath.Value) (sprintf "/c where %s" cmd) with
    | ErrorLevel _ -> None
    | OK -> !result

let createTempDir () =
    let path = Path.GetTempFileName ()
    File.Delete path
    Directory.CreateDirectory path |> ignore
    path

let convertToShortPathBat = lazy (
    let bat = Path.Combine(createTempDir (), "ConvertToShortPath.bat")
    File.WriteAllLines (bat, [| "@ECHO OFF"; """for /f "delims=" %%I in (%1) do echo %%~dfsI""" |] )
    bat
)

let convertToShortPath path =
    let result = ref None
    let lastLine = function null -> () | l -> result := Some l

    let cmdArgs = {
        WorkingDirectory = Path.GetTempPath();
        RedirectOutput = Some lastLine;
        RedirectError = Some (printfn "%s");
        RedirectInput = None;
    }
    
    match exec' cmdArgs (cmdExePath.Value) (sprintf """/c ""%s" "%s"" """ convertToShortPathBat.Value path) with
    | ErrorLevel _ -> path
    | OK -> match !result with None -> path | Some p -> p
    
    
let createFSharpTest () =
    let baseDir = Path.Combine(__SOURCE_DIRECTORY__, "..", "fsharp")
    
    let testCaseData (path: string) = 
        let name = sprintf "test: %s" (path.Replace(baseDir, ""))
        (new TestCaseData( path ))
            .SetName(name)
            .SetCategory("4")
            .SetDescription("An exception is expected")
    
    Directory.EnumerateDirectories(baseDir, "*", SearchOption.AllDirectories)
    |> Seq.filter (fun d -> DirectoryInfo(d).EnumerateDirectories() |> Seq.isEmpty)
    |> Seq.take 2
    |> Seq.map testCaseData

type Permutation = FSI_FILE | FSI_STDIN | FSI_STDIN_OPT | FSI_STDIN_GUI | FSC_BASIC | FSC_BASIC_64 | FSC_HW | FSC_O3 | GENERATED_SIGNATURE | EMPTY_SIGNATURE | EMPTY_SIGNATURE_OPT | FSC_OPT_MINUS_DEBUG | FSC_OPT_PLUS_DEBUG | FRENCH | SPANISH | AS_DLL | WRAPPER_NAMESPACE | WRAPPER_NAMESPACE_OPT

let createFSharpTestPermu list =
    let testCaseData (p: Permutation) = 
        let name = sprintf "%A" p
        (new TestCaseData( p ))
            .SetName(name)
            .SetCategory("4")
            .SetDescription("An exception is expected")
    
    list
    |> Seq.map testCaseData

let allPermutation = 
            [ FSI_FILE; FSI_STDIN; FSI_STDIN_OPT; FSI_STDIN_GUI; FSC_BASIC; FSC_HW; FSC_O3; GENERATED_SIGNATURE; EMPTY_SIGNATURE; EMPTY_SIGNATURE_OPT; FSC_OPT_MINUS_DEBUG; FSC_OPT_PLUS_DEBUG; FRENCH; SPANISH; AS_DLL; WRAPPER_NAMESPACE; WRAPPER_NAMESPACE_OPT ]

type FSharpSuite () =
    static member TestCases
        with get() = createFSharpTest ()

    static member AllPermutations
        with get() = createFSharpTestPermu allPermutation

let fileExists path = if path |> File.Exists then Some path else None

type BuildResult = OK | Error of int
type RunResult = OK | Error of (int * string) | Skipped of string

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


let echo = printfn

/// copy /y %source1% tmptest2.ml
let copy_y workDir source to' = 
    source |> List.iter (fun s -> File.Copy(Path.Combine(workDir, s), Path.Combine(workDir, to'), true))

/// echo // empty file  > tmptest2.mli
let echo_tofile workDir text p = 
    File.WriteAllText(Path.Combine(workDir, p), text)

/// type %source1%  >> tmptest3.ml
let type_append_tofile workDir source p =
    let append_tofile f t =
        let from = Path.Combine(workDir, f)
        let to' = Path.Combine(workDir, t)
        File.AppendAllText( from, File.ReadAllText(to'))
    source |> List.iter (fun s -> append_tofile s p)

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

//let private envOrDefault key def = match env key with Some x -> x | None -> def
//let private envOrFail key = match env key with Some x -> x | None -> failwithf "environment variable '%s' required " key

type Commands = {
    echo_tofile: string -> string -> unit;
    copy_y: string list -> string -> unit;
    type_append_tofile: string list -> string -> unit;
    fsc: string -> string list -> CmdResult;
    peverify: string -> CmdResult;
    clix: string -> string -> CmdResult;
    fsi: string -> string list -> CmdResult;
    fsiIn: string -> string list -> CmdResult;
    csc: string -> string list -> CmdResult;
}

let getHelpers cfg workDir = 
    let exec input =
        exec' {
            WorkingDirectory = workDir;
            RedirectOutput = Some (printfn "%s");
            RedirectError = Some (printfn "%s");
            RedirectInput = input;
        }

    let zipBlank = List.fold (fun s t -> s + " " + t) ""
        
    let fsc flags srcFiles =
        //TODO envvars
        // "%FSC%" %fsc_flags% --define:COMPILING_WITH_EMPTY_SIGNATURE -o:tmptest2.exe tmptest2.mli tmptest2.ml
        exec None cfg.FSC (sprintf "%s%s"  flags (srcFiles |> zipBlank))

    let csc flags srcFiles =
        //TODO envvars
        // "%FSC%" %fsc_flags% --define:COMPILING_WITH_EMPTY_SIGNATURE -o:tmptest2.exe tmptest2.mli tmptest2.ml
        exec None cfg.CSC (sprintf "%s%s"  flags (srcFiles |> zipBlank))

    let clix =
        //TODO env args
        exec None

    let fsi flags sources =
        //TODO env args
        exec None cfg.FSI (sprintf "%s%s" flags (sources |> zipBlank))

    let fsiIn flags sources =
        let inputWriter (writer: StreamWriter) =
            let pipeFile name =
                use reader = Path.Combine(workDir,name) |> File.OpenRead
                use ms = new MemoryStream()
                reader.CopyTo (ms)
                ms.Position <- 0L
                try
                    ms.CopyTo(writer.BaseStream)
                with 
                | :? System.IO.IOException as ex -> //input closed is ok if process is closed
                    ()
            sources |> List.iter pipeFile

        exec (Some inputWriter) cfg.FSI (sprintf "%s%s" flags (sources |> zipBlank))

    let peverify path = 
        //TODO env args
        exec None cfg.PEVERIFY path

    { echo_tofile = echo_tofile workDir;
      copy_y = copy_y workDir;
      type_append_tofile = type_append_tofile workDir;
      fsc = fsc;
      peverify = peverify;
      clix = clix;
      fsi = fsi;
      fsiIn = fsiIn; 
      csc = csc }

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
