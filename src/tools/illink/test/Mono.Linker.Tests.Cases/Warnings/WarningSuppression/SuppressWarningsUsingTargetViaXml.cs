using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Warnings.WarningSuppression.Dependencies;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SetupLinkAttributesFile ("SuppressWarningsUsingTargetViaXml.xml")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/TriggerWarnings_Lib.cs" })]
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
