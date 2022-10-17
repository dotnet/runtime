using System;
using Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AttributeDefinedAndUsedInOtherAssemblyIsKept_Lib.cs" })]
	[KeptMemberInAssembly ("library.dll", typeof (AttributeDefinedAndUsedInOtherAssemblyIsKept_Lib.FooAttribute), ".ctor()")]
	class AttributeDefinedAndUsedInOtherAssemblyIsKept
	{
		static void Main ()
		{
			Method ();
		}

		[AttributeDefinedAndUsedInOtherAssemblyIsKept_Lib.Foo]
		[Kept]
		[KeptAttributeAttribute (typeof (AttributeDefinedAndUsedInOtherAssemblyIsKept_Lib.FooAttribute))]
		static void Method ()
		{
			AttributeDefinedAndUsedInOtherAssemblyIsKept_Lib.UseTheAttributeType ();
		}
	}
}
