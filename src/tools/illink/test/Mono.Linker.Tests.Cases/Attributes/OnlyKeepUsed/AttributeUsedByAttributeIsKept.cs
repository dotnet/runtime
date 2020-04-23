using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	class AttributeUsedByAttributeIsKept
	{
		static void Main ()
		{
			var jar = new Jar ();
			jar.SomeMethod ();
		}

		[Foo]
		[Bar]
		[Kar]
		[NotUsed]
		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (FooAttribute))]
		[KeptAttributeAttribute (typeof (BarAttribute))]
		[KeptAttributeAttribute (typeof (KarAttribute))]
		class Jar
		{
			[Kept]
			public void SomeMethod ()
			{
				var attr = typeof (Jar).GetCustomAttributes (typeof (FooAttribute), false)[0];
				var asFooAttr = (FooAttribute) attr;
				asFooAttr.MethodWeWillCall ();
			}
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class FooAttribute : Attribute
		{
			[Kept]
			public FooAttribute ()
			{
				// This ctor should be marked lazy.  So let's use another attribute type here which should then trigger that attribute
				// to be marked
				var str = typeof (BarAttribute).ToString ();
				MethodsUsedFromLateMarking.Method1 ();
			}

			[Foo]
			[Bar]
			[Kar]
			[NotUsed]
			[Kept]
			[KeptAttributeAttribute (typeof (FooAttribute))]
			[KeptAttributeAttribute (typeof (BarAttribute))]
			[KeptAttributeAttribute (typeof (KarAttribute))]
			public void MethodWeWillCall ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class BarAttribute : Attribute
		{
			[Kept]
			public BarAttribute ()
			{
				// Let's do this one more time to make sure we catch everything
				var str = typeof (KarAttribute).ToString ();
				MethodsUsedFromLateMarking.Method2 ();
			}
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KarAttribute : Attribute
		{
			[Kept]
			public KarAttribute ()
			{
				MethodsUsedFromLateMarking.Method3 ();
			}
		}

		class NotUsedAttribute : Attribute
		{
		}

		[Kept]
		static class MethodsUsedFromLateMarking
		{
			[Kept]
			public static void Method1 ()
			{
			}

			[Kept]
			public static void Method2 ()
			{
			}

			[Kept]
			public static void Method3 ()
			{
			}
		}

	}
}
