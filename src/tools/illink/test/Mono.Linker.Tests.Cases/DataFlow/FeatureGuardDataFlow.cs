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

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	[IgnoreSubstitutions (false)]
	public class FeatureGuardDataFlow
	{
		public static void Main ()
		{
			SupportedFeatures.Test ();
			FeatureGuardTargets.Test ();
			FeatureGuardBodyValidation.Test ();
			FeatureSwitchBehavior.Test ();
		}

		class SupportedFeatures {
			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool GuardRequiresDynamicCode => true;

			static void CanDefineGuardForRequiresDynamicCode ()
			{
				if (GuardRequiresDynamicCode)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute))]
			[FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
			static bool GuardRequiresUnreferencedCode => true;

			static void CanDefineGuardForRequiresUnreferencedCode ()
			{
				if (GuardRequiresUnreferencedCode)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresAssemblyFilesAttribute))]
			[FeatureGuard(typeof(RequiresAssemblyFilesAttribute))]
			static bool GuardRequiresAssemblyFiles => true;

			static void CanDefineGuardForRequiresAssemblyFiles ()
			{
				if (GuardRequiresAssemblyFiles)
					RequiresAssemblyFiles ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void CallTestDynamicAndUnreferencedCodeGuarded ()
			{
				RequiresDynamicCode ();
				RequiresUnreferencedCode ();
			}

			static void CallTestDynamicAndUnreferencedCodeUnguarded ()
			{
				if (TestFeatureGuards.GuardDynamicCodeAndUnreferencedCode) {
					RequiresDynamicCode ();
					RequiresUnreferencedCode ();
				}
			}

			public static void Test ()
			{
				CanDefineGuardForRequiresDynamicCode ();
				CanDefineGuardForRequiresUnreferencedCode ();
				CanDefineGuardForRequiresAssemblyFiles ();
			}
		}

		class FeatureGuardTargets {
			public static void Test ()
			{
			}
		}

		class FeatureGuardBodyValidation {
			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool ReturnTrueGuard => true;

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool ReturnFalseGuard => false;

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool OtherConditionGuard => OtherCondition ();

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool DirectGuard => RuntimeFeature.IsDynamicCodeSupported;

			// BUG: We're not smart enough to do this analysis yet. Leave it unsupported for now.
			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool AndGuard => RuntimeFeature.IsDynamicCodeSupported && OtherCondition ();

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool OrGuard => RuntimeFeature.IsDynamicCodeSupported || OtherCondition ();

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool NotGuard => !RuntimeFeature.IsDynamicCodeSupported;

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool NotNotGuard => !!RuntimeFeature.IsDynamicCodeSupported;

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool EqualsTrueGuard => RuntimeFeature.IsDynamicCodeSupported == true;

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool EqualsFalseGuard => RuntimeFeature.IsDynamicCodeSupported == false;

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool TrueEqualsGuard => true == RuntimeFeature.IsDynamicCodeSupported;

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool FalseEqualsGuard => false == RuntimeFeature.IsDynamicCodeSupported;

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool NotEqualsTrueGuard => RuntimeFeature.IsDynamicCodeSupported != true;

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool NotEqualsFalseGuard => RuntimeFeature.IsDynamicCodeSupported != false;

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool TrueNotEqualsGuard => true != RuntimeFeature.IsDynamicCodeSupported;

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool FalseNotEqualsGuard => false != RuntimeFeature.IsDynamicCodeSupported;

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool IsTrueGuard => RuntimeFeature.IsDynamicCodeSupported is true;

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool IsFalseGuard => RuntimeFeature.IsDynamicCodeSupported is false;

			static bool OtherCondition () => true;

			static void TestReturnTrueGuard ()
			{
				if (ReturnTrueGuard)
					RequiresDynamicCode ();
			}

			static void TestReturnFalseGuard ()
			{
				if (ReturnFalseGuard)
					RequiresDynamicCode ();
			}

			static void TestOtherConditionGuard ()
			{
				if (OtherConditionGuard)
					RequiresDynamicCode ();
			}

			static void TestDirectGuard ()
			{
				if (DirectGuard)
					RequiresDynamicCode ();
			}

			static void TestAndGuard ()
			{
				if (AndGuard)
					RequiresDynamicCode ();
			}

			static void TestOrGuard ()
			{
				if (OrGuard)
					RequiresDynamicCode ();
			}

			static void TestNotGuard ()
			{
				if (NotGuard)
					RequiresDynamicCode ();
			}

			static void TestNotNotGuard ()
			{
				if (NotNotGuard)
					RequiresDynamicCode ();
			}

			static void TestEqualsTrueGuard ()
			{
				if (EqualsTrueGuard)
					RequiresDynamicCode ();
			}

			static void TestEqualsFalseGuard ()
			{
				if (EqualsFalseGuard)
					RequiresDynamicCode ();
			}

			static void TestTrueEqualsGuard ()
			{
				if (TrueEqualsGuard)
					RequiresDynamicCode ();
			}

			static void TestFalseEqualsGuard ()
			{
				if (FalseEqualsGuard)
					RequiresDynamicCode ();
			}

			static void TestNotEqualsTrueGuard ()
			{
				if (NotEqualsTrueGuard)
					RequiresDynamicCode ();
			}

			static void TestNotEqualsFalseGuard ()
			{
				if (NotEqualsFalseGuard)
					RequiresDynamicCode ();
			}

			static void TestTrueNotEqualsGuard ()
			{
				if (TrueNotEqualsGuard)
					RequiresDynamicCode ();
			}

			static void TestFalseNotEqualsGuard ()
			{
				if (FalseNotEqualsGuard)
					RequiresDynamicCode ();
			}

			static void TestIsTrueGuard ()
			{
				if (IsTrueGuard)
					RequiresDynamicCode ();
			}

			static void TestIsFalseGuard ()
			{
				if (IsFalseGuard)
					RequiresDynamicCode ();
			}

			public static void Test ()
			{
				TestReturnTrueGuard ();
				TestReturnFalseGuard ();
				TestOtherConditionGuard ();
				TestDirectGuard ();
				TestAndGuard ();
				TestOrGuard ();
				TestNotGuard ();
				TestNotNotGuard ();
				TestEqualsTrueGuard ();
				TestEqualsFalseGuard ();
				TestTrueEqualsGuard ();
				TestFalseEqualsGuard ();
				TestNotEqualsTrueGuard ();
				TestNotEqualsFalseGuard ();
				TestTrueNotEqualsGuard ();
				TestFalseNotEqualsGuard ();
				TestIsTrueGuard ();
				TestIsFalseGuard ();
			}
		}

		class FeatureSwitchBehavior {
			public static void Test ()
			{
			}
		}

		[RequiresDynamicCode (nameof (RequiresDynamicCode))]
		static void RequiresDynamicCode () { }

		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode))]
		static void RequiresUnreferencedCode () { }

		[RequiresAssemblyFiles (nameof (RequiresAssemblyFiles))]
		static void RequiresAssemblyFiles () { }

		class TestFeatureGuards
		{

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
			public static bool GuardDynamicCodeAndUnreferencedCode => RuntimeFeature.IsDynamicCodeSupported;
		}
	}
}
