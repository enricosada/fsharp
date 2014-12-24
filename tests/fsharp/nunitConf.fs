module NUnitConf

open System
open System.IO
open NUnit.Framework

open UpdateCmd
open TestConfig
open All

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
    let FSCBinPath = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", (sprintf "%O" configurationName), "net40", "bin")

    let defaults = [ "FSCBinPath", FSCBinPath ] |> Map.ofList

    let env = defaults |> join (envVars ())

    //TODO check FSCBinPath directory exists

    let logToConsole = printfn "%s"

    let exec input workDir =
        exec' {
            WorkingDirectory = workDir; 
            RedirectError = Some logToConsole;  
            RedirectOutput = Some logToConsole;  
            RedirectInput = input;  
        }

    updateCmd exec env { Configuration = configurationName; Ngen = doNgen; }

    let cfg = config exec env

    logConfig cfg

    let smokeTest () =
        let tempFile = 
            let p = Path.GetTempFileName()
            File.AppendAllText (p, """echo 'printfn "ciao"; exit 0""")
            p

        match cmds.fsc "" [tempFile]  with
        | ErrorLevel e -> Assert.Fail (sprintf """invalid fsc '%s' """ cfg.FSC)
        | Ok -> ()

        match cmds.fsiIn "" [tempFile] with
        | ErrorLevel e -> Assert.Fail (sprintf """invalid fsi '%s' """ cfg.FSI)
        | Ok -> ()
    
    smokeTest ()

    let ex cmdArgs path arguments =
        let fullpath = path |> getfullpath cmdArgs.WorkingDirectory
        printfn "%s" (sprintf "%s %s" fullpath arguments)
        exec' cmdArgs fullpath arguments

    cmds

let suiteHelpers = lazy (
    initializeSuite ()
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

open All

let createTestCaseData (categories: string list) (properties: string list) list =
    let testCaseData (p: Permutation) =
        let name = sprintf "%A" p
        let tc = new TestCaseData( p )
        tc.SetName(name) |> ignore
        tc.SetCategory(sprintf "%A" p) |> ignore
        categories |> List.iter (fun c -> tc.Categories.Add(c) |> ignore)
        properties |> List.iter (fun p -> tc.Categories.Add(p) |> ignore)
        tc    
    list
    |> Seq.map testCaseData

let allPermutation = 
    [ FSI_FILE; FSI_STDIN; FSI_STDIN_OPT; FSI_STDIN_GUI;
      FSC_BASIC; FSC_HW; FSC_O3;
      GENERATED_SIGNATURE; EMPTY_SIGNATURE; EMPTY_SIGNATURE_OPT; 
      FSC_OPT_MINUS_DEBUG; FSC_OPT_PLUS_DEBUG; 
      FRENCH; SPANISH;
      AS_DLL; 
      WRAPPER_NAMESPACE; WRAPPER_NAMESPACE_OPT ]
