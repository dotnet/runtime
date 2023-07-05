using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// Actions:
	// link - This assembly, Forwarder.dll and Implementation.dll
	[SetupLinkerDefaultAction ("link")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

	[RemovedAssembly ("Forwarder.dll")]
	[KeptTypeInAssembly ("Implementation.dll", typeof (ImplementationLibrary))]
	[KeptTypeInAssembly ("Implementation.dll", typeof (ImplementationStruct))]
	class AttributeArgumentForwarded
	{
		static void Main ()
		{
			Test_Parameter_TypeRef ();
			Test_Parameter_TypeRefMDArray ();
			Test_Parameter_PointerTypeRef ();
			Test_Property_ArrayOfTypeRefs ();
			Test_Field_GenericOfTypeRefArray ();
			Test_Field_OpenGeneric ();
			Test_Field_ArrayOfPointerTypeRef ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (typeof (ImplementationLibrary))]
		public static void Test_Parameter_TypeRef ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (typeof (ImplementationLibrary[,][]))]
		public static void Test_Parameter_TypeRefMDArray ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (typeof (ImplementationStruct*))]
		public static void Test_Parameter_PointerTypeRef ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (TestProperty = new object[] { typeof (ImplementationLibrary) })]
		public static void Test_Property_ArrayOfTypeRefs ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (TestField = typeof (SomeGenericType<ImplementationLibrary[]>))]
		public static void Test_Field_GenericOfTypeRefArray ()
		{
		}


		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (TestField = typeof (SomeGenericType<>))]
		public static void Test_Field_OpenGeneric ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		[TestType (TestField = new object[] { typeof (ImplementationStruct*) })]
		public static void Test_Field_ArrayOfPointerTypeRef ()
		{
		}

		// This hits Roslyn bug https://github.com/dotnet/roslyn/issues/48765
		//[Kept]
		//[KeptAttributeAttribute (typeof (TestTypeAttribute))]
		//[TestType (TestField = new object[] { typeof (delegate*<int, void>) })]
		//public static void Test_Field_ArrayOfFunctionPointer ()
		//{
		//}
	}

	[KeptBaseType (typeof (Attribute))]
	public class TestTypeAttribute : Attribute
	{
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
	public class SomeGenericType<T>
	{
	}
}
