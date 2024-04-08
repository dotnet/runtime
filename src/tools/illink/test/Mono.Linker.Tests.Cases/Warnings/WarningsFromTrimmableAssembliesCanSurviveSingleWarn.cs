using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Warnings.Dependencies;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[IgnoreTestCase ("Ignore in NativeAOT, see https://github.com/dotnet/runtime/issues/82447", IgnoredBy = Tool.NativeAot)]
	[SkipKeptItemsValidation]
	[SetupCompileBefore ("library.dll", new[] { typeof (TriggerWarnings_TrimmableLib) })]
	[SetupLinkerArgument ("--singlewarn")]
	[LogContains (".*warning IL2026: .*TriggerWarnings_TrimmableLib.RUCIntentional.*RUC warning left in the trimmable assembly.*", regexMatch: true)]
	[LogDoesNotContain ("IL2072")]
	[LogContains ("warning IL2104: Assembly 'library' produced trim warnings")]
	[LogDoesNotContain ("IL2026")]
	public class WarningsFromTrimmableAssembliesCanSurviveSingleWarn
	{
		public static void Main ()
		{
			TriggerWarnings_TrimmableLib.Main ();
		}
	}
}
