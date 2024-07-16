// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.LinkAttributes.Dependencies;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[KeptMember (".ctor()")]
	[SetupLinkAttributesFile ("TestRemoveFeatureAttributes.xml")]
	[RemovedAttributeInAssembly ("FeatureProperties.dll", typeof (FeatureSwitchDefinitionAttribute), typeof (FeatureProperties), nameof (FeatureProperties.FeatureSwitchDefinition))]
	[RemovedAttributeInAssembly ("FeatureProperties.dll", typeof (FeatureGuardAttribute), typeof (FeatureProperties), nameof (FeatureProperties.FeatureGuard))]
	[RemovedMemberInAssembly ("FeatureProperties.dll", typeof (FeatureProperties), "Removed()")]
	[SetupCompileBefore ("FeatureProperties.dll", new[] { "Dependencies/FeatureProperties.cs" })]
	[SetupLinkerArgument ("--feature", "FeatureSwitch", "false")]
	[SetupLinkerAction ("copy", "test")] // prevent trimming calls to feature switch properties
	[IgnoreLinkAttributes (false)]
	class FeatureAttributeRemovalInCopyAssembly
	{
		public static void Main ()
		{
			TestFeatureSwitch ();
			TestFeatureGuard ();
		}

		[Kept]
		static void TestFeatureSwitch () {
			if (FeatureProperties.FeatureSwitchDefinition)
				Unused ();
		}

		[Kept]
		static void TestFeatureGuard () {
			if (FeatureProperties.FeatureGuard)
				Unused ();
		}

		[Kept]
		static void Unused () { }
	}
}
