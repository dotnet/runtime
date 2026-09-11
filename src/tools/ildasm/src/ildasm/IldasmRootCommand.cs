// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace ILDisassembler;

internal sealed class IldasmRootCommand : RootCommand
{
    // Input file
    public Argument<string> InputFilePath { get; } =
        new("input-file-path") { Description = "Input PE file (.dll or .exe) to disassemble" };

    // Output redirection options
    public Option<string?> OutputFilePath { get; } =
        new("-o", "--output") { Description = "Direct output to file rather than to console" };

    public Option<bool> Html { get; } =
        new("--html", "--htm") { Description = "Output in HTML format (valid with --output option only)" };

    public Option<bool> Rtf { get; } =
        new("--rtf") { Description = "Output in rich text format (valid with --output option only)" };

    // File/console output options
    public Option<bool> ShowBytes { get; } =
        new("--bytes", "--byt") { Description = "Show actual bytes (in hex) as instruction comments" };

    public Option<bool> RawExceptionHandling { get; } =
        new("--raweh") { Description = "Show exception handling clauses in raw form" };

    public Option<bool> ShowTokens { get; } =
        new("--tokens", "--tok") { Description = "Show metadata tokens of classes and members" };

    public Option<bool> ShowSource { get; } =
        new("--source", "--src") { Description = "Show original source lines as comments (requires PDB)" };

    public Option<bool> ShowLineNumbers { get; } =
        new("--linenum", "--lin") { Description = "Include references to original source lines" };

    public Option<string?> Visibility { get; } =
        new("--visibility", "--vis") { Description = "Only disassemble items with specified visibility (PUB|PRI|FAM|ASM|FAA|FOA|PSC)" };

    public Option<bool> PublicOnly { get; } =
        new("--pubonly", "--pub") { Description = "Only disassemble the public items (same as --visibility=PUB)" };

    public Option<bool> QuoteAllNames { get; } =
        new("--quoteallnames", "--quo") { Description = "Include all names into single quotes" };

    public Option<bool> NoCustomAttributes { get; } =
        new("--noca") { Description = "Suppress output of custom attributes" };

    public Option<bool> CustomAttributesVerbal { get; } =
        new("--caverbal", "--cav") { Description = "Output CA blobs in verbal form (default: binary form)" };

    public Option<bool> R2RNativeMetadata { get; } =
        new("--r2rnativemetadata", "--r2r") { Description = "Output the metadata from the R2R Native manifest" };

    // EXE/DLL file options
    public Option<bool> Utf8 { get; } =
        new("--utf8") { Description = "Use UTF-8 encoding for output (default)" };

    public Option<bool> Unicode { get; } =
        new("--unicode", "--uni") { Description = "Use UNICODE encoding for output" };

    public Option<bool> NoIL { get; } =
        new("--noil") { Description = "Suppress IL assembler code output" };

    public Option<bool> ForwardDeclarations { get; } =
        new("--forward", "--for") { Description = "Use forward class declaration" };

    public Option<bool> TypeList { get; } =
        new("--typelist", "--typ") { Description = "Output full list of types (to preserve type ordering in round-trip)" };

    public Option<bool> Headers { get; } =
        new("--headers", "--hea") { Description = "Include file headers information in the output" };

    public Option<string?> Item { get; } =
        new("--item", "--ite") { Description = "Disassemble the specified item only (<class>[::method[(sig)]])" };

    public Option<bool> Stats { get; } =
        new("--stats", "--sta") { Description = "Include statistics on the image" };

    public Option<bool> ClassList { get; } =
        new("--classlist", "--cla") { Description = "Include list of classes defined in the module" };

    public Option<bool> All { get; } =
        new("--all") { Description = "Combination of --headers, --bytes, --stats, --classlist, --tokens" };

    public Option<string?> Metadata { get; } =
        new("--metadata", "--met") { Description = "Show MetaData (MDHEADER|HEX|CSV|UNREX|SCHEMA|RAW|HEAPS|VALIDATE)" };

    // Common options
    public Option<bool> NoLogo { get; } =
        new("--nologo", "--nol") { Description = "Don't display the logo" };

    public Option<bool> Quiet { get; } =
        new("--quiet", "-q") { Description = "Don't report disassembly progress" };

    public ParseResult Result { get; private set; } = null!;

    internal const string ProductName = ".NET IL Disassembler";

    public IldasmRootCommand() : base(ProductName)
    {
        Arguments.Add(InputFilePath);

        // Output redirection
        Options.Add(OutputFilePath);
        Options.Add(Html);
        Options.Add(Rtf);

        // File/console output options
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

        // EXE/DLL options
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

        // Common options
        Options.Add(NoLogo);
        Options.Add(Quiet);

        this.SetAction(result =>
        {
            Result = result;
            return new Program(this).Run();
        });
    }
}
