using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics
{
	[ExpectedNoWarnings]
	public class NewConstraintOnClass
	{
		public static void Main ()
		{
			NewConstraint.Test ();
			StructConstraint.Test ();
			UnmanagedConstraint.Test ();
		}

		[Kept]
		class NewConstraint
		{
			class TestClass
			{
				static readonly int field = 1;

				[Kept]
				public TestClass ()
				{
				}

				public TestClass (int a)
				{
				}

				public void Foo ()
				{
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			class WithConstraint<
				[KeptGenericParamAttributes (GenericParameterAttributes.DefaultConstructorConstraint)]
				T
			> where T : new()
			{
			}

			[Kept]
			public static void Test ()
			{
				var a = new WithConstraint<TestClass> ();
			}
		}

		[Kept]
		class StructConstraint
		{
			struct TestStruct
			{
				static readonly int field = 1;

				[Kept]
				public TestStruct ()
				{
				}

				public TestStruct (int a)
				{
				}

				public void Foo ()
				{
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			class WithConstraint<
				[KeptGenericParamAttributes (GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint)]
				T
			> where T : struct
			{
			}

			[Kept]
			public static void Test ()
			{
				var a = new WithConstraint<TestStruct> ();
			}
		}

		[Kept]
		class UnmanagedConstraint
		{
			struct TestStruct
			{
				static readonly int field = 1;

				[Kept]
				public TestStruct ()
				{
				}

				public TestStruct (int a)
				{
				}

				public void Foo ()
				{
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			class WithConstraint<
				[KeptAttributeAttribute (typeof (IsUnmanagedAttribute))]
				[KeptGenericParamAttributes (GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint)]
				T
			> where T : unmanaged
			{
			}

			[Kept]
			public static void Test ()
			{
				var a = new WithConstraint<TestStruct> ();
			}
		}
	}
}

namespace System.Runtime.CompilerServices
{
	// NativeAOT test infra filters out System.* members from validation for now
	[Kept (By = Tool.Trimmer)]
	[KeptMember (".ctor()", By = Tool.Trimmer)]
	public partial class IsUnmanagedAttribute
	{
	}
}
