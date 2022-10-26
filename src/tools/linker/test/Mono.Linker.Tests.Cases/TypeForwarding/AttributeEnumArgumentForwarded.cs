using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// Actions:
	// link - This assembly, Forwarder.dll and Implementation.dll
	[SetupLinkerDefaultAction ("link")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/MyEnum.cs" })]
	[SetupCompileBefore ("Attribute.dll", new[] { "Dependencies/AttributeWithEnumArgument.cs" }, references: new[] { "Forwarder.dll" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/MyEnum.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/MyEnumForwarder.cs" }, references: new[] { "Implementation.dll" })]

	[KeptTypeInAssembly ("Forwarder.dll", typeof (UsedToReferenceForwarderAssembly))]
	[KeptTypeInAssembly ("Implementation.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.MyEnum")]
	[RemovedForwarder ("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.MyEnum")]
	class AttributeEnumArgumentForwarded
	{
		static void Main ()
		{
			// For the issue to repro, the forwarder assembly must be processed by SweepStep before
			// the attribute. Referencing it first in the test does this, even though it's not really
			// a guarantee, since the assembly action dictionary doesn't guarantee order.
			var _ = typeof (UsedToReferenceForwarderAssembly);
			var _2 = typeof (AttributedType);
		}
	}
}
