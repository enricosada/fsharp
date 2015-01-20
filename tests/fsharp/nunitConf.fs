module NUnitConf

open System
open System.IO
open NUnit.Framework

open UpdateCmd
open TestConfig
open PlatformHelpers


let checkTestResult =
    function
    | Success () -> ()
    | Failure (GenericError msg) -> Assert.Fail (msg)
    | Failure (ProcessExecError (err, msg)) -> Assert.Fail (sprintf "ERRORLEVEL %i %s" err msg)
    | Failure (Skipped msg) -> Assert.Ignore(sprintf "skipped. Reason: %s" msg)


let skip msg () = Failure (Skipped msg)
let genericError msg () = Failure (GenericError msg)
let errorLevel exitCode msg () = Failure (ProcessExecError (exitCode,msg))

let envVars () = 
    System.Environment.GetEnvironmentVariables () 
    |> Seq.cast<System.Collections.DictionaryEntry>
    |> Seq.map (fun d -> d.Key :?> string, d.Value :?> string)
    |> Map.ofSeq

let initializeSuite () =

    let configurationName = DEBUG
    let doNgen = false;
    let FSCBinPath = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", (sprintf "%O" configurationName), "net40", "bin")

    let mapWithDefaults defaults m =
        Seq.concat [ (Map.toSeq defaults) ; (Map.toSeq m) ] |> Map.ofSeq

    let env = envVars () |> mapWithDefaults ( ["FSCBinPath", FSCBinPath] |> Map.ofList )

    processor {
        do! updateCmd env { Configuration = configurationName; Ngen = doNgen; }
            |> Attempt.Run
            |> function Success () -> Success () | Failure msg -> genericError msg ()

        let cfg =
            let c = config env
            let usedEnvVars =
                c.EnvironmentVariables 
                |> Map.add "FSC" c.FSC             
            { c with EnvironmentVariables = usedEnvVars }

        logConfig cfg

        let checkfscBinPath () = processor {
            let fscBinPath = cfg.EnvironmentVariables |> Map.tryFind "FSCBinPath"
            return!
                match fscBinPath |> Option.bind directoryExists with
                | Some _ -> Success
                | None -> genericError "environment variable 'FSCBinPath' is required to be a valid directory"
            }

        let smokeTest () = processor {
            let tempFile ext = 
                let p = Path.ChangeExtension( Path.GetTempFileName(), ext)
                File.AppendAllText (p, """printfn "ciao"; exit 0""")
                p

            let tempDir = Commands.createTempDir ()
            let exec exe args = 
                log "%s %s" exe args
                use toLog = redirectToLog ()
                Process.exec { RedirectError = Some toLog.Post; RedirectOutput = Some toLog.Post; RedirectInput = None } tempDir cfg.EnvironmentVariables exe args

            do! Commands.fsc exec cfg.FSC "" [ tempFile ".fs" ] |> checkResult

            do! Commands.fsi exec cfg.FSI "" [ tempFile ".fsx" ] |> checkResult
        
            }
    
        do! checkfscBinPath ()

        do! smokeTest ()

        return cfg
    } 


let suiteHelpers = lazy (
    initializeSuite ()
    |> Attempt.Run 
    |> function Success x -> x | Failure err -> failwith (sprintf "Error %A" err)
)

[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Class ||| AttributeTargets.Interface ||| AttributeTargets.Assembly, AllowMultiple = true)>]
type public InitializeSuiteAttribute () =
    inherit Attribute()

    interface ITestAction with
        member x.Targets = ActionTargets.Test ||| ActionTargets.Suite
        member x.BeforeTest details =
            if details.IsSuite 
            then suiteHelpers.Force() |> ignore

        member x.AfterTest details =
            ()
    
    // Workaround: NUnit try to find a *public* instance property Targets (ignoring cast to ITestAction)
    //
    // x.Targets doesn't work because is implemented as method instead of readonly property, as follow
    //
    //    [SpecialName]
    //    ActionTargets ITestAction.NUnit\u002DFramework\u002DITestAction\u002Dget_Targets()
    //    {
    //      return (ActionTargets) (1 | 2);
    //    }
    //
    // instead of
    //
    //    public ActionTargets Targets
    //    {
    //      get
    //      {
    //        return (ActionTargets) (1 | 2);
    //      }
    //    }
    //
    member x.Targets = ActionTargets.Test ||| ActionTargets.Suite


[<assembly:InitializeSuite()>]
()

module FSharpTestSuite =

    let allPermutation = 
        [ FSI_FILE; FSI_STDIN; FSI_STDIN_OPT; FSI_STDIN_GUI;
          FSC_BASIC; FSC_HW; FSC_O3;
          GENERATED_SIGNATURE; EMPTY_SIGNATURE; EMPTY_SIGNATURE_OPT; 
          FSC_OPT_MINUS_DEBUG; FSC_OPT_PLUS_DEBUG; 
          FRENCH; SPANISH;
          AS_DLL; 
          WRAPPER_NAMESPACE; WRAPPER_NAMESPACE_OPT ]

    let getTagsOfFile path =
        match File.ReadLines(path) |> Seq.tryFind (fun _ -> true) with
        | None -> []
        | Some line -> 
            line.TrimStart('/').Split([| '#' |], StringSplitOptions.RemoveEmptyEntries)
            |> Seq.map (fun s -> s.Trim())
            |> Seq.filter (fun s -> s.Length > 0)
            |> Seq.distinct
            |> Seq.toList

    let getTestFileMetadata dir =
        Directory.EnumerateFiles(dir, "*.fs*")
        |> Seq.toList
        |> List.collect getTagsOfFile

    let setTestDataInfo (group,name) (tc: TestCaseData) =
        let testDir = Path.Combine(__SOURCE_DIRECTORY__, group, name)
        let categories = [ group; name ] @ (testDir |> getTestFileMetadata)
        let properties = [ "DIRECTORY", testDir ] |> Map.ofList

        categories |> List.iter (fun (c: string) -> tc.Categories.Add(c) |> ignore)
        properties |> Map.iter (fun k v -> tc.Properties.Add(k,v))
        tc
    
    let setCategory s (tc: TestCaseData) =
        tc.SetCategory(s)

module FileGuard =

    let private remove = fileExists >> Option.iter File.Delete

    [<AllowNullLiteral>]
    type T (path: string) =
        member x.Path = path
        interface IDisposable with
            member x.Dispose () = path |> remove

    let create path = 
        path |> remove
        new T(path)
    
    let exists (guard: T) = guard.Path |> fileExists |> Option.isSome
        

let checkGuardExists guard = processor {
    if not <| (guard |> FileGuard.exists)
    then return! genericError (sprintf "exit code 0 but %s file doesn't exists" (guard.Path |> Path.GetFileName))
    }



type TestRunContext = { Directory: string; Config: TestConfig }

let check (f: Attempt<_,_>) =
    f |> Attempt.Run |> checkTestResult



type RedirectInfo = { Output: RedirectTo; Input: RedirectFrom option }
and RedirectTo =
    | Inherit
    | Output of RedirectToType
    | OutputAndError of RedirectToType
    | Error of RedirectToType
and RedirectToType = Overwrite of FilePath | Append of FilePath
and RedirectFrom = RedirectInput of FilePath list

module Command =

    let logExec dir path args redirect =
        let inF =
            function
            | None -> ""
            | Some(RedirectInput l) -> sprintf " <%s" (l |> Seq.ofList |> String.concat " ")
        let redirectType = function Overwrite x -> sprintf ">%s" x | Append x -> sprintf ">>%s" x
        let outF =
            function
            | Inherit -> ""
            | Output r-> sprintf " 1%s" (redirectType r)
            | OutputAndError r -> sprintf " 1%s 2>&1" (redirectType r)
            | Error r -> sprintf " 2%s" (redirectType r)
        sprintf "%s%s%s%s" path (match args with "" -> "" | x -> " " + x) (inF redirect.Input) (outF redirect.Output)

    let exec dir envVars redirect path args =
        let { Output = o; Input = i} = redirect

        let inputWriter sources (writer: StreamWriter) =
            let pipeFile name = async {
                let path =
                    if Path.IsPathRooted(name) then name
                    else Path.Combine(dir,name) |> Path.GetFullPath

                use reader = File.OpenRead (path)
                use ms = new MemoryStream()
                do! reader.CopyToAsync (ms) |> (Async.AwaitIAsyncResult >> Async.Ignore)
                ms.Position <- 0L
                try
                    do! ms.CopyToAsync(writer.BaseStream) |> (Async.AwaitIAsyncResult >> Async.Ignore)
                    do! writer.FlushAsync() |> (Async.AwaitIAsyncResult >> Async.Ignore)
                with
                | :? System.IO.IOException as ex -> //input closed is ok if process is closed
                    ()
                }
            sources |> List.iter (pipeFile >> Async.RunSynchronously)

        let inF fCont cmdArgs =
            match i with
            | None -> fCont cmdArgs
            | Some(RedirectInput l) -> fCont { cmdArgs with RedirectInput = Some (inputWriter l) }

        let openWrite = function Append p -> new StreamWriter(p,true) | Overwrite p -> new StreamWriter(p,false)

        let outF fCont cmdArgs =
            match o with
            | RedirectTo.Inherit ->  
                use toLog = redirectToLog ()
                fCont { cmdArgs with RedirectOutput = Some (toLog.Post); RedirectError = Some (toLog.Post) }
            | Output r ->
                use writer = openWrite r
                use outFile = redirectTo writer
                use toLog = redirectToLog ()
                fCont { cmdArgs with RedirectOutput = Some (outFile.Post); RedirectError = Some (toLog.Post) }
            | OutputAndError r ->
                use writer = openWrite r
                use outFile = redirectTo writer
                fCont { cmdArgs with RedirectOutput = Some (outFile.Post); RedirectError = Some (outFile.Post) }
            | Error r ->
                use writer = openWrite r
                use outFile = redirectTo writer
                use toLog = redirectToLog ()
                fCont { cmdArgs with RedirectOutput = Some (toLog.Post); RedirectError = Some (outFile.Post) }
            
        let exec cmdArgs =
            log "%s" (logExec dir path args redirect)
            Process.exec cmdArgs dir envVars path args

        { RedirectOutput = None; RedirectError = None; RedirectInput = None }
        |> (outF (inF exec))
    