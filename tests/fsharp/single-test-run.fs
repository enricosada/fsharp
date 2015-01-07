module SingleTestRun

open System
open System.IO
open All
open TestConfig
open NUnit.Framework
open PlatformHelpers
     
let singleTestRun' cfg testDir =

    let fullpath path = if Path.IsPathRooted(path) then path else (testDir/path)
    let fileExists = fullpath >> All.fileExists

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

    let loglines = printfn "%s"
    let exec exe args = 
        printfn "%s %s" exe args
        exec' { RedirectOutput = Some loglines; RedirectError = Some loglines; RedirectInput = None; } testDir cfg.EnvironmentVariables exe args

    let execIn input exe args = 
        exec' { RedirectOutput = Some loglines; RedirectError = Some loglines; RedirectInput = Some input; } testDir cfg.EnvironmentVariables exe args

    let clix exe args = exec (testDir/exe) args
    let fsi = Commands.fsi exec cfg.FSI
    let fsiIn flags sources =
        let inputs = sources |> List.map fullpath
        inputs
        |> List.map (fun p -> (p, p |> fileExists))
        |> List.tryPick (function p, None -> Some p | _, Some _ -> None)
        |> function
           | Some p ->
               printfn "redirected file '%s' not found" p
               ErrorLevel -1
           | None -> 
               printf "%s %s" cfg.FSI flags
               inputs |> List.iter (fun p -> printf " < %s " p)
               printfn ""
               Commands.fsiIn execIn cfg.FSI flags inputs

    let fsi_flags = cfg.fsi_flags

    let withTestOkFile = withFileGuard (testDir/"test.ok")

    // :FSI_STDIN
    // @echo do :FSI_STDIN
    let runFSI_STDIN () =
        // if NOT EXIST dont.pipe.to.stdin (
        match fileExists "dont.pipe.to.stdin" with
        | None -> 
            // if exist test.ok (del /f /q test.ok)
            // %CLIX% "%FSI%" %fsi_flags% < %sources% && (
            let run () = fsiIn fsi_flags sources
            // dir test.ok > NUL 2>&1 ) || (
            // @echo FSI_STDIN failed;
            // set ERRORMSG=%ERRORMSG% FSI_STDIN failed;
            // )
            run |> withTestOkFile
        // )
        | Some _ -> Skipped "dont.pipe.to.stdin found"

    // :FSI_STDIN_OPT
    // @echo do :FSI_STDIN_OPT
    let runFSI_STDIN_OPT () =
        // if NOT EXIST dont.pipe.to.stdin (
        match fileExists "dont.pipe.to.stdin" with
        | None -> 
            // if exist test.ok (del /f /q test.ok)
            // %CLIX% "%FSI%" %fsi_flags% --optimize < %sources% && (
            let run () = fsiIn (sprintf "%s --optimize" fsi_flags) sources
            // dir test.ok > NUL 2>&1 ) || (
            // @echo FSI_STDIN_OPT failed
            // set ERRORMSG=%ERRORMSG% FSI_STDIN_OPT failed;
            // )
            run |> withTestOkFile
        // )
        | Some _ -> Skipped "dont.pipe.to.stdin found"

    // :FSI_STDIN_GUI
    // @echo do :FSI_STDIN_GUI
    let runFSI_STDIN_GUI () =
        // if NOT EXIST dont.pipe.to.stdin (
        match fileExists "dont.pipe.to.stdin" with
        | None ->
            // if exist test.ok (del /f /q test.ok)
            // %CLIX% "%FSI%" %fsi_flags% --gui < %sources% && (
            let run () = fsiIn (sprintf "%s --gui" fsi_flags) sources
            // dir test.ok > NUL 2>&1 ) || (
            // @echo FSI_STDIN_GUI failed;
            // set ERRORMSG=%ERRORMSG% FSI_STDIN_GUI failed;
            // )
            run |> withTestOkFile
        // )
        | Some _ -> Skipped "dont.pipe.to.stdin found"

    // :FSI_FILE
    // @echo do :FSI_FILE
    let runFSI_FILE () =
        // if NOT EXIST dont.run.as.script (
        match fileExists "dont.run.as.script" with
        | None -> 
            // if exist test.ok (del /f /q test.ok)
            // %CLIX% "%FSI%" %fsi_flags% %sources% && (
            let run () = fsi fsi_flags sources
            // dir test.ok > NUL 2>&1 ) || (
            // @echo FSI_FILE failed
            // set ERRORMSG=%ERRORMSG% FSI_FILE failed;
            // )
            run |> withTestOkFile
        // )
        | Some _ -> Skipped "dont.run.as.script found"

    // :FSC_BASIC
    // @echo do :FSC_BASIC
    let runFSC_BASIC () =
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\test.exe && (
        let run () = clix "test.exe" ""
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSC_BASIC failed
        // set ERRORMSG=%ERRORMSG% FSC_BASIC failed;
        // )
        run |> withTestOkFile

    // :FSC_BASIC_64
    // @echo do :FSC_BASIC_64
    let runFSC_BASIC_64 () =
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\testX64.exe && (
        let run () = clix "testX64.exe" ""
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSC_BASIC_64 failed
        // set ERRORMSG=%ERRORMSG% FSC_BASIC_64 failed;
        // )
        run |> withTestOkFile

    // :FSC_HW
    // @echo do :FSC_HW
    let runFSC_HW () =
        // if exist test-hw.* (
        if Directory.EnumerateFiles(testDir, "test-hw.*") |> Seq.tryPick fileExists |> Option.isSome then (
            // if exist test.ok (del /f /q test.ok)
            // %CLIX% .\test-hw.exe && (
            let run () = clix "test-hw.exe" ""
            // dir test.ok > NUL 2>&1 ) || (
            // @echo  :FSC_HW failed
            // set ERRORMSG=%ERRORMSG% FSC_HW failed;
            // )
            run |> withTestOkFile
        //)
        )
        else Skipped "not found test-hw.*"

    // :FSC_O3
    // @echo do :FSC_O3
    let runFSC_O3 () =
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\test--optimize.exe && (
        let run () = clix "test--optimize.exe" ""
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSC_O3 failed
        // set ERRORMSG=%ERRORMSG% FSC_03 failed;
        // )
        run |> withTestOkFile

    // :FSC_OPT_MINUS_DEBUG
    // @echo do :FSC_OPT_MINUS_DEBUG
    let runFSC_OPT_MINUS_DEBUG () =
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\test--optminus--debug.exe && (
        let run () = clix "test--optminus--debug.exe" ""
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSC_OPT_MINUS_DEBUG failed
        // set ERRORMSG=%ERRORMSG% FSC_OPT_MINUS_DEBUG failed;
        // )
        run |> withTestOkFile

    // :FSC_OPT_PLUS_DEBUG
    // @echo do :FSC_OPT_PLUS_DEBUG
    let runFSC_OPT_PLUS_DEBUG () =
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\test--optplus--debug.exe && (
        let run () = clix "test--optplus--debug.exe" ""
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSC_OPT_PLUS_DEBUG failed
        // set ERRORMSG=%ERRORMSG% FSC_OPT_PLUS_DEBUG failed;
        // )
        run |> withTestOkFile

    // :GENERATED_SIGNATURE
    // @echo do :GENERATED_SIGNATURE
    let runGENERATED_SIGNATURE () =
        // if NOT EXIST dont.use.generated.signature (
        match "dont.use.generated.signature" |> fileExists with
        | None -> 
            // if exist test.ml (
            match "test.ml" |> fileExists with
            | Some  _ ->
                // if exist test.ok (del /f /q test.ok)
                // %CLIX% tmptest1.exe && (
                let run () = clix "tmptest1.exe" ""
                // dir test.ok > NUL 2>&1 ) || (
                // @echo :GENERATED_SIGNATURE failed
                // set ERRORMSG=%ERRORMSG% FSC_GENERATED_SIGNATURE failed;
                // )
                run |> withTestOkFile
            // )
            | None -> Skipped "not found test.ml"
        //)
        | Some _ -> Skipped "dont.use.generated.signature found"

    // :EMPTY_SIGNATURE
    // @echo do :EMPTY_SIGNATURE
    let runEMPTY_SIGNATURE () =
        // if NOT EXIST dont.use.empty.signature (
        match "dont.use.empty.signature" |> fileExists with
        | None -> 
            // if exist test.ml (
            match "test.ml" |> fileExists with
            | Some  _ ->
                // if exist test.ok (del /f /q test.ok)
                // %CLIX% tmptest2.exe && (
                let run () = clix "tmptest2.exe" ""
                // dir test.ok > NUL 2>&1 ) || (
                // @echo :EMPTY_SIGNATURE failed
                // set ERRORMSG=%ERRORMSG% FSC_EMPTY_SIGNATURE failed;
                // )
                run |> withTestOkFile
            // )
            | None -> Skipped "test.ml not found"
        //)
        | Some _ -> Skipped "dont.use.empty.signature found"

    // :EMPTY_SIGNATURE_OPT
    // @echo do :EMPTY_SIGNATURE_OPT
    let runEMPTY_SIGNATURE_OPT () =
        // if NOT EXIST dont.use.empty.signature (
        match "dont.use.empty.signature" |> fileExists with
        | None -> 
            // if exist test.ml (
            match "test.ml" |> fileExists with
            | Some  _ ->
                // if exist test.ok (del /f /q test.ok)
                // %CLIX% tmptest2--optimize.exe && (
                let run () = clix "tmptest2--optimize.exe" ""
                // dir test.ok > NUL 2>&1 ) || (
                // @echo :EMPTY_SIGNATURE_OPT --optimize failed
                // set ERRORMSG=%ERRORMSG% EMPTY_SIGNATURE_OPT --optimize failed;
                // )
                run |> withTestOkFile
            // )
            | None -> Skipped "test.ml not found"
        //)
        | Some _ -> Skipped "dont.use.empty.signature found"

    // :FRENCH
    // @echo do :FRENCH
    let runFRENCH () =
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\test.exe fr-FR && (
        let run () = clix "test.exe" "fr-FR"
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FRENCH failed
        // set ERRORMSG=%ERRORMSG% FRENCH failed;
        // )
        run |> withTestOkFile

    // :SPANISH
    // @echo do :SPANISH
    let runSPANISH () =
        // if exist test.ok (del /f /q test.ok)
        // %CLIX% .\test.exe es-ES && (
        let run () = clix "test.exe" "es-ES"
        // dir test.ok > NUL 2>&1 ) || (
        // @echo :SPANISH failed
        // set ERRORMSG=%ERRORMSG% SPANISH failed;
        // )
        run |> withTestOkFile

    // :AS_DLL
    // @echo do :AS_DLL
    let runAS_DLL () =
        //if NOT EXIST dont.compile.test.as.dll (
        match "dont.compile.test.as.dll" |> fileExists with
        | None ->
            // if exist test.ok (del /f /q test.ok)
            // %CLIX% .\test--optimize-client-of-lib.exe && (
            let run () = clix "test--optimize-client-of-lib.exe" ""
            // dir test.ok > NUL 2>&1 ) || (
            // @echo :AS_DLL failed
            // set ERRORMSG=%ERRORMSG% AS_DLL failed;
            // )
            run |> withTestOkFile
        //)
        | Some _ -> Skipped "dont.compile.test.as.dll found"

    // :WRAPPER_NAMESPACE
    // @echo do :WRAPPER_NAMESPACE
    let runWRAPPER_NAMESPACE () =
        // if NOT EXIST dont.use.wrapper.namespace (
        match "dont.use.wrapper.namespace" |> fileExists with
        | None ->
            // if exist test.ml (
            match "test.ml" |> fileExists with
            | Some _ ->
                // if exist test.ok (del /f /q test.ok)
                // %CLIX% .\tmptest3.exe && (
                let run () = clix "tmptest3.exe" ""
                // dir test.ok > NUL 2>&1 ) || (
                // @echo :WRAPPER_NAMESPACE failed
                // set ERRORMSG=%ERRORMSG% WRAPPER_NAMESPACE failed;
                // )
                run |> withTestOkFile
            // )
            | None -> Skipped "test.ml not found"
        //)
        | Some _ -> Skipped "dont.use.wrapper.namespace found"

    // :WRAPPER_NAMESPACE_OPT
    // @echo do :WRAPPER_NAMESPACE_OPT
    let runWRAPPER_NAMESPACE_OPT () =
        // if NOT EXIST dont.use.wrapper.namespace (
        match "dont.use.wrapper.namespace" |> fileExists with
        | None ->
            // if exist test.ml (
            match "test.ml" |> fileExists with
            | Some _ ->
                // if exist test.ok (del /f /q test.ok)
                // %CLIX% .\tmptest3--optimize.exe && (
                let run () = clix "tmptest3--optimize.exe" ""
                // dir test.ok > NUL 2>&1 ) || (
                // @echo :WRAPPER_NAMESPACE_OPT failed
                // set ERRORMSG=%ERRORMSG% WRAPPER_NAMESPACE_OPT failed;
                // )
                run |> withTestOkFile
            // )
            | None -> Skipped "test.ml not found"
        // )
        | Some _ -> Skipped "dont.use.wrapper.namespace found"

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
        echo "Ran fsharp %s ok." testDir
        //exit /b 0
        ()

    //:Skip
    let doneSkipped msg =
        //echo Skipped %~f0
        echo "Skipped %s" testDir
        //exit /b 0
        Assert.Ignore (sprintf "skipped. Reason: %s" msg)

    //:Error
    let doneError err msg =
        //echo %ERRORMSG%
        echo "%s" msg
        //exit /b %ERRORLEVEL% 
        Assert.Fail (sprintf "ERRORLEVEL %i %s" err msg)

    let tests config p =
        //dir build.ok > NUL ) || (
        //  @echo 'build.ok' not found.
        //  set ERRORMSG=%ERRORMSG% Skipped because 'build.ok' not found.
        //  goto :ERROR
        //)
        match testDir/"build.ok" |> fileExists with
        | None -> Error (-1,"Skipped because 'build.ok' not found.")
        | Some _ ->
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

            singleTestRun' cfg testDir p ()

    let checkRun = function
        | OK -> doneOK ()
        | Skipped msg -> doneSkipped msg
        | Error (err,msg) -> doneError err msg
    
    (fun p -> tests config p |> checkRun)
        