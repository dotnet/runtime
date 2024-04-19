// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Libraries.Dependencies;

namespace Mono.Linker.Tests.Cases.Libraries
{
	[IgnoreTestCase ("NativeAOT doesn't implement library trimming the same way", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute (typeof (IgnoreTestCaseAttribute), By = Tool.Trimmer)]
	// This validates the trimming behavior of a library when it is trimmed following
	// the instructions for preparing libraries for trimming:
	// https://learn.microsoft.comdotnet/core/deploying/trimming/prepare-libraries-for-trimming
	[SetupCompileBefore ("exportedtype.dll", new[] { typeof (RootAllLibrary_ExportedType) })]
	[SetupCompileBefore ("optionaldependency.dll", new[] { typeof (RootAllLibrary_OptionalDependency) })]
	[SetupCompileBefore ("removedattribute.dll", new[] { typeof (RootAllLibrary_RemovedAttribute) },
		resources: new object[] {
			new string[] { "Dependencies/RootAllLibrary_LinkAttributes.xml", "ILLink.LinkAttributes.xml" }
		})]
	[SetupCompileBefore ("library.dll",
		new[] { typeof (RootAllLibrary) },
		references: new[] { "exportedtype.dll", "optionaldependency.dll", "removedattribute.dll" },
		defines: new[] { "RootAllLibrary" },
		resources: new object[] {
			new string[] { "Dependencies/RootAllLibrary_Substitutions.xml", "ILLink.Substitutions.xml" },
		})]

	[SetupLinkerArgument ("-a", "library.dll")]
	[IgnoreSubstitutions (false)]
	[IgnoreLinkAttributes (false)]

	[ExpectedNoWarnings]
	[KeptMemberInAssembly ("library.dll", typeof (RootAllLibrary), "Public()")]
	[KeptMemberInAssembly ("library.dll", typeof (RootAllLibrary), "Private()")]
	[KeptMemberInAssembly ("library.dll", typeof (RootAllLibrary), "RemovedBranch()")]
	[KeptMemberInAssembly ("library.dll", typeof (RootAllLibrary), "get_SubstitutedProperty()")]
	[KeptTypeInAssembly ("library.dll", "Mono.Linker.Tests.Cases.Libraries.Dependencies.NonPublicType")]
	[KeptTypeInAssembly ("library.dll", "Mono.Linker.Tests.Cases.Libraries.Dependencies.RootAllLibrary/NestedType")]

	// Type forwarders are kept (but not necessarily the type definition that the forwarder points to)
	[KeptTypeInAssembly ("library.dll", typeof (RootAllLibrary_ExportedType))]
	[RemovedAssembly ("exportedtype.dll")]

	// Substitutions and branch removal work. Dependencies reachable only from removed branches are removed.
	[RemovedAssembly ("optionaldependency.dll")]
	[LogDoesNotContain ("IL2026:.*SubstitutedProperty.get:.*RequiresUnreferencedCode\\(\\)", regexMatch: true)]
	[LogDoesNotContain ("IL2026:.*RemovedBranch\\(\\):.*RootAllLibrary_OptionalDependency.Use\\(\\)", regexMatch: true)]

	// Attribute removal works.
	[RemovedAssembly ("removedattribute.dll")]

	[Kept]
	public class RootAllLibraryBehavior
	{
		[Kept]
		public static void Main ()
		{
		}
	}
}
