using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Libraries
{
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