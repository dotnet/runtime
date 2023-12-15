using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[ExpectNonZeroExitCode (1)]
	[SkipKeptItemsValidation]
	[Define("IN_TEST_BUILD")]
	[SetupLinkerSubstitutionFile ("CanWarnAsErrorSubstitutions.xml")]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--warnaserror")]
	[LogContains ("error IL2007")]
	[LogContains ("error IL2008")]
	[LogContains ("error IL2009")]
	[LogContains ("error IL2010")]
	[LogContains ("error IL2011")]
	[LogContains ("error IL2012")]
	[NoLinkedOutput]
	public class CanWarnAsErrorGlobal
	{
		public static void Main ()
		{
		}
	}

#if IN_TEST_BUILD
	public class CanWarnAsError
	{
		class HelperClass
		{
			private int helperField = 0;
			int HelperMethod ()
			{
				return 0;
			}
		}
	}
#endif
}
