using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System.Runtime.CompilerServices;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	public class RunClassConstructorUsedViaReflection
	{
		public static void Main ()
		{
			TestRunClassConstructor ();
			TestNonKeptStaticConstructor ();
			TestNull ();
			TestDataFlowType ();
			TestIfElseUsingRuntimeTypeHandle (1);
			TestIfElseUsingType (1);
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestRunClassConstructor ()
		{
			RuntimeHelpers.RunClassConstructor (typeof (OnlyUsedViaReflection).TypeHandle);
		}

		[Kept]
		static void TestNonKeptStaticConstructor ()
		{
			var a = new NonKeptStaticConstructorClass ();
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestNull ()
		{
			Type type = null;
			RuntimeHelpers.RunClassConstructor (type.TypeHandle);
		}

		[Kept]
		static Type FindType ()
		{
			return null;
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (RuntimeHelpers), nameof (RuntimeHelpers.RunClassConstructor), new Type[] { typeof (RuntimeTypeHandle) },
			"A value from unknown source is passed into the parameter 'type' of method " +
			"'System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(RuntimeTypeHandle)'. " +
			"It's not possible to guarantee availability of the target static constructor.")]

		static void TestDataFlowType ()
		{
			Type type = FindType ();
			RuntimeHelpers.RunClassConstructor (type.TypeHandle);
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		[UnrecognizedReflectionAccessPattern (typeof (RuntimeHelpers), nameof (RuntimeHelpers.RunClassConstructor), new Type[] { typeof (RuntimeTypeHandle) },
			"A value from unknown source is passed into the parameter 'type' of method " +
			"'System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(RuntimeTypeHandle)'. " +
			"It's not possible to guarantee availability of the target static constructor.")]

		static void TestIfElseUsingRuntimeTypeHandle (int i)
		{
			RuntimeTypeHandle myType;
			if (i == 1) {
				myType = typeof (IfClass).TypeHandle;
			} else if (i == 2) {
				myType = FindType ().TypeHandle;
			} else {
				myType = typeof (ElseClass).TypeHandle;
			}
			RuntimeHelpers.RunClassConstructor (myType);
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestIfElseUsingType (int i)
		{
			Type myType;
			if (i == 1) {
				myType = typeof (IfClass2);
			} else if (i == 2) {
				myType = null;
			} else {
				myType = typeof (ElseClass2);
			}
			RuntimeHelpers.RunClassConstructor (myType.TypeHandle);
		}

		[Kept]
		[KeptMember (".cctor()")]
		class OnlyUsedViaReflection
		{
			[Kept]
			static int i = 5;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class NonKeptStaticConstructorClass
		{
			static int i = 5;
		}

		[Kept]
		[KeptMember (".cctor()")]
		class IfClass
		{
			public IfClass ()
			{ }
			private IfClass (int foo)
			{ }
		}

		[Kept]
		[KeptMember (".cctor()")]
		class ElseClass
		{
			[Kept]
			static ElseClass ()
			{ }
			public ElseClass (int foo)
			{ }
		}
		[Kept]
		[KeptMember (".cctor()")]
		class IfClass2
		{
			public IfClass2 ()
			{ }
			private IfClass2 (int foo)
			{ }
		}

		[Kept]
		[KeptMember (".cctor()")]
		class ElseClass2
		{
			[Kept]
			static ElseClass2 ()
			{ }
			public ElseClass2 (int foo)
			{ }
		}
	}
}