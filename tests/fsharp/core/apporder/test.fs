module ``Core-Apporder``

open System
open System.IO
open NUnit.Framework
open All

let permutations = All.allPermutation |> createFSharpTestPermu

[<Test>]
[<TestCaseSource("permutations")>]
let run (p: Permutation) =
    printfn "%A" p
    Assert.Fail()
    
