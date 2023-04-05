using System;

namespace Mono.Linker.Tests.Cases.Basic
{
	class UnusedDelegateGetsRemoved
	{
		static void Main ()
		{
		}

		public delegate void Foo ();
	}
}
