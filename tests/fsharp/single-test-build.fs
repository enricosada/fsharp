module SingleTestBuild

open System
open System.IO
open System.Diagnostics
open NUnit.Framework
open All
open TestConfig

let singleTestBuild cfg testDir =
    //@if "%_echo%"=="" echo off
    //setlocal
    ignore "useless"

    //if EXIST build.ok DEL /f /q build.ok
    let buildOkPath = testDir / "build.ok"
    buildOkPath |> fileExists |> Option.iter File.Delete //TODO "/f" -> forza rimozione readonly, "/Q" -> no interactive

    //call %~d0%~p0..\config.bat
    ignore "param"

    //if NOT "%FSC:NOTAVAIL=X%" == "%FSC%" (
    //  goto Skip
    //)
    //TODO boh

    //set source1=
    //if exist test.ml (set source1=test.ml)
    //if exist test.fs (set source1=test.fs)
    let source1 = 
        ["test.ml"; "test.fs"] 
        |> List.filter (fun name -> (testDir/name) |> fileExists |> Option.isSome)

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

    let { echo_tofile = echo_tofile; 
          copy_y = copy_y; 
          type_append_tofile = type_append_tofile;
          peverify = peverify;
          fsc = fsc;
        } = getHelpers cfg testDir

    //:Ok
    let doneOk () =
        //echo Built fsharp %~f0 ok.
        echo "Built fsharp %s ok." testDir
        //echo. > build.ok
        echo_tofile Environment.NewLine "build.ok"
        //endlocal
        //exit /b 0
        ()

    //:Skip
    let doneSkipped () =
        //echo Skipped %~f0
        echo "Skipped %s" testDir
        //endlocal
        //exit /b 0
        ()

    //:Error
    let doneError err msg =
        //echo Test Script Failed (perhaps test did not emit test.ok signal file?)
        //endlocal
        //exit /b %ERRORLEVEL%
        Assert.Fail (sprintf "ERRORLEVEL %i %s" err msg)

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
    let doPeverify cmd = 
        match testDir/"dont.run.peverify" |> fileExists with
        | None ->
            match peverify cmd with
            | Success -> OK
            | ErrorLevel err -> Error (err, "peverify error")
        | Some _ -> Skipped "dont.run.peverify found"

    let doNOOP () =
        //@echo No build action to take for this permutation
        echo "No build action to take for this permutation"
        OK

    let doBasic () =
        // FSC %fsc_flags% --define:BASIC_TEST -o:test.exe -g %sources%
        //if ERRORLEVEL 1 goto Error
        match fsc (sprintf "%s --define:BASIC_TEST -o:test.exe -g" cfg.fsc_flags) sources with
        | ErrorLevel err -> Error (err, genericErrorMessage)
        | Success ->
            //if NOT EXIST dont.run.peverify (
            //    "%PEVERIFY%" test.exe
            //    @if ERRORLEVEL 1 goto Error
            //)
            match doPeverify "text.exe" with OK | Skipped _ -> OK | Error x -> Error x

    let doBasic64 () =
        // "%FSC%" %fsc_flags% --define:BASIC_TEST --platform:x64 -o:testX64.exe -g %sources%
        // if ERRORLEVEL 1 goto Error
        match fsc (sprintf "%s --define:BASIC_TEST --platform:x64 -o:testX64.exe -g" cfg.fsc_flags) sources with
        | ErrorLevel err -> Error (err, genericErrorMessage)
        | Success ->
            // if NOT EXIST dont.run.peverify (
            //     "%PEVERIFY%" testX64.exe
            //     @if ERRORLEVEL 1 goto Error
            // )
            match doPeverify "testX64.exe" with OK | Skipped _ -> OK | Error x -> Error x

    let doFscHW () =
        // if exist test-hw.* (
        if Directory.EnumerateFiles(testDir, "test-hw.*") |> Seq.tryPick fileExists |> Option.isSome then (
            // "%FSC%" %fsc_flags% -o:test-hw.exe -g %sourceshw%
            // if ERRORLEVEL 1 goto Error
            match fsc (sprintf "%s -o:test-hw.exe -g" cfg.fsc_flags) sourceshw with
            | ErrorLevel err -> Error (err, genericErrorMessage)
            | Success ->
                // if NOT EXIST dont.run.peverify (
                //   "%PEVERIFY%" test-hw.exe
                //   @if ERRORLEVEL 1 goto Error
                // )
                match doPeverify "test-hw.exe" with OK | Skipped _ -> OK | Error x -> Error x
        //)
        ) else Skipped "not found test-hw.*"

    let doFscO3 () =
        //"%FSC%" %fsc_flags% --optimize --define:PERF -o:test--optimize.exe -g %sources%
        //if ERRORLEVEL 1 goto Error
        match fsc (sprintf "%s --optimize --define:PERF -o:test--optimize.exe -g" cfg.fsc_flags) sources with
        | ErrorLevel err -> Error (err, genericErrorMessage)
        | Success ->
            //if NOT EXIST dont.run.peverify (
            //    "%PEVERIFY%" test--optimize.exe
            //    @if ERRORLEVEL 1 goto Error
            //)
            match doPeverify "test--optimize.exe" with OK | Skipped _ -> OK | Error x -> Error x

    let doGeneratedSignature () =
        //if NOT EXIST dont.use.generated.signature (
        match testDir/"dont.use.generated.signature" |> fileExists with
        | Some _ -> Skipped "dont.use.generated.signature found"
        | None ->
            // if exist test.ml (
            match testDir/"test.ml" |> fileExists with
            | None -> Skipped "not found test.ml"
            | Some _ ->
                //  echo Generating interface file...
                echo "%s" "Generating interface file..."
                //  copy /y %source1% tmptest.ml
                copy_y source1 "tmptest.ml"
                //  REM NOTE: use --generate-interface-file since results may be in Unicode
                //  "%FSC%" %fsc_flags% --sig:tmptest.mli tmptest.ml
                //  if ERRORLEVEL 1 goto Error
                match fsc (sprintf "%s --sig:tmptest.mli" cfg.fsc_flags) ["tmptest.ml"] with
                | ErrorLevel err -> Error (err, genericErrorMessage)
                | Success ->
                    //  echo Compiling against generated interface file...
                    echo "%s" "Compiling against generated interface file..."
                    //  "%FSC%" %fsc_flags% -o:tmptest1.exe tmptest.mli tmptest.ml
                    //  if ERRORLEVEL 1 goto Error
                    match fsc (sprintf "%s -o:tmptest1.exe" cfg.fsc_flags) ["tmptest.mli";"tmptest.ml"] with
                    | ErrorLevel err -> Error (err, genericErrorMessage)
                    | Success ->
                        //  if NOT EXIST dont.run.peverify (
                        //    "%PEVERIFY%" tmptest1.exe
                        //    @if ERRORLEVEL 1 goto Error
                        //  )
                        match doPeverify "tmptest1.exe" with OK | Skipped _ -> OK | Error x -> Error x
            // )
        //)

    let doEmptySignature () =
        //if NOT EXIST dont.use.empty.signature (
        match testDir/"dont.use.empty.signature" |> fileExists with
        | Some _ -> Skipped "dont.use.empty.signature found"
        | None ->
            // if exist test.ml ( 
            match testDir/"test.ml" |> fileExists with
            | None -> Skipped "not found test.ml"
            | Some _ ->
                // echo Compiling against empty interface file...
                echo "%s" "Compiling against empty interface file..."
                // echo // empty file  > tmptest2.mli
                echo_tofile "// empty file" "tmptest2.mli"
                // copy /y %source1% tmptest2.ml
                copy_y source1 "tmptest2.ml"
                // "%FSC%" %fsc_flags% --define:COMPILING_WITH_EMPTY_SIGNATURE -o:tmptest2.exe tmptest2.mli tmptest2.ml
                // if ERRORLEVEL 1 goto Error
                match fsc (sprintf "%s --define:COMPILING_WITH_EMPTY_SIGNATURE -o:tmptest2.exe" cfg.fsc_flags) ["tmptest2.mli";"tmptest2.ml"] with
                | ErrorLevel err -> Error (err, genericErrorMessage)
                | Success ->
                    // if NOT EXIST dont.run.peverify (
                    //     "%PEVERIFY%" tmptest2.exe
                    //     @if ERRORLEVEL 1 goto Error
                    // )
                    match doPeverify "tmptest2.exe" with OK | Skipped _ -> OK | Error x -> Error x
            // )
        // )


    let doEmptySignatureOpt () =
        //if NOT EXIST dont.use.empty.signature (
        match testDir/"dont.use.empty.signature" |> fileExists with
        | Some _ -> Skipped "dont.use.empty.signature found"
        | None ->
            // if exist test.ml ( 
            match testDir/"test.ml" |> fileExists with
            | None -> Skipped "not found test.ml"
            | Some _ ->
                // echo Compiling against empty interface file...
                echo "%s" "Compiling against empty interface file..."
                // echo // empty file  > tmptest2.mli
                echo_tofile "// empty file" "tmptest2.mli"
                // copy /y %source1% tmptest2.ml
                copy_y source1 "tmptest2.ml"
                // "%FSC%" %fsc_flags% --define:COMPILING_WITH_EMPTY_SIGNATURE --optimize -o:tmptest2--optimize.exe tmptest2.mli tmptest2.ml
                // if ERRORLEVEL 1 goto Error
                match fsc (sprintf "%s --define:COMPILING_WITH_EMPTY_SIGNATURE --optimize -o:tmptest2--optimize.exe" cfg.fsc_flags) ["tmptest2.mli";"tmptest2.ml"] with
                | ErrorLevel err -> Error (err, genericErrorMessage)
                | Success ->
                    // if NOT EXIST dont.run.peverify (
                    //     "%PEVERIFY%" tmptest2--optimize.exe
                    //     @if ERRORLEVEL 1 goto Error
                    // )
                    match doPeverify "tmptest2--optimize.exe" with OK | Skipped _ -> OK | Error x -> Error x
            // )
        // )

    let doOptFscMinusDebug () =
        // "%FSC%" %fsc_flags% --optimize- --debug -o:test--optminus--debug.exe -g %sources%
        // if ERRORLEVEL 1 goto Error
        match fsc (sprintf "%s --optimize- --debug -o:test--optminus--debug.exe -g" cfg.fsc_flags) sources with
        | ErrorLevel err -> Error (err, genericErrorMessage)
        | Success ->
            // if NOT EXIST dont.run.peverify (
            //     "%PEVERIFY%" test--optminus--debug.exe
            //     @if ERRORLEVEL 1 goto Error
            // )
            match doPeverify "test--optminus--debug.exe" with OK | Skipped _ -> OK | Error x -> Error x

    let doOptFscPlusDebug () =
        // "%FSC%" %fsc_flags% --optimize+ --debug -o:test--optplus--debug.exe -g %sources%
        // if ERRORLEVEL 1 goto Error
        match fsc (sprintf "%s --optimize+ --debug -o:test--optplus--debug.exe -g" cfg.fsc_flags) sources with
        | ErrorLevel err -> Error (err, genericErrorMessage)
        | Success ->
            // if NOT EXIST dont.run.peverify (
            //     "%PEVERIFY%" test--optplus--debug.exe
            //     @if ERRORLEVEL 1 goto Error
            // )
            match doPeverify "test--optplus--debug.exe" with OK | Skipped _ -> OK | Error x -> Error x

    let doAsDLL () =
        //REM Compile as a DLL to exercise pickling of interface data, then recompile the original source file referencing this DLL
        //REM THe second compilation will not utilize the information from the first in any meaningful way, but the
        //REM compiler will unpickle the interface and optimization data, so we test unpickling as well.

        //if NOT EXIST dont.compile.test.as.dll (
        match testDir/"dont.compile.test.as.dll" |> fileExists with
        | Some _ -> Skipped "dont.compile.test.as.dll found"
        | None ->
            // "%FSC%" %fsc_flags% --optimize -a -o:test--optimize-lib.dll -g %sources%
            // if ERRORLEVEL 1 goto Error
            match fsc (sprintf "%s --optimize -a -o:test--optimize-lib.dll -g" cfg.fsc_flags) sources with
            | ErrorLevel err -> Error (err, genericErrorMessage)
            | Success ->
                // "%FSC%" %fsc_flags% --optimize -r:test--optimize-lib.dll -o:test--optimize-client-of-lib.exe -g %sources%
                // if ERRORLEVEL 1 goto Error
                match fsc (sprintf "%s --optimize -r:test--optimize-lib.dll -o:test--optimize-client-of-lib.exe -g" cfg.fsc_flags) sources with
                | ErrorLevel err -> Error (err, genericErrorMessage)
                | Success ->
                    // if NOT EXIST dont.run.peverify (
                    //     "%PEVERIFY%" test--optimize-lib.dll
                    //     @if ERRORLEVEL 1 goto Error
                    // )
                    match doPeverify "test--optimize-lib.dll" with 
                    | Error x -> Error x
                    | OK | Skipped _ ->
                        // if NOT EXIST dont.run.peverify (
                        //     "%PEVERIFY%" test--optimize-client-of-lib.exe
                        // )
                        // @if ERRORLEVEL 1 goto Error
                        match doPeverify "test--optimize-client-of-lib.exe" with OK | Skipped _ -> OK | Error x -> Error x
        //)

    let doWrapperNamespace () =
        // if NOT EXIST dont.use.wrapper.namespace (
        match testDir/"dont.use.wrapper.namespace" |> fileExists with
        | Some _ -> Skipped "dont.use.wrapper.namespace found"
        | None ->
            // if exist test.ml (
            match testDir/"test.ml" |> fileExists with
            | None -> Skipped "not found test.ml"
            | Some _ -> 
                // echo Compiling when wrapped in a namespace declaration...
                echo "%s" "Compiling when wrapped in a namespace declaration..."
                // echo module TestNamespace.TestModule > tmptest3.ml
                echo_tofile "module TestNamespace.TestModule" "tmptest3.ml"
                // type %source1%  >> tmptest3.ml
                type_append_tofile source1 "tmptest3.ml"
                // "%FSC%" %fsc_flags% -o:tmptest3.exe tmptest3.ml
                // if ERRORLEVEL 1 goto Error
                match fsc (sprintf "%s -o:tmptest3.exe" cfg.fsc_flags) ["tmptest3.ml"] with
                | ErrorLevel err -> Error (err, genericErrorMessage)
                | Success ->
                    // if NOT EXIST dont.run.peverify (
                    //     "%PEVERIFY%" tmptest3.exe
                    //     @if ERRORLEVEL 1 goto Error
                    // )
                    match doPeverify "tmptest3.exe" with OK | Skipped _ -> OK | Error x -> Error x
            // )
        //)

    let doWrapperNamespaceOpt () =
        //if NOT EXIST dont.use.wrapper.namespace (
        match testDir/"dont.use.wrapper.namespace" |> fileExists with
        | Some _ -> Skipped "dont.use.wrapper.namespace found"
        | None ->
            // if exist test.ml (
            match testDir/"test.ml" |> fileExists with
            | None -> Skipped "not found test.ml"
            | Some _ ->
                // echo Compiling when wrapped in a namespace declaration...
                echo "%s" "Compiling when wrapped in a namespace declaration..."
                // echo module TestNamespace.TestModule > tmptest3.ml
                echo_tofile "module TestNamespace.TestModule" "tmptest3.ml"
                // type %source1%  >> tmptest3.ml
                type_append_tofile source1 "tmptest3.ml"
                // "%FSC%" %fsc_flags% --optimize -o:tmptest3--optimize.exe tmptest3.ml
                // if ERRORLEVEL 1 goto Error
                match fsc (sprintf "%s --optimize -o:tmptest3--optimize.exe" cfg.fsc_flags) ["tmptest3.ml"] with
                | ErrorLevel err -> Error (err, genericErrorMessage)
                | Success ->
                    // if NOT EXIST dont.run.peverify (
                    //     "%PEVERIFY%" tmptest3--optimize.exe
                    //     @if ERRORLEVEL 1 goto Error
                    // )
                    match doPeverify "tmptest3--optimize.exe" with OK | Skipped _ -> OK | Error x -> Error x
            // )
        // )

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

    let checkBuild = function
        | OK -> doneOk ()
        | Skipped _ -> doneOk ()
        | Error (err,msg) -> doneError err msg

    (fun p -> build p () |> checkBuild)
