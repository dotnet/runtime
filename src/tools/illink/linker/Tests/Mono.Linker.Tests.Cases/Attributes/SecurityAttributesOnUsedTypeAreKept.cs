using System;
using System.Security.Permissions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes {
	class SecurityAttributesOnUsedTypeAreKept {
		static void Main ()
		{
			new Foo ();
		}

		[SecurityPermission (SecurityAction.LinkDemand)]
		[Kept]
		[KeptMember (".ctor()")]
		[KeptSecurity (typeof (SecurityPermissionAttribute))]
		class Foo {
		}
	}
}
