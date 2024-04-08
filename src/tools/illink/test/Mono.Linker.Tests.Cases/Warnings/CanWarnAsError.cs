using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[ExpectNonZeroExitCode (1)]
	[SkipKeptItemsValidation]
	[SetupLinkerSubstitutionFile ("CanWarnAsErrorSubstitutions.xml")]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--warnaserror-")]
	[SetupLinkerArgument ("--warnaserror+", "IL2011,IgnoreThis")]
	[SetupLinkerArgument ("--warnaserror", "IL2012,CS4321,IgnoreThisToo")]
	[SetupLinkerArgument ("--warnaserror", "IL2010")]
	[SetupLinkerArgument ("--warnaserror-", "IL2010")]
	[LogContains ("warning IL2007")]
	[LogContains ("warning IL2008")]
	[LogContains ("warning IL2009")]
	[LogContains ("warning IL2010")]
	[LogContains ("error IL2011")]
	[LogContains ("error IL2012")]
	[NoLinkedOutput]
	public class CanWarnAsError
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
