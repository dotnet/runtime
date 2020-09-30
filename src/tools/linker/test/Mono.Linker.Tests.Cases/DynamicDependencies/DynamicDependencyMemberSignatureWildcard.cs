using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/DynamicDependencyMethodInAssemblyLibrary.cs" })]
	[LogContains ("IL2037: Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMemberSignatureWildcard.Dependency(): No members were resolved for '*'.")]
	public class DynamicDependencyMemberSignatureWildcard
	{
		public static void Main ()
		{
			Dependency ();
		}

		[Kept]
		[DynamicDependency ("*", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyMethodInAssemblyLibrary", "library")]
		static void Dependency ()
		{
		}
	}
}