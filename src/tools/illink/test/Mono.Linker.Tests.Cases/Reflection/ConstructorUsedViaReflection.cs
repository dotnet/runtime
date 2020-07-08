using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	public class ConstructorUsedViaReflection
	{
		public static void Main ()
		{
			TestWithIntegerParameter ();
			TestWithBindingFlags ();
			TestWithCallingConvention ();
			TestNullType ();
			TestDataFlowType ();
			TestIfElse (true);
		}

		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (Type[]) },
			typeof (IntegerParameterConstructor), ".ctor", new Type[0])]
		[Kept]
		static void TestWithIntegerParameter ()
		{
			var constructor = typeof (IntegerParameterConstructor).GetConstructor (new Type[] { typeof (int) });
			constructor.Invoke (null, new object[] { });
		}

		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (BindingFlags), typeof (Binder), typeof (Type[]), typeof (ParameterModifier[]) },
			typeof (OnlyUsedViaReflection), ".ctor", new Type[0])]
		[Kept]
		static void TestWithBindingFlags ()
		{
			var constructor = typeof (OnlyUsedViaReflection).GetConstructor (BindingFlags.Public, GetNullValue ("some argument", 2, 3), new Type[] { }, new ParameterModifier[] { });
			constructor.Invoke (null, new object[] { });
		}

		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (BindingFlags), typeof (Binder), typeof (CallingConventions), typeof (Type[]), typeof (ParameterModifier[]) },
			typeof (CallingConventionConstructor), ".ctor", new Type[0])]
		[Kept]
		static void TestWithCallingConvention ()
		{
			var constructor = typeof (CallingConventionConstructor).GetConstructor (BindingFlags.Public, GetNullValue ("some argument", 2, 3), CallingConventions.HasThis, new Type[] { }, new ParameterModifier[] { });
			constructor.Invoke (null, new object[] { });
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var constructor = type.GetConstructor (new Type[] { });
		}

		[Kept]
		static Type FindType ()
		{
			return null;
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (Type[]) },
			"The return value of method 'Mono.Linker.Tests.Cases.Reflection.ConstructorUsedViaReflection.FindType()' with dynamically accessed member kinds 'None' " +
			"is passed into the implicit 'this' parameter of method 'System.Type.GetConstructor(Type[])' which requires dynamically accessed member kinds 'PublicParameterlessConstructor'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicParameterlessConstructor'.")]
		[Kept]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var constructor = type.GetConstructor (new Type[] { });
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (BindingFlags), typeof (Binder), typeof (Type[]), typeof (ParameterModifier[]) },
			typeof (IfConstructor), ".ctor", new Type[0])]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (BindingFlags), typeof (Binder), typeof (Type[]), typeof (ParameterModifier[]) },
			typeof (ElseConstructor), ".ctor", new Type[0])]
		static void TestIfElse (bool decision)
		{
			Type myType;
			if (decision) {
				myType = typeof (IfConstructor);
			} else {
				myType = typeof (ElseConstructor);
			}
			var constructor = myType.GetConstructor (BindingFlags.Public, null, new Type[] { }, null);
			constructor.Invoke (null, new object[] { });
		}

		[Kept]
		static Binder GetNullValue (string str, int i, long g)
		{
			return null;
		}

		[Kept]
		private class IntegerParameterConstructor
		{
			[Kept]
			public IntegerParameterConstructor ()
			{ }

			[Kept]
			public IntegerParameterConstructor (int i)
			{ }

			private IntegerParameterConstructor (string foo)
			{ }

			protected IntegerParameterConstructor (string foo, string bar)
			{ }
		}

		[Kept]
		private class OnlyUsedViaReflection
		{
			[Kept]
			public OnlyUsedViaReflection ()
			{ }

			[Kept]
			public OnlyUsedViaReflection (string bar)
			{ }

			private OnlyUsedViaReflection (int foo)
			{ }

			protected OnlyUsedViaReflection (int foo, int bar)
			{ }

			internal OnlyUsedViaReflection (int foo, int bar, int baz)
			{ }
		}

		[Kept]
		private class CallingConventionConstructor
		{
			[Kept]
			public CallingConventionConstructor ()
			{ }

			[Kept]
			public CallingConventionConstructor (string bar)
			{ }

			private CallingConventionConstructor (int foo)
			{ }

			protected CallingConventionConstructor (int foo, int bar)
			{ }

			internal CallingConventionConstructor (int foo, int bar, int baz)
			{ }
		}

		[Kept]
		private class IfConstructor
		{
			[Kept]
			public IfConstructor ()
			{ }

			[Kept]
			public IfConstructor (int foo)
			{ }

			private IfConstructor (string foo)
			{ }

			protected IfConstructor (int foo, int bar)
			{ }

			internal IfConstructor (int foo, string bar)
			{ }
		}

		[Kept]
		private class ElseConstructor
		{
			[Kept]
			public ElseConstructor ()
			{ }

			[Kept]
			public ElseConstructor (int foo)
			{ }

			private ElseConstructor (string foo)
			{ }

			protected ElseConstructor (int foo, int bar)
			{ }

			internal ElseConstructor (int foo, string bar)
			{ }
		}
	}
}