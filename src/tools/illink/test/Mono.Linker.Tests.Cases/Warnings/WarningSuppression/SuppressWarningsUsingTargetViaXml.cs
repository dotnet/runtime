using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Warnings.Dependencies;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	// For netcoreapp we don't have to specify the assembly for the attribute, since the attribute comes from corelib
	// and will be found always.
	// For mono though, we have to specify the assembly (Mono.Linker.Tests.Cases.Expectations) because at the time of processing
	// that assembly is not yet loaded into the closure in the linker, so it won't find the attribute type.
#if NETCOREAPP
	[SetupLinkAttributesFile ("SuppressWarningsUsingTargetViaXml.netcore.xml")]
#else
	[SetupLinkAttributesFile ("SuppressWarningsUsingTargetViaXml.mono.xml")]
#endif
#if !ILLINK
	[SetupCompileBefore ("library.dll", new[] { typeof (TriggerWarnings_Lib) }, new[] { "System.Core.dll" })]
#else
	[SetupCompileBefore ("library.dll", new[] { typeof (TriggerWarnings_Lib) })]
#endif

	[KeptAssembly ("library.dll")]
	[SetupLinkerAction ("link", "library.dll")]
	[LogDoesNotContain ("TriggerUnrecognizedPattern()")]
	public class SuppressWarningsUsingTargetViaXml
	{
		public static void Main ()
		{
			TriggerWarnings_Lib.Main ();
		}
	}
}
