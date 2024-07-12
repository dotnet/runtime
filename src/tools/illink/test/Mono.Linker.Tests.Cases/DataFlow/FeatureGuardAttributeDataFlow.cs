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
	[SetupCompileBefore ("TestFeatures.dll", new[] { "Dependencies/TestFeatures.cs" })]
	public class FeatureGuardAttributeDataFlow
	{
		public static void Main ()
		{
			ValidGuardBodies.Test ();
			InvalidGuardBodies.Test ();
			InvalidFeatureGuards.Test ();
		}

		class ValidGuardBodies {

			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool ReturnFalseGuard => false;

			static void TestReturnFalseGuard ()
			{
				if (ReturnFalseGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool DirectGuard => TestFeatures.IsUnreferencedCodeSupported;

			static void TestDirectGuard ()
			{
				if (DirectGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool IndirectGuard => DirectGuard;

			static void TestIndirectGuard ()
			{
				if (IndirectGuard)
					RequiresUnreferencedCode ();
			}

			// Analyzer doesn't understand this pattern because it compiles into a CFG that effectively
			// looks like this:
			//
			//     bool tmp;
			//     if (TestFeatures.IsUnreferencedCodeSupported)
			//         tmp = OtherCondition ();
			//     else
			//         tmp = false;
			//     return tmp;
			//
			// The analyzer doesn't do constant propagation of the boolean, so it doesn't know that
			// the return value is always false when TestFeatures.IsUnreferencedCodeSupported is false.
			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool AndGuard => TestFeatures.IsUnreferencedCodeSupported && OtherCondition ();

			static void TestAndGuard ()
			{
				if (AndGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool NotNotGuard => !!TestFeatures.IsUnreferencedCodeSupported;

			static void TestNotNotGuard ()
			{
				if (NotNotGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool EqualsTrueGuard => TestFeatures.IsUnreferencedCodeSupported == true;

			static void TestEqualsTrueGuard ()
			{
				if (EqualsTrueGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool TrueEqualsGuard => true == TestFeatures.IsUnreferencedCodeSupported;

			static void TestTrueEqualsGuard ()
			{
				if (TrueEqualsGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool NotEqualsFalseGuard => TestFeatures.IsUnreferencedCodeSupported != false;

			static void TestNotEqualsFalseGuard ()
			{
				if (NotEqualsFalseGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool FalseNotEqualsGuard => false != TestFeatures.IsUnreferencedCodeSupported;

			static void TestFalseNotEqualsGuard ()
			{
				if (FalseNotEqualsGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool IsTrueGuard => TestFeatures.IsUnreferencedCodeSupported is true;

			static void TestIsTrueGuard ()
			{
				if (IsTrueGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool IsNotFalseGuard => TestFeatures.IsUnreferencedCodeSupported is not false;

			static void TestIsNotFalseGuard ()
			{
				if (IsNotFalseGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool IfReturnTrueGuard {
				get {
					if (TestFeatures.IsUnreferencedCodeSupported)
						return true;
					return false;
				}
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool ElseReturnTrueGuard {
				get {
					if (!TestFeatures.IsUnreferencedCodeSupported)
						return false;
					else
						return true;
				}
			}

			static void TestElseReturnTrueGuard ()
			{
				if (ElseReturnTrueGuard)
					RequiresUnreferencedCode ();
			}

			static void TestIfReturnTrueGuard ()
			{
				if (IfReturnTrueGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool AssertReturnFalseGuard {
				 get {
					Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
					return false;
				 }
			}

			static void TestAssertReturnFalseGuard ()
			{
				if (AssertReturnFalseGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool AssertNotReturnFalseGuard {
				 get {
					Debug.Assert (!TestFeatures.IsUnreferencedCodeSupported);
					return false;
				 }
			}

			static void TestAssertNotReturnFalseGuard ()
			{
				if (AssertNotReturnFalseGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool AssertReturnTrueGuard {
				 get {
					Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
					return true;
				 }
			}

			static void TestAssertReturnTrueGuard ()
			{
				if (AssertReturnTrueGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool ThrowGuard {
				get {
					if (!TestFeatures.IsUnreferencedCodeSupported)
						throw new Exception ();
					return false;
				}
			}

			static void TestThrowGuard ()
			{
				if (ThrowGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool TernaryIfGuard => TestFeatures.IsUnreferencedCodeSupported ? true : false;

			static void TestTernaryIfGuard ()
			{
				if (TernaryIfGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool TernaryElseGuard => !TestFeatures.IsUnreferencedCodeSupported ? false : true;

			static void TestTernaryElseGuard ()
			{
				if (TernaryElseGuard)
					RequiresUnreferencedCode ();
			}

			public static void Test ()
			{
				TestDirectGuard ();
				TestIndirectGuard ();

				TestReturnFalseGuard ();
				TestAndGuard ();
				TestNotNotGuard ();
				TestEqualsTrueGuard ();
				TestTrueEqualsGuard ();
				TestNotEqualsFalseGuard ();
				TestFalseNotEqualsGuard ();
				TestIsTrueGuard ();
				TestIsNotFalseGuard ();
				TestIfReturnTrueGuard ();
				TestElseReturnTrueGuard ();
				TestAssertReturnFalseGuard ();
				TestAssertNotReturnFalseGuard ();
				TestAssertReturnTrueGuard ();
				TestThrowGuard ();
				TestTernaryIfGuard ();
				TestTernaryElseGuard ();
			}
		}

		class InvalidGuardBodies {
			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool ReturnTrueGuard => true;

			static void TestReturnTrueGuard ()
			{
				if (ReturnTrueGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool OtherConditionGuard => OtherCondition ();

			static void TestOtherConditionGuard ()
			{
				if (OtherConditionGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool OrGuard => TestFeatures.IsUnreferencedCodeSupported || OtherCondition ();

			static void TestOrGuard ()
			{
				if (OrGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool NotGuard => !TestFeatures.IsUnreferencedCodeSupported;

			static void TestNotGuard ()
			{
				if (NotGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool EqualsFalseGuard => TestFeatures.IsUnreferencedCodeSupported == false;

			static void TestEqualsFalseGuard ()
			{
				if (EqualsFalseGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool FalseEqualsGuard => false == TestFeatures.IsUnreferencedCodeSupported;

			static void TestFalseEqualsGuard ()
			{
				if (FalseEqualsGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool NotEqualsTrueGuard => TestFeatures.IsUnreferencedCodeSupported != true;

			static void TestNotEqualsTrueGuard ()
			{
				if (NotEqualsTrueGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool TrueNotEqualsGuard => true != TestFeatures.IsUnreferencedCodeSupported;

			static void TestTrueNotEqualsGuard ()
			{
				if (TrueNotEqualsGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool IsNotTrueGuard => TestFeatures.IsUnreferencedCodeSupported is not true;

			static void TestIsNotTrueGuard ()
			{
				if (IsNotTrueGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool IsFalseGuard => TestFeatures.IsUnreferencedCodeSupported is false;

			static void TestIsFalseGuard ()
			{
				if (IsFalseGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool IfReturnFalseGuard {
				get {
					if (TestFeatures.IsUnreferencedCodeSupported)
						return false;
					return true;
				}
			}

			static void TestIfReturnFalseGuard ()
			{
				if (IfReturnFalseGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool ElseReturnFalseGuard {
				get {
					if (!TestFeatures.IsUnreferencedCodeSupported)
						return true;
					else
						return false;
				}
			}

			static void TestElseReturnFalseGuard ()
			{
				if (ElseReturnFalseGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool AssertNotReturnTrueGuard {
				 get {
					Debug.Assert (!TestFeatures.IsUnreferencedCodeSupported);
					return true;
				 }
			}

			static void TestAssertNotReturnTrueGuard ()
			{
				if (AssertNotReturnTrueGuard)
					RequiresUnreferencedCode ();
			}

			public static void Test ()
			{
				TestOtherConditionGuard ();

				TestReturnTrueGuard ();
				TestOrGuard ();
				TestNotGuard ();
				TestEqualsFalseGuard ();
				TestFalseEqualsGuard ();
				TestNotEqualsTrueGuard ();
				TestTrueNotEqualsGuard ();
				TestIsNotTrueGuard ();
				TestIsFalseGuard ();
				TestIfReturnFalseGuard ();
				TestElseReturnFalseGuard ();
				TestAssertNotReturnTrueGuard ();
			}
		}

		class InvalidFeatureGuards {
			[ExpectedWarning ("IL4001", Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static int NonBooleanProperty => 0;

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCodeAttribute))]
			static void TestNonBooleanProperty ()
			{
				if (NonBooleanProperty == 0)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4001", Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			bool NonStaticProperty => true;

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCodeAttribute))]
			static void TestNonStaticProperty ()
			{
				var instance = new InvalidFeatureGuards ();
				if (instance.NonStaticProperty)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool SetOnlyProperty { set => throw null; }

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCodeAttribute))]
			static void TestSetOnlyProperty ()
			{
				if (SetOnlyProperty = true)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4001", Tool.Analyzer, "")]
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool GetAndSetProperty { get => true; set => throw null; }

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCodeAttribute))]
			static void TestGetAndSetProperty ()
			{
				if (GetAndSetProperty)
					RequiresUnreferencedCode ();
			}

			// No warning for this case because we don't validate that the attribute usage matches
			// the expected AttributeUsage.Property for assemblies that define their own version
			// of FeatureGuardAttribute.
			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool Method () => true;

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCodeAttribute))]
			static void TestMethod ()
			{
				if (Method ())
					RequiresUnreferencedCode ();
			}

			public static void Test ()
			{
				TestNonBooleanProperty ();
				TestNonStaticProperty ();
				TestSetOnlyProperty ();
				TestGetAndSetProperty ();
				TestMethod ();
			}
		}

		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode))]
		static void RequiresUnreferencedCode () { }

		static bool OtherCondition () => true;
	}
}

namespace System.Diagnostics.CodeAnalysis
{
    // Allow AttributeTargets.Method for testing invalid usages of a custom FeatureGuardAttribute
    [AttributeUsage (AttributeTargets.Property | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class FeatureGuardAttribute : Attribute
    {
        public Type FeatureType { get; }

        public FeatureGuardAttribute (Type featureType)
        {
            FeatureType = featureType;
        }
    }
}
