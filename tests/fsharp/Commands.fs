module Commands

open System
open System.IO

open PlatformHelpers

let getfullpath workDir path =
    let rooted =
        if Path.IsPathRooted(path) then path
        else Path.Combine(workDir, path)
    rooted |> Path.GetFullPath

/// copy /y %source1% tmptest2.ml
let copy_y workDir source to' = 
    log "copy /y %s %s" source to'
    File.Copy( source |> getfullpath workDir, to' |> getfullpath workDir, true)

// echo. > build.ok
let ``echo._tofile`` workDir text p =
    log "echo.%s> %s" text p
    let to' = p |> getfullpath workDir in File.WriteAllText(to', text + Environment.NewLine)

/// echo // empty file  > tmptest2.mli
let echo_tofile workDir text p =
    log "echo %s> %s" text p
    let to' = p |> getfullpath workDir in File.WriteAllText(to', text + Environment.NewLine)

/// type %source1%  >> tmptest3.ml
let type_append_tofile workDir source p =
    log "type %s >> %s" source p
    let from = source |> getfullpath workDir
    let to' = p |> getfullpath workDir
    let contents = File.ReadAllText(to')
    File.AppendAllText(from, contents)

// %GACUTIL% /if %BINDIR%\FSharp.Core.dll
let gacutil exec exeName flags assembly : CmdResult =
    exec exeName (sprintf """%s "%s" """ flags assembly)

// "%NGEN32%" install "%BINDIR%\fsc.exe" /queue:1
// "%NGEN32%" install "%BINDIR%\fsi.exe" /queue:1
// "%NGEN32%" install "%BINDIR%\FSharp.Build.dll" /queue:1
// "%NGEN32%" executeQueuedItems 1
let ngen exec (ngenExe: FilePath) assemblies =
    let queue = assemblies |> List.map (fun a -> (sprintf "install \"%s\" /queue:1" a))

    List.concat [ ["executeQueuedItems 1"]; queue ]
    |> Seq.ofList
    |> Seq.map (fun args -> exec ngenExe args)
    |> Seq.takeWhile (function ErrorLevel _ -> false | Ok -> true)
    |> Seq.last

let fsc exec fscExe flags srcFiles =
    // "%FSC%" %fsc_flags% --define:COMPILING_WITH_EMPTY_SIGNATURE -o:tmptest2.exe tmptest2.mli tmptest2.ml
    exec fscExe (sprintf "%s %s"  flags (srcFiles |> Seq.ofList |> String.concat " "))

let csc exec cscExe flags srcFiles =
    exec cscExe (sprintf "%s %s"  flags (srcFiles |> Seq.ofList |> String.concat " "))

let fsi exec fsiExe flags sources =
    exec fsiExe (sprintf "%s %s" flags (sources |> Seq.ofList |> String.concat " "))

let fsiIn exec fsiExe flags sources =
    let inputWriter (writer: StreamWriter) =
        let pipeFile name =
            use reader = File.OpenRead name
            use ms = new MemoryStream()
            reader.CopyTo (ms)
            ms.Position <- 0L
            try
                ms.CopyTo(writer.BaseStream)
            with
            | :? System.IO.IOException as ex -> //input closed is ok if process is closed
                ()
        sources |> List.iter pipeFile

    exec inputWriter fsiExe flags

let peverify exec peverifyExe path =
    exec peverifyExe path

let createTempDir () =
    let path = Path.GetTempFileName ()
    File.Delete path
    Directory.CreateDirectory path |> ignore
    path

let convertToShortPath path =
    log "convert to short path %s" path
    let result = ref None
    let lastLine = function null -> () | l -> result := Some l

    let cmdArgs = {
        RedirectOutput = Some lastLine;
        RedirectError = None;
        RedirectInput = None;
    }
    
    let args = sprintf """/c for /f "delims=" %%I in ("%s") do echo %%~dfsI""" path

    match Process.exec cmdArgs (Path.GetTempPath()) Map.empty "cmd.exe" args with
    | ErrorLevel _ -> path
    | Ok -> match !result with None -> path | Some p -> p

let where envVars cmd =
    log "where %s" cmd
    let result = ref None
    let lastLine = function null -> () | l -> result := Some l

    let cmdArgs = { RedirectOutput = Some lastLine; RedirectError = None; RedirectInput = None; }
    
    match Process.exec cmdArgs (Path.GetTempPath()) envVars "cmd.exe" (sprintf "/c where %s" cmd) with
    | ErrorLevel _ -> None
    | CmdResult.Success -> !result    
