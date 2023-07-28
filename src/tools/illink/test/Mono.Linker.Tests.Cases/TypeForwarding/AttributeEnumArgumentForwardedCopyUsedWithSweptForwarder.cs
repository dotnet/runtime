using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	[SetupLinkerAction ("copyused", "Attribute")]
	[SetupLinkerAction ("copyused", "Forwarder")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/MyEnum.cs" })]
	[SetupCompileBefore ("Attribute.dll", new[] { "Dependencies/AttributeWithEnumArgument.cs" },
		defines: new[] { "INCLUDE_FORWARDER" }, references: new[] { "Forwarder.dll" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/MyEnum.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/MyEnumForwarder.cs" }, references: new[] { "Implementation.dll" })]

	[KeptTypeInAssembly ("Forwarder.dll", typeof (UsedToReferenceForwarderAssembly))]
	[KeptTypeInAssembly ("Implementation.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.MyEnum")]
	[RemovedForwarder ("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.MyEnum")]
	class AttributeEnumArgumentForwardedCopyUsedWithSweptForwarder
	{
		static void Main ()
		{
			// attribute enum arg typeref -> forwarder -> enum def
			// copyused                      copyused

			// The forwarder is not referenced and will be removed. This is a regression test for a case where the copyused typeref
			// scope is not updated, causing an exception to be thrown when the assembly is output.

			// Mark the attribute enum argument, causing cecil to read the attribute blob and resolve the typeref through the forwarder.
			var _ = typeof (AttributedType);

			// The attribute assembly needs to be written back with cecil for this to cause problems.
			// To prevent its action from being changed to "copy", it has an unused type forwarder which gets swept.

			// Reference the forwarder assembly (but without marking the forwarder) to prevent it from being removed entirely.
			// This ensures that:
			// - The forwarder assembly is swept and the enum forwarder is removed (invalidating the attribute argument's typeref)
			// - The attribute assembly doesn't reference a removed assembly.
			//   Referencing a removed assembly would change its action to "save" and it would get typerefs updated (correctly), avoiding the bug
			//   this is trying to reproduce.
			var _2 = typeof (UsedToReferenceForwarderAssembly);
		}
	}
}
