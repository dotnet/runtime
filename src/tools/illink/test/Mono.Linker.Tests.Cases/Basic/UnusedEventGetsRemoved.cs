using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class UnusedEventGetsRemoved
	{
		public static void Main ()
		{
			var tmp = new Foo ();
		}

		[KeptMember (".ctor()")]
		public class Foo
		{
			public delegate void CustomDelegate ();

			public event CustomDelegate Bar;
		}
	}
}
