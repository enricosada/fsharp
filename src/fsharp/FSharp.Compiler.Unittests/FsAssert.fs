module FsAssert

open System
open Microsoft.FSharp.Core.LanguagePrimitives
open NUnit.Framework

let inline failtest message = Assert.Fail message

let inline failtestf fmt = Printf.kprintf failtest fmt


type Recorder () =
    let mutable xs = []
    member recorder.Record(x) = xs <- box x :: xs
    member recorder.Values = xs

let inline spy f = 
    let recorder = Recorder()
    (recorder, fun ps -> recorder.Record(ps); f ps)


[<RequireQualifiedAccess>]
module Expect =

    let inline once check (recorder: Recorder) =
        match recorder.Values with
        | [] -> failtest "expected one invocation"
        | [x] -> 
            match x with
            | :? _ as a -> check a
            | _ -> invalidArg "check" (sprintf "expected function with type %s but was %s" typeof<_>.FullName (x.GetType().FullName))
        | v -> failtestf "expected one invocation but were %d: %A" v.Length v


    let inline never (r: Recorder) = 
        match r.Values with
        | [] -> ()
        | v -> failtestf "no invocations expected but were %d: '%A'" v.Length v



[<RequireQualifiedAccess>]
module Check =

    let inline areEqual<'T when 'T : equality> (expected : 'T) (actual : 'T) =
        let eqConstraint = Is.EqualTo(expected).Using FastGenericEqualityComparer<'T>
        Assert.That (actual, eqConstraint, (sprintf "Expected '%A' but was '%A'" expected actual))

    let (|Warning|_|) (exn: Exception) =
        match exn with
        | :? Microsoft.FSharp.Compiler.ErrorLogger.Error as e -> let n,d = e.Data0 in Some (n,d)
        | :? Microsoft.FSharp.Compiler.ErrorLogger.NumberedError as e -> let n,d = e.Data0 in Some (n,d)
        | _ -> None

