using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileBefore ("unusedreference.dll", new[] { "Dependencies/UnusedAssemblyDependency.cs" })]
	[SetupCompileBefore ("reference.dll", new[] { "Dependencies/AssemblyDependency.cs" }, addAsReference: false)]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyDependencyWithMultipleReferences.cs" }, new[] { "reference.dll", "unusedreference.dll" }, addAsReference: false)]
	// TODO: keep library even if type is not found in it (https://github.com/dotnet/linker/issues/1795)
	// [KeptAssembly ("library")]
	public class AssemblyImportedViaReflectionWithSweptReferences
	{
		public static void Main ()
		{
			AccessNonExistingTypeInAssembly ();
		}

		[Kept]

		static void AccessNonExistingTypeInAssembly ()
		{
			// Import the library without marking it.
			var typeName = "DoesntExist, library";
			var typeKept = Type.GetType (typeName, false);
		}

		static void ReferenceUnusedAssemblyDependency ()
		{
			UnusedAssemblyDependency.UsedToKeepReferenceAtCompileTime ();
		}
	}
}