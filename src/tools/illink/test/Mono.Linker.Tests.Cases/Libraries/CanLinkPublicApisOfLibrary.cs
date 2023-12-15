using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Libraries
{
	[IgnoreTestCase("NativeAOT doesn't implement library trimming the same way", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute (typeof (IgnoreTestCaseAttribute), By = Tool.Trimmer)]
	[SetupLinkerLinkPublicAndFamily]
	[SetupCompileAsLibrary]
	[Kept]
	[KeptMember (".ctor()")]
	public class CanLinkPublicApisOfLibrary
	{
		[Kept]
		public static void Main ()
		{
		}

		[Kept]
		public void UnusedPublicMethod ()
		{
		}

		[Kept]
		protected void UnusedProtectedMethod ()
		{
		}

		[Kept]
		protected internal void UnusedProtectedInternalMethod ()
		{
		}

		internal void UnunsedInternalMethod ()
		{
		}

		private void UnusedPrivateMethod ()
		{
		}
	}
}
