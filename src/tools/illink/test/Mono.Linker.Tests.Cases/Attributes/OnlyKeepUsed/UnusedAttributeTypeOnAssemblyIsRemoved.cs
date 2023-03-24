using System;
using Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: UnusedAttributeTypeOnAssemblyIsRemoved.Foo]

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	class UnusedAttributeTypeOnAssemblyIsRemoved
	{
		static void Main ()
		{
		}

		public class FooAttribute : Attribute
		{
		}
	}
}
