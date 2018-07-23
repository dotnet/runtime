using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Libraries {
	[SetupLinkerLinkPublicAndFamily]
	[SetupCompileAsLibrary]
	[Kept]
	[KeptMember (".ctor()")]
	public class CanLinkPublicApisOfLibrary {
		// Kept because by default libraries their action set to copy
		[Kept]
		public static void Main ()
		{
			// Main is needed for the test collector to find and treat as a test
		}

		[Kept]
		public void UnusedPublicMethod ()
		{
		}

		private void UnusedPrivateMethod ()
		{
		}
	}
}