using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	class UnusedDerivedAttributeType
	{
		static void Main ()
		{
			var tmp = new Bar ();
			var str = typeof (BaseAttribute).ToString ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[Derived]
		class Bar
		{
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class BaseAttribute : Attribute
		{
		}

		// The derived attribute is removed even if it used on a type (Bar)
		// and the base class is directly referenced from method body (Main).
		class DerivedAttribute : BaseAttribute
		{
		}
	}
}
