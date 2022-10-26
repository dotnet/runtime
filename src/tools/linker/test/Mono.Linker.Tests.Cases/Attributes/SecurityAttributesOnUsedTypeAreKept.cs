using System;
using System.Security.Permissions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[SetupLinkerArgument ("--strip-security", "false")]
	class SecurityAttributesOnUsedTypeAreKept
	{
		static void Main ()
		{
			new Foo ();
		}

		[SecurityPermission (SecurityAction.LinkDemand)]
		[Kept]
		[KeptMember (".ctor()")]
		[KeptSecurity (typeof (SecurityPermissionAttribute))]
		class Foo
		{
		}
	}
}
