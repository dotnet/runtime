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
	// Note: the XML must be passed as an embedded resource named ILLink.Substitutions.xml,
	// not as a separate substitution file, for it to work with NativeAot.
	// Related: https://github.com/dotnet/runtime/issues/88647
	[SetupCompileBefore ("TestFeatures.dll", new[] { "Dependencies/TestFeatures.cs" },
		resources: new object[] { new [] { "FeatureCheckDataFlowTestSubstitutions.xml", "ILLink.Substitutions.xml" } })]
	// FeatureCheckAttribute is currently only supported by the analyzer.
	// The same guard behavior is achieved for ILLink/ILCompiler using substitutions.
	[SetupCompileResource ("FeatureCheckAttributeDataFlowTestSubstitutions.xml", "ILLink.Substitutions.xml")]
	[IgnoreSubstitutions (false)]
	public class FeatureCheckAttributeDataFlow
	{
		public static void Main ()
		{
			DefineFeatureCheck.Test ();
			ValidGuardBodies.Test ();
			InvalidGuardBodies.Test ();
			InvalidFeatureChecks.Test ();
		}

		class DefineFeatureCheck {
			[FeatureCheck (typeof(RequiresDynamicCodeAttribute))]
			static bool GuardDynamicCode => RuntimeFeature.IsDynamicCodeSupported;

			static void TestGuardDynamicCode ()
			{
				if (GuardDynamicCode)
					RequiresDynamicCode ();
			}

			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool GuardUnreferencedCode => TestFeatures.IsUnreferencedCodeSupported;

			static void TestGuardUnreferencedCode ()
			{
				if (GuardUnreferencedCode)
					RequiresUnreferencedCode ();
			}

			[FeatureCheck (typeof(RequiresAssemblyFilesAttribute))]
			static bool GuardAssemblyFiles => TestFeatures.IsAssemblyFilesSupported;

			static void TestGuardAssemblyFiles ()
			{
				if (GuardAssemblyFiles)
					RequiresAssemblyFiles ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute), ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(DynamicCodeAndUnreferencedCode))]
			static bool GuardDynamicCodeAndUnreferencedCode => RuntimeFeature.IsDynamicCodeSupported && TestFeatures.IsUnreferencedCodeSupported;

			[FeatureDependsOn (typeof (RequiresDynamicCodeAttribute))]
			[FeatureDependsOn (typeof (RequiresUnreferencedCodeAttribute))]
			static class DynamicCodeAndUnreferencedCode {}

			static void TestMultipleGuards ()
			{
				if (GuardDynamicCodeAndUnreferencedCode) {
					RequiresDynamicCode ();
					RequiresUnreferencedCode ();
				}
			}

			[FeatureDependsOn (typeof (RequiresDynamicCodeAttribute))]
			static class DynamicCode1 {}

			[FeatureDependsOn (typeof (DynamicCode1))]
			static class DynamicCode2 {}

			[FeatureCheck (typeof (DynamicCode2))]
			static bool GuardDynamicCodeIndirect => RuntimeFeature.IsDynamicCodeSupported;

			static void TestIndirectGuard ()
			{
				if (GuardDynamicCodeIndirect)
					RequiresDynamicCode ();
			}

			[FeatureDependsOn (typeof (RequiresDynamicCodeAttribute))]
			[FeatureDependsOn (typeof (DynamicCodeCycle))]
			static class DynamicCodeCycle {}

			[FeatureCheck (typeof (DynamicCodeCycle))]
			static bool GuardDynamicCodeCycle => RuntimeFeature.IsDynamicCodeSupported;

			[FeatureCheck (typeof (DynamicCodeCycle))]
			static void TestFeatureDependencyCycle1 ()
			{
				if (GuardDynamicCodeCycle)
					RequiresDynamicCode ();
			}

			[FeatureDependsOn (typeof (DynamicCodeCycle2_B))]
			static class DynamicCodeCycle2_A {}

			[FeatureDependsOn (typeof (RequiresDynamicCodeAttribute))]
			[FeatureDependsOn (typeof (DynamicCodeCycle2_A))]
			static class DynamicCodeCycle2_B {}

			[FeatureDependsOn (typeof (DynamicCodeCycle2_A))]
			static class DynamicCodeCycle2 {}

			[FeatureCheck (typeof (DynamicCodeCycle2))]
			static bool GuardDynamicCodeCycle2 => RuntimeFeature.IsDynamicCodeSupported;

			static void TestFeatureDependencyCycle2 ()
			{
				if (GuardDynamicCodeCycle2)
					RequiresDynamicCode ();
			}

			public static void Test ()
			{
				TestGuardDynamicCode ();
				TestGuardUnreferencedCode ();
				TestGuardAssemblyFiles ();
				TestMultipleGuards ();
				TestIndirectGuard ();
				TestFeatureDependencyCycle1 ();
				TestFeatureDependencyCycle2 ();
			}
		}

		class ValidGuardBodies {

			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool ReturnFalseGuard => false;

			static void TestReturnFalseGuard ()
			{
				if (ReturnFalseGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool DirectGuard => TestFeatures.IsUnreferencedCodeSupported;

			static void TestDirectGuard ()
			{
				if (DirectGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
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
			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool AndGuard => TestFeatures.IsUnreferencedCodeSupported && OtherCondition ();

			static void TestAndGuard ()
			{
				if (AndGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool NotNotGuard => !!TestFeatures.IsUnreferencedCodeSupported;

			static void TestNotNotGuard ()
			{
				if (NotNotGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool EqualsTrueGuard => TestFeatures.IsUnreferencedCodeSupported == true;

			static void TestEqualsTrueGuard ()
			{
				if (EqualsTrueGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool TrueEqualsGuard => true == TestFeatures.IsUnreferencedCodeSupported;

			static void TestTrueEqualsGuard ()
			{
				if (TrueEqualsGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool NotEqualsFalseGuard => TestFeatures.IsUnreferencedCodeSupported != false;

			static void TestNotEqualsFalseGuard ()
			{
				if (NotEqualsFalseGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool FalseNotEqualsGuard => false != TestFeatures.IsUnreferencedCodeSupported;

			static void TestFalseNotEqualsGuard ()
			{
				if (FalseNotEqualsGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool IsTrueGuard => TestFeatures.IsUnreferencedCodeSupported is true;

			static void TestIsTrueGuard ()
			{
				if (IsTrueGuard)
					RequiresUnreferencedCode ();
			}

			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool IsNotFalseGuard => TestFeatures.IsUnreferencedCodeSupported is not false;

			static void TestIsNotFalseGuard ()
			{
				if (IsNotFalseGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool IfReturnTrueGuard {
				get {
					if (TestFeatures.IsUnreferencedCodeSupported)
						return true;
					return false;
				}
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
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

			[FeatureCheck (typeof (RequiresUnreferencedCodeAttribute))]
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

			[FeatureCheck (typeof (RequiresUnreferencedCodeAttribute))]
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

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof (RequiresUnreferencedCodeAttribute))]
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

			[FeatureCheck (typeof (RequiresUnreferencedCodeAttribute))]
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

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool TernaryIfGuard => TestFeatures.IsUnreferencedCodeSupported ? true : false;

			static void TestTernaryIfGuard ()
			{
				if (TernaryIfGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
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
			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool ReturnTrueGuard => true;

			static void TestReturnTrueGuard ()
			{
				if (ReturnTrueGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool OtherConditionGuard => OtherCondition ();

			static void TestOtherConditionGuard ()
			{
				if (OtherConditionGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool OrGuard => TestFeatures.IsUnreferencedCodeSupported || OtherCondition ();

			static void TestOrGuard ()
			{
				if (OrGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool NotGuard => !TestFeatures.IsUnreferencedCodeSupported;

			static void TestNotGuard ()
			{
				if (NotGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool EqualsFalseGuard => TestFeatures.IsUnreferencedCodeSupported == false;

			static void TestEqualsFalseGuard ()
			{
				if (EqualsFalseGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool FalseEqualsGuard => false == TestFeatures.IsUnreferencedCodeSupported;

			static void TestFalseEqualsGuard ()
			{
				if (FalseEqualsGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool NotEqualsTrueGuard => TestFeatures.IsUnreferencedCodeSupported != true;

			static void TestNotEqualsTrueGuard ()
			{
				if (NotEqualsTrueGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool TrueNotEqualsGuard => true != TestFeatures.IsUnreferencedCodeSupported;

			static void TestTrueNotEqualsGuard ()
			{
				if (TrueNotEqualsGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool IsNotTrueGuard => TestFeatures.IsUnreferencedCodeSupported is not true;

			static void TestIsNotTrueGuard ()
			{
				if (IsNotTrueGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static bool IsFalseGuard => TestFeatures.IsUnreferencedCodeSupported is false;

			static void TestIsFalseGuard ()
			{
				if (IsFalseGuard)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
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

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
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

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof (RequiresUnreferencedCodeAttribute))]
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

		class InvalidFeatureChecks {
			[ExpectedWarning ("IL4001", ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			static int NonBooleanProperty => 0;

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCodeAttribute))]
			static void TestNonBooleanProperty ()
			{
				if (NonBooleanProperty == 0)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4001", ProducedBy = Tool.Analyzer)]
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
			bool NonStaticProperty => true;

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCodeAttribute))]
			static void TestNonStaticProperty ()
			{
				var instance = new InvalidFeatureChecks ();
				if (instance.NonStaticProperty)
					RequiresUnreferencedCode ();
			}

			// No warning for this case because we don't validate that the attribute usage matches
			// the expected AttributeUsage.Property for assemblies that define their own version
			// of FeatureCheckAttributes.
			[FeatureCheck (typeof(RequiresUnreferencedCodeAttribute))]
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
