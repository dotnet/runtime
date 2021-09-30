using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "--exclude-feature is not supported on .NET Core")]
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[SetupLinkerArgument ("--exclude-feature", "com")]
	public class ComAttributesAreRemovedWhenFeatureExcluded
	{
		public static void Main ()
		{
			var tmp = ReturnValueUsedToMarkType ();
		}

		[Kept]
		static A ReturnValueUsedToMarkType ()
		{
			return null;
		}

		[Kept]
		[RemovedPseudoAttribute (0x00001000u)]
		[ComImport]
		[Guid ("D7BB1889-3AB7-4681-A115-60CA9158FECA")]
		[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
		interface A
		{
		}
	}
}