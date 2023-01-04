using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/DynamicDependencyMethodInAssemblyLibrary.cs" })]
	[ExpectedNoWarnings]
	public class DynamicDependencyMemberSignatureWildcard
	{
		public static void Main ()
		{
			Dependency ();
		}

		[Kept]
		[ExpectedWarning ("IL2037", "'*'", "'Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyMethodInAssemblyLibrary'")]
		[DynamicDependency ("*", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyMethodInAssemblyLibrary", "library")]
		static void Dependency ()
		{
		}
	}
}