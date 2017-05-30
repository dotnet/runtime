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
			var compiler = _factory.CreateCompiler ();
			var sourceFiles = sandbox.SourceFiles.Select(s => s.ToString()).ToArray();

			var references = metadataProvider.GetReferencedAssemblies(sandbox.InputDirectory);
			var inputAssemblyPath = compiler.CompileTestIn (sandbox.InputDirectory, "test.exe", sourceFiles, references, null);

			references = metadataProvider.GetReferencedAssemblies(sandbox.ExpectationsDirectory);
			var expectationsAssemblyPath = compiler.CompileTestIn (sandbox.ExpectationsDirectory, "test.exe", sourceFiles, references, new [] { "INCLUDE_EXPECTATIONS" });
			return new ManagedCompilationResult (inputAssemblyPath, expectationsAssemblyPath);
		}

		private void PrepForLink (TestCaseSandbox sandbox, ManagedCompilationResult compilationResult)
		{
			var entryPointLinkXml = sandbox.InputDirectory.Combine ("entrypoint.xml");
			LinkXmlHelpers.WriteXmlFileToPreserveEntryPoint (compilationResult.InputAssemblyPath, entryPointLinkXml);
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

			builder.AddCoreLink (caseDefinedOptions.CoreLink);

			// Running the blacklist step causes a ton of stuff to be preserved.  That's good for normal use cases, but for
			// our test cases that pollutes the results
			if (!string.IsNullOrEmpty (caseDefinedOptions.IncludeBlacklistStep))
				builder.IncludeBlacklist (caseDefinedOptions.IncludeBlacklistStep);

			// Internationalization assemblies pollute our test case results as well so disable them
			if (!string.IsNullOrEmpty (caseDefinedOptions.Il8n))
				builder.AddIl8n (caseDefinedOptions.Il8n);

			linker.Link (builder.ToArgs ());

			return new LinkedTestCaseResult (testCase, compilationResult.InputAssemblyPath, sandbox.OutputDirectory.Combine (compilationResult.InputAssemblyPath.FileName), compilationResult.ExpectationsAssemblyPath);
		}
	}
}