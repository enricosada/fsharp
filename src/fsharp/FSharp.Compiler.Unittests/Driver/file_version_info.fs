module FSharp.Compiler.Unittests.Driver.``File version info``

open System
open FSharp.Core.Unittests.LibraryTestFx
open NUnit.Framework

open Microsoft.FSharp.Compiler.Driver.MainModuleBuilder

open FsAssert

module ``fileVersion`` =
    let fileVersionAttrName = typeof<System.Reflection.AssemblyFileVersionAttribute>.FullName

    [<Test>]
    let ``should use AssemblyFileVersionAttribute if set`` () = 
        let findStringAttr n = n |> Check.areEqual fileVersionAttrName; Some "1.2.3.4"
        let warn = failtestf "no warning expected but was '%A'"
        fileVersion warn findStringAttr (1us,0us,0us,0us) |> Check.areEqual (1us,2us,3us,4us) 

    [<Test>] 
    let ``should raise warning FS2003 if AssemblyFileVersionAttribute is not a valid version`` () = 
        let findStringAttr _ = Some "1.2a.3.3"
        let warn, w =
            let checkIsWarning2003 = function
                | Check.Warning(2003, description) as a ->
                    StringAssert.Contains("1.2a.3.3", description)
                    StringAssert.Contains(fileVersionAttrName, description)
                | ex -> failtestf "expecting warning 2003 but was %A" ex
            checkIsWarning2003 |> spy
        fileVersion w findStringAttr (3us,7us,8us,6us) |> Check.areEqual (3us,7us,8us,6us)
        warn |> Expect.once ignore

    [<Test>] 
    let ``should fallback to assemblyVersion if AssemblyFileVersionAttribute not set`` () = 
        let findStringAttr n = n |> Check.areEqual fileVersionAttrName; None;
        let warn = failtestf "no warning expected but was '%A'"
        fileVersion warn findStringAttr (1us,0us,0us,4us) |> Check.areEqual (1us,0us,0us,4us)

module ``productVersion`` =
    let fileInfVersionAttrName = typeof<System.Reflection.AssemblyInformationalVersionAttribute>.FullName

    [<Test>] 
    let ``should use AssemblyInformationalVersionAttribute if set`` () = 
        let findStringAttr, f  = (fun _ -> Some "12.34.56.78") |> spy
        productVersion ignore f (1us,0us,0us,6us) |> Check.areEqual "12.34.56.78"
        findStringAttr |> Expect.once (Check.areEqual fileInfVersionAttrName) 

    [<Test>] 
    let ``should raise warning FS2003 if AssemblyInformationalVersionAttribute is not a valid version`` () = 
        let findStringAttr _ = Some "1.2.3-main (build #12)"
        let warn, w =
            let checkIsWarning2003 = function
                | Check.Warning(2003, description) as a ->
                    StringAssert.Contains("1.2.3-main (build #12)", description)
                    StringAssert.Contains(fileInfVersionAttrName, description)
                | ex -> failtestf "expecting warning 2003 but was %A" ex
            checkIsWarning2003 |> spy
        productVersion w findStringAttr (1us,0us,0us,6us) |> Check.areEqual "1.2.3-main (build #12)"
        warn |> Expect.once ignore

    [<Test>] 
    let ``should fallback to fileVersion if AssemblyInformationalVersionAttribute not set`` () = 
        let warn = failtestf "no warnings expected, but was '%A'"
        productVersion warn (fun _ -> None) (3us,2us,1us,0us) |> Check.areEqual "3.2.1.0" 

    [<Test>] 
    let ``should fallback to fileVersion if AssemblyInformationalVersionAttribute is empty`` () = 
        let warn = failtestf "no warnings expected, but was '%A'"
        productVersion warn (fun _ -> Some "") (3us,2us,1us,0us) |> Check.areEqual "3.2.1.0" 


module ``productVersionAsInt`` =

    [<Test>] 
    let ``should use values if valid major.minor.revision.build version format`` () = 
        productVersionAsInts "1.2.3.4" |> Check.areEqual (1us,2us,3us,4us) 
        productVersionAsInts "0.0.0.0" |> Check.areEqual (0us,0us,0us,0us) 
        productVersionAsInts "3213.57843.32382.59493" |> Check.areEqual (3213us,57843us,32382us,59493us)
        productVersionAsInts "3213.57843.32382.59493" |> Check.areEqual (3213us,57843us,32382us,59493us)
        let max = System.UInt16.MaxValue
        productVersionAsInts (sprintf "%d.%d.%d.%d" max max max max) |> Check.areEqual (max,max,max,max)
        productVersionAsInts (sprintf "%d.%d.%d.%d" 70000 70000 70000 70000) |> Check.areEqual (4464us,4464us,4464us,4464us)

    [<Test>] 
    let ``should zero starting from first invalid version part`` () = 
        [
            "1.2.3.4", (1us,2us,3us,4us);
            "1.2.3.4a", (1us,2us,3us,0us);
            "1.2.c3.4", (1us,2us,0us,0us)
            "1.2-d.3.4", (1us,0us,0us,0us);
            "1dd.2.3.4", (0us,0us,0us,0us);
            "1dd.2da.d3hj.dd4ds", (0us,0us,0us,0us);
            "1.5.6.7.dasd", (1us,5us,6us,7us);
            "9.3", (9us,3us,0us,0us);
            "", (0us,0us,0us,0us);
        ]
        |> Map.ofList
        |> Map.iter (fun from expected -> productVersionAsInts from |> Check.areEqual expected )
