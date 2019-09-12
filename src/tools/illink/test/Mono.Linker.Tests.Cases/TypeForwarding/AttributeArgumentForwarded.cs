using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// Actions:
	// link - This assembly, Forwarder.dll and Implementation.dll
	[SetupLinkerUserAction ("link")]
	[KeepTypeForwarderOnlyAssemblies ("false")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

	[RemovedAssembly ("Forwarder.dll")]
	[KeptMemberInAssembly ("Implementation.dll", typeof (ImplementationLibrary))]
	class AttributeArgumentForwarded
	{
		static void Main()
		{
			Test_1 ();
			Test_1b ();
			Test_1c ();
			Test_2 ();
			Test_3 ();
			Test_3a ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (typeof (ImplementationLibrary))]
		public static void Test_1 ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (typeof (ImplementationLibrary[,][]))]
		public static void Test_1b ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (typeof (ImplementationLibrary [,] []))]
		public static void Test_1c ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (TestProperty = new object [] { typeof (ImplementationLibrary) })]
		public static void Test_2 ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (TestField = typeof (SomeGenericType<ImplementationLibrary[]>))]
		public static void Test_3 ()
		{
		}


		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (TestField = typeof (SomeGenericType<>))]
		public static void Test_3a ()
		{
		}
	}

	[KeptBaseType (typeof (Attribute))]
	public class TestTypeAttribute : Attribute {
		[Kept]
		public TestTypeAttribute ()
		{
		}

		[Kept]
		public TestTypeAttribute (Type arg)
		{
		}

		[KeptBackingField]
		[Kept]
		public object TestProperty { get; [Kept] set; }

		[Kept]
		public object TestField;
	}

	[Kept]
	public class SomeGenericType<T> {
	}
}
