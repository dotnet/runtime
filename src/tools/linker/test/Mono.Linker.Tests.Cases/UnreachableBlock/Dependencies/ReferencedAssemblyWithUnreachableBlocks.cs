using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies
{
	public class AssemblyWithUnreachableBlocks
	{
		public AssemblyWithUnreachableBlocks ()
		{
			TestProperty ();
		}

		static void TestProperty ()
		{
			if (PropBool)
				NeverReached ();
		}

		static void NeverReached () { }

		static bool PropBool {
			get => false;
		}
	}
}