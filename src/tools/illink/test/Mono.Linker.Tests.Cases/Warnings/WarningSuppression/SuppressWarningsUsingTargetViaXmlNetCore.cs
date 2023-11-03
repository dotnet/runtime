// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Warnings.Dependencies;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	// https://github.com/dotnet/runtime/issues/82447
	[IgnoreTestCase ("NativeAOT doesn't support suppressing warnings via XML", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute(typeof(IgnoreTestCaseAttribute))]
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
