using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class UsedViaReflectionIntegrationTest
	{
		public static void Main ()
		{
			var test = 42;

			var constructor = typeof (OnlyUsedViaReflection).GetConstructor (BindingFlags.Public, null, new Type[] { }, new ParameterModifier[] { });
			constructor.Invoke (null, new object[] { });

			if (test == 42) {
				var method = typeof (OnlyUsedViaReflection).GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.NonPublic);
				method.Invoke (null, new object[] { });
			}
		}

		[Kept]
		private class OnlyUsedViaReflection
		{
			[Kept]
			public OnlyUsedViaReflection ()
			{ }

			public OnlyUsedViaReflection (string bar)
			{ }

			private OnlyUsedViaReflection (int foo)
			{ }

			protected OnlyUsedViaReflection (int foo, int bar)
			{ }

			internal OnlyUsedViaReflection (int foo, int bar, int baz)
			{ }

			[Kept]
			private static int OnlyCalledViaReflection ()
			{
				return 42;
			}

			private int OnlyCalledViaReflection (int foo)
			{
				return 43;
			}

			public int OnlyCalledViaReflection (int foo, int bar)
			{
				return 44;
			}

			public static int OnlyCalledViaReflection (int foo, int bar, int baz)
			{
				return 45;
			}
		}
	}
}
