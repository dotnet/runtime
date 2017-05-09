using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink {
	[CoreLink ("link")]
	[KeptAssembly ("mscorlib.dll")]
	// We can't check everything that should be removed, but we should be able to check a few niche things that
	// we known should be removed which will at least verify that the core library was processed
	// TODO by Mike : List a few types
	class LinkingOfCoreLibrariesRemovesUnusedTypes {
		public static void Main ()
		{
		}
	}
}