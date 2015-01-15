module ``FSharp-Tests-Core``

open System
open System.IO
open NUnit.Framework

open TestConfig
open SingleTestBuild
open SingleTestRun
open SingleNegTest
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
        do! clix ("."/"testcs.exe") ""
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

    

module Forwarders = 

    let testData = [ (new TestCaseData()) |> setTestDataInfo "forwarders" ]

    [<Test; TestCaseSource("testData")>]
    let forwarders () = check (processor {
        let { Directory = dir; Config = cfg } = testConfig ()

        let exec path args =
            log "%s %s" path args
            Process.exec { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = None; } dir cfg.EnvironmentVariables path args
        let fsc args = Commands.fsc exec cfg.FSC args >> checkResult
        let peverify = Commands.peverify exec cfg.PEVERIFY >> checkResult
        let csc args = Commands.csc exec cfg.CSC args >> checkResult
        let copy_y = Commands.copy_y dir
        let mkdir = Commands.mkdir_p dir

        // mkdir orig
        mkdir "orig"
        // mkdir split
        mkdir "split"

        // %CSC% /nologo  /target:library /out:orig\a.dll /define:PART1;PART2 a.cs
        do! csc """/nologo  /target:library /out:orig\a.dll /define:PART1;PART2""" ["a.cs"]

        // %CSC% /nologo  /target:library /out:orig\b.dll /r:orig\a.dll b.cs 
        do! csc """/nologo  /target:library /out:orig\b.dll /r:orig\a.dll""" ["b.cs"]

        // "%FSC%" -a -o:orig\c.dll -r:orig\b.dll -r:orig\a.dll c.fs
        do! fsc """-a -o:orig\c.dll -r:orig\b.dll -r:orig\a.dll""" ["c.fs"]

        // %CSC% /nologo  /target:library /out:split\a-part1.dll /define:PART1;SPLIT a.cs  
        do! csc """/nologo  /target:library /out:split\a-part1.dll /define:PART1;SPLIT""" ["a.cs"]

        // %CSC% /nologo  /target:library /r:split\a-part1.dll /out:split\a.dll /define:PART2;SPLIT a.cs
        do! csc """/nologo  /target:library /r:split\a-part1.dll /out:split\a.dll /define:PART2;SPLIT""" ["a.cs"]

        // copy /y orig\b.dll split\b.dll
        copy_y ("orig"/"b.dll") ("split"/"b.dll")
        // copy /y orig\c.dll split\c.dll
        copy_y ("orig"/"c.dll") ("split"/"c.dll")

        // "%FSC%" -o:orig\test.exe -r:orig\b.dll -r:orig\a.dll test.fs
        do! fsc """-o:orig\test.exe -r:orig\b.dll -r:orig\a.dll""" ["test.fs"]

        // "%FSC%" -o:split\test.exe -r:split\b.dll -r:split\a-part1.dll -r:split\a.dll test.fs
        do! fsc """-o:split\test.exe -r:split\b.dll -r:split\a-part1.dll -r:split\a.dll""" ["test.fs"]

        // "%FSC%" -o:split\test-against-c.exe -r:split\c.dll -r:split\a-part1.dll -r:split\a.dll test.fs
        do! fsc """-o:split\test-against-c.exe -r:split\c.dll -r:split\a-part1.dll -r:split\a.dll""" ["test.fs"]

        // pushd split
        // "%PEVERIFY%" a-part1.dll
        do! peverify ("split"/"a-part1.dll")

        // REM "%PEVERIFY%" a.dll
        // REM   @if ERRORLEVEL 1 goto Error

        // "%PEVERIFY%" b.dll
        do! peverify ("split"/"b.dll")

        // "%PEVERIFY%" c.dll
        do! peverify ("split"/"c.dll")

        // "%PEVERIFY%" test.exe
        do! peverify ("split"/"test.exe")

        // "%PEVERIFY%" test-against-c.exe
        do! peverify ("split"/"test-against-c.exe")

        // popd

        })

module FsFromCs = 

    let testData = [ (new TestCaseData()) |> setTestDataInfo "fsfromcs" ]

    let build cfg dir = processor {
        let exec path args =
            log "%s %s" path args
            Process.exec { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = None; } dir cfg.EnvironmentVariables path args
        let fsc args = Commands.fsc exec cfg.FSC args >> checkResult
        let peverify = Commands.peverify exec cfg.PEVERIFY >> checkResult
        let csc args = Commands.csc exec cfg.CSC args >> checkResult
        let fsc_flags = cfg.fsc_flags

        // "%FSC%" %fsc_flags% -a --doc:lib.xml -o:lib.dll -g lib.ml
        do! fsc (sprintf "%s -a --doc:lib.xml -o:lib.dll -g" fsc_flags) ["lib.ml"]

        // "%PEVERIFY%" lib.dll
        do! peverify "lib.dll"

        // %CSC% /nologo /r:"%FSCOREDLLPATH%" /r:System.Core.dll /r:lib.dll /out:test.exe test.cs 
        do! csc (sprintf """/nologo /r:"%s" /r:System.Core.dll /r:lib.dll /out:test.exe""" cfg.FSCOREDLLPATH) ["test.cs"]

        // "%FSC%" %fsc_flags% -a --doc:lib--optimize.xml -o:lib--optimize.dll -g lib.ml
        do! fsc (sprintf """%s -a --doc:lib--optimize.xml -o:lib--optimize.dll -g""" fsc_flags) ["lib.ml"]

        // "%PEVERIFY%" lib--optimize.dll
        do! peverify "lib--optimize.dll"

        // %CSC% 
        do! csc (sprintf """/nologo /r:"%s"  /r:System.Core.dll /r:lib--optimize.dll    /out:test--optimize.exe""" cfg.FSCOREDLLPATH) ["test.cs"]
        
        }

    let run cfg dir = processor {
        let exec path args =
            log "%s %s" path args
            Process.exec { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = None; } dir cfg.EnvironmentVariables path args
        let clix exe = exec exe >> checkResult

        // %CLIX% .\test.exe
        do! clix ("."/"test.exe") ""

        // %CLIX% .\test--optimize.exe
        do! clix ("."/"test--optimize.exe") ""

        }


    [<Test; TestCaseSource("testData")>]
    let fsfromcs () = check (processor {
        let { Directory = dir; Config = cfg } = testConfig ()

        do! build cfg dir

        do! run cfg dir
                
        })

module QueriesCustomQueryOps = 

    let testData = [ (new TestCaseData()) |> setTestDataInfo "queriesCustomQueryOps" ]

    let build cfg dir = processor {
        let exec path args =
            log "%s %s" path args
            Process.exec { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = None; } dir cfg.EnvironmentVariables path args
        let fsc args = Commands.fsc exec cfg.FSC args >> checkResult
        let peverify = Commands.peverify exec cfg.PEVERIFY >> checkResult
        let csc args = Commands.csc exec cfg.CSC args >> checkResult
        let fsc_flags = cfg.fsc_flags

        // "%FSC%" %fsc_flags% -o:test.exe -g test.fsx
        do! fsc (sprintf """%s -o:test.exe -g""" fsc_flags) ["test.fsx"]

        // "%PEVERIFY%" test.exe 
        do! peverify "test.exe"

        // "%FSC%" %fsc_flags% --optimize -o:test--optimize.exe -g test.fsx
        do! fsc (sprintf """%s --optimize -o:test--optimize.exe -g""" fsc_flags) ["test.fsx"]

        // "%PEVERIFY%" test--optimize.exe 
        do! peverify "test--optimize.exe"

        // call ..\..\single-neg-test.bat negativetest
        do! singleNegTest cfg dir "negativetest"
        
        }

    let run cfg dir = processor {
        let exec path args =
            log "%s %s" path args
            Process.exec { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = None; } dir cfg.EnvironmentVariables path args
        let clix exe = exec exe >> checkResult
        let fsi args = Commands.fsi exec cfg.FSI args >> checkResult

        // echo TestC
        log "TestC"
        do! processor {
            // if exist test.ok (del /f /q test.ok)
            use testOkFile = FileGuard.create (dir/"test.ok")

            // "%FSI%" %fsi_flags% test.fsx
            do! fsi cfg.fsi_flags ["test.fsx"]

            // if NOT EXIST test.ok goto SetError
            do! testOkFile |> NUnitConf.checkGuardExists
        }

        // echo TestD
        log "TestD"
        do! processor {
            // if exist test.ok (del /f /q test.ok)
            use testOkFile = FileGuard.create (dir/"test.ok")

            // %CLIX% test.exe
            do! clix ("."/"test.exe") ""

            // if NOT EXIST test.ok goto SetError
            do! testOkFile |> NUnitConf.checkGuardExists
            }

        do! processor {
            // if exist test.ok (del /f /q test.ok)
            use testOkFile = FileGuard.create (dir/"test.ok")

            // %CLIX% test--optimize.exe
            do! clix ("."/"test--optimize.exe") ""

            // if NOT EXIST test.ok goto SetError
            do! testOkFile |> NUnitConf.checkGuardExists
            }

        }


    [<Test; TestCaseSource("testData")>]
    let queriesCustomQueryOps () = check (processor {
        let { Directory = dir; Config = cfg } = testConfig ()

        do! build cfg dir

        do! run cfg dir
                
        })
