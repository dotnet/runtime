using System;

namespace Mono.Linker.Tests.Cases.Basic
{
	class UnusedEnumGetsRemoved
	{
		static void Main ()
		{
		}

		enum Unused
		{
			One,
			Two,
			Three
		}
	}
}
