using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class ConstructorUsedViaReflection {
		public static void Main ()
		{
			var constructor = typeof (OnlyUsedViaReflection).GetConstructor (BindingFlags.Public, GetNullValue ("some argument", 2, 3), new Type[]{}, new ParameterModifier[]{});
			constructor.Invoke (null, new object[] { });
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
