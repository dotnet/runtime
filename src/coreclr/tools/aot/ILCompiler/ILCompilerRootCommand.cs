// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.IO;

using Internal.TypeSystem;

namespace ILCompiler
{
    internal sealed class ILCompilerRootCommand : RootCommand
    {
        public Argument<Dictionary<string, string>> InputFilePaths { get; } =
            new("input-file-path", result => Helpers.BuildPathDictionary(result.Tokens, true), false, "Input file(s)") { Arity = ArgumentArity.OneOrMore };
        public Option<Dictionary<string, string>> ReferenceFiles { get; } =
            new(new[] { "--reference", "-r" }, result => Helpers.BuildPathDictionary(result.Tokens, false), true, "Reference file(s) for compilation");
        public Option<string> OutputFilePath { get; } =
            new(new[] { "--out", "-o" }, "Output file path");
        public Option<bool> Optimize { get; } =
            new(new[] { "--optimize", "-O" }, "Enable optimizations");
        public Option<bool> OptimizeSpace { get; } =
            new(new[] { "--optimize-space", "--Os" }, "Enable optimizations, favor code space");
        public Option<bool> OptimizeTime { get; } =
            new(new[] { "--optimize-time", "--Ot" }, "Enable optimizations, favor code speed");
        public Option<string[]> MibcFilePaths { get; } =
            new(new[] { "--mibc", "-m" }, Array.Empty<string>, "Mibc file(s) for profile guided optimization");
        public Option<bool> EnableDebugInfo { get; } =
            new(new[] { "--debug", "-g" }, "Emit debugging information");
        public Option<bool> UseDwarf5 { get; } =
            new(new[] { "--gdwarf-5" }, "Generate source-level debug information with dwarf version 5");
        public Option<bool> NativeLib { get; } =
            new(new[] { "--nativelib" }, "Compile as static or shared library");
        public Option<bool> SplitExeInitialization { get; } =
            new(new[] { "--splitinit" }, "Split initialization of an executable between the library entrypoint and a main entrypoint");
        public Option<string> ExportsFile { get; } =
            new(new[] { "--exportsfile" }, "File to write exported method definitions");
        public Option<string> DgmlLogFileName { get; } =
            new(new[] { "--dgmllog" }, "Save result of dependency analysis as DGML");
        public Option<bool> GenerateFullDgmlLog { get; } =
            new(new[] { "--fulllog" }, "Save detailed log of dependency analysis");
        public Option<string> ScanDgmlLogFileName { get; } =
            new(new[] { "--scandgmllog" }, "Save result of scanner dependency analysis as DGML");
        public Option<bool> GenerateFullScanDgmlLog { get; } =
            new(new[] { "--scanfulllog" }, "Save detailed log of scanner dependency analysis");
        public Option<bool> IsVerbose { get; } =
            new(new[] { "--verbose" }, "Enable verbose logging");
        public Option<string> SystemModuleName { get; } =
            new(new[] { "--systemmodule" }, () => Helpers.DefaultSystemModule, "System module name (default: System.Private.CoreLib)");
        public Option<bool> MultiFile { get; } =
            new(new[] { "--multifile" }, "Compile only input files (do not compile referenced assemblies)");
        public Option<bool> WaitForDebugger { get; } =
            new(new[] { "--waitfordebugger" }, "Pause to give opportunity to attach debugger");
        public Option<bool> Resilient { get; } =
            new(new[] { "--resilient" }, "Ignore unresolved types, methods, and assemblies. Defaults to false");
        public Option<string[]> CodegenOptions { get; } =
            new(new[] { "--codegenopt" }, Array.Empty<string>, "Define a codegen option");
        public Option<string[]> RdXmlFilePaths { get; } =
            new(new[] { "--rdxml" }, Array.Empty<string>, "RD.XML file(s) for compilation");
        public Option<string[]> LinkTrimFilePaths { get; } =
            new(new[] { "--descriptor" }, Array.Empty<string>, "ILLinkTrim.Descriptor file(s) for compilation");
        public Option<string> MapFileName { get; } =
            new(new[] { "--map" }, "Generate a map file");
        public Option<string> MstatFileName { get; } =
            new(new[] { "--mstat" }, "Generate an mstat file");
        public Option<string> MetadataLogFileName { get; } =
            new(new[] { "--metadatalog" }, "Generate a metadata log file");
        public Option<bool> NoMetadataBlocking { get; } =
            new(new[] { "--nometadatablocking" }, "Ignore metadata blocking for internal implementation details");
        public Option<bool> CompleteTypesMetadata { get; } =
            new(new[] { "--completetypemetadata" }, "Generate complete metadata for types");
        public Option<string> ReflectionData { get; } =
            new(new[] { "--reflectiondata" }, "Reflection data to generate (one of: all, none)");
        public Option<bool> ScanReflection { get; } =
            new(new[] { "--scanreflection" }, "Scan IL for reflection patterns");
        public Option<bool> UseScanner { get; } =
            new(new[] { "--scan" }, "Use IL scanner to generate optimized code (implied by -O)");
        public Option<bool> NoScanner { get; } =
            new(new[] { "--noscan" }, "Do not use IL scanner to generate optimized code");
        public Option<string> IlDump { get; } =
            new(new[] { "--ildump" }, "Dump IL assembly listing for compiler-generated IL");
        public Option<bool> EmitStackTraceData { get; } =
            new(new[] { "--stacktracedata" }, "Emit data to support generating stack trace strings at runtime");
        public Option<bool> MethodBodyFolding { get; } =
            new(new[] { "--methodbodyfolding" }, "Fold identical method bodies");
        public Option<string[]> InitAssemblies { get; } =
            new(new[] { "--initassembly" }, Array.Empty<string>, "Assembly(ies) with a library initializer");
        public Option<string[]> AppContextSwitches { get; } =
            new(new[] { "--appcontextswitch" }, Array.Empty<string>, "System.AppContext switches to set (format: 'Key=Value')");
        public Option<string[]> FeatureSwitches { get; } =
            new(new[] { "--feature" }, Array.Empty<string>, "Feature switches to apply (format: 'Namespace.Name=[true|false]'");
        public Option<string[]> RuntimeOptions { get; } =
            new(new[] { "--runtimeopt" }, Array.Empty<string>, "Runtime options to set");
        public Option<int> Parallelism { get; } =
            new(new[] { "--parallelism" }, result =>
            {
                if (result.Tokens.Count > 0)
                    return int.Parse(result.Tokens[0].Value);

                // Limit parallelism to 24 wide at most by default, more parallelism is unlikely to improve compilation speed
                // as many portions of the process are single threaded, and is known to use excessive memory.
                var parallelism = Math.Min(24, Environment.ProcessorCount);

                // On 32bit platforms restrict it more, as virtual address space is quite limited
                if (!Environment.Is64BitProcess)
                    parallelism = Math.Min(4, parallelism);

                return parallelism;
            }, true, "Maximum number of threads to use during compilation");
        public Option<string> InstructionSet { get; } =
            new(new[] { "--instruction-set" }, "Instruction set to allow or disallow");
        public Option<string> Guard { get; } =
            new(new[] { "--guard" }, "Enable mitigations. Options: 'cf': CFG (Control Flow Guard, Windows only)");
        public Option<bool> Dehydrate { get; } =
            new(new[] { "--dehydrate" }, "Dehydrate runtime data structures");
        public Option<bool> PreinitStatics { get; } =
            new(new[] { "--preinitstatics" }, "Interpret static constructors at compile time if possible (implied by -O)");
        public Option<bool> NoPreinitStatics { get; } =
            new(new[] { "--nopreinitstatics" }, "Do not interpret static constructors at compile time");
        public Option<string[]> SuppressedWarnings { get; } =
            new(new[] { "--nowarn" }, Array.Empty<string>, "Disable specific warning messages");
        public Option<bool> SingleWarn { get; } =
            new(new[] { "--singlewarn" }, "Generate single AOT/trimming warning per assembly");
        public Option<bool> NoTrimWarn { get; } =
            new(new[] { "--notrimwarn" }, "Disable warnings related to trimming");
        public Option<bool> NoAotWarn { get; } =
            new(new[] { "--noaotwarn" }, "Disable warnings related to AOT");
        public Option<string[]> SingleWarnEnabledAssemblies { get; } =
            new(new[] { "--singlewarnassembly" }, Array.Empty<string>, "Generate single AOT/trimming warning for given assembly");
        public Option<string[]> SingleWarnDisabledAssemblies { get; } =
            new(new[] { "--nosinglewarnassembly" }, Array.Empty<string>, "Expand AOT/trimming warnings for given assembly");
        public Option<string[]> DirectPInvokes { get; } =
            new(new[] { "--directpinvoke" }, Array.Empty<string>, "PInvoke to call directly");
        public Option<string[]> DirectPInvokeLists { get; } =
            new(new[] { "--directpinvokelist" }, Array.Empty<string>, "File with list of PInvokes to call directly");
        public Option<int> MaxGenericCycle { get; } =
            new(new[] { "--maxgenericcycle" }, () => CompilerTypeSystemContext.DefaultGenericCycleCutoffPoint, "Max depth of generic cycle");
        public Option<string[]> RootedAssemblies { get; } =
            new(new[] { "--root" }, Array.Empty<string>, "Fully generate given assembly");
        public Option<IEnumerable<string>> ConditionallyRootedAssemblies { get; } =
            new(new[] { "--conditionalroot" }, result => ILLinkify(result.Tokens), true, "Fully generate given assembly if it's used");
        public Option<IEnumerable<string>> TrimmedAssemblies { get; } =
            new(new[] { "--trim" }, result => ILLinkify(result.Tokens), true, "Trim the specified assembly");
        public Option<bool> RootDefaultAssemblies { get; } =
            new(new[] { "--defaultrooting" }, "Root assemblies that are not marked [IsTrimmable]");
        public Option<TargetArchitecture> TargetArchitecture { get; } =
            new(new[] { "--targetarch" }, result => Helpers.GetTargetArchitecture(result.Tokens.Count > 0 ? result.Tokens[0].Value : null), true, "Target architecture for cross compilation");
        public Option<TargetOS> TargetOS { get; } =
            new(new[] { "--targetos" }, result => Helpers.GetTargetOS(result.Tokens.Count > 0 ? result.Tokens[0].Value : null), true, "Target OS for cross compilation");
        public Option<string> JitPath { get; } =
            new(new[] { "--jitpath" }, "Path to JIT compiler library");
        public Option<string> SingleMethodTypeName { get; } =
            new(new[] { "--singlemethodtypename" }, "Single method compilation: assembly-qualified name of the owning type");
        public Option<string> SingleMethodName { get; } =
            new(new[] { "--singlemethodname" }, "Single method compilation: name of the method");
        public Option<string[]> SingleMethodGenericArgs { get; } =
            new(new[] { "--singlemethodgenericarg" }, "Single method compilation: generic arguments to the method");
        public Option<string> MakeReproPath { get; } =
            new(new[] { "--make-repro-path" }, "Path where to place a repro package");
        public Option<string[]> UnmanagedEntryPointsAssemblies { get; } =
            new(new[] { "--generateunmanagedentrypoints" }, Array.Empty<string>, "Generate unmanaged entrypoints for a given assembly");

        public OptimizationMode OptimizationMode { get; private set; }
        public ParseResult Result;

        public ILCompilerRootCommand(string[] args) : base(".NET Native IL Compiler")
        {
            AddArgument(InputFilePaths);
            AddOption(ReferenceFiles);
            AddOption(OutputFilePath);
            AddOption(Optimize);
            AddOption(OptimizeSpace);
            AddOption(OptimizeTime);
            AddOption(MibcFilePaths);
            AddOption(EnableDebugInfo);
            AddOption(UseDwarf5);
            AddOption(NativeLib);
            AddOption(SplitExeInitialization);
            AddOption(ExportsFile);
            AddOption(DgmlLogFileName);
            AddOption(GenerateFullDgmlLog);
            AddOption(ScanDgmlLogFileName);
            AddOption(GenerateFullScanDgmlLog);
            AddOption(IsVerbose);
            AddOption(SystemModuleName);
            AddOption(MultiFile);
            AddOption(WaitForDebugger);
            AddOption(Resilient);
            AddOption(CodegenOptions);
            AddOption(RdXmlFilePaths);
            AddOption(LinkTrimFilePaths);
            AddOption(MapFileName);
            AddOption(MstatFileName);
            AddOption(MetadataLogFileName);
            AddOption(NoMetadataBlocking);
            AddOption(CompleteTypesMetadata);
            AddOption(ReflectionData);
            AddOption(ScanReflection);
            AddOption(UseScanner);
            AddOption(NoScanner);
            AddOption(IlDump);
            AddOption(EmitStackTraceData);
            AddOption(MethodBodyFolding);
            AddOption(InitAssemblies);
            AddOption(AppContextSwitches);
            AddOption(FeatureSwitches);
            AddOption(RuntimeOptions);
            AddOption(Parallelism);
            AddOption(InstructionSet);
            AddOption(Guard);
            AddOption(Dehydrate);
            AddOption(PreinitStatics);
            AddOption(NoPreinitStatics);
            AddOption(SuppressedWarnings);
            AddOption(SingleWarn);
            AddOption(NoTrimWarn);
            AddOption(NoAotWarn);
            AddOption(SingleWarnEnabledAssemblies);
            AddOption(SingleWarnDisabledAssemblies);
            AddOption(DirectPInvokes);
            AddOption(DirectPInvokeLists);
            AddOption(MaxGenericCycle);
            AddOption(RootedAssemblies);
            AddOption(ConditionallyRootedAssemblies);
            AddOption(TrimmedAssemblies);
            AddOption(RootDefaultAssemblies);
            AddOption(TargetArchitecture);
            AddOption(TargetOS);
            AddOption(JitPath);
            AddOption(SingleMethodTypeName);
            AddOption(SingleMethodName);
            AddOption(SingleMethodGenericArgs);
            AddOption(MakeReproPath);
            AddOption(UnmanagedEntryPointsAssemblies);

            this.SetHandler(context =>
            {
                Result = context.ParseResult;

                if (context.ParseResult.GetValue(OptimizeSpace))
                {
                    OptimizationMode = OptimizationMode.PreferSize;
                }
                else if (context.ParseResult.GetValue(OptimizeTime))
                {
                    OptimizationMode = OptimizationMode.PreferSpeed;
                }
                else if (context.ParseResult.GetValue(Optimize))
                {
                    OptimizationMode = OptimizationMode.Blended;
                }
                else
                {
                    OptimizationMode = OptimizationMode.None;
                }

                try
                {
                    string makeReproPath = context.ParseResult.GetValue(MakeReproPath);
                    if (makeReproPath != null)
                    {
                        // Create a repro package in the specified path
                        // This package will have the set of input files needed for compilation
                        // + the original command line arguments
                        // + a rsp file that should work to directly run out of the zip file

                        Helpers.MakeReproPackage(makeReproPath, context.ParseResult.GetValue(OutputFilePath), args, context.ParseResult,
                            inputOptions : new[] { "r", "reference", "m", "mibc", "rdxml", "directpinvokelist", "descriptor" },
                            outputOptions : new[] { "o", "out", "exportsfile" });
                    }

                    context.ExitCode = new Program(this).Run();
                }
#if DEBUG
                catch (CodeGenerationFailedException ex) when (DumpReproArguments(ex))
                {
                    throw new NotSupportedException(); // Unreachable
                }
#else
                catch (Exception e)
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.Error.WriteLine("Error: " + e.Message);
                    Console.Error.WriteLine(e.ToString());

                    Console.ResetColor();

                    context.ExitCode = 1;
                }
#endif
            });
        }

        public static IEnumerable<Action<HelpContext>> GetExtendedHelp(HelpContext _)
        {
            foreach (Action<HelpContext> sectionDelegate in HelpBuilder.Default.GetLayout())
                yield return sectionDelegate;

            yield return _ =>
            {
                Console.WriteLine("Options may be passed on the command line, or via response file. On the command line switch values may be specified by passing " +
                    "the option followed by a space followed by the value of the option, or by specifying a : between option and switch value. A response file " +
                    "is specified by passing the @ symbol before the response file name. In a response file all options must be specified on their own lines, and " +
                    "only the : syntax for switches is supported.\n");

                Console.WriteLine("Use the '--' option to disambiguate between input files that have begin with -- and options. After a '--' option, all arguments are " +
                    "considered to be input files. If no input files begin with '--' then this option is not necessary.\n");

                string[] ValidArchitectures = new string[] { "arm", "arm64", "x86", "x64" };
                string[] ValidOS = new string[] { "windows", "linux", "freebsd", "osx", "maccatalyst", "ios", "iossimulator", "tvos", "tvossimulator" };

                Console.WriteLine("Valid switches for {0} are: '{1}'. The default value is '{2}'\n", "--targetos", string.Join("', '", ValidOS), Helpers.GetTargetOS(null).ToString().ToLowerInvariant());

                Console.WriteLine(string.Format("Valid switches for {0} are: '{1}'. The default value is '{2}'\n", "--targetarch", string.Join("', '", ValidArchitectures), Helpers.GetTargetArchitecture(null).ToString().ToLowerInvariant()));

                Console.WriteLine("The allowable values for the --instruction-set option are described in the table below. Each architecture has a different set of valid " +
                    "instruction sets, and multiple instruction sets may be specified by separating the instructions sets by a ','. For example 'avx2,bmi,lzcnt'");

                foreach (string arch in ValidArchitectures)
                {
                    Console.Write(arch);
                    Console.Write(": ");

                    TargetArchitecture targetArch = Helpers.GetTargetArchitecture(arch);
                    bool first = true;
                    foreach (var instructionSet in Internal.JitInterface.InstructionSetFlags.ArchitectureToValidInstructionSets(targetArch))
                    {
                        // Only instruction sets with are specifiable should be printed to the help text
                        if (instructionSet.Specifiable)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                Console.Write(", ");
                            }
                            Console.Write(instructionSet.Name);
                        }
                    }

                    Console.WriteLine();
                }

                Console.WriteLine();
                Console.WriteLine("The following CPU names are predefined groups of instruction sets and can be used in --instruction-set too:");
                Console.WriteLine(string.Join(", ", Internal.JitInterface.InstructionSetFlags.AllCpuNames));
            };
        }

        private static IEnumerable<string> ILLinkify(IReadOnlyList<Token> tokens)
        {
            if (tokens.Count == 0)
            {
                yield return string.Empty;
                yield break;
            }

            foreach(Token token in tokens)
            {
                string rootedAssembly = token.Value;

                // For compatibility with IL Linker, the parameter could be a file name or an assembly name.
                // This is the logic IL Linker uses to decide how to interpret the string. Really.
                string simpleName;
                if (File.Exists(rootedAssembly))
                    simpleName = Path.GetFileNameWithoutExtension(rootedAssembly);
                else
                    simpleName = rootedAssembly;
                yield return simpleName;
            }
        }

#if DEBUG
            private static bool DumpReproArguments(CodeGenerationFailedException ex)
            {
                Console.WriteLine("To repro, add following arguments to the command line:");

                MethodDesc failingMethod = ex.Method;

                var formatter = new CustomAttributeTypeNameFormatter((IAssemblyDesc)failingMethod.Context.SystemModule);

                Console.Write($"--singlemethodtypename \"{formatter.FormatName(failingMethod.OwningType, true)}\"");
                Console.Write($" --singlemethodname {failingMethod.Name}");

                for (int i = 0; i < failingMethod.Instantiation.Length; i++)
                    Console.Write($" --singlemethodgenericarg \"{formatter.FormatName(failingMethod.Instantiation[i], true)}\"");

                return false;
            }
#endif
    }
}
