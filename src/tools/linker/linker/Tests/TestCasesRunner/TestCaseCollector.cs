using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.TestCases;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class TestCaseCollector {
		private readonly NPath _rootDirectory;
		private readonly NPath _testCaseAssemblyPath;

		public TestCaseCollector (string rootDirectory, string testCaseAssemblyPath)
			: this (rootDirectory.ToNPath (), testCaseAssemblyPath.ToNPath ())
		{
		}

		public TestCaseCollector (NPath rootDirectory, NPath testCaseAssemblyPath)
		{
			_rootDirectory = rootDirectory;
			_testCaseAssemblyPath = testCaseAssemblyPath;
		}

		public IEnumerable<TestCase> Collect ()
		{
			return Collect (AllSourceFiles ());
		}

		public TestCase Collect (NPath sourceFile)
		{
			return Collect (new [] { sourceFile }).First ();
		}

		public IEnumerable<TestCase> Collect (IEnumerable<NPath> sourceFiles)
		{
			_rootDirectory.DirectoryMustExist ();
			_testCaseAssemblyPath.FileMustExist ();

			using (var caseAssemblyDefinition = AssemblyDefinition.ReadAssembly (_testCaseAssemblyPath.ToString ())) {
				foreach (var file in sourceFiles) {
					TestCase testCase;
					if (CreateCase (caseAssemblyDefinition, file, out testCase))
						yield return testCase;
				}
			}
		}

		public IEnumerable<NPath> AllSourceFiles ()
		{
			_rootDirectory.DirectoryMustExist ();

			foreach (var file in _rootDirectory.Files ("*.cs")) {
				yield return file;
			}

			foreach (var subDir in _rootDirectory.Directories ()) {
				if (subDir.FileName == "bin" || subDir.FileName == "obj" || subDir.FileName == "Properties")
					continue;

				foreach (var file in subDir.Files ("*.cs", true)) {
					yield return file;
				}
			}
		}

		private bool CreateCase (AssemblyDefinition caseAssemblyDefinition, NPath sourceFile, out TestCase testCase)
		{
			var potentialCase = new TestCase (sourceFile, _rootDirectory, _testCaseAssemblyPath);

			var typeDefinition = FindTypeDefinition (caseAssemblyDefinition, potentialCase);

			if (typeDefinition == null)
				throw new InvalidOperationException ($"Could not find the matching type for test case {sourceFile}.  Ensure the file name and class name match");

			if (typeDefinition.HasAttribute (nameof (NotATestCaseAttribute))) {
				testCase = null;
				return false;
			}

			// Verify the class as a static main method
			var mainMethod = typeDefinition.Methods.FirstOrDefault (m => m.Name == "Main");

			if (mainMethod == null)
				throw new InvalidOperationException ($"{typeDefinition} in {sourceFile} is missing a Main() method");

			if (!mainMethod.IsStatic)
				throw new InvalidOperationException ($"The Main() method for {typeDefinition} in {sourceFile} should be static");

			testCase = potentialCase;
			return true;
		}

		private static TypeDefinition FindTypeDefinition (AssemblyDefinition caseAssemblyDefinition, TestCase testCase)
		{
			var typeDefinition = caseAssemblyDefinition.MainModule.GetType (testCase.ReconstructedFullTypeName);

			// For all of the Test Cases, the full type name we constructed from the directory structure will be correct and we can successfully find
			// the type from GetType.
			if (typeDefinition != null)
				return typeDefinition;

			// However, some of types are supporting types rather than test cases.  and may not follow the standardized naming scheme of the test cases
			// We still need to be able to locate these type defs so that we can parse some of the metadata on them.
			// One example, Unity run's into this with it's tests that require a type UnityEngine.MonoBehaviours to exist.  This tpe is defined in it's own
			// file and it cannot follow our standardized naming directory & namespace naming scheme since the namespace must be UnityEngine
			foreach (var type in caseAssemblyDefinition.MainModule.Types) {
				//  Let's assume we should never have to search for a test case that has no namespace.  If we don't find the type from GetType, then o well, that's not a test case.
				if (string.IsNullOrEmpty (type.Namespace))
					continue;

				if (type.Name == testCase.Name) {
					// This isn't foolproof, but let's do a little extra vetting to make sure the type we found corresponds to the source file we are
					// processing.
					if (!testCase.SourceFile.ReadAllText ().Contains ($"namespace {type.Namespace}"))
						continue;

					return type;
				}
			}

			return null;
		}
	}
}