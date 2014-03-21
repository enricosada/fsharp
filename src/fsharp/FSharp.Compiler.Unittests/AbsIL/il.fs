module FSharp.Compiler.Unittests.AbstractIL.IL

open System
open FSharp.Core.Unittests.LibraryTestFx
open NUnit.Framework

open Microsoft.FSharp.Compiler.AbstractIL.IL

open FsAssert

module Platform =

    [<Test; Platform("Mono")>] 
    let ``Running on mono`` () =
        runningOnMono |> Check.areEqual true

    [<Test; Platform("Net")>] 
    let ``Running on DotNET`` () =
        runningOnMono |> Check.areEqual false


module ``Parse ILVersion`` =
    let toVI (c1,c2,c3,c4) : ILVersionInfo = (c1,c2,c3,c4)

    [<Test>]
    let parseILVersion () =
        parseILVersion "0.0.0.0" |> Check.areEqual ((0us,0us,0us,0us)|> toVI)
        parseILVersion "1.2.3.4" |> Check.areEqual ((1us,2us,3us,4us)|> toVI)


