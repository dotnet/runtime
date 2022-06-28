// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

using Internal.CommandLine;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.Reflection.ReadyToRun;

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
        private ulong _imageBase;

        // File names as strings in args
        private Dictionary<string, string> _inputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _unrootedInputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _referenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Modules and their names after loading
        private Dictionary<string, string> _allInputFilePaths = new Dictionary<string, string>();
        private List<ModuleDesc> _referenceableModules = new List<ModuleDesc>();

        private Dictionary<string, string> _inputbubblereferenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private CompilerTypeSystemContext _typeSystemContext;
        private ReadyToRunMethodLayoutAlgorithm _methodLayout;
        private ReadyToRunFileLayoutAlgorithm _fileLayout;

        private Program()
        {
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

            foreach (var reference in _commandLineOptions.InputBubbleReferenceFilePaths)
              Helpers.AppendExpandedPaths(_inputbubblereferenceFilePaths, reference, false);


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
                    "pettishansen" => ReadyToRunMethodLayoutAlgorithm.PettisHansen,
                    "random" => ReadyToRunMethodLayoutAlgorithm.Random,
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

        public static TargetArchitecture GetTargetArchitectureFromArg(string archArg, out bool armelAbi)
        {
            armelAbi = false;
            if (archArg.Equals("x86", StringComparison.OrdinalIgnoreCase))
                return TargetArchitecture.X86;
            else if (archArg.Equals("x64", StringComparison.OrdinalIgnoreCase))
                return  TargetArchitecture.X64;
            else if (archArg.Equals("arm", StringComparison.OrdinalIgnoreCase))
                return  TargetArchitecture.ARM;
            else if (archArg.Equals("armel", StringComparison.OrdinalIgnoreCase))
            {
                armelAbi = true;
                return TargetArchitecture.ARM;
            }
            else if (archArg.Equals("arm64", StringComparison.OrdinalIgnoreCase))
                return TargetArchitecture.ARM64;
            else
                throw new CommandLineException(SR.TargetArchitectureUnsupported);
        }

        private void ConfigureTarget()
        {
            //
            // Set target Architecture and OS
            //
            if (_commandLineOptions.TargetArch != null)
            {
                _targetArchitecture = GetTargetArchitectureFromArg(_commandLineOptions.TargetArch, out _armelAbi);
            }
            if (_commandLineOptions.TargetOS != null)
            {
                if (_commandLineOptions.TargetOS.Equals("windows", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.Windows;
                else if (_commandLineOptions.TargetOS.Equals("linux", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.Linux;
                else if (_commandLineOptions.TargetOS.Equals("osx", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.OSX;
                else if (_commandLineOptions.TargetOS.Equals("freebsd", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.FreeBSD;
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
                instructionSetSupportBuilder.AddSupportedInstructionSet("sse2"); // Lower baselines included by implication
            }
            else if (_targetArchitecture == TargetArchitecture.ARM64)
            {
                instructionSetSupportBuilder.AddSupportedInstructionSet("neon"); // Lower baselines included by implication
            }


            if (_commandLineOptions.InstructionSet != null)
            {
                List<string> instructionSetParams = new List<string>();

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
                    string instructionSet = instructionSetSpecifier.Substring(1);

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
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sse4.2"); // Lower SSE versions included by implication
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

        private void ConfigureImageBase(TargetDetails targetDetails)
        {
            bool is64BitTarget = targetDetails.PointerSize == sizeof(long);

            if (_commandLineOptions.ImageBase != null)
                _imageBase = is64BitTarget ? Convert.ToUInt64(_commandLineOptions.ImageBase, 16) : Convert.ToUInt32(_commandLineOptions.ImageBase, 16);
            else
                _imageBase = is64BitTarget ? PEWriter.PE64HeaderConstants.DllImageBase : PEWriter.PE32HeaderConstants.ImageBase;
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

            if (_commandLineOptions.SingleFileCompilation && !_commandLineOptions.OutNearInput)
                throw new CommandLineException(SR.MissingOutNearInput);

            ConfigureTarget();
            InstructionSetSupport instructionSetSupport = ConfigureInstructionSetSupport();

            SharedGenericsMode genericsMode = SharedGenericsMode.CanonicalReferenceTypes;

            var targetDetails = new TargetDetails(_targetArchitecture, _targetOS, _armelAbi ? TargetAbi.NativeAotArmel : TargetAbi.NativeAot, instructionSetSupport.GetVectorTSimdVector());

            ConfigureImageBase(targetDetails);

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
                // When we undo this hack, replace the foreach with
                //  typeSystemContext.InputFilePaths = inFilePaths;
                //

                foreach (var inputFile in _inputFilePaths)
                {
                    try
                    {
                        var module = _typeSystemContext.GetModuleFromPath(inputFile.Value);
                        if ((module.PEReader.PEHeaders.CorHeader.Flags & (CorFlags.ILLibrary | CorFlags.ILOnly)) == (CorFlags)0
                            && module.PEReader.TryGetReadyToRunHeader(out int _))
                        {
                            Console.WriteLine(SR.IgnoringCompositeImage, inputFile.Value);
                            continue;
                        }
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
                        EcmaModule module = _typeSystemContext.GetModuleFromPath(referenceFile, throwOnFailureToLoad: false);
                        if (module == null)
                            continue;

                        _referenceableModules.Add(module);
                        if (_commandLineOptions.InputBubble && _inputbubblereferenceFilePaths.Count == 0)
                        {
                            // In large version bubble mode add reference paths to the compilation group
                            // Consider bubble as large if no explicit bubble references were passed
                            versionBubbleModulesHash.Add(module);
                        }
                    }
                    catch { } // Ignore non-managed pe files
                }

                if (_commandLineOptions.InputBubble)
                {
                    foreach (var referenceFile in _inputbubblereferenceFilePaths.Values)
                    {
                        try
                        {
                            EcmaModule module = _typeSystemContext.GetModuleFromPath(referenceFile, throwOnFailureToLoad: false);

                            if (module == null)
                                continue;

                            versionBubbleModulesHash.Add(module);
                        }
                        catch { } // Ignore non-managed pe files
                    }
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
                        typeSystemContext.InputFilePaths = singleCompilationInputFilePaths;
                        typeSystemContext.ReferenceFilePaths = _referenceFilePaths;
                        typeSystemContext.SetSystemModule((EcmaModule)typeSystemContext.GetModuleForSimpleName(systemModuleName));
                    }

                    RunSingleCompilation(singleCompilationInputFilePaths, instructionSetSupport, compositeRootPath, unrootedInputFilePaths, singleCompilationVersionBubbleModulesHash, typeSystemContext);
                }

                // In case of inputbubble ni.dll are created as ni.dll.tmp in order to not interfere with crossgen2, move them all to ni.dll
                // See https://github.com/dotnet/runtime/issues/55663#issuecomment-898161751 for more details
                if (_commandLineOptions.InputBubble)
                {
                    foreach (var inputFile in inputFilePaths)
                    {
                        var tmpOutFile = inputFile.Value.Replace(".dll", ".ni.dll.tmp");
                        var outFile = inputFile.Value.Replace(".dll", ".ni.dll");
                        Console.WriteLine($@"Moving R2R PE file: {tmpOutFile} to {outFile}");
                        System.IO.File.Move(tmpOutFile, outFile);
                    }
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
            string inFilePath = inFilePaths.First().Value;
            string inputFileExtension = Path.GetExtension(inFilePath);
            string nearOutFilePath = inputFileExtension switch
            {
                ".dll" => Path.ChangeExtension(inFilePath,
                    _commandLineOptions.SingleFileCompilation && _commandLineOptions.InputBubble
                        ? ".ni.dll.tmp"
                        : ".ni.dll"),
                ".exe" => Path.ChangeExtension(inFilePath,
                    _commandLineOptions.SingleFileCompilation && _commandLineOptions.InputBubble
                        ? ".ni.exe.tmp"
                        : ".ni.exe"),
                _ => throw new CommandLineException(string.Format(SR.UnsupportedInputFileExtension, inputFileExtension))
            };
            string outFile = _commandLineOptions.OutNearInput ? nearOutFilePath : _commandLineOptions.OutputFilePath;

            using (PerfEventSource.StartStopEvents.CompilationEvents())
            {
                ICompilation compilation;
                using (PerfEventSource.StartStopEvents.LoadingEvents())
                {

                    List<EcmaModule> inputModules = new List<EcmaModule>();
                    List<EcmaModule> rootingModules = new List<EcmaModule>();

                    foreach (var inputFile in inFilePaths)
                    {
                        EcmaModule module = typeSystemContext.GetModuleFromPath(inputFile.Value);
                        inputModules.Add(module);
                        rootingModules.Add(module);
                        versionBubbleModulesHash.Add(module);


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
                    ReadyToRunCompilationModuleGroupConfig groupConfig = new ReadyToRunCompilationModuleGroupConfig();
                    groupConfig.Context = typeSystemContext;
                    groupConfig.IsCompositeBuildMode = _commandLineOptions.Composite;
                    groupConfig.IsInputBubble = _commandLineOptions.InputBubble;
                    groupConfig.CompilationModuleSet = inputModules;
                    groupConfig.VersionBubbleModuleSet = versionBubbleModules;
                    groupConfig.CompileGenericDependenciesFromVersionBubbleModuleSet = _commandLineOptions.CompileBubbleGenerics;

                    if (singleMethod != null)
                    {
                        // Compiling just a single method
                        compilationGroup = new SingleMethodCompilationModuleGroup(
                            groupConfig,
                            singleMethod);
                        compilationRoots.Add(new SingleMethodRootProvider(singleMethod));
                    }
                    else if (_commandLineOptions.CompileNoMethods)
                    {
                        compilationGroup = new NoMethodsCompilationModuleGroup(groupConfig);
                    }
                    else
                    {
                        // Single assembly compilation.
                        compilationGroup = new ReadyToRunSingleAssemblyCompilationModuleGroup(groupConfig);
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

                    CompositeImageSettings compositeImageSettings = new CompositeImageSettings();

                    if (_commandLineOptions.CompositeKeyFile != null)
                    {
                        byte[] compositeStrongNameKey = File.ReadAllBytes(_commandLineOptions.CompositeKeyFile);
                        if (!IsValidPublicKey(compositeStrongNameKey))
                        {
                            throw new Exception(string.Format(SR.ErrorCompositeKeyFileNotPublicKey));
                        }

                        compositeImageSettings.PublicKey = compositeStrongNameKey.ToImmutableArray();
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
                        .UseMapFile(_commandLineOptions.Map)
                        .UseMapCsvFile(_commandLineOptions.MapCsv)
                        .UsePdbFile(_commandLineOptions.Pdb, _commandLineOptions.PdbPath)
                        .UsePerfMapFile(_commandLineOptions.PerfMap, _commandLineOptions.PerfMapPath, _commandLineOptions.PerfMapFormatVersion)
                        .UseProfileFile(jsonProfile != null)
                        .UseProfileData(profileDataManager)
                        .FileLayoutAlgorithms(_methodLayout, _fileLayout)
                        .UseCompositeImageSettings(compositeImageSettings)
                        .UseJitPath(_commandLineOptions.JitPath)
                        .UseInstructionSetSupport(instructionSetSupport)
                        .UseCustomPESectionAlignment(_commandLineOptions.CustomPESectionAlignment)
                        .UseVerifyTypeAndFieldLayout(_commandLineOptions.VerifyTypeAndFieldLayout)
                        .GenerateOutputFile(outFile)
                        .UseImageBase(_imageBase)
                        .UseILProvider(ilProvider)
                        .UseBackendOptions(_commandLineOptions.CodegenOptions)
                        .UseLogger(logger)
                        .UseParallelism(_commandLineOptions.Parallelism)
                        .UseResilience(_commandLineOptions.Resilient)
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

        private enum AlgorithmClass
        {
            Signature = 1,
            Hash = 4,
        }

        private enum AlgorithmSubId
        {
            Sha1Hash = 4,
            MacHash = 5,
            RipeMdHash = 6,
            RipeMd160Hash = 7,
            Ssl3ShaMD5Hash = 8,
            HmacHash = 9,
            Tls1PrfHash = 10,
            HashReplacOwfHash = 11,
            Sha256Hash = 12,
            Sha384Hash = 13,
            Sha512Hash = 14,
        }

        private struct AlgorithmId
        {
            // From wincrypt.h
            private const int AlgorithmClassOffset = 13;
            private const int AlgorithmClassMask = 0x7;
            private const int AlgorithmSubIdOffset = 0;
            private const int AlgorithmSubIdMask = 0x1ff;

            private readonly uint _flags;

            public const int RsaSign = 0x00002400;
            public const int Sha = 0x00008004;

            public bool IsSet
            {
                get { return _flags != 0; }
            }

            public AlgorithmClass Class
            {
                get { return (AlgorithmClass)((_flags >> AlgorithmClassOffset) & AlgorithmClassMask); }
            }

            public AlgorithmSubId SubId
            {
                get { return (AlgorithmSubId)((_flags >> AlgorithmSubIdOffset) & AlgorithmSubIdMask); }
            }

            public AlgorithmId(uint flags)
            {
                _flags = flags;
            }
        }

        private static ReadOnlySpan<byte> s_ecmaKey => new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0 };

        private const int SnPublicKeyBlobSize = 13;

        // From wincrypt.h
        private const byte PublicKeyBlobId = 0x06;
        private const byte PrivateKeyBlobId = 0x07;

        // internal for testing
        internal const int s_publicKeyHeaderSize = SnPublicKeyBlobSize - 1;

        // From StrongNameInternal.cpp
        // Checks to see if a public key is a valid instance of a PublicKeyBlob as
        // defined in StongName.h
        internal static bool IsValidPublicKey(byte[] blob)
        {
            // The number of public key bytes must be at least large enough for the header and one byte of data.
            if (blob.Length < s_publicKeyHeaderSize + 1)
            {
                return false;
            }

            // Check for the ECMA key, which does not obey the invariants checked below.
            if (blob.AsSpan().SequenceEqual(s_ecmaKey))
            {
                return true;
            }

            var blobReader = new BinaryReader(new MemoryStream(blob, writable: false));

            // Signature algorithm ID
            var sigAlgId = blobReader.ReadUInt32();
            // Hash algorithm ID
            var hashAlgId = blobReader.ReadUInt32();
            // Size of public key data in bytes, not including the header
            var publicKeySize = blobReader.ReadUInt32();
            // publicKeySize bytes of public key data
            var publicKey = blobReader.ReadByte();

            // The number of public key bytes must be the same as the size of the header plus the size of the public key data.
            if (blob.Length != s_publicKeyHeaderSize + publicKeySize)
            {
                return false;
            }

            // The public key must be in the wincrypto PUBLICKEYBLOB format
            if (publicKey != PublicKeyBlobId)
            {
                return false;
            }

            var signatureAlgorithmId = new AlgorithmId(sigAlgId);
            if (signatureAlgorithmId.IsSet && signatureAlgorithmId.Class != AlgorithmClass.Signature)
            {
                return false;
            }

            var hashAlgorithmId = new AlgorithmId(hashAlgId);
            if (hashAlgorithmId.IsSet && (hashAlgorithmId.Class != AlgorithmClass.Hash || hashAlgorithmId.SubId < AlgorithmSubId.Sha1Hash))
            {
                return false;
            }

            return true;
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
