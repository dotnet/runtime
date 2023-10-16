using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Libraries
{
	[IgnoreTestCase ("NativeAOT doesn't implement library trimming the same way", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute (typeof (IgnoreTestCaseAttribute), By = Tool.Trimmer)]
	[SetupCompileAsLibrary]
	[SetupLinkerArgument ("-a", "test.dll")]
	[Kept]
	[KeptMember (".ctor()")]
	public class DefaultLibraryLinkBehavior
	{
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

		[Kept]
		private void UnusedPrivateMethod ()
		{
		}
	}
}
