module ``FSharp-Tests-Core``

open System
open System.IO
open NUnit.Framework

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

    open PlatformHelpers

    let build cfg dir p = processor {
        let exec path args =
            log "%s %s" path args
            exec' { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = None; } dir cfg.EnvironmentVariables path args
        let fsc args = Commands.fsc exec cfg.FSC args >> checkResult
        let peverify = Commands.peverify exec cfg.PEVERIFY >> checkResult
        let csc args = Commands.csc exec cfg.CSC args >> checkResult

        // "%FSC%" %fsc_flags% -a -o:test.dll -g test.fs
        do! fsc (sprintf "%s -a -o:test.dll -g" cfg.fsc_flags) ["test.fs"]

        // "%PEVERIFY%" test.dll
        do! peverify "test.dll"

        // %CSC% /r:"%FSCOREDLLPATH%" /reference:test.dll /debug+ testcs.cs
        do! csc (sprintf """/r:"%s" /reference:test.dll /debug+""" cfg.FSCOREDLLPATH) ["testcs.cs"]

        // "%PEVERIFY%" testcs.exe
        do! peverify "testcs.exe"
        }

    let run cfg dir p = processor {
        let exec path args = 
            log "%s %s" path args
            exec' { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = None; } dir cfg.EnvironmentVariables path args
        let clix exe = exec exe >> checkResult
        let fsi args = Commands.fsi exec cfg.FSI args >> checkResult

        use testOkFile = FileGuard.create (dir/"test.ok")

        // %CLIX% "%FSI%" test.fs && (
        do! fsi "" ["test.fs"]

        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSI failed;
        // goto Error
        // set ERRORMSG=%ERRORMSG% FSI failed;
        // )
        do! testOkFile |> NUnitConf.checkGuardExists

        // %CLIX% .\testcs.exe
        do! clix (dir/"testcs.exe") ""
        }

    [<Test; TestCaseSource("permutations")>]
    let events p =
        let checkFailure f c p = (f c p) >> Attempt.Run >> (fun x -> checkTestResult x)
        p |> test [build |> checkFailure; run |> checkFailure]

