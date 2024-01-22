// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0005

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysis;
using ILLink.Shared;

using Debug = System.Diagnostics.Debug;
using InstructionSet = Internal.JitInterface.InstructionSet;

namespace ILCompiler
{
    internal sealed class Program
    {
        private readonly ILCompilerRootCommand _command;
        private static readonly char[] s_separator = new char[] { ',', ';', ' ' };

        public Program(ILCompilerRootCommand command)
        {
            _command = command;

            if (Get(command.WaitForDebugger))
            {
                Console.WriteLine("Waiting for debugger to attach. Press ENTER to continue");
                Console.ReadLine();
            }
        }

        private IReadOnlyCollection<MethodDesc> CreateInitializerList(CompilerTypeSystemContext context)
        {
            List<ModuleDesc> assembliesWithInitializers = new List<ModuleDesc>();

            // Build a list of assemblies that have an initializer that needs to run before
            // any user code runs.
            foreach (string initAssemblyName in Get(_command.InitAssemblies))
            {
                ModuleDesc assembly = context.ResolveAssembly(new AssemblyName(initAssemblyName), throwIfNotFound: true);
                assembliesWithInitializers.Add(assembly);
            }

            var libraryInitializers = new LibraryInitializers(context, assembliesWithInitializers);

            return libraryInitializers.LibraryInitializerMethods;
        }

        public int Run()
        {
            string outputFilePath = Get(_command.OutputFilePath);
            if (outputFilePath == null)
                throw new CommandLineException("Output filename must be specified (/out <file>)");

            var suppressedWarningCategories = new List<string>();
            if (Get(_command.NoTrimWarn))
                suppressedWarningCategories.Add(MessageSubCategory.TrimAnalysis);
            if (Get(_command.NoAotWarn))
                suppressedWarningCategories.Add(MessageSubCategory.AotAnalysis);

            ILProvider ilProvider = new NativeAotILProvider();

            Dictionary<int, bool> warningsAsErrors = new Dictionary<int, bool>();
            foreach (int warning in ProcessWarningCodes(Get(_command.WarningsAsErrorsEnable)))
            {
                warningsAsErrors[warning] = true;
            }
            foreach (int warning in ProcessWarningCodes(Get(_command.WarningsAsErrorsDisable)))
            {
                warningsAsErrors[warning] = false;
            }
            var logger = new Logger(Console.Out, ilProvider, Get(_command.IsVerbose), ProcessWarningCodes(Get(_command.SuppressedWarnings)),
                Get(_command.SingleWarn), Get(_command.SingleWarnEnabledAssemblies), Get(_command.SingleWarnDisabledAssemblies), suppressedWarningCategories,
                Get(_command.TreatWarningsAsErrors), warningsAsErrors);

            // NativeAOT is full AOT and its pre-compiled methods can not be
            // thrown away at runtime if they mismatch in required ISAs or
            // computed layouts of structs. The worst case scenario is simply
            // that the image targets a higher machine than the user has and
            // it fails to launch. Thus we want to have usage of Vector<T>
            // directly encoded as part of the required ISAs.
            bool isVectorTOptimistic = false;

            TargetArchitecture targetArchitecture = Get(_command.TargetArchitecture);
            TargetOS targetOS = Get(_command.TargetOS);
            InstructionSetSupport instructionSetSupport = Helpers.ConfigureInstructionSetSupport(Get(_command.InstructionSet), Get(_command.MaxVectorTBitWidth), isVectorTOptimistic, targetArchitecture, targetOS,
                "Unrecognized instruction set {0}", "Unsupported combination of instruction sets: {0}/{1}", logger,
                optimizingForSize: _command.OptimizationMode == OptimizationMode.PreferSize);

            string systemModuleName = Get(_command.SystemModuleName);
            string reflectionData = Get(_command.ReflectionData);
            bool supportsReflection = reflectionData != "none" && systemModuleName == Helpers.DefaultSystemModule;

            //
            // Initialize type system context
            //

            SharedGenericsMode genericsMode = SharedGenericsMode.CanonicalReferenceTypes;

            var simdVectorLength = instructionSetSupport.GetVectorTSimdVector();
            var targetAbi = TargetAbi.NativeAot;
            var targetDetails = new TargetDetails(targetArchitecture, targetOS, targetAbi, simdVectorLength);
            CompilerTypeSystemContext typeSystemContext =
                new CompilerTypeSystemContext(targetDetails, genericsMode, supportsReflection ? DelegateFeature.All : 0,
                    genericCycleDepthCutoff: Get(_command.MaxGenericCycleDepth),
                    genericCycleBreadthCutoff: Get(_command.MaxGenericCycleBreadth));

            //
            // TODO: To support our pre-compiled test tree, allow input files that aren't managed assemblies since
            // some tests contain a mixture of both managed and native binaries.
            //
            // See: https://github.com/dotnet/corert/issues/2785
            //
            // When we undo this hack, replace the foreach with
            //  typeSystemContext.InputFilePaths = _command.Result.GetValueForArgument(inputFilePaths);
            //
            Dictionary<string, string> inputFilePaths = new Dictionary<string, string>();
            foreach (var inputFile in _command.Result.GetValue(_command.InputFilePaths))
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
            typeSystemContext.ReferenceFilePaths = Get(_command.ReferenceFiles);
            if (!typeSystemContext.InputFilePaths.ContainsKey(systemModuleName)
                && !typeSystemContext.ReferenceFilePaths.ContainsKey(systemModuleName))
                throw new CommandLineException($"System module {systemModuleName} does not exists. Make sure that you specify --systemmodule");

            typeSystemContext.SetSystemModule(typeSystemContext.GetModuleForSimpleName(systemModuleName));

            if (typeSystemContext.InputFilePaths.Count == 0)
                throw new CommandLineException("No input files specified");

            ilProvider = new HardwareIntrinsicILProvider(
                instructionSetSupport,
                new ExternSymbolMappedField(typeSystemContext.GetWellKnownType(WellKnownType.Int32), "g_cpuFeatures"),
                ilProvider);

            SecurityMitigationOptions securityMitigationOptions = 0;
            string guard = Get(_command.Guard);
            if (StringComparer.OrdinalIgnoreCase.Equals(guard, "cf"))
            {
                if (targetOS != TargetOS.Windows)
                {
                    throw new CommandLineException($"Control flow guard only available on Windows");
                }

                securityMitigationOptions = SecurityMitigationOptions.ControlFlowGuardAnnotations;
            }
            else if (!string.IsNullOrEmpty(guard))
            {
                throw new CommandLineException($"Unrecognized mitigation option '{guard}'");
            }

            //
            // Initialize compilation group and compilation roots
            //

            // Single method mode?
            MethodDesc singleMethod = CheckAndParseSingleMethodModeArguments(typeSystemContext);

            CompilationModuleGroup compilationGroup;
            List<ICompilationRootProvider> compilationRoots = new List<ICompilationRootProvider>();
            bool multiFile = Get(_command.MultiFile);
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
                foreach (var inputFile in typeSystemContext.InputFilePaths)
                {
                    EcmaModule module = typeSystemContext.GetModuleFromPath(inputFile.Value);

                    if (module.PEReader.PEHeaders.IsExe)
                    {
                        if (entrypointModule != null)
                            throw new Exception("Multiple EXE modules");
                        entrypointModule = module;
                    }

                    compilationRoots.Add(new UnmanagedEntryPointsRootProvider(module));
                }

                bool nativeLib = Get(_command.NativeLib);
                bool SplitExeInitialization = Get(_command.SplitExeInitialization);
                if (multiFile)
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
                    if (entrypointModule == null && (!nativeLib || SplitExeInitialization))
                        throw new Exception("No entrypoint module");

                    compilationGroup = new SingleFileCompilationModuleGroup();
                }

                const string settingsBlobName = "g_compilerEmbeddedSettingsBlob";
                const string knobsBlobName = "g_compilerEmbeddedKnobsBlob";
                string[] runtimeOptions = Get(_command.RuntimeOptions);
                string[] runtimeKnobs = Get(_command.RuntimeKnobs);
                if (nativeLib)
                {
                    // Set owning module of generated native library startup method to compiler generated module,
                    // to ensure the startup method is included in the object file during multimodule mode build
                    compilationRoots.Add(new NativeLibraryInitializerRootProvider(typeSystemContext.GeneratedAssembly, CreateInitializerList(typeSystemContext)));
                    compilationRoots.Add(new RuntimeConfigurationRootProvider(settingsBlobName, runtimeOptions));
                    compilationRoots.Add(new RuntimeConfigurationRootProvider(knobsBlobName, runtimeKnobs));
                    compilationRoots.Add(new ExpectedIsaFeaturesRootProvider(instructionSetSupport));
                    if (SplitExeInitialization)
                    {
                        compilationRoots.Add(new MainMethodRootProvider(entrypointModule, CreateInitializerList(typeSystemContext), generateLibraryAndModuleInitializers: false));
                    }
                }
                else if (entrypointModule != null)
                {
                    compilationRoots.Add(new MainMethodRootProvider(entrypointModule, CreateInitializerList(typeSystemContext), generateLibraryAndModuleInitializers: !SplitExeInitialization));
                    compilationRoots.Add(new RuntimeConfigurationRootProvider(settingsBlobName, runtimeOptions));
                    compilationRoots.Add(new RuntimeConfigurationRootProvider(knobsBlobName, runtimeKnobs));
                    compilationRoots.Add(new ExpectedIsaFeaturesRootProvider(instructionSetSupport));
                    if (SplitExeInitialization)
                    {
                        compilationRoots.Add(new NativeLibraryInitializerRootProvider(typeSystemContext.GeneratedAssembly, CreateInitializerList(typeSystemContext)));
                    }
                }

                string win32resourcesModule = Get(_command.Win32ResourceModuleName);
                if (typeSystemContext.Target.IsWindows && !string.IsNullOrEmpty(win32resourcesModule))
                {
                    EcmaModule module = typeSystemContext.GetModuleForSimpleName(win32resourcesModule);
                    compilationRoots.Add(new Win32ResourcesRootProvider(module));
                }

                foreach (var unmanagedEntryPointsAssembly in Get(_command.UnmanagedEntryPointsAssemblies))
                {
                    if (typeSystemContext.InputFilePaths.ContainsKey(unmanagedEntryPointsAssembly))
                    {
                        // Skip adding UnmanagedEntryPointsRootProvider for modules that have been already registered as an input module
                        continue;
                    }
                    EcmaModule module = typeSystemContext.GetModuleForSimpleName(unmanagedEntryPointsAssembly);
                    compilationRoots.Add(new UnmanagedEntryPointsRootProvider(module));
                }

                foreach (var rdXmlFilePath in Get(_command.RdXmlFilePaths))
                {
                    compilationRoots.Add(new RdXmlRootProvider(typeSystemContext, rdXmlFilePath));
                }

                foreach (var linkTrimFilePath in Get(_command.LinkTrimFilePaths))
                {
                    if (!File.Exists(linkTrimFilePath))
                        throw new CommandLineException($"'{linkTrimFilePath}' doesn't exist");
                    compilationRoots.Add(new ILCompiler.DependencyAnalysis.TrimmingDescriptorNode(linkTrimFilePath));
                }
            }

            // Root whatever assemblies were specified on the command line
            string[] rootedAssemblies = Get(_command.RootedAssemblies);
            foreach (var rootedAssembly in rootedAssemblies)
            {
                EcmaModule module = typeSystemContext.GetModuleForSimpleName(rootedAssembly);

                // We only root the module type. The rest will fall out because we treat rootedAssemblies
                // same as conditionally rooted ones and here we're fulfilling the condition ("something is used").
                compilationRoots.Add(
                    new GenericRootProvider<ModuleDesc>(module,
                    (ModuleDesc module, IRootingServiceProvider rooter) => rooter.AddReflectionRoot(module.GetGlobalModuleType(), "Command line root")));
            }

            // Unless explicitly opted in at the command line, we enable scanner for retail builds by default.
            // We also don't do this for multifile because scanner doesn't simulate inlining (this would be
            // fixable by using a CompilationGroup for the scanner that has a bigger worldview, but
            // let's cross that bridge when we get there).
            bool useScanner = Get(_command.UseScanner) ||
                (_command.OptimizationMode != OptimizationMode.None && !multiFile);

            useScanner &= !Get(_command.NoScanner);

            bool resilient = Get(_command.Resilient);
            if (resilient && useScanner)
            {
                // If we're in resilient mode (invalid IL doesn't crash the compiler) and using scanner,
                // assume invalid code is present. Scanner may not detect all invalid code that RyuJIT detect.
                // If they disagree, we won't know how the vtable of InvalidProgramException should look like
                // and that would be a compiler crash.
                MethodDesc throwInvalidProgramMethod = typeSystemContext.GetHelperEntryPoint("ThrowHelpers", "ThrowInvalidProgramException");
                compilationRoots.Add(
                    new GenericRootProvider<MethodDesc>(throwInvalidProgramMethod,
                    (MethodDesc method, IRootingServiceProvider rooter) => rooter.AddCompilationRoot(method, "Invalid IL insurance")));
            }

            //
            // Compile
            //

            var builder = new RyuJitCompilationBuilder(typeSystemContext, compilationGroup);

            string compilationUnitPrefix = multiFile ? Path.GetFileNameWithoutExtension(outputFilePath) : "";
            builder.UseCompilationUnitPrefix(compilationUnitPrefix);

            string[] mibcFilePaths = Get(_command.MibcFilePaths);
            if (mibcFilePaths.Length > 0)
                ((RyuJitCompilationBuilder)builder).UseProfileData(mibcFilePaths);

            string jitPath = Get(_command.JitPath);
            if (!string.IsNullOrEmpty(jitPath))
                ((RyuJitCompilationBuilder)builder).UseJitPath(jitPath);

            PInvokeILEmitterConfiguration pinvokePolicy = new ConfigurablePInvokePolicy(typeSystemContext.Target,
                Get(_command.DirectPInvokes), Get(_command.DirectPInvokeLists));

            var featureSwitches = new Dictionary<string, bool>();
            foreach (var switchPair in Get(_command.FeatureSwitches))
            {
                string[] switchAndValue = switchPair.Split('=');
                if (switchAndValue.Length != 2
                    || !bool.TryParse(switchAndValue[1], out bool switchValue))
                    throw new CommandLineException($"Unexpected feature switch pair '{switchPair}'");
                featureSwitches[switchAndValue[0]] = switchValue;
            }

            BodyAndFieldSubstitutions substitutions = default;
            IReadOnlyDictionary<ModuleDesc, IReadOnlySet<string>> resourceBlocks = default;
            foreach (string substitutionFilePath in Get(_command.SubstitutionFilePaths))
            {
                using FileStream fs = File.OpenRead(substitutionFilePath);
                substitutions.AppendFrom(BodySubstitutionsParser.GetSubstitutions(
                    logger, typeSystemContext, XmlReader.Create(fs), substitutionFilePath, featureSwitches));

                fs.Seek(0, SeekOrigin.Begin);

                resourceBlocks = ManifestResourceBlockingPolicy.UnionBlockings(resourceBlocks,
                    ManifestResourceBlockingPolicy.SubstitutionsReader.GetSubstitutions(
                        logger, typeSystemContext, XmlReader.Create(fs), substitutionFilePath, featureSwitches));
            }

            ilProvider = new FeatureSwitchManager(ilProvider, logger, featureSwitches, substitutions);

            CompilerGeneratedState compilerGeneratedState = new CompilerGeneratedState(ilProvider, logger);

            var stackTracePolicy = Get(_command.EmitStackTraceData) ?
                (StackTraceEmissionPolicy)new EcmaMethodStackTraceEmissionPolicy() : new NoStackTraceEmissionPolicy();

            MetadataBlockingPolicy mdBlockingPolicy;
            ManifestResourceBlockingPolicy resBlockingPolicy;
            UsageBasedMetadataGenerationOptions metadataGenerationOptions = default;
            if (supportsReflection)
            {
                mdBlockingPolicy = new NoMetadataBlockingPolicy();

                resBlockingPolicy = new ManifestResourceBlockingPolicy(logger, featureSwitches, resourceBlocks);

                metadataGenerationOptions |= UsageBasedMetadataGenerationOptions.AnonymousTypeHeuristic;
                if (Get(_command.CompleteTypesMetadata))
                    metadataGenerationOptions |= UsageBasedMetadataGenerationOptions.CompleteTypesOnly;
                if (Get(_command.ScanReflection))
                    metadataGenerationOptions |= UsageBasedMetadataGenerationOptions.ReflectionILScanning;
                if (reflectionData == "all")
                    metadataGenerationOptions |= UsageBasedMetadataGenerationOptions.CreateReflectableArtifacts;
                if (Get(_command.RootDefaultAssemblies))
                    metadataGenerationOptions |= UsageBasedMetadataGenerationOptions.RootDefaultAssemblies;
            }
            else
            {
                mdBlockingPolicy = new FullyBlockedMetadataBlockingPolicy();
                resBlockingPolicy = new FullyBlockedManifestResourceBlockingPolicy();
            }

            DynamicInvokeThunkGenerationPolicy invokeThunkGenerationPolicy = new DefaultDynamicInvokeThunkGenerationPolicy();

            var flowAnnotations = new ILLink.Shared.TrimAnalysis.FlowAnnotations(logger, ilProvider, compilerGeneratedState);

            MetadataManagerOptions metadataOptions = default;
            if (Get(_command.Dehydrate))
                metadataOptions |= MetadataManagerOptions.DehydrateData;

            MetadataManager metadataManager = new UsageBasedMetadataManager(
                    compilationGroup,
                    typeSystemContext,
                    mdBlockingPolicy,
                    resBlockingPolicy,
                    Get(_command.MetadataLogFileName),
                    stackTracePolicy,
                    invokeThunkGenerationPolicy,
                    flowAnnotations,
                    metadataGenerationOptions,
                    metadataOptions,
                    logger,
                    featureSwitches,
                    Get(_command.ConditionallyRootedAssemblies),
                    rootedAssemblies,
                    Get(_command.TrimmedAssemblies),
                    Get(_command.SatelliteFilePaths));

            InteropStateManager interopStateManager = new InteropStateManager(typeSystemContext.GeneratedAssembly);
            InteropStubManager interopStubManager = new UsageBasedInteropStubManager(interopStateManager, pinvokePolicy, logger);

            // Enable static data preinitialization in optimized builds.
            bool preinitStatics = Get(_command.PreinitStatics) ||
                (_command.OptimizationMode != OptimizationMode.None && !multiFile);
            preinitStatics &= !Get(_command.NoPreinitStatics);

            TypePreinit.TypePreinitializationPolicy preinitPolicy = preinitStatics ?
                new TypePreinit.TypeLoaderAwarePreinitializationPolicy() : new TypePreinit.DisabledPreinitializationPolicy();

            var preinitManager = new PreinitializationManager(typeSystemContext, compilationGroup, ilProvider, preinitPolicy, new StaticReadOnlyFieldPolicy());
            builder
                .UseILProvider(ilProvider)
                .UsePreinitializationManager(preinitManager);

#if DEBUG
            List<TypeDesc> scannerConstructedTypes = null;
            List<MethodDesc> scannerCompiledMethods = null;
#endif

            int parallelism = Get(_command.Parallelism);
            if (useScanner)
            {
                // Run the scanner in a separate stack frame so that there's no dangling references to
                // it once we're done with it and it can be garbage collected.
                RunScanner();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            void RunScanner()
            {
                ILScannerBuilder scannerBuilder = builder.GetILScannerBuilder()
                    .UseCompilationRoots(compilationRoots)
                    .UseMetadataManager(metadataManager)
                    .UseParallelism(parallelism)
                    .UseInteropStubManager(interopStubManager)
                    .UseLogger(logger);

                string scanDgmlLogFileName = Get(_command.ScanDgmlLogFileName);
                if (scanDgmlLogFileName != null)
                    scannerBuilder.UseDependencyTracking(Get(_command.GenerateFullScanDgmlLog) ?
                            DependencyTrackingLevel.All : DependencyTrackingLevel.First);

                IILScanner scanner = scannerBuilder.ToILScanner();

                ILScanResults scanResults = scanner.Scan();

#if DEBUG
                scannerCompiledMethods = new List<MethodDesc>(scanResults.CompiledMethodBodies);
                scannerConstructedTypes = new List<TypeDesc>(scanResults.ConstructedEETypes);
#endif

                if (scanDgmlLogFileName != null)
                    scanResults.WriteDependencyLog(scanDgmlLogFileName);

                metadataManager = ((UsageBasedMetadataManager)metadataManager).ToAnalysisBasedMetadataManager();

                interopStubManager = scanResults.GetInteropStubManager(interopStateManager, pinvokePolicy);

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

                // If we're doing preinitialization, use a new preinitialization manager that
                // has the whole program view.
                if (preinitStatics)
                {
                    var readOnlyFieldPolicy = scanResults.GetReadOnlyFieldPolicy();
                    preinitManager = new PreinitializationManager(typeSystemContext, compilationGroup, ilProvider, scanResults.GetPreinitializationPolicy(),
                        readOnlyFieldPolicy);
                    builder.UsePreinitializationManager(preinitManager)
                        .UseReadOnlyFieldPolicy(readOnlyFieldPolicy);
                }

                // If we have a scanner, we can inline threadstatics storage using the information we collected at scanning time.
                if (!Get(_command.NoInlineTls) &&
                    (targetOS == TargetOS.Linux || (targetArchitecture == TargetArchitecture.X64 && targetOS == TargetOS.Windows)))
                {
                    builder.UseInlinedThreadStatics(scanResults.GetInlinedThreadStatics());
                }
            }

            string ilDump = Get(_command.IlDump);
            DebugInformationProvider debugInfoProvider = Get(_command.EnableDebugInfo) ?
                (ilDump == null ? new DebugInformationProvider() : new ILAssemblyGeneratingMethodDebugInfoProvider(ilDump, new EcmaOnlyDebugInformationProvider())) :
                new NullDebugInformationProvider();

            string dgmlLogFileName = Get(_command.DgmlLogFileName);
            DependencyTrackingLevel trackingLevel = dgmlLogFileName == null ?
                DependencyTrackingLevel.None : (Get(_command.GenerateFullDgmlLog) ?
                    DependencyTrackingLevel.All : DependencyTrackingLevel.First);

            compilationRoots.Add(metadataManager);
            compilationRoots.Add(interopStubManager);

            builder
                .UseInstructionSetSupport(instructionSetSupport)
                .UseBackendOptions(Get(_command.CodegenOptions))
                .UseMethodBodyFolding(enable: Get(_command.MethodBodyFolding))
                .UseParallelism(parallelism)
                .UseMetadataManager(metadataManager)
                .UseInteropStubManager(interopStubManager)
                .UseLogger(logger)
                .UseDependencyTracking(trackingLevel)
                .UseCompilationRoots(compilationRoots)
                .UseOptimizationMode(_command.OptimizationMode)
                .UseSecurityMitigationOptions(securityMitigationOptions)
                .UseDebugInfoProvider(debugInfoProvider)
                .UseDwarf5(Get(_command.UseDwarf5))
                .UseResilience(resilient);

            ICompilation compilation = builder.ToCompilation();

            string mapFileName = Get(_command.MapFileName);
            string mstatFileName = Get(_command.MstatFileName);

            List<ObjectDumper> dumpers = new List<ObjectDumper>();

            if (mapFileName != null)
                dumpers.Add(new XmlObjectDumper(mapFileName));

            if (mstatFileName != null)
                dumpers.Add(new MstatObjectDumper(mstatFileName, typeSystemContext));

            CompilationResults compilationResults = compilation.Compile(outputFilePath, ObjectDumper.Compose(dumpers));
            string exportsFile = Get(_command.ExportsFile);
            if (exportsFile != null)
            {
                ExportsFileWriter defFileWriter = new ExportsFileWriter(typeSystemContext, exportsFile, Get(_command.ExportDynamicSymbols));

                if (Get(_command.ExportUnmanagedEntryPoints))
                {
                    foreach (var compilationRoot in compilationRoots)
                    {
                        if (compilationRoot is UnmanagedEntryPointsRootProvider provider)
                            defFileWriter.AddExportedMethods(provider.ExportedMethods);
                    }
                }

                defFileWriter.EmitExportedMethods();
            }

            typeSystemContext.LogWarnings(logger);

            if (dgmlLogFileName != null)
                compilationResults.WriteDependencyLog(dgmlLogFileName);

#if DEBUG
            if (scannerConstructedTypes != null)
            {
                // If the scanner and compiler don't agree on what to compile, the outputs of the scanner might not actually be usable.
                // We are going to check this two ways:
                // 1. The methods and types generated during compilation are a subset of method and types scanned
                // 2. The methods and types scanned are a subset of methods and types compiled (this has a chance to hold for unoptimized builds only).

                // Check that methods and types generated during compilation are a subset of method and types scanned
                bool scanningFail = false;
                DiffCompilationResults(ref scanningFail, compilationResults.CompiledMethodBodies, scannerCompiledMethods,
                    "Methods", "compiled", "scanned", method => !(method.GetTypicalMethodDefinition() is EcmaMethod) || IsRelatedToInvalidInput(method));
                DiffCompilationResults(ref scanningFail, compilationResults.ConstructedEETypes, scannerConstructedTypes,
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
                if (_command.OptimizationMode == OptimizationMode.None)
                {
                    // Check that methods and types scanned are a subset of methods and types compiled

                    // If we find diffs here, they're not critical, but still might be causing a Size on Disk regression.
                    bool dummy = false;

                    // We additionally skip methods in SIMD module because there's just too many intrisics to handle and IL scanner
                    // doesn't expand them. They would show up as noisy diffs.
                    DiffCompilationResults(ref dummy, scannerCompiledMethods, compilationResults.CompiledMethodBodies,
                    "Methods", "scanned", "compiled", method => !(method.GetTypicalMethodDefinition() is EcmaMethod) || method.OwningType.IsIntrinsic);
                    DiffCompilationResults(ref dummy, scannerConstructedTypes, compilationResults.ConstructedEETypes,
                        "EETypes", "scanned", "compiled", type => !(type.GetTypeDefinition() is EcmaType));
                }

                if (scanningFail)
                    throw new Exception("Scanning failure");
            }
#endif

            if (debugInfoProvider is IDisposable)
                ((IDisposable)debugInfoProvider).Dispose();

            preinitManager.LogStatistics(logger);

            return 0;
        }

        private static void DiffCompilationResults<T>(ref bool result, IEnumerable<T> set1, IEnumerable<T> set2, string prefix,
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

        private static TypeDesc FindType(CompilerTypeSystemContext context, string typeName)
        {
            ModuleDesc systemModule = context.SystemModule;

            TypeDesc foundType = systemModule.GetTypeByCustomAttributeTypeName(typeName, false,
                (module, typeDefName) => (MetadataType)module.Context.GetCanonType(typeDefName));

            if (foundType == null)
                throw new CommandLineException($"Type '{typeName}' not found");

            return foundType;
        }

        private MethodDesc CheckAndParseSingleMethodModeArguments(CompilerTypeSystemContext context)
        {
            string singleMethodName = Get(_command.SingleMethodName);
            string singleMethodTypeName = Get(_command.SingleMethodTypeName);
            string[] singleMethodGenericArgs = Get(_command.SingleMethodGenericArgs);

            if (singleMethodName == null && singleMethodTypeName == null && singleMethodGenericArgs.Length == 0)
                return null;

            if (singleMethodName == null || singleMethodTypeName == null)
                throw new CommandLineException("Both method name and type name are required parameters for single method mode");

            TypeDesc owningType = FindType(context, singleMethodTypeName);

            // TODO: allow specifying signature to distinguish overloads
            MethodDesc method = owningType.GetMethod(singleMethodName, null);
            if (method == null)
                throw new CommandLineException($"Method '{singleMethodName}' not found in '{singleMethodTypeName}'");

            if (method.Instantiation.Length != singleMethodGenericArgs.Length)
            {
                throw new CommandLineException(
                    $"Expected {method.Instantiation.Length} generic arguments for method '{singleMethodName}' on type '{singleMethodTypeName}'");
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

        private static IEnumerable<int> ProcessWarningCodes(IEnumerable<string> warningCodes)
        {
            foreach (string value in warningCodes)
            {
                string[] values = value.Split(s_separator, StringSplitOptions.RemoveEmptyEntries);
                foreach (string id in values)
                {
                    if (!id.StartsWith("IL", StringComparison.Ordinal) || !ushort.TryParse(id.AsSpan(2), out ushort code))
                        continue;

                    yield return code;
                }
            }
        }

        private T Get<T>(CliOption<T> option) => _command.Result.GetValue(option);

        private static int Main(string[] args) =>
            new CliConfiguration(new ILCompilerRootCommand(args)
                .UseVersion()
                .UseExtendedHelp(ILCompilerRootCommand.GetExtendedHelp))
            {
                ResponseFileTokenReplacer = Helpers.TryReadResponseFile
            }.Invoke(args);
    }
}
