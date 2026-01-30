// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace ILAssembler;

internal sealed class IlasmRootCommand : RootCommand
{
    public Argument<string> InputFilePath { get; } =
        new("input-file-path") { Description = "Input IL source file" };

    public Option<string> OutputFilePath { get; } =
        new("--output", "-o") { Description = "Compile to file with specified name (user must provide extension, if any)" };

    public Option<bool> BuildDll { get; } =
        new("--dll") { Description = "Compile to .dll" };

    public Option<bool> BuildExe { get; } =
        new("--exe") { Description = "Compile to .exe (default)" };

    public Option<bool> Debug { get; } =
        new("--debug", "-g") { Description = "Disable JIT optimization, create PDB file, use sequence points from PDB" };

    public Option<string> DebugMode { get; } =
        new("--debug-mode") { Description = "Debug mode: 'impl' (implicit sequence points) or 'opt' (enable JIT optimization)" };

    public Option<bool> Optimize { get; } =
        new("--optimize", "-O") { Description = "Optimize long instructions to short" };

    public Option<bool> Fold { get; } =
        new("--fold") { Description = "Fold identical method bodies into one" };

    public Option<bool> NoLogo { get; } =
        new("--nologo") { Description = "Don't print the logo" };

    public Option<bool> Quiet { get; } =
        new("--quiet", "-q") { Description = "Don't report assembly progress" };

    public Option<bool> NoAutoInherit { get; } =
        new("--noautoinherit") { Description = "Disable inheriting from System.Object by default" };

    public Option<bool> Pdb { get; } =
        new("--pdb") { Description = "Create the PDB file without enabling debug info tracking" };

    public Option<bool> AppContainer { get; } =
        new("--appcontainer") { Description = "Create an AppContainer exe or dll" };

    public Option<bool> Deterministic { get; } =
        new("--deterministic", "--det") { Description = "Produce deterministic outputs" };

    public Option<bool> Clock { get; } =
        new("--clock") { Description = "Measure and report compilation times" };

    public Option<string> KeyFile { get; } =
        new("--key", "-k") { Description = "Compile with strong signature (file contains private key, prefix with @ for key source name)" };

    public Option<string> AssemblyName { get; } =
        new("--aname") { Description = "Override the name of the compiled assembly" };

    public Option<string> IncludePath { get; } =
        new("--include", "-I") { Description = "Set path to search for #include'd files" };

    public Option<int> Subsystem { get; } =
        new("--subsystem") { Description = "Set Subsystem value in the NT Optional header" };

    public Option<string> SubsystemVersion { get; } =
        new("--ssver") { Description = "Set Subsystem version number in the NT Optional header (format: major.minor)" };

    public Option<int> Flags { get; } =
        new("--flags") { Description = "Set CLR ImageFlags value in the CLR header" };

    public Option<int> Alignment { get; } =
        new("--alignment") { Description = "Set FileAlignment value in the NT Optional header" };

    public Option<long> ImageBase { get; } =
        new("--base") { Description = "Set ImageBase value in the NT Optional header (max 2GB for 32-bit images)" };

    public Option<int> StackReserve { get; } =
        new("--stack") { Description = "Set SizeOfStackReserve value in the NT Optional header" };

    public Option<string> MetadataVersion { get; } =
        new("--mdv") { Description = "Set Metadata version string" };

    public Option<string> MetadataStreamVersion { get; } =
        new("--msv") { Description = "Set Metadata stream version (format: major.minor)" };

    public Option<bool> Pe64 { get; } =
        new("--pe64") { Description = "Create a 64bit image (PE32+)" };

    public Option<bool> HighEntropyVa { get; } =
        new("--highentropyva") { Description = "Set High Entropy Virtual Address capable PE32+ images (default for /APPCONTAINER)" };

    // NOTE: NoCorStub is not applicable for pure managed assemblies (used for mixed-mode native stubs)
    public Option<bool> NoCorStub { get; } =
        new("--nocorstub") { Description = "Suppress generation of CORExeMain stub" };

    public Option<bool> StripReloc { get; } =
        new("--stripreloc") { Description = "Indicate that no base relocations are needed" };

    public Option<bool> TargetX64 { get; } =
        new("--x64") { Description = "Target processor: 64bit AMD processor" };

    public Option<bool> TargetArm { get; } =
        new("--arm") { Description = "Target processor: ARM (AArch32) processor" };

    public Option<bool> TargetArm64 { get; } =
        new("--arm64") { Description = "Target processor: ARM64 (AArch64) processor" };

    public Option<bool> Prefer32Bit { get; } =
        new("--32bitpreferred") { Description = "Create a 32BitPreferred image (PE32)" };

    public Option<bool> WaitForDebugger { get; } =
        new("--waitfordebugger") { Description = "Pause to give opportunity to attach debugger" };

    public ParseResult Result { get; private set; } = null!;

    internal const string ProductName = ".NET IL Assembler";

    public IlasmRootCommand() : base(ProductName)
    {
        Arguments.Add(InputFilePath);
        Options.Add(OutputFilePath);
        Options.Add(BuildDll);
        Options.Add(BuildExe);
        Options.Add(Debug);
        Options.Add(DebugMode);
        Options.Add(Optimize);
        Options.Add(Fold);
        Options.Add(NoLogo);
        Options.Add(Quiet);
        Options.Add(NoAutoInherit);
        Options.Add(Pdb);
        Options.Add(AppContainer);
        Options.Add(Deterministic);
        Options.Add(Clock);
        Options.Add(KeyFile);
        Options.Add(AssemblyName);
        Options.Add(IncludePath);
        Options.Add(Subsystem);
        Options.Add(SubsystemVersion);
        Options.Add(Flags);
        Options.Add(Alignment);
        Options.Add(ImageBase);
        Options.Add(StackReserve);
        Options.Add(MetadataVersion);
        Options.Add(MetadataStreamVersion);
        Options.Add(Pe64);
        Options.Add(HighEntropyVa);
        Options.Add(NoCorStub);
        Options.Add(StripReloc);
        Options.Add(TargetX64);
        Options.Add(TargetArm);
        Options.Add(TargetArm64);
        Options.Add(Prefer32Bit);
        Options.Add(WaitForDebugger);

        this.SetAction(result =>
        {
            Result = result;

            if (result.GetValue(WaitForDebugger))
            {
                Console.WriteLine("Waiting for debugger to attach. Press ENTER to continue");
                Console.ReadLine();
            }

            try
            {
                return new Program(this).Run();
            }
            catch (Exception e)
            {
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Error: " + e.Message);
                Console.ResetColor();
                return 1;
            }
        });
    }
}
