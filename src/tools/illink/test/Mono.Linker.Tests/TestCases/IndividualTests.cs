using System;
using System.IO;
using System.Xml;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.CommandLine.Mvid;
using Mono.Linker.Tests.Cases.References.Individual;
using Mono.Linker.Tests.Cases.Tracing.Individual;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;
using Mono.Linker.Tests.TestCasesRunner;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCases
{
	[TestFixture]
	public class IndividualTests
	{
		[Test]
		public void CanSkipUnresolved ()
		{
			var testcase = CreateIndividualCase (typeof (CanSkipUnresolved));
			var result = Run (testcase);

			// We can't use the ResultChecker on the output because there will be unresolved types/methods
			// Let's just make sure that the output assembly exists.  That's enough to verify that the linker didn't throw due to the
			// missing types/methods
			if (!result.OutputAssemblyPath.Exists ())
				Assert.Fail ($"The linked assembly is missing.  Should have existed at {result.OutputAssemblyPath}");
		}

		[Test]
		public void CanEnableDependenciesDump ()
		{
			var testcase = CreateIndividualCase (typeof (CanEnableDependenciesDump));
			var result = Run (testcase);

			var outputPath = result.OutputAssemblyPath.Parent.Combine (Tracer.DefaultDependenciesFileName);
			if (!outputPath.Exists ())
				Assert.Fail ($"The dependency dump file is missing.  Expected it to exist at {outputPath}");
		}

		[Test]
		public void CanDumpDependenciesToUncompressedXml ()
		{
			var testcase = CreateIndividualCase (typeof (CanDumpDependenciesToUncompressedXml));
			var result = Run (testcase);

			var outputPath = result.OutputAssemblyPath.Parent.Combine ("linker-dependencies.xml");
			if (!outputPath.Exists ())
				Assert.Fail($"The dependency dump file is missing.  Expected it to exist at {outputPath}");

			// Do a basic check to verify that the contents of the file are uncompressed xml
			using (var reader = new XmlTextReader (outputPath.ToString ())) {
				reader.Read ();
				reader.Read ();
				reader.Read ();
				Assert.That (reader.Name, Is.EqualTo ("dependencies"), $"Expected to be at the dependencies element, but the current node name is `{reader.Name}`");
			}
		}

		[Test]
		public void CanEnableReducedTracing ()
		{
			var testcase = CreateIndividualCase (typeof (CanEnableReducedTracing));
			var result = Run (testcase);

			// Note: This name needs to match what is setup in the test case arguments to the linker
			const string expectedDependenciesFileName = "linker-dependencies.xml";
			var outputPath = result.OutputAssemblyPath.Parent.Combine (expectedDependenciesFileName);
			if (!outputPath.Exists ())
				Assert.Fail($"The dependency dump file is missing.  Expected it to exist at {outputPath}");

			// Let's go a little bit further and make sure it looks like reducing tracking actually worked.
			// This is intentionally a loose assertion.  This test isn't meant to verify how reduced tracing works,
			// it's here to make sure that enabling the option enables the behavior.
			var lineCount = outputPath.ReadAllLines ().Length;

			// When reduced tracing is not enabled there are around 16k of lines in the output file.
			// With reduced tracing there should be less than 65, but to be safe, we'll check for less than 200.
			// Reduced tracing on System.Private.CoreLib.dll produces about 130 lines just for NullableAttribute usages.
			const int expectedMaxLines = 200;
			Assert.That (lineCount, Is.LessThan (expectedMaxLines), $"There were `{lineCount}` lines in the dump file.  This is more than expected max of {expectedMaxLines} and likely indicates reduced tracing was not enabled.  Dump file can be found at: {outputPath}");
		}

		[Test]
		public void DeterministicMvidWorks()
		{
			var testCase = CreateIndividualCase (typeof (DeterministicMvidWorks));
			var runner = new TestRunner (new ObjectFactory ());
			var result = runner.Run (testCase);

			var originalMvid = GetMvid (result.InputAssemblyPath);
			var firstOutputMvid = GetMvid (result.OutputAssemblyPath);
			Assert.That (firstOutputMvid, Is.Not.EqualTo (originalMvid));
			
			var result2 = runner.Relink(result);
			
			var secondOutputMvid = GetMvid(result2.OutputAssemblyPath);
			Assert.That (secondOutputMvid, Is.Not.EqualTo (originalMvid));
			// The id should match the first output since we relinked the same assembly
			Assert.That (secondOutputMvid, Is.EqualTo (firstOutputMvid));
		}

		[Test]
		public void NewMvidWorks ()
		{
			var testCase = CreateIndividualCase (typeof (NewMvidWorks));
			var runner = new TestRunner (new ObjectFactory ());
			var result = runner.Run (testCase);

			var originalMvid = GetMvid (result.InputAssemblyPath);
			var firstOutputMvid = GetMvid (result.OutputAssemblyPath);
			Assert.That (firstOutputMvid, Is.Not.EqualTo (originalMvid));
			
			var result2 = runner.Relink (result);
			
			var secondOutputMvid = GetMvid (result2.OutputAssemblyPath);
			Assert.That (secondOutputMvid, Is.Not.EqualTo (originalMvid));
			Assert.That (secondOutputMvid, Is.Not.EqualTo (firstOutputMvid));
		}

		[Test]
		public void RetainMvidWorks ()
		{
			var testCase = CreateIndividualCase (typeof (RetainMvid));
			var runner = new TestRunner (new ObjectFactory ());
			var result = runner.Run (testCase);

			var originalMvid = GetMvid (result.InputAssemblyPath);
			var firstOutputMvid = GetMvid (result.OutputAssemblyPath);
			Assert.That (firstOutputMvid, Is.EqualTo (originalMvid));
			
			var result2 = runner.Relink (result);
			
			var secondOutputMvid = GetMvid (result2.OutputAssemblyPath);
			Assert.That (secondOutputMvid, Is.EqualTo (originalMvid));
			Assert.That (secondOutputMvid, Is.EqualTo (firstOutputMvid));
		}

		[Test]
		public void DefaultMvidBehavior ()
		{
			var testCase = CreateIndividualCase (typeof (NewMvidWorks));
			var runner = new TestRunner (new ObjectFactory ());
			var result = runner.Run (testCase);

			var originalMvid = GetMvid (result.InputAssemblyPath);
			var firstOutputMvid = GetMvid (result.OutputAssemblyPath);
			Assert.That (firstOutputMvid, Is.Not.EqualTo (originalMvid));
			
			var result2 = runner.Relink (result);
			
			var secondOutputMvid = GetMvid (result2.OutputAssemblyPath);
			Assert.That (secondOutputMvid, Is.Not.EqualTo (originalMvid));
			Assert.That (secondOutputMvid, Is.Not.EqualTo (firstOutputMvid));
		}

		protected Guid GetMvid (NPath assemblyPath)
		{
			using (var assembly = AssemblyDefinition.ReadAssembly (assemblyPath))
			{
				return assembly.MainModule.Mvid;
			}
		}

		private TestCase CreateIndividualCase (Type testCaseType)
		{
			return TestDatabase.CreateCollector ().CreateIndividualCase (testCaseType);
		}

		protected virtual LinkedTestCaseResult Run (TestCase testCase)
		{
			var runner = new TestRunner (new ObjectFactory ());
			return runner.Run (testCase);
		}
	}
}
