using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Warnings.Dependencies;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[SkipKeptItemsValidation]
	[SetupCompileBefore ("library.dll", new[] { typeof (TriggerWarnings_Lib) })]
	[SetupLinkerArgument ("--singlewarn+")]
	[SetupLinkerArgument ("--singlewarn-", "library")]
	[LogContains ("warning IL2104: Assembly 'test' produced trim warnings")]
	[LogContains ("IL2026.*" + nameof (TriggerWarnings_Lib) + ".*" + nameof (TriggerWarnings_Lib.RUCType) + ".*--RUCType--", regexMatch: true)]
	[LogDoesNotContain ("IL2026")]
	[LogDoesNotContain ("warning IL2104: Assembly 'library' produced trim warnings")]
	public class CanNotSingleWarnPerAssembly
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
