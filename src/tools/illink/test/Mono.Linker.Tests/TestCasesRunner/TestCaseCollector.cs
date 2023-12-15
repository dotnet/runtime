// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestCaseCollector
	{
		private readonly NPath _rootDirectory;
		private readonly NPath _testCaseAssemblyRoot;

		public TestCaseCollector (string rootDirectory, string testCaseAssemblyRoot)
			: this (rootDirectory.ToNPath (), testCaseAssemblyRoot.ToNPath ())
		{
		}

		public TestCaseCollector (NPath rootDirectory, NPath testCaseAssemblyRoot)
		{
			_rootDirectory = rootDirectory;
			_testCaseAssemblyRoot = testCaseAssemblyRoot;
		}

		public IEnumerable<TestCase> Collect ()
		{
			return Collect (AllSourceFiles ());
		}

		public TestCase Collect (NPath sourceFile)
		{
			return Collect (new[] { sourceFile }).FirstOrDefault ();
		}

		public IEnumerable<TestCase> Collect (IEnumerable<NPath> sourceFiles)
		{
			_rootDirectory.DirectoryMustExist ();
			_testCaseAssemblyRoot.DirectoryMustExist ();

			foreach (var file in sourceFiles) {
				var testCaseAssemblyPath = FindTestCaseAssembly (file);
				testCaseAssemblyPath.FileMustExist ();
				if (CreateCase (testCaseAssemblyPath, file, out TestCase testCase)) {
					yield return testCase;
				}
			}
		}

		NPath FindTestCaseAssembly (NPath sourceFile)
		{
			if (!sourceFile.IsChildOf (_rootDirectory))
				throw new ArgumentException ($"{sourceFile} is not a child of {_rootDirectory}");
			
			var current = sourceFile;
			do {
				// Find nearest .csproj in the test root source directory
				if (current.Parent.Files ("*.csproj").FirstOrDefault () is NPath csproj) {
					// Expect testcase assembly in the output with the same relative path as the csproj
					var relative = csproj.Parent.RelativeTo (_rootDirectory);
					return _testCaseAssemblyRoot.Combine (relative).Combine (csproj.ChangeExtension ("dll").FileName);
				}

				current = current.Parent;
			} while (current != _rootDirectory);

			throw new InvalidOperationException ($"Could not find a .csproj file for {sourceFile}");
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

					var relativeParents = file.RelativeTo (_rootDirectory);
					// Magic : Anything in a directory named Dependencies is assumed to be a dependency to a test case
					// and never a test itself
					// This makes life a little easier when writing these supporting files as it removes some constraints you would previously have
					// had to follow such as ensuring a class exists that matches the file name and putting [NotATestCase] on that class
					if (relativeParents.RecursiveParents.Any (p => p.Elements.Any () && p.FileName == "Dependencies"))
						continue;

					// Magic: Anything in a directory named Individual is expected to be ran by it's own [Test] rather than as part of [TestCaseSource]
					if (relativeParents.RecursiveParents.Any (p => p.Elements.Any () && p.FileName == "Individual"))
						continue;

					yield return file;
				}
			}
		}

		public TestCase CreateIndividualCase (Type testCaseType)
		{
			_rootDirectory.DirectoryMustExist ();
			_testCaseAssemblyRoot.DirectoryMustExist ();

			var pathRelativeToAssembly = $"{testCaseType.FullName.Substring (testCaseType.Module.Name.Length - 3).Replace ('.', '/')}.cs";
			var fullSourcePath = _rootDirectory.Combine (pathRelativeToAssembly).FileMustExist ();
			var testCaseAssemblyPath = FindTestCaseAssembly (fullSourcePath);

			if (!CreateCase (testCaseAssemblyPath, fullSourcePath, out TestCase testCase))
				throw new ArgumentException ($"Could not create a test case for `{testCaseType}`.  Ensure the namespace matches it's location on disk");

			return testCase;
		}

		private bool CreateCase (NPath caseAssemblyPath, NPath sourceFile, out TestCase testCase)
		{
			using AssemblyDefinition caseAssemblyDefinition = AssemblyDefinition.ReadAssembly (caseAssemblyPath.ToString ());
			var potentialCase = new TestCase (sourceFile, _rootDirectory, caseAssemblyPath);

			var typeDefinition = potentialCase.FindTypeDefinition (caseAssemblyDefinition);

			testCase = null;

			if (typeDefinition == null) {
				Console.WriteLine ($"Could not find the matching type for test case {sourceFile}.  Ensure the file name and class name match");
				return false;
			}

			if (typeDefinition.HasAttribute (nameof (NotATestCaseAttribute))) {
				return false;
			}

			// Verify the class as a static main method
			MethodDefinition mainMethod = typeDefinition.Methods.FirstOrDefault (m => m.Name ==
				(typeDefinition.FullName == "Program"
					? "<Main>$" // Compiler-generated Main for top-level statements
					: "Main"));

			if (mainMethod == null) {
				Console.WriteLine ($"{typeDefinition} in {sourceFile} is missing a Main() method");
				return false;
			}

			if (!mainMethod.IsStatic) {
				Console.WriteLine ($"The Main() method for {typeDefinition} in {sourceFile} should be static");
				return false;
			}

			testCase = potentialCase;
			return true;
		}
	}
}
