using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Statics
{
	public class UnusedStaticMethodGetsRemoved
	{
		public static void Main ()
		{
			A.UsedMethod ();
		}
	}

	[Kept]
	class A
	{
		[Kept]
		public static void UsedMethod ()
		{
		}

		static void UnusedMethod ()
		{
		}
	}
}