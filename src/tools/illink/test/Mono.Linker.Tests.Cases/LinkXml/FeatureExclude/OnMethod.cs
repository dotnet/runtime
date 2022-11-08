using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml.FeatureExclude
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "--exclude-feature is not supported on .NET Core")]
	[SetupLinkerArgument ("--exclude-feature", "one")]
	[SetupLinkerDescriptorFile ("OnMethod.xml")]
	public class OnMethod
	{
		public static void Main ()
		{
		}

		public void FeatureOne ()
		{
		}

		[Kept]
		public void FeatureTwo ()
		{
		}
	}
}
