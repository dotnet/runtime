using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Warnings.Dependencies;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[IgnoreTestCase ("Ignore in NativeAOT, see https://github.com/dotnet/runtime/issues/82447", IgnoredBy = Tool.NativeAot)]
	[SkipKeptItemsValidation]
	[SetupCompileBefore ("library.dll", new[] { typeof (TriggerWarnings_Lib) })]
	[SetupLinkerArgument ("--singlewarn")]
	[SetupLinkerArgument ("--nowarn", "IL2026")]
	[LogDoesNotContain ("IL2026")]
	[LogDoesNotContain ("IL2104: Assembly 'test' produced trim warnings")]
	[LogContains ("warning IL2104: Assembly 'library' produced trim warnings")]
	public class CanSingleWarnWithNoWarn
	{
		public static void Main ()
		{
			CreateWarnings ();
			TriggerWarnings_Lib.Main ();
		}

		public static void CreateWarnings ()
		{
			RequireUnreferencedCode ();
		}

		[RequiresUnreferencedCode ("Requires unreferenced code.")]
		public static void RequireUnreferencedCode ()
		{
		}
	}
}
