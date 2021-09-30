using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.FeatureSettings
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetCore, "Feature settings in descriptors are not supported on Mono.")]
	[SetupLinkerDescriptorFile ("FeatureDescriptorsGlobalTrue.xml")]
	[SetupLinkerDescriptorFile ("FeatureDescriptorsGlobalFalse.xml")]
	[SetupLinkerDescriptorFile ("FeatureDescriptors.xml")]
	[SetupLinkerArgument ("--feature", "GlobalCondition", "true")]
	[SetupLinkerArgument ("--feature", "AssemblyCondition", "false")]
	[SetupLinkerArgument ("--feature", "TypeCondition", "true")]
	[SetupLinkerArgument ("--feature", "MethodCondition", "false")]
	[SetupLinkerArgument ("--feature", "FieldCondition", "true")]
	[SetupLinkerArgument ("--feature", "PropertyCondition", "false")]
	[SetupLinkerArgument ("--feature", "EventCondition", "true")]
	public class FeatureDescriptors
	{
		public static void Main ()
		{
		}

		[Kept]
		static bool DefaultConditionTrue;
		static bool DefaultConditionFalse;

		[Kept]
		static bool GlobalConditionTrue;
		static bool GlobalConditionFalse;

		static bool AssemblyConditionTrue;
		[Kept]
		static bool AssemblyConditionFalse;

		[Kept]
		static bool TypeConditionTrue;
		static bool TypeConditionFalse;


		static void MethodConditionTrue ()
		{
		}

		[Kept]
		static void MethodConditionFalse ()
		{
		}

		[Kept]
		static bool FieldConditionTrue;
		static bool FieldConditionFalse;

		static bool PropertyConditionTrue { get; set; }
		[Kept]
		[KeptBackingField]
		static bool PropertyConditionFalse { [Kept] get; [Kept] set; }

		[Kept]
		[KeptBackingField]
		[KeptEventAddMethod]
		[KeptEventRemoveMethod]
		static event EventHandler EventConditionTrue;
		static event EventHandler EVentConditionFalse;
	}
}