using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.TestCases;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class TestRunner {
		private readonly ObjectFactory _factory;

		public TestRunner (ObjectFactory factory)
		{
			_factory = factory;
		}

		public LinkedTestCaseResult Run (TestCase testCase)
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

		private TestCaseSandbox Sandbox (TestCase testCase, TestCaseMetadaProvider metadataProvider)
		{
			var sandbox = _factory.CreateSandbox (testCase);
			sandbox.Populate (metadataProvider);
			return sandbox;
		}

		private ManagedCompilationResult Compile (TestCaseSandbox sandbox, TestCaseMetadaProvider metadataProvider)
		{
			var compiler = _factory.CreateCompiler (sandbox, metadataProvider);
			var sourceFiles = sandbox.SourceFiles.Select(s => s.ToString()).ToArray();

			var assemblyName = metadataProvider.GetAssemblyName ();

			var references = metadataProvider.GetReferencedAssemblies(sandbox.InputDirectory);
			var resources = sandbox.ResourceFiles.ToArray ();
			var inputAssemblyPath = compiler.CompileTestIn (sandbox.InputDirectory, assemblyName, sourceFiles, references, null, resources);

			references = metadataProvider.GetReferencedAssemblies(sandbox.ExpectationsDirectory);
			var expectationsAssemblyPath = compiler.CompileTestIn (sandbox.ExpectationsDirectory, assemblyName, sourceFiles, references, new [] { "INCLUDE_EXPECTATIONS" }, resources);
			return new ManagedCompilationResult (inputAssemblyPath, expectationsAssemblyPath);
		}

		protected virtual void PrepForLink (TestCaseSandbox sandbox, ManagedCompilationResult compilationResult)
		{
		}

		private LinkedTestCaseResult Link (TestCase testCase, TestCaseSandbox sandbox, ManagedCompilationResult compilationResult, TestCaseMetadaProvider metadataProvider)
		{
			var linker = _factory.CreateLinker ();
			var builder = _factory.CreateLinkerArgumentBuilder ();
			var caseDefinedOptions = metadataProvider.GetLinkerOptions ();

			builder.AddOutputDirectory (sandbox.OutputDirectory);
			foreach (var linkXmlFile in sandbox.LinkXmlFiles)
				builder.AddLinkXmlFile (linkXmlFile);

			builder.AddSearchDirectory (sandbox.InputDirectory);
			foreach (var extraSearchDir in metadataProvider.GetExtraLinkerSearchDirectories ())
				builder.AddSearchDirectory (extraSearchDir);

			builder.ProcessOptions (caseDefinedOptions);

			AddAdditionalLinkOptions (builder, metadataProvider);

			// TODO: Should be overridable
			builder.LinkFromAssembly (compilationResult.InputAssemblyPath.ToString ());

			linker.Link (builder.ToArgs ());

			return new LinkedTestCaseResult (testCase, compilationResult.InputAssemblyPath, sandbox.OutputDirectory.Combine (compilationResult.InputAssemblyPath.FileName), compilationResult.ExpectationsAssemblyPath);
		}

		protected virtual void AddAdditionalLinkOptions (LinkerArgumentBuilder builder, TestCaseMetadaProvider metadataProvider)
		{
		}
	}
}