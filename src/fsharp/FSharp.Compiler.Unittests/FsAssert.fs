[<RequireQualifiedAccess>]
module FsAssert

open System
open Microsoft.FSharp.Core.LanguagePrimitives
open NUnit.Framework

/// <summary>
/// Verifies that two values are equal.
/// If they are not, then an NUnit.Framework.AssertException is thrown.
/// </summary>
/// <param name="expected">The expected value.</param>
/// <param name="actual">The actual value.</param>
let inline areEqual<'T when 'T : equality> (expected : 'T) (actual : 'T) =
    let eqConstraint = Is.EqualTo(expected).Using FastGenericEqualityComparer<'T>
    Assert.That (actual, eqConstraint) |> ignore

type Recorder () =
    let mutable xs = []
    member recorder.Record(x) = xs <- box x :: xs
    member recorder.Values = xs

let inline spy f = 
    let recorder = Recorder()
    (recorder, fun ps -> recorder.Record(ps); f ps)

let inline once<'T> (t: 'T) (recorder:Recorder) =
    areEqual [box t] (recorder.Values)
