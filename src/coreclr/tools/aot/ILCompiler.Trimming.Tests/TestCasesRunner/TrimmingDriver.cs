// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using ILCompiler;
using ILCompiler.Dataflow;
using ILLink.Shared.TrimAnalysis;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TrimmingDriver
	{
		internal const string DefaultSystemModule = "System.Private.CoreLib";

		public ILScanResults Trim (ILCompilerOptions options, TrimmingCustomizations? customizations, ILogWriter logWriter)
		{
			ComputeDefaultOptions (out var targetOS, out var targetArchitecture);
			var targetDetails = new TargetDetails (targetArchitecture, targetOS, TargetAbi.NativeAot);
			CompilerTypeSystemContext typeSystemContext =
				new CompilerTypeSystemContext (targetDetails, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All, genericCycleDepthCutoff: -1);

			typeSystemContext.InputFilePaths = options.InputFilePaths;
			typeSystemContext.ReferenceFilePaths = options.ReferenceFilePaths;
			typeSystemContext.SetSystemModule (typeSystemContext.GetModuleForSimpleName (DefaultSystemModule));

			List<EcmaModule> inputModules = new List<EcmaModule> ();
			foreach (var inputFile in typeSystemContext.InputFilePaths) {
				EcmaModule module = typeSystemContext.GetModuleFromPath (inputFile.Value);
				inputModules.Add (module);
			}

			foreach (var trimAssembly in options.TrimAssemblies) {
				EcmaModule module = typeSystemContext.GetModuleForSimpleName (trimAssembly);
				inputModules.Add (module);
			}

			CompilationModuleGroup compilationGroup;
			if (options.FrameworkCompilation)
				compilationGroup = new SingleFileCompilationModuleGroup ();
			else
				compilationGroup = new TestInfraMultiFileSharedCompilationModuleGroup (typeSystemContext, inputModules);

			List<ICompilationRootProvider> compilationRoots = new List<ICompilationRootProvider> ();
			EcmaModule? entrypointModule = null;
			foreach (var inputFile in typeSystemContext.InputFilePaths) {
				EcmaModule module = typeSystemContext.GetModuleFromPath (inputFile.Value);

				if (module.PEReader.PEHeaders.IsExe) {
					if (entrypointModule != null)
						throw new Exception ("Multiple EXE modules");
					entrypointModule = module;
				}

				compilationRoots.Add (new UnmanagedEntryPointsRootProvider (module));
			}

			compilationRoots.Add (new MainMethodRootProvider (entrypointModule, CreateInitializerList (typeSystemContext, options), generateLibraryAndModuleInitializers: true));

			foreach (var rootedAssembly in options.AdditionalRootAssemblies) {
				EcmaModule module = typeSystemContext.GetModuleForSimpleName (rootedAssembly);

				// We only root the module type. The rest will fall out because we treat rootedAssemblies
				// same as conditionally rooted ones and here we're fulfilling the condition ("something is used").
				compilationRoots.Add (
					new GenericRootProvider<ModuleDesc> (module,
					(ModuleDesc module, IRootingServiceProvider rooter) => rooter.AddReflectionRoot (module.GetGlobalModuleType (), "Command line root")));
			}

			ILProvider ilProvider = new NativeAotILProvider ();

			Logger logger = new Logger (
				logWriter,
				ilProvider,
				isVerbose: true,
				suppressedWarnings: Enumerable.Empty<int> (),
				options.SingleWarn,
				singleWarnEnabledModules: Enumerable.Empty<string> (),
				singleWarnDisabledModules: Enumerable.Empty<string> (),
				suppressedCategories: Enumerable.Empty<string> (),
				treatWarningsAsErrors: options.TreatWarningsAsErrors,
				warningsAsErrors: options.WarningsAsErrors);

			foreach (var descriptor in options.Descriptors) {
				if (!File.Exists (descriptor))
					throw new FileNotFoundException ($"'{descriptor}' doesn't exist");
				compilationRoots.Add (new ILCompiler.DependencyAnalysis.TrimmingDescriptorNode (descriptor));
			}

			var featureSwitches = options.FeatureSwitches;
			BodyAndFieldSubstitutions substitutions = default;
			IReadOnlyDictionary<ModuleDesc, IReadOnlySet<string>>? resourceBlocks = default;
			foreach (string substitutionFilePath in options.SubstitutionFiles)
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

			CompilerGeneratedState compilerGeneratedState = new CompilerGeneratedState (ilProvider, logger);

			UsageBasedMetadataManager metadataManager = new UsageBasedMetadataManager (
				compilationGroup,
				typeSystemContext,
				new NoMetadataBlockingPolicy (),
				new ManifestResourceBlockingPolicy (logger, options.FeatureSwitches, new Dictionary<ModuleDesc, IReadOnlySet<string>>()),
				logFile: null,
				new NoStackTraceEmissionPolicy (),
				new DefaultDynamicInvokeThunkGenerationPolicy (),
				new FlowAnnotations (logger, ilProvider, compilerGeneratedState),
				UsageBasedMetadataGenerationOptions.ReflectionILScanning,
				options: default,
				logger,
				options.FeatureSwitches,
				Array.Empty<string> (),
				options.AdditionalRootAssemblies.ToArray (),
				options.TrimAssemblies.ToArray (),
				Array.Empty<string> ());

			PInvokeILEmitterConfiguration pinvokePolicy = new ILCompilerTestPInvokePolicy ();
			InteropStateManager interopStateManager = new InteropStateManager (typeSystemContext.GeneratedAssembly);
			InteropStubManager interopStubManager = new UsageBasedInteropStubManager (interopStateManager, pinvokePolicy, logger);

			CompilationBuilder builder = new RyuJitCompilationBuilder (typeSystemContext, compilationGroup)
				.UseILProvider (ilProvider)
				.UseCompilationUnitPrefix("");

			IILScanner scanner = builder.GetILScannerBuilder ()
				.UseCompilationRoots (compilationRoots)
				.UseMetadataManager (metadataManager)
				.UseParallelism (System.Diagnostics.Debugger.IsAttached ? 1 : -1)
				.UseInteropStubManager (interopStubManager)
				.ToILScanner ();

			return scanner.Scan ();
		}

		public static void ComputeDefaultOptions (out TargetOS os, out TargetArchitecture arch)
		{
			if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
				os = TargetOS.Windows;
			else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux))
				os = TargetOS.Linux;
			else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
				os = TargetOS.OSX;
			else if (RuntimeInformation.IsOSPlatform (OSPlatform.FreeBSD))
				os = TargetOS.FreeBSD;
			else
				throw new NotImplementedException ();

			switch (RuntimeInformation.ProcessArchitecture) {
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
				throw new NotImplementedException ();
			}
		}

		private static IReadOnlyCollection<MethodDesc> CreateInitializerList (CompilerTypeSystemContext context, ILCompilerOptions options)
		{
			List<ModuleDesc> assembliesWithInitalizers = new List<ModuleDesc> ();

			// Build a list of assemblies that have an initializer that needs to run before
			// any user code runs.
			foreach (string initAssemblyName in options.InitAssemblies) {
				ModuleDesc assembly = context.ResolveAssembly (new AssemblyName (initAssemblyName), throwIfNotFound: true);
				assembliesWithInitalizers.Add (assembly);
			}

			var libraryInitializers = new LibraryInitializers (context, assembliesWithInitalizers);

			List<MethodDesc> initializerList = new List<MethodDesc> (libraryInitializers.LibraryInitializerMethods);
			return initializerList;
		}
	}
}
