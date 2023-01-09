using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Dependencies;

namespace Mono.Linker.Tests.Cases.References
{
	// Actions:
	// link - This assembly
	// copyused - library1.dll
	[SetupLinkerDefaultAction ("copyused")]
	[SetupLinkerAction ("link", "test")]

	[SetupCompileBefore ("library1.dll", new[] { "Dependencies/UserAssembliesAreLinkedByDefault_Library1.cs" })]

	[KeptAssembly ("library1.dll")]
	[KeptMemberInAssembly ("library1.dll", typeof (UserAssembliesAreLinkedByDefault_Library1), "UsedMethod()")]
	[KeptMemberInAssembly ("library1.dll", typeof (UserAssembliesAreLinkedByDefault_Library1), "UnusedMethod()")]
	class CopyUsedAreKeptFully
	{
		public static void Main ()
		{
			new UserAssembliesAreLinkedByDefault_Library1 ().UsedMethod ();
		}
	}
}
