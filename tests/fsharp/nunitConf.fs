module NUnitConf

open System
open System.IO
open NUnit.Framework

open UpdateCmd
open TestConfig
open PlatformHelpers

let envVars () = 
    System.Environment.GetEnvironmentVariables () 
    |> Seq.cast<System.Collections.DictionaryEntry>
    |> Seq.map (fun d -> d.Key :?> string, d.Value :?> string)
    |> Map.ofSeq

let join p q = 
    Seq.concat [ (Map.toSeq p) ; (Map.toSeq q) ]
    |> Map.ofSeq

let initializeSuite () =

    let configurationName = DEBUG
    let doNgen = false;
    let FSCBinPath = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", (sprintf "%O" configurationName), "net40", "bin")

    let defaults = [ "FSCBinPath", FSCBinPath ] |> Map.ofList

    let env = defaults |> join (envVars ())

    //TODO check FSCBinPath directory exists
    processor {
        do! updateCmd env { Configuration = configurationName; Ngen = doNgen; } |> Attempt.Run

        let cfg = config env

        logConfig cfg

        let smokeTest () = processor {
            let tempFile ext = 
                let p = Path.ChangeExtension( Path.GetTempFileName(), ext)
                File.AppendAllText (p, """printfn "ciao"; exit 0""")
                p

            let tempDir = Commands.createTempDir ()
            let exec exe args = 
                log "%s %s" exe args
                exec' { RedirectError = Some (log "%s"); RedirectOutput = Some (log "%s"); RedirectInput = None } tempDir envVars exe args
            let execIn input exe args = 
                log "%s %s" exe args
                exec' { RedirectError = Some (log "%s"); RedirectOutput = Some (log "%s"); RedirectInput = Some input } tempDir envVars exe args

            let checkResult = function CmdResult.ErrorLevel err -> Failure (sprintf "ERRORLEVEL %d" err) | CmdResult.Success -> Success ()

            do! Commands.fsc exec cfg.FSC "" [ tempFile ".fs" ] |> checkResult

            do! Commands.fsi exec cfg.FSI "" [ tempFile ".fsx" ] |> checkResult
        
            }
    
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
    inherit System.Attribute()

    interface ITestAction with
        member x.Targets = (ActionTargets.Test ||| ActionTargets.Suite)
        member x.BeforeTest details =
            if details.IsSuite 
            then suiteHelpers.Force() |> ignore

        member x.AfterTest details = 
            ()  
    
    // Workaround: NUnit try to find a *public* instance property Targets (ignoring cast to ITestAction)
    //
    // x.Targets doesn't work because is implemented as follow
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
    member x.Targets = (ActionTargets.Test ||| ActionTargets.Suite)


[<assembly:InitializeSuite()>]
()

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

let createTestCaseData (group,name) list =
    let testDir = Path.Combine(__SOURCE_DIRECTORY__, group, name)
    let categories = [group; name] @ (testDir |> getTestFileMetadata)
    let properties = [ "DIRECTORY", testDir ] |> Map.ofList

    let testCaseData (p: Permutation) =
        let name = sprintf "%A" p
        let tc = new TestCaseData( p )
        tc.SetName(name) |> ignore
        tc.SetCategory(sprintf "%A" p) |> ignore
        categories |> List.iter (fun (c: string) -> tc.Categories.Add(c) |> ignore)
        properties |> Map.iter (fun k v -> tc.Properties.Add(k,v))
        tc    
    
    list
    |> Seq.map testCaseData

let checkTestResult =
    function
    | Success () -> ()
    | Failure (Error (err, msg)) -> Assert.Fail (sprintf "ERRORLEVEL %i %s" err msg)
    | Failure (Skipped msg) -> Assert.Ignore(sprintf "skipped. Reason: %s" msg)
