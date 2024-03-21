using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UnusedInterfaceTypeOnTypeWithPreserveNothingIsRemoved.xml")]
	public class UnusedInterfaceTypeOnTypeWithPreserveNothingIsRemoved
	{
		public static void Main ()
		{
		}

		interface IFoo
		{
		}

		[Kept]
		class Bar : IFoo
		{
		}
	}
}
