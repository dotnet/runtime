using System;
using System.Linq;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class TestRunner {
		private readonly ObjectFactory _factory;

		public TestRunner (ObjectFactory factory)
		{
			_factory = factory;
		}

		public virtual LinkedTestCaseResult Run (TestCase testCase)
		{
			using (var fullTestCaseAssemblyDefinition = AssemblyDefinition.ReadAssembly (testCase.OriginalTestCaseAssemblyPath.ToString ())) {
				var metadataProvider = _factory.CreateMetadataProvider (testCase, fullTestCaseAssemblyDefinition);

				string ignoreReason;
				if (metadataProvider.IsIgnored (out ignoreReason))
					Assert.Ignore (ignoreReason);

				var sandbox = Sandbox (testCase, metadataProvider);
				var compilationResult = Compile (sandbox, metadataProvider);
				PrepForLink (sandbox, compilationResult);
				return Link (testCase, sandbox, compilationResult, metadataProvider);
			}
		}

		public virtual LinkedTestCaseResult Relink (LinkedTestCaseResult result)
		{
			PrepForLink (result.Sandbox, result.CompilationResult);
			return Link (result.TestCase, result.Sandbox, result.CompilationResult, result.MetadataProvider);
		}

		private TestCaseSandbox Sandbox (TestCase testCase, TestCaseMetadaProvider metadataProvider)
		{
			var sandbox = _factory.CreateSandbox (testCase);
			sandbox.Populate (metadataProvider);
			return sandbox;
		}

		private ManagedCompilationResult Compile (TestCaseSandbox sandbox, TestCaseMetadaProvider metadataProvider)
		{
			var inputCompiler = _factory.CreateCompiler (sandbox, metadataProvider);
			var expectationsCompiler = _factory.CreateCompiler (sandbox, metadataProvider);
			var sourceFiles = sandbox.SourceFiles.Select(s => s.ToString()).ToArray();

			var assemblyName = metadataProvider.GetAssemblyName ();

			var commonReferences = metadataProvider.GetCommonReferencedAssemblies(sandbox.InputDirectory).ToArray ();
			var mainAssemblyReferences = metadataProvider.GetReferencedAssemblies(sandbox.InputDirectory).ToArray ();
			var resources = sandbox.ResourceFiles.ToArray ();
			var additionalArguments = metadataProvider.GetSetupCompilerArguments ().ToArray ();
			
			var expectationsCommonReferences = metadataProvider.GetCommonReferencedAssemblies (sandbox.ExpectationsDirectory).ToArray ();
			var expectationsMainAssemblyReferences = metadataProvider.GetReferencedAssemblies (sandbox.ExpectationsDirectory).ToArray ();

			var inputTask = Task.Run(() => inputCompiler.CompileTestIn (sandbox.InputDirectory, assemblyName, sourceFiles, commonReferences, mainAssemblyReferences, null, resources, additionalArguments));
			var expectationsTask = Task.Run(() => expectationsCompiler.CompileTestIn (sandbox.ExpectationsDirectory, assemblyName, sourceFiles, expectationsCommonReferences, expectationsMainAssemblyReferences, new[] {"INCLUDE_EXPECTATIONS"}, resources, additionalArguments));

			NPath inputAssemblyPath = null;
			NPath expectationsAssemblyPath = null;
			try {
				inputAssemblyPath = GetResultOfTaskThatMakesNUnitAssertions (inputTask);
				expectationsAssemblyPath = GetResultOfTaskThatMakesNUnitAssertions (expectationsTask);
			} catch (Exception) {
				// If completing the input assembly task threw, we need to wait for the expectations task to complete before continuing
				// otherwise we could set the next test up for a race condition with the expectations compilation over access to the sandbox directory
				if (inputAssemblyPath == null && expectationsAssemblyPath == null)
				{
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

		private LinkedTestCaseResult Link (TestCase testCase, TestCaseSandbox sandbox, ManagedCompilationResult compilationResult, TestCaseMetadaProvider metadataProvider)
		{
			var linker = _factory.CreateLinker ();
			var builder = _factory.CreateLinkerArgumentBuilder (metadataProvider);

			AddLinkOptions (sandbox, compilationResult, builder, metadataProvider);

			linker.Link (builder.ToArgs ());

			return new LinkedTestCaseResult (testCase, compilationResult.InputAssemblyPath, sandbox.OutputDirectory.Combine (compilationResult.InputAssemblyPath.FileName), compilationResult.ExpectationsAssemblyPath, sandbox, metadataProvider, compilationResult);
		}

		protected virtual void AddLinkOptions (TestCaseSandbox sandbox, ManagedCompilationResult compilationResult, LinkerArgumentBuilder builder, TestCaseMetadaProvider metadataProvider)
		{
			var caseDefinedOptions = metadataProvider.GetLinkerOptions ();

			builder.AddOutputDirectory (sandbox.OutputDirectory);
			foreach (var linkXmlFile in sandbox.LinkXmlFiles)
				builder.AddLinkXmlFile (linkXmlFile);

			foreach (var linkXmlFile in sandbox.ResponseFiles)
				builder.AddResponseFile (linkXmlFile);

			builder.AddSearchDirectory (sandbox.InputDirectory);
			foreach (var extraSearchDir in metadataProvider.GetExtraLinkerSearchDirectories ())
				builder.AddSearchDirectory (extraSearchDir);

			builder.ProcessOptions (caseDefinedOptions);

			builder.ProcessTestInputAssembly (compilationResult.InputAssemblyPath);
		}

		private T GetResultOfTaskThatMakesNUnitAssertions<T> (Task<T> task)
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