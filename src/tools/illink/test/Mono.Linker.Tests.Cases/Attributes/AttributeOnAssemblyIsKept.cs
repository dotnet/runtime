using System;
using Mono.Linker.Tests.Cases.Attributes;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

[assembly: AttributeOnAssemblyIsKept.Foo]
[assembly: KeptAttributeAttribute (typeof (AttributeOnAssemblyIsKept.FooAttribute))]

namespace Mono.Linker.Tests.Cases.Attributes
{
	class AttributeOnAssemblyIsKept
	{
		static void Main ()
		{
		}

		[KeptBaseType (typeof (System.Attribute))]
		public class FooAttribute : Attribute
		{
			[Kept]
			public FooAttribute ()
			{
			}
		}
	}
}
