using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Warnings.Dependencies;

namespace Mono.Linker.Tests.Cases.Warnings.Individual
{
	[SetupLinkerTrimMode ("skip")]
#if !NET
	[SetupCompileBefore ("library.dll", new[] { typeof (TriggerWarnings_Lib) }, new[] { "System.Core.dll" })]
#else
	[SetupCompileBefore ("library.dll", new[] { typeof (TriggerWarnings_Lib) })]
#endif
	[KeptAssembly ("library.dll")]
	[SetupLinkerAction ("link", "library.dll")]
	[SetupLinkerArgument ("--generate-warning-suppressions", "xml")]

	// Test that --warnaserror has no effect on --generate-warning-suppressions
	[SetupLinkerArgument ("--warnaserror")]
	public class CanGenerateWarningSuppressionFileXml
	{
		public static void Main ()
		{
			TriggerWarnings_Lib.Main ();
		}
	}
}
