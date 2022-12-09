using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// Actions:
	// link - Forwarder.dll and Implementation.dll
	// copy - this (test.dll) assembly

	[SetupLinkerDefaultAction ("link")]
	[SetupLinkerAction ("copy", "test")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

	[KeptTypeInAssembly ("Forwarder.dll", typeof (ImplementationLibrary))]
	[KeptTypeInAssembly ("Implementation.dll", typeof (ImplementationLibrary))]
	static class AttributesScopeUpdated
	{
		static void Main ()
		{
		}

		[Kept]
		public static void Test_1 ([KeptAttributeAttribute (typeof (TestType3Attribute))][TestType3 (typeof (ImplementationLibrary))] int arg)
		{
		}

		[Kept]
		public static void Test_2<[KeptAttributeAttribute (typeof (TestType3Attribute))][TestType3 (typeof (ImplementationLibrary))] T> ()
		{
		}

		[Kept]
		[return: KeptAttributeAttribute (typeof (TestType3Attribute))]
		[return: TestType3 (typeof (ImplementationLibrary))]
		public static void Test_3 ()
		{
		}
	}

	[KeptBaseType (typeof (Attribute))]
	public class TestType3Attribute : Attribute
	{
		[Kept]
		public TestType3Attribute (Type type)
		{
		}
	}
}
