// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.CommandLine;
using System.CommandLine.Parsing;
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

namespace ILCompiler
{
    internal sealed class Program
    {
        private readonly Crossgen2RootCommand _command;

        // Modules and their names after loading
        private Dictionary<string, string> _allInputFilePaths = new();
        private List<ModuleDesc> _referenceableModules = new();

        private ReadyToRunCompilerContext _typeSystemContext;
        private ulong _imageBase;

        private readonly bool _inputBubble;
        private readonly bool _singleFileCompilation;
        private readonly bool _outNearInput;
        private readonly string _outputFilePath;

        public Program(Crossgen2RootCommand command)
        {
            _command = command;
            _inputBubble = Get(command.InputBubble);
            _singleFileCompilation = Get(command.SingleFileCompilation);
            _outNearInput = Get(command.OutNearInput);
            _outputFilePath = Get(command.OutputFilePath);

            if (Get(command.WaitForDebugger))
            {
                Console.WriteLine("Waiting for debugger to attach. Press ENTER to continue");
                Console.ReadLine();
            }
        }

        private void ConfigureImageBase(TargetDetails targetDetails)
        {
            bool is64BitTarget = targetDetails.PointerSize == sizeof(long);

            string imageBaseArg = Get(_command.ImageBase);
            if (imageBaseArg != null)
                _imageBase = is64BitTarget ? Convert.ToUInt64(imageBaseArg, 16) : Convert.ToUInt32(imageBaseArg, 16);
            else
                _imageBase = is64BitTarget ? PEWriter.PE64HeaderConstants.DllImageBase : PEWriter.PE32HeaderConstants.ImageBase;
        }

        public int Run()
        {
            if (_outputFilePath == null && !_outNearInput)
                throw new CommandLineException(SR.MissingOutputFile);

            if (_singleFileCompilation && !_outNearInput)
                throw new CommandLineException(SR.MissingOutNearInput);

            TargetArchitecture targetArchitecture = Get(_command.TargetArchitecture);
            TargetOS targetOS = Get(_command.TargetOS);
            InstructionSetSupport instructionSetSupport = Helpers.ConfigureInstructionSetSupport(Get(_command.InstructionSet), targetArchitecture, targetOS,
                SR.InstructionSetMustNotBe, SR.InstructionSetInvalidImplication);
            SharedGenericsMode genericsMode = SharedGenericsMode.CanonicalReferenceTypes;
            var targetDetails = new TargetDetails(targetArchitecture, targetOS, Crossgen2RootCommand.IsArmel ? TargetAbi.NativeAotArmel : TargetAbi.NativeAot, instructionSetSupport.GetVectorTSimdVector());

            ConfigureImageBase(targetDetails);

            bool versionBubbleIncludesCoreLib = false;
            Dictionary<string, string> inputFilePathsArg = _command.Result.GetValue(_command.InputFilePaths);
            Dictionary<string, string> unrootedInputFilePathsArg = Get(_command.UnrootedInputFilePaths);

            if (_inputBubble)
            {
                versionBubbleIncludesCoreLib = true;
            }
            else
            {
                if (!_singleFileCompilation)
                {
                    foreach (var inputFile in inputFilePathsArg)
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
                    foreach (var inputFile in unrootedInputFilePathsArg)
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
            _typeSystemContext = new ReadyToRunCompilerContext(targetDetails, genericsMode, versionBubbleIncludesCoreLib, instructionSetSupport);

            string compositeRootPath = Get(_command.CompositeRootPath);

            // Collections for already loaded modules
            Dictionary<string, string> inputFilePaths = new Dictionary<string, string>();
            Dictionary<string, string> unrootedInputFilePaths = new Dictionary<string, string>();
            HashSet<ModuleDesc> versionBubbleModulesHash = new HashSet<ModuleDesc>();
            Dictionary<string, string> referenceFilePaths = Get(_command.ReferenceFilePaths);

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

                foreach (var inputFile in inputFilePathsArg)
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

                foreach (var unrootedInputFile in unrootedInputFilePathsArg)
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
                _typeSystemContext.ReferenceFilePaths = referenceFilePaths;

                if (_typeSystemContext.InputFilePaths.Count == 0)
                {
                    if (inputFilePathsArg.Count > 0)
                    {
                        Console.WriteLine(SR.InputWasNotLoadable);
                        return 2;
                    }
                    throw new CommandLineException(SR.NoInputFiles);
                }

                Dictionary<string, string> inputBubbleReferenceFilePaths = Get(_command.InputBubbleReferenceFilePaths);
                foreach (var referenceFile in referenceFilePaths.Values)
                {
                    try
                    {
                        EcmaModule module = _typeSystemContext.GetModuleFromPath(referenceFile, throwOnFailureToLoad: false);
                        if (module == null)
                            continue;

                        _referenceableModules.Add(module);
                        if (_inputBubble && inputBubbleReferenceFilePaths.Count == 0)
                        {
                            // In large version bubble mode add reference paths to the compilation group
                            // Consider bubble as large if no explicit bubble references were passed
                            versionBubbleModulesHash.Add(module);
                        }
                    }
                    catch { } // Ignore non-managed pe files
                }

                if (_inputBubble)
                {
                    foreach (var referenceFile in inputBubbleReferenceFilePaths.Values)
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

            string systemModuleName = Get(_command.SystemModuleName) ?? Helpers.DefaultSystemModule;
            _typeSystemContext.SetSystemModule((EcmaModule)_typeSystemContext.GetModuleForSimpleName(systemModuleName));
            ReadyToRunCompilerContext typeSystemContext = _typeSystemContext;

            if (_singleFileCompilation)
            {
                var singleCompilationInputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var inputFile in inputFilePaths)
                {
                    var singleCompilationVersionBubbleModulesHash = new HashSet<ModuleDesc>(versionBubbleModulesHash);

                    singleCompilationInputFilePaths.Clear();
                    singleCompilationInputFilePaths.Add(inputFile.Key, inputFile.Value);
                    typeSystemContext.InputFilePaths = singleCompilationInputFilePaths;

                    if (!_inputBubble)
                    {
                        bool singleCompilationVersionBubbleIncludesCoreLib = versionBubbleIncludesCoreLib || (String.Compare(inputFile.Key, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) == 0);

                        typeSystemContext = new ReadyToRunCompilerContext(targetDetails, genericsMode, singleCompilationVersionBubbleIncludesCoreLib, _typeSystemContext.InstructionSetSupport, _typeSystemContext);
                        typeSystemContext.InputFilePaths = singleCompilationInputFilePaths;
                        typeSystemContext.ReferenceFilePaths = referenceFilePaths;
                        typeSystemContext.SetSystemModule((EcmaModule)typeSystemContext.GetModuleForSimpleName(systemModuleName));
                    }

                    RunSingleCompilation(singleCompilationInputFilePaths, instructionSetSupport, compositeRootPath, unrootedInputFilePaths, singleCompilationVersionBubbleModulesHash, typeSystemContext);
                }

                // In case of inputbubble ni.dll are created as ni.dll.tmp in order to not interfere with crossgen2, move them all to ni.dll
                // See https://github.com/dotnet/runtime/issues/55663#issuecomment-898161751 for more details
                if (_inputBubble)
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

        private void RunSingleCompilation(Dictionary<string, string> inFilePaths, InstructionSetSupport instructionSetSupport, string compositeRootPath, Dictionary<string, string> unrootedInputFilePaths, HashSet<ModuleDesc> versionBubbleModulesHash, ReadyToRunCompilerContext typeSystemContext)
        {
            //
            // Initialize output filename
            //
            var e = inFilePaths.GetEnumerator();
            e.MoveNext();
            string inFilePath = e.Current.Value;
            string inputFileExtension = Path.GetExtension(inFilePath);
            string nearOutFilePath = inputFileExtension switch
            {
                ".dll" => Path.ChangeExtension(inFilePath,
                    _singleFileCompilation&& _inputBubble
                        ? ".ni.dll.tmp"
                        : ".ni.dll"),
                ".exe" => Path.ChangeExtension(inFilePath,
                    _singleFileCompilation && _inputBubble
                        ? ".ni.exe.tmp"
                        : ".ni.exe"),
                _ => throw new CommandLineException(string.Format(SR.UnsupportedInputFileExtension, inputFileExtension))
            };

            string outFile = _outNearInput ? nearOutFilePath : _outputFilePath;
            string dgmlLogFileName = Get(_command.DgmlLogFileName);

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

                    string[] crossModuleInlining = Get(_command.CrossModuleInlining);
                    if (crossModuleInlining.Length > 0)
                    {
                        foreach (var crossModulePgoAssemblyName in crossModuleInlining)
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

                    var logger = new Logger(Console.Out, Get(_command.IsVerbose));

                    List<string> mibcFiles = new List<string>();
                    foreach (var file in Get(_command.MibcFilePaths))
                    {
                        mibcFiles.Add(file);
                    }

                    List<ModuleDesc> versionBubbleModules = new List<ModuleDesc>(versionBubbleModulesHash);
                    bool composite = Get(_command.Composite);
                    if (!composite && inputModules.Count != 1)
                    {
                        throw new Exception(string.Format(SR.ErrorMultipleInputFilesCompositeModeOnly, string.Join("; ", inputModules)));
                    }

                    bool compileBubbleGenerics = Get(_command.CompileBubbleGenerics);
                    ReadyToRunCompilationModuleGroupBase compilationGroup;
                    List<ICompilationRootProvider> compilationRoots = new List<ICompilationRootProvider>();
                    ReadyToRunCompilationModuleGroupConfig groupConfig = new ReadyToRunCompilationModuleGroupConfig();
                    groupConfig.Context = typeSystemContext;
                    groupConfig.IsCompositeBuildMode = composite;
                    groupConfig.IsInputBubble = _inputBubble;
                    groupConfig.CompilationModuleSet = inputModules;
                    groupConfig.VersionBubbleModuleSet = versionBubbleModules;
                    groupConfig.CompileGenericDependenciesFromVersionBubbleModuleSet = compileBubbleGenerics;
                    groupConfig.CrossModuleGenericCompilation = crossModuleInlineableCode.Count > 0;
                    groupConfig.CrossModuleInlining = groupConfig.CrossModuleGenericCompilation; // Currently we set these flags to the same values
                    groupConfig.CrossModuleInlineable = crossModuleInlineableCode;
                    groupConfig.CompileAllPossibleCrossModuleCode = false;
                    groupConfig.InstructionSetSupport = instructionSetSupport;

                    // Handle non-local generics command line option
                    ModuleDesc nonLocalGenericsHome = compileBubbleGenerics ? inputModules[0] : null;
                    string nonLocalGenericsModule = Get(_command.NonLocalGenericsModule);
                    if (nonLocalGenericsModule == "*")
                    {
                        groupConfig.CompileAllPossibleCrossModuleCode = true;
                        nonLocalGenericsHome = inputModules[0];
                    }
                    else if (nonLocalGenericsModule == "")
                    {
                        // Nothing was specified
                    }
                    else
                    {
                        bool matchFound = false;

                        // Allow module to be specified by assembly name or by filename
                        if (nonLocalGenericsModule.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                            nonLocalGenericsModule = Path.GetFileNameWithoutExtension(nonLocalGenericsModule);
                        foreach (var module in inputModules)
                        {
                            if (String.Compare(module.Assembly.GetName().Name, nonLocalGenericsModule, StringComparison.OrdinalIgnoreCase) == 0)
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
                                if (String.Compare(module.Assembly.GetName().Name, nonLocalGenericsModule, StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    matchFound = true;
                                    break;
                                }
                            }

                            if (!matchFound)
                            {
                                throw new CommandLineException(string.Format(SR.ErrorNonLocalGenericsModule, nonLocalGenericsModule));
                            }
                        }
                    }

                    bool compileNoMethods = Get(_command.CompileNoMethods);
                    if (singleMethod != null)
                    {
                        // Compiling just a single method
                        compilationGroup = new SingleMethodCompilationModuleGroup(
                            groupConfig,
                            singleMethod);
                        compilationRoots.Add(new SingleMethodRootProvider(singleMethod));
                    }
                    else if (compileNoMethods)
                    {
                        compilationGroup = new NoMethodsCompilationModuleGroup(groupConfig);
                    }
                    else
                    {
                        // Single assembly compilation.
                        compilationGroup = new ReadyToRunSingleAssemblyCompilationModuleGroup(groupConfig);
                    }

                    // R2R field layout needs compilation group information
                    typeSystemContext.SetCompilationGroup(compilationGroup);

                    // Load any profiles generated by method call chain analyis
                    CallChainProfile jsonProfile = null;
                    string callChainProfileFile = Get(_command.CallChainProfileFile);
                    if (!string.IsNullOrEmpty(callChainProfileFile))
                    {
                        jsonProfile = new CallChainProfile(callChainProfileFile, typeSystemContext, _referenceableModules);
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
                        Get(_command.EmbedPgoData),
                        Get(_command.SupportIbc),
                        crossModuleInlineableCode.Count == 0 ? compilationGroup.VersionsWithMethodBody : compilationGroup.CrossModuleInlineable,
                        Get(_command.SynthesizeRandomMibc));

                    bool partial = Get(_command.Partial);
                    compilationGroup.ApplyProfileGuidedOptimizationData(profileDataManager, partial);

                    if ((singleMethod == null) && !compileNoMethods)
                    {
                        // For normal compilations add compilation roots.
                        foreach (var module in rootingModules)
                        {
                            compilationRoots.Add(new ReadyToRunProfilingRootProvider(module, profileDataManager));
                            // If we're doing partial precompilation, only use profile data.
                            if (!partial)
                            {
                                if (ReadyToRunVisibilityRootProvider.UseVisibilityBasedRootProvider(module))
                                {
                                    compilationRoots.Add(new ReadyToRunVisibilityRootProvider(module));

                                    if (ReadyToRunXmlRootProvider.TryCreateRootProviderFromEmbeddedDescriptorFile(module, out ReadyToRunXmlRootProvider xmlProvider))
                                    {
                                        compilationRoots.Add(xmlProvider);
                                    }
                                }
                                else
                                {
                                    compilationRoots.Add(new ReadyToRunLibraryRootProvider(module));
                                }
                            }

                            if (!_command.CompositeOrInputBubble)
                            {
                                break;
                            }
                        }
                    }
                    // In single-file compilation mode, use the assembly's DebuggableAttribute to determine whether to optimize
                    // or produce debuggable code if an explicit optimization level was not specified on the command line
                    OptimizationMode optimizationMode = _command.OptimizationMode;
                    if (optimizationMode == OptimizationMode.None && !Get(_command.OptimizeDisabled) && !composite)
                    {
                        System.Diagnostics.Debug.Assert(inputModules.Count == 1);
                        optimizationMode = ((EcmaAssembly)inputModules[0].Assembly).HasOptimizationsDisabled() ? OptimizationMode.None : OptimizationMode.Blended;
                    }

                    CompositeImageSettings compositeImageSettings = new CompositeImageSettings();
                    string compositeKeyFile = Get(_command.CompositeKeyFile);
                    if (compositeKeyFile != null)
                    {
                        byte[] compositeStrongNameKey = File.ReadAllBytes(compositeKeyFile);
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

                    DependencyTrackingLevel trackingLevel = dgmlLogFileName == null ?
                        DependencyTrackingLevel.None : (Get(_command.GenerateFullDgmlLog) ? DependencyTrackingLevel.All : DependencyTrackingLevel.First);

                    NodeFactoryOptimizationFlags nodeFactoryFlags = new NodeFactoryOptimizationFlags();
                    nodeFactoryFlags.OptimizeAsyncMethods = Get(_command.AsyncMethodOptimization);

                    builder
                        .UseMapFile(Get(_command.Map))
                        .UseMapCsvFile(Get(_command.MapCsv))
                        .UsePdbFile(Get(_command.Pdb), Get(_command.PdbPath))
                        .UsePerfMapFile(Get(_command.PerfMap), Get(_command.PerfMapPath), Get(_command.PerfMapFormatVersion))
                        .UseProfileFile(jsonProfile != null)
                        .UseProfileData(profileDataManager)
                        .UseNodeFactoryOptimizationFlags(nodeFactoryFlags)
                        .FileLayoutAlgorithms(Get(_command.MethodLayout), Get(_command.FileLayout))
                        .UseCompositeImageSettings(compositeImageSettings)
                        .UseJitPath(Get(_command.JitPath))
                        .UseInstructionSetSupport(instructionSetSupport)
                        .UseCustomPESectionAlignment(Get(_command.CustomPESectionAlignment))
                        .UseVerifyTypeAndFieldLayout(Get(_command.VerifyTypeAndFieldLayout))
                        .UseHotColdSplitting(Get(_command.HotColdSplitting))
                        .GenerateOutputFile(outFile)
                        .UseImageBase(_imageBase)
                        .UseILProvider(ilProvider)
                        .UseBackendOptions(Get(_command.CodegenOptions))
                        .UseLogger(logger)
                        .UseParallelism(Get(_command.Parallelism))
                        .UseResilience(Get(_command.Resilient))
                        .UseDependencyTracking(trackingLevel)
                        .UseCompilationRoots(compilationRoots)
                        .UseOptimizationMode(optimizationMode);

                    if (Get(_command.PrintReproInstructions))
                        builder.UsePrintReproInstructions(CreateReproArgumentString);

                    compilation = builder.ToCompilation();

                }
                compilation.Compile(outFile);

                if (dgmlLogFileName != null)
                    compilation.WriteDependencyLog(dgmlLogFileName);

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

            TypeDesc foundType = systemModule.GetTypeByCustomAttributeTypeName(typeName, false,
                (module, typeDefName) => (MetadataType)module.Context.GetCanonType(typeDefName));

            if (foundType == null)
                throw new CommandLineException(string.Format(SR.TypeNotFound, typeName));

            return foundType;
        }

        private MethodDesc CheckAndParseSingleMethodModeArguments(CompilerTypeSystemContext context)
        {
            string[] singleMethodGenericArgs = Get(_command.SingleMethodGenericArgs);
            string singleMethodName = Get(_command.SingleMethodName);
            string singleMethodTypeName = Get(_command.SingleMethodTypeName);
            if (singleMethodName == null && singleMethodTypeName == null && singleMethodGenericArgs.Length == 0)
                return null;

            if (singleMethodName == null || singleMethodTypeName == null)
                throw new CommandLineException(SR.TypeAndMethodNameNeeded);

            TypeDesc owningType = FindType(context, singleMethodTypeName);

            // TODO: allow specifying signature to distinguish overloads
            MethodDesc method = null;
            bool printMethodList = false;
            int curIndex = 0;
            foreach (var searchMethod in owningType.GetMethods())
            {
                if (searchMethod.Name != singleMethodName)
                    continue;

                curIndex++;
                if (Get(_command.SingleMethodIndex) != 0)
                {
                    if (curIndex == Get(_command.SingleMethodIndex))
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
                    if (searchMethod.Name != singleMethodName)
                        continue;

                    curIndex++;
                    Console.WriteLine($"{curIndex} - {searchMethod}");
                }
                throw new CommandLineException(SR.SingleMethodIndexNeeded);
            }

            if (method == null)
                throw new CommandLineException(string.Format(SR.MethodNotFoundOnType, singleMethodName, singleMethodTypeName));

            if (method.Instantiation.Length != singleMethodGenericArgs.Length)
            {
                throw new CommandLineException(
                    string.Format(SR.GenericArgCountMismatch, method.Instantiation.Length, singleMethodName, singleMethodTypeName));
            }

            if (method.HasInstantiation)
            {
                List<TypeDesc> genericArguments = new List<TypeDesc>();
                foreach (var argString in singleMethodGenericArgs)
                    genericArguments.Add(FindType(context, argString));
                method = method.MakeInstantiatedMethod(genericArguments.ToArray());
            }

            return method;
        }

        internal static string CreateReproArgumentString(MethodDesc method)
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

        private T Get<T>(Option<T> option) => _command.Result.GetValue(option);

        private static int Main(string[] args) =>
            new CommandLineBuilder(new Crossgen2RootCommand(args))
                .UseTokenReplacer(Helpers.TryReadResponseFile)
                .UseVersionOption("--version", "-v")
                .UseHelp(context => context.HelpBuilder.CustomizeLayout(Crossgen2RootCommand.GetExtendedHelp))
                .UseParseErrorReporting()
                .Build()
                .Invoke(args);
    }
}
