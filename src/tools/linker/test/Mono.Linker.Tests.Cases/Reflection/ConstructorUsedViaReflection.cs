using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class ConstructorUsedViaReflection {
		public static void Main ()
		{
			TestWithBindingFlags ();
			TestNullType ();
			TestDataFlowType ();
		}

		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetConstructor), new Type [] { typeof (BindingFlags), typeof (Binder), typeof (Type[]), typeof (ParameterModifier []) },
			typeof (OnlyUsedViaReflection), ".ctor", new Type [0])]
		[Kept]
		static void TestWithBindingFlags ()
		{
			var constructor = typeof (OnlyUsedViaReflection).GetConstructor (BindingFlags.Public, GetNullValue ("some argument", 2, 3), new Type [] { }, new ParameterModifier [] { });
			constructor.Invoke (null, new object [] { });
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetConstructor), new Type [] { typeof (Type []) })]
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
		static Binder GetNullValue (string str, int i, long g)
		{
			return null;
		}

		[Kept]
		private class OnlyUsedViaReflection {
			[Kept]
			public OnlyUsedViaReflection ()
			{ }

			[Kept]
			public OnlyUsedViaReflection(string bar)
			{ }

			private OnlyUsedViaReflection (int foo)
			{ }

			protected OnlyUsedViaReflection(int foo, int bar)
			{ }

			internal OnlyUsedViaReflection(int foo, int bar, int baz)
			{ }
		}
	}
}
