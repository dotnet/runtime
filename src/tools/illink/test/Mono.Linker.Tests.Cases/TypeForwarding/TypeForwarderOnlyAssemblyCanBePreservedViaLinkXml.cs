using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]
	// Add another assembly in that uses the forwarder just to make things a little more complex
	[SetupCompileBefore ("Library.dll", new[] { "Dependencies/LibraryUsingForwarder.cs" }, references: new[] { "Forwarder.dll" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

	[KeptAssembly ("Forwarder.dll")]
	[KeptMemberInAssembly ("Implementation.dll", typeof (ImplementationLibrary), "GetSomeValue()")]
	[KeptMemberInAssembly ("Library.dll", typeof (LibraryUsingForwarder), "GetValueFromOtherAssembly()")]

	[KeptTypeInAssembly ("Forwarder.dll", typeof (ImplementationLibrary))]
	[SetupLinkerDescriptorFile ("TypeForwarderOnlyAssemblyCanBePreservedViaLinkXml.xml")]
	public class TypeForwarderOnlyAssemblyCanBePreservedViaLinkXml
	{
		static void Main ()
		{
			Console.WriteLine (new ImplementationLibrary ().GetSomeValue ());
			Console.WriteLine (new LibraryUsingForwarder ().GetValueFromOtherAssembly ());
		}
	}
}