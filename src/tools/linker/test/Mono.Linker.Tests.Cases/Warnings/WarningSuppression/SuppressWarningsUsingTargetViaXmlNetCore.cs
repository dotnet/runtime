// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Warnings.Dependencies;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetCore, "This test is specific to .NET Core")]
	// For netcoreapp we don't have to specify the assembly for the attribute, since the attribute comes from corelib
	// and will be found always.
	[SetupLinkAttributesFile ("SuppressWarningsUsingTargetViaXml.netcore.xml")]
	[SetupCompileBefore ("library.dll", new[] { typeof (TriggerWarnings_Lib) })]

	[KeptAssembly ("library.dll")]
	[SetupLinkerAction ("link", "library.dll")]
	[LogDoesNotContain ("TriggerUnrecognizedPattern()")]
	public class SuppressWarningsUsingTargetViaXmlNetCore
	{
		public static void Main ()
		{
			TriggerWarnings_Lib.Main ();
		}
	}
}
