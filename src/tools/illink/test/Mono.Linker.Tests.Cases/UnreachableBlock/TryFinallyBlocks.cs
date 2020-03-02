using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupLinkerArgument ("--enable-opt", "ipconstantpropagation")]
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