module ``FSharp-Tests-Core``

open System
open System.IO
open NUnit.Framework
open All
open TestConfig
open SingleTestBuild
open SingleTestRun
open NUnitConf

let test phases (p: Permutation) =
    let dir = NUnit.Framework.TestContext.CurrentContext.Test.Properties.["DIRECTORY"] :?> string
    let cfg = suiteHelpers.Value
    phases |> List.iter (fun phase -> phase cfg dir p)

let allPermutations = NUnitConf.allPermutation

let createTestCaseData name = NUnitConf.createTestCaseData ("core", name)

module Access =
    let permutations = allPermutations |> createTestCaseData "access"

    [<Test; TestCaseSource("permutations")>]
    let access p = 
        p |> test [singleTestBuild; singleTestRun]

module Apporder = 
    let permutations = allPermutations |> createTestCaseData "apporder"

    [<Test; TestCaseSource("permutations")>]
    let apporder p = 
        p |> test [singleTestBuild; singleTestRun]

module Attributes = 
    let permutations = allPermutations |> createTestCaseData "attributes"

    [<Test; TestCaseSource("permutations")>]
    let attributes p = 
        p |> test [singleTestBuild; singleTestRun]

module Comprehensions = 
    let permutations = allPermutations |> createTestCaseData "comprehensions"

    [<Test; TestCaseSource("permutations")>]
    let comprehensions p = 
        p |> test [singleTestBuild; singleTestRun]

module ControlWpf = 
    let permutations = allPermutations |> createTestCaseData "controlwpf"

    [<Test; TestCaseSource("permutations")>]
    let controlWpf p = 
        p |> test [singleTestBuild; singleTestRun]

module Events = 
    let permutations = allPermutations |> createTestCaseData "events"

    let failIfError = function
    | OK -> ()
    | Error (err, msg) -> Assert.Fail (sprintf "ERRORLEVEL %i %s" err msg)
    | Skipped msg -> ()

    open PlatformHelpers

    let build cfg dir p =
        let loglines = printfn "%s"
        let exec path args =
            printfn "%s %s" path args
            exec' { RedirectOutput = Some loglines; RedirectError = Some loglines; RedirectInput = None; } dir cfg.EnvironmentVariables path args
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
        let exec path args = 
            printfn "%s %s" path args
            exec' { RedirectOutput = Some loglines; RedirectError = Some loglines; RedirectInput = None; } dir cfg.EnvironmentVariables path args
        let clix = exec
        let fsi = Commands.fsi exec cfg.FSI

        // %CLIX% "%FSI%" test.fs && (
        match withFileGuard (dir/"test.ok") (fun () -> fsi "" ["test.fs"]) with
        | Error x -> Error x
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSI failed;
        // goto Error
        // set ERRORMSG=%ERRORMSG% FSI failed;
        // )
        | Ok ->
            // %CLIX% .\testcs.exe
            match clix (dir/"testcs.exe") "" with
            | ErrorLevel err -> Error (err, "testcs.exe")
            // if ERRORLEVEL 1 goto Error
            | Ok -> OK

    [<Test; TestCaseSource("permutations")>]
    let events p =
        let checkFailure f c d = (f c d) >> failIfError
        p |> test [build |> checkFailure; run |> checkFailure]

