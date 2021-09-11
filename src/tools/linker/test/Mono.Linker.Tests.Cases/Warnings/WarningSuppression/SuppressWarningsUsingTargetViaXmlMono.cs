// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Warnings.Dependencies;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "This test is specific to .NET Framework")]
	// For mono though, we have to specify the assembly (Mono.Linker.Tests.Cases.Expectations) because at the time of processing
	// that assembly is not yet loaded into the closure in the linker, so it won't find the attribute type.
	[SetupLinkAttributesFile ("SuppressWarningsUsingTargetViaXml.mono.xml")]
	[SetupCompileBefore ("library.dll", new[] { typeof (TriggerWarnings_Lib) }, new[] { "System.Core.dll" })]

	[KeptAssembly ("library.dll")]
	[SetupLinkerAction ("link", "library.dll")]
	[LogDoesNotContain ("TriggerUnrecognizedPattern()")]
	public class SuppressWarningsUsingTargetViaXmlMono
	{
		public static void Main ()
		{
			TriggerWarnings_Lib.Main ();
		}
	}
}