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
        let fsiIn args = Commands.fsiIn (execIn dir cfg.EnvironmentVariables) cfg.FSI args >> checkResult

        // if exist test1.ok (del /f /q test1.ok)
        use testOkFile = FileGuard.create (dir/"test1.ok")

        do! fsiIn (sprintf "%s %s" cfg.fsi_flags flags) [dir/"test1.fsx"]

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
        let fsiIn args = Commands.fsiIn (execIn dir cfg.EnvironmentVariables) cfg.FSI args >> checkResult

        // if exist test2.ok (del /f /q test2.ok)
        use testOkFile = FileGuard.create (dir/"test2.ok")

        // "%FSI%" %fsi_flags%  /shadowcopyreferences+  < test2.fsx
        do! fsiIn (sprintf "%s %s" cfg.fsi_flags flags) [dir/"test2.fsx"]

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
        let copy_y f = Commands.copy_y dir f >> checkResult
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
        do! copy_y ("orig"/"b.dll") ("split"/"b.dll")
        // copy /y orig\c.dll split\c.dll
        do! copy_y ("orig"/"c.dll") ("split"/"c.dll")

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

module Printing = 

    let testData = 
        // "%FSI%" %fsc_flags_errors_ok%  --nologo --use:preludePrintSize200.fsx      <test.fsx >z.raw.output.test.200.txt     2>&1 
        // findstr /v "%CD%" z.raw.output.test.200.txt     | findstr /v -C:"--help' for options" > z.output.test.200.txt
        // if NOT EXIST z.output.test.200.bsl     COPY z.output.test.200.txt     z.output.test.200.bsl
        // %PRDIFF% z.output.test.200.txt     z.output.test.200.bsl     > z.output.test.200.diff
        [ "", "z.output.test.default.txt", "z.output.test.default.bsl" ;
          "--use:preludePrintSize1000.fsx", "z.output.test.1000.txt", "z.output.test.1000.bsl" ;
          "--use:preludePrintSize200.fsx", "z.output.test.200.txt", "z.output.test.200.bsl" ;
          "--use:preludeShowDeclarationValuesFalse.fsx", "z.output.test.off.txt", "z.output.test.off.bsl" ;
          "--quiet", "z.output.test.quiet.txt", "z.output.test.quiet.bsl" ]
        |> List.map (fun (flag,diff,expected) -> (new TestCaseData(flag, diff, expected)) |> setTestDataInfo "printing" )

    [<Test; TestCaseSource("testData")>]
    let printing flag diffFile expectedFile = check (processor {
        let { Directory = dir; Config = cfg } = testConfig ()

        let exec path args =
            log "%s %s" path args
            Process.exec { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = None; } dir cfg.EnvironmentVariables path args
        let fsc args = Commands.fsc exec cfg.FSC args >> checkResult
        let peverify = Commands.peverify exec cfg.PEVERIFY >> checkResult
        let csc args = Commands.csc exec cfg.CSC args >> checkResult
        let copy from' = Commands.copy_y dir from' >> checkResult

        let redirectToFile path =
            File.WriteAllText(path, "")
            MailboxProcessor.Start(fun inbox -> 
                let rec loop () = async { 
                    let! msg = inbox.Receive ()
                    File.AppendAllLines (path, [| msg |])
                    return! loop () }
                loop ())

        let fsiInAndRedirectOutErr flags inputFile outErrRedirectedTo =
            use outFile = redirectToFile outErrRedirectedTo
            let exec' input path args =
                log "%s %s <%s >%s 2>&1" path args inputFile outErrRedirectedTo
                let appendToFile l = outFile.Post (l)
                Process.exec { RedirectOutput = Some appendToFile; RedirectError = Some appendToFile; RedirectInput = Some input; } dir cfg.EnvironmentVariables path args
            // "%FSI%" %fsc_flags_errors_ok%  --nologo                                    <test.fsx >z.raw.output.test.default.txt 2>&1
            Commands.fsiIn exec' cfg.FSI flags [ inputFile ] |> checkResult
        
        // rem recall  >fred.txt 2>&1 merges stderr into the stdout redirect
        // rem however 2>&1  >fred.txt did not seem to do it.

        // REM Here we use diff.exe without -dew option to trap whitespace changes, like bug 4429.
        // REM Any whitespace change needs to be investigated, these tests are to check exact output.
        // REM Base line updates are easy: sd edit and delete the .bsl and rerun the test.
        // set PRDIFF=%~d0%~p0..\..\..\fsharpqa\testenv\bin\%processor_architecture%\diff.exe
        // echo Diff tool is %PRDIFF%
        // if NOT EXIST %PRDIFF% (
        //     echo ERROR: Diff tool not found at %PRDIFF%
        //     exit /b 1
        // )
        let prdiff a = Commands.fsdiff exec cfg.FSDIFF false a >> checkResult

        let fsc_flags_errors_ok = ""

        // echo == Plain
        // "%FSI%" %fsc_flags_errors_ok%  --nologo                                    <test.fsx >z.raw.output.test.default.txt 2>&1
        // echo == PrintSize 1000
        // "%FSI%" %fsc_flags_errors_ok%  --nologo --use:preludePrintSize1000.fsx     <test.fsx >z.raw.output.test.1000.txt    2>&1 
        // echo == PrintSize 200
        // "%FSI%" %fsc_flags_errors_ok%  --nologo --use:preludePrintSize200.fsx      <test.fsx >z.raw.output.test.200.txt     2>&1 
        // echo == ShowDeclarationValues off
        // "%FSI%" %fsc_flags_errors_ok%  --nologo --use:preludeShowDeclarationValuesFalse.fsx <test.fsx >z.raw.output.test.off.txt     2>&1
        // echo == Quiet
        // "%FSI%" %fsc_flags_errors_ok% --nologo --quiet                              <test.fsx >z.raw.output.test.quiet.txt   2>&1
        let rawFile = Path.GetTempFileName()
        do! fsiInAndRedirectOutErr (sprintf "%s --nologo %s" fsc_flags_errors_ok flag) (dir/"test.fsx") rawFile

        // REM REVIEW: want to normalise CWD paths, not suppress them.
        let ``findstr /v`` text = Seq.filter (fun (s: string) -> not <| s.Contains(text))
        let removeCDandHelp from' to' =
            File.ReadLines from' |> (``findstr /v`` dir) |> (``findstr /v`` "--help' for options") |> (fun lines -> File.WriteAllLines(dir/to', lines))

        // findstr /v "%CD%" z.raw.output.test.default.txt | findstr /v -C:"--help' for options" > z.output.test.default.txt
        // findstr /v "%CD%" z.raw.output.test.1000.txt    | findstr /v -C:"--help' for options" > z.output.test.1000.txt
        // findstr /v "%CD%" z.raw.output.test.200.txt     | findstr /v -C:"--help' for options" > z.output.test.200.txt
        // findstr /v "%CD%" z.raw.output.test.off.txt     | findstr /v -C:"--help' for options" > z.output.test.off.txt
        // findstr /v "%CD%" z.raw.output.test.quiet.txt   | findstr /v -C:"--help' for options" > z.output.test.quiet.txt
        removeCDandHelp rawFile diffFile

        let withDefault default' to' =
            fileExists (dir/to') |> function None -> Some (copy default' to') | Some _ -> None
        // if NOT EXIST z.output.test.default.bsl COPY z.output.test.default.txt z.output.test.default.bsl
        // if NOT EXIST z.output.test.off.bsl     COPY z.output.test.off.txt     z.output.test.off.bsl
        // if NOT EXIST z.output.test.1000.bsl    COPY z.output.test.1000.txt    z.output.test.1000.bsl
        // if NOT EXIST z.output.test.200.bsl     COPY z.output.test.200.txt     z.output.test.200.bsl
        // if NOT EXIST z.output.test.quiet.bsl   COPY z.output.test.quiet.txt   z.output.test.quiet.bsl
        do! expectedFile |> withDefault diffFile

        // %PRDIFF% z.output.test.default.txt z.output.test.default.bsl > z.output.test.default.diff
        // %PRDIFF% z.output.test.off.txt     z.output.test.off.bsl     > z.output.test.off.diff
        // %PRDIFF% z.output.test.1000.txt    z.output.test.1000.bsl    > z.output.test.1000.diff
        // %PRDIFF% z.output.test.200.txt     z.output.test.200.bsl     > z.output.test.200.diff
        // %PRDIFF% z.output.test.quiet.txt   z.output.test.quiet.bsl   > z.output.test.quiet.diff
        do! prdiff diffFile expectedFile

        // echo ======== Differences From ========
        // TYPE  z.output.test.default.diff
        // TYPE  z.output.test.off.diff
        // TYPE  z.output.test.1000.diff
        // TYPE  z.output.test.200.diff
        // TYPE  z.output.test.quiet.diff
        // echo ========= Differences To =========
        // 
        // TYPE  z.output.test.default.diff  > zz.alldiffs
        // TYPE  z.output.test.off.diff     >> zz.alldiffs
        // TYPE  z.output.test.1000.diff    >> zz.alldiffs
        // TYPE  z.output.test.200.diff     >> zz.alldiffs
        // TYPE  z.output.test.quiet.diff   >> zz.alldiffs
        // 
        // for /f %%c IN (zz.alldiffs) do (
        //   echo NOTE -------------------------------------
        //   echo NOTE ---------- THERE ARE DIFFs ----------
        //   echo NOTE -------------------------------------
        //   echo .
        //   echo To update baselines: "sd edit *bsl", "del *bsl", "build.bat" regenerates bsl, "sd diff ...", check what changed.
        //   goto Error
        // )
        ignore "printed to log"


        })
