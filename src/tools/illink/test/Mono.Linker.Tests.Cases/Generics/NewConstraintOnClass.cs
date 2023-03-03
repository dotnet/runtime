using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics
{
	public class NewConstraintOnClass
	{
		public static void Main ()
		{
			var g1 = new G1<G1Ctor> ();
		}

		class G1Ctor
		{
			static readonly int field = 1;

			[Kept]
			public G1Ctor ()
			{
			}

			public G1Ctor (int a)
			{
			}

			public void Foo ()
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		class G1<T> where T : G1Ctor, new()
		{
		}
	}
}
