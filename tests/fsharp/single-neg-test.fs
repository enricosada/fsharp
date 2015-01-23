﻿module SingleNegTest

open System
open System.IO
open TestConfig
open NUnit.Framework
open PlatformHelpers
open NUnitConf

let internal singleNegTest' (cfg: TestConfig) workDir testname = processor {

    // @if "%_echo%"=="" echo off
    ignore "useless"

    // setlocal
    ignore "useless"

    // set ERRORMSG=

    // call %~d0%~p0..\config.bat
    ignore "from arguments"

    // if errorlevel 1 (
    //     set ERRORMSG=%ERRORMSG% config.bat failed;
    //     goto :ERROR
    // )
    ignore "already checked"

    let fsdiff a = 
        let exec p = Command.exec workDir cfg.EnvironmentVariables { Output = Inherit; Input = None } p >> checkResult
        Commands.fsdiff exec cfg.FSDIFF true a
    let envOrFail key =
        cfg.EnvironmentVariables 
        |> Map.tryFind key 
        |> function Some x -> (fun () -> Success x) | None -> NUnitConf.genericError (sprintf "environment variable '%s' required " key)
    let fullpath path = if Path.IsPathRooted(path) then path else (workDir/path)
    let fileExists = fullpath >> fileExists
    let fsc_flags = cfg.fsc_flags

    // if not exist "%FSC%" (
    //   set ERRORMSG=Could not find FSC at path "%FSC%"
    //   goto :ERROR
    // )
    ignore "already checked"

    // set testname=%1
    ignore "from arguments"

    // REM == Set baseline (fsc vs vs, in case the vs baseline exists)
    let BSLFILE = 
        // IF     EXIST %testname%.vsbsl (set BSLFILE=%testname%.vsbsl)
        // IF NOT EXIST %testname%.vsbsl (set BSLFILE=%testname%.bsl)
        match (workDir/(sprintf "%s.vsbsl" testname)) |> fileExists with
        | Some p -> (sprintf "%s.vsbsl" testname)
        | None -> (sprintf "%s.bsl" testname)

    // %FSDIFF% %~f0 %~f0
    // @if ERRORLEVEL 1 (
    //     set ERRORMSG=%ERRORMSG% FSDIFF likely not found;
    //     goto Error
    // )
    do! fsdiff BSLFILE BSLFILE

    // set sources=
    // if exist "%testname%.mli" (set sources=%sources% %testname%.mli)
    // if exist "%testname%.fsi" (set sources=%sources% %testname%.fsi)
    // if exist "%testname%.ml" (set sources=%sources% %testname%.ml)
    // if exist "%testname%.fs" (set sources=%sources% %testname%.fs)
    // if exist "%testname%.fsx" (set sources=%sources% %testname%.fsx)
    // if exist "%testname%a.mli" (set sources=%sources% %testname%a.mli)
    // if exist "%testname%a.fsi" (set sources=%sources% %testname%a.fsi)
    // if exist "%testname%a.ml" (set sources=%sources% %testname%a.ml)
    // if exist "%testname%a.fs" (set sources=%sources% %testname%a.fs)
    // if exist "%testname%b.mli" (set sources=%sources% %testname%b.mli)
    // if exist "%testname%b.fsi" (set sources=%sources% %testname%b.fsi)
    // if exist "%testname%b.ml" (set sources=%sources% %testname%b.ml)
    // if exist "%testname%b.fs" (set sources=%sources% %testname%b.fs)
    let sources = [
        let src = 
            [ testname + ".mli"; testname + ".fsi"; testname + ".ml"; testname + ".fs"; testname +  ".fsx"; 
              testname + "a.mli"; testname + "a.fsi"; testname + "a.ml"; testname + "a.fs";
              testname + "b.mli"; testname + "b.fsi"; testname + "b.ml"; testname + "b.fs" ]

        yield! src |> List.filter (fileExists >> Option.isSome)
    
        // if exist "helloWorldProvider.dll" (set sources=%sources% -r:helloWorldProvider.dll)
        if "helloWorldProvider.dll" |> fileExists |> Option.isSome
        then yield "-r:helloWorldProvider.dll"
        ]

    // REM check negative tests for bootstrapped fsc.exe due to line-ending differences
    // if "%FSC:fscp=X%" == "%FSC%" ( 
    if cfg.FSC.Contains("fscp")
    then return! NUnitConf.skip "bootstrapped fsc.exe due to line-ending differences"
    else
        // echo Negative typechecker testing: %testname%
        log "Negative typechecker testing: %s" testname

        let fsc' = 
            // "%FSC%" %fsc_flags% --vserrors --warnaserror --nologo --maxerrors:10000 -a -o:%testname%.dll  %sources% 2> %testname%.err
            // @if NOT ERRORLEVEL 1 (
            //     set ERRORMSG=%ERRORMSG% FSC passed unexpectedly for  %sources%;
            //     goto SetError
            // )
            let ``exec 2>`` errPath = Command.exec workDir cfg.EnvironmentVariables { Output = Error(Overwrite(errPath |> fullpath)); Input = None }
            let checkErrorLevel1 = function 
                | CmdResult.ErrorLevel 1 -> Success
                | CmdResult.Success | CmdResult.ErrorLevel _ -> NUnitConf.genericError (sprintf "FSC passed unexpectedly for  %A" sources)

            Printf.ksprintf (fun flags sources errPath -> Commands.fsc (``exec 2>`` errPath) cfg.FSC flags sources |> checkErrorLevel1)
        
        let fsdiff a b = processor {
            let out = new ResizeArray<string>()
            let redirectOutputToFile path args =
                log "%s %s" path args
                let toLog = redirectToLog ()
                Process.exec { RedirectOutput = Some (function null -> () | s -> out.Add(s)); RedirectError = Some toLog.Post; RedirectInput = None; } workDir cfg.EnvironmentVariables path args
            do! (Commands.fsdiff redirectOutputToFile cfg.FSDIFF true a b) |> checkResult
            return out.ToArray() |> List.ofArray
            }

        // "%FSC%" %fsc_flags% --vserrors --warnaserror --nologo --maxerrors:10000 -a -o:%testname%.dll  %sources% 2> %testname%.err
        do! fsc' """%s --vserrors --warnaserror --nologo --maxerrors:10000 -a -o:%s.dll""" fsc_flags testname sources (sprintf "%s.err" testname)

        // %FSDIFF% %testname%.err %testname%.bsl > %testname%.diff
        let! testnameDiff = fsdiff (sprintf "%s.err" testname) (sprintf "%s.bsl" testname)

        // for /f %%c IN (%testname%.diff) do (
        do! match testnameDiff with
            | [] -> Success
            | l ->
                // echo ***** %testname%.err %testname%.bsl differed: a bug or baseline may neeed updating
                log "***** %s.err %s.bsl differed: a bug or baseline may neeed updating" testname testname
                // set ERRORMSG=%ERRORMSG% %testname%.err %testname%.bsl differ;
                NUnitConf.genericError (sprintf "%s.err %s.bsl differ; %A" testname testname l)

        // echo Good, output %testname%.err matched %testname%.bsl
        log "Good, output %s.err matched %s.bsl" testname testname

        // "%FSC%" %fsc_flags% --test:ContinueAfterParseFailure --vserrors --warnaserror --nologo --maxerrors:10000 -a -o:%testname%.dll  %sources% 2> %testname%.vserr
        do! fsc' "%s --test:ContinueAfterParseFailure --vserrors --warnaserror --nologo --maxerrors:10000 -a -o:%s.dll" fsc_flags testname sources (sprintf "%s.vserr" testname)

        // @if NOT ERRORLEVEL 1 (
        //     set ERRORMSG=%ERRORMSG% FSC passed unexpectedly for  %sources%;
        //     goto SetError
        // )
        // 
        // %FSDIFF% %testname%.vserr %BSLFILE% > %testname%.vsdiff
        // 
        // for /f %%c IN (%testname%.vsdiff) do (
        //     echo ***** %testname%.vserr %BSLFILE% differed: a bug or baseline may neeed updating
        //     set ERRORMSG=%ERRORMSG% %testname%.vserr %BSLFILE% differ;
        //     IF DEFINED WINDIFF  (start %windiff% %BSLFILE%  %testname%.vserr)
        //     goto SetError
        // )
        // echo Good, output %testname%.vserr matched %BSLFILE%
    // )
    }

let singleNegTest =

    // :Ok
    let doneOK x =
        // echo Ran fsharp %~f0 ok.
        log "Ran fsharp %%~f0 ok"
        // endlocal
        // exit /b 0
        // goto :EOF
        Success x

    // :Skip
    let doneSkipped msg x =
        // echo Skipped %~f0
        log "Skipped %s" msg
        // endlocal
        // exit /b 0
        // goto :EOF
        Success x

    // :Error
    let doneError err msg =
        // echo %ERRORMSG%
        log "%s" msg
        // exit /b %ERRORLEVEL% 
        // goto :EOF
        Failure (err)

    // :SETERROR
    // set NonexistentErrorLevel 2> nul
    // goto Error
    // goto :EOF

    let flow cfg workDir testname () =    
        singleNegTest' cfg workDir testname
        |> Attempt.Run
        |> function
           | Success () -> doneOK ()
           | Failure (Skipped msg) -> doneSkipped msg ()
           | Failure (GenericError msg) -> doneError (GenericError msg) msg
           | Failure (ProcessExecError (err,msg)) -> doneError (ProcessExecError(err,msg)) msg
    flow


