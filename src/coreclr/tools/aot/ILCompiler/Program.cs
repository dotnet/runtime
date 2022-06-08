// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Internal.CommandLine;

using Debug = System.Diagnostics.Debug;
using InstructionSet = Internal.JitInterface.InstructionSet;

namespace ILCompiler
{
    internal class Program
    {
        private const string DefaultSystemModule = "System.Private.CoreLib";

        private Dictionary<string, string> _inputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _referenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string _outputFilePath;
        private bool _isVerbose;

        private string _dgmlLogFileName;
        private bool _generateFullDgmlLog;
        private string _scanDgmlLogFileName;
        private bool _generateFullScanDgmlLog;

        private TargetArchitecture _targetArchitecture;
        private string _targetArchitectureStr;
        private TargetOS _targetOS;
        private string _targetOSStr;
        private OptimizationMode _optimizationMode;
        private bool _enableDebugInfo;
        private string _ilDump;
        private string _systemModuleName = DefaultSystemModule;
        private bool _multiFile;
        private bool _nativeLib;
        private string _exportsFile;
        private bool _useScanner;
        private bool _noScanner;
        private bool _preinitStatics;
        private bool _noPreinitStatics;
        private bool _emitStackTraceData;
        private string _mapFileName;
        private string _metadataLogFileName;
        private bool _noMetadataBlocking;
        private string _reflectionData;
        private bool _completeTypesMetadata;
        private bool _scanReflection;
        private bool _methodBodyFolding;
        private int _parallelism = Environment.ProcessorCount;
        private string _instructionSet;
        private string _guard;
        private int _maxGenericCycle = CompilerTypeSystemContext.DefaultGenericCycleCutoffPoint;
        private bool _useDwarf5;
        private string _jitPath;

        private string _singleMethodTypeName;
        private string _singleMethodName;
        private IReadOnlyList<string> _singleMethodGenericArgs;

        private IReadOnlyList<string> _codegenOptions = Array.Empty<string>();

        private IReadOnlyList<string> _rdXmlFilePaths = Array.Empty<string>();

        private IReadOnlyList<string> _initAssemblies = Array.Empty<string>();

        private IReadOnlyList<string> _appContextSwitches = Array.Empty<string>();

        private IReadOnlyList<string> _runtimeOptions = Array.Empty<string>();

        private IReadOnlyList<string> _featureSwitches = Array.Empty<string>();

        private IReadOnlyList<string> _suppressedWarnings = Array.Empty<string>();

        private IReadOnlyList<string> _directPInvokes = Array.Empty<string>();

        private IReadOnlyList<string> _directPInvokeLists = Array.Empty<string>();

        private bool _resilient;

        private IReadOnlyList<string> _rootedAssemblies = Array.Empty<string>();
        private IReadOnlyList<string> _conditionallyRootedAssemblies = Array.Empty<string>();
        private IReadOnlyList<string> _trimmedAssemblies = Array.Empty<string>();
        private bool _rootDefaultAssemblies;

        public IReadOnlyList<string> _mibcFilePaths = Array.Empty<string>();

        private IReadOnlyList<string> _singleWarnEnabledAssemblies = Array.Empty<string>();
        private IReadOnlyList<string> _singleWarnDisabledAssemblies = Array.Empty<string>();
        private bool _singleWarn;

        private string _makeReproPath;

        private bool _help;

        private Program()
        {
        }

        private void Help(string helpText)
        {
            Console.WriteLine();
            Console.Write("Microsoft (R) .NET Native IL Compiler");
            Console.Write(" ");
            Console.Write(typeof(Program).GetTypeInfo().Assembly.GetName().Version);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(helpText);
        }

        public static void ComputeDefaultOptions(out TargetOS os, out TargetArchitecture arch)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                os = TargetOS.Windows;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                os = TargetOS.Linux;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                os = TargetOS.OSX;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                os = TargetOS.FreeBSD;
            else
                throw new NotImplementedException();

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    arch = TargetArchitecture.X86;
                    break;
                case Architecture.X64:
                    arch = TargetArchitecture.X64;
                    break;
                case Architecture.Arm:
                    arch = TargetArchitecture.ARM;
                    break;
                case Architecture.Arm64:
                    arch = TargetArchitecture.ARM64;
                    break;
                default:
                    throw new NotImplementedException();
            }

        }

        private void InitializeDefaultOptions()
        {
            ComputeDefaultOptions(out _targetOS, out _targetArchitecture);
        }

        private ArgumentSyntax ParseCommandLine(string[] args)
        {
            var validReflectionDataOptions = new string[] { "all", "none" };

            IReadOnlyList<string> inputFiles = Array.Empty<string>();
            IReadOnlyList<string> referenceFiles = Array.Empty<string>();

            bool optimize = false;
            bool optimizeSpace = false;
            bool optimizeTime = false;

            bool waitForDebugger = false;
            AssemblyName name = typeof(Program).GetTypeInfo().Assembly.GetName();
            ArgumentSyntax argSyntax = ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.ApplicationName = name.Name.ToString();

                // HandleHelp writes to error, fails fast with crash dialog and lacks custom formatting.
                syntax.HandleHelp = false;
                syntax.HandleErrors = true;

                syntax.DefineOption("h|help", ref _help, "Help message for ILC");
                syntax.DefineOptionList("r|reference", ref referenceFiles, "Reference file(s) for compilation");
                syntax.DefineOption("o|out", ref _outputFilePath, "Output file path");
                syntax.DefineOption("O", ref optimize, "Enable optimizations");
                syntax.DefineOption("Os", ref optimizeSpace, "Enable optimizations, favor code space");
                syntax.DefineOption("Ot", ref optimizeTime, "Enable optimizations, favor code speed");
                syntax.DefineOptionList("m|mibc", ref _mibcFilePaths, "Mibc file(s) for profile guided optimization"); ;
                syntax.DefineOption("g", ref _enableDebugInfo, "Emit debugging information");
                syntax.DefineOption("gdwarf-5", ref _useDwarf5, "Generate source-level debug information with dwarf version 5");
                syntax.DefineOption("nativelib", ref _nativeLib, "Compile as static or shared library");
                syntax.DefineOption("exportsfile", ref _exportsFile, "File to write exported method definitions");
                syntax.DefineOption("dgmllog", ref _dgmlLogFileName, "Save result of dependency analysis as DGML");
                syntax.DefineOption("fulllog", ref _generateFullDgmlLog, "Save detailed log of dependency analysis");
                syntax.DefineOption("scandgmllog", ref _scanDgmlLogFileName, "Save result of scanner dependency analysis as DGML");
                syntax.DefineOption("scanfulllog", ref _generateFullScanDgmlLog, "Save detailed log of scanner dependency analysis");
                syntax.DefineOption("verbose", ref _isVerbose, "Enable verbose logging");
                syntax.DefineOption("systemmodule", ref _systemModuleName, "System module name (default: System.Private.CoreLib)");
                syntax.DefineOption("multifile", ref _multiFile, "Compile only input files (do not compile referenced assemblies)");
                syntax.DefineOption("waitfordebugger", ref waitForDebugger, "Pause to give opportunity to attach debugger");
                syntax.DefineOption("resilient", ref _resilient, "Ignore unresolved types, methods, and assemblies. Defaults to false");
                syntax.DefineOptionList("codegenopt", ref _codegenOptions, "Define a codegen option");
                syntax.DefineOptionList("rdxml", ref _rdXmlFilePaths, "RD.XML file(s) for compilation");
                syntax.DefineOption("map", ref _mapFileName, "Generate a map file");
                syntax.DefineOption("metadatalog", ref _metadataLogFileName, "Generate a metadata log file");
                syntax.DefineOption("nometadatablocking", ref _noMetadataBlocking, "Ignore metadata blocking for internal implementation details");
                syntax.DefineOption("completetypemetadata", ref _completeTypesMetadata, "Generate complete metadata for types");
                syntax.DefineOption("reflectiondata", ref _reflectionData, $"Reflection data to generate (one of: {string.Join(", ", validReflectionDataOptions)})");
                syntax.DefineOption("scanreflection", ref _scanReflection, "Scan IL for reflection patterns");
                syntax.DefineOption("scan", ref _useScanner, "Use IL scanner to generate optimized code (implied by -O)");
                syntax.DefineOption("noscan", ref _noScanner, "Do not use IL scanner to generate optimized code");
                syntax.DefineOption("ildump", ref _ilDump, "Dump IL assembly listing for compiler-generated IL");
                syntax.DefineOption("stacktracedata", ref _emitStackTraceData, "Emit data to support generating stack trace strings at runtime");
                syntax.DefineOption("methodbodyfolding", ref _methodBodyFolding, "Fold identical method bodies");
                syntax.DefineOptionList("initassembly", ref _initAssemblies, "Assembly(ies) with a library initializer");
                syntax.DefineOptionList("appcontextswitch", ref _appContextSwitches, "System.AppContext switches to set (format: 'Key=Value')");
                syntax.DefineOptionList("feature", ref _featureSwitches, "Feature switches to apply (format: 'Namespace.Name=[true|false]'");
                syntax.DefineOptionList("runtimeopt", ref _runtimeOptions, "Runtime options to set");
                syntax.DefineOption("parallelism", ref _parallelism, "Maximum number of threads to use during compilation");
                syntax.DefineOption("instructionset", ref _instructionSet, "Instruction set to allow or disallow");
                syntax.DefineOption("guard", ref _guard, "Enable mitigations. Options: 'cf': CFG (Control Flow Guard, Windows only)");
                syntax.DefineOption("preinitstatics", ref _preinitStatics, "Interpret static constructors at compile time if possible (implied by -O)");
                syntax.DefineOption("nopreinitstatics", ref _noPreinitStatics, "Do not interpret static constructors at compile time");
                syntax.DefineOptionList("nowarn", ref _suppressedWarnings, "Disable specific warning messages");
                syntax.DefineOption("singlewarn", ref _singleWarn, "Generate single AOT/trimming warning per assembly");
                syntax.DefineOptionList("singlewarnassembly", ref _singleWarnEnabledAssemblies, "Generate single AOT/trimming warning for given assembly");
                syntax.DefineOptionList("nosinglewarnassembly", ref _singleWarnDisabledAssemblies, "Expand AOT/trimming warnings for given assembly");
                syntax.DefineOptionList("directpinvoke", ref _directPInvokes, "PInvoke to call directly");
                syntax.DefineOptionList("directpinvokelist", ref _directPInvokeLists, "File with list of PInvokes to call directly");
                syntax.DefineOption("maxgenericcycle", ref _maxGenericCycle, "Max depth of generic cycle");
                syntax.DefineOptionList("root", ref _rootedAssemblies, "Fully generate given assembly");
                syntax.DefineOptionList("conditionalroot", ref _conditionallyRootedAssemblies, "Fully generate given assembly if it's used");
                syntax.DefineOptionList("trim", ref _trimmedAssemblies, "Trim the specified assembly");
                syntax.DefineOption("defaultrooting", ref _rootDefaultAssemblies, "Root assemblies that are not marked [IsTrimmable]");

                syntax.DefineOption("targetarch", ref _targetArchitectureStr, "Target architecture for cross compilation");
                syntax.DefineOption("targetos", ref _targetOSStr, "Target OS for cross compilation");
                syntax.DefineOption("jitpath", ref _jitPath, "Path to JIT compiler library");

                syntax.DefineOption("singlemethodtypename", ref _singleMethodTypeName, "Single method compilation: assembly-qualified name of the owning type");
                syntax.DefineOption("singlemethodname", ref _singleMethodName, "Single method compilation: name of the method");
                syntax.DefineOptionList("singlemethodgenericarg", ref _singleMethodGenericArgs, "Single method compilation: generic arguments to the method");

                syntax.DefineOption("make-repro-path", ref _makeReproPath, "Path where to place a repro package");

                syntax.DefineParameterList("in", ref inputFiles, "Input file(s) to compile");
            });

            if (_help)
            {
                List<string> extraHelp = new List<string>();

                extraHelp.Add("Options may be passed on the command line, or via response file. On the command line switch values may be specified by passing " +
                    "the option followed by a space followed by the value of the option, or by specifying a : between option and switch value. A response file " +
                    "is specified by passing the @ symbol before the response file name. In a response file all options must be specified on their own lines, and " +
                    "only the : syntax for switches is supported.");

                extraHelp.Add("");

                extraHelp.Add("Use the '--' option to disambiguate between input files that have begin with -- and options. After a '--' option, all arguments are " +
                    "considered to be input files. If no input files begin with '--' then this option is not necessary.");

                extraHelp.Add("");

                string[] ValidArchitectures = new string[] { "arm", "arm64", "x86", "x64" };
                string[] ValidOS = new string[] { "windows", "linux", "osx" };

                Program.ComputeDefaultOptions(out TargetOS defaultOs, out TargetArchitecture defaultArch);

                extraHelp.Add(String.Format("Valid switches for {0} are: '{1}'. The default value is '{2}'", "--targetos", String.Join("', '", ValidOS), defaultOs.ToString().ToLowerInvariant()));

                extraHelp.Add("");

                extraHelp.Add(String.Format("Valid switches for {0} are: '{1}'. The default value is '{2}'", "--targetarch", String.Join("', '", ValidArchitectures), defaultArch.ToString().ToLowerInvariant()));

                extraHelp.Add("");

                extraHelp.Add("The allowable values for the --instruction-set option are described in the table below. Each architecture has a different set of valid " +
                    "instruction sets, and multiple instruction sets may be specified by separating the instructions sets by a ','. For example 'avx2,bmi,lzcnt'");

                foreach (string arch in ValidArchitectures)
                {
                    StringBuilder archString = new StringBuilder();

                    archString.Append(arch);
                    archString.Append(": ");

                    TargetArchitecture targetArch = Program.GetTargetArchitectureFromArg(arch);
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
                                archString.Append(", ");
                            }
                            archString.Append(instructionSet.Name);
                        }
                    }

                    extraHelp.Add(archString.ToString());
                }

                argSyntax.ExtraHelpParagraphs = extraHelp;
            }

            if (waitForDebugger)
            {
                Console.WriteLine("Waiting for debugger to attach. Press ENTER to continue");
                Console.ReadLine();
            }

            _optimizationMode = OptimizationMode.None;
            if (optimizeSpace)
            {
                if (optimizeTime)
                    Console.WriteLine("Warning: overriding -Ot with -Os");
                _optimizationMode = OptimizationMode.PreferSize;
            }
            else if (optimizeTime)
                _optimizationMode = OptimizationMode.PreferSpeed;
            else if (optimize)
                _optimizationMode = OptimizationMode.Blended;

            foreach (var input in inputFiles)
                Helpers.AppendExpandedPaths(_inputFilePaths, input, true);

            foreach (var reference in referenceFiles)
                Helpers.AppendExpandedPaths(_referenceFilePaths, reference, false);

            if (_makeReproPath != null)
            {
                // Create a repro package in the specified path
                // This package will have the set of input files needed for compilation
                // + the original command line arguments
                // + a rsp file that should work to directly run out of the zip file

                Helpers.MakeReproPackage(_makeReproPath, _outputFilePath, args, argSyntax, new[] { "-r", "-m", "--rdxml", "--directpinvokelist" });
            }

            if (_reflectionData != null && Array.IndexOf(validReflectionDataOptions, _reflectionData) < 0)
            {
                Console.WriteLine($"Warning: option '{_reflectionData}' not recognized");
            }

            return argSyntax;
        }

        private IReadOnlyCollection<MethodDesc> CreateInitializerList(CompilerTypeSystemContext context)
        {
            List<ModuleDesc> assembliesWithInitializers = new List<ModuleDesc>();

            // Build a list of assemblies that have an initializer that needs to run before
            // any user code runs.
            foreach (string initAssemblyName in _initAssemblies)
            {
                ModuleDesc assembly = context.ResolveAssembly(new AssemblyName(initAssemblyName), throwIfNotFound: true);
                assembliesWithInitializers.Add(assembly);
            }

            var libraryInitializers = new LibraryInitializers(context, assembliesWithInitializers);

            List<MethodDesc> initializerList = new List<MethodDesc>(libraryInitializers.LibraryInitializerMethods);

            // If there are any AppContext switches the user wishes to enable, generate code that sets them.
            if (_appContextSwitches.Count > 0)
            {
                MethodDesc appContextInitMethod = new Internal.IL.Stubs.StartupCode.AppContextInitializerMethod(
                    context.GeneratedAssembly.GetGlobalModuleType(), _appContextSwitches);
                initializerList.Add(appContextInitMethod);
            }

            return initializerList;
        }

        private static TargetArchitecture GetTargetArchitectureFromArg(string archArg)
        {
            if (archArg.Equals("x86", StringComparison.OrdinalIgnoreCase))
                return TargetArchitecture.X86;
            else if (archArg.Equals("x64", StringComparison.OrdinalIgnoreCase))
                return TargetArchitecture.X64;
            else if (archArg.Equals("arm", StringComparison.OrdinalIgnoreCase))
                return TargetArchitecture.ARM;
            else if (archArg.Equals("arm64", StringComparison.OrdinalIgnoreCase))
                return TargetArchitecture.ARM64;
            else
                throw new CommandLineException("Target architecture is not supported");
        }

        private static TargetOS GetTargetOSFromArg(string osArg)
        {
            if (osArg.Equals("windows", StringComparison.OrdinalIgnoreCase))
                return TargetOS.Windows;
            else if (osArg.Equals("linux", StringComparison.OrdinalIgnoreCase))
                return TargetOS.Linux;
            else if (osArg.Equals("osx", StringComparison.OrdinalIgnoreCase))
                return TargetOS.OSX;
            else
                throw new CommandLineException("Target OS is not supported");
        }

        private int Run(string[] args)
        {
            InitializeDefaultOptions();

            ArgumentSyntax syntax = ParseCommandLine(args);
            if (_help)
            {
                Help(syntax.GetHelpText());
                return 1;
            }

            if (_outputFilePath == null)
                throw new CommandLineException("Output filename must be specified (/out <file>)");

            //
            // Set target Architecture and OS
            //
            if (_targetArchitectureStr != null)
            {
                _targetArchitecture = GetTargetArchitectureFromArg(_targetArchitectureStr);
            }
            if (_targetOSStr != null)
            {
                _targetOS = GetTargetOSFromArg(_targetOSStr);
            }

            InstructionSetSupportBuilder instructionSetSupportBuilder = new InstructionSetSupportBuilder(_targetArchitecture);

            // The runtime expects certain baselines that the codegen can assume as well.
            if ((_targetArchitecture == TargetArchitecture.X86) || (_targetArchitecture == TargetArchitecture.X64))
            {
                instructionSetSupportBuilder.AddSupportedInstructionSet("sse2"); // Lower baselines included by implication
            }
            else if (_targetArchitecture == TargetArchitecture.ARM64)
            {
                instructionSetSupportBuilder.AddSupportedInstructionSet("neon"); // Lower baselines included by implication
            }

            if (_instructionSet != null)
            {
                List<string> instructionSetParams = new List<string>();

                // Normalize instruction set format to include implied +.
                string[] instructionSetParamsInput = _instructionSet.Split(',');
                for (int i = 0; i < instructionSetParamsInput.Length; i++)
                {
                    string instructionSet = instructionSetParamsInput[i];

                    if (String.IsNullOrEmpty(instructionSet))
                        throw new CommandLineException("Instruction set must not be empty");

                    char firstChar = instructionSet[0];
                    if ((firstChar != '+') && (firstChar != '-'))
                    {
                        instructionSet = "+" + instructionSet;
                    }
                    instructionSetParams.Add(instructionSet);
                }

                Dictionary<string, bool> instructionSetSpecification = new Dictionary<string, bool>();
                foreach (string instructionSetSpecifier in instructionSetParams)
                {
                    string instructionSet = instructionSetSpecifier.Substring(1);

                    bool enabled = instructionSetSpecifier[0] == '+' ? true : false;
                    if (enabled)
                    {
                        if (!instructionSetSupportBuilder.AddSupportedInstructionSet(instructionSet))
                            throw new CommandLineException($"Unrecognized instruction set '{instructionSet}'");
                    }
                    else
                    {
                        if (!instructionSetSupportBuilder.RemoveInstructionSetSupport(instructionSet))
                            throw new CommandLineException($"Unrecognized instruction set '{instructionSet}'");
                    }
                }
            }

            instructionSetSupportBuilder.ComputeInstructionSetFlags(out var supportedInstructionSet, out var unsupportedInstructionSet,
                (string specifiedInstructionSet, string impliedInstructionSet) =>
                    throw new CommandLineException(String.Format("Unsupported combination of instruction sets: {0}/{1}", specifiedInstructionSet, impliedInstructionSet)));

            InstructionSetSupportBuilder optimisticInstructionSetSupportBuilder = new InstructionSetSupportBuilder(_targetArchitecture);

            // Optimistically assume some instruction sets are present.
            if ((_targetArchitecture == TargetArchitecture.X86) || (_targetArchitecture == TargetArchitecture.X64))
            {
                // We set these hardware features as opportunistically enabled as most of hardware in the wild supports them.
                // Note that we do not indicate support for AVX, or any other instruction set which uses the VEX encodings as
                // the presence of those makes otherwise acceptable code be unusable on hardware which does not support VEX encodings.
                //
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sse4.2"); // Lower SSE versions included by implication
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("aes");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("pclmul");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("movbe");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("popcnt");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("lzcnt");

                // If AVX was enabled, we can opportunistically enable instruction sets which use the VEX encodings
                Debug.Assert(InstructionSet.X64_AVX == InstructionSet.X86_AVX);
                if (supportedInstructionSet.HasInstructionSet(InstructionSet.X64_AVX))
                {
                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("fma");
                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("bmi");
                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("bmi2");
                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("avxvnni");
                }
            }
            else if (_targetArchitecture == TargetArchitecture.ARM64)
            {
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("aes");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("crc");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sha1");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sha2");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("lse");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("rcpc");
            }

            optimisticInstructionSetSupportBuilder.ComputeInstructionSetFlags(out var optimisticInstructionSet, out _,
                (string specifiedInstructionSet, string impliedInstructionSet) => throw new NotSupportedException());
            optimisticInstructionSet.Remove(unsupportedInstructionSet);
            optimisticInstructionSet.Add(supportedInstructionSet);

            var instructionSetSupport = new InstructionSetSupport(supportedInstructionSet,
                                                                  unsupportedInstructionSet,
                                                                  optimisticInstructionSet,
                                                                  InstructionSetSupportBuilder.GetNonSpecifiableInstructionSetsForArch(_targetArchitecture),
                                                                  _targetArchitecture);

            bool supportsReflection = _reflectionData != "none" && _systemModuleName == DefaultSystemModule;

            //
            // Initialize type system context
            //

            SharedGenericsMode genericsMode = SharedGenericsMode.CanonicalReferenceTypes;

            var simdVectorLength = instructionSetSupport.GetVectorTSimdVector();
            var targetAbi = TargetAbi.NativeAot;
            var targetDetails = new TargetDetails(_targetArchitecture, _targetOS, targetAbi, simdVectorLength);
            CompilerTypeSystemContext typeSystemContext =
                new CompilerTypeSystemContext(targetDetails, genericsMode, supportsReflection ? DelegateFeature.All : 0, _maxGenericCycle);

            //
            // TODO: To support our pre-compiled test tree, allow input files that aren't managed assemblies since
            // some tests contain a mixture of both managed and native binaries.
            //
            // See: https://github.com/dotnet/corert/issues/2785
            //
            // When we undo this hack, replace the foreach with
            //  typeSystemContext.InputFilePaths = _inputFilePaths;
            //
            Dictionary<string, string> inputFilePaths = new Dictionary<string, string>();
            foreach (var inputFile in _inputFilePaths)
            {
                try
                {
                    var module = typeSystemContext.GetModuleFromPath(inputFile.Value);
                    inputFilePaths.Add(inputFile.Key, inputFile.Value);
                }
                catch (TypeSystemException.BadImageFormatException)
                {
                    // Keep calm and carry on.
                }
            }

            typeSystemContext.InputFilePaths = inputFilePaths;
            typeSystemContext.ReferenceFilePaths = _referenceFilePaths;
            if (!typeSystemContext.InputFilePaths.ContainsKey(_systemModuleName)
                && !typeSystemContext.ReferenceFilePaths.ContainsKey(_systemModuleName))
                throw new CommandLineException($"System module {_systemModuleName} does not exists. Make sure that you specify --systemmodule");

            typeSystemContext.SetSystemModule(typeSystemContext.GetModuleForSimpleName(_systemModuleName));

            if (typeSystemContext.InputFilePaths.Count == 0)
                throw new CommandLineException("No input files specified");

            SecurityMitigationOptions securityMitigationOptions = 0;
            if (StringComparer.OrdinalIgnoreCase.Equals(_guard, "cf"))
            {
                if (_targetOS != TargetOS.Windows)
                {
                    throw new CommandLineException($"Control flow guard only available on Windows");
                }

                securityMitigationOptions = SecurityMitigationOptions.ControlFlowGuardAnnotations;
            }
            else if (!String.IsNullOrEmpty(_guard))
            {
                throw new CommandLineException($"Unrecognized mitigation option '{_guard}'");
            }

            //
            // Initialize compilation group and compilation roots
            //

            // Single method mode?
            MethodDesc singleMethod = CheckAndParseSingleMethodModeArguments(typeSystemContext);

            CompilationModuleGroup compilationGroup;
            List<ICompilationRootProvider> compilationRoots = new List<ICompilationRootProvider>();
            if (singleMethod != null)
            {
                // Compiling just a single method
                compilationGroup = new SingleMethodCompilationModuleGroup(singleMethod);
                compilationRoots.Add(new SingleMethodRootProvider(singleMethod));
            }
            else
            {
                // Either single file, or multifile library, or multifile consumption.
                EcmaModule entrypointModule = null;
                bool systemModuleIsInputModule = false;
                foreach (var inputFile in typeSystemContext.InputFilePaths)
                {
                    EcmaModule module = typeSystemContext.GetModuleFromPath(inputFile.Value);

                    if (module.PEReader.PEHeaders.IsExe)
                    {
                        if (entrypointModule != null)
                            throw new Exception("Multiple EXE modules");
                        entrypointModule = module;
                    }

                    if (module == typeSystemContext.SystemModule)
                        systemModuleIsInputModule = true;

                    compilationRoots.Add(new ExportedMethodsRootProvider(module));
                }

                if (entrypointModule != null)
                {
                    compilationRoots.Add(new MainMethodRootProvider(entrypointModule, CreateInitializerList(typeSystemContext)));
                    compilationRoots.Add(new RuntimeConfigurationRootProvider(_runtimeOptions));
                    compilationRoots.Add(new ExpectedIsaFeaturesRootProvider(instructionSetSupport));
                }

                if (_multiFile)
                {
                    List<EcmaModule> inputModules = new List<EcmaModule>();

                    foreach (var inputFile in typeSystemContext.InputFilePaths)
                    {
                        EcmaModule module = typeSystemContext.GetModuleFromPath(inputFile.Value);

                        if (entrypointModule == null)
                        {
                            // This is a multifile production build - we need to root all methods
                            compilationRoots.Add(new LibraryRootProvider(module));
                        }
                        inputModules.Add(module);
                    }

                    compilationGroup = new MultiFileSharedCompilationModuleGroup(typeSystemContext, inputModules);
                }
                else
                {
                    if (entrypointModule == null && !_nativeLib)
                        throw new Exception("No entrypoint module");

                    if (!systemModuleIsInputModule)
                        compilationRoots.Add(new ExportedMethodsRootProvider((EcmaModule)typeSystemContext.SystemModule));
                    compilationGroup = new SingleFileCompilationModuleGroup();
                }

                if (_nativeLib)
                {
                    // Set owning module of generated native library startup method to compiler generated module,
                    // to ensure the startup method is included in the object file during multimodule mode build
                    compilationRoots.Add(new NativeLibraryInitializerRootProvider(typeSystemContext.GeneratedAssembly, CreateInitializerList(typeSystemContext)));
                    compilationRoots.Add(new RuntimeConfigurationRootProvider(_runtimeOptions));
                    compilationRoots.Add(new ExpectedIsaFeaturesRootProvider(instructionSetSupport));
                }

                foreach (var rdXmlFilePath in _rdXmlFilePaths)
                {
                    compilationRoots.Add(new RdXmlRootProvider(typeSystemContext, rdXmlFilePath));
                }
            }

            _conditionallyRootedAssemblies = new List<string>(_conditionallyRootedAssemblies.Select(a => ILLinkify(a)));
            _trimmedAssemblies = new List<string>(_trimmedAssemblies.Select(a => ILLinkify(a)));

            static string ILLinkify(string rootedAssembly)
            {
                // For compatibility with IL Linker, the parameter could be a file name or an assembly name.
                // This is the logic IL Linker uses to decide how to interpret the string. Really.
                string simpleName;
                if (File.Exists(rootedAssembly))
                    simpleName = Path.GetFileNameWithoutExtension(rootedAssembly);
                else
                    simpleName = rootedAssembly;
                return simpleName;
            }

            // Root whatever assemblies were specified on the command line
            foreach (var rootedAssembly in _rootedAssemblies)
            {
                // For compatibility with IL Linker, the parameter could be a file name or an assembly name.
                // This is the logic IL Linker uses to decide how to interpret the string. Really.
                EcmaModule module = File.Exists(rootedAssembly)
                    ? typeSystemContext.GetModuleFromPath(rootedAssembly)
                    : typeSystemContext.GetModuleForSimpleName(rootedAssembly);

                // We only root the module type. The rest will fall out because we treat _rootedAssemblies
                // same as conditionally rooted ones and here we're fulfilling the condition ("something is used").
                compilationRoots.Add(
                    new GenericRootProvider<ModuleDesc>(module,
                    (ModuleDesc module, IRootingServiceProvider rooter) => rooter.AddCompilationRoot(module.GetGlobalModuleType(), "Command line root")));
            }

            //
            // Compile
            //

            CompilationBuilder builder = new RyuJitCompilationBuilder(typeSystemContext, compilationGroup);

            string compilationUnitPrefix = _multiFile ? System.IO.Path.GetFileNameWithoutExtension(_outputFilePath) : "";
            builder.UseCompilationUnitPrefix(compilationUnitPrefix);

            if (_mibcFilePaths.Count > 0)
                ((RyuJitCompilationBuilder)builder).UseProfileData(_mibcFilePaths);
            if (!String.IsNullOrEmpty(_jitPath))
                ((RyuJitCompilationBuilder)builder).UseJitPath(_jitPath);

            PInvokeILEmitterConfiguration pinvokePolicy = new ConfigurablePInvokePolicy(typeSystemContext.Target, _directPInvokes, _directPInvokeLists);

            ILProvider ilProvider = new NativeAotILProvider();

            List<KeyValuePair<string, bool>> featureSwitches = new List<KeyValuePair<string, bool>>();
            foreach (var switchPair in _featureSwitches)
            {
                string[] switchAndValue = switchPair.Split('=');
                if (switchAndValue.Length != 2
                    || !bool.TryParse(switchAndValue[1], out bool switchValue))
                    throw new CommandLineException($"Unexpected feature switch pair '{switchPair}'");
                featureSwitches.Add(new KeyValuePair<string, bool>(switchAndValue[0], switchValue));
            }
            ilProvider = new FeatureSwitchManager(ilProvider, featureSwitches);

            var logger = new Logger(Console.Out, _isVerbose, ProcessWarningCodes(_suppressedWarnings), _singleWarn, _singleWarnEnabledAssemblies, _singleWarnDisabledAssemblies);

            var stackTracePolicy = _emitStackTraceData ?
                (StackTraceEmissionPolicy)new EcmaMethodStackTraceEmissionPolicy() : new NoStackTraceEmissionPolicy();

            MetadataBlockingPolicy mdBlockingPolicy;
            ManifestResourceBlockingPolicy resBlockingPolicy;
            UsageBasedMetadataGenerationOptions metadataGenerationOptions = default;
            if (supportsReflection)
            {
                mdBlockingPolicy = _noMetadataBlocking
                    ? (MetadataBlockingPolicy)new NoMetadataBlockingPolicy()
                    : new BlockedInternalsBlockingPolicy(typeSystemContext);

                resBlockingPolicy = new ManifestResourceBlockingPolicy(featureSwitches);

                metadataGenerationOptions |= UsageBasedMetadataGenerationOptions.AnonymousTypeHeuristic;
                if (_completeTypesMetadata)
                    metadataGenerationOptions |= UsageBasedMetadataGenerationOptions.CompleteTypesOnly;
                if (_scanReflection)
                    metadataGenerationOptions |= UsageBasedMetadataGenerationOptions.ReflectionILScanning;
                if (_reflectionData == "all")
                    metadataGenerationOptions |= UsageBasedMetadataGenerationOptions.CreateReflectableArtifacts;
                if (_rootDefaultAssemblies)
                    metadataGenerationOptions |= UsageBasedMetadataGenerationOptions.RootDefaultAssemblies;
            }
            else
            {
                mdBlockingPolicy = new FullyBlockedMetadataBlockingPolicy();
                resBlockingPolicy = new FullyBlockedManifestResourceBlockingPolicy();
            }

            DynamicInvokeThunkGenerationPolicy invokeThunkGenerationPolicy = new DefaultDynamicInvokeThunkGenerationPolicy();

            var flowAnnotations = new ILLink.Shared.TrimAnalysis.FlowAnnotations(logger, ilProvider);

            MetadataManager metadataManager = new UsageBasedMetadataManager(
                    compilationGroup,
                    typeSystemContext,
                    mdBlockingPolicy,
                    resBlockingPolicy,
                    _metadataLogFileName,
                    stackTracePolicy,
                    invokeThunkGenerationPolicy,
                    flowAnnotations,
                    metadataGenerationOptions,
                    logger,
                    featureSwitches,
                    _conditionallyRootedAssemblies.Concat(_rootedAssemblies),
                    _trimmedAssemblies);

            InteropStateManager interopStateManager = new InteropStateManager(typeSystemContext.GeneratedAssembly);
            InteropStubManager interopStubManager = new UsageBasedInteropStubManager(interopStateManager, pinvokePolicy, logger);

            // Unless explicitly opted in at the command line, we enable scanner for retail builds by default.
            // We also don't do this for multifile because scanner doesn't simulate inlining (this would be
            // fixable by using a CompilationGroup for the scanner that has a bigger worldview, but
            // let's cross that bridge when we get there).
            bool useScanner = _useScanner ||
                (_optimizationMode != OptimizationMode.None && !_multiFile);

            useScanner &= !_noScanner;

            // Enable static data preinitialization in optimized builds.
            bool preinitStatics = _preinitStatics ||
                (_optimizationMode != OptimizationMode.None && !_multiFile);
            preinitStatics &= !_noPreinitStatics;

            var preinitManager = new PreinitializationManager(typeSystemContext, compilationGroup, ilProvider, preinitStatics);
            builder
                .UseILProvider(ilProvider)
                .UsePreinitializationManager(preinitManager);

            ILScanResults scanResults = null;
            if (useScanner)
            {
                ILScannerBuilder scannerBuilder = builder.GetILScannerBuilder()
                    .UseCompilationRoots(compilationRoots)
                    .UseMetadataManager(metadataManager)
                    .UseParallelism(_parallelism)
                    .UseInteropStubManager(interopStubManager)
                    .UseLogger(logger);

                if (_scanDgmlLogFileName != null)
                    scannerBuilder.UseDependencyTracking(_generateFullScanDgmlLog ? DependencyTrackingLevel.All : DependencyTrackingLevel.First);

                IILScanner scanner = scannerBuilder.ToILScanner();

                scanResults = scanner.Scan();

                metadataManager = ((UsageBasedMetadataManager)metadataManager).ToAnalysisBasedMetadataManager();

                interopStubManager = scanResults.GetInteropStubManager(interopStateManager, pinvokePolicy);
            }

            DebugInformationProvider debugInfoProvider = _enableDebugInfo ?
                (_ilDump == null ? new DebugInformationProvider() : new ILAssemblyGeneratingMethodDebugInfoProvider(_ilDump, new EcmaOnlyDebugInformationProvider())) :
                new NullDebugInformationProvider();

            DependencyTrackingLevel trackingLevel = _dgmlLogFileName == null ?
                DependencyTrackingLevel.None : (_generateFullDgmlLog ? DependencyTrackingLevel.All : DependencyTrackingLevel.First);

            compilationRoots.Add(metadataManager);
            compilationRoots.Add(interopStubManager);

            builder
                .UseInstructionSetSupport(instructionSetSupport)
                .UseBackendOptions(_codegenOptions)
                .UseMethodBodyFolding(enable: _methodBodyFolding)
                .UseParallelism(_parallelism)
                .UseMetadataManager(metadataManager)
                .UseInteropStubManager(interopStubManager)
                .UseLogger(logger)
                .UseDependencyTracking(trackingLevel)
                .UseCompilationRoots(compilationRoots)
                .UseOptimizationMode(_optimizationMode)
                .UseSecurityMitigationOptions(securityMitigationOptions)
                .UseDebugInfoProvider(debugInfoProvider)
                .UseDwarf5(_useDwarf5);

            if (scanResults != null)
            {
                // If we have a scanner, feed the vtable analysis results to the compilation.
                // This could be a command line switch if we really wanted to.
                builder.UseVTableSliceProvider(scanResults.GetVTableLayoutInfo());

                // If we have a scanner, feed the generic dictionary results to the compilation.
                // This could be a command line switch if we really wanted to.
                builder.UseGenericDictionaryLayoutProvider(scanResults.GetDictionaryLayoutInfo());

                // If we have a scanner, we can drive devirtualization using the information
                // we collected at scanning time (effectively sealing unsealed types if possible).
                // This could be a command line switch if we really wanted to.
                builder.UseDevirtualizationManager(scanResults.GetDevirtualizationManager());

                // If we use the scanner's result, we need to consult it to drive inlining.
                // This prevents e.g. devirtualizing and inlining methods on types that were
                // never actually allocated.
                builder.UseInliningPolicy(scanResults.GetInliningPolicy());

                // Use an error provider that prevents us from re-importing methods that failed
                // to import with an exception during scanning phase. We would see the same failure during
                // compilation, but before RyuJIT gets there, it might ask questions that we don't
                // have answers for because we didn't scan the entire method.
                builder.UseMethodImportationErrorProvider(scanResults.GetMethodImportationErrorProvider());
            }

            builder.UseResilience(_resilient);

            ICompilation compilation = builder.ToCompilation();

            ObjectDumper dumper = _mapFileName != null ? new ObjectDumper(_mapFileName) : null;

            CompilationResults compilationResults = compilation.Compile(_outputFilePath, dumper);
            if (_exportsFile != null)
            {
                ExportsFileWriter defFileWriter = new ExportsFileWriter(typeSystemContext, _exportsFile);
                foreach (var compilationRoot in compilationRoots)
                {
                    if (compilationRoot is ExportedMethodsRootProvider provider)
                        defFileWriter.AddExportedMethods(provider.ExportedMethods);
                }

                defFileWriter.EmitExportedMethods();
            }

            typeSystemContext.LogWarnings(logger);

            if (_dgmlLogFileName != null)
                compilationResults.WriteDependencyLog(_dgmlLogFileName);

            if (scanResults != null)
            {
                if (_scanDgmlLogFileName != null)
                    scanResults.WriteDependencyLog(_scanDgmlLogFileName);

                // If the scanner and compiler don't agree on what to compile, the outputs of the scanner might not actually be usable.
                // We are going to check this two ways:
                // 1. The methods and types generated during compilation are a subset of method and types scanned
                // 2. The methods and types scanned are a subset of methods and types compiled (this has a chance to hold for unoptimized builds only).

                // Check that methods and types generated during compilation are a subset of method and types scanned
                bool scanningFail = false;
                DiffCompilationResults(ref scanningFail, compilationResults.CompiledMethodBodies, scanResults.CompiledMethodBodies,
                    "Methods", "compiled", "scanned", method => !(method.GetTypicalMethodDefinition() is EcmaMethod) || IsRelatedToInvalidInput(method));
                DiffCompilationResults(ref scanningFail, compilationResults.ConstructedEETypes, scanResults.ConstructedEETypes,
                    "EETypes", "compiled", "scanned", type => !(type.GetTypeDefinition() is EcmaType));

                static bool IsRelatedToInvalidInput(MethodDesc method)
                {
                    // RyuJIT is more sensitive to invalid input and might detect cases that the scanner didn't have trouble with.
                    // If we find logic related to compiling fallback method bodies (methods that just throw) that got compiled
                    // but not scanned, it's usually fine. If it wasn't fine, we would probably crash before getting here.
                    return method.OwningType is MetadataType mdType
                        && mdType.Module == method.Context.SystemModule
                        && (mdType.Name.EndsWith("Exception") || mdType.Namespace.StartsWith("Internal.Runtime"));
                }

                // If optimizations are enabled, the results will for sure not match in the other direction due to inlining, etc.
                // But there's at least some value in checking the scanner doesn't expand the universe too much in debug.
                if (_optimizationMode == OptimizationMode.None)
                {
                    // Check that methods and types scanned are a subset of methods and types compiled

                    // If we find diffs here, they're not critical, but still might be causing a Size on Disk regression.
                    bool dummy = false;

                    // We additionally skip methods in SIMD module because there's just too many intrisics to handle and IL scanner
                    // doesn't expand them. They would show up as noisy diffs.
                    DiffCompilationResults(ref dummy, scanResults.CompiledMethodBodies, compilationResults.CompiledMethodBodies,
                    "Methods", "scanned", "compiled", method => !(method.GetTypicalMethodDefinition() is EcmaMethod) || method.OwningType.IsIntrinsic);
                    DiffCompilationResults(ref dummy, scanResults.ConstructedEETypes, compilationResults.ConstructedEETypes,
                        "EETypes", "scanned", "compiled", type => !(type.GetTypeDefinition() is EcmaType));
                }

                if (scanningFail)
                    throw new Exception("Scanning failure");
            }

            if (debugInfoProvider is IDisposable)
                ((IDisposable)debugInfoProvider).Dispose();

            preinitManager.LogStatistics(logger);

            return 0;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void DiffCompilationResults<T>(ref bool result, IEnumerable<T> set1, IEnumerable<T> set2, string prefix,
            string set1name, string set2name, Predicate<T> filter)
        {
            HashSet<T> diff = new HashSet<T>(set1);
            diff.ExceptWith(set2);

            // TODO: move ownership of compiler-generated entities to CompilerTypeSystemContext.
            // https://github.com/dotnet/corert/issues/3873
            diff.RemoveWhere(filter);

            if (diff.Count > 0)
            {
                result = true;

                Console.WriteLine($"*** {prefix} {set1name} but not {set2name}:");

                foreach (var d in diff)
                {
                    Console.WriteLine(d.ToString());
                }
            }
        }

        private TypeDesc FindType(CompilerTypeSystemContext context, string typeName)
        {
            ModuleDesc systemModule = context.SystemModule;

            TypeDesc foundType = systemModule.GetTypeByCustomAttributeTypeName(typeName, false, (typeDefName, module, throwIfNotFound) =>
            {
                return (MetadataType)context.GetCanonType(typeDefName)
                    ?? CustomAttributeTypeNameParser.ResolveCustomAttributeTypeDefinitionName(typeDefName, module, throwIfNotFound);
            });
            if (foundType == null)
                throw new CommandLineException($"Type '{typeName}' not found");

            return foundType;
        }

        private MethodDesc CheckAndParseSingleMethodModeArguments(CompilerTypeSystemContext context)
        {
            if (_singleMethodName == null && _singleMethodTypeName == null && _singleMethodGenericArgs == null)
                return null;

            if (_singleMethodName == null || _singleMethodTypeName == null)
                throw new CommandLineException("Both method name and type name are required parameters for single method mode");

            TypeDesc owningType = FindType(context, _singleMethodTypeName);

            // TODO: allow specifying signature to distinguish overloads
            MethodDesc method = owningType.GetMethod(_singleMethodName, null);
            if (method == null)
                throw new CommandLineException($"Method '{_singleMethodName}' not found in '{_singleMethodTypeName}'");

            if (method.HasInstantiation != (_singleMethodGenericArgs != null) ||
                (method.HasInstantiation && (method.Instantiation.Length != _singleMethodGenericArgs.Count)))
            {
                throw new CommandLineException(
                    $"Expected {method.Instantiation.Length} generic arguments for method '{_singleMethodName}' on type '{_singleMethodTypeName}'");
            }

            if (method.HasInstantiation)
            {
                List<TypeDesc> genericArguments = new List<TypeDesc>();
                foreach (var argString in _singleMethodGenericArgs)
                    genericArguments.Add(FindType(context, argString));
                method = method.MakeInstantiatedMethod(genericArguments.ToArray());
            }

            return method;
        }

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

        private static IEnumerable<int> ProcessWarningCodes(IEnumerable<string> warningCodes)
        {
            foreach (string value in warningCodes)
            {
                string[] values = value.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string id in values)
                {
                    if (!id.StartsWith("IL", StringComparison.Ordinal) || !ushort.TryParse(id.Substring(2), out ushort code))
                        continue;

                    yield return code;
                }
            }
        }

        private static int Main(string[] args)
        {
#if DEBUG
            try
            {
                return new Program().Run(args);
            }
            catch (CodeGenerationFailedException ex) when (DumpReproArguments(ex))
            {
                throw new NotSupportedException(); // Unreachable
            }
#else
            try
            {
                return new Program().Run(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error: " + e.Message);
                Console.Error.WriteLine(e.ToString());
                return 1;
            }
#endif
        }
    }
}
