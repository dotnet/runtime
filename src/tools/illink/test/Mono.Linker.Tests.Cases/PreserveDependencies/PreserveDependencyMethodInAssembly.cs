using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.PreserveDependencies
{
	[KeptMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies.PreserveDependencyMethodInAssemblyLibrary", ".ctor()")]
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "Dependencies/PreserveDependencyAttribute.cs" })]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/PreserveDependencyMethodInAssemblyLibrary.cs" })]
	public class PreserveDependencyMethodInAssembly
	{
		public static void Main ()
		{
			Dependency ();
		}

		[Kept]
		[PreserveDependency (".ctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies.PreserveDependencyMethodInAssemblyLibrary", "library")]
		static void Dependency ()
		{
		}
	}
}
