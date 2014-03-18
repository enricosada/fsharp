#light
namespace Microsoft.FSharp
open System.Reflection

[<assembly:AssemblyDescription("FSharp.Compiler.dll")>]
[<assembly:AssemblyTitle("FSharp.Compiler.dll")>]
[<assembly:AssemblyCopyright("\169 Microsoft Corporation and other contributors.  Apache 2.0 License.")>]
[<assembly:AssemblyProduct("F# (open source edition)")>]

#if NO_STRONG_NAMES
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("fsc")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("fsi")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("fsiAnyCpu")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.LanguageService")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.ProjectSystem.Base")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.ProjectSystem.FSharp")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.ProjectSystem.PropertyPages")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.Compiler.Interactive.Settings")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.Compiler.Server.Shared")>]

// Note: internals visible to unit test DLLs in Retail (and all) builds.
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Test")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Salsa")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Unittests")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("SystematicUnitTests")>]
#if BUILDING_WITH_LKG
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("fsc-proto")>]
#endif
#endif
#if STRONG_NAME_AND_DELAY_SIGN_FSHARP_COMPILER_WITH_MSFT_KEY

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("fsc, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("fsi, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("fsiAnyCpu, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.LanguageService, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.LanguageService.Base, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.ProjectSystem.Base, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.ProjectSystem.FSharp, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.ProjectSystem.PropertyPages, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.Compiler.Interactive.Settings, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.Compiler.Server.Shared, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Salsa, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Unittests, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("SystematicUnitTests, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Test, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
#endif

#if STRONG_NAME_FSHARP_COMPILER_WITH_TEST_KEY
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("fsc, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("fsi, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("fsiAnyCpu, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.LanguageService, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.LanguageService.Base, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.ProjectSystem.Base, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.ProjectSystem.FSharp, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.ProjectSystem.PropertyPages, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.Compiler.Interactive.Settings, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.Compiler.Server.Shared, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Salsa, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Unittests, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("SystematicUnitTests, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Test, PublicKey=002400000480000094000000060200000024000052534131000400000100010077d32e043d184cf8cebf177201ec6fad091581a3a639a0534f1c4ebb3ab847a6b6636990224a04cf4bd1aec51ecec44cf0c8922eb5bb2ee65ec3fb9baa87e141042c96ce414f98af33508c7e24dab5b068aa802f6693881537ee0efcb5d3f1c9aaf8215ac42e92ba9a5a02574d6890d07464cb2f338b043b1c4ffe98efe069ee")>]
#endif


#if BUILDING_WITH_LKG
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("fsc-proto, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")>]
#endif

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FSharp.Compiler.Unittests")>]

do()
