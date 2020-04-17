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
			TestIfElse (1);
		}

		[Kept]
		static void TestRunClassConstructor ()
		{
			RuntimeHelpers.RunClassConstructor (typeof (OnlyUsedViaReflection).TypeHandle);
		}

		[Kept]
		static void TestNonKeptStaticConstructor ()
		{
			var a = new NonKeptStaticConstructorClass();
		}

		[Kept]
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
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			RuntimeHelpers.RunClassConstructor (type.TypeHandle);
		}

		[Kept]
		static void TestIfElse (int i)
		{
			Type myType;
			if (i == 1) {
				myType = typeof (IfClass);
			} else {
				myType = typeof (ElseClass);
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
	}
}