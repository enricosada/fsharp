﻿module SingleTestBuild

open System
open System.IO
open System.Diagnostics

open NUnit.Framework
open TestConfig
open PlatformHelpers

let singleTestBuild cfg testDir =
    //@if "%_echo%"=="" echo off
    //setlocal
    ignore "useless"

    //if EXIST build.ok DEL /f /q build.ok
    let buildOkPath = testDir / "build.ok"
    buildOkPath |> fileExists |> Option.iter File.Delete //TODO "/f" -> forza rimozione readonly, "/q" -> no interactive

    //call %~d0%~p0..\config.bat
    ignore "param"

    //if NOT "%FSC:NOTAVAIL=X%" == "%FSC%" (
    //  goto Skip
    //)
    ignore "alredy checked fsc/fsi exists"

    //set source1=
    //if exist test.ml (set source1=test.ml)
    //if exist test.fs (set source1=test.fs)
    let source1 = 
        ["test.ml"; "test.fs"] 
        |> List.rev
        |> List.tryFind (fun name -> (testDir/name) |> fileExists |> Option.isSome)

    //set sources=
    //if exist testlib.fsi (set sources=%sources% testlib.fsi)
    //if exist testlib.fs (set sources=%sources% testlib.fs)
    //if exist test.mli (set sources=%sources% test.mli)
    //if exist test.ml (set sources=%sources% test.ml)
    //if exist test.fsi (set sources=%sources% test.fsi)
    //if exist test.fs (set sources=%sources% test.fs)
    //if exist test2.mli (set sources=%sources% test2.mli)
    //if exist test2.ml (set sources=%sources% test2.ml)
    //if exist test2.fsi (set sources=%sources% test2.fsi)
    //if exist test2.fs (set sources=%sources% test2.fs)
    //if exist test.fsx (set sources=%sources% test.fsx)
    //if exist test2.fsx (set sources=%sources% test2.fsx)
    let sources =
        ["testlib.fsi";"testlib.fs";"test.mli";"test.ml";"test.fsi";"test.fs";"test2.mli";"test2.ml";"test2.fsi";"test2.fs";"test.fsx";"test2.fsx"]
        |> List.filter (fun name -> (testDir/name) |> fileExists |> Option.isSome)

    //set sourceshw=
    //if exist test-hw.mli (set sourceshw=%sourceshw% test-hw.mli)
    //if exist test-hw.ml (set sourceshw=%sourceshw% test-hw.ml)
    //if exist test-hw.fsx (set sourceshw=%sourceshw% test-hw.fsx)
    //if exist test2-hw.mli (set sourceshw=%sourceshw% test2-hw.mli)
    //if exist test2-hw.ml (set sourceshw=%sourceshw% test2-hw.ml)
    //if exist test2-hw.fsx (set sourceshw=%sourceshw% test2-hw.fsx)
    let sourceshw =
        ["test-hw.mli";"test-hw.ml";"test-hw.fsx";"test2-hw.mli";"test2-hw.ml";"test2-hw.fsx"]
        |> List.filter (fun name -> (testDir/name) |> fileExists |> Option.isSome)

    //rem to run the 64 bit version of the code set FSC_BASIC_64=FSC_BASIC_64
    //set PERMUTATIONS_LIST=FSI_FILE FSI_STDIN FSI_STDIN_OPT FSI_STDIN_GUI FSC_BASIC %FSC_BASIC_64% FSC_HW FSC_O3 GENERATED_SIGNATURE EMPTY_SIGNATURE EMPTY_SIGNATURE_OPT FSC_OPT_MINUS_DEBUG FSC_OPT_PLUS_DEBUG FRENCH SPANISH AS_DLL WRAPPER_NAMESPACE WRAPPER_NAMESPACE_OPT

    //if "%REDUCED_RUNTIME%"=="1" (
    //    echo REDUCED_RUNTIME set
    //    
    //    if not defined PERMUTATIONS (
    //        powershell.exe %PSH_FLAGS% -command "&{& '%~d0%~p0\PickPermutations.ps1' '%cd%' '%FSC%' '%PERMUTATIONS_LIST%'}" > _perm.txt
    //        if errorlevel 1 (
    //            set ERRORMSG=%ERRORMSG% PickPermutations.ps1 failed;
    //            goto :ERROR
    //        )
    //        set /p PERMUTATIONS=<_perm.txt
    //    )
    //    
    //    powershell.exe %PSH_FLAGS% -command "&{& '%~d0%~p0\DecidePEVerify.ps1' '%cd%' '%FSC%'}"
    //    if errorlevel 1 (
    //        set ERRORMSG=%ERRORMSG% DecidePEVerify.ps1 failed;
    //        goto :ERROR
    //    )
    //)

    //if not defined PERMUTATIONS (
    //    echo "PERMUTATIONS not defined. Building everything."
    //    set PERMUTATIONS=%PERMUTATIONS_LIST%
    //)

    //for %%A in (%PERMUTATIONS%) do (
    //    call :%%A
    //    IF ERRORLEVEL 1 EXIT /B 1
    //)
    ignore "permutations useless because build type is an input"

    let exec exe args =
        log "%s %s" exe args
        use toLog = redirectToLog ()
        Process.exec { RedirectOutput = Some toLog.Post; RedirectError = Some toLog.Post; RedirectInput = None; } testDir cfg.EnvironmentVariables exe args

    let echo_tofile = Commands.echo_tofile testDir
    let copy_y f = Commands.copy_y testDir f >> checkResult
    let type_append_tofile = Commands.type_append_tofile testDir
    let fsc flagsFormat = Printf.ksprintf (fun flags -> Commands.fsc exec cfg.FSC flags >> checkResult) flagsFormat
    let fsc_flags = cfg.fsc_flags
    let peverify = Commands.peverify exec cfg.PEVERIFY >> checkResult
    let ``echo._tofile`` = Commands.``echo._tofile`` testDir

    //:Ok
    let doneOk x =
        //echo Built fsharp %~f0 ok.
        log "Built fsharp %s ok." testDir
        //echo. > build.ok
        ``echo._tofile`` " " "build.ok"
        //endlocal
        //exit /b 0
        Success x

    //:Skip
    let doneSkipped x =
        //echo Skipped %~f0
        log "Skipped %s" testDir
        //endlocal
        //exit /b 0
        Success x

    //:Error
    let doneError err msg =
        //echo Test Script Failed (perhaps test did not emit test.ok signal file?)
        log "%s" msg
        //endlocal
        //exit /b %ERRORLEVEL%
        Failure (err)

    let genericErrorMessage = "Test Script Failed (perhaps test did not emit test.ok signal file?)"

    //:SETERROR
    //set NonexistentErrorLevel 2> nul
    //goto Error

    
    /// <summary>
    /// if NOT EXIST dont.run.peverify (    <para/>
    ///    "%PEVERIFY%" test.exe            <para/>
    ///    @if ERRORLEVEL 1 goto Error      <para/>
    /// )                                   <para/>
    /// </summary>
    let doPeverify cmd = processor {
        if testDir/"dont.run.peverify" |> fileExists |> Option.isNone
        then do! peverify cmd
        }

    let doNOOP () = processor {
        //@echo No build action to take for this permutation
        log "No build action to take for this permutation"
        }

    let doBasic () = processor { 
        // FSC %fsc_flags% --define:BASIC_TEST -o:test.exe -g %sources%
        //if ERRORLEVEL 1 goto Error
        do! fsc "%s --define:BASIC_TEST -o:test.exe -g" fsc_flags sources 

        //if NOT EXIST dont.run.peverify (
        //    "%PEVERIFY%" test.exe
        //    @if ERRORLEVEL 1 goto Error
        //)
        do! doPeverify "test.exe"
        }

    let doBasic64 () = processor {
        // "%FSC%" %fsc_flags% --define:BASIC_TEST --platform:x64 -o:testX64.exe -g %sources%
        // if ERRORLEVEL 1 goto Error
        do! fsc "%s --define:BASIC_TEST --platform:x64 -o:testX64.exe -g" fsc_flags sources

        // if NOT EXIST dont.run.peverify (
        //     "%PEVERIFY%" testX64.exe
        //     @if ERRORLEVEL 1 goto Error
        // )
        do! doPeverify "testX64.exe"
        }

    let doFscHW () = processor {
        // if exist test-hw.* (
        if Directory.EnumerateFiles(testDir, "test-hw.*") |> Seq.tryPick fileExists |> Option.isSome then
            // "%FSC%" %fsc_flags% -o:test-hw.exe -g %sourceshw%
            // if ERRORLEVEL 1 goto Error
            do! fsc "%s -o:test-hw.exe -g" fsc_flags sourceshw

            // if NOT EXIST dont.run.peverify (
            //   "%PEVERIFY%" test-hw.exe
            //   @if ERRORLEVEL 1 goto Error
            // )
            do! doPeverify "test-hw.exe" 
        //)
        }

    let doFscO3 () = processor {
        //"%FSC%" %fsc_flags% --optimize --define:PERF -o:test--optimize.exe -g %sources%
        //if ERRORLEVEL 1 goto Error
        do! fsc "%s --optimize --define:PERF -o:test--optimize.exe -g" fsc_flags sources 
        //if NOT EXIST dont.run.peverify (
        //    "%PEVERIFY%" test--optimize.exe
        //    @if ERRORLEVEL 1 goto Error
        //)
        do! doPeverify "test--optimize.exe"
        }

    let doGeneratedSignature () = processor {
        //if NOT EXIST dont.use.generated.signature (
        if testDir/"dont.use.generated.signature" |> fileExists |> Option.isNone then
            // if exist test.ml (
            if testDir/"test.ml" |> fileExists |> Option.isSome then
                //  echo Generating interface file...
                log "Generating interface file..."
                //  copy /y %source1% tmptest.ml
                do! source1 |> Option.map (fun from -> copy_y from "tmptest.ml")
                //  REM NOTE: use --generate-interface-file since results may be in Unicode
                //  "%FSC%" %fsc_flags% --sig:tmptest.mli tmptest.ml
                //  if ERRORLEVEL 1 goto Error
                do! fsc "%s --sig:tmptest.mli" fsc_flags ["tmptest.ml"]

                //  echo Compiling against generated interface file...
                log "Compiling against generated interface file..."
                //  "%FSC%" %fsc_flags% -o:tmptest1.exe tmptest.mli tmptest.ml
                //  if ERRORLEVEL 1 goto Error
                do! fsc "%s -o:tmptest1.exe" fsc_flags ["tmptest.mli";"tmptest.ml"]

                //  if NOT EXIST dont.run.peverify (
                //    "%PEVERIFY%" tmptest1.exe
                //    @if ERRORLEVEL 1 goto Error
                //  )
                do! doPeverify "tmptest1.exe"
            // )
        //)
        }

    let doEmptySignature () = processor {
        //if NOT EXIST dont.use.empty.signature (
        if testDir/"dont.use.empty.signature" |> fileExists |> Option.isNone then
            // if exist test.ml ( 
            if testDir/"test.ml" |> fileExists |> Option.isSome then
                // echo Compiling against empty interface file...
                log "Compiling against empty interface file..."
                // echo // empty file  > tmptest2.mli
                echo_tofile "// empty file  " "tmptest2.mli"
                // copy /y %source1% tmptest2.ml
                do! source1 |> Option.map (fun from -> copy_y from "tmptest2.ml")
                // "%FSC%" %fsc_flags% --define:COMPILING_WITH_EMPTY_SIGNATURE -o:tmptest2.exe tmptest2.mli tmptest2.ml
                // if ERRORLEVEL 1 goto Error
                do! fsc "%s --define:COMPILING_WITH_EMPTY_SIGNATURE -o:tmptest2.exe" fsc_flags ["tmptest2.mli";"tmptest2.ml"]

                // if NOT EXIST dont.run.peverify (
                //     "%PEVERIFY%" tmptest2.exe
                //     @if ERRORLEVEL 1 goto Error
                // )
                do! doPeverify "tmptest2.exe"
            // )
        // )
        }


    let doEmptySignatureOpt () = processor {
        //if NOT EXIST dont.use.empty.signature (
        if testDir/"dont.use.empty.signature" |> fileExists |> Option.isNone then
            // if exist test.ml ( 
            if testDir/"test.ml" |> fileExists |> Option.isSome then
                // echo Compiling against empty interface file...
                log "Compiling against empty interface file..."
                // echo // empty file  > tmptest2.mli
                echo_tofile "// empty file  " "tmptest2.mli"
                // copy /y %source1% tmptest2.ml
                do! source1 |> Option.map (fun from -> copy_y from "tmptest2.ml")
                // "%FSC%" %fsc_flags% --define:COMPILING_WITH_EMPTY_SIGNATURE --optimize -o:tmptest2--optimize.exe tmptest2.mli tmptest2.ml
                // if ERRORLEVEL 1 goto Error
                do! fsc "%s --define:COMPILING_WITH_EMPTY_SIGNATURE --optimize -o:tmptest2--optimize.exe" fsc_flags ["tmptest2.mli";"tmptest2.ml"]

                // if NOT EXIST dont.run.peverify (
                //     "%PEVERIFY%" tmptest2--optimize.exe
                //     @if ERRORLEVEL 1 goto Error
                // )
                do! doPeverify "tmptest2--optimize.exe"
            // )
        // )
        }

    let doOptFscMinusDebug () = processor {
        // "%FSC%" %fsc_flags% --optimize- --debug -o:test--optminus--debug.exe -g %sources%
        // if ERRORLEVEL 1 goto Error
        do! fsc "%s --optimize- --debug -o:test--optminus--debug.exe -g" fsc_flags sources

        // if NOT EXIST dont.run.peverify (
        //     "%PEVERIFY%" test--optminus--debug.exe
        //     @if ERRORLEVEL 1 goto Error
        // )
        do! doPeverify "test--optminus--debug.exe"
        }

    let doOptFscPlusDebug () = processor {
        // "%FSC%" %fsc_flags% --optimize+ --debug -o:test--optplus--debug.exe -g %sources%
        // if ERRORLEVEL 1 goto Error
        do! fsc "%s --optimize+ --debug -o:test--optplus--debug.exe -g" fsc_flags sources

        // if NOT EXIST dont.run.peverify (
        //     "%PEVERIFY%" test--optplus--debug.exe
        //     @if ERRORLEVEL 1 goto Error
        // )
        do! doPeverify "test--optplus--debug.exe"
        }

    let doAsDLL () = processor {
        //REM Compile as a DLL to exercise pickling of interface data, then recompile the original source file referencing this DLL
        //REM THe second compilation will not utilize the information from the first in any meaningful way, but the
        //REM compiler will unpickle the interface and optimization data, so we test unpickling as well.

        //if NOT EXIST dont.compile.test.as.dll (
        if testDir/"dont.compile.test.as.dll" |> fileExists |> Option.isNone then
            // "%FSC%" %fsc_flags% --optimize -a -o:test--optimize-lib.dll -g %sources%
            // if ERRORLEVEL 1 goto Error
            do! fsc "%s --optimize -a -o:test--optimize-lib.dll -g" fsc_flags sources

            // "%FSC%" %fsc_flags% --optimize -r:test--optimize-lib.dll -o:test--optimize-client-of-lib.exe -g %sources%
            // if ERRORLEVEL 1 goto Error
            do! fsc "%s --optimize -r:test--optimize-lib.dll -o:test--optimize-client-of-lib.exe -g" fsc_flags sources

            // if NOT EXIST dont.run.peverify (
            //     "%PEVERIFY%" test--optimize-lib.dll
            //     @if ERRORLEVEL 1 goto Error
            // )
            do! doPeverify "test--optimize-lib.dll"

            // if NOT EXIST dont.run.peverify (
            //     "%PEVERIFY%" test--optimize-client-of-lib.exe
            // )
            // @if ERRORLEVEL 1 goto Error
            do! doPeverify "test--optimize-client-of-lib.exe"
        //)
        }

    let doWrapperNamespace () = processor {
        // if NOT EXIST dont.use.wrapper.namespace (
        if testDir/"dont.use.wrapper.namespace" |> fileExists |> Option.isNone then
            // if exist test.ml (
            if testDir/"test.ml" |> fileExists |> Option.isSome then
                // echo Compiling when wrapped in a namespace declaration...
                log "Compiling when wrapped in a namespace declaration..."
                // echo module TestNamespace.TestModule > tmptest3.ml
                echo_tofile "module TestNamespace.TestModule " "tmptest3.ml"
                // type %source1%  >> tmptest3.ml
                source1 |> Option.iter (fun from -> type_append_tofile from "tmptest3.ml")
                // "%FSC%" %fsc_flags% -o:tmptest3.exe tmptest3.ml
                // if ERRORLEVEL 1 goto Error
                do! fsc "%s -o:tmptest3.exe" fsc_flags ["tmptest3.ml"]

                // if NOT EXIST dont.run.peverify (
                //     "%PEVERIFY%" tmptest3.exe
                //     @if ERRORLEVEL 1 goto Error
                // )
                do! doPeverify "tmptest3.exe"
            // )
        //)
        }

    let doWrapperNamespaceOpt () = processor {
        //if NOT EXIST dont.use.wrapper.namespace (
        if testDir/"dont.use.wrapper.namespace" |> fileExists |> Option.isNone then
            // if exist test.ml (
            if testDir/"test.ml" |> fileExists |> Option.isSome then
                // echo Compiling when wrapped in a namespace declaration...
                log "Compiling when wrapped in a namespace declaration..."
                // echo module TestNamespace.TestModule > tmptest3.ml
                echo_tofile "module TestNamespace.TestModule " "tmptest3.ml"
                // type %source1%  >> tmptest3.ml
                source1 |> Option.iter (fun from -> type_append_tofile from "tmptest3.ml")
                // "%FSC%" %fsc_flags% --optimize -o:tmptest3--optimize.exe tmptest3.ml
                // if ERRORLEVEL 1 goto Error
                do! fsc "%s --optimize -o:tmptest3--optimize.exe" fsc_flags ["tmptest3.ml"]

                // if NOT EXIST dont.run.peverify (
                //     "%PEVERIFY%" tmptest3--optimize.exe
                //     @if ERRORLEVEL 1 goto Error
                // )
                do! doPeverify "tmptest3--optimize.exe"
            // )
        // )
        }

    let build = function
        | FSI_FILE -> doNOOP
        | FSI_STDIN -> doNOOP
        | FSI_STDIN_OPT -> doNOOP
        | FSI_STDIN_GUI -> doNOOP
        | FRENCH -> doBasic
        | SPANISH -> doBasic
        | FSC_BASIC -> doBasic
        | FSC_BASIC_64 -> doBasic64
        | FSC_HW -> doFscHW
        | FSC_O3 -> doFscO3
        | GENERATED_SIGNATURE -> doGeneratedSignature
        | EMPTY_SIGNATURE -> doEmptySignature
        | EMPTY_SIGNATURE_OPT -> doEmptySignatureOpt
        | FSC_OPT_MINUS_DEBUG -> doOptFscMinusDebug
        | FSC_OPT_PLUS_DEBUG -> doOptFscPlusDebug
        | AS_DLL -> doAsDLL
        | WRAPPER_NAMESPACE -> doWrapperNamespace
        | WRAPPER_NAMESPACE_OPT -> doWrapperNamespaceOpt

    let flow p () =
        build p () 
        |> Attempt.Run 
        |> function 
            | Success () -> doneOk () 
            | Failure (Skipped _) -> doneSkipped ()
            | Failure (GenericError msg) -> doneError (GenericError msg) msg
            | Failure (ProcessExecError (err,msg)) -> doneError (ProcessExecError(err,msg)) msg
    
    flow