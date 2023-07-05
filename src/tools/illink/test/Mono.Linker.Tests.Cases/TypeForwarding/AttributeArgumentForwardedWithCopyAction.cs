using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// On .NET FW the built in compiler will drop the reference to the forwarder assembly when compiling the test assembly.
	// Use roslyn to give consistent behavior across platforms
	[SetupCSharpCompilerToUse ("csc")]

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
	static class AttributeArgumentForwardedWithCopyAction
	{
		static void Main ()
		{
			Test_1 ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestType2Attribute))]
		[TestType2 (typeof (ImplementationLibrary))]
		public static void Test_1 ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestType2Attribute))]
		[TestType2 (typeof (ImplementationLibrary[,][]))]
		public static void Test_1b ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestType2Attribute))]
		[TestType2 (typeof (ImplementationLibrary[,][]))]
		public static void Test_1c ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestType2Attribute))]
		[TestType2 (TestProperty = new object[] { typeof (ImplementationLibrary) })]
		public static void Test_2 ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestType2Attribute))]
		[TestType2 (TestField = typeof (SomeGenericType2<ImplementationLibrary[]>))]
		public static void Test_3 ()
		{
		}


		[Kept]
		[KeptAttributeAttribute (typeof (TestType2Attribute))]
		[TestType2 (TestField = typeof (SomeGenericType2<>))]
		public static void Test_3a ()
		{
		}
	}

	[KeptBaseType (typeof (Attribute))]
	public class TestType2Attribute : Attribute
	{
		[Kept]
		public TestType2Attribute ()
		{
		}

		[Kept]
		public TestType2Attribute (Type arg)
		{
		}

		[KeptBackingField]
		[Kept]
		public object TestProperty { [Kept] get; [Kept] set; }

		[Kept]
		public object TestField;
	}

	[Kept]
	static class SomeGenericType2<T>
	{
	}
}
