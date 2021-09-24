using System;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "--exclude-feature is not supported on .NET Core")]
	[SetupLinkerArgument ("--exclude-feature", "com")]
	[SetupLinkerDescriptorFile ("CanPreserveExcludedFeatureCom.xml")]
	public class CanPreserveExcludedFeatureCom
	{
		public static void Main ()
		{
			var a = new A ();
		}
	}

	[Kept]
	[KeptMember (".ctor()")]
	[KeptAttributeAttribute (typeof (GuidAttribute))]
	[ComImport]
	[Guid ("D7BB1889-3AB7-4681-A115-60CA9158FECA")]
	class A
	{
		private int field;
	}
}