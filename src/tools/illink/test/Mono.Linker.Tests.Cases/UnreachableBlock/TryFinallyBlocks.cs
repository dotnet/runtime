using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	public class TryFinallyBlocks
	{
		public static void Main ()
		{
			TestSimpleTry ();
		}

		[Kept]
		static void TestSimpleTry ()
		{
			if (Prop != 3)
				Reached_1 ();
		}

		[Kept]
		static int Prop {
			[Kept]
			get {
				try {
					return 3;
				} finally {

				}
			}
		}

		[Kept]
		static void Reached_1 ()
		{
		}
	}
}