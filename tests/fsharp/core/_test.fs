module ``FSharp-Tests-Core``

open System
open System.IO
open NUnit.Framework

open TestConfig
open SingleTestBuild
open SingleTestRun
open NUnitConf
open PlatformHelpers

let setTestDataInfo name = FSharpTestSuite.setTestDataInfo ("core", name)

type TestRunConfig = { Directory: string; Config: TestConfig }

let testConfig () =
    { Directory = NUnit.Framework.TestContext.CurrentContext.Test.Properties.["DIRECTORY"] :?> string;
      Config = suiteHelpers.Value }

let check (f: Attempt<_,_>) =
    f |> Attempt.Run |> checkTestResult

let test phases (p: Permutation) =
    let { Directory = dir; Config = cfg } = testConfig ()
    phases |> List.iter (fun phase -> phase cfg dir p)

module Access =
    let permutations =
        FSharpTestSuite.allPermutation
        |> List.map (fun p -> (new TestCaseData (p)).SetCategory(sprintf "%A" p) |> setTestDataInfo "access")

    [<Test; TestCaseSource("permutations")>]
    let access p = check (processor {
        let { Directory = dir; Config = cfg } = testConfig ()
        
        do! singleTestBuild cfg dir p
        
        do! singleTestRun cfg dir p
        })

module Apporder = 
    let permutations =
        FSharpTestSuite.allPermutation
        |> List.map (fun p -> (new TestCaseData (p)).SetCategory(sprintf "%A" p) |> setTestDataInfo "apporder")

    [<Test; TestCaseSource("permutations")>]
    let apporder p = check  (processor {
        let { Directory = dir; Config = cfg } = testConfig ()
        
        do! singleTestBuild cfg dir p
        
        do! singleTestRun cfg dir p
        })

module Attributes = 
    let permutations =
        FSharpTestSuite.allPermutation
        |> List.map (fun p -> (new TestCaseData (p)).SetCategory(sprintf "%A" p) |> setTestDataInfo "attributes")

    [<Test; TestCaseSource("permutations")>]
    let attributes p = check  (processor {
        let { Directory = dir; Config = cfg } = testConfig ()
        
        do! singleTestBuild cfg dir p
        
        do! singleTestRun cfg dir p
        }) 

module Comprehensions = 
    let permutations = 
        FSharpTestSuite.allPermutation
        |> List.map (fun p -> (new TestCaseData (p)).SetCategory(sprintf "%A" p) |> setTestDataInfo "comprehensions")

    [<Test; TestCaseSource("permutations")>]
    let comprehensions p = check  (processor {
        let { Directory = dir; Config = cfg } = testConfig ()
        
        do! singleTestBuild cfg dir p
        
        do! singleTestRun cfg dir p
        })

module ControlWpf = 
    let permutations = 
        FSharpTestSuite.allPermutation
        |> List.map (fun p -> (new TestCaseData (p)).SetCategory(sprintf "%A" p) |> setTestDataInfo "controlwpf")

    [<Test; TestCaseSource("permutations")>]
    let controlWpf p = check  (processor {
        let { Directory = dir; Config = cfg } = testConfig ()
        
        do! singleTestBuild cfg dir p
        
        do! singleTestRun cfg dir p
        })

module Events = 

    open PlatformHelpers

    let build cfg dir = processor {
        let exec path args =
            log "%s %s" path args
            Process.exec { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = None; } dir cfg.EnvironmentVariables path args
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

    let run cfg dir = processor {
        let exec path args = 
            log "%s %s" path args
            Process.exec { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = None; } dir cfg.EnvironmentVariables path args
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

    let testData =
        [ (new TestCaseData ()) |> setTestDataInfo "events" ]

    [<Test; TestCaseSource("testData")>]
    let events () = check  (processor {
        let { Directory = dir; Config = cfg } = testConfig ()

        do! build cfg dir
        
        do! run cfg dir
        })


module ``FSI-Shadowcopy`` = 

    open PlatformHelpers

    let execIn dir envVars input = 
        Process.exec { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = Some input; } dir envVars

    let test1Data = 
        // "%FSI%" %fsi_flags%                          < test1.fsx
        // "%FSI%" %fsi_flags%  --shadowcopyreferences- < test1.fsx
        [""; "--shadowcopyreferences-"] 
        |> List.map (fun flags -> (new TestCaseData(flags)) |> setTestDataInfo "fsi-shadowcopy")

    [<Test; TestCaseSource("test1Data")>]
    let ``shadowcopy disabled`` (flags: string) = check  (processor {
        let { Directory = dir; Config = cfg } = testConfig ()
        let fsi args = Commands.fsiIn (execIn dir cfg.EnvironmentVariables) cfg.FSI args >> checkResult

        // if exist test1.ok (del /f /q test1.ok)
        use testOkFile = FileGuard.create (dir/"test1.ok")

        do! fsi (sprintf "%s %s" cfg.fsi_flags flags) [dir/"test1.fsx"]

        // if NOT EXIST test1.ok goto SetError
        do! testOkFile |> NUnitConf.checkGuardExists
        })

    let test2Data = 
        // "%FSI%" %fsi_flags%  /shadowcopyreferences+  < test2.fsx
        // "%FSI%" %fsi_flags%  --shadowcopyreferences  < test2.fsx
        ["/shadowcopyreferences+"; "--shadowcopyreferences"] 
        |> List.map (fun flags -> (new TestCaseData(flags)) |> setTestDataInfo "fsi-shadowcopy")

    [<Test; TestCaseSource("test2Data")>]
    let ``shadowcopy enabled`` (flags: string) = check (processor {
        let { Directory = dir; Config = cfg } = testConfig ()
        let fsi args = Commands.fsiIn (execIn dir cfg.EnvironmentVariables) cfg.FSI args >> checkResult

        // if exist test2.ok (del /f /q test2.ok)
        use testOkFile = FileGuard.create (dir/"test2.ok")

        // "%FSI%" %fsi_flags%  /shadowcopyreferences+  < test2.fsx
        do! fsi (sprintf "%s %s" cfg.fsi_flags flags) [dir/"test2.fsx"]

        // if NOT EXIST test2.ok goto SetError
        do! testOkFile |> NUnitConf.checkGuardExists
        })

    
