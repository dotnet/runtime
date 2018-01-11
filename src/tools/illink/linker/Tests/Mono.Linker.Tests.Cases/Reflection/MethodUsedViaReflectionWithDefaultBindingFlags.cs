using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {
	public class MethodUsedViaReflectionWithDefaultBindingFlags {
		public static void Main ()
		{
			var method = typeof (MethodUsedViaReflectionWithDefaultBindingFlags).GetMethod ("OnlyCalledViaReflection");
			method.Invoke (null, new object[] { });
		}

		[Kept]
		private static int OnlyCalledViaReflection ()
		{
			return 42;
		}

		[Kept]
		private int OnlyCalledViaReflection (int foo)
		{
			return 43;
		}

		[Kept]
		public int OnlyCalledViaReflection (int foo, int bar)
		{
			return 44;
		}

		[Kept]
		public static int OnlyCalledViaReflection (int foo, int bar, int baz)
		{
			return 45;
		}
	}
}
