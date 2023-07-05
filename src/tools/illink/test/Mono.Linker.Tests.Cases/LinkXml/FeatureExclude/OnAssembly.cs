using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.LinkXml.FeatureExclude.Dependencies;

namespace Mono.Linker.Tests.Cases.LinkXml.FeatureExclude
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "--exclude-feature is not supported on .NET Core")]
	[SetupLinkerArgument ("--exclude-feature", "one")]
	[SetupCompileBefore ("library1.dll", new[] { typeof (OnAssembly_Lib1) })]
	[SetupCompileBefore ("library2.dll", new[] { typeof (OnAssembly_Lib2) })]
	[RemovedTypeInAssembly ("library1.dll", typeof (OnAssembly_Lib1.FeatureOneClass))]
	[KeptTypeInAssembly ("library2.dll", typeof (OnAssembly_Lib2.FeatureTwoClass))]
	[SetupLinkerDescriptorFile ("OnAssembly.xml")]
	public class OnAssembly
	{
		public static void Main ()
		{
			OnAssembly_Lib1.UsedSoCompilerDoesntRemoveReference ();
			OnAssembly_Lib2.UsedSoCompilerDoesntRemoveReference ();
		}
	}
}
