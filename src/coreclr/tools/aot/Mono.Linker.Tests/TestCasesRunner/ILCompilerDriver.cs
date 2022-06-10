// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using ILCompiler;
using ILLink.Shared.TrimAnalysis;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class ILCompilerDriver
	{
		private const string DefaultSystemModule = "System.Private.CoreLib";

		public void Trim (ILCompilerOptions options, ILogWriter logWriter)
		{
			ComputeDefaultOptions (out var targetOS, out var targetArchitecture);
			var targetDetails = new TargetDetails (targetArchitecture, targetOS, TargetAbi.NativeAot);
			CompilerTypeSystemContext typeSystemContext =
				new CompilerTypeSystemContext (targetDetails, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All);

			typeSystemContext.InputFilePaths = options.InputFilePaths;
			typeSystemContext.ReferenceFilePaths = options.ReferenceFilePaths;
			typeSystemContext.SetSystemModule (typeSystemContext.GetModuleForSimpleName (DefaultSystemModule));

			List<EcmaModule> inputModules = new List<EcmaModule> ();
			foreach (var inputFile in typeSystemContext.InputFilePaths) {
				EcmaModule module = typeSystemContext.GetModuleFromPath (inputFile.Value);
				inputModules.Add (module);
			}

			CompilationModuleGroup compilationGroup = new MultiFileSharedCompilationModuleGroup (typeSystemContext, inputModules);

			List<ICompilationRootProvider> compilationRoots = new List<ICompilationRootProvider> ();
			EcmaModule? entrypointModule = null;
			foreach (var inputFile in typeSystemContext.InputFilePaths) {
				EcmaModule module = typeSystemContext.GetModuleFromPath (inputFile.Value);

				if (module.PEReader.PEHeaders.IsExe) {
					if (entrypointModule != null)
						throw new Exception ("Multiple EXE modules");
					entrypointModule = module;
				}

				compilationRoots.Add (new ExportedMethodsRootProvider (module));
			}

			compilationRoots.Add (new MainMethodRootProvider (entrypointModule, CreateInitializerList (typeSystemContext, options)));

			ILProvider ilProvider = new NativeAotILProvider ();

			ilProvider = new FeatureSwitchManager (ilProvider, options.FeatureSwitches);

			Logger logger = new Logger (logWriter, isVerbose: true);

			UsageBasedMetadataManager metadataManager = new UsageBasedMetadataManager (
				compilationGroup,
				typeSystemContext,
				new NoMetadataBlockingPolicy (),
				new ManifestResourceBlockingPolicy (options.FeatureSwitches),
				logFile: null,
				new NoStackTraceEmissionPolicy (),
				new NoDynamicInvokeThunkGenerationPolicy (),
				new FlowAnnotations (logger, ilProvider),
				UsageBasedMetadataGenerationOptions.ReflectionILScanning,
				logger,
				Array.Empty<KeyValuePair<string, bool>> (),
				Array.Empty<string> (),
				options.TrimAssemblies.ToArray ());

			CompilationBuilder builder = new RyuJitCompilationBuilder (typeSystemContext, compilationGroup)
				.UseILProvider (ilProvider)
				.UseCompilationUnitPrefix("");

			IILScanner scanner = builder.GetILScannerBuilder ()
				.UseCompilationRoots (compilationRoots)
				.UseMetadataManager (metadataManager)
				.UseParallelism (System.Diagnostics.Debugger.IsAttached ? 1 : -1)
				.ToILScanner ();

			ILScanResults results = scanner.Scan ();
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

		private IReadOnlyCollection<MethodDesc> CreateInitializerList (CompilerTypeSystemContext context, ILCompilerOptions options)
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
