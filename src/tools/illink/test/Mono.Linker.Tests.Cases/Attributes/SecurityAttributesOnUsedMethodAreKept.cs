using System;
using System.Security.Permissions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[SetupLinkerArgument ("--strip-security", "false")]
	class SecurityAttributesOnUsedMethodAreKept
	{
		static void Main ()
		{
			new Foo ().Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Foo
		{
			[SecurityPermission (SecurityAction.LinkDemand)]
			[Kept]
			[KeptSecurity (typeof (SecurityPermissionAttribute))]
			public void Method ()
			{
			}
		}
	}
}
