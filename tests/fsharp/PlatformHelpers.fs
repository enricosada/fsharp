module PlatformHelpers

type PROCESSOR_ARCHITECTURE = X86 | IA64 | AMD64 | Unknown of string
    with override this.ToString() = match this with X86 -> "x86" | IA64 -> "IA64" | AMD64 -> "AMD64" | Unknown arc -> arc

let parseProcessorArchitecture (s: string) =
    match s.ToUpper() with
    | "X86" -> X86
    | "IA64" -> IA64
    | "AMD64" -> AMD64
    | arc -> Unknown s


//  %~i	    -   expands %i removing any surrounding quotes (")
//  %~fi	-   expands %i to a fully qualified path name
//  %~di	-   expands %i to a drive letter only
//  %~pi	-   expands %i to a path only
//  %~ni	-   expands %i to a file name only
//  %~xi	-   expands %i to a file extension only
//  %~si	-   expanded path contains short names only

let inline (/) a b = System.IO.Path.Combine(a,b)

