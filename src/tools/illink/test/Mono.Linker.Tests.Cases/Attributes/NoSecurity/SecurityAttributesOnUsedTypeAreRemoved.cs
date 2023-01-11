using System.Security.Permissions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.NoSecurity
{
	[SetupLinkerArgument ("--strip-security", "true")]
	public class SecurityAttributesOnUsedTypeAreRemoved
	{
		static void Main ()
		{
			new Foo ();
		}

		[SecurityPermission (SecurityAction.LinkDemand)]
		[Kept]
		[KeptMember (".ctor()")]
		[RemovedPseudoAttribute (262144u)]
		class Foo
		{
		}
	}
}