using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class MethodUsedViaReflectionAndLocal
	{
		public static void Main ()
		{
			new A (); // Needed to avoid lazy body marking stubbing
			new B (); // Needed to avoid lazy body marking stubbing

			var typeA = typeof (A);
			var typeB = typeof (B);
			Console.WriteLine (typeB); // Use typeB so the C# compiler keeps it in the IL code.
			var method = typeA.GetMethod ("Foo", BindingFlags.Public);
			method.Invoke (null, new object[] { });
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class A
		{
			[Kept]
			public int Foo ()
			{
				return 42;
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class B
		{
			public int Foo ()
			{
				return 43;
			}
		}
	}
}
