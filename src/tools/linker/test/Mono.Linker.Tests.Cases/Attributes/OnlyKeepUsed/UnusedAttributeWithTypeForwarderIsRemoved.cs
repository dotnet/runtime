using System;
using Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/UnusedAttributeWithTypeForwarderIsRemoved_Lib.cs" })]
	[SetupCompileAfter ("implementation.dll", new[] { "Dependencies/UnusedAttributeWithTypeForwarderIsRemoved_Lib.cs" })]
	[SetupCompileAfter ("library.dll", new[] { "Dependencies/UnusedAttributeWithTypeForwarderIsRemoved_Forwarder.cs" }, new[] { "implementation.dll" })]

	[RemovedAssembly ("library.dll")]
	[RemovedTypeInAssembly ("implementation.dll", typeof (UnusedAttributeWithTypeForwarderIsRemoved_LibAttribute))]
	class UnusedAttributeWithTypeForwarderIsRemoved
	{
		static void Main ()
		{
			Method (null);
		}

		[Kept]

		static void Method ([UnusedAttributeWithTypeForwarderIsRemoved_Lib ("")] string arg)
		{
			UnusedAttributeWithTypeForwarderIsRemoved_OtherUsedClass.UsedMethod ();
		}
	}
}
