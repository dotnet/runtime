using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml.FeatureExclude
{
#if NETCOREAPP
	[IgnoreTestCase ("--exclude-feature is not supported on .NET Core")]
#endif
	[SetupLinkerArgument ("--exclude-feature", "one")]
	[SetupLinkerDescriptorFile ("OnType.xml")]
	public class OnType
	{
		public static void Main ()
		{
		}

		class FeatureOneClass
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		class FeatureTwoClass
		{
		}
	}
}
