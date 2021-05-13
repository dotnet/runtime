// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

using Internal.CommandLine;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    internal class Program
    {
        private const string DefaultSystemModule = "System.Private.CoreLib";

        private CommandLineOptions _commandLineOptions;
        public TargetOS _targetOS;
        public TargetArchitecture _targetArchitecture;
        private bool _armelAbi = false;
        public OptimizationMode _optimizationMode;

        // File names as strings in args
        private Dictionary<string, string> _inputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _unrootedInputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _referenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Modules and their names after loading
        private Dictionary<string, string> _allInputFilePaths = new Dictionary<string, string>();
        private List<ModuleDesc> _referenceableModules = new List<ModuleDesc>();

        private CompilerTypeSystemContext _typeSystemContext;
        private ReadyToRunMethodLayoutAlgorithm _methodLayout;
        private ReadyToRunFileLayoutAlgorithm _fileLayout;

        private Program()
        {
        }

        private void InitializeDefaultOptions()
        {
            // We could offer this as a command line option, but then we also need to
            // load a different RyuJIT, so this is a future nice to have...
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _targetOS = TargetOS.Windows;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                _targetOS = TargetOS.Linux;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                _targetOS = TargetOS.OSX;
            else
                throw new NotImplementedException();

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    _targetArchitecture = TargetArchitecture.X86;
                    break;
                case Architecture.X64:
                    _targetArchitecture = TargetArchitecture.X64;
                    break;
                case Architecture.Arm:
                    _targetArchitecture = TargetArchitecture.ARM;
                    break;
                case Architecture.Arm64:
                    _targetArchitecture = TargetArchitecture.ARM64;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void ProcessCommandLine(string[] args)
        {
            PerfEventSource.StartStopEvents.CommandLineProcessingStart();
            _commandLineOptions = new CommandLineOptions(args);
            PerfEventSource.StartStopEvents.CommandLineProcessingStop();

            if (_commandLineOptions.Help)
            {
                return;
            }

            if (_commandLineOptions.WaitForDebugger)
            {
                Console.WriteLine(SR.WaitingForDebuggerAttach);
                Console.ReadLine();
            }

            if (_commandLineOptions.CompileBubbleGenerics)
            {
                if (!_commandLineOptions.CompositeOrInputBubble)
                {
                    Console.WriteLine(SR.WarningIgnoringBubbleGenerics);
                    _commandLineOptions.CompileBubbleGenerics = false;
                }
            }

            _optimizationMode = OptimizationMode.None;
            if (_commandLineOptions.OptimizeDisabled)
            {
                if (_commandLineOptions.Optimize || _commandLineOptions.OptimizeSpace || _commandLineOptions.OptimizeTime)
                    Console.WriteLine(SR.WarningOverridingOptimize);
            }
            else if (_commandLineOptions.OptimizeSpace)
            {
                if (_commandLineOptions.OptimizeTime)
                    Console.WriteLine(SR.WarningOverridingOptimizeSpace);
                _optimizationMode = OptimizationMode.PreferSize;
            }
            else if (_commandLineOptions.OptimizeTime)
                _optimizationMode = OptimizationMode.PreferSpeed;
            else if (_commandLineOptions.Optimize)
                _optimizationMode = OptimizationMode.Blended;

            foreach (var input in _commandLineOptions.InputFilePaths)
                Helpers.AppendExpandedPaths(_inputFilePaths, input, true);

            foreach (var input in _commandLineOptions.UnrootedInputFilePaths)
                Helpers.AppendExpandedPaths(_unrootedInputFilePaths, input, true);

            foreach (var reference in _commandLineOptions.ReferenceFilePaths)
                Helpers.AppendExpandedPaths(_referenceFilePaths, reference, false);


            int alignment = _commandLineOptions.CustomPESectionAlignment;
            if (alignment != 0)
            {
                // Must be a power of two and >= 4096
                if (alignment < 4096 || (alignment & (alignment - 1)) != 0)
                    throw new CommandLineException(SR.InvalidCustomPESectionAlignment);
            }

            if (_commandLineOptions.MethodLayout != null)
            {
                _methodLayout = _commandLineOptions.MethodLayout.ToLowerInvariant() switch
                {
                    "defaultsort" => ReadyToRunMethodLayoutAlgorithm.DefaultSort,
                    "exclusiveweight" => ReadyToRunMethodLayoutAlgorithm.ExclusiveWeight,
                    "hotcold" => ReadyToRunMethodLayoutAlgorithm.HotCold,
                    "hotwarmcold" => ReadyToRunMethodLayoutAlgorithm.HotWarmCold,
                    "callfrequency" => ReadyToRunMethodLayoutAlgorithm.CallFrequency,
                    _ => throw new CommandLineException(SR.InvalidMethodLayout)
                };
            }

            if (_commandLineOptions.FileLayout != null)
            {
                _fileLayout = _commandLineOptions.FileLayout.ToLowerInvariant() switch
                {
                    "defaultsort" => ReadyToRunFileLayoutAlgorithm.DefaultSort,
                    "methodorder" => ReadyToRunFileLayoutAlgorithm.MethodOrder,
                    _ => throw new CommandLineException(SR.InvalidFileLayout)
                };
            }

        }

        private void ConfigureTarget()
        {
            //
            // Set target Architecture and OS
            //
            if (_commandLineOptions.TargetArch != null)
            {
                if (_commandLineOptions.TargetArch.Equals("x86", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.X86;
                else if (_commandLineOptions.TargetArch.Equals("x64", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.X64;
                else if (_commandLineOptions.TargetArch.Equals("arm", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.ARM;
                else if (_commandLineOptions.TargetArch.Equals("armel", StringComparison.OrdinalIgnoreCase))
                {
                    _targetArchitecture = TargetArchitecture.ARM;
                    _armelAbi = true;
                }
                else if (_commandLineOptions.TargetArch.Equals("arm64", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.ARM64;
                else
                    throw new CommandLineException(SR.TargetArchitectureUnsupported);
            }
            if (_commandLineOptions.TargetOS != null)
            {
                if (_commandLineOptions.TargetOS.Equals("windows", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.Windows;
                else if (_commandLineOptions.TargetOS.Equals("linux", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.Linux;
                else if (_commandLineOptions.TargetOS.Equals("osx", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.OSX;
                else
                    throw new CommandLineException(SR.TargetOSUnsupported);
            }
        }

        private InstructionSetSupport ConfigureInstructionSetSupport()
        {
            InstructionSetSupportBuilder instructionSetSupportBuilder = new InstructionSetSupportBuilder(_targetArchitecture);

            // Ready to run images are built with certain instruction set baselines
            if ((_targetArchitecture == TargetArchitecture.X86) || (_targetArchitecture == TargetArchitecture.X64))
            {
                instructionSetSupportBuilder.AddSupportedInstructionSet("sse");
                instructionSetSupportBuilder.AddSupportedInstructionSet("sse2");
            }
            else if (_targetArchitecture == TargetArchitecture.ARM64)
            {
                instructionSetSupportBuilder.AddSupportedInstructionSet("base");
                instructionSetSupportBuilder.AddSupportedInstructionSet("neon");
            }


            if (_commandLineOptions.InstructionSet != null)
            {
                List<string> instructionSetParams = new List<string>();

                // At this time, instruction sets may only be specified with --input-bubble, as
                // we do not yet have a stable ABI for all vector parameter/return types.
                if (!_commandLineOptions.InputBubble)
                    throw new CommandLineException(SR.InstructionSetWithoutInputBubble);

                // Normalize instruction set format to include implied +.
                string[] instructionSetParamsInput = _commandLineOptions.InstructionSet.Split(",");
                for (int i = 0; i < instructionSetParamsInput.Length; i++)
                {
                    string instructionSet = instructionSetParamsInput[i];

                    if (String.IsNullOrEmpty(instructionSet))
                        throw new CommandLineException(String.Format(SR.InstructionSetMustNotBe, ""));

                    char firstChar = instructionSet[0];
                    if ((firstChar != '+') && (firstChar != '-'))
                    {
                        instructionSet =  "+" + instructionSet;
                    }
                    instructionSetParams.Add(instructionSet);
                }

                Dictionary<string, bool> instructionSetSpecification = new Dictionary<string, bool>();
                foreach (string instructionSetSpecifier in instructionSetParams)
                {
                    string instructionSet = instructionSetSpecifier.Substring(1, instructionSetSpecifier.Length - 1);

                    bool enabled = instructionSetSpecifier[0] == '+' ? true : false;
                    if (enabled)
                    {
                        if (!instructionSetSupportBuilder.AddSupportedInstructionSet(instructionSet))
                            throw new CommandLineException(String.Format(SR.InstructionSetMustNotBe, instructionSet));
                    }
                    else
                    {
                        if (!instructionSetSupportBuilder.RemoveInstructionSetSupport(instructionSet))
                            throw new CommandLineException(String.Format(SR.InstructionSetMustNotBe, instructionSet));
                    }
                }
            }

            instructionSetSupportBuilder.ComputeInstructionSetFlags(out var supportedInstructionSet, out var unsupportedInstructionSet,
                (string specifiedInstructionSet, string impliedInstructionSet) =>
                    throw new CommandLineException(String.Format(SR.InstructionSetInvalidImplication, specifiedInstructionSet, impliedInstructionSet)));

            InstructionSetSupportBuilder optimisticInstructionSetSupportBuilder = new InstructionSetSupportBuilder(_targetArchitecture);

            // Ready to run images are built with certain instruction sets that are optimistically assumed to be present
            if ((_targetArchitecture == TargetArchitecture.X86) || (_targetArchitecture == TargetArchitecture.X64))
            {
                // For ReadyToRun we set these hardware features as enabled always, as most
                // of hardware in the wild supports them. Note that we do not indicate support for AVX, or any other
                // instruction set which uses the VEX encodings as the presence of those makes otherwise acceptable
                // code be unusable on hardware which does not support VEX encodings, as well as emulators that do not
                // support AVX instructions. As the jit generates logic that depends on these features it will call
                // notifyInstructionSetUsage, which will result in generation of a fixup to verify the behavior of
                // code.
                //
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sse");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sse2");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sse4.1");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sse4.2");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("aes");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("pclmul");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("popcnt");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("lzcnt");
            }

            optimisticInstructionSetSupportBuilder.ComputeInstructionSetFlags(out var optimisticInstructionSet, out _,
                (string specifiedInstructionSet, string impliedInstructionSet) => throw new NotSupportedException());
            optimisticInstructionSet.Remove(unsupportedInstructionSet);
            optimisticInstructionSet.Add(supportedInstructionSet);

            return new InstructionSetSupport(supportedInstructionSet,
                                                                  unsupportedInstructionSet,
                                                                  optimisticInstructionSet,
                                                                  InstructionSetSupportBuilder.GetNonSpecifiableInstructionSetsForArch(_targetArchitecture),
                                                                  _targetArchitecture);
        }

        private int Run(string[] args)
        {
            InitializeDefaultOptions();

            ProcessCommandLine(args);

            if (_commandLineOptions.Help)
            {
                Console.WriteLine(_commandLineOptions.HelpText);
                return 1;
            }

            if (_commandLineOptions.OutputFilePath == null && !_commandLineOptions.OutNearInput)
                throw new CommandLineException(SR.MissingOutputFile);

            ConfigureTarget();
            InstructionSetSupport instructionSetSupport = ConfigureInstructionSetSupport();

            SharedGenericsMode genericsMode = SharedGenericsMode.CanonicalReferenceTypes;

            var targetDetails = new TargetDetails(_targetArchitecture, _targetOS, _armelAbi ? TargetAbi.CoreRTArmel : TargetAbi.CoreRT, instructionSetSupport.GetVectorTSimdVector());

            bool versionBubbleIncludesCoreLib = false;
            if (_commandLineOptions.InputBubble)
            {
                versionBubbleIncludesCoreLib = true;
            }
            else
            {
                if (!_commandLineOptions.SingleFileCompilation)
                {
                    foreach (var inputFile in _inputFilePaths)
                    {
                        if (String.Compare(inputFile.Key, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            versionBubbleIncludesCoreLib = true;
                            break;
                        }
                    }
                }
                if (!versionBubbleIncludesCoreLib)
                {
                    foreach (var inputFile in _unrootedInputFilePaths)
                    {
                        if (String.Compare(inputFile.Key, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            versionBubbleIncludesCoreLib = true;
                            break;
                        }
                    }
                }
            }

            //
            // Initialize type system context
            //
            _typeSystemContext = new ReadyToRunCompilerContext(targetDetails, genericsMode, versionBubbleIncludesCoreLib);

            string compositeRootPath = _commandLineOptions.CompositeRootPath;

            // Collections for already loaded modules
            Dictionary<string, string> inputFilePaths = new Dictionary<string, string>();
            Dictionary<string, string> unrootedInputFilePaths = new Dictionary<string, string>();
            HashSet<ModuleDesc> versionBubbleModulesHash = new HashSet<ModuleDesc>();

            using (PerfEventSource.StartStopEvents.LoadingEvents())
            {
                //
                // TODO: To support our pre-compiled test tree, allow input files that aren't managed assemblies since
                // some tests contain a mixture of both managed and native binaries.
                //
                // See: https://github.com/dotnet/corert/issues/2785
                //
                // When we undo this this hack, replace this foreach with
                //  typeSystemContext.InputFilePaths = inFilePaths;
                //

                foreach (var inputFile in _inputFilePaths)
                {
                    try
                    {
                        var module = _typeSystemContext.GetModuleFromPath(inputFile.Value);
                        _allInputFilePaths.Add(inputFile.Key, inputFile.Value);
                        inputFilePaths.Add(inputFile.Key, inputFile.Value);
                        _referenceableModules.Add(module);
                        if (compositeRootPath == null)
                        {
                            compositeRootPath = Path.GetDirectoryName(inputFile.Value);
                        }
                    }
                    catch (TypeSystemException.BadImageFormatException)
                    {
                        // Keep calm and carry on.
                    }
                }

                foreach (var unrootedInputFile in _unrootedInputFilePaths)
                {
                    try
                    {
                        var module = _typeSystemContext.GetModuleFromPath(unrootedInputFile.Value);
                        if (!_allInputFilePaths.ContainsKey(unrootedInputFile.Key))
                        {
                            _allInputFilePaths.Add(unrootedInputFile.Key, unrootedInputFile.Value);
                            unrootedInputFilePaths.Add(unrootedInputFile.Key, unrootedInputFile.Value);
                            _referenceableModules.Add(module);
                            if (compositeRootPath == null)
                            {
                                compositeRootPath = Path.GetDirectoryName(unrootedInputFile.Value);
                            }
                        }
                    }
                    catch (TypeSystemException.BadImageFormatException)
                    {
                        // Keep calm and carry on.
                    }
                }

                CheckManagedCppInputFiles(_allInputFilePaths.Values);

                _typeSystemContext.InputFilePaths = _allInputFilePaths;
                _typeSystemContext.ReferenceFilePaths = _referenceFilePaths;

                if (_typeSystemContext.InputFilePaths.Count == 0)
                {
                    if (_commandLineOptions.InputFilePaths.Count > 0)
                    {
                        Console.WriteLine(SR.InputWasNotLoadable);
                        return 2;
                    }
                    throw new CommandLineException(SR.NoInputFiles);
                }

                foreach (var referenceFile in _referenceFilePaths.Values)
                {
                    try
                    {
                        EcmaModule module = _typeSystemContext.GetModuleFromPath(referenceFile);
                        _referenceableModules.Add(module);
                        if (_commandLineOptions.InputBubble)
                        {
                            // In large version bubble mode add reference paths to the compilation group
                            versionBubbleModulesHash.Add(module);
                        }
                    }
                    catch { } // Ignore non-managed pe files
                }
            }

            string systemModuleName = _commandLineOptions.SystemModule ?? DefaultSystemModule;
            _typeSystemContext.SetSystemModule((EcmaModule)_typeSystemContext.GetModuleForSimpleName(systemModuleName));
            CompilerTypeSystemContext typeSystemContext = _typeSystemContext;

            if (_commandLineOptions.SingleFileCompilation)
            {
                var singleCompilationInputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var inputFile in inputFilePaths)
                {
                    var singleCompilationVersionBubbleModulesHash = new HashSet<ModuleDesc>(versionBubbleModulesHash);

                    singleCompilationInputFilePaths.Clear();
                    singleCompilationInputFilePaths.Add(inputFile.Key, inputFile.Value);
                    typeSystemContext.InputFilePaths = singleCompilationInputFilePaths;

                    if (!_commandLineOptions.InputBubble)
                    {
                        bool singleCompilationVersionBubbleIncludesCoreLib = versionBubbleIncludesCoreLib || (String.Compare(inputFile.Key, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) == 0);

                        typeSystemContext = new ReadyToRunCompilerContext(targetDetails, genericsMode, singleCompilationVersionBubbleIncludesCoreLib, _typeSystemContext);
                        typeSystemContext.SetSystemModule((EcmaModule)typeSystemContext.GetModuleForSimpleName(systemModuleName));
                    }

                    RunSingleCompilation(singleCompilationInputFilePaths, instructionSetSupport, compositeRootPath, unrootedInputFilePaths, singleCompilationVersionBubbleModulesHash, typeSystemContext);
                }
            }
            else
            {
                RunSingleCompilation(inputFilePaths, instructionSetSupport, compositeRootPath, unrootedInputFilePaths, versionBubbleModulesHash, typeSystemContext);
            }

            return 0;
        }

        private void RunSingleCompilation(Dictionary<string, string> inFilePaths, InstructionSetSupport instructionSetSupport, string compositeRootPath, Dictionary<string, string> unrootedInputFilePaths, HashSet<ModuleDesc> versionBubbleModulesHash, CompilerTypeSystemContext typeSystemContext)
        {
            //
            // Initialize output filename
            //
            var outFile = _commandLineOptions.OutNearInput ? inFilePaths.First().Value.Replace(".dll", ".ni.dll") : _commandLineOptions.OutputFilePath;

            using (PerfEventSource.StartStopEvents.CompilationEvents())
            {
                ICompilation compilation;
                using (PerfEventSource.StartStopEvents.LoadingEvents())
                {

                    List<EcmaModule> inputModules = new List<EcmaModule>();
                    List<EcmaModule> rootingModules = new List<EcmaModule>();
                    Guid? inputModuleMvid = null;

                    foreach (var inputFile in inFilePaths)
                    {
                        EcmaModule module = typeSystemContext.GetModuleFromPath(inputFile.Value);
                        inputModules.Add(module);
                        rootingModules.Add(module);
                        versionBubbleModulesHash.Add(module);

                        if (!_commandLineOptions.Composite && !inputModuleMvid.HasValue)
                        {
                            inputModuleMvid = module.MetadataReader.GetGuid(module.MetadataReader.GetModuleDefinition().Mvid);
                        }

                        if (!_commandLineOptions.CompositeOrInputBubble)
                        {
                            break;
                        }
                    }

                    foreach (var unrootedInputFile in unrootedInputFilePaths)
                    {
                        EcmaModule module = typeSystemContext.GetModuleFromPath(unrootedInputFile.Value);
                        inputModules.Add(module);
                        versionBubbleModulesHash.Add(module);
                    }

                    //
                    // Initialize compilation group and compilation roots
                    //

                    // Single method mode?
                    MethodDesc singleMethod = CheckAndParseSingleMethodModeArguments(typeSystemContext);

                    var logger = new Logger(Console.Out, _commandLineOptions.Verbose);

                    List<string> mibcFiles = new List<string>();
                    foreach (var file in _commandLineOptions.MibcFilePaths)
                    {
                        mibcFiles.Add(file);
                    }

                    List<ModuleDesc> versionBubbleModules = new List<ModuleDesc>(versionBubbleModulesHash);

                    if (!_commandLineOptions.Composite && inputModules.Count != 1)
                    {
                        throw new Exception(string.Format(SR.ErrorMultipleInputFilesCompositeModeOnly, string.Join("; ", inputModules)));
                    }

                    ReadyToRunCompilationModuleGroupBase compilationGroup;
                    List<ICompilationRootProvider> compilationRoots = new List<ICompilationRootProvider>();
                    if (singleMethod != null)
                    {
                        // Compiling just a single method
                        compilationGroup = new SingleMethodCompilationModuleGroup(
                            typeSystemContext,
                            _commandLineOptions.Composite,
                            _commandLineOptions.InputBubble,
                            inputModules,
                            versionBubbleModules,
                            _commandLineOptions.CompileBubbleGenerics,
                            singleMethod);
                        compilationRoots.Add(new SingleMethodRootProvider(singleMethod));
                    }
                    else if (_commandLineOptions.CompileNoMethods)
                    {
                        compilationGroup = new NoMethodsCompilationModuleGroup(
                            typeSystemContext,
                            _commandLineOptions.Composite,
                            _commandLineOptions.InputBubble,
                            inputModules,
                            versionBubbleModules,
                            _commandLineOptions.CompileBubbleGenerics);
                    }
                    else
                    {
                        // Single assembly compilation.
                        compilationGroup = new ReadyToRunSingleAssemblyCompilationModuleGroup(
                            typeSystemContext,
                            _commandLineOptions.Composite,
                            _commandLineOptions.InputBubble,
                            inputModules,
                            versionBubbleModules,
                            _commandLineOptions.CompileBubbleGenerics);
                    }

                    // Load any profiles generated by method call chain analyis
                    CallChainProfile jsonProfile = null;

                    if (!string.IsNullOrEmpty(_commandLineOptions.CallChainProfileFile))
                    {
                        jsonProfile = new CallChainProfile(_commandLineOptions.CallChainProfileFile, typeSystemContext, _referenceableModules);
                    }

                    // Examine profile guided information as appropriate
                    ProfileDataManager profileDataManager =
                        new ProfileDataManager(logger,
                        _referenceableModules,
                        inputModules,
                        versionBubbleModules,
                        _commandLineOptions.CompileBubbleGenerics ? inputModules[0] : null,
                        mibcFiles,
                        jsonProfile,
                        typeSystemContext,
                        compilationGroup,
                        _commandLineOptions.EmbedPgoData);

                    if (_commandLineOptions.Partial)
                        compilationGroup.ApplyProfilerGuidedCompilationRestriction(profileDataManager);
                    else
                        compilationGroup.ApplyProfilerGuidedCompilationRestriction(null);

                    if ((singleMethod == null) && !_commandLineOptions.CompileNoMethods)
                    {
                        // For normal compilations add compilation roots.
                        foreach (var module in rootingModules)
                        {
                            compilationRoots.Add(new ReadyToRunRootProvider(
                                module,
                                profileDataManager,
                                profileDrivenPartialNGen: _commandLineOptions.Partial));

                            if (!_commandLineOptions.CompositeOrInputBubble)
                            {
                                break;
                            }
                        }
                    }
                    // In single-file compilation mode, use the assembly's DebuggableAttribute to determine whether to optimize
                    // or produce debuggable code if an explicit optimization level was not specified on the command line
                    OptimizationMode optimizationMode = _optimizationMode;
                    if (optimizationMode == OptimizationMode.None && !_commandLineOptions.OptimizeDisabled && !_commandLineOptions.Composite)
                    {
                        System.Diagnostics.Debug.Assert(inputModules.Count == 1);
                        optimizationMode = ((EcmaAssembly)inputModules[0].Assembly).HasOptimizationsDisabled() ? OptimizationMode.None : OptimizationMode.Blended;
                    }

                    //
                    // Compile
                    //

                    ReadyToRunCodegenCompilationBuilder builder = new ReadyToRunCodegenCompilationBuilder(
                        typeSystemContext, compilationGroup, _allInputFilePaths.Values, compositeRootPath);
                    string compilationUnitPrefix = "";
                    builder.UseCompilationUnitPrefix(compilationUnitPrefix);

                    ILProvider ilProvider = new ReadyToRunILProvider();

                    DependencyTrackingLevel trackingLevel = _commandLineOptions.DgmlLogFileName == null ?
                        DependencyTrackingLevel.None : (_commandLineOptions.GenerateFullDgmlLog ? DependencyTrackingLevel.All : DependencyTrackingLevel.First);

                    builder
                        .UseIbcTuning(_commandLineOptions.Tuning)
                        .UseResilience(_commandLineOptions.Resilient)
                        .UseMapFile(_commandLineOptions.Map)
                        .UseMapCsvFile(_commandLineOptions.MapCsv)
                        .UsePdbFile(_commandLineOptions.Pdb, _commandLineOptions.PdbPath)
                        .UsePerfMapFile(_commandLineOptions.PerfMap, _commandLineOptions.PerfMapPath, inputModuleMvid)
                        .UseProfileFile(jsonProfile != null)
                        .UseParallelism(_commandLineOptions.Parallelism)
                        .UseProfileData(profileDataManager)
                        .FileLayoutAlgorithms(_methodLayout, _fileLayout)
                        .UseJitPath(_commandLineOptions.JitPath)
                        .UseInstructionSetSupport(instructionSetSupport)
                        .UseCustomPESectionAlignment(_commandLineOptions.CustomPESectionAlignment)
                        .UseVerifyTypeAndFieldLayout(_commandLineOptions.VerifyTypeAndFieldLayout)
                        .GenerateOutputFile(outFile)
                        .UseILProvider(ilProvider)
                        .UseBackendOptions(_commandLineOptions.CodegenOptions)
                        .UseLogger(logger)
                        .UseDependencyTracking(trackingLevel)
                        .UseCompilationRoots(compilationRoots)
                        .UseOptimizationMode(optimizationMode);

                    if (_commandLineOptions.PrintReproInstructions)
                        builder.UsePrintReproInstructions(CreateReproArgumentString);

                    compilation = builder.ToCompilation();

                }
                compilation.Compile(outFile);

                if (_commandLineOptions.DgmlLogFileName != null)
                    compilation.WriteDependencyLog(_commandLineOptions.DgmlLogFileName);

                compilation.Dispose();
            }
        }

        private void CheckManagedCppInputFiles(IEnumerable<string> inputPaths)
        {
            foreach (string inputFilePath in inputPaths)
            {
                EcmaModule module = _typeSystemContext.GetModuleFromPath(inputFilePath);
                if ((module.PEReader.PEHeaders.CorHeader.Flags & (CorFlags.ILLibrary | CorFlags.ILOnly)) == (CorFlags)0)
                {
                    throw new CommandLineException(string.Format(SR.ManagedCppNotSupported, inputFilePath));
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
                throw new CommandLineException(string.Format(SR.TypeNotFound, typeName));

            return foundType;
        }

        private MethodDesc CheckAndParseSingleMethodModeArguments(CompilerTypeSystemContext context)
        {
            if (_commandLineOptions.SingleMethodName == null && _commandLineOptions.SingleMethodTypeName == null && _commandLineOptions.SingleMethodGenericArg == null)
                return null;

            if (_commandLineOptions.SingleMethodName == null || _commandLineOptions.SingleMethodTypeName == null)
                throw new CommandLineException(SR.TypeAndMethodNameNeeded);

            TypeDesc owningType = FindType(context, _commandLineOptions.SingleMethodTypeName);

            // TODO: allow specifying signature to distinguish overloads
            MethodDesc method = null;
            bool printMethodList = false;
            int curIndex = 0;
            foreach (var searchMethod in owningType.GetMethods())
            {
                if (searchMethod.Name != _commandLineOptions.SingleMethodName)
                    continue;

                curIndex++;
                if (_commandLineOptions.SingleMethodIndex != 0)
                {
                    if (curIndex == _commandLineOptions.SingleMethodIndex)
                    {
                        method = searchMethod;
                        break;
                    }
                }
                else
                {
                    if (method == null)
                    {
                        method = searchMethod;
                    }
                    else
                    {
                        printMethodList = true;
                    }
                }
            }

            if (printMethodList)
            {
                curIndex = 0;
                foreach (var searchMethod in owningType.GetMethods())
                {
                    if (searchMethod.Name != _commandLineOptions.SingleMethodName)
                        continue;

                    curIndex++;
                    Console.WriteLine($"{curIndex} - {searchMethod}");
                }
                throw new CommandLineException(SR.SingleMethodIndexNeeded);
            }

            if (method == null)
                throw new CommandLineException(string.Format(SR.MethodNotFoundOnType, _commandLineOptions.SingleMethodName, _commandLineOptions.SingleMethodTypeName));

            if (method.HasInstantiation != (_commandLineOptions.SingleMethodGenericArg != null) ||
                (method.HasInstantiation && (method.Instantiation.Length != _commandLineOptions.SingleMethodGenericArg.Count)))
            {
                throw new CommandLineException(
                    string.Format(SR.GenericArgCountMismatch, method.Instantiation.Length, _commandLineOptions.SingleMethodName, _commandLineOptions.SingleMethodTypeName));
            }

            if (method.HasInstantiation)
            {
                List<TypeDesc> genericArguments = new List<TypeDesc>();
                foreach (var argString in _commandLineOptions.SingleMethodGenericArg)
                    genericArguments.Add(FindType(context, argString));
                method = method.MakeInstantiatedMethod(genericArguments.ToArray());
            }

            return method;
        }

        private static string CreateReproArgumentString(MethodDesc method)
        {
            StringBuilder sb = new StringBuilder();

            var formatter = new CustomAttributeTypeNameFormatter((IAssemblyDesc)method.Context.SystemModule);

            sb.Append($"--singlemethodtypename \"{formatter.FormatName(method.OwningType, true)}\"");
            sb.Append($" --singlemethodname \"{method.Name}\"");
            {
                int curIndex = 0;
                foreach (var searchMethod in method.OwningType.GetMethods())
                {
                    if (searchMethod.Name != method.Name)
                        continue;

                    curIndex++;
                    if (searchMethod == method.GetMethodDefinition())
                    {
                        sb.Append($" --singlemethodindex {curIndex}");
                    }
                }
            }

            for (int i = 0; i < method.Instantiation.Length; i++)
                sb.Append($" --singlemethodgenericarg \"{formatter.FormatName(method.Instantiation[i], true)}\"");

            return sb.ToString();
        }

        private static bool DumpReproArguments(CodeGenerationFailedException ex)
        {
            Console.WriteLine(SR.DumpReproInstructions);

            MethodDesc failingMethod = ex.Method;
            Console.WriteLine(CreateReproArgumentString(failingMethod));
            return false;
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
                Console.Error.WriteLine(string.Format(SR.ProgramError, e.Message));
                Console.Error.WriteLine(e.ToString());
                return 1;
            }
#endif
        }
    }
}
