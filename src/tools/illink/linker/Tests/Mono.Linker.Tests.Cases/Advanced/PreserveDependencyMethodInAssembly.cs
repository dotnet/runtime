using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Advanced
{
	[KeptMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.Advanced.Dependencies.PreserveDependencyMethodInAssemblyLibrary", ".ctor()")]
	[SetupCompileBefore ("library.dll", new [] { "Dependencies/PreserveDependencyMethodInAssemblyLibrary.cs" })]
	public class PreserveDependencyMethodInAssembly
	{
		public static void Main ()
		{
			Dependency ();
		}

		[Kept]
		[PreserveDependency (".ctor()", "Mono.Linker.Tests.Cases.Advanced.Dependencies.PreserveDependencyMethodInAssemblyLibrary", "library")]
		static void Dependency ()
		{
		}
	}
}
