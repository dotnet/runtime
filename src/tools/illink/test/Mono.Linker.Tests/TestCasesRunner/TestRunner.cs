// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestRunner
	{
		private readonly ObjectFactory _factory;

		public TestRunner (ObjectFactory factory)
		{
			_factory = factory;
		}

		public virtual LinkedTestCaseResult Run (TestCase testCase)
		{
			using (var fullTestCaseAssemblyDefinition = AssemblyDefinition.ReadAssembly (testCase.OriginalTestCaseAssemblyPath.ToString ())) {
				var compilationMetadataProvider = _factory.CreateCompilationMetadataProvider (testCase, fullTestCaseAssemblyDefinition);

				if (compilationMetadataProvider.IsIgnored (out string ignoreReason))
					Assert.Ignore (ignoreReason);

				var sandbox = Sandbox (testCase, compilationMetadataProvider);
				var compilationResult = Compile (sandbox, compilationMetadataProvider);
				using (var expectationsAssemblyDefinition = AssemblyDefinition.ReadAssembly (compilationResult.ExpectationsAssemblyPath.ToString ())) {
					var metadataProvider = _factory.CreateMetadataProvider (testCase, expectationsAssemblyDefinition);

					sandbox.PopulateFromExpectations (metadataProvider);

					PrepForLink (sandbox, compilationResult);
					return Link (testCase, sandbox, compilationResult, metadataProvider);
				}
			}
		}

		public virtual LinkedTestCaseResult Relink (LinkedTestCaseResult result)
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

			var inputTask = Task.Run (() => inputCompiler.CompileTestIn (sandbox.InputDirectory, assemblyName, sourceFiles, commonReferences, mainAssemblyReferences, null, resources, additionalArguments));
			var expectationsTask = Task.Run (() => expectationsCompiler.CompileTestIn (sandbox.ExpectationsDirectory, assemblyName, sourceFiles, expectationsCommonReferences, expectationsMainAssemblyReferences, new[] { "INCLUDE_EXPECTATIONS" }, resources, additionalArguments));

			NPath inputAssemblyPath = null;
			NPath expectationsAssemblyPath = null;
			try {
				inputAssemblyPath = GetResultOfTaskThatMakesNUnitAssertions (inputTask);
				expectationsAssemblyPath = GetResultOfTaskThatMakesNUnitAssertions (expectationsTask);
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

		protected virtual void PrepForLink (TestCaseSandbox sandbox, ManagedCompilationResult compilationResult)
		{
		}

		private LinkedTestCaseResult Link (TestCase testCase, TestCaseSandbox sandbox, ManagedCompilationResult compilationResult, TestCaseMetadataProvider metadataProvider)
		{
			var linker = _factory.CreateLinker ();
			var linkerCustomizations = CustomizeLinker (linker, metadataProvider);

			var builder = _factory.CreateLinkerArgumentBuilder (metadataProvider);

			AddLinkOptions (sandbox, compilationResult, builder, metadataProvider);

			LinkerTestLogger logger = new LinkerTestLogger ();
			linker.Link (builder.ToArgs (), linkerCustomizations, logger);

			return new LinkedTestCaseResult (testCase, compilationResult.InputAssemblyPath, sandbox.OutputDirectory.Combine (compilationResult.InputAssemblyPath.FileName), compilationResult.ExpectationsAssemblyPath, sandbox, metadataProvider, compilationResult, logger, linkerCustomizations);
		}

		protected virtual void AddLinkOptions (TestCaseSandbox sandbox, ManagedCompilationResult compilationResult, LinkerArgumentBuilder builder, TestCaseMetadataProvider metadataProvider)
		{
			var caseDefinedOptions = metadataProvider.GetLinkerOptions (sandbox.InputDirectory);

			builder.AddOutputDirectory (sandbox.OutputDirectory);

			foreach (var rspFile in sandbox.ResponseFiles)
				builder.AddResponseFile (rspFile);

			foreach (var inputReference in sandbox.InputDirectory.Files ()) {
				var ext = inputReference.ExtensionWithDot;
				if (ext == ".dll" || ext == ".exe")
					builder.AddReference (inputReference);
			}
			var coreAction = caseDefinedOptions.TrimMode ?? "skip";
			foreach (var extraReference in metadataProvider.GetExtraLinkerReferences ()) {
				builder.AddReference (extraReference);
				builder.AddAssemblyAction (coreAction, extraReference.FileNameWithoutExtension);
			}

			builder.ProcessOptions (caseDefinedOptions);

			builder.ProcessTestInputAssembly (compilationResult.InputAssemblyPath);
		}

		protected virtual LinkerCustomizations CustomizeLinker (LinkerDriver linker, TestCaseMetadataProvider metadataProvider)
		{
			LinkerCustomizations customizations = new LinkerCustomizations ();

			metadataProvider.CustomizeLinker (linker, customizations);

			return customizations;
		}

		private static T GetResultOfTaskThatMakesNUnitAssertions<T> (Task<T> task)
		{
			try {
				return task.Result;
			} catch (AggregateException e) {
				if (e.InnerException != null) {
					if (e.InnerException is AssertionException
					|| e.InnerException is SuccessException
					|| e.InnerException is IgnoreException
					|| e.InnerException is InconclusiveException)
						throw e.InnerException;
				}

				throw;
			}
		}
	}
}