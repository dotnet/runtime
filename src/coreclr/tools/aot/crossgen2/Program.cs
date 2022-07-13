// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.Reflection.ReadyToRun;
using ILCompiler.DependencyAnalysis;
using ILCompiler.IBC;

namespace ILCompiler;

internal class Program
{
    private readonly Crossgen2RootCommand _command;

    public Program(Crossgen2RootCommand command)
    {
        _command = command;

        if (command.Result.GetValueForOption(command.WaitForDebugger))
        {
            Console.WriteLine("Waiting for debugger to attach. Press ENTER to continue");
            Console.ReadLine();
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
            if (_targetOS == TargetOS.OSX)
            {
                // For osx-arm64 we know that apple-m1 is a baseline
                instructionSetSupportBuilder.AddSupportedInstructionSet("apple-m1");
            }
            else
            {
                instructionSetSupportBuilder.AddSupportedInstructionSet("neon"); // Lower baselines included by implication
            }
        }

        if (_command.InstructionSet != null)
        {
            List<string> instructionSetParams = new List<string>();

            // Normalize instruction set format to include implied +.
            string[] instructionSetParamsInput = _command.InstructionSet.Split(",");
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

    private ulong ConfigureImageBase(TargetDetails targetDetails)
    {
        bool is64BitTarget = targetDetails.PointerSize == sizeof(long);

        if (_command.ImageBase != null)
            return is64BitTarget ? Convert.ToUInt64(_command.ImageBase, 16) : Convert.ToUInt32(_command.ImageBase, 16);

        return is64BitTarget ? PEWriter.PE64HeaderConstants.DllImageBase : PEWriter.PE32HeaderConstants.ImageBase;
    }

    private int Run(string[] args)
    {
        InitializeDefaultOptions();

        ProcessCommandLine(args);

        if (_command.Help)
        {
            Console.WriteLine(_command.HelpText);
            return 1;
        }

        if (_command.Version)
        {
            string version = GetCompilerVersion();
            Console.WriteLine(version);
            return 0;
        }

        if (_command.OutputFilePath == null && !_command.OutNearInput)
            throw new CommandLineException(SR.MissingOutputFile);

        if (_command.SingleFileCompilation && !_command.OutNearInput)
            throw new CommandLineException(SR.MissingOutNearInput);

        ConfigureTarget();
        InstructionSetSupport instructionSetSupport = ConfigureInstructionSetSupport();

        SharedGenericsMode genericsMode = SharedGenericsMode.CanonicalReferenceTypes;

        var targetDetails = new TargetDetails(_targetArchitecture, _targetOS, _armelAbi ? TargetAbi.NativeAotArmel : TargetAbi.NativeAot, instructionSetSupport.GetVectorTSimdVector());

        bool versionBubbleIncludesCoreLib = false;
        if (_command.InputBubble)
        {
            versionBubbleIncludesCoreLib = true;
        }
        else
        {
            if (!_command.SingleFileCompilation)
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

        string compositeRootPath = _command.CompositeRootPath;

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
                if (_command.InputFilePaths.Count > 0)
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
                    if (_command.InputBubble && _inputbubblereferenceFilePaths.Count == 0)
                    {
                        // In large version bubble mode add reference paths to the compilation group
                        // Consider bubble as large if no explicit bubble references were passed
                        versionBubbleModulesHash.Add(module);
                    }
                }
                catch { } // Ignore non-managed pe files
            }

            if (_command.InputBubble)
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

        string systemModuleName = _command.SystemModule ?? Helpers.DefaultSystemModule;
        _typeSystemContext.SetSystemModule((EcmaModule)_typeSystemContext.GetModuleForSimpleName(systemModuleName));
        CompilerTypeSystemContext typeSystemContext = _typeSystemContext;

        if (_command.SingleFileCompilation)
        {
            var singleCompilationInputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var inputFile in inputFilePaths)
            {
                var singleCompilationVersionBubbleModulesHash = new HashSet<ModuleDesc>(versionBubbleModulesHash);

                singleCompilationInputFilePaths.Clear();
                singleCompilationInputFilePaths.Add(inputFile.Key, inputFile.Value);
                typeSystemContext.InputFilePaths = singleCompilationInputFilePaths;

                if (!_command.InputBubble)
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
            if (_command.InputBubble)
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
                _command.SingleFileCompilation && _command.InputBubble
                    ? ".ni.dll.tmp"
                    : ".ni.dll"),
            ".exe" => Path.ChangeExtension(inFilePath,
                _command.SingleFileCompilation && _command.InputBubble
                    ? ".ni.exe.tmp"
                    : ".ni.exe"),
            _ => throw new CommandLineException(string.Format(SR.UnsupportedInputFileExtension, inputFileExtension))
        };
        string outFile = _command.OutNearInput ? nearOutFilePath : _command.OutputFilePath;

        using (PerfEventSource.StartStopEvents.CompilationEvents())
        {
            ICompilation compilation;
            using (PerfEventSource.StartStopEvents.LoadingEvents())
            {
                List<EcmaModule> inputModules = new List<EcmaModule>();
                List<EcmaModule> rootingModules = new List<EcmaModule>();
                HashSet<EcmaModule> crossModuleInlineableCode = new HashSet<EcmaModule>();

                foreach (var inputFile in inFilePaths)
                {
                    EcmaModule module = typeSystemContext.GetModuleFromPath(inputFile.Value);
                    inputModules.Add(module);
                    rootingModules.Add(module);
                    versionBubbleModulesHash.Add(module);


                    if (!_command.CompositeOrInputBubble)
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

                if (_command.CrossModuleInlining != null)
                {
                    foreach (var crossModulePgoAssemblyName in _command.CrossModuleInlining)
                    {
                        foreach (var module in _referenceableModules)
                        {
                            if (!versionBubbleModulesHash.Contains(module))
                            {
                                if (crossModulePgoAssemblyName == "*" ||
                                        (String.Compare(crossModulePgoAssemblyName, module.Assembly.GetName().Name, StringComparison.OrdinalIgnoreCase) == 0))
                                {
                                    crossModuleInlineableCode.Add((EcmaModule)module);
                                }
                            }
                        }
                    }
                }

                //
                // Initialize compilation group and compilation roots
                //

                // Single method mode?
                MethodDesc singleMethod = CheckAndParseSingleMethodModeArguments(typeSystemContext);

                var logger = new Logger(Console.Out, _command.Verbose);

                List<string> mibcFiles = new List<string>();
                foreach (var file in _command.MibcFilePaths)
                {
                    mibcFiles.Add(file);
                }

                List<ModuleDesc> versionBubbleModules = new List<ModuleDesc>(versionBubbleModulesHash);

                if (!_command.Composite && inputModules.Count != 1)
                {
                    throw new Exception(string.Format(SR.ErrorMultipleInputFilesCompositeModeOnly, string.Join("; ", inputModules)));
                }


                ReadyToRunCompilationModuleGroupBase compilationGroup;
                List<ICompilationRootProvider> compilationRoots = new List<ICompilationRootProvider>();
                ReadyToRunCompilationModuleGroupConfig groupConfig = new ReadyToRunCompilationModuleGroupConfig();
                groupConfig.Context = typeSystemContext;
                groupConfig.IsCompositeBuildMode = _command.Composite;
                groupConfig.IsInputBubble = _command.InputBubble;
                groupConfig.CompilationModuleSet = inputModules;
                groupConfig.VersionBubbleModuleSet = versionBubbleModules;
                groupConfig.CompileGenericDependenciesFromVersionBubbleModuleSet = _command.CompileBubbleGenerics;
                groupConfig.CrossModuleGenericCompilation = crossModuleInlineableCode.Count > 0;
                groupConfig.CrossModuleInlining = groupConfig.CrossModuleGenericCompilation; // Currently we set these flags to the same values
                groupConfig.CrossModuleInlineable = crossModuleInlineableCode;
                groupConfig.CompileAllPossibleCrossModuleCode = false;

                // Handle non-local generics command line option
                ModuleDesc nonLocalGenericsHome = _command.CompileBubbleGenerics ? inputModules[0] : null;
                if (_command.NonLocalGenericsModule == "*")
                {
                    groupConfig.CompileAllPossibleCrossModuleCode = true;
                    nonLocalGenericsHome = inputModules[0];
                }
                else if (_command.NonLocalGenericsModule == "")
                {
                    // Nothing was specified
                }
                else
                {
                    bool matchFound = false;

                    // Allow module to be specified by assembly name or by filename
                    if (_command.NonLocalGenericsModule.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        _command.NonLocalGenericsModule = Path.GetFileNameWithoutExtension(_command.NonLocalGenericsModule);
                    foreach (var module in inputModules)
                    {
                        if (String.Compare(module.Assembly.GetName().Name, _command.NonLocalGenericsModule, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            matchFound = true;
                            nonLocalGenericsHome = module;
                            groupConfig.CompileAllPossibleCrossModuleCode = true;
                            break;
                        }
                    }

                    if (!matchFound)
                    {
                        foreach (var module in _referenceableModules)
                        {
                            if (String.Compare(module.Assembly.GetName().Name, _command.NonLocalGenericsModule, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                matchFound = true;
                                break;
                            }
                        }

                        if (!matchFound)
                        {
                            throw new CommandLineException(string.Format(SR.ErrorNonLocalGenericsModule, _command.NonLocalGenericsModule));
                        }
                    }
                }

                if (singleMethod != null)
                {
                    // Compiling just a single method
                    compilationGroup = new SingleMethodCompilationModuleGroup(
                        groupConfig,
                        singleMethod);
                    compilationRoots.Add(new SingleMethodRootProvider(singleMethod));
                }
                else if (_command.CompileNoMethods)
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

                if (!string.IsNullOrEmpty(_command.CallChainProfileFile))
                {
                    jsonProfile = new CallChainProfile(_command.CallChainProfileFile, typeSystemContext, _referenceableModules);
                }

                // Examine profile guided information as appropriate
                MIbcProfileParser.MibcGroupParseRules parseRule;
                if (nonLocalGenericsHome != null)
                {
                    parseRule = MIbcProfileParser.MibcGroupParseRules.VersionBubbleWithCrossModule2;
                }
                else
                {
                    parseRule = MIbcProfileParser.MibcGroupParseRules.VersionBubbleWithCrossModule1;
                }

                ProfileDataManager profileDataManager =
                    new ProfileDataManager(logger,
                    _referenceableModules,
                    inputModules,
                    versionBubbleModules,
                    crossModuleInlineableCode,
                    nonLocalGenericsHome,
                    mibcFiles,
                    parseRule,
                    jsonProfile,
                    typeSystemContext,
                    compilationGroup,
                    _command.EmbedPgoData,
                    crossModuleInlineableCode.Count == 0 ? compilationGroup.VersionsWithMethodBody : compilationGroup.CrossModuleInlineable);

                compilationGroup.ApplyProfileGuidedOptimizationData(profileDataManager, _command.Partial);

                if ((singleMethod == null) && !_command.CompileNoMethods)
                {
                    // For normal compilations add compilation roots.
                    foreach (var module in rootingModules)
                    {
                        compilationRoots.Add(new ReadyToRunRootProvider(
                            module,
                            profileDataManager,
                            profileDrivenPartialNGen: _command.Partial));

                        if (!_command.CompositeOrInputBubble)
                        {
                            break;
                        }
                    }
                }
                // In single-file compilation mode, use the assembly's DebuggableAttribute to determine whether to optimize
                // or produce debuggable code if an explicit optimization level was not specified on the command line
                OptimizationMode optimizationMode = _optimizationMode;
                if (optimizationMode == OptimizationMode.None && !_command.OptimizeDisabled && !_command.Composite)
                {
                    System.Diagnostics.Debug.Assert(inputModules.Count == 1);
                    optimizationMode = ((EcmaAssembly)inputModules[0].Assembly).HasOptimizationsDisabled() ? OptimizationMode.None : OptimizationMode.Blended;
                }

                CompositeImageSettings compositeImageSettings = new CompositeImageSettings();

                if (_command.CompositeKeyFile != null)
                {
                    byte[] compositeStrongNameKey = File.ReadAllBytes(_command.CompositeKeyFile);
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

                ILProvider ilProvider = new ReadyToRunILProvider(compilationGroup);

                DependencyTrackingLevel trackingLevel = _command.DgmlLogFileName == null ?
                    DependencyTrackingLevel.None : (_command.GenerateFullDgmlLog ? DependencyTrackingLevel.All : DependencyTrackingLevel.First);

                NodeFactoryOptimizationFlags nodeFactoryFlags = new NodeFactoryOptimizationFlags();
                nodeFactoryFlags.OptimizeAsyncMethods = _command.AsyncMethodOptimization;

                builder
                    .UseMapFile(_command.Map)
                    .UseMapCsvFile(_command.MapCsv)
                    .UsePdbFile(_command.Pdb, _command.PdbPath)
                    .UsePerfMapFile(_command.PerfMap, _command.PerfMapPath, _command.PerfMapFormatVersion)
                    .UseProfileFile(jsonProfile != null)
                    .UseProfileData(profileDataManager)
                    .UseNodeFactoryOptimizationFlags(nodeFactoryFlags)
                    .FileLayoutAlgorithms(_methodLayout, _fileLayout)
                    .UseCompositeImageSettings(compositeImageSettings)
                    .UseJitPath(_command.JitPath)
                    .UseInstructionSetSupport(instructionSetSupport)
                    .UseCustomPESectionAlignment(_command.CustomPESectionAlignment)
                    .UseVerifyTypeAndFieldLayout(_command.VerifyTypeAndFieldLayout)
                    .GenerateOutputFile(outFile)
                    .UseImageBase(ConfigureImageBase(targetDetails))
                    .UseILProvider(ilProvider)
                    .UseBackendOptions(_command.CodegenOptions)
                    .UseLogger(logger)
                    .UseParallelism(_command.Parallelism)
                    .UseResilience(_command.Resilient)
                    .UseDependencyTracking(trackingLevel)
                    .UseCompilationRoots(compilationRoots)
                    .UseOptimizationMode(optimizationMode);

                if (_command.PrintReproInstructions)
                    builder.UsePrintReproInstructions(CreateReproArgumentString);

                compilation = builder.ToCompilation();

            }
            compilation.Compile(outFile);

            if (_command.DgmlLogFileName != null)
                compilation.WriteDependencyLog(_command.DgmlLogFileName);

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
        if (_command.SingleMethodName == null && _command.SingleMethodTypeName == null && _command.SingleMethodGenericArg == null)
            return null;

        if (_command.SingleMethodName == null || _command.SingleMethodTypeName == null)
            throw new CommandLineException(SR.TypeAndMethodNameNeeded);

        TypeDesc owningType = FindType(context, _command.SingleMethodTypeName);

        // TODO: allow specifying signature to distinguish overloads
        MethodDesc method = null;
        bool printMethodList = false;
        int curIndex = 0;
        foreach (var searchMethod in owningType.GetMethods())
        {
            if (searchMethod.Name != _command.SingleMethodName)
                continue;

            curIndex++;
            if (_command.SingleMethodIndex != 0)
            {
                if (curIndex == _command.SingleMethodIndex)
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
                if (searchMethod.Name != _command.SingleMethodName)
                    continue;

                curIndex++;
                Console.WriteLine($"{curIndex} - {searchMethod}");
            }
            throw new CommandLineException(SR.SingleMethodIndexNeeded);
        }

        if (method == null)
            throw new CommandLineException(string.Format(SR.MethodNotFoundOnType, _command.SingleMethodName, _command.SingleMethodTypeName));

        if (method.HasInstantiation != (_command.SingleMethodGenericArg != null) ||
            (method.HasInstantiation && (method.Instantiation.Length != _command.SingleMethodGenericArg.Count)))
        {
            throw new CommandLineException(
                string.Format(SR.GenericArgCountMismatch, method.Instantiation.Length, _command.SingleMethodName, _command.SingleMethodTypeName));
        }

        if (method.HasInstantiation)
        {
            List<TypeDesc> genericArguments = new List<TypeDesc>();
            foreach (var argString in _command.SingleMethodGenericArg)
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

    private static int Main(string[] args) =>
        new CommandLineBuilder(new Crossgen2RootCommand(args))
            .UseVersionOption()
            .UseHelp(context => context.HelpBuilder.CustomizeLayout(GetExtendedHelp))
            .UseParseErrorReporting()
            .Build()
            .Invoke(args);


    private T Get<T>(Option<T> option) => _command.Result.GetValueForOption(option);

    private static int Main(string[] args) =>
        new CommandLineBuilder(new Crossgen2RootCommand(args))
            .UseVersionOption()
            .UseHelp(context => context.HelpBuilder.CustomizeLayout(Crossgen2RootCommand.GetExtendedHelp))
            .UseParseErrorReporting()
            .Build()
            .Invoke(args);
}
