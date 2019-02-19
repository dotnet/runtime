using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class ConstructorUsedViaReflection {
		public static void Main ()
		{
			var constructor = typeof (OnlyUsedViaReflection).GetConstructor (BindingFlags.Public, null, new Type[]{}, new ParameterModifier[]{});
			constructor.Invoke (null, new object[] { });
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
