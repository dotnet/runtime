using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileBefore ("unusedreference.dll", new[] { "Dependencies/UnusedAssemblyDependency.cs" })]
	[SetupCompileBefore ("reference.dll", new[] { "Dependencies/AssemblyDependency.cs" }, addAsReference: false)]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyDependencyWithMultipleReferences.cs" }, new[] { "reference.dll", "unusedreference.dll" }, addAsReference: false)]
	// TODO: keep library even if type is not found in it (https://github.com/dotnet/linker/issues/1795)
	// [KeptAssembly ("library")]
	public class DynamicDependencyMethodInNonReferencedAssemblyWithSweptReferences
	{
		public static void Main ()
		{
			DynamicDependencyOnNonExistingTypeInAssembly ();
		}

		[Kept]
		[DynamicDependency ("#ctor()", "DoesntExist", "library")]
		static void DynamicDependencyOnNonExistingTypeInAssembly ()
		{
		}

		static void ReferenceUnusedAssemblyDependency ()
		{
			UnusedAssemblyDependency.UsedToKeepReferenceAtCompileTime ();
		}
	}
}