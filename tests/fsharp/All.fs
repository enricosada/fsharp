module All

open System
open System.IO
open System.Diagnostics
open NUnit.Framework

type CmdResult = Success | ErrorLevel of int

let exec_bat_in workDir path =
    printfn "%s" path
    let processInfo = new ProcessStartInfo("cmd.exe", "/c " + path)
    processInfo.WorkingDirectory <- workDir
    processInfo.CreateNoWindow <- true
    processInfo.UseShellExecute <- false
    // Redirect the output
    processInfo.RedirectStandardError <- true
    processInfo.RedirectStandardOutput <- true

    let p = new Process()
    p.StartInfo <- processInfo

    let log (x: DataReceivedEventArgs) = printfn "%s" x.Data

    p.OutputDataReceived.Add log
    p.ErrorDataReceived.Add log

    p.Start() |> ignore
    p.BeginOutputReadLine()
    p.BeginErrorReadLine()

    p.WaitForExit()

    // Read the streams

    let exitCode = p.ExitCode
    p.Close()

    match exitCode with
    | 0 -> Success
    | err -> ErrorLevel err


let exec_bat_in' workDir path input =
    printfn "%s" path
    let processInfo = new ProcessStartInfo("cmd.exe", "/c " + path)
    processInfo.WorkingDirectory <- workDir
    processInfo.CreateNoWindow <- true
    processInfo.UseShellExecute <- false
    // Redirect the output
    processInfo.RedirectStandardError <- true
    processInfo.RedirectStandardOutput <- true
    processInfo.RedirectStandardInput <- true

    let p = new Process()
    p.StartInfo <- processInfo

    let log (x: DataReceivedEventArgs) = printfn "%s" x.Data

    p.OutputDataReceived.Add log
    p.ErrorDataReceived.Add log

    p.Start() |> ignore
    p.BeginOutputReadLine()
    p.BeginErrorReadLine()

    let inputWriter = p.StandardInput
    input inputWriter

    p.WaitForExit()

    // Read the streams

    let exitCode = p.ExitCode
    p.Close()

    match exitCode with
    | 0 -> Success
    | err -> ErrorLevel err

let exec_bat path = exec_bat_in (Path.GetDirectoryName(path)) path
    

let whereCommand cmd =
    let path = sprintf "where %s" cmd
    let processInfo = new ProcessStartInfo("cmd.exe", "/c " + path)
    processInfo.CreateNoWindow <- true
    processInfo.UseShellExecute <- false
    // Redirect the output
    processInfo.RedirectStandardError <- true
    processInfo.RedirectStandardOutput <- true

    let p = new Process()
    p.StartInfo <- processInfo

    p.Start() |> ignore

    p.WaitForExit()

    let result =
        match p.ExitCode with
        | 0 -> Some (p.StandardOutput.ReadLine())
        | _ -> None

    p.Close()
    result

let convertToShortPathBat = lazy (
    let bat = Path.GetTempFileName () |> (fun p -> Path.ChangeExtension (p, ".bat"))
    File.AppendAllLines (bat, [| "@ECHO OFF"; """for /f "delims=" %%I in (%1) do echo %%~dfsI""" |] )
    bat
)

let convertToShortPath path =
    let processInfo = new ProcessStartInfo("cmd.exe", sprintf """/c ""%s" "%s"" """ convertToShortPathBat.Value path)
    processInfo.CreateNoWindow <- true
    processInfo.UseShellExecute <- false
    // Redirect the output
    processInfo.RedirectStandardError <- true
    processInfo.RedirectStandardOutput <- true

    let p = new Process()
    p.StartInfo <- processInfo

    p.Start() |> ignore

    p.WaitForExit()

    let result =
        match p.ExitCode with
        | 0 -> p.StandardOutput.ReadLine()
        | _ -> path

    p.Close()
    result
    
    
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

let peverify config assembly =
    Success

let private env key = 
    match Environment.GetEnvironmentVariable(key) with
    | null -> None
    | "" -> None
    | x -> Some x

//let private envOrDefault key def = match env key with Some x -> x | None -> def
//let private envOrFail key = match env key with Some x -> x | None -> failwithf "environment variable '%s' required " key
