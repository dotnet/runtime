using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[SkipKeptItemsValidation]
	[SetupLinkerSubstitutionFile ("CanDisableWarningsSubstitutions.xml")]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--nowarn", "IL2067,IL2007;IL2008;IL2009,IL2010,ThisWillBeIgnored")]
	[SetupLinkerArgument ("--nowarn", "IL2011,2012,0123")]
	[LogDoesNotContain ("IL20(06|07|08|09|10|11)")]
	[LogContains ("IL2012")]
	public class CanDisableWarnings
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
