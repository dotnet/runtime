using System;
using System.Reflection.Emit;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	public class ComplexConditions
	{
		public static void Main()
		{
			Test_1 (null);
		}

		[Kept]
		[ExpectBodyModified]
		static void Test_1 (object type)
		{
			if (type is Type || (IsDynamicCodeSupported && type is TypeBuilder))
				Reached_1 ();
		}

		[Kept]
		static bool IsDynamicCodeSupported {
			[Kept]
			get {
				return true;
			}
		}

		[Kept]
		static void Reached_1 ()
		{			
		}
	}
}