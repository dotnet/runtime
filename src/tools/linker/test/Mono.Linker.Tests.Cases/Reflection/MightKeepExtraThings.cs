using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {
	public class MightKeepExtraThings {
		public static void Main ()
		{
			var typeA = typeof (A);
			var typeB = typeof (B);
			Console.WriteLine (typeB); // Use typeB so the C# compiler keeps it in the IL code.
			var method = typeA.GetMethod ("Foo", BindingFlags.Public);
			method.Invoke (null, new object[] { });
		}

		[Kept]
		public class A {
			[Kept]
			public int Foo ()
			{
				return 42;
			}
		}

		[Kept]
		public class B {
			[Kept]
			public int Foo ()
			{
				return 43;
			}
		}
	}
}
