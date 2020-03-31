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
					   typeof (Type), nameof (Type.GetConstructor), new Type [] { typeof (Type []) },
					   typeof (IntegerParameterConstructor), ".ctor", new Type [0])]
		[Kept]
		static void TestWithIntegerParameter ()
		{
			var constructor = typeof (IntegerParameterConstructor).GetConstructor (new Type [] { typeof (int) });
			constructor.Invoke (null, new object [] { });
		}

		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetConstructor), new Type [] { typeof (BindingFlags), typeof (Binder), typeof (Type []), typeof (ParameterModifier []) },
			typeof (OnlyUsedViaReflection), ".ctor", new Type [0])]
		[Kept]
		static void TestWithBindingFlags ()
		{
			var constructor = typeof (OnlyUsedViaReflection).GetConstructor (BindingFlags.Public, GetNullValue ("some argument", 2, 3), new Type [] { }, new ParameterModifier [] { });
			constructor.Invoke (null, new object [] { });
		}

		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetConstructor), new Type [] { typeof (BindingFlags), typeof (Binder), typeof (CallingConventions), typeof (Type []), typeof (ParameterModifier []) },
			typeof (CallingConventionConstructor), ".ctor", new Type [0])]
		[Kept]
		static void TestWithCallingConvention ()
		{
			var constructor = typeof (CallingConventionConstructor).GetConstructor (BindingFlags.Public, GetNullValue ("some argument", 2, 3), CallingConventions.HasThis, new Type [] { }, new ParameterModifier [] { });
			constructor.Invoke (null, new object [] { });
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var constructor = type.GetConstructor (new Type [] { });
		}

		[Kept]
		static Type FindType ()
		{
			return null;
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetConstructor), new Type [] { typeof (Type []) })]
		[Kept]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var constructor = type.GetConstructor (new Type [] { });
		}

		[Kept]
		static void TestIfElse (bool decision)
		{
			if (decision) {
				var constructor = typeof (IfElseConstructor).GetConstructor (BindingFlags.Public, GetNullValue ("some argument", 2, 3), new Type [] { }, new ParameterModifier [] { });
				constructor.Invoke (null, new object [] { });
			} else {
				var constructor = typeof (IfElseConstructor).GetConstructor (BindingFlags.NonPublic, GetNullValue ("some argument", 2, 3), new Type [] { }, new ParameterModifier [] { });
				constructor.Invoke (null, new object [] { });
			}
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

			[Kept]
			private IntegerParameterConstructor (string foo)
			{ }

			[Kept]
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
		private class IfElseConstructor
		{
			[Kept]
			public IfElseConstructor ()
			{ }

			[Kept]
			public IfElseConstructor (int foo)
			{ }

			[Kept]
			private IfElseConstructor (string foo)
			{ }

			[Kept]
			protected IfElseConstructor (int foo, int bar)
			{ }

			[Kept]
			internal IfElseConstructor (int foo, string bar)
			{ }
		}
	}
}