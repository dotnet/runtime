// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCases
{
	public class TestCase
	{
		public TestCase (NPath sourceFile, NPath rootCasesDirectory, NPath originalTestCaseAssemblyPath)
		{
			SourceFile = sourceFile;
			RootCasesDirectory = rootCasesDirectory;
			OriginalTestCaseAssemblyPath = originalTestCaseAssemblyPath;
			Name = sourceFile.FileNameWithoutExtension;
			var fullyRelative = sourceFile.RelativeTo (rootCasesDirectory);
			var displayNameRelative = fullyRelative.RelativeTo (new NPath (fullyRelative.Elements.First ()));
			string displayNameBase = displayNameRelative.Depth == 1 ? "" : displayNameRelative.Parent.ToString (SlashMode.Forward).Replace ('/', '.');
			DisplayName = sourceFile.FileNameWithoutExtension == "Program" && sourceFile.Parent.FileName == originalTestCaseAssemblyPath.FileNameWithoutExtension
				? displayNameBase
				: $"{displayNameBase}.{sourceFile.FileNameWithoutExtension}";
			if (DisplayName.StartsWith("."))
				DisplayName = DisplayName.Substring(1);

			// A little hacky, but good enough for name.  No reason why namespace & type names
			// should not follow the directory structure
			//ReconstructedFullTypeName = $"{sourceFile.Parent.RelativeTo (rootCasesDirectory.Parent).ToString (SlashMode.Forward).Replace ('/', '.')}.{sourceFile.FileNameWithoutExtension}";
			reconstructedFullTypeName = $"Mono.Linker.Tests.Cases.{fullyRelative.Parent.ToString (SlashMode.Forward).Replace ('/', '.')}.{sourceFile.FileNameWithoutExtension}";

			var firstParentRelativeToRoot = SourceFile.RelativeTo (rootCasesDirectory).Elements.First ();
			TestSuiteDirectory = rootCasesDirectory.Combine (firstParentRelativeToRoot);
		}

		public NPath RootCasesDirectory { get; }

		public string Name { get; }

		public string DisplayName { get; }

		public NPath SourceFile { get; }

		public NPath OriginalTestCaseAssemblyPath { get; }

		private string reconstructedFullTypeName;

		public bool HasLinkXmlFile {
			get { return SourceFile.ChangeExtension ("xml").FileExists (); }
		}

		public NPath LinkXmlFile {
			get {
				if (!HasLinkXmlFile)
					throw new InvalidOperationException ("This test case does not have a link xml file");

				return SourceFile.ChangeExtension ("xml");
			}
		}

		public NPath TestSuiteDirectory { get; }

		public TypeDefinition FindTypeDefinition (AssemblyDefinition assemblyDefinition)
			=> TryFindTypeDefinition (assemblyDefinition) is TypeDefinition typeDefinition
				? typeDefinition
				: throw new InvalidOperationException ($"Could not find the type definition for {Name} in {assemblyDefinition.Name}");

		public TypeDefinition? TryFindTypeDefinition (AssemblyDefinition caseAssemblyDefinition)
		{
			var typeDefinition = caseAssemblyDefinition.MainModule.GetType (reconstructedFullTypeName);

			// For all of the Test Cases, the full type name we constructed from the directory structure will be correct and we can successfully find
			// the type from GetType.
			if (typeDefinition != null)
				return typeDefinition;

			// However, some of types are supporting types rather than test cases and may not follow the standardized naming scheme of the test cases.
			// We still need to be able to locate these type defs so that we can parse some of the metadata on them.
			// One example, Unity run's into this with its tests that require a type UnityEngine.MonoBehaviours to exist.  This type is defined in its own
			// file and it cannot follow our standardized naming directory & namespace naming scheme since the namespace must be UnityEngine.
			// Also look for compiler-generated Program type for top-level statements.
			foreach (var type in caseAssemblyDefinition.MainModule.Types) {
				if (type.Name == "Program" &&
					type.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (CompilerGeneratedAttribute)))
					return type;

				//  Let's assume we should never have to search for a test case that has no namespace.  If we don't find the type from GetType, then o well, that's not a test case.
				if (string.IsNullOrEmpty (type.Namespace))
					continue;

				if (type.Name == Name) {
					// This isn't foolproof, but let's do a little extra vetting to make sure the type we found corresponds to the source file we are
					// processing.
					if (!SourceFile.ReadAllText ().Contains ($"namespace {type.Namespace}"))
						continue;

					return type;
				}
			}

			return null;
		}
	}
}
