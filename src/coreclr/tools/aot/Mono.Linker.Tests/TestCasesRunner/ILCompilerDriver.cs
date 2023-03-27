// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ILCompiler;
using ILCompiler.Dataflow;
using ILLink.Shared.TrimAnalysis;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class ILCompilerDriver
	{
		internal const string DefaultSystemModule = "System.Private.CoreLib";

		public ILScanResults Trim (ILCompilerOptions options, ILogWriter logWriter)
		{
			ComputeDefaultOptions (out var targetOS, out var targetArchitecture);
			var targetDetails = new TargetDetails (targetArchitecture, targetOS, TargetAbi.NativeAot);
			CompilerTypeSystemContext typeSystemContext =
				new CompilerTypeSystemContext (targetDetails, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All, genericCycleCutoffPoint: -1);

			typeSystemContext.InputFilePaths = options.InputFilePaths;
			typeSystemContext.ReferenceFilePaths = options.ReferenceFilePaths;
			typeSystemContext.SetSystemModule (typeSystemContext.GetModuleForSimpleName (DefaultSystemModule));

			List<EcmaModule> inputModules = new List<EcmaModule> ();
			foreach (var inputFile in typeSystemContext.InputFilePaths) {
				EcmaModule module = typeSystemContext.GetModuleFromPath (inputFile.Value);
				inputModules.Add (module);
			}

			foreach (var trimAssembly in options.TrimAssemblies) {
				EcmaModule module = typeSystemContext.GetModuleFromPath (trimAssembly);
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

			ILProvider ilProvider = new NativeAotILProvider ();

			Logger logger = new Logger (
				logWriter,
				ilProvider,
				isVerbose: true,
				suppressedWarnings: Enumerable.Empty<int> (),
				options.SingleWarn,
				singleWarnEnabledModules: Enumerable.Empty<string> (),
				singleWarnDisabledModules: Enumerable.Empty<string> (),
				suppressedCategories: Enumerable.Empty<string> ());

			foreach (var descriptor in options.Descriptors) {
				if (!File.Exists (descriptor))
					throw new FileNotFoundException ($"'{descriptor}' doesn't exist");
				compilationRoots.Add (new ILCompiler.DependencyAnalysis.TrimmingDescriptorNode (descriptor));
			}
			
			ilProvider = new FeatureSwitchManager (ilProvider, logger, options.FeatureSwitches);

			CompilerGeneratedState compilerGeneratedState = new CompilerGeneratedState (ilProvider, logger);

			UsageBasedMetadataManager metadataManager = new UsageBasedMetadataManager (
				compilationGroup,
				typeSystemContext,
				new NoMetadataBlockingPolicy (),
				new ManifestResourceBlockingPolicy (logger, options.FeatureSwitches),
				logFile: null,
				new NoStackTraceEmissionPolicy (),
				new NoDynamicInvokeThunkGenerationPolicy (),
				new FlowAnnotations (logger, ilProvider, compilerGeneratedState),
				UsageBasedMetadataGenerationOptions.ReflectionILScanning,
				options: default,
				logger,
				Array.Empty<KeyValuePair<string, bool>> (),
				Array.Empty<string> (),
				options.AdditionalRootAssemblies.ToArray (),
				options.TrimAssemblies.ToArray ());

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
