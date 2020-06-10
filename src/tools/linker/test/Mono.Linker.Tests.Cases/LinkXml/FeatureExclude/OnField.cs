using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml.FeatureExclude
{
#if NETCOREAPP
	[IgnoreTestCase ("--exclude-feature is not supported on .NET Core")]
#endif
	[SetupLinkerArgument ("--exclude-feature", "one")]
	[SetupLinkerDescriptorFile ("OnField.xml")]
	public class OnField
	{
		public static void Main ()
		{
		}

		private int _featureOne;

		[Kept]
		private int _featureTwo;
	}
}
