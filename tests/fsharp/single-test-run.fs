module SingleTestRun

open System
open System.IO
open TestConfig
open NUnit.Framework
open PlatformHelpers

let withFileGuard path (f: Attempt<_,_>) = processor {
    //  if exist test.ok (del /f /q test.ok)
    path |> fileExists |> Option.iter File.Delete
    //  %CLIX% "%FSI%" %fsi_flags% < %sources% && (
    //  dir test.ok > NUL 2>&1 ) || (
    //  @echo FSI_STDIN failed;
    //  set ERRORMSG=%ERRORMSG% FSI_STDIN failed;
    //  )
    do! f ()

    if path |> fileExists |> Option.isNone then
        let msg = sprintf "exit code 0 but %s file doesn't exists" (Path.GetFileName(path))
        return! (fun () -> Failure (Error (0, msg)))
    }

let singleTestRun' cfg testDir =

    let fullpath path = if Path.IsPathRooted(path) then path else (testDir/path)
    let fileExists = fullpath >> fileExists

    // set sources=
    // if exist testlib.fsi (set sources=%sources% testlib.fsi)
    // if exist testlib.fs (set sources=%sources% testlib.fs)
    // if exist test.mli (set sources=%sources% test.mli)
    // if exist test.ml (set sources=%sources% test.ml)
    // if exist test.fsi (set sources=%sources% test.fsi)
    // if exist test.fs (set sources=%sources% test.fs)
    // if exist test2.mli (set sources=%sources% test2.mli)
    // if exist test2.ml (set sources=%sources% test2.ml)
    // if exist test2.fsi (set sources=%sources% test2.fsi)
    // if exist test2.fs (set sources=%sources% test2.fs)
    // if exist test.fsx (set sources=%sources% test.fsx)
    // if exist test2.fsx (set sources=%sources% test2.fsx)
    let sources =
        ["testlib.fsi";"testlib.fs";"test.mli";"test.ml";"test.fsi";"test.fs";"test2.mli";"test2.ml";"test2.fsi";"test2.fs";"test.fsx";"test2.fsx"]
        |> List.filter (fileExists >> Option.isSome)

    // set sourceshw=
    // if exist test-hw.mli (set sourceshw=%sourceshw% test-hw.mli)
    // if exist test-hw.ml (set sourceshw=%sourceshw% test-hw.ml)
    // if exist test2-hw.mli (set sourceshw=%sourceshw% test2-hw.mli)
    // if exist test2-hw.ml (set sourceshw=%sourceshw% test2-hw.ml)
    // if exist test-hw.fsi (set sourceshw=%sourceshw% test-hw.fsi)
    // if exist test-hw.fs (set sourceshw=%sourceshw% test-hw.fs)
    // if exist test2-hw.fsi (set sourceshw=%sourceshw% test2-hw.fsi)
    // if exist test2-hw.fs (set sourceshw=%sourceshw% test2-hw.fs)
    // if exist test-hw.fsx (set sourceshw=%sourceshw% test-hw.fsx)
    // if exist test2-hw.fsx (set sourceshw=%sourceshw% test2-hw.fsx)
    let sourceshw =
        ["test-hw.mli";"test-hw.ml";"test2-hw.mli";"test2-hw.ml";"test-hw.fsi";"test-hw.fs";"test2-hw.fsi";"test2-hw.fs";"test-hw.fsx";"test2-hw.fsx"]
        |> List.filter (fileExists >> Option.isSome)

    // :START

    // set PERMUTATIONS_LIST=FSI_FILE FSI_STDIN FSI_STDIN_OPT FSI_STDIN_GUI FSC_BASIC %FSC_BASIC_64% FSC_HW FSC_O3 GENERATED_SIGNATURE EMPTY_SIGNATURE EMPTY_SIGNATURE_OPT FSC_OPT_MINUS_DEBUG FSC_OPT_PLUS_DEBUG FRENCH SPANISH AS_DLL WRAPPER_NAMESPACE WRAPPER_NAMESPACE_OPT
    // 
    // if "%REDUCED_RUNTIME%"=="1" (
    //     echo REDUCED_RUNTIME set
    //     
    //     if not defined PERMUTATIONS (
    //         powershell.exe %PSH_FLAGS% -command "&{& '%~d0%~p0\PickPermutations.ps1' '%cd%' '%FSC%' '%PERMUTATIONS_LIST%'}" > _perm.txt
    //         if errorlevel 1 (
    //             set ERRORMSG=%ERRORMSG% PickPermutations.ps1 failed;
    //             goto :ERROR
    //         )
    //         set /p PERMUTATIONS=<_perm.txt
    //     )
    // )

    // if not defined PERMUTATIONS (
    //     echo "PERMUTATIONS not defined. Running everything."
    //     set PERMUTATIONS=%PERMUTATIONS_LIST%
    // )

    // for %%A in (%PERMUTATIONS%) do (
    //     call :%%A
    //     IF ERRORLEVEL 1 EXIT /B 1
    // )

    // if "%ERRORMSG%"==""  goto Ok

    // set NonexistentErrorLevel 2> nul
    // goto :ERROR

    // :END

    // :EXIT_PATHS

    // REM =========================================
    // REM THE TESTS
    // REM =========================================

    let exec exe args = 
        log "%s %s" exe args
        exec' { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = None; } testDir cfg.EnvironmentVariables exe args

    let execIn input exe args = 
        exec' { RedirectOutput = Some (log "%s"); RedirectError = Some (log "%s"); RedirectInput = Some input; } testDir cfg.EnvironmentVariables exe args

    let clix exe = exec (testDir/exe) >> checkResult
    let fsi args = Commands.fsi exec cfg.FSI args >> checkResult
    let fsiIn flags sources =
        log "%s %s < %s" cfg.FSI flags (sources |> Seq.ofList |> String.concat " ")
        let inputs = sources |> List.map fullpath
        inputs
        |> List.map (fun p -> (p, p |> fileExists))
        |> List.tryPick (function p, None -> Some p | _, Some _ -> None)
        |> function
           | Some p ->
               log "redirected file '%s' not found" p
               ErrorLevel -1
           | None ->
               Commands.fsiIn execIn cfg.FSI flags inputs
        |> checkResult

    let fsi_flags = cfg.fsi_flags

    let withTestOkFile = withFileGuard (testDir/"test.ok")

    // :FSI_STDIN
    // @echo do :FSI_STDIN
    let runFSI_STDIN () = processor {
        // if NOT EXIST dont.pipe.to.stdin (
        if fileExists "dont.pipe.to.stdin" |> Option.isNone then
            // if exist test.ok (del /f /q test.ok)
            // %CLIX% "%FSI%" %fsi_flags% < %sources% && (
            let run () = fsiIn fsi_flags sources
            // dir test.ok > NUL 2>&1 ) || (
            // @echo FSI_STDIN failed;
            // set ERRORMSG=%ERRORMSG% FSI_STDIN failed;
            // )
            do! run |> withTestOkFile
        // )
        }

    // :FSI_STDIN_OPT
    // @echo do :FSI_STDIN_OPT
    let runFSI_STDIN_OPT () = processor {
        // if NOT EXIST dont.pipe.to.stdin (
        if fileExists "dont.pipe.to.stdin" |> Option.isNone then
            // if exist test.ok (del /f /q test.ok)
            // %CLIX% "%FSI%" %fsi_flags% --optimize < %sources% && (
            let run () = fsiIn (sprintf "%s --optimize" fsi_flags) sources
            // dir test.ok > NUL 2>&1 ) || (
            // @echo FSI_STDIN_OPT failed
            // set ERRORMSG=%ERRORMSG% FSI_STDIN_OPT failed;
            // )
            do! run |> withTestOkFile
        // )
        }

    // :FSI_STDIN_GUI
    // @echo do :FSI_STDIN_GUI
    let runFSI_STDIN_GUI () = processor {
        // if NOT EXIST dont.pipe.to.stdin (
        if fileExists "dont.pipe.to.stdin" |> Option.isNone then
            // if exist test.ok (del /f /q test.ok)
            // %CLIX% "%FSI%" %fsi_flags% --gui < %sources% && (
            let run () = fsiIn (sprintf "%s --gui" fsi_flags) sources
            // dir test.ok > NUL 2>&1 ) || (
            // @echo FSI_STDIN_GUI failed;
            // set ERRORMSG=%ERRORMSG% FSI_STDIN_GUI failed;
            // )
            do! run |> withTestOkFile
        // )
        }

    // :FSI_FILE
    // @echo do :FSI_FILE
    let runFSI_FILE () = processor {
        // if NOT EXIST dont.run.as.script (
        if fileExists "dont.run.as.script" |> Option.isNone then
            // if exist test.ok (del /f /q test.ok)
            // %CLIX% "%FSI%" %fsi_flags% %sources% && (
            let run () = fsi fsi_flags sources
            // dir test.ok > NUL 2>&1 ) || (
            // @echo FSI_FILE failed
            // set ERRORMSG=%ERRORMSG% FSI_FILE failed;
            // )
            do! run |> withTestOkFile
        // )
        }

    // :FSC_BASIC
    // @echo do :FSC_BASIC
    let runFSC_BASIC () = processor {
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\test.exe && (
        let run () = clix "test.exe" ""
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSC_BASIC failed
        // set ERRORMSG=%ERRORMSG% FSC_BASIC failed;
        // )
        do! run |> withTestOkFile
        }

    // :FSC_BASIC_64
    // @echo do :FSC_BASIC_64
    let runFSC_BASIC_64 () = processor {
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\testX64.exe && (
        let run () = clix "testX64.exe" ""
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSC_BASIC_64 failed
        // set ERRORMSG=%ERRORMSG% FSC_BASIC_64 failed;
        // )
        do! run |> withTestOkFile
        }

    // :FSC_HW
    // @echo do :FSC_HW
    let runFSC_HW () = processor {
        // if exist test-hw.* (
        if Directory.EnumerateFiles(testDir, "test-hw.*") |> Seq.tryPick fileExists |> Option.isSome then
            // if exist test.ok (del /f /q test.ok)
            // %CLIX% .\test-hw.exe && (
            let run () = clix "test-hw.exe" ""
            // dir test.ok > NUL 2>&1 ) || (
            // @echo  :FSC_HW failed
            // set ERRORMSG=%ERRORMSG% FSC_HW failed;
            // )
            do! run |> withTestOkFile
        //)
        }

    // :FSC_O3
    // @echo do :FSC_O3
    let runFSC_O3 () = processor {
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\test--optimize.exe && (
        let run () = clix "test--optimize.exe" ""
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSC_O3 failed
        // set ERRORMSG=%ERRORMSG% FSC_03 failed;
        // )
        do! run |> withTestOkFile
        }

    // :FSC_OPT_MINUS_DEBUG
    // @echo do :FSC_OPT_MINUS_DEBUG
    let runFSC_OPT_MINUS_DEBUG () = processor {
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\test--optminus--debug.exe && (
        let run () = clix "test--optminus--debug.exe" ""
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSC_OPT_MINUS_DEBUG failed
        // set ERRORMSG=%ERRORMSG% FSC_OPT_MINUS_DEBUG failed;
        // )
        do! run |> withTestOkFile
        }

    // :FSC_OPT_PLUS_DEBUG
    // @echo do :FSC_OPT_PLUS_DEBUG
    let runFSC_OPT_PLUS_DEBUG () = processor {
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\test--optplus--debug.exe && (
        let run () = clix "test--optplus--debug.exe" ""
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSC_OPT_PLUS_DEBUG failed
        // set ERRORMSG=%ERRORMSG% FSC_OPT_PLUS_DEBUG failed;
        // )
        do! run |> withTestOkFile
        }

    // :GENERATED_SIGNATURE
    // @echo do :GENERATED_SIGNATURE
    let runGENERATED_SIGNATURE () = processor {
        // if NOT EXIST dont.use.generated.signature (
        if "dont.use.generated.signature" |> fileExists |> Option.isNone then
            // if exist test.ml (
            if "test.ml" |> fileExists |> Option.isSome then
                // if exist test.ok (del /f /q test.ok)
                // %CLIX% tmptest1.exe && (
                let run () = clix "tmptest1.exe" ""
                // dir test.ok > NUL 2>&1 ) || (
                // @echo :GENERATED_SIGNATURE failed
                // set ERRORMSG=%ERRORMSG% FSC_GENERATED_SIGNATURE failed;
                // )
                do! run |> withTestOkFile
            // )
        //)
        }

    // :EMPTY_SIGNATURE
    // @echo do :EMPTY_SIGNATURE
    let runEMPTY_SIGNATURE () = processor {
        // if NOT EXIST dont.use.empty.signature (
        if "dont.use.empty.signature" |> fileExists |> Option.isNone then
            // if exist test.ml (
            if "test.ml" |> fileExists |> Option.isSome then
                // if exist test.ok (del /f /q test.ok)
                // %CLIX% tmptest2.exe && (
                let run () = clix "tmptest2.exe" ""
                // dir test.ok > NUL 2>&1 ) || (
                // @echo :EMPTY_SIGNATURE failed
                // set ERRORMSG=%ERRORMSG% FSC_EMPTY_SIGNATURE failed;
                // )
                do! run |> withTestOkFile
            // )
        //)
        }

    // :EMPTY_SIGNATURE_OPT
    // @echo do :EMPTY_SIGNATURE_OPT
    let runEMPTY_SIGNATURE_OPT () = processor {
        // if NOT EXIST dont.use.empty.signature (
        if "dont.use.empty.signature" |> fileExists |> Option.isNone then
            // if exist test.ml (
            if "test.ml" |> fileExists |> Option.isSome then
                // if exist test.ok (del /f /q test.ok)
                // %CLIX% tmptest2--optimize.exe && (
                let run () = clix "tmptest2--optimize.exe" ""
                // dir test.ok > NUL 2>&1 ) || (
                // @echo :EMPTY_SIGNATURE_OPT --optimize failed
                // set ERRORMSG=%ERRORMSG% EMPTY_SIGNATURE_OPT --optimize failed;
                // )
                do! run |> withTestOkFile
            // )
        //)
        }

    // :FRENCH
    // @echo do :FRENCH
    let runFRENCH () = processor {
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\test.exe fr-FR && (
        let run () = clix "test.exe" "fr-FR"
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FRENCH failed
        // set ERRORMSG=%ERRORMSG% FRENCH failed;
        // )
        do! run |> withTestOkFile
        }

    // :SPANISH
    // @echo do :SPANISH
    let runSPANISH () = processor {
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\test.exe es-ES && (
        let run () = clix "test.exe" "es-ES"
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :SPANISH failed
        // set ERRORMSG=%ERRORMSG% SPANISH failed;
        // )
        do! run |> withTestOkFile
        }

    // :AS_DLL
    // @echo do :AS_DLL
    let runAS_DLL () = processor {
        //if NOT EXIST dont.compile.test.as.dll (
        if "dont.compile.test.as.dll" |> fileExists |> Option.isNone then
            // if exist test.ok (del /f /q test.ok)
            // %CLIX% .\test--optimize-client-of-lib.exe && (
            let run () = clix "test--optimize-client-of-lib.exe" ""
            // dir test.ok > NUL 2>&1 ) || (
            // @echo :AS_DLL failed
            // set ERRORMSG=%ERRORMSG% AS_DLL failed;
            // )
            do! run |> withTestOkFile
        //)
        }

    // :WRAPPER_NAMESPACE
    // @echo do :WRAPPER_NAMESPACE
    let runWRAPPER_NAMESPACE () = processor {
        // if NOT EXIST dont.use.wrapper.namespace (
        if "dont.use.wrapper.namespace" |> fileExists |> Option.isNone then
            // if exist test.ml (
            if "test.ml" |> fileExists |> Option.isSome then
                // if exist test.ok (del /f /q test.ok)
                // %CLIX% .\tmptest3.exe && (
                let run () = clix "tmptest3.exe" ""
                // dir test.ok > NUL 2>&1 ) || (
                // @echo :WRAPPER_NAMESPACE failed
                // set ERRORMSG=%ERRORMSG% WRAPPER_NAMESPACE failed;
                // )
                do! run |> withTestOkFile
            // )
        //)
        }

    // :WRAPPER_NAMESPACE_OPT
    // @echo do :WRAPPER_NAMESPACE_OPT
    let runWRAPPER_NAMESPACE_OPT () = processor {
        // if NOT EXIST dont.use.wrapper.namespace (
        if "dont.use.wrapper.namespace" |> fileExists |> Option.isNone then
            // if exist test.ml (
            if "test.ml" |> fileExists |> Option.isSome then
                // if exist test.ok (del /f /q test.ok)
                // %CLIX% .\tmptest3--optimize.exe && (
                let run () = clix "tmptest3--optimize.exe" ""
                // dir test.ok > NUL 2>&1 ) || (
                // @echo :WRAPPER_NAMESPACE_OPT failed
                // set ERRORMSG=%ERRORMSG% WRAPPER_NAMESPACE_OPT failed;
                // )
                do! run |> withTestOkFile
            // )
        // )
        }

    let run = function
        | FSI_FILE -> runFSI_FILE
        | FSI_STDIN -> runFSI_STDIN
        | FSI_STDIN_OPT -> runFSI_STDIN_OPT
        | FSI_STDIN_GUI -> runFSI_STDIN_GUI
        | FRENCH -> runFRENCH
        | SPANISH -> runSPANISH
        | FSC_BASIC -> runFSC_BASIC
        | FSC_BASIC_64 -> runFSC_BASIC_64
        | FSC_HW -> runFSC_HW
        | FSC_O3 -> runFSC_O3
        | GENERATED_SIGNATURE -> runGENERATED_SIGNATURE
        | EMPTY_SIGNATURE -> runEMPTY_SIGNATURE
        | EMPTY_SIGNATURE_OPT -> runEMPTY_SIGNATURE_OPT
        | FSC_OPT_MINUS_DEBUG -> runFSC_OPT_MINUS_DEBUG
        | FSC_OPT_PLUS_DEBUG -> runFSC_OPT_PLUS_DEBUG
        | AS_DLL -> runAS_DLL
        | WRAPPER_NAMESPACE -> runWRAPPER_NAMESPACE
        | WRAPPER_NAMESPACE_OPT -> runWRAPPER_NAMESPACE_OPT

    run

let singleTestRun config testDir =
    //@if "%_echo%"=="" echo off
    //setlocal
    ignore "unused"

    //set ERRORMSG=
    ignore "unused"

    //:Ok
    let doneOK () =
        //echo Ran fsharp %~f0 ok.
        log "Ran fsharp %s ok." testDir
        //exit /b 0
        Success () |> NUnitConf.checkTestResult

    //:Skip
    let doneSkipped msg =
        //echo Skipped %~f0
        log "Skipped %s" testDir
        //exit /b 0
        Failure (Skipped msg) |> NUnitConf.checkTestResult

    //:Error
    let doneError err msg =
        //echo %ERRORMSG%
        log "%s" msg
        //exit /b %ERRORLEVEL% 
        Failure (Error(err,msg)) |> NUnitConf.checkTestResult

    let tests config p = processor {
        //dir build.ok > NUL ) || (
        //  @echo 'build.ok' not found.
        //  set ERRORMSG=%ERRORMSG% Skipped because 'build.ok' not found.
        //  goto :ERROR
        //)
        if testDir/"build.ok" |> fileExists |> Option.isSome then
            // call %~d0%~p0..\config.bat
            let cfg = config
            // if errorlevel 1 (
            //   set ERRORMSG=%ERRORMSG% config.bat failed;
            //   goto :ERROR
            // )

            // if not exist "%FSC%" (
            //   set ERRORMSG=%ERRORMSG% fsc.exe not found at the location "%FSC%"
            //   goto :ERROR
            // )
            ignore "already checked at test suite startup"

            // if not exist "%FSI%" (
            //   set ERRORMSG=%ERRORMSG% fsi.exe not found at the location "%FSI%"
            //   goto :ERROR
            // )
            ignore "already checked at test suite startup"

            do! singleTestRun' cfg testDir p ()
        }

    let flow p =    
        tests config p
        |> Attempt.Run
        |> function
            | Success () -> doneOK ()
            | Failure (Skipped msg) -> doneSkipped msg
            | Failure (Error (err,msg)) -> doneError err msg

    flow
        