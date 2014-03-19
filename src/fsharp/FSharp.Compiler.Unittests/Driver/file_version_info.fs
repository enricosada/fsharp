module FSharp.Compiler.Unittests.Driver.``File version info``

open System
open FSharp.Core.Unittests.LibraryTestFx
open NUnit.Framework

open Microsoft.FSharp.Compiler.Driver.MainModuleBuilder

module ``fileVersion`` =
    let fileVersionAttrName = typeof<System.Reflection.AssemblyFileVersionAttribute>.FullName

    [<Test>] 
    let ``should use AssemblyFileVersionAttribute if set`` () = 
        let r, findStringAttr = (fun _ -> Some "1.2.3.4") |> FsAssert.spy
        let assemblyVersion = (1us,0us,0us,0us)
        FsAssert.areEqual (1us,2us,3us,4us) (fileVersion findStringAttr assemblyVersion)
        FsAssert.once (fileVersionAttrName) r

    [<Test>] 
    let ``should fallback to assemblyVersion if AssemblyFileVersionAttribute not set`` () = 
        let findStringAttr name = FsAssert.areEqual fileVersionAttrName name; None
        let assemblyVersion = (1us,0us,0us,4us)
        FsAssert.areEqual (1us,0us,0us,4us) (fileVersion findStringAttr assemblyVersion)

module ``productVersion`` =
    let fileInfVersionAttrName = typeof<System.Reflection.AssemblyInformationalVersionAttribute>.FullName

    [<Test>] 
    let ``should use AssemblyInformationalVersionAttribute if set`` () = 
        let r, findStringAttr = (fun _ -> Some "1.2.3-main (build #12)") |> FsAssert.spy
        let assemblyVersion = (1us,0us,0us,6us)
        FsAssert.areEqual "1.2.3-main (build #12)" (productVersion findStringAttr assemblyVersion)
        FsAssert.once (fileInfVersionAttrName) r

    [<Test>] 
    let ``should fallback to assemblyVersion if AssemblyInformationalVersionAttribute not set or empty`` () = 
        let findStringAttr name = FsAssert.areEqual fileInfVersionAttrName name; None
        let assemblyFileVersion = (3us,2us,1us,0us)
        FsAssert.areEqual "3.2.1.0" (productVersion findStringAttr assemblyFileVersion)
        FsAssert.areEqual "3.2.1.0" (productVersion (fun _ -> Some "") assemblyFileVersion)


module ``productVersionAsInt`` =

    [<Test>] 
    let ``should use values if valid major.minor.revision.build version format`` () = 
        FsAssert.areEqual (1us,2us,3us,4us) (productVersionAsInts "1.2.3.4")
        FsAssert.areEqual (0us,0us,0us,0us) (productVersionAsInts "0.0.0.0")
        FsAssert.areEqual (3213us,57843us,32382us,59493us) (productVersionAsInts "3213.57843.32382.59493")
        FsAssert.areEqual (3213us,57843us,32382us,59493us) (productVersionAsInts "3213.57843.32382.59493")
        let max = System.UInt16.MaxValue
        FsAssert.areEqual (max,max,max,max) (productVersionAsInts (sprintf "%d.%d.%d.%d" max max max max))
        FsAssert.areEqual (4464us,4464us,4464us,4464us) (productVersionAsInts (sprintf "%d.%d.%d.%d" 70000 70000 70000 70000))

    [<Test>] 
    let ``should zero starting from first invalid version part`` () = 
       FsAssert.areEqual (1us,2us,3us,4us) (productVersionAsInts "1.2.3.4")
       FsAssert.areEqual (1us,2us,3us,0us) (productVersionAsInts "1.2.3.4a")
       FsAssert.areEqual (1us,2us,0us,0us) (productVersionAsInts "1.2.c3.4")
       FsAssert.areEqual (1us,0us,0us,0us) (productVersionAsInts "1.2-d.3.4")
       FsAssert.areEqual (0us,0us,0us,0us) (productVersionAsInts "1dd.2.3.4")
       FsAssert.areEqual (0us,0us,0us,0us) (productVersionAsInts "1dd.2da.d3hj.dd4ds")
       FsAssert.areEqual (1us,5us,6us,7us) (productVersionAsInts "1.5.6.7.dasd")
       FsAssert.areEqual (9us,3us,0us,0us) (productVersionAsInts "9.3")
       FsAssert.areEqual (0us,0us,0us,0us) (productVersionAsInts "")
 
