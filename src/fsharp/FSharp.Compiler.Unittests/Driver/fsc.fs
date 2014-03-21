module FSharp.Compiler.Unittests.Driver.Fsc

open System
open FSharp.Core.Unittests.LibraryTestFx
open NUnit.Framework

open Microsoft.FSharp.Compiler.Driver

open FsAssert

module ``fsc aa`` =
    open System.IO
    
    open Microsoft.FSharp.Compiler.Driver

    exception FscExit of int

    let cd dir =
        let old = System.IO.Directory.GetCurrentDirectory()
        System.IO.Directory.SetCurrentDirectory dir
        {new IDisposable with member x.Dispose() = System.IO.Directory.SetCurrentDirectory old }

    let redirect out err =
        Console.SetOut out
        Console.SetError err
        {new IDisposable with 
            member x.Dispose() = 
                let output = new StreamWriter(Console.OpenStandardOutput()) in output.AutoFlush <- true
                Console.SetOut(output)
                let error = new StreamWriter(Console.OpenStandardError()) in error.AutoFlush <- true
                Console.SetOut(error) }

    let logtomemory () = 
        let stream = new MemoryStream()
        let writer = new StreamWriter(stream)
        writer.AutoFlush <- true
        writer

    let readfrom (memory: StreamWriter) = 
        memory.BaseStream.Position <- 0L; 
        let reader = new StreamReader(memory.BaseStream)
        [ while reader.Peek() >= 0 do  yield reader.ReadLine() ]

    type FscOutput = { 
        warnings: string list;
        consoleout: string list;
        outputfile: FileInfo; }

    let fsc (args: string list) (name, code) =
        let quote = sprintf "\"%s\""
        let makeTempDir () =
            let dir = Path.Combine (Path.GetTempPath(), Path.GetRandomFileName())
            Directory.CreateDirectory(dir) |> ignore
            dir
            
        let basedir = makeTempDir ()
        let sourcefile = Path.Combine [| basedir; name |]
        let outPath = Path.ChangeExtension (sourcefile, ".dll")

        let parseWarnings = List.filter (fun (x: string) -> x.StartsWith("warning"))

        let fscmainwrapper a =
            use cd = cd basedir
            let out, err = logtomemory (), logtomemory()
            use log = redirect out err
            let exit =
                let exiter = { new Microsoft.FSharp.Compiler.ErrorLogger.Exiter with member x.Exit n = raise (FscExit(n)) }
                try
                    mainCompile (a |> List.toArray, true, exiter); 0
                with FscExit n -> n
            let consoleout, consoleerr = out |> readfrom, err |> readfrom
            (exit, {warnings = consoleerr |> parseWarnings; outputfile = FileInfo(outPath); consoleout = consoleout})

        File.WriteAllText(sourcefile, code)
        (sprintf "-o:%s" (outPath |> quote)) :: args @ [sourcefile]
        |> fscmainwrapper

    let fsc_library args input = fsc ("--target:library" :: args) input

    [<Test>] 
    let ``should compile library without warnings`` () = 
        let code =
            """
                module test
                let add x y = x + y
            """
        let r, o = fsc_library [] ("add.fs", code)
        r |> Check.areEqual 0
        o.outputfile.Exists |> Check.areEqual true
        (o.warnings, o.consoleout) |> Check.areEqual ([], [])


    [<Test>] 
    let ``should set file version info on generated file`` () = 
        let code =
            """
                namespace CST.RI.Anshun
                open System.Reflection
                open System.Runtime.CompilerServices
                open System.Runtime.InteropServices
                [<assembly: AssemblyTitle("CST.RI.Anshun.TreloarStation")>]
                [<assembly: AssemblyDescription("Assembly is a part of Restricted Intelligence of Anshun planet")>]
                [<assembly: AssemblyConfiguration("RELEASE")>]
                [<assembly: AssemblyCompany("Compressed Space Transport")>]
                [<assembly: AssemblyProduct("CST.RI.Anshun")>]
                [<assembly: AssemblyCopyright("Copyright © Compressed Space Transport 2380")>]
                [<assembly: AssemblyTrademark("CST ™")>]
                [<assembly: AssemblyVersion("12.34.56.78")>]
                [<assembly: AssemblyFileVersion("99.88.77.66")>]
                [<assembly: AssemblyInformationalVersion("17.56.2912.14")>]
                ()
            """
        let r,o = fsc_library [] ("test.fs", code)
        r |> Check.areEqual 0
        o.outputfile.Exists |> Check.areEqual true
        let fv = System.Diagnostics.FileVersionInfo.GetVersionInfo(o.outputfile.FullName)
        fv.CompanyName |> Check.areEqual "Compressed Space Transport"
        fv.FileVersion |> Check.areEqual "99.88.77.66"
        (fv.FileMajorPart, fv.FileMinorPart, fv.FileBuildPart, fv.FilePrivatePart) |> Check.areEqual (99,88,77,66)
        fv.ProductVersion |> Check.areEqual "17.56.2912.14"
        (fv.ProductMajorPart, fv.ProductMinorPart, fv.ProductBuildPart, fv.ProductPrivatePart) |> Check.areEqual (17,56,2912,14)
        fv.LegalCopyright |> Check.areEqual "Copyright © Compressed Space Transport 2380"
        fv.LegalTrademarks |> Check.areEqual "CST ™"
        o.warnings |> Check.areEqual []

    [<Test>] 
    let ``should raise warning FS2003 if AssemblyInformationalVersion has invalid version`` () = 
        let code =
            """
                namespace CST.RI.Anshun
                open System.Reflection
                [<assembly: AssemblyVersion("4.5.6.7")>]
                [<assembly: AssemblyInformationalVersion("45.2048.main1.2-hotfix (upgrade Second Chance security)")>]
                ()
            """
        let r,o = fsc_library [] ("test.fs", code)
        r |> Check.areEqual 0
        o.outputfile.Exists |> Check.areEqual true
        let fv = System.Diagnostics.FileVersionInfo.GetVersionInfo(o.outputfile.FullName)
        fv.ProductVersion |> Check.areEqual "45.2048.main1.2-hotfix (upgrade Second Chance security)"
        (fv.ProductMajorPart, fv.ProductMinorPart, fv.ProductBuildPart, fv.ProductPrivatePart) |> Check.areEqual (45,2048,0,0)
        o.warnings |> Check.areEqual ["warning FS2003: An System.Reflection.AssemblyInformationalVersionAttribute specified version '45.2048.main1.2-hotfix (upgrade Second Chance security)', but this value is invalid and has been ignored"]
