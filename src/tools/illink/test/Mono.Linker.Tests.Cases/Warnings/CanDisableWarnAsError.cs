using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[ExpectNonZeroExitCode (1)]
	[IgnoreTestCase ("Ignore in NativeAOT, see https://github.com/dotnet/runtime/issues/82447", IgnoredBy = Tool.NativeAot)]
	[SkipKeptItemsValidation]
	[SkipRemainingErrorsValidation]
	[SetupLinkerSubstitutionFile ("CanDisableWarnAsErrorSubstitutions.xml")]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--warnaserror")]
	[SetupLinkerArgument ("--warnaserror-", "IL2010,IL2011,IL2012,IgnoreThis")]
	[SetupLinkerArgument ("--warnaserror", "IL2010")]
	[LogContains ("error IL2007")]
	[LogContains ("error IL2008")]
	[LogContains ("error IL2009")]
	[LogContains ("error IL2010")]
	[LogContains ("warning IL2011")]
	[LogContains ("warning IL2012")]
	[NoLinkedOutput]
	public class CanDisableWarnAsError
	{
		public static void Main ()
		{
		}

		class HelperClass
		{
			private int helperField = 0;
			int HelperMethod ()
			{
				return 0;
			}
		}
	}
}
