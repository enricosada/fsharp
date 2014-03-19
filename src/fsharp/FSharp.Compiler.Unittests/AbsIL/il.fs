module FSharp.Compiler.Unittests.AbstractIL.IL

open System
open FSharp.Core.Unittests.LibraryTestFx
open NUnit.Framework

open Microsoft.FSharp.Compiler.AbstractIL.IL


module Platform =

    [<Test; Platform("Mono")>] 
    let ``Running on mono`` () =
        Assert.True(runningOnMono)

    [<Test; Platform("Net")>] 
    let ``Running on DotNET`` () =
        Assert.False(runningOnMono)


module ``Parse ILVersion`` =
    let toVI (c1,c2,c3,c4) : ILVersionInfo = (c1,c2,c3,c4)

    [<Test>]
    let parseILVersion () =
        FsAssert.areEqual ((0us,0us,0us,0us)|> toVI) (parseILVersion "0.0.0.0")
        FsAssert.areEqual ((1us,2us,3us,4us)|> toVI) (parseILVersion "1.2.3.4")


