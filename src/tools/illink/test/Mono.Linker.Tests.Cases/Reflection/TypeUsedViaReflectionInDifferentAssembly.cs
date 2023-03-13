using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyDependency.cs" })]
	[KeptAssembly ("library.dll")]
	[KeptTypeInAssembly ("library.dll", "Mono.Linker.Tests.Cases.Reflection.Dependencies.AssemblyDependency/TypeThatIsUsedViaReflection")]
	public class TypeUsedViaReflectionInDifferentAssembly
	{
		public static void Main ()
		{
			AssemblyDependency.UsedToKeepReferenceAtCompileTime ();
			Helper ();
		}

		[Kept]
		static Type Helper ()
		{
			return Type.GetType ("Mono.Linker.Tests.Cases.Reflection.Dependencies.AssemblyDependency+TypeThatIsUsedViaReflection, library");
		}
	}
}