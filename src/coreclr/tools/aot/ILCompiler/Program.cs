// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;
using InstructionSet = Internal.JitInterface.InstructionSet;

namespace ILCompiler
{
    internal class Program
    {
        private readonly ILCompilerRootCommand _command;

        public Program(ILCompilerRootCommand command)
        {
            _command = command;

            if (command.Result.GetValueForOption(command.WaitForDebugger))
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

            List<MethodDesc> initializerList = new List<MethodDesc>(libraryInitializers.LibraryInitializerMethods);

            // If there are any AppContext switches the user wishes to enable, generate code that sets them.
            string[] appContextSwitches = Get(_command.AppContextSwitches);
            if (appContextSwitches.Length > 0)
            {
                MethodDesc appContextInitMethod = new Internal.IL.Stubs.StartupCode.AppContextInitializerMethod(
                    context.GeneratedAssembly.GetGlobalModuleType(), appContextSwitches);
                initializerList.Add(appContextInitMethod);
            }

            return initializerList;
        }

        public int Run()
        {
            string outputFilePath = Get(_command.OutputFilePath);
            if (outputFilePath == null)
                throw new CommandLineException("Output filename must be specified (/out <file>)");

            TargetArchitecture targetArchitecture = Get(_command.TargetArchitecture);
            InstructionSetSupportBuilder instructionSetSupportBuilder = new InstructionSetSupportBuilder(targetArchitecture);
            TargetOS targetOS = Get(_command.TargetOS);

            // The runtime expects certain baselines that the codegen can assume as well.
            if ((targetArchitecture == TargetArchitecture.X86) || (targetArchitecture == TargetArchitecture.X64))
            {
                instructionSetSupportBuilder.AddSupportedInstructionSet("sse2"); // Lower baselines included by implication
            }
            else if (targetArchitecture == TargetArchitecture.ARM64)
            {
                if (targetOS == TargetOS.OSX)
                {
                    // For osx-arm64 we know that apple-m1 is a baseline
                    instructionSetSupportBuilder.AddSupportedInstructionSet("apple-m1");
                }
                else
                {
                    instructionSetSupportBuilder.AddSupportedInstructionSet("neon"); // Lower baselines included by implication
                }
            }

            string instructionSetArg = Get(_command.InstructionSet);
            if (instructionSetArg != null)
            {
                var instructionSetParams = new List<string>();

                // Normalize instruction set format to include implied +.
                string[] instructionSetParamsInput = instructionSetArg.Split(',');
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

            InstructionSetSupportBuilder optimisticInstructionSetSupportBuilder = new InstructionSetSupportBuilder(targetArchitecture);

            // Optimistically assume some instruction sets are present.
            if (targetArchitecture == TargetArchitecture.X86 || targetArchitecture == TargetArchitecture.X64)
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
            else if (targetArchitecture == TargetArchitecture.ARM64)
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
                                                                    InstructionSetSupportBuilder.GetNonSpecifiableInstructionSetsForArch(targetArchitecture),
                                                                    targetArchitecture);

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
                new CompilerTypeSystemContext(targetDetails, genericsMode, supportsReflection ? DelegateFeature.All : 0, Get(_command.MaxGenericCycle));

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
            foreach (var inputFile in _command.Result.GetValueForArgument(_command.InputFilePaths))
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
            else if (!String.IsNullOrEmpty(guard))
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

                string[] runtimeOptions = Get(_command.RuntimeOptions);
                if (entrypointModule != null)
                {
                    compilationRoots.Add(new MainMethodRootProvider(entrypointModule, CreateInitializerList(typeSystemContext)));
                    compilationRoots.Add(new RuntimeConfigurationRootProvider(runtimeOptions));
                    compilationRoots.Add(new ExpectedIsaFeaturesRootProvider(instructionSetSupport));
                }

                bool nativeLib = Get(_command.NativeLib);
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
                    if (entrypointModule == null && !nativeLib)
                        throw new Exception("No entrypoint module");

                    if (!systemModuleIsInputModule)
                        compilationRoots.Add(new ExportedMethodsRootProvider((EcmaModule)typeSystemContext.SystemModule));
                    compilationGroup = new SingleFileCompilationModuleGroup();
                }

                if (nativeLib)
                {
                    // Set owning module of generated native library startup method to compiler generated module,
                    // to ensure the startup method is included in the object file during multimodule mode build
                    compilationRoots.Add(new NativeLibraryInitializerRootProvider(typeSystemContext.GeneratedAssembly, CreateInitializerList(typeSystemContext)));
                    compilationRoots.Add(new RuntimeConfigurationRootProvider(runtimeOptions));
                    compilationRoots.Add(new ExpectedIsaFeaturesRootProvider(instructionSetSupport));
                }

                foreach (var rdXmlFilePath in Get(_command.RdXmlFilePaths))
                {
                    compilationRoots.Add(new RdXmlRootProvider(typeSystemContext, rdXmlFilePath));
                }
            }

            // Root whatever assemblies were specified on the command line
            string[] rootedAssemblies = Get(_command.RootedAssemblies);
            foreach (var rootedAssembly in rootedAssemblies)
            {
                // For compatibility with IL Linker, the parameter could be a file name or an assembly name.
                // This is the logic IL Linker uses to decide how to interpret the string. Really.
                EcmaModule module = File.Exists(rootedAssembly)
                    ? typeSystemContext.GetModuleFromPath(rootedAssembly)
                    : typeSystemContext.GetModuleForSimpleName(rootedAssembly);

                // We only root the module type. The rest will fall out because we treat rootedAssemblies
                // same as conditionally rooted ones and here we're fulfilling the condition ("something is used").
                compilationRoots.Add(
                    new GenericRootProvider<ModuleDesc>(module,
                    (ModuleDesc module, IRootingServiceProvider rooter) => rooter.AddCompilationRoot(module.GetGlobalModuleType(), "Command line root")));
            }

            //
            // Compile
            //

            CompilationBuilder builder = new RyuJitCompilationBuilder(typeSystemContext, compilationGroup);

            string compilationUnitPrefix = multiFile ? Path.GetFileNameWithoutExtension(outputFilePath) : "";
            builder.UseCompilationUnitPrefix(compilationUnitPrefix);

            string[] mibcFilePaths = Get(_command.MibcFilePaths);
            if (mibcFilePaths.Length > 0)
                ((RyuJitCompilationBuilder)builder).UseProfileData(mibcFilePaths);

            string jitPath = Get(_command.JitPath);
            if (!String.IsNullOrEmpty(jitPath))
                ((RyuJitCompilationBuilder)builder).UseJitPath(jitPath);

            PInvokeILEmitterConfiguration pinvokePolicy = new ConfigurablePInvokePolicy(typeSystemContext.Target,
                Get(_command.DirectPInvokes), Get(_command.DirectPInvokeLists));

            ILProvider ilProvider = new NativeAotILProvider();

            List<KeyValuePair<string, bool>> featureSwitches = new List<KeyValuePair<string, bool>>();
            foreach (var switchPair in Get(_command.FeatureSwitches))
            {
                string[] switchAndValue = switchPair.Split('=');
                if (switchAndValue.Length != 2
                    || !bool.TryParse(switchAndValue[1], out bool switchValue))
                    throw new CommandLineException($"Unexpected feature switch pair '{switchPair}'");
                featureSwitches.Add(new KeyValuePair<string, bool>(switchAndValue[0], switchValue));
            }
            ilProvider = new FeatureSwitchManager(ilProvider, featureSwitches);

            var logger = new Logger(Console.Out, Get(_command.IsVerbose), ProcessWarningCodes(Get(_command.SuppressedWarnings)),
                Get(_command.SingleWarn), Get(_command.SingleWarnEnabledAssemblies), Get(_command.SingleWarnDisabledAssemblies));

            var stackTracePolicy = Get(_command.EmitStackTraceData) ?
                (StackTraceEmissionPolicy)new EcmaMethodStackTraceEmissionPolicy() : new NoStackTraceEmissionPolicy();

            MetadataBlockingPolicy mdBlockingPolicy;
            ManifestResourceBlockingPolicy resBlockingPolicy;
            UsageBasedMetadataGenerationOptions metadataGenerationOptions = default;
            if (supportsReflection)
            {
                mdBlockingPolicy = Get(_command.NoMetadataBlocking) ?
                    new NoMetadataBlockingPolicy() : new BlockedInternalsBlockingPolicy(typeSystemContext);

                resBlockingPolicy = new ManifestResourceBlockingPolicy(featureSwitches);

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

            var flowAnnotations = new ILLink.Shared.TrimAnalysis.FlowAnnotations(logger, ilProvider);

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
                    logger,
                    featureSwitches,
                    Get(_command.ConditionallyRootedAssemblies),
                    rootedAssemblies,
                    Get(_command.TrimmedAssemblies));

            InteropStateManager interopStateManager = new InteropStateManager(typeSystemContext.GeneratedAssembly);
            InteropStubManager interopStubManager = new UsageBasedInteropStubManager(interopStateManager, pinvokePolicy, logger);

            // Unless explicitly opted in at the command line, we enable scanner for retail builds by default.
            // We also don't do this for multifile because scanner doesn't simulate inlining (this would be
            // fixable by using a CompilationGroup for the scanner that has a bigger worldview, but
            // let's cross that bridge when we get there).
            bool useScanner = Get(_command.UseScanner) ||
                (_command.OptimizationMode != OptimizationMode.None && !multiFile);

            useScanner &= !Get(_command.NoScanner);

            // Enable static data preinitialization in optimized builds.
            bool preinitStatics = Get(_command.PreinitStatics) ||
                (_command.OptimizationMode != OptimizationMode.None && !multiFile);
            preinitStatics &= !Get(_command.NoPreinitStatics);

            var preinitManager = new PreinitializationManager(typeSystemContext, compilationGroup, ilProvider, preinitStatics);
            builder
                .UseILProvider(ilProvider)
                .UsePreinitializationManager(preinitManager);

            ILScanResults scanResults = null;
            string scanDgmlLogFileName = Get(_command.ScanDgmlLogFileName);
            int parallelism = Get(_command.Parallelism);
            if (useScanner)
            {
                ILScannerBuilder scannerBuilder = builder.GetILScannerBuilder()
                    .UseCompilationRoots(compilationRoots)
                    .UseMetadataManager(metadataManager)
                    .UseParallelism(parallelism)
                    .UseInteropStubManager(interopStubManager)
                    .UseLogger(logger);

                if (scanDgmlLogFileName != null)
                    scannerBuilder.UseDependencyTracking(Get(_command.GenerateFullScanDgmlLog) ?
                            DependencyTrackingLevel.All : DependencyTrackingLevel.First);

                IILScanner scanner = scannerBuilder.ToILScanner();

                scanResults = scanner.Scan();

                metadataManager = ((UsageBasedMetadataManager)metadataManager).ToAnalysisBasedMetadataManager();

                interopStubManager = scanResults.GetInteropStubManager(interopStateManager, pinvokePolicy);
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
                .UseDwarf5(Get(_command.UseDwarf5));

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

            builder.UseResilience(Get(_command.Resilient));

            ICompilation compilation = builder.ToCompilation();

            string mapFileName = Get(_command.MapFileName);
            ObjectDumper dumper = mapFileName != null ? new ObjectDumper(mapFileName) : null;

            CompilationResults compilationResults = compilation.Compile(outputFilePath, dumper);
            string exportsFile = Get(_command.ExportsFile);
            if (exportsFile != null)
            {
                ExportsFileWriter defFileWriter = new ExportsFileWriter(typeSystemContext, exportsFile);
                foreach (var compilationRoot in compilationRoots)
                {
                    if (compilationRoot is ExportedMethodsRootProvider provider)
                        defFileWriter.AddExportedMethods(provider.ExportedMethods);
                }

                defFileWriter.EmitExportedMethods();
            }

            typeSystemContext.LogWarnings(logger);

            if (dgmlLogFileName != null)
                compilationResults.WriteDependencyLog(dgmlLogFileName);

            if (scanResults != null)
            {
                if (scanDgmlLogFileName != null)
                    scanResults.WriteDependencyLog(scanDgmlLogFileName);

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
                if (_command.OptimizationMode == OptimizationMode.None)
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

            if (method.HasInstantiation != (singleMethodGenericArgs != null) ||
                (method.HasInstantiation && (method.Instantiation.Length != singleMethodGenericArgs.Length)))
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
                string[] values = value.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string id in values)
                {
                    if (!id.StartsWith("IL", StringComparison.Ordinal) || !ushort.TryParse(id.Substring(2), out ushort code))
                        continue;

                    yield return code;
                }
            }
        }

        private T Get<T>(Option<T> option) => _command.Result.GetValueForOption(option);

        private static int Main(string[] args) =>
            new CommandLineBuilder(new ILCompilerRootCommand(args))
                .UseVersionOption()
                .UseHelp(context => context.HelpBuilder.CustomizeLayout(ILCompilerRootCommand.GetExtendedHelp))
                .UseParseErrorReporting()
                .Build()
                .Invoke(args);
    }
}
