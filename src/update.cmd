@rem ===========================================================================================================
@rem Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, 
@rem               Version 2.0.  See License.txt in the project root for license information.
@rem ===========================================================================================================

@echo ON
setlocal

if /i "%1" == "debug" goto :ok
if /i "%1" == "release" goto :ok

echo GACs built binaries, adds required strong name verification skipping, and optionally NGens built binaries
echo Usage:
echo    update.cmd debug [-ngen]
echo    update.cmd release [-ngen]
exit /b 1

:ok

set BINDIR=%~dp0..\%1\net40\bin

if /i "%PROCESSOR_ARCHITECTURE%"=="x86" set X86_PROGRAMFILES=%ProgramFiles%
if /I "%PROCESSOR_ARCHITECTURE%"=="AMD64" set X86_PROGRAMFILES=%ProgramFiles(x86)%

set GACUTIL="%X86_PROGRAMFILES%\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\gacutil.exe"
set SN32="%X86_PROGRAMFILES%\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\sn.exe"
set SN64="%X86_PROGRAMFILES%\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\x64\sn.exe"
set NGEN32=%windir%\Microsoft.NET\Framework\v4.0.30319\ngen.exe
set NGEN64=%windir%\Microsoft.NET\Framework64\v4.0.30319\ngen.exe

rem Disable strong-name validation for F# binaries built from open source that are signed with the microsoft key
%SN32% -Vr FSharp.Core,b03f5f7f11d50a3a 
%SN32% -Vr FSharp.Build,b03f5f7f11d50a3a
%SN32% -Vr FSharp.Compiler.Interactive.Settings,b03f5f7f11d50a3a
%SN32% -Vr FSharp.Compiler.Hosted,b03f5f7f11d50a3a

%SN32% -Vr FSharp.Compiler,b03f5f7f11d50a3a
%SN32% -Vr FSharp.Compiler.Server.Shared,b03f5f7f11d50a3a
%SN32% -Vr FSharp.Editor,b03f5f7f11d50a3a
%SN32% -Vr FSharp.LanguageService,b03f5f7f11d50a3a
%SN32% -Vr FSharp.LanguageService.Base,b03f5f7f11d50a3a
%SN32% -Vr FSharp.LanguageService.Compiler,b03f5f7f11d50a3a
%SN32% -Vr FSharp.ProjectSystem.Base,b03f5f7f11d50a3a
%SN32% -Vr FSharp.ProjectSystem.FSharp,b03f5f7f11d50a3a
%SN32% -Vr FSharp.ProjectSystem.PropertyPages,b03f5f7f11d50a3a
%SN32% -Vr FSharp.VS.FSI,b03f5f7f11d50a3a
%SN32% -Vr Unittests,b03f5f7f11d50a3a
%SN32% -Vr Salsa,b03f5f7f11d50a3a

if /i "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    %SN64% -Vr FSharp.Core,b03f5f7f11d50a3a
    %SN64% -Vr FSharp.Build,b03f5f7f11d50a3a
    %SN64% -Vr FSharp.Compiler.Interactive.Settings,b03f5f7f11d50a3a
    %SN64% -Vr FSharp.Compiler.Hosted,b03f5f7f11d50a3a

    %SN64% -Vr FSharp.Compiler,b03f5f7f11d50a3a
    %SN64% -Vr FSharp.Compiler.Server.Shared,b03f5f7f11d50a3a
    %SN64% -Vr FSharp.Editor,b03f5f7f11d50a3a
    %SN64% -Vr FSharp.LanguageService,b03f5f7f11d50a3a
    %SN64% -Vr FSharp.LanguageService.Base,b03f5f7f11d50a3a
    %SN64% -Vr FSharp.LanguageService.Compiler,b03f5f7f11d50a3a
    %SN64% -Vr FSharp.ProjectSystem.Base,b03f5f7f11d50a3a
    %SN64% -Vr FSharp.ProjectSystem.FSharp,b03f5f7f11d50a3a
    %SN64% -Vr FSharp.ProjectSystem.PropertyPages,b03f5f7f11d50a3a
    %SN64% -Vr FSharp.VS.FSI,b03f5f7f11d50a3a
    %SN64% -Vr Unittests,b03f5f7f11d50a3a
    %SN64% -Vr Salsa,b03f5f7f11d50a3a
)

rem Only GACing FSharp.Core for now
%GACUTIL% /if %BINDIR%\FSharp.Core.dll || goto :error

rem NGen fsc, fsi, fsiAnyCpu, and FSharp.Build.dll
if /i not "%2"=="-ngen" goto :donengen

"%NGEN32%" install "%BINDIR%\fsc.exe" /queue:1
"%NGEN32%" install "%BINDIR%\fsi.exe" /queue:1
"%NGEN32%" install "%BINDIR%\FSharp.Build.dll" /queue:1
"%NGEN32%" executeQueuedItems 1

if /i "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    "%NGEN64%" install "%BINDIR%\fsiAnyCpu.exe" /queue:1
    "%NGEN64%" install "%BINDIR%\FSharp.Build.dll" /queue:1
    "%NGEN64%" executeQueuedItems 1
)

:donengen


goto :EOF

:error
echo Failed with error #%ERRORLEVEL%.
exit /b %ERRORLEVEL%

