using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Dependencies;

namespace Mono.Linker.Tests.Cases.References
{
	[SetupLinkerAction ("link", "test")]
	[SetupLinkerDefaultAction ("copy")]

	// Used assembly references are kept
	[SetupCompileBefore ("usedlibrary.dll", new[] { "Dependencies/UsedReferencedAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("usedlibrary.dll")]


	// Unused references from copy assembly are kept
	[SetupCompileBefore ("unusedlibraryfromcopy.dll", new[] { "Dependencies/UnusedReferencedFromCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("unusedlibraryfromcopy.dll")]
	// Unused assembly references are kept
	[SetupCompileBefore ("unusedlibrary.dll", new[] { "Dependencies/UnusedReferencedAssembly.cs" }, references: new[] { "unusedlibraryfromcopy.dll" })]
	[KeptAllTypesAndMembersInAssembly ("unusedlibrary.dll")]
	// Unused dynamic references from copy assembly are kept
	[SetupCompileBefore ("unuseddynamiclibraryfromcopy.dll", new[] { "Dependencies/UnusedDynamicallyReferencedFromCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("unuseddynamiclibraryfromcopy.dll")]


	// Unused references from dynamic copy assembly are kept
	[SetupCompileBefore ("unusedlibraryfromdynamiccopy.dll", new[] { "Dependencies/UnusedReferencedFromDynamicCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("unusedlibraryfromdynamiccopy.dll")]
	// Used dynamic references are kept
	[SetupCompileBefore ("useddynamiclibrary.dll", new[] { "Dependencies/UsedDynamicallyReferencedAssembly.cs" }, references: new[] { "unusedlibraryfromdynamiccopy.dll" })]
	[KeptAllTypesAndMembersInAssembly ("useddynamiclibrary.dll")]
	// Unused dynamic references from dynamic copy assembly are kept
	[SetupCompileBefore ("unuseddynamiclibraryfromdynamiccopy.dll", new[] { "Dependencies/UnusedDynamicallyReferencedFromDynamicCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("unuseddynamiclibraryfromdynamiccopy.dll")]


	// Unused dynamic references from link assembly are kept (due to copy default action)
	[SetupCompileBefore ("unuseddynamiclibrary.dll", new[] { "Dependencies/UnusedDynamicallyReferencedAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("unuseddynamiclibrary.dll")]


	// Unreferenced libraries are kept (due to copy default action)
	[SetupCompileBefore ("unreferencedlibrary.dll", new[] { "Dependencies/UnreferencedAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("unreferencedlibrary.dll")]
	class CopyAreKeptFully
	{
		public static void Main ()
		{
			Used ();
		}

		[Kept]
		public static void Used ()
		{
			var _ = new UsedReferencedAssembly ();
			var _2 = Type.GetType ("Mono.Linker.Tests.Cases.References.Dependencies.UsedDynamicallyReferencedAssembly, useddynamiclibrary");
		}

		public static void Unused ()
		{
			var _ = new UnusedReferencedAssembly ();
			var _2 = Type.GetType ("Mono.Linker.Tests.Cases.References.Dependencies.UnusedDynamicallyReferencedAssembly, unuseddynamiclibrary");
		}
	}
}
