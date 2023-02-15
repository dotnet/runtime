using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UnusedNonRequiredTypeIsRemoved.xml")]
	public class UnusedNonRequiredTypeIsRemoved
	{
		public static void Main ()
		{
		}

		class Unused
		{
		}
	}
}