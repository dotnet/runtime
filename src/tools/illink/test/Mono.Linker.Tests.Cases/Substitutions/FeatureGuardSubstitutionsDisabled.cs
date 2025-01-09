// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[ExpectedNoWarnings]
	[SetupCompileBefore ("TestFeatures.dll", new[] { "Dependencies/TestFeatures.cs" })]
	[SetupLinkerArgument ("--disable-opt", "substitutefeatureguards")]
	[SetupLinkerArgument ("--feature", "Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutionsDisabled.FeatureSwitch", "false")]
	public class FeatureGuardSubstitutionsDisabled
	{
		public static void Main ()
		{
			TestGuard ();
			TestFeatureSwitch ();
		}

		[Kept]
		[ExpectedWarning ("IL4000", Tool.Analyzer, "")]
		[KeptAttributeAttribute (typeof (FeatureGuardAttribute))]
		[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
		static bool GuardUnreferencedCode {
			[Kept]
			get => throw null;
		}

		[Kept]
		// Body is not modified because feature guard substitutions are disabled in this test
		[ExpectedWarning ("IL2026")]
		static void TestGuard ()
		{
			if (GuardUnreferencedCode)
				RequiresUnreferencedCode ();
		}

		[FeatureSwitchDefinition ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutionsDisabled.FeatureSwitch")]
		static bool FeatureSwitch => throw null;

		[Kept]
		[ExpectedWarning ("IL2026", Tool.Analyzer, "")]
		// Feature switches are still substituted when feature guard substitutions are disabled
		[ExpectBodyModified]
		static void TestFeatureSwitch ()
		{
			if (FeatureSwitch)
				RequiresUnreferencedCode ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode))]
		static void RequiresUnreferencedCode () { }
	}
}
