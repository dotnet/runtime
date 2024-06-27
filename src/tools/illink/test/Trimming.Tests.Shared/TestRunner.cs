// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public partial class TestRunner
	{
		private readonly ObjectFactory _factory;

		public TestRunner (ObjectFactory factory)
		{
			_factory = factory;
		}

		public virtual TrimmedTestCaseResult? Run (TestCase testCase)
		{
			try {
				using (var fullTestCaseAssemblyDefinition = AssemblyDefinition.ReadAssembly (testCase.OriginalTestCaseAssemblyPath.ToString ())) {
					var compilationMetadataProvider = _factory.CreateCompilationMetadataProvider (testCase, fullTestCaseAssemblyDefinition);

					if (compilationMetadataProvider.IsIgnored (out string? ignoreReason))
						IgnoreTest (ignoreReason);

					var sandbox = Sandbox (testCase, compilationMetadataProvider);
					var compilationResult = Compile (sandbox, compilationMetadataProvider);
					using (var expectationsAssemblyDefinition = AssemblyDefinition.ReadAssembly (compilationResult.ExpectationsAssemblyPath.ToString ())) {
						var metadataProvider = _factory.CreateMetadataProvider (testCase, expectationsAssemblyDefinition);

						sandbox.PopulateFromExpectations (metadataProvider);

						PrepForLink (sandbox, compilationResult);
						return Link (testCase, sandbox, compilationResult, metadataProvider);
					}
				}
			} catch (IgnoreTestException) {
				return null;
			}
		}

		partial void IgnoreTest (string reason);

		public virtual TrimmedTestCaseResult Relink (TrimmedTestCaseResult result)
		{
			PrepForLink (result.Sandbox, result.CompilationResult);
			return Link (result.TestCase, result.Sandbox, result.CompilationResult, result.MetadataProvider);
		}

		private TestCaseSandbox Sandbox (TestCase testCase, TestCaseCompilationMetadataProvider metadataProvider)
		{
			var sandbox = _factory.CreateSandbox (testCase);
			sandbox.Populate (metadataProvider);
			return sandbox;
		}

		private ManagedCompilationResult Compile (TestCaseSandbox sandbox, TestCaseCompilationMetadataProvider metadataProvider)
		{
			var inputCompiler = _factory.CreateCompiler (sandbox, metadataProvider);
			var expectationsCompiler = _factory.CreateCompiler (sandbox, metadataProvider);
			var sourceFiles = sandbox.SourceFiles.Select (s => s.ToString ()).ToArray ();

			var assemblyName = metadataProvider.GetAssemblyName ();

			var commonReferences = metadataProvider.GetCommonReferencedAssemblies (sandbox.InputDirectory).ToArray ();
			var mainAssemblyReferences = metadataProvider.GetReferencedAssemblies (sandbox.InputDirectory).ToArray ();
			var resources = sandbox.ResourceFiles.ToArray ();
			var additionalArguments = metadataProvider.GetSetupCompilerArguments ().ToArray ();

			var expectationsCommonReferences = metadataProvider.GetCommonReferencedAssemblies (sandbox.ExpectationsDirectory).ToArray ();
			var expectationsMainAssemblyReferences = metadataProvider.GetReferencedAssemblies (sandbox.ExpectationsDirectory).ToArray ();

			var additionalDefines = GetAdditionalDefines ();
			var inputTask = Task.Run (() => inputCompiler.CompileTestIn (
				sandbox.InputDirectory,
				assemblyName,
				sourceFiles,
				commonReferences,
				mainAssemblyReferences,
				additionalDefines?.ToArray (),
				resources,
				additionalArguments));

			var expectationsDefines = new string[] { "INCLUDE_EXPECTATIONS" };
			if (additionalDefines != null)
				expectationsDefines = expectationsDefines.Concat (additionalDefines).ToArray ();

			var expectationsTask = Task.Run (() => expectationsCompiler.CompileTestIn (
				sandbox.ExpectationsDirectory,
				assemblyName,
				sourceFiles,
				expectationsCommonReferences,
				expectationsMainAssemblyReferences,
				expectationsDefines,
				resources,
				additionalArguments));

			NPath? inputAssemblyPath = null;
			NPath? expectationsAssemblyPath = null;
			try {
				inputAssemblyPath = GetResultOfTaskThatMakesAssertions (inputTask);
				expectationsAssemblyPath = GetResultOfTaskThatMakesAssertions (expectationsTask);
			} catch (Exception) {
				// If completing the input assembly task threw, we need to wait for the expectations task to complete before continuing
				// otherwise we could set the next test up for a race condition with the expectations compilation over access to the sandbox directory
				if (inputAssemblyPath == null && expectationsAssemblyPath == null) {
					try {
						expectationsTask.Wait ();
					} catch (Exception) {
						// Don't care, we want to throw the first exception
					}
				}

				throw;
			}

			return new ManagedCompilationResult (inputAssemblyPath, expectationsAssemblyPath);
		}

		private partial IEnumerable<string>? GetAdditionalDefines();

		protected virtual void PrepForLink (TestCaseSandbox sandbox, ManagedCompilationResult compilationResult)
		{
		}

		private TrimmedTestCaseResult Link (TestCase testCase, TestCaseSandbox sandbox, ManagedCompilationResult compilationResult, TestCaseMetadataProvider metadataProvider)
		{
			var trimmer = _factory.CreateTrimmer ();
			var trimmingCustomizations = CustomizeTrimming (trimmer, metadataProvider);

			var builder = _factory.CreateTrimmingArgumentBuilder (metadataProvider);

			AddTrimmingOptions (sandbox, compilationResult, builder, metadataProvider);

			var logger = new TrimmingTestLogger ();
			var trimmingResults = trimmer.Trim (builder.Build (), trimmingCustomizations, logger);

			return new TrimmedTestCaseResult (
				testCase,
				compilationResult.InputAssemblyPath,
				sandbox.OutputDirectory.Combine (compilationResult.InputAssemblyPath.FileName),
				compilationResult.ExpectationsAssemblyPath,
				sandbox,
				metadataProvider,
				compilationResult,
				logger,
				trimmingCustomizations,
				trimmingResults);
		}

		protected virtual void AddTrimmingOptions (TestCaseSandbox sandbox, ManagedCompilationResult compilationResult, TrimmingArgumentBuilder builder, TestCaseMetadataProvider metadataProvider)
		{
			var caseDefinedOptions = metadataProvider.GetLinkerOptions (sandbox.InputDirectory);

			AddOutputDirectory (sandbox, compilationResult, builder);

			foreach (var rspFile in sandbox.ResponseFiles)
				builder.AddResponseFile (rspFile);

			foreach (var inputReference in sandbox.InputDirectory.Files ()) {
				var ext = inputReference.ExtensionWithDot;
				if (ext == ".dll" || ext == ".exe")
					AddInputReference (inputReference, builder);
			}
			var coreAction = caseDefinedOptions.TrimMode ?? "skip";
			foreach (var extraReference in metadataProvider.GetExtraLinkerReferences ()) {
				builder.AddReference (extraReference);
				builder.AddAssemblyAction (coreAction, extraReference.FileNameWithoutExtension);
			}

			builder.ProcessOptions (caseDefinedOptions);

			AddDumpDependenciesOptions (caseDefinedOptions, compilationResult, builder, metadataProvider);

			builder.ProcessTestInputAssembly (compilationResult.InputAssemblyPath);
		}

		protected partial TrimmingCustomizations? CustomizeTrimming (TrimmingDriver linker, TestCaseMetadataProvider metadataProvider);

		protected partial void AddDumpDependenciesOptions (TestCaseLinkerOptions caseDefinedOptions, ManagedCompilationResult compilationResult, TrimmingArgumentBuilder builder, TestCaseMetadataProvider metadataProvider);

		static partial void AddOutputDirectory (TestCaseSandbox sandbox, ManagedCompilationResult compilationResult, TrimmingArgumentBuilder builder);

		static partial void AddInputReference (NPath inputReference, TrimmingArgumentBuilder builder);
	}
}
