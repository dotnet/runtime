using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class UsedEnumIsKept
	{
		static void Main ()
		{
			// Use all of the values in case we implement a feature in the future that removes unused values
			HelperAsEnum (Used.One);
			HelperAsEnum (Used.Two);
			HelperAsObject (Used.Three);
		}

		[Kept]
		static void HelperAsEnum (Used arg)
		{
		}

		[Kept]
		static void HelperAsObject (object arg)
		{
		}

		[Kept]
		[KeptMember ("value__")]
		[KeptBaseType (typeof (Enum))]
		enum Used
		{
			[Kept]
			One,

			[Kept]
			Two,

			[Kept]
			Three
		}
	}
}
