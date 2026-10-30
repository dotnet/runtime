// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace ILDisassembler;

internal sealed class IldasmRootCommand : RootCommand
{
    public Argument<string> InputFilePath { get; } =
        new("input-file-path") { Description = "Input PE file (.dll or .exe) to disassemble" };

    // Output redirection
    public Option<string> OutputFilePath { get; } =
        new("--output", "-o") { Description = "Direct output to file rather than to console" };

    public Option<bool> Html { get; } =
        new("--html") { Description = "Output in HTML format (valid with --output option only)" };

    public Option<bool> Rtf { get; } =
        new("--rtf") { Description = "Output in rich text format (valid with --output option only)" };

    // File/console output
    public Option<bool> ShowBytes { get; } =
        new("--bytes") { Description = "Show actual bytes (in hex) as instruction comments" };

    public Option<bool> RawExceptionHandling { get; } =
        new("--raweh") { Description = "Show exception handling clauses in raw form" };

    public Option<bool> ShowTokens { get; } =
        new("--tokens") { Description = "Show metadata tokens of classes and members" };

    public Option<bool> ShowSource { get; } =
        new("--source") { Description = "Show original source lines as comments (requires PDB)" };

    public Option<bool> ShowLineNumbers { get; } =
        new("--linenum") { Description = "Include references to original source lines" };

    public Option<string> Visibility { get; } =
        new("--visibility") { Description = "Only disassemble the items with specified visibility: <vis>[+<vis>...] (<vis> = PUB|PRI|FAM|ASM|FAA|FOA|PSC)" };

    public Option<bool> PublicOnly { get; } =
        new("--pubonly") { Description = "Only disassemble the public items (same as --visibility PUB)" };

    public Option<bool> QuoteAllNames { get; } =
        new("--quoteallnames") { Description = "Include all names into single quotes" };

    public Option<bool> NoCustomAttributes { get; } =
        new("--noca") { Description = "Suppress output of custom attributes" };

    public Option<bool> CustomAttributesVerbal { get; } =
        new("--caverbal") { Description = "Output CA blobs in verbal form (default: binary form)" };

    public Option<bool> R2RNativeMetadata { get; } =
        new("--r2rnativemetadata") { Description = "Output the metadata from the R2R Native manifest" };

    // EXE and DLL files
    public Option<bool> Utf8 { get; } =
        new("--utf8") { Description = "Use UTF-8 encoding for output (default)" };

    public Option<bool> Unicode { get; } =
        new("--unicode") { Description = "Use UNICODE encoding for output" };

    public Option<bool> NoIL { get; } =
        new("--noil") { Description = "Suppress IL assembler code output" };

    public Option<bool> ForwardDeclarations { get; } =
        new("--forward") { Description = "Use forward class declaration" };

    public Option<bool> TypeList { get; } =
        new("--typelist") { Description = "Output full list of types (to preserve type ordering in round-trip)" };

    public Option<bool> Headers { get; } =
        new("--headers") { Description = "Include file headers information in the output" };

    public Option<string> Item { get; } =
        new("--item") { Description = "Disassemble the specified item only: <class>[::<method>[(<sig>)]]" };

    public Option<bool> Stats { get; } =
        new("--stats") { Description = "Include statistics on the image" };

    public Option<bool> ClassList { get; } =
        new("--classlist") { Description = "Include list of classes defined in the module" };

    public Option<bool> All { get; } =
        new("--all") { Description = "Combination of --headers, --bytes, --stats, --classlist, --tokens" };

    public Option<bool> Metadata { get; } =
        new("--metadata") { Description = "Show MetaData" };

    public Option<string[]> MetadataFilters { get; } =
        new("--metadata-filter") { Description = "Show MetaData, selecting what is shown (implies --metadata): MDHEADER|HEX|CSV|UNREX|SCHEMA|RAW|HEAPS|VALIDATE" };

    // Common
    public Option<bool> NoLogo { get; } =
        new("--nologo") { Description = "Don't print the logo" };

    public Option<bool> Quiet { get; } =
        new("--quiet", "-q") { Description = "Don't report disassembly progress" };

    public Option<bool> WaitForDebugger { get; } =
        new("--waitfordebugger") { Description = "Pause to give opportunity to attach debugger" };

    internal const string ProductName = ".NET IL Disassembler";

    public IldasmRootCommand() : base(ProductName)
    {
        Arguments.Add(InputFilePath);
        Options.Add(OutputFilePath);
        Options.Add(Html);
        Options.Add(Rtf);
        Options.Add(ShowBytes);
        Options.Add(RawExceptionHandling);
        Options.Add(ShowTokens);
        Options.Add(ShowSource);
        Options.Add(ShowLineNumbers);
        Options.Add(Visibility);
        Options.Add(PublicOnly);
        Options.Add(QuoteAllNames);
        Options.Add(NoCustomAttributes);
        Options.Add(CustomAttributesVerbal);
        Options.Add(R2RNativeMetadata);
        Options.Add(Utf8);
        Options.Add(Unicode);
        Options.Add(NoIL);
        Options.Add(ForwardDeclarations);
        Options.Add(TypeList);
        Options.Add(Headers);
        Options.Add(Item);
        Options.Add(Stats);
        Options.Add(ClassList);
        Options.Add(All);
        Options.Add(Metadata);
        Options.Add(MetadataFilters);
        Options.Add(NoLogo);
        Options.Add(Quiet);
        Options.Add(WaitForDebugger);
    }
}
