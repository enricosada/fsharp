module ``FSharp-Tests-Core``

open System
open System.IO
open NUnit.Framework
open All
open TestConfig
open SingleTestBuild
open SingleTestRun
open NUnitConf

let testDir subDir = Path.Combine(__SOURCE_DIRECTORY__, subDir)

let getTagsOfFile path =
    match File.ReadLines(path) |> Seq.tryFind (fun _ -> true) with
    | None -> []
    | Some line -> 
        line.TrimStart([|'/'|]).Split([| '#' |], StringSplitOptions.RemoveEmptyEntries)
        |> Seq.map (fun s -> s.Trim())
        |> Seq.filter (fun s -> s.Length > 0)
        |> Seq.distinct
        |> Seq.toList

let getProperties subDir =
    Directory.EnumerateFiles(testDir subDir, "*.fs*")
    |> Seq.toList
    |> List.collect getTagsOfFile

let test dirName phases (p: Permutation) =
    let dir = (testDir dirName)
    phases |> List.iter (fun phase -> phase cfg dir p)

let allPermutations = NUnitConf.allPermutation

module Access =
    let permutations = allPermutations |> createTestCaseData (["core";"access"] @ (getProperties "access")) []

    [<Test; TestCaseSource("permutations")>]
    let access p = 
        p |> test "access" [singleTestBuild; singleTestRun]

module Apporder = 
    let permutations = allPermutations |> createTestCaseData (["core";"apporder"] @ (getProperties "apporder")) []

    [<Test; TestCaseSource("permutations")>]
    let apporder p = 
        p |> test "apporder" [singleTestBuild; singleTestRun]

module Attributes = 
    let permutations = allPermutations |> createTestCaseData  (["core";"attributes"] @ (getProperties "attributes")) []

    [<Test; TestCaseSource("permutations")>]
    let attributes p = 
        p |> test "attributes" [singleTestBuild; singleTestRun]

module Comprehensions = 
    let permutations = allPermutations |> createTestCaseData  (["core";"comprenhensions"] @ (getProperties "comprehensions")) []

    [<Test; TestCaseSource("permutations")>]
    let comprehensions p = 
        p |> test "comprehensions" [singleTestBuild; singleTestRun]

module ControlWpf = 
    let permutations = allPermutations |> createTestCaseData  (["core";"controlwpf"] @ (getProperties "controlwpf")) []

    [<Test; TestCaseSource("permutations")>]
    let controlWpf p = 
        p |> test "controlWpf" [singleTestBuild; singleTestRun]

module Events = 
    let permutations = allPermutations |> createTestCaseData  (["core";"events"] @ (getProperties "events")) []

    let failIfError = function
    | OK -> ()
    | Error (err, msg) -> Assert.Fail (sprintf "ERRORLEVEL %i %s" err msg)
    | Skipped msg -> ()

    open PlatformHelpers

    let build cfg dir p =
        let loglines = printfn "%s" 
        let exec = exec' { RedirectOutput = Some loglines; RedirectError = Some loglines; RedirectInput = None; } dir []
        let fsc = Commands.fsc exec cfg.FSC
        let peverify = Commands.peverify exec cfg.PEVERIFY
        let csc = Commands.csc exec cfg.CSC
        let fsc_flags = cfg.fsc_flags
        let FSharpCoreDllPath = cfg.FSCOREDLLPATH

        // "%FSC%" %fsc_flags% -a -o:test.dll -g test.fs
        match fsc (sprintf "%s -a -o:test.dll -g" fsc_flags) ["test.fs"] with
        | ErrorLevel err -> Error (err, "fsc failed")
            // @if ERRORLEVEL 1 goto Error
        | Ok ->
            // "%PEVERIFY%" test.dll
            match peverify "test.dll" with
            | ErrorLevel err -> Error (err, "peverify failed")
                // @if ERRORLEVEL 1 goto Error
            | Ok ->
                // %CSC% /r:"%FSCOREDLLPATH%" /reference:test.dll /debug+ testcs.cs
                match csc (sprintf """/r:"%s" /reference:test.dll /debug+""" FSharpCoreDllPath) ["testcs.cs"] with
                | ErrorLevel err -> Error (err, "csc failed")
                    // @if ERRORLEVEL 1 goto Error
                | Ok ->
                    // "%PEVERIFY%" testcs.exe
                    match peverify "testcs.exe" with
                    | ErrorLevel err -> Error (err, "peverify failed")
                    // @if ERRORLEVEL 1 goto Error
                    | Ok -> OK

    let run cfg dir p =
        let loglines = printfn "%s" 
        let exec = exec' { RedirectOutput = Some loglines; RedirectError = Some loglines; RedirectInput = None; } dir []
        let clix = exec
        let fsi = Commands.fsi exec cfg.FSI

        // %CLIX% "%FSI%" test.fs && (
        match withFileGuard (dir/"test.ok") (fun () -> fsi "" ["test.fs"])  with
        | Error x -> Error x
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSI failed;
        // goto Error
        // set ERRORMSG=%ERRORMSG% FSI failed;
        // )
        | Ok ->
            // %CLIX% .\testcs.exe
            match clix "."/"testcs.exe" "" with
            | ErrorLevel err -> Error (err, "testcs.exe")
            // if ERRORLEVEL 1 goto Error
            | Ok -> OK

    [<Test; TestCaseSource("permutations")>]
    let events (p: Permutation) = 
        let checkFailure f c d = (f c d) >> failIfError
        p |> test "events" [build |> checkFailure; run |> checkFailure]

