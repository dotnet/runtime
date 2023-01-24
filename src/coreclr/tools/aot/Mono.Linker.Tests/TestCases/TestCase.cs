// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
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
			string displayNameBase = displayNameRelative.Depth == 1 ? "" : displayNameRelative.Parent.ToString (SlashMode.Forward).Replace ('/', '.') + ".";
			DisplayName = $"{displayNameBase}{sourceFile.FileNameWithoutExtension}";

			// A little hacky, but good enough for name.  No reason why namespace & type names
			// should not follow the directory structure
			//ReconstructedFullTypeName = $"{sourceFile.Parent.RelativeTo (rootCasesDirectory.Parent).ToString (SlashMode.Forward).Replace ('/', '.')}.{sourceFile.FileNameWithoutExtension}";
			ReconstructedFullTypeName = $"Mono.Linker.Tests.Cases.{fullyRelative.Parent.ToString (SlashMode.Forward).Replace ('/', '.')}.{sourceFile.FileNameWithoutExtension}";

			var firstParentRelativeToRoot = SourceFile.RelativeTo (rootCasesDirectory).Elements.First ();
			TestSuiteDirectory = rootCasesDirectory.Combine (firstParentRelativeToRoot);
		}

		public NPath RootCasesDirectory { get; }

		public string Name { get; }

		public string DisplayName { get; }

		public NPath SourceFile { get; }

		public NPath OriginalTestCaseAssemblyPath { get; }

		public string ReconstructedFullTypeName { get; }

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
	}
}
