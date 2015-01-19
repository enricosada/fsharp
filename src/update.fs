module UpdateCmd

open System.IO

open PlatformHelpers
open Commands
open NUnit.Framework

type Configuration = DEBUG | RELEASE
    with override this.ToString() = match this with DEBUG -> "Debug" | RELEASE -> "Release"

type updateCmdArgs = { Configuration: Configuration; Ngen: bool; }

let updateCmd envVars args = processor {
    // @echo off
    // setlocal
    ignore "useless"

    // if /i "%1" == "debug" goto :ok
    // if /i "%1" == "release" goto :ok
    ignore "already validated input"

    // echo GACs built binaries, adds required strong name verification skipping, and optionally NGens built binaries
    // echo Usage:
    // echo    update.cmd debug [-ngen]
    // echo    update.cmd release [-ngen]
    // exit /b 1
    ignore "useless help"

    //:ok
    let env k () = match envVars |> Map.tryFind k with None -> Failure (sprintf "environment variable '%s' not found" k) | Some x -> Success x
    let ``~dp0`` = __SOURCE_DIRECTORY__
    let exec exe args = 
        log "%s %s" exe args
        use toLog = redirectToLog ()
        Process.exec { RedirectError = Some toLog.Post; RedirectOutput = Some toLog.Post; RedirectInput = None } ``~dp0`` envVars exe args

    // set BINDIR=%~dp0..\%1\net40\bin
    let! BINDIR = env "FSCBinPath"

    let! PROCESSOR_ARCHITECTURE = processor {
        let! pa = env "PROCESSOR_ARCHITECTURE" 
        return pa |> parseProcessorArchitecture }

    // if /i "%PROCESSOR_ARCHITECTURE%"=="x86" set X86_PROGRAMFILES=%ProgramFiles%
    // if /I "%PROCESSOR_ARCHITECTURE%"=="AMD64" set X86_PROGRAMFILES=%ProgramFiles(x86)%
    let! X86_PROGRAMFILES =
        match PROCESSOR_ARCHITECTURE with
        | X86 -> env "ProgramFiles"
        | AMD64 -> env "ProgramFiles(x86)"
        | arc -> (fun () -> Failure (sprintf "unsupported PROCESSOR_ARCHITECTURE %O" arc))

    let! windir = env "windir"

    // set GACUTIL="%X86_PROGRAMFILES%\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\gacutil.exe"
    let GACUTIL = X86_PROGRAMFILES/"Microsoft SDKs"/"Windows"/"v8.0A"/"bin"/"NETFX 4.0 Tools"/"gacutil.exe"
    // set SN32="%X86_PROGRAMFILES%\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\sn.exe"
    let SN32 = X86_PROGRAMFILES/"Microsoft SDKs"/"Windows"/"v8.0A"/"bin"/"NETFX 4.0 Tools"/"sn.exe"
    // set SN64="%X86_PROGRAMFILES%\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\x64\sn.exe"
    let SN64 = X86_PROGRAMFILES/"Microsoft SDKs"/"Windows"/"v8.0A"/"bin"/"NETFX 4.0 Tools"/"x64"/"sn.exe"
    // set NGEN32=%windir%\Microsoft.NET\Framework\v4.0.30319\ngen.exe
    let NGEN32 = windir/"Microsoft.NET"/"Framework"/"v4.0.30319"/"ngen.exe"
    // set NGEN64=%windir%\Microsoft.NET\Framework64\v4.0.30319\ngen.exe
    let NGEN64 = windir/"Microsoft.NET"/"Framework64"/"v4.0.30319"/"ngen.exe"

    let checkResult = function CmdResult.ErrorLevel err -> Failure (sprintf "ERRORLEVEL %d" err) | CmdResult.Success -> Success ()

    let gacutil flags = gacutil exec GACUTIL flags >> checkResult
    let ngen32 = ngen exec NGEN32 >> checkResult
    let ngen64 = ngen exec NGEN64 >> checkResult
    let sn32 = exec SN32 >> checkResult
    let sn64 = exec SN32 >> checkResult

    // rem Disable strong-name validation for F# binaries built from open source that are signed with the microsoft key
    // %SN32% -Vr FSharp.Core,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.Build,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.Compiler.Interactive.Settings,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.Compiler.Hosted,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.Compiler,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.Compiler.Server.Shared,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.Editor,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.LanguageService,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.LanguageService.Base,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.LanguageService.Compiler,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.ProjectSystem.Base,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.ProjectSystem.FSharp,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.ProjectSystem.PropertyPages,b03f5f7f11d50a3a
    // %SN32% -Vr FSharp.VS.FSI,b03f5f7f11d50a3a
    // %SN32% -Vr Unittests,b03f5f7f11d50a3a
    // %SN32% -Vr Salsa,b03f5f7f11d50a3a

    let strongName (snExe: string -> Result<_,_>) = processor {
        let all = 
            [ "FSharp.Core";
            "FSharp.Build";
            "FSharp.Compiler.Interactive.Settings";"FSharp.Compiler.Hosted";
            "FSharp.Compiler";"FSharp.Compiler.Server.Shared";
            "FSharp.Editor";
            "FSharp.LanguageService";"FSharp.LanguageService.Base";"FSharp.LanguageService.Compiler";
            "FSharp.ProjectSystem.Base";"FSharp.ProjectSystem.FSharp";"FSharp.ProjectSystem.PropertyPages";
            "FSharp.VS.FSI";
            "Unittests";
            "Salsa" ]
        for a in all do
            do! snExe (sprintf " -Vr %s,b03f5f7f11d50a3a" a) 
        }

    do! strongName sn32
        
    //if /i "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    do! if PROCESSOR_ARCHITECTURE = AMD64 then
            //  %SN64% -Vr FSharp.Core,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.Build,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.Compiler.Interactive.Settings,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.Compiler.Hosted,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.Compiler,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.Compiler.Server.Shared,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.Editor,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.LanguageService,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.LanguageService.Base,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.LanguageService.Compiler,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.ProjectSystem.Base,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.ProjectSystem.FSharp,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.ProjectSystem.PropertyPages,b03f5f7f11d50a3a
            //  %SN64% -Vr FSharp.VS.FSI,b03f5f7f11d50a3a
            //  %SN64% -Vr Unittests,b03f5f7f11d50a3a
            //  %SN64% -Vr Salsa,b03f5f7f11d50a3a
            strongName sn64
        else 
            (fun () -> Success ())
    //)

    // rem Only GACing FSharp.Core for now
    // %GACUTIL% /if %BINDIR%\FSharp.Core.dll
    do! gacutil "/if" (BINDIR/"FSharp.Core.dll")

    // rem NGen fsc, fsi, fsiAnyCpu, and FSharp.Build.dll
    // if /i not "%2"=="-ngen" goto :donengen

    if args.Ngen then
        // "%NGEN32%" install "%BINDIR%\fsc.exe" /queue:1
        // "%NGEN32%" install "%BINDIR%\fsi.exe" /queue:1
        // "%NGEN32%" install "%BINDIR%\FSharp.Build.dll" /queue:1
        // "%NGEN32%" executeQueuedItems 1
        do! ngen32 [BINDIR/"fsc.exe"; BINDIR/"fsi.exe"; BINDIR/"FSharp.Build.dll"]

        // if /i "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
        if PROCESSOR_ARCHITECTURE = AMD64 then
            // "%NGEN64%" install "%BINDIR%\fsiAnyCpu.exe" /queue:1
            // "%NGEN64%" install "%BINDIR%\FSharp.Build.dll" /queue:1
            // "%NGEN64%" executeQueuedItems 1
            do! ngen64 [BINDIR/"fsiAnyCpu.exe"; BINDIR/"FSharp.Build.dll"]
        // )
    //:donengen
    
    }