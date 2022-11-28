using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Dependencies;

namespace Mono.Linker.Tests.Cases.References
{
	[SetupCompileBefore ("library1.dll", new[] { "Dependencies/UserAssembliesAreLinkedByDefault_Library1.cs" })]

	[KeptAssembly ("library1.dll")]
	[KeptMemberInAssembly ("library1.dll", typeof (UserAssembliesAreLinkedByDefault_Library1), "UsedMethod()")]
	[RemovedMemberInAssembly ("library1.dll", typeof (UserAssembliesAreLinkedByDefault_Library1), "UnusedMethod()")]
	class UserAssembliesAreLinkedByDefault
	{
		public static void Main ()
		{
			new UserAssembliesAreLinkedByDefault_Library1 ().UsedMethod ();
		}
	}
}
