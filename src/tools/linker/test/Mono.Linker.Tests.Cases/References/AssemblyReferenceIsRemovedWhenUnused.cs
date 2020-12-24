using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Dependencies;

namespace Mono.Linker.Tests.Cases.References
{
	[SetupCompileBefore ("library1.dll", new[] { "Dependencies/AssemblyReferenceIsRemovedWhenUnusedLib.cs" })]

	[RemovedAssembly ("library1.dll")]
	[RemovedAssemblyReference ("test", "library1")]
	class AssemblyReferenceIsRemovedWhenUnused
	{
		public static void Main ()
		{
		}

		static void Unused ()
		{
			new AssemblyReferenceIsRemovedWhenUnusedLib ().UsedMethod ();
		}
	}
}
