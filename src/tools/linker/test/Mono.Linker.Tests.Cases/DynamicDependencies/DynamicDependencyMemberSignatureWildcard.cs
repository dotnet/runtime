using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[LogContains ("IL2037: Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMemberSignatureWildcard::Dependency(): No members were resolved for '*'.")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/DynamicDependencyMethodInAssemblyLibrary.cs" })]
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