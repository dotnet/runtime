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
			DefineFeatureGuard.Test ();
			GuardBodyValidation.Test ();
			InvalidFeatureGuards.Test ();
		}

		class DefineFeatureGuard {
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool GuardDynamicCode => RuntimeFeature.IsDynamicCodeSupported;

			static void TestGuardDynamicCode ()
			{
				if (GuardDynamicCode)
					RequiresDynamicCode ();
			}

			[FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
			static bool GuardUnreferencedCode => TestFeatures.IsUnreferencedCodeSupported;

			static void TestGuardUnreferencedCode ()
			{
				if (GuardUnreferencedCode)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard(typeof(RequiresAssemblyFilesAttribute))]
			static bool GuardAssemblyFiles => TestFeatures.IsAssemblyFilesSupported;

			static void TestGuardAssemblyFiles ()
			{
				if (GuardAssemblyFiles)
					RequiresAssemblyFiles ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
			static bool GuardDynamicCodeAndUnreferencedCode => RuntimeFeature.IsDynamicCodeSupported && TestFeatures.IsUnreferencedCodeSupported;

			static void TestMultipleGuards ()
			{
				if (GuardDynamicCodeAndUnreferencedCode) {
					RequiresDynamicCode ();
					RequiresUnreferencedCode ();
				}
			}

			public static void Test ()
			{
				TestGuardDynamicCode ();
				TestGuardUnreferencedCode ();
				TestGuardAssemblyFiles ();
				TestMultipleGuards ();
			}
		}

		class GuardBodyValidation {
			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool ReturnTrueGuard => true;

			static void TestReturnTrueGuard ()
			{
				if (ReturnTrueGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool ReturnFalseGuard => false;

			static void TestReturnFalseGuard ()
			{
				if (ReturnFalseGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool OtherConditionGuard => OtherCondition ();

			static void TestOtherConditionGuard ()
			{
				if (OtherConditionGuard)
					RequiresDynamicCode ();
			}

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool DirectGuard => RuntimeFeature.IsDynamicCodeSupported;

			static void TestDirectGuard ()
			{
				if (DirectGuard)
					RequiresDynamicCode ();
			}

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool IndirectGuard => DirectGuard;

			static void TestIndirectGuard ()
			{
				if (IndirectGuard)
					RequiresDynamicCode ();
			}

			// BUG: We're not smart enough to do this analysis yet. Leave it unsupported for now.
			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool AndGuard => RuntimeFeature.IsDynamicCodeSupported && OtherCondition ();

			static void TestAndGuard ()
			{
				if (AndGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool OrGuard => RuntimeFeature.IsDynamicCodeSupported || OtherCondition ();

			static void TestOrGuard ()
			{
				if (OrGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool NotGuard => !RuntimeFeature.IsDynamicCodeSupported;

			static void TestNotGuard ()
			{
				if (NotGuard)
					RequiresDynamicCode ();
			}

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool NotNotGuard => !!RuntimeFeature.IsDynamicCodeSupported;

			static void TestNotNotGuard ()
			{
				if (NotNotGuard)
					RequiresDynamicCode ();
			}

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool EqualsTrueGuard => RuntimeFeature.IsDynamicCodeSupported == true;

			static void TestEqualsTrueGuard ()
			{
				if (EqualsTrueGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool EqualsFalseGuard => RuntimeFeature.IsDynamicCodeSupported == false;

			static void TestEqualsFalseGuard ()
			{
				if (EqualsFalseGuard)
					RequiresDynamicCode ();
			}

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool TrueEqualsGuard => true == RuntimeFeature.IsDynamicCodeSupported;

			static void TestTrueEqualsGuard ()
			{
				if (TrueEqualsGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool FalseEqualsGuard => false == RuntimeFeature.IsDynamicCodeSupported;

			static void TestFalseEqualsGuard ()
			{
				if (FalseEqualsGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool NotEqualsTrueGuard => RuntimeFeature.IsDynamicCodeSupported != true;

			static void TestNotEqualsTrueGuard ()
			{
				if (NotEqualsTrueGuard)
					RequiresDynamicCode ();
			}

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool NotEqualsFalseGuard => RuntimeFeature.IsDynamicCodeSupported != false;

			static void TestNotEqualsFalseGuard ()
			{
				if (NotEqualsFalseGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool TrueNotEqualsGuard => true != RuntimeFeature.IsDynamicCodeSupported;

			static void TestTrueNotEqualsGuard ()
			{
				if (TrueNotEqualsGuard)
					RequiresDynamicCode ();
			}

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool FalseNotEqualsGuard => false != RuntimeFeature.IsDynamicCodeSupported;

			static void TestFalseNotEqualsGuard ()
			{
				if (FalseNotEqualsGuard)
					RequiresDynamicCode ();
			}

			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool IsTrueGuard => RuntimeFeature.IsDynamicCodeSupported is true;

			static void TestIsTrueGuard ()
			{
				if (IsTrueGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool IsFalseGuard => RuntimeFeature.IsDynamicCodeSupported is false;

			static void TestIsFalseGuard ()
			{
				if (IsFalseGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool IfGuard {
				get {
					if (RuntimeFeature.IsDynamicCodeSupported)
						return true;
					return false;
				}
			}

			static void TestIfGuard ()
			{
				if (IfGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool ElseGuard {
				get {
					if (!RuntimeFeature.IsDynamicCodeSupported)
						return false;
					else
						return true;
				}
			}

			static void TestElseGuard ()
			{
				if (ElseGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool TernaryIfGuard => RuntimeFeature.IsDynamicCodeSupported ? true : false;

			static void TestTernaryIfGuard ()
			{
				if (TernaryIfGuard)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute))]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool TernaryElseGuard => !RuntimeFeature.IsDynamicCodeSupported ? false : true;

			static void TestTernaryElseGuard ()
			{
				if (TernaryElseGuard)
					RequiresDynamicCode ();
			}

			public static void Test ()
			{
				TestReturnTrueGuard ();
				TestReturnFalseGuard ();
				TestOtherConditionGuard ();
				TestDirectGuard ();
				TestIndirectGuard ();
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
				TestIfGuard ();
				TestElseGuard ();
				TestTernaryIfGuard ();
				TestTernaryElseGuard ();
			}
		}

		class InvalidFeatureGuards {
			[ExpectedWarning ("IL4001")]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static int NonBooleanProperty => 0;

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCodeAttribute))]
			static void TestNonBooleanProperty ()
			{
				if (NonBooleanProperty == 0)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL4001")]
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			bool NonStaticProperty => true;

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCodeAttribute))]
			static void TestNonStaticProperty ()
			{
				var instance = new InvalidFeatureGuards ();
				if (instance.NonStaticProperty)
					RequiresDynamicCode ();
			}

			// No warning for this case because we don't validate that the attribute usage matches
			// the expected AttributeUsage.Property for assemblies that define their own version
			// of FeatureGuardAttribute.
			[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
			static bool Method () => true;

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCodeAttribute))]
			static void TestMethod ()
			{
				if (Method ())
					RequiresDynamicCode ();
			}

			public static void Test ()
			{
				TestNonBooleanProperty ();
				TestNonStaticProperty ();
				TestMethod ();
			}
		}

		[RequiresDynamicCode (nameof (RequiresDynamicCode))]
		static void RequiresDynamicCode () { }

		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode))]
		static void RequiresUnreferencedCode () { }

		[RequiresAssemblyFiles (nameof (RequiresAssemblyFiles))]
		static void RequiresAssemblyFiles () { }

		static bool OtherCondition () => true;
	}
}
