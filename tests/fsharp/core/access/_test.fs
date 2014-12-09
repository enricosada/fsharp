module ``Core-Access``

open System
open System.IO
open NUnit.Framework
open All
open TestConfig
open SingleTestBuild
open SingleTestRun

let permutations = All.allPermutation |> createFSharpTestPermu

[<Test>]
[<TestCaseSource("permutations")>]
let run (p: Permutation) =
    let cfg = getConfig.Value
    logConfig cfg
    singleTestBuild cfg __SOURCE_DIRECTORY__ p
    singleTestRun cfg __SOURCE_DIRECTORY__ p

