module ``FSharp-Tests-Core``

open System
open System.IO
open NUnit.Framework
open All
open TestConfig
open SingleTestBuild
open SingleTestRun

type Permutations () =
    member x.all () = All.allPermutation |> createFSharpTestPermu

let testDir subDir = Path.Combine(__SOURCE_DIRECTORY__, subDir)

let test dirName phases (p: Permutation) =
    let cfg, dir = getConfig.Value, (testDir dirName)
    logConfig cfg
    phases |> List.iter (fun phase -> phase cfg dir p)

module Access =

    [<Test; TestCaseSource(typeof<Permutations>, "all")>]
    let access p = 
        p |> test "access" [singleTestBuild; singleTestRun]

module Apporder = 

    [<Test; TestCaseSource(typeof<Permutations>, "all")>]
    let apporder p = 
        p |> test "apporder" [singleTestBuild; singleTestRun]

module Attributes = 

    [<Test; TestCaseSource(typeof<Permutations>, "all")>]
    let attributes p = 
        p |> test "attributes" [singleTestBuild; singleTestRun]

module Comprehensions = 

    [<Test; TestCaseSource(typeof<Permutations>, "all")>]
    let comprehensions p = 
        p |> test "comprehensions" [singleTestBuild; singleTestRun]

module ControlWpf = 

    [<Test; TestCaseSource(typeof<Permutations>, "all")>]
    let controlWpf p = 
        p |> test "controlWpf" [singleTestBuild; singleTestRun]

module Events = 
    let build cfg dir p =
        // "%FSC%" %fsc_flags% -a -o:test.dll -g test.fs
        // @if ERRORLEVEL 1 goto Error

        // "%PEVERIFY%" test.dll
        // @if ERRORLEVEL 1 goto Error

        // %CSC% /r:"%FSCOREDLLPATH%" /reference:test.dll /debug+ testcs.cs
        // @if ERRORLEVEL 1 goto Error

        // "%PEVERIFY%" testcs.exe
        // @if ERRORLEVEL 1 goto Error
        Assert.Fail("todo: implement")        

    let run cfg dir p =
        // %CLIX% "%FSI%" test.fs && (
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSI failed;
        // goto Error
        // set ERRORMSG=%ERRORMSG% FSI failed;
        // )

        // %CLIX% .\testcs.exe
        // if ERRORLEVEL 1 goto Error
        Assert.Fail("todo: implement")        

    [<Test; TestCaseSource(typeof<Permutations>, "all")>]
    let events p = 
        p |> test "events" [build; run]

