// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Parsing;

using Internal.TypeSystem;

namespace ILCompiler
{
    internal sealed class ILCompilerRootCommand : RootCommand
    {
        public Argument<Dictionary<string, string>> InputFilePaths { get; } =
            new("input-file-path") { CustomParser = result => Helpers.BuildPathDictionary(result.Tokens, true), Description = "Input file(s)", Arity = ArgumentArity.OneOrMore };
        public Option<Dictionary<string, string>> ReferenceFiles { get; } =
            new("--reference", "-r") { CustomParser = result => Helpers.BuildPathDictionary(result.Tokens, false), DefaultValueFactory = result => Helpers.BuildPathDictionary(result.Tokens, false), Description = "Reference file(s) for compilation" };
        public Option<string> OutputFilePath { get; } =
            new("--out", "-o") { Description = "Output file path" };
        public Option<bool> Optimize { get; } =
            new("--optimize", "-O") { Description = "Enable optimizations" };
        public Option<bool> OptimizeSpace { get; } =
            new("--optimize-space", "--Os") { Description = "Enable optimizations, favor code space" };
        public Option<bool> OptimizeTime { get; } =
            new("--optimize-time", "--Ot") { Description = "Enable optimizations, favor code speed" };
        public Option<string[]> MibcFilePaths { get; } =
            new("--mibc", "-m") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Mibc file(s) for profile guided optimization" };
        public Option<MethodLayoutAlgorithm> MethodLayout { get; } =
            new("--method-layout") { CustomParser = MakeMethodLayoutAlgorithm, DefaultValueFactory = MakeMethodLayoutAlgorithm, Description = "Layout algorithm used by profile-driven optimization for arranging methods in a file.", HelpName = "arg" };
        public Option<FileLayoutAlgorithm> FileLayout { get; } =
            new("--file-layout") { CustomParser = MakeFileLayoutAlgorithm, DefaultValueFactory = MakeFileLayoutAlgorithm, Description = "Layout algorithm used by profile-driven optimization for arranging non-method contents in a file.", HelpName = "arg" };
        public Option<string[]> SatelliteFilePaths { get; } =
            new("--satellite") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Satellite assemblies associated with inputs/references" };
        public Option<bool> EnableDebugInfo { get; } =
            new("--debug", "-g") { Description = "Emit debugging information" };
        public Option<bool> UseDwarf5 { get; } =
            new("--gdwarf-5") { Description = "Generate source-level debug information with dwarf version 5" };
        public Option<bool> NativeLib { get; } =
            new("--nativelib") { Description = "Compile as static or shared library" };
        public Option<bool> SplitExeInitialization { get; } =
            new("--splitinit") { Description = "Split initialization of an executable between the library entrypoint and a main entrypoint" };
        public Option<string> ExportsFile { get; } =
            new("--exportsfile") { Description = "File to write exported symbol and method definitions" };
        public Option<bool> ExportUnmanagedEntryPoints { get; } =
            new("--export-unmanaged-entrypoints") { Description = "Controls whether the named UnmanagedCallersOnly methods are exported" };
        public Option<string[]> ExportDynamicSymbols { get; } =
            new("--export-dynamic-symbol") { Description = "Add dynamic export symbol to exports file" };
        public Option<string> DgmlLogFileName { get; } =
            new("--dgmllog") { Description = "Save result of dependency analysis as DGML" };
        public Option<bool> GenerateFullDgmlLog { get; } =
            new("--fulllog") { Description = "Save detailed log of dependency analysis" };
        public Option<string> ScanDgmlLogFileName { get; } =
            new("--scandgmllog") { Description = "Save result of scanner dependency analysis as DGML" };
        public Option<bool> GenerateFullScanDgmlLog { get; } =
            new("--scanfulllog") { Description = "Save detailed log of scanner dependency analysis" };
        public Option<bool> IsVerbose { get; } =
            new("--verbose") { Description = "Enable verbose logging" };
        public Option<string> SystemModuleName { get; } =
            new("--systemmodule") { DefaultValueFactory = _ => Helpers.DefaultSystemModule, Description = "System module name (default: System.Private.CoreLib)" };
        public Option<string> Win32ResourceModuleName { get; } =
            new("--win32resourcemodule") { Description = "Name of the module from which to copy Win32 resources (Windows target only)" };
        public Option<bool> MultiFile { get; } =
            new("--multifile") { Description = "Compile only input files (do not compile referenced assemblies)" };
        public Option<bool> WaitForDebugger { get; } =
            new("--waitfordebugger") { Description = "Pause to give opportunity to attach debugger" };
        public Option<bool> Resilient { get; } =
            new("--resilient") { Description = "Ignore unresolved types, methods, and assemblies. Defaults to false" };
        public Option<string[]> CodegenOptions { get; } =
            new("--codegenopt") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Define a codegen option" };
        public Option<string[]> RdXmlFilePaths { get; } =
            new("--rdxml") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "RD.XML file(s) for compilation" };
        public Option<string[]> LinkTrimFilePaths { get; } =
            new("--descriptor") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "ILLink.Descriptor file(s) for compilation" };
        public Option<string[]> SubstitutionFilePaths { get; } =
            new("--substitution") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "ILLink.Substitution file(s) for compilation" };
        public Option<string> MapFileName { get; } =
            new("--map") { Description = "Generate a map file" };
        public Option<string> MstatFileName { get; } =
            new("--mstat") { Description = "Generate an mstat file" };
        public Option<string> SourceLinkFileName { get; } =
            new("--sourcelink") { Description = "Generate a SourceLink file" };
        public Option<string> MetadataLogFileName { get; } =
            new("--metadatalog") { Description = "Generate a metadata log file" };
        public Option<bool> CompleteTypesMetadata { get; } =
            new("--completetypemetadata") { Description = "Generate complete metadata for types" };
        public Option<string> ReflectionData { get; } =
            new("--reflectiondata") { Description = "Reflection data to generate (one of: all, none)" };
        public Option<bool> ScanReflection { get; } =
            new("--scanreflection") { Description = "Scan IL for reflection patterns" };
        public Option<bool> UseScanner { get; } =
            new("--scan") { Description = "Use IL scanner to generate optimized code (implied by -O)" };
        public Option<bool> NoScanner { get; } =
            new("--noscan") { Description = "Do not use IL scanner to generate optimized code" };
        public Option<string> IlDump { get; } =
            new("--ildump") { Description = "Dump IL assembly listing for compiler-generated IL" };
        public Option<bool> NoInlineTls { get; } =
            new("--noinlinetls") { Description = "Do not generate inline thread local statics" };
        public Option<bool> EmitStackTraceData { get; } =
            new("--stacktracedata") { Description = "Emit data to support generating stack trace strings at runtime" };
        public Option<bool> MethodBodyFolding { get; } =
            new("--methodbodyfolding") { Description = "Fold identical method bodies" };
        public Option<string[]> InitAssemblies { get; } =
            new("--initassembly") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Assembly(ies) with a library initializer" };
        public Option<string[]> FeatureSwitches { get; } =
            new("--feature") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Feature switches to apply (format: 'Namespace.Name=[true|false]'" };
        public Option<string[]> RuntimeOptions { get; } =
            new("--runtimeopt") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Runtime options to set" };
        public Option<string[]> RuntimeKnobs { get; } =
            new("--runtimeknob") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Runtime knobs to set" };
        public Option<int> Parallelism { get; } =
            new("--parallelism") { CustomParser = MakeParallelism, DefaultValueFactory = MakeParallelism, Description = "Maximum number of threads to use during compilation" };
        public Option<string> InstructionSet { get; } =
            new("--instruction-set") { Description = "Instruction set to allow or disallow" };
        public Option<int> MaxVectorTBitWidth { get; } =
            new("--max-vectort-bitwidth") { Description = "Maximum width, in bits, that Vector<T> is allowed to be" };
        public Option<string> Guard { get; } =
            new("--guard") { Description = "Enable mitigations. Options: 'cf': CFG (Control Flow Guard, Windows only)" };
        public Option<bool> Dehydrate { get; } =
            new("--dehydrate") { Description = "Dehydrate runtime data structures" };
        public Option<bool> PreinitStatics { get; } =
            new("--preinitstatics") { Description = "Interpret static constructors at compile time if possible (implied by -O)" };
        public Option<bool> NoPreinitStatics { get; } =
            new("--nopreinitstatics") { Description = "Do not interpret static constructors at compile time" };
        public Option<bool> InstrumentReachability { get; } =
            new("--reachabilityinstrument") { Description = "Instrument code for dynamic reachability" };
        public Option<string> UseReachability { get; } =
            new("--reachabilityuse") { Description = "Use dynamic reachability instrumentation data to produce minimal output" };
        public Option<string[]> SuppressedWarnings { get; } =
            new("--nowarn") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Disable specific warning messages" };
        public Option<bool> SingleWarn { get; } =
            new("--singlewarn") { Description = "Generate single AOT/trimming warning per assembly" };
        public Option<bool> NoTrimWarn { get; } =
            new("--notrimwarn") { Description = "Disable warnings related to trimming" };
        public Option<bool> NoAotWarn { get; } =
            new("--noaotwarn") { Description = "Disable warnings related to AOT" };
        public Option<string[]> SingleWarnEnabledAssemblies { get; } =
            new("--singlewarnassembly") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Generate single AOT/trimming warning for given assembly" };
        public Option<string[]> SingleWarnDisabledAssemblies { get; } =
            new("--nosinglewarnassembly") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Expand AOT/trimming warnings for given assembly" };
        public Option<bool> TreatWarningsAsErrors { get; } =
            new("--warnaserror") { Description = "Treat warnings as errors" };
        public Option<string[]> WarningsAsErrorsEnable { get; } =
            new("--warnaserr") { Description = "Enable treating specific warnings as errors" };
        public Option<string[]> WarningsAsErrorsDisable { get; } =
            new("--nowarnaserr") { Description = "Disable treating specific warnings as errors" };
        public Option<string[]> DirectPInvokes { get; } =
            new("--directpinvoke") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "PInvoke to call directly" };
        public Option<string[]> DirectPInvokeLists { get; } =
            new("--directpinvokelist") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "File with list of PInvokes to call directly" };
        public Option<string[]> RootedAssemblies { get; } =
            new("--root") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Fully generate given assembly" };
        public Option<string[]> ConditionallyRootedAssemblies { get; } =
            new("--conditionalroot") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Fully generate given assembly if it's used" };
        public Option<string[]> TrimmedAssemblies { get; } =
            new("--trim") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Trim the specified assembly" };
        public Option<bool> RootDefaultAssemblies { get; } =
            new("--defaultrooting") { Description = "Root assemblies that are not marked [IsTrimmable]" };
        public Option<TargetArchitecture> TargetArchitecture { get; } =
            new("--targetarch") { CustomParser = MakeTargetArchitecture, DefaultValueFactory = MakeTargetArchitecture, Description = "Target architecture for cross compilation", HelpName = "arg" };
        public Option<TargetOS> TargetOS { get; } =
            new("--targetos") { CustomParser = result => Helpers.GetTargetOS(result.Tokens.Count > 0 ? result.Tokens[0].Value : null), DefaultValueFactory = result => Helpers.GetTargetOS(result.Tokens.Count > 0 ? result.Tokens[0].Value : null), Description = "Target OS for cross compilation", HelpName = "arg" };
        public Option<string> JitPath { get; } =
            new("--jitpath") { Description = "Path to JIT compiler library" };
        public Option<string> SingleMethodTypeName { get; } =
            new("--singlemethodtypename") { Description = "Single method compilation: assembly-qualified name of the owning type" };
        public Option<string> SingleMethodName { get; } =
            new("--singlemethodname") { Description = "Single method compilation: name of the method" };
        public Option<int> MaxGenericCycleDepth { get; } =
            new("--maxgenericcycle") { DefaultValueFactory = _ => CompilerTypeSystemContext.DefaultGenericCycleDepthCutoff, Description = "Max depth of generic cycle" };
        public Option<int> MaxGenericCycleBreadth { get; } =
            new("--maxgenericcyclebreadth") { DefaultValueFactory = _ => CompilerTypeSystemContext.DefaultGenericCycleBreadthCutoff, Description = "Max breadth of generic cycle expansion" };
        public Option<string[]> SingleMethodGenericArgs { get; } =
            new("--singlemethodgenericarg") { Description = "Single method compilation: generic arguments to the method" };
        public Option<string> MakeReproPath { get; } =
            new("--make-repro-path") { Description = "Path where to place a repro package" };
        public Option<string[]> UnmanagedEntryPointsAssemblies { get; } =
            new("--generateunmanagedentrypoints") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Generate unmanaged entrypoints for a given assembly" };

        public OptimizationMode OptimizationMode { get; private set; }
        public ParseResult Result;
        public static bool IsArmel { get; private set; }

        public ILCompilerRootCommand(string[] args) : base(".NET Native IL Compiler")
        {
            Arguments.Add(InputFilePaths);
            Options.Add(ReferenceFiles);
            Options.Add(OutputFilePath);
            Options.Add(Optimize);
            Options.Add(OptimizeSpace);
            Options.Add(OptimizeTime);
            Options.Add(MibcFilePaths);
            Options.Add(MethodLayout);
            Options.Add(FileLayout);
            Options.Add(SatelliteFilePaths);
            Options.Add(EnableDebugInfo);
            Options.Add(UseDwarf5);
            Options.Add(NativeLib);
            Options.Add(SplitExeInitialization);
            Options.Add(ExportsFile);
            Options.Add(ExportDynamicSymbols);
            Options.Add(ExportUnmanagedEntryPoints);
            Options.Add(DgmlLogFileName);
            Options.Add(GenerateFullDgmlLog);
            Options.Add(ScanDgmlLogFileName);
            Options.Add(GenerateFullScanDgmlLog);
            Options.Add(IsVerbose);
            Options.Add(SystemModuleName);
            Options.Add(Win32ResourceModuleName);
            Options.Add(MultiFile);
            Options.Add(WaitForDebugger);
            Options.Add(Resilient);
            Options.Add(CodegenOptions);
            Options.Add(RdXmlFilePaths);
            Options.Add(LinkTrimFilePaths);
            Options.Add(SubstitutionFilePaths);
            Options.Add(MapFileName);
            Options.Add(MstatFileName);
            Options.Add(SourceLinkFileName);
            Options.Add(MetadataLogFileName);
            Options.Add(CompleteTypesMetadata);
            Options.Add(ReflectionData);
            Options.Add(ScanReflection);
            Options.Add(UseScanner);
            Options.Add(NoScanner);
            Options.Add(NoInlineTls);
            Options.Add(IlDump);
            Options.Add(EmitStackTraceData);
            Options.Add(MethodBodyFolding);
            Options.Add(InitAssemblies);
            Options.Add(FeatureSwitches);
            Options.Add(RuntimeOptions);
            Options.Add(RuntimeKnobs);
            Options.Add(Parallelism);
            Options.Add(InstructionSet);
            Options.Add(MaxVectorTBitWidth);
            Options.Add(Guard);
            Options.Add(Dehydrate);
            Options.Add(PreinitStatics);
            Options.Add(NoPreinitStatics);
            Options.Add(InstrumentReachability);
            Options.Add(UseReachability);
            Options.Add(SuppressedWarnings);
            Options.Add(SingleWarn);
            Options.Add(NoTrimWarn);
            Options.Add(NoAotWarn);
            Options.Add(SingleWarnEnabledAssemblies);
            Options.Add(SingleWarnDisabledAssemblies);
            Options.Add(TreatWarningsAsErrors);
            Options.Add(WarningsAsErrorsEnable);
            Options.Add(WarningsAsErrorsDisable);
            Options.Add(DirectPInvokes);
            Options.Add(DirectPInvokeLists);
            Options.Add(MaxGenericCycleDepth);
            Options.Add(MaxGenericCycleBreadth);
            Options.Add(RootedAssemblies);
            Options.Add(ConditionallyRootedAssemblies);
            Options.Add(TrimmedAssemblies);
            Options.Add(RootDefaultAssemblies);
            Options.Add(TargetArchitecture);
            Options.Add(TargetOS);
            Options.Add(JitPath);
            Options.Add(SingleMethodTypeName);
            Options.Add(SingleMethodName);
            Options.Add(SingleMethodGenericArgs);
            Options.Add(MakeReproPath);
            Options.Add(UnmanagedEntryPointsAssemblies);

            this.SetAction(result =>
            {
                Result = result;

                if (result.GetValue(OptimizeSpace))
                {
                    OptimizationMode = OptimizationMode.PreferSize;
                }
                else if (result.GetValue(OptimizeTime))
                {
                    OptimizationMode = OptimizationMode.PreferSpeed;
                }
                else if (result.GetValue(Optimize))
                {
                    OptimizationMode = OptimizationMode.Blended;
                }
                else
                {
                    OptimizationMode = OptimizationMode.None;
                }

                try
                {
                    string makeReproPath = result.GetValue(MakeReproPath);
                    if (makeReproPath != null)
                    {
                        // Create a repro package in the specified path
                        // This package will have the set of input files needed for compilation
                        // + the original command line arguments
                        // + a rsp file that should work to directly run out of the zip file

#pragma warning disable CA1861 // Avoid constant arrays as arguments. Only executed once during the execution of the program.
                        Helpers.MakeReproPackage(makeReproPath, result.GetValue(OutputFilePath), args, result,
                            inputOptions : new[] { "-r", "--reference", "-m", "--mibc", "--rdxml", "--directpinvokelist", "--descriptor", "--satellite" },
                            outputOptions : new[] { "-o", "--out", "--exportsfile", "--dgmllog", "--scandgmllog", "--mstat", "--sourcelink" });
#pragma warning restore CA1861 // Avoid constant arrays as arguments
                    }

                    return new Program(this).Run();
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
                }

                return 1;
#endif
            });
        }

        public static void PrintExtendedHelp(ParseResult _)
        {
            Console.WriteLine("Options may be passed on the command line, or via response file. On the command line switch values may be specified by passing " +
                "the option followed by a space followed by the value of the option, or by specifying a : between option and switch value. A response file " +
                "is specified by passing the @ symbol before the response file name. In a response file all options must be specified on their own lines, and " +
                "only the : syntax for switches is supported.\n");

            Console.WriteLine("Use the '--' option to disambiguate between input files that have begin with -- and options. After a '--' option, all arguments are " +
                "considered to be input files. If no input files begin with '--' then this option is not necessary.\n");

            string[] ValidArchitectures = new string[] { "arm", "arm64", "x86", "x64", "riscv64", "loongarch64" };
            string[] ValidOS = new string[] { "windows", "linux", "freebsd", "osx", "maccatalyst", "ios", "iossimulator", "tvos", "tvossimulator" };

            Console.WriteLine("Valid switches for {0} are: '{1}'. The default value is '{2}'\n", "--targetos", string.Join("', '", ValidOS), Helpers.GetTargetOS(null).ToString().ToLowerInvariant());

            Console.WriteLine(string.Format("Valid switches for {0} are: '{1}'. The default value is '{2}'\n", "--targetarch", string.Join("', '", ValidArchitectures), Helpers.GetTargetArchitecture(null).ToString().ToLowerInvariant()));

            Console.WriteLine("The allowable values for the --instruction-set option are described in the table below. Each architecture has a different set of valid " +
                "instruction sets, and multiple instruction sets may be specified by separating the instructions sets by a ','. For example 'avx2,bmi,lzcnt'");

            foreach (string arch in ValidArchitectures)
            {
                TargetArchitecture targetArch = Helpers.GetTargetArchitecture(arch);
                bool first = true;
                foreach (var instructionSet in Internal.JitInterface.InstructionSetFlags.ArchitectureToValidInstructionSets(targetArch))
                {
                    // Only instruction sets with are specifiable should be printed to the help text
                    if (instructionSet.Specifiable)
                    {
                        if (first)
                        {
                            Console.Write(arch);
                            Console.Write(": ");
                            first = false;
                        }
                        else
                        {
                            Console.Write(", ");
                        }
                        Console.Write(instructionSet.Name);
                    }
                }

                if (first) continue; // no instruction-set found for this architecture

                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine("The following CPU names are predefined groups of instruction sets and can be used in --instruction-set too:");
            Console.WriteLine(string.Join(", ", Internal.JitInterface.InstructionSetFlags.AllCpuNames));
        }

        private static TargetArchitecture MakeTargetArchitecture(ArgumentResult result)
        {
            string firstToken = result.Tokens.Count > 0 ? result.Tokens[0].Value : null;
            if (firstToken != null && firstToken.Equals("armel", StringComparison.OrdinalIgnoreCase))
            {
                IsArmel = true;
                return Internal.TypeSystem.TargetArchitecture.ARM;
            }

            return Helpers.GetTargetArchitecture(firstToken);
        }

        private static int MakeParallelism(ArgumentResult result)
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
        }

        private static MethodLayoutAlgorithm MakeMethodLayoutAlgorithm(ArgumentResult result)
        {
            if (result.Tokens.Count == 0)
                return MethodLayoutAlgorithm.DefaultSort;

            return result.Tokens[0].Value.ToLowerInvariant() switch
            {
                "defaultsort" => MethodLayoutAlgorithm.DefaultSort,
                "exclusiveweight" => MethodLayoutAlgorithm.ExclusiveWeight,
                "hotcold" => MethodLayoutAlgorithm.HotCold,
                "instrumentedhotcold" => MethodLayoutAlgorithm.InstrumentedHotCold,
                "hotwarmcold" => MethodLayoutAlgorithm.HotWarmCold,
                "pettishansen" => MethodLayoutAlgorithm.PettisHansen,
                "random" => MethodLayoutAlgorithm.Random,
                _ => throw new CommandLineException(result.Tokens[0].Value)
            };
        }

        private static FileLayoutAlgorithm MakeFileLayoutAlgorithm(ArgumentResult result)
        {
            if (result.Tokens.Count == 0)
                return FileLayoutAlgorithm.DefaultSort;

            return result.Tokens[0].Value.ToLowerInvariant() switch
            {
                "defaultsort" => FileLayoutAlgorithm.DefaultSort,
                "methodorder" => FileLayoutAlgorithm.MethodOrder,
                _ => throw new CommandLineException(result.Tokens[0].Value)
            };
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
