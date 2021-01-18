using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Libraries
{
#if !NETCOREAPP
	[IgnoreTestCase ("Correctly handled by illink only")]
#endif
	[Kept]
	[KeptMember (".ctor()")]
	[SetupLinkerAction ("copyused", "test")]
	[SetupLinkerLinkPublicAndFamily]
	public class CopyUsedAssemblyWithPublicRoots
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
		private void UnusedPrivateMethod ()
		{
		}
	}
}