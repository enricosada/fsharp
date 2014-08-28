@echo on
::Env

REM
REM FSHARP_REPO is OPENEDITION or VISUALFSHARP, default OPENEDITION
REM read FSHARP_REPO value from *last* line of fsharp_repo.txt, or FSHARP_REPO env var if defined
REM

if "%FSHARP_REPO%"=="" (
	for /f "delims=" %%x in (fsharp_repo.txt) do set FSHARP_REPO=%%x
)

if "%FSHARP_REPO%"=="" (
	set FSHARP_REPO=OPENEDITION
)
echo Repo: %FSHARP_REPO%

REM
REM MSBUILD LOCATION
REM

if %PROCESSOR_ARCHITECTURE%==x86 (
	set MSBuild="%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"
) else (
	set MSBUILD="%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
)

if /i "%PROCESSOR_ARCHITECTURE%"=="x86" set X86_PROGRAMFILES=%ProgramFiles%
if /I "%PROCESSOR_ARCHITECTURE%"=="AMD64" set X86_PROGRAMFILES=%ProgramFiles(x86)%

if %FSHARP_REPO%==VISUALFSHARP (
	REM VISUALFSHARP require msbuild 12.0
	set MSBUILD="%X86_PROGRAMFILES%\MSBuild\12.0\bin\MSBuild.exe"
)
echo MSBuild: %MSBUILD%

echo TargetFSharpLibraryFramework: %TargetFSharpLibraryFramework%

::Clean
del /F /S /Q lib\proto
del /F /S /Q lib\release

::Build
pushd src
set ABS_PATH=%CD%
if %FSHARP_REPO%==VISUALFSHARP (
    "%X86_PROGRAMFILES%\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\gacutil.exe" /i "%ABS_PATH%\..\lkg\FSharp-2.0.50726.900\bin\FSharp.Core.dll" || goto :error
)

%MSBUILD% "%ABS_PATH%\fsharp-proto-build.proj" || goto :error
%MSBUILD% "%ABS_PATH%\fsharp-library-build.proj" /p:TargetFramework=net40 /p:Configuration=Release || goto :error
%MSBUILD% "%ABS_PATH%\fsharp-compiler-build.proj" /p:TargetFramework=net40 /p:Configuration=Release || goto :error
if %FSHARP_REPO%==VISUALFSHARP (
%MSBUILD% "%ABS_PATH%\fsharp-typeproviders-build.proj" /p:TargetFramework=net40 /p:Configuration=Release || goto :error
)

if "%TargetFSharpLibraryFramework%"=="" (
%MSBUILD% "%ABS_PATH%\fsharp-library-build.proj" /p:TargetFramework=net20 /p:Configuration=Release
%MSBUILD% "%ABS_PATH%\fsharp-library-build.proj" /p:TargetFramework=portable47 /p:Configuration=Release
%MSBUILD% "%ABS_PATH%\fsharp-library-build.proj" /p:TargetFramework=portable7 /p:Configuration=Release
%MSBUILD% "%ABS_PATH%\fsharp-library-build.proj" /p:TargetFramework=portable78 /p:Configuration=Release
%MSBUILD% "%ABS_PATH%\fsharp-library-build.proj" /p:TargetFramework=portable259 /p:Configuration=Release
%MSBUILD% "%ABS_PATH%\fsharp-library-build.proj" /p:TargetFramework=sl5 /p:Configuration=Release
) else (
%MSBUILD% "%ABS_PATH%\fsharp-library-build.proj" /p:TargetFramework=%TargetFSharpLibraryFramework% /p:Configuration=Release || goto :error
REM %MSBUILD% "%ABS_PATH%\fsharp-library-unittests-build.proj" /p:TargetFramework=%TargetFSharpLibraryFramework% /p:Configuration=Release || goto :error
)

if %FSHARP_REPO%==VISUALFSHARP (
	REM Use this script to add the built FSharp.Core to the GAC, add required strong name validation skips
	REM but dont NGen the compiler and libraries. ( update.cmd release -ngen )
	call "%ABS_PATH%\update.cmd" Release || goto :error
)

popd

if not "%BuildVSIntegration%"=="" (
	pushd vsintegration
	set ABS_PATH=%CD%
	%MSBUILD% "%ABS_PATH%\fsharp-vsintegration-build.proj" || goto :error
	popd
)

goto :EOF

:error
echo Failed with error #%ERRORLEVEL%.
exit /b %ERRORLEVEL%

