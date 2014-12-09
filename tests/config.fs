module TestConfig

open System
open System.IO
open All

open Microsoft.Win32
open System.Collections.Generic

let GetSdk81Path sdkIdent =
    let regPath = Path.Combine(@"SOFTWARE\Microsoft\Microsoft SDKs\Windows\v8.1A\", sdkIdent)
    use baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
    use regKey  = baseKey.OpenSubKey(regPath, false)
   
    if (regKey = null) then None
    else 
        match regKey.GetValue("InstallationFolder") with
        | null -> None
        | :? string as v -> Some v
        | x -> failwithf "unexpected '%A' " x 

type FSLibPaths = {
    FSCOREDLLPATH: string;
    FSCOREDLL20PATH: string;
    FSCOREDLLPORTABLEPATH: string;
    FSCOREDLLNETCOREPATH: string;
    FSCOREDLLNETCORE78PATH: string;
    FSCOREDLLNETCORE259PATH: string;
    FSDATATPPATH: string;
}

let inline (/) a b = Path.Combine(a,b)

// REM ===
// REM === Find paths to shipped F# libraries referenced by clients
// REM ===
let GetFSLibPaths env OSARCH FSCBinPath =
    // REM == Find out OS architecture, no matter what cmd prompt
    // SET OSARCH=%PROCESSOR_ARCHITECTURE%
    // IF NOT "%PROCESSOR_ARCHITEW6432%"=="" SET OSARCH=%PROCESSOR_ARCHITEW6432%
    ignore (OSARCH, "already define")

    // REM == Find out path to native 'Program Files 32bit', no matter what
    // REM == architecture we are running on and no matter what command
    // REM == prompt we came from.
    // IF /I "%OSARCH%"=="x86"   set X86_PROGRAMFILES=%ProgramFiles%
    // IF /I "%OSARCH%"=="IA64"  set X86_PROGRAMFILES=%ProgramFiles(x86)%
    // IF /I "%OSARCH%"=="AMD64" set X86_PROGRAMFILES=%ProgramFiles(x86)%
    let X86_PROGRAMFILES =
        match OSARCH with
        | X86 -> env |> Map.find "ProgramFiles"
        | IA64 -> env |> Map.find "ProgramFiles(x86)"
        | AMD64 -> env |> Map.find "ProgramFiles(x86)"
        | Unknown os -> failwithf "OSARCH '%s' not supported" os

    // REM == Default VS install locations
    let mutable libs = {
        // set FSCOREDLLPATH=%X86_PROGRAMFILES%\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.1.0
        FSCOREDLLPATH = X86_PROGRAMFILES / @"Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.1.0"
        // set FSCOREDLL20PATH=%X86_PROGRAMFILES%\Reference Assemblies\Microsoft\FSharp\.NETFramework\v2.0\2.3.0.0
        FSCOREDLL20PATH = X86_PROGRAMFILES / @"Reference Assemblies\Microsoft\FSharp\.NETFramework\v2.0\2.3.0.0"
        // set FSCOREDLLPORTABLEPATH=%X86_PROGRAMFILES%\Reference Assemblies\Microsoft\FSharp\.NETPortable\2.3.5.1
        FSCOREDLLPORTABLEPATH = X86_PROGRAMFILES / @"Reference Assemblies\Microsoft\FSharp\.NETPortable\2.3.5.1"
        // set FSCOREDLLNETCOREPATH=%X86_PROGRAMFILES%\Reference Assemblies\Microsoft\FSharp\.NETCore\3.3.1.0
        FSCOREDLLNETCOREPATH = X86_PROGRAMFILES / @"Reference Assemblies\Microsoft\FSharp\.NETCore\3.3.1.0"
        // set FSCOREDLLNETCORE78PATH=%X86_PROGRAMFILES%\Reference Assemblies\Microsoft\FSharp\.NETCore\3.78.3.1
        FSCOREDLLNETCORE78PATH = X86_PROGRAMFILES / @"Reference Assemblies\Microsoft\FSharp\.NETCore\3.78.3.1"
        // set FSCOREDLLNETCORE259PATH=%X86_PROGRAMFILES%\Reference Assemblies\Microsoft\FSharp\.NETCore\3.259.3.1
        FSCOREDLLNETCORE259PATH = X86_PROGRAMFILES / @"Reference Assemblies\Microsoft\FSharp\.NETCore\3.259.3.1"
        // set FSDATATPPATH=%X86_PROGRAMFILES%\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.0.0\Type Providers
        FSDATATPPATH =  X86_PROGRAMFILES / @"Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.0.0\Type Providers"
    }
    let libsRef = ref libs

    let ifExistDllSet relativePath dll found  =
        FSCBinPath |> Option.bind (fun bindir -> let dir = bindir/relativePath in dir/dll |> fileExists |> Option.map (fun _ -> dir)) |> Option.iter found

    // REM == Check if using open build instead
    // IF EXIST "%FSCBinPath%\FSharp.Core.dll" set FSCOREDLLPATH=%FSCBinPath%
    ifExistDllSet "" "FSharp.Core.dll" (fun dir -> libsRef := {!libsRef with FSCOREDLLPATH = dir})
    // IF EXIST "%FSCBinPath%\..\..\net20\bin\FSharp.Core.dll" set FSCOREDLL20PATH=%FSCBinPath%\..\..\net20\bin
    ifExistDllSet @"..\..\net20\bin" "FSharp.Core.dll" (fun dir -> libsRef := {!libsRef with FSCOREDLL20PATH = dir})
    // IF EXIST "%FSCBinPath%\..\..\portable47\bin\FSharp.Core.dll" set FSCOREDLLPORTABLEPATH=%FSCBinPath%\..\..\portable47\bin
    ifExistDllSet @"..\..\portable47\bin" "FSharp.Core.dll" (fun dir -> libsRef := {!libsRef with FSCOREDLLPORTABLEPATH = dir})
    // IF EXIST "%FSCBinPath%\..\..\portable7\bin\FSharp.Core.dll" set FSCOREDLLNETCOREPATH=%FSCBinPath%\..\..\portable7\bin
    ifExistDllSet @"..\..\portable7\bin" "FSharp.Core.dll" (fun dir -> libsRef := {!libsRef with FSCOREDLLNETCOREPATH = dir})
    // IF EXIST "%FSCBinPath%\..\..\portable78\bin\FSharp.Core.dll" set FSCOREDLLNETCORE78PATH=%FSCBinPath%\..\..\portable78\bin
    ifExistDllSet @"..\..\portable78\bin" "FSharp.Core.dll" (fun dir -> libsRef := {!libsRef with FSCOREDLLNETCORE78PATH = dir})
    // IF EXIST "%FSCBinPath%\..\..\portable259\bin\FSharp.Core.dll" set FSCOREDLLNETCORE259PATH=%FSCBinPath%\..\..\portable259\bin
    ifExistDllSet @"..\..\portable259\bin" "FSharp.Core.dll" (fun dir -> libsRef := {!libsRef with FSCOREDLLNETCORE259PATH = dir})
    // IF EXIST "%FSCBinPath%\FSharp.Data.TypeProviders.dll" set FSDATATPPATH=%FSCBinPath%
    ifExistDllSet "FSharp.Data.TypeProviders.dll" "" (fun dir -> libsRef := {!libsRef with FSDATATPPATH = dir})

    // set FSCOREDLLPATH=%FSCOREDLLPATH%\FSharp.Core.dll
    libs <- {libs with FSCOREDLLPATH = libs.FSCOREDLLPATH/"FSharp.Core.dll"}
    // set FSCOREDLL20PATH=%FSCOREDLL20PATH%\FSharp.Core.dll
    libs <- {libs with FSCOREDLL20PATH = libs.FSCOREDLL20PATH/"FSharp.Core.dll"}
    // set FSCOREDLLPORTABLEPATH=%FSCOREDLLPORTABLEPATH%\FSharp.Core.dll
    libs <- {libs with FSCOREDLLPORTABLEPATH = libs.FSCOREDLLPORTABLEPATH/"FSharp.Core.dll"}
    // set FSCOREDLLNETCOREPATH=%FSCOREDLLNETCOREPATH%\FSharp.Core.dll
    libs <- {libs with FSCOREDLLNETCOREPATH = libs.FSCOREDLLNETCOREPATH/"FSharp.Core.dll"}
    // set FSCOREDLLNETCORE78PATH=%FSCOREDLLNETCORE78PATH%\FSharp.Core.dll
    libs <- {libs with FSCOREDLLNETCORE78PATH = libs.FSCOREDLLNETCORE78PATH/"FSharp.Core.dll"}
    // set FSCOREDLLNETCORE259PATH=%FSCOREDLLNETCORE259PATH%\FSharp.Core.dll
    libs <- {libs with FSCOREDLLNETCORE259PATH = libs.FSCOREDLLNETCORE259PATH/"FSharp.Core.dll"}
    // set FSDATATPPATH=%FSDATATPPATH%\FSharp.Data.TypeProviders.dll
    libs <- {libs with FSDATATPPATH = libs.FSDATATPPATH/"FSharp.Data.TypeProviders.dll"}

    X86_PROGRAMFILES, libs

// REM ===
// REM === Find path to FSC/FSI looking up the registry
// REM === Will set the FSCBinPath env variable.
// REM === This if for Dev11+/NDP4.5
// REM === Works on both XP and Vista and hopefully everything else
// REM === Works on 32bit and 64 bit, no matter what cmd prompt it is invoked from
// REM === 
let SetFSCBinPath45 () =
    // FOR /F "tokens=1-2*" %%a IN ('reg query "%REG_SOFTWARE%\Microsoft\FSharp\3.1\Runtime\v4.0" /ve') DO set FSCBinPath=%%c
    // IF EXIST "%FSCBinPath%" goto :EOF
    // FOR /F "tokens=1-3*" %%a IN ('reg query "%REG_SOFTWARE%\Microsoft\FSharp\3.1\Runtime\v4.0" /ve') DO set FSCBinPath=%%d
    // goto :EOF
    None

let attendedLog envVars X86_PROGRAMFILES CORDIR CORDIR40 =
    let getMsbuildPath =
        // rem first see if we have got msbuild installed
        let MSBuildToolsPath = envVars |> Map.tryFind "MSBuildToolsPath" |> ref

        // if exist "%X86_PROGRAMFILES%\MSBuild\12.0\Bin\MSBuild.exe" SET MSBuildToolsPath=%X86_PROGRAMFILES%\MSBuild\12.0\Bin\
        let dir = X86_PROGRAMFILES/"MSBuild"/"12.0"/"Bin" + "\\" in dir/"MSBuild.exe" |> fileExists |> Option.iter (fun dir -> MSBuildToolsPath := Some dir)
        // if not "%MSBuildToolsPath%" == "" goto done_MsBuildToolsPath
        match !MSBuildToolsPath with
        | Some x -> Some x
        | None ->
            //                        IF NOT "%CORDIR%"=="" IF EXIST "%CORDIR%\msbuild.exe"         SET MSBuildToolsPath=%CORDIR%
            if (not <| (CORDIR = "")) then fileExists (CORDIR/"msbuild.exe") |> Option.iter (fun dir -> MSBuildToolsPath := Some dir)
            // IF     "%CORDIR40%"=="" IF NOT "%CORDIR%"=="" IF EXIST "%CORDIR%\..\V3.5\msbuild.exe" SET MSBuildToolsPath="%CORDIR%\..\V3.5\"
            if (CORDIR40 |> Option.isNone) then
                if (not <| (CORDIR = "")) then
                    let dir = CORDIR/".."/"V3.5" + "\\" in dir/"msbuild.exe" |> fileExists |> Option.iter (fun dir -> MSBuildToolsPath := Some dir)

            // IF NOT "%CORDIR%"=="" FOR /f %%j IN ("%MSBuildToolsPath%") do SET MSBuildToolsPath=%%~fj
            if (not <| (CORDIR = "")) 
            then MSBuildToolsPath := (!MSBuildToolsPath) |> Option.map Path.GetFullPath
            !MSBuildToolsPath
        // :done_MsBuildToolsPath

    // reg query "%REG_SOFTWARE%\Microsoft\VisualStudio\12.0\Setup" | findstr /r /c:"Express .* for Windows Desktop" > NUL
    // if NOT ERRORLEVEL 1 (
    //     set INSTALL_SKU=DESKTOP_EXPRESS
    //     goto :done_SKU
    // )
    // reg query "%REG_SOFTWARE%\Microsoft\VisualStudio\12.0\Setup" | findstr /r /c:"Express .* for Web" > NUL
    // if NOT ERRORLEVEL 1 (
    //     set INSTALL_SKU=WEB_EXPRESS
    //     goto :done_SKU
    // )
    // reg query "%REG_SOFTWARE%\Microsoft\VisualStudio\12.0\Setup" | findstr /r /c:"Ultimate" > NUL
    // if NOT ERRORLEVEL 1 (
    //     set INSTALL_SKU=ULTIMATE
    //     goto :done_SKU
    // )
    // set INSTALL_SKU=CLEAN
    // :done_SKU

    // exit /b 0
    getMsbuildPath, Ultimate


let config envVars =
    // @if "%_echo%"=="" echo off
    ignore "useless"
    // set _SCRIPT_DRIVE=%~d0
    let _SCRIPT_DRIVE = __SOURCE_DIRECTORY__ |> Path.GetPathRoot
    // set _SCRIPT_PATH=%~p0
    ignore "unused"
    // set SCRIPT_ROOT=%_SCRIPT_DRIVE%%_SCRIPT_PATH%
    let SCRIPT_ROOT = __SOURCE_DIRECTORY__ |> Path.GetFullPath

    let env key = envVars |> Map.tryFind key
    let envOrDefault key def = env key |> Option.fold (fun s t -> t) def
    let envOrFail key = env key |> function Some x -> x | None -> failwithf "environment variable '%s' required " key

    // set REG_SOFTWARE=HKLM\SOFTWARE
    // IF /I "%PROCESSOR_ARCHITECTURE%"=="AMD64" (set REG_SOFTWARE=%REG_SOFTWARE%\Wow6432Node)
    let PROCESSOR_ARCHITECTURE = envOrFail "PROCESSOR_ARCHITECTURE" |> parseProcessorArchitecture
    let REG_SOFTWARE = @"HKLM\SOFTWARE" + (match PROCESSOR_ARCHITECTURE with AMD64 -> @"\Wow6432Node" | _ -> "")

    // if not defined FSHARP_HOME set FSHARP_HOME=%SCRIPT_ROOT%..\..
    // for /f %%i in ("%FSHARP_HOME%") do set FSHARP_HOME=%%~fi
    let FSHARP_HOME =
        envOrDefault "FSHARP_HOME" (SCRIPT_ROOT/"..")
        |> Path.GetFullPath

    // REM Do we know where fsc.exe is?
    // IF DEFINED FSCBinPath goto :FSCBinPathFound
    // FOR /F "delims=" %%i IN ('where fsc.exe') DO SET FSCBinPath=%%~dpi
    // :FSCBinPathFound
    let mutable FSCBinPath =
        match env "FSCBinPath" with
        | Some p -> Some p
        | None -> whereCommand "fsc.exe" |> Option.map Path.GetDirectoryName

    // SET CLIFLAVOUR=cli\4.5
    let CLIFLAVOUR = @"cli\4.5"

    // if not exist "%FSCBinPath%\fsc.exe" call :SetFSCBinPath45
    if FSCBinPath |> Option.map (fun dir -> dir/"fsc.exe" |> fileExists) |> Option.isNone
    then FSCBinPath <- SetFSCBinPath45 ()

    // if not exist "%FSCBinPath%\fsc.exe" echo %FSCBinPath%\fsc.exe still not found. Assume that user has added it to path somewhere
    ignore "log"

    // REM add %FSCBinPath% to path only if not already there. Otherwise, the path keeps growing.
    // echo %path%; | find /i "%FSCBinPath%;" > NUL
    // if ERRORLEVEL 1    set PATH=%PATH%;%FSCBinPath%


    // if "%FSDIFF%"=="" set FSDIFF=%SCRIPT_ROOT%fsharpqa\testenv\bin\%processor_architecture%\diff.exe -dew
    let FSDIFF = envOrDefault "FSDIFF" (SCRIPT_ROOT/"fsharpqa"/"testenv"/"bin"/(PROCESSOR_ARCHITECTURE.ToString())/"diff.exe -dew")

    // rem check if we're already configured, if not use the configuration from the last line of the config file
    // if "%fsc%"=="" ( 
    //   set csc_flags=/nologo
    //   set fsiroot=fsi
    // )
    let FSC = env "fsc" |> ref
    let csc_flags = !FSC |> function None -> Some "/nologo" | Some _ -> env "csc_flags"
    let fsiroot = !FSC |> (function None -> Some "fsi" | Some _ -> env "fsiroot") |> ref

    // if not defined ALINK  set ALINK=al.exe
    let ALINK = ref (envOrDefault "ALINK" "al.exe")
    // if not defined CSC    set CSC=csc.exe %csc_flags%
    let CSC = envOrDefault "CSC" (sprintf "csc.exe %s" (csc_flags |> function None -> "" | Some flags -> flags))

    // REM SDK Dependencires.
    // if not defined ILDASM   set ILDASM=ildasm.exe
    let ILDASM = ref (envOrDefault "ILDASM" "ildasm.exe")
    // if not defined GACUTIL   set GACUTIL=gacutil.exe
    let GACUTIL = ref (envOrDefault "GACUTIL" "gacutil.exe")
    // if not defined PEVERIFY set PEVERIFY=peverify.exe
    let PEVERIFY = ref (envOrDefault "PEVERIFY" "peverify.exe")
    // if not defined RESGEN   set RESGEN=resgen.exe
    let RESGEN = ref (envOrDefault "RESGEN" "resgen.exe")

    // if "%fsiroot%" == "" ( set fsiroot=fsi)
    if !fsiroot |> Option.isNone then fsiroot := Some "fsi"

    // REM == Test strategy: if we are on a 32bit OS => use fsi.exe
    // REM ==                if we are on a 64bit OS => use fsiAnyCPU.exe
    // REM == This way we get coverage of both binaries without having to
    // REM == double the test matrix. Note that our nightly automation
    // REM == always cover x86 and x64... so we won't miss much. There
    // REM == is an implicit assumption that the CLR will do it's job
    // REM == to make an FSIAnyCPU.exe behave as FSI.exe on a 32bit OS.
    // REM == On 64 bit machines ensure that we run the 64 bit versions of tests too.

    // SET OSARCH=%PROCESSOR_ARCHITECTURE%
    // IF NOT "%PROCESSOR_ARCHITEW6432%"=="" SET OSARCH=%PROCESSOR_ARCHITEW6432%
    let OSARCH =
        match env "PROCESSOR_ARCHITEW6432" |> Option.map parseProcessorArchitecture with
        | Some arc -> arc
        | None -> PROCESSOR_ARCHITECTURE
         
    // IF "%fsiroot%"=="fsi" IF NOT "%OSARCH%"=="x86" (
    //   SET fsiroot=fsiAnyCPU
    //   set FSC_BASIC_64=FSC_BASIC_64
    // )
    let mutable FSC_BASIC_64 = env "FSC_BASIC_64"
    match !fsiroot, OSARCH with
    | Some "fsi", X86 -> ()
    | Some "fsi", arc ->
        fsiroot := Some "fsiAnyCPU"
        FSC_BASIC_64 <- Some "FSC_BASIC_64"
    | _ -> ()


    // REM ---------------------------------------------------------------
    // REM If we set a "--cli-version" flag anywhere in the flags then assume its v1.x
    // REM and generate a config file, so we end up running the test on the right version
    // REM of the CLR.  Also modify the CORSDK used.
    // REM
    // REM Use CLR 1.1 at a minimum since 1.0 is not installed on most of my machines
    // REM otherwise assume v2.0
    // REM TODO: we need to update this to be v2.0 or v3.5 and nothing else.

    // set fsc_flags=%fsc_flags% 
    let mutable fsc_flags = env "fsc_flags"

    // set CLR_SUPPORTS_GENERICS=true
    let CLR_SUPPORTS_GENERICS = true
    // set ILDASM=%ILDASM%
    ILDASM := !ILDASM
    // set GACUTIL=%GACUTIL%
    GACUTIL := !GACUTIL
    // set CLR_SUPPORTS_WINFORMS=true
    let CLR_SUPPORTS_WINFORMS = true
    // set CLR_SUPPORTS_SYSTEM_WEB=true
    let CLR_SUPPORTS_SYSTEM_WEB = true

    // REM ==
    // REM == F# v1.0 targets NetFx3.5 (i.e. NDP2.0)
    // REM == It is ok to hardcode the location, since this is not going to
    // REM == change ever. Well, if/when we target a different runtime we'll have
    // REM == to come and update this, but for now we MUST make sure we use the 2.0 stuff.
    // REM ==
    // REM == If we run on a 64bit machine (from a 64bit command prompt!), we use the 64bit
    // REM == CLR, but tweaking 'Framework' to 'Framework64'.
    // REM ==
    // set CORDIR=%SystemRoot%\Microsoft.NET\Framework\v2.0.50727\
    let SystemRoot = envOrFail "SystemRoot"
    let CORDIR = ref (SystemRoot/"Microsoft.NET"/"Framework"/"v2.0.50727" + "\\")
    // set CORDIR40=
    // FOR /D %%i IN (%windir%\Microsoft.NET\Framework\v4.0.?????) do set CORDIR40=%%i
    let windir = envOrFail "windir"
    let CORDIR40 =
        let d = windir/"Microsoft.NET"/"Framework"
        match Directory.EnumerateDirectories (d, "v4.0.?????") |> List.ofSeq |> List.rev with
        | x :: _ -> Some x
        | [] -> None
    // IF NOT "%CORDIR40%"=="" set CORDIR=%CORDIR40%
    CORDIR40 |> Option.iter (fun dir -> CORDIR := dir)

    // REM == Use the same runtime as our architecture
    // REM == ASSUMPTION: This could be a good or bad thing.
    // IF /I NOT "%PROCESSOR_ARCHITECTURE%"=="x86" set CORDIR=%CORDIR:Framework=Framework64%
    match PROCESSOR_ARCHITECTURE with X86 -> () | _ -> CORDIR := (!CORDIR).Replace("Framework", "Framework64")

    // FOR /F "tokens=2*" %%A IN ('reg QUERY "%REG_SOFTWARE%\Microsoft\Microsoft SDKs\Windows\v8.1A\WinSDK-NetFx40Tools" /v InstallationFolder') DO SET CORSDK=%%B
    let CORSDK = env "CORSDK" |> ref
    CORSDK := GetSdk81Path "WinSDK-NetFx40Tools"

    // IF "%CORSDK%"=="" FOR /F "tokens=2*" %%A IN ('reg QUERY "HKLM\Software\Microsoft\Microsoft SDKs\Windows" /v CurrentInstallFolder') DO SET CORSDK=%%BBin
    if !CORSDK |> Option.isNone then (
        match Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Microsoft SDKs\Windows", "CurrentInstallFolder", null) with
        | null -> ()
        | :? string as x -> CORSDK := Some x
        | _ -> ()
    )

    // IF NOT "%CORDIR40%"=="" IF EXIST "%CORSDK%\NETFX 4.0 Tools" set CORSDK=%CORSDK%\NETFX 4.0 Tools
    !CORSDK |> Option.iter (fun sdk -> 
        let d = sdk/"NETFX 4.0 Tools"
        if Directory.Exists(d) then CORSDK := Some d
    )

    // REM == Fix up CORSDK for 64bit platforms...
    // IF /I "%PROCESSOR_ARCHITECTURE%"=="AMD64" SET CORSDK=%CORSDK%\x64
    // IF /I "%PROCESSOR_ARCHITECTURE%"=="IA64"  SET CORSDK=%CORSDK%\IA64
    match PROCESSOR_ARCHITECTURE with
    | AMD64 -> CORSDK := !CORSDK |> Option.map (fun dir -> dir/"x64")
    | IA64 -> CORSDK := !CORSDK |> Option.map (fun dir -> dir/"IA64")
    | _ -> ()

    // REM add powerpack to flags only if not already there. Otherwise, the variable can keep growing.
    // echo %fsc_flags% | find /i "powerpack"
    // if ERRORLEVEL 1 set fsc_flags=%fsc_flags% -r:System.Core.dll --nowarn:20
    if fsc_flags |> Option.exists (fun flags -> flags.ToLower().Contains("powerpack")) then ()
    else fsc_flags <- Some (sprintf "%s -r:System.Core.dll --nowarn:20" (fsc_flags |> Option.fold (fun s t -> t) ""))

    // if not defined fsi_flags set fsi_flags=%fsc_flags:--define:COMPILED=% --define:INTERACTIVE --maxerrors:1 --abortonerror
    let mutable fsi_flags = env "fsi_flags"
    if fsi_flags |> Option.isNone then (
        let fsc_flags_no_compiled = fsc_flags |> Option.fold (fun s flags -> flags.Replace("--define:COMPILED", "")) ""
        fsi_flags <- Some (sprintf "%s --define:INTERACTIVE --maxerrors:1 --abortonerror" fsc_flags_no_compiled)
    )

    // echo %fsc_flags%; | find "--define:COMPILED" > NUL || (
    //     set fsc_flags=%fsc_flags% --define:COMPILED
    // )
    if not <| (fsc_flags |> Option.exists (fun flags -> flags.Contains("--define:COMPILED")))
    then fsc_flags <- Some (sprintf "%s --define:COMPILED" (fsc_flags |> Option.fold (fun s t -> t) ""))

    // if NOT "%fsc_flags:generate-config-file=X%"=="%fsc_flags%" ( 
    //     if NOT "%fsc_flags:clr-root=X%"=="%fsc_flags%" ( 
    //         set fsc_flags=%fsc_flags% --clr-root:%CORDIR%
    //     )
    // )
//  --clr-root non e' un flag valido di fsc
//    if not <| (fsc_flags |> Option.exists (fun flags -> flags.Contains("generate-config-file")))  then
//        if not <| (fsc_flags |> Option.exists (fun flags -> flags.Contains("clr-root"))) then 
//            fsc_flags <- Some (sprintf "%s --clr-root:%s" (fsc_flags |> Option.fold (fun s t -> t) "") (!CORDIR))

    // if "%CORDIR%"=="unknown" set CORDIR=
    if (!CORDIR) = "unknown" then CORDIR := ""

    // REM use short names in the path so you don't have to deal with the space in things like "Program Files"
    // for /f "delims=" %%I in ("%CORSDK%") do set CORSDK=%%~dfsI%
    !CORSDK |> Option.iter (fun sdk -> CORSDK := Some (convertToShortPath sdk))
    // for /f "delims=" %%I in ("%CORDIR%") do set CORDIR=%%~dfsI%
    CORDIR := convertToShortPath !CORDIR


    // set NGEN=
    let NGEN = ref None

    // REM ==
    // REM == Set path to C# compiler. If we are NOT on NetFx4.0, try we prefer C# 3.5 to C# 2.0 
    // REM == This is because we have tests that reference System.Core.dll from C# code!
    // REM == (e.g. fsharp\core\fsfromcs)
    // REM ==
    // IF NOT "%CORDIR%"=="" IF EXIST "%CORDIR%\csc.exe" SET CSC="%CORDIR%\csc.exe" %csc_flags%
    let CSC = ref None
    if not <| (!CORDIR = "") then
        fileExists (!CORDIR/"csc.exe") |> Option.iter (fun cscExe -> CSC := Some (sprintf "%s %s" cscExe (csc_flags |> Option.fold (fun s t -> t) "")))
    // IF     "%CORDIR40%"=="" IF NOT "%CORDIR%"=="" IF EXIST "%CORDIR%\..\V3.5\csc.exe" SET CSC="%CORDIR%\..\v3.5\csc.exe" %csc_flags%
    if CORDIR40 |> Option.isNone then
        if not <| (!CORDIR = "") then
            fileExists (!CORDIR/".."/"V3.5"/"csc.exe") |> Option.iter (fun cscExe -> CSC := Some (sprintf "%s %s" cscExe (csc_flags |> Option.fold (fun s t -> t) "")))


    // IF NOT "%CORDIR%"=="" IF EXIST "%CORDIR%\ngen.exe"            SET NGEN=%CORDIR%\ngen.exe
    if not <| (!CORDIR = "") then fileExists (!CORDIR/"ngen.exe") |> Option.iter (fun exe -> NGEN := Some exe)
    // IF NOT "%CORDIR%"=="" IF EXIST "%CORDIR%\al.exe"              SET ALINK=%CORDIR%\al.exe
    if not <| (!CORDIR = "") then fileExists (!CORDIR/"al.exe") |> Option.iter (fun exe -> ALINK := exe)

    // REM ==
    // REM == The logic here is: pick the latest msbuild
    // REM == If we are testing against NDP4.0, then don't try msbuild 3.5
    // REM ==
    let setFromSDK sdk =
        // IF NOT "%CORSDK%"=="" IF EXIST "%CORSDK%\ildasm.exe"          SET ILDASM=%CORSDK%\ildasm.exe
        fileExists (sdk/"ildasm.exe") |> Option.iter (fun exe -> ILDASM := exe)
        // IF NOT "%CORSDK%"=="" IF EXIST "%CORSDK%\gacutil.exe"         SET GACUTIL=%CORSDK%\gacutil.exe
        fileExists (sdk/"gacutil.exe") |> Option.iter (fun exe -> GACUTIL := exe)
        // IF NOT "%CORSDK%"=="" IF EXIST "%CORSDK%\peverify.exe"        SET PEVERIFY=%CORSDK%\peverify.exe
        fileExists (sdk/"peverify.exe") |> Option.iter (fun exe -> PEVERIFY := exe)
        // IF NOT "%CORSDK%"=="" IF EXIST "%CORSDK%\resgen.exe"          SET RESGEN=%CORSDK%\resgen.exe
        fileExists (sdk/"resgen.exe") |> Option.iter (fun exe -> RESGEN := exe)
        // IF NOT "%CORSDK%"=="" IF NOT EXIST "%RESGEN%" IF EXIST "%CORSDK%\..\resgen.exe"       SET RESGEN=%CORSDK%\..\resgen.exe
        if fileExists !RESGEN |> Option.isNone then fileExists (sdk/".."/"resgen.exe") |> Option.iter (fun exe -> RESGEN := exe)
        // IF NOT "%CORSDK%"=="" IF EXIST "%CORSDK%\al.exe"              SET ALINK=%CORSDK%\al.exe
        fileExists (sdk/"al.exe") |> Option.iter (fun exe -> ALINK := exe)
    
    !CORSDK |> Option.iter setFromSDK

    // IF NOT DEFINED FSC SET FSC=fsc.exe
    let FSC = ref (envOrDefault "FSC" "fsc.exe")
    // IF NOT DEFINED FSI SET FSI=%fsiroot%.exe
    let FSI = ref (envOrDefault "FSI" (!fsiroot |> Option.fold (fun s t -> t + s) ".exe"))

    // IF DEFINED FSCBinPath IF EXIST "%FSCBinPath%\fsc.exe"   SET FSC=%FSCBinPath%\fsc.exe
    FSCBinPath |> Option.bind (fun dir -> fileExists (dir/"fsc.exe")) |> Option.iter (fun p -> FSC := p)
    // IF DEFINED FSCBinPath IF EXIST "%FSCBinPath%\%fsiroot%.exe"   SET FSI=%FSCBinPath%\%fsiroot%.exe
    match FSCBinPath, !fsiroot with
    | Some dir, Some fsiExe -> (dir/(fsiExe+".exe")) |> fileExists |> Option.iter (fun p -> FSI := p)
    | _ -> ()

    // REM == Located F# library DLLs in either open or Visual Studio contexts
    // call :GetFSLibPaths
    let X86_PROGRAMFILES, libs = GetFSLibPaths envVars OSARCH FSCBinPath

    // REM == Set standard flags for invoking powershell scripts
    // IF NOT DEFINED PSH_FLAGS SET PSH_FLAGS=-nologo -noprofile -executionpolicy bypass
    let PSH_FLAGS = envOrDefault "PSH_FLAGS" "-nologo -noprofile -executionpolicy bypass"

    let cfg = {
      EnvironmentVariables = envVars;
      ALINK = !ALINK;
      CORDIR = !CORDIR;
      CORSDK = (!CORSDK).Value;
      CSC = (!CSC).Value;
      csc_flags = csc_flags.Value;
      FSC = FSC.Value;
      fsc_flags = fsc_flags.Value;
      FSCBinPath = FSCBinPath.Value;
      FSCOREDLL20PATH = libs.FSCOREDLL20PATH;
      FSCOREDLLPATH = libs.FSCOREDLLPATH;
      FSCOREDLLPORTABLEPATH = libs.FSCOREDLLPORTABLEPATH;
      FSCOREDLLNETCOREPATH = libs.FSCOREDLLNETCOREPATH;
      FSCOREDLLNETCORE78PATH = libs.FSCOREDLLNETCORE78PATH;
      FSCOREDLLNETCORE259PATH = libs.FSCOREDLLNETCORE259PATH;
      FSDATATPPATH = libs.FSDATATPPATH;
      FSDIFF = FSDIFF;
      FSI = FSI.Value;
      fsi_flags = fsi_flags.Value;
      GACUTIL = GACUTIL.Value;
      ILDASM = ILDASM.Value;
      INSTALL_SKU = None;
      MSBUILDTOOLSPATH = None;
      NGEN = (!NGEN).Value;
      PEVERIFY = !PEVERIFY;
      RESGEN = !RESGEN;
    }

    // if DEFINED _UNATTENDEDLOG exit /b 0
    match env "_UNATTENDEDLOG" with
    | Some _ -> cfg
    | None ->
        let msbuildToolsPath, installSku = attendedLog envVars X86_PROGRAMFILES !CORDIR CORDIR40
        { cfg with MSBUILDTOOLSPATH = msbuildToolsPath; INSTALL_SKU = (Some installSku) }


let logConfig (cfg: TestConfig) =
    echo "%s" "---------------------------------------------------------------"
    echo "%s" "Executables"
    echo "%s" ""
    echo "ALINK               =%A" cfg.ALINK
    echo "CORDIR              =%A" cfg.CORDIR
    echo "CORSDK              =%A" cfg.CORSDK
    echo "CSC                 =%A" cfg.CSC
    echo "csc_flags           =%A" cfg.csc_flags
    echo "FSC                 =%A" cfg.FSC
    echo "fsc_flags           =%A" cfg.fsc_flags
    echo "FSCBinPath          =%A" cfg.FSCBinPath
    echo "FSCOREDLL20PATH     =%A" cfg.FSCOREDLL20PATH
    echo "FSCOREDLLPATH       =%A" cfg.FSCOREDLLPATH
    echo "FSCOREDLLPORTABLEPATH =%A" cfg.FSCOREDLLPORTABLEPATH
    echo "FSCOREDLLNETCOREPATH=%A" cfg.FSCOREDLLNETCOREPATH
    echo "FSCOREDLLNETCORE78PATH=%A" cfg.FSCOREDLLNETCORE78PATH
    echo "FSCOREDLLNETCORE259PATH=%A" cfg.FSCOREDLLNETCORE259PATH
    echo "FSDATATPPATH        =%A" cfg.FSDATATPPATH
    echo "FSDIFF              =%A" cfg.FSDIFF
    echo "FSI                 =%A" cfg.FSI
    echo "fsi_flags           =%A" cfg.fsi_flags
    echo "GACUTIL             =%A" cfg.GACUTIL
    echo "ILDASM              =%A" cfg.ILDASM
    echo "INSTALL_SKU         =%A" cfg.INSTALL_SKU
    echo "MSBUILDTOOLSPATH    =%A" cfg.MSBUILDTOOLSPATH
    echo "NGEN                =%A" cfg.NGEN
    echo "PEVERIFY            =%A" cfg.PEVERIFY
    echo "RESGEN              =%A" cfg.RESGEN
    echo "---------------------------------------------------------------"

let getConfig = lazy (
    System.Environment.GetEnvironmentVariables () 
    |> Seq.cast<System.Collections.DictionaryEntry>
    |> Seq.map (fun d -> d.Key :?> string, d.Value :?> string)
    |> Map.ofSeq
    |> config
)
