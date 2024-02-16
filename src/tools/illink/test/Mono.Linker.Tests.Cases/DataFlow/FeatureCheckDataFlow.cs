// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
	[IgnoreSubstitutions (false)]
	public class FeatureCheckDataFlow
	{
		public static void Main ()
		{
			CallFeatureUnguarded.Test ();
			CallFeatureGuarded.Test ();
			FeatureCheckBooleanExpressions.Test ();
			TestFeatureChecks.Test ();
			FeatureCheckCombinations.Test ();
			GuardedPatterns.Test ();
			ExceptionalDataFlow.Test ();
			CompilerGeneratedCodeDataflow.Test ();
		}

		class CallFeatureUnguarded
		{
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void Unguarded ()
			{
				RequiresUnreferencedCode ();
				RequiresDynamicCode ();
				RequiresAssemblyFiles ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedIf ()
			{
				if (!TestFeatures.IsUnreferencedCodeSupported) {
					RequiresUnreferencedCode ();
					RequiresDynamicCode ();
					RequiresAssemblyFiles ();
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedElse ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported)
				{
					throw new Exception ();
				}
				else
				{
					RequiresUnreferencedCode ();
					RequiresDynamicCode ();
					RequiresAssemblyFiles ();
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void UnguardedAnd ()
			{
				var a = !TestFeatures.IsUnreferencedCodeSupported && RequiresUnreferencedCodeBool ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void UnguardedOr ()
			{
				var a = TestFeatures.IsUnreferencedCodeSupported || RequiresUnreferencedCodeBool ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void UnguardedTernary ()
			{
				var a = TestFeatures.IsUnreferencedCodeSupported ? true : RequiresUnreferencedCodeBool ();
				var b = !TestFeatures.IsUnreferencedCodeSupported ? RequiresUnreferencedCodeBool () : true;
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedThrow ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported)
				{
					throw new Exception ();
				}

				RequiresUnreferencedCode ();
				RequiresDynamicCode ();
				RequiresAssemblyFiles ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedReturn ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported)
				{
					return;
				}

				RequiresUnreferencedCode ();
				RequiresDynamicCode ();
				RequiresAssemblyFiles ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void UnguardedAssert ()
			{
				Debug.Assert (!TestFeatures.IsUnreferencedCodeSupported);

				RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void UnguardedDoesNotReturnIfTrue ()
			{
				DoesNotReturnIfTrue (TestFeatures.IsUnreferencedCodeSupported);

				RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void UnguardedDoesNotReturnIfFalse ()
			{
				DoesNotReturnIfFalse (!TestFeatures.IsUnreferencedCodeSupported);

				RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void UnguardedDoesNotReturn ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported)
					DoesNotReturn ();

				RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void UnguardedDoesNotReturnIfFalseCtor ()
			{
				new DoesNotReturnIfFalseCtor (!TestFeatures.IsUnreferencedCodeSupported);

				RequiresUnreferencedCode ();
			}

			public static void Test ()
			{
				Unguarded ();
				UnguardedIf ();
				UnguardedElse ();
				UnguardedAnd ();
				UnguardedOr ();
				UnguardedTernary ();
				UnguardedThrow ();
				UnguardedReturn ();
				UnguardedAssert ();
				UnguardedDoesNotReturnIfTrue ();
				UnguardedDoesNotReturnIfFalse ();
				UnguardedDoesNotReturn ();
				UnguardedDoesNotReturnIfFalseCtor ();
			}
		}

		class CallFeatureGuarded
		{
			public static void Test ()
			{
				GuardedIf ();
				GuardedElse ();
				GuardedAnd ();
				GuardedOr ();
				GuardedTernary ();
				GuardedThrow ();
				GuardedReturn ();
				GuardedAssert ();
				GuardedDoesNotReturnIfTrue ();
				GuardedDoesNotReturnIfFalse ();
				GuardedDoesNotReturn ();
				GuardedDoesNotReturnIfFalseCtor ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.Analyzer)]
			static void GuardedIf ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					RequiresUnreferencedCode ();
					RequiresDynamicCode ();
					RequiresAssemblyFiles ();
				}
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.Analyzer)]
			static void GuardedElse ()
			{
				if (!TestFeatures.IsUnreferencedCodeSupported)
				{
					throw new Exception ();
				}
				else
				{
					RequiresUnreferencedCode ();
					RequiresDynamicCode ();
					RequiresAssemblyFiles ();
				}
			}

			static void GuardedAnd ()
			{
				var a = TestFeatures.IsUnreferencedCodeSupported && RequiresUnreferencedCodeBool ();
			}

			static void GuardedOr ()
			{
				var a = !TestFeatures.IsUnreferencedCodeSupported || RequiresUnreferencedCodeBool ();
			}

			static void GuardedTernary ()
			{
				var a = TestFeatures.IsUnreferencedCodeSupported ? RequiresUnreferencedCodeBool () : true;
				var b = !TestFeatures.IsUnreferencedCodeSupported ? true : RequiresUnreferencedCodeBool ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.Analyzer)]
			static void GuardedThrow ()
			{
				if (!TestFeatures.IsUnreferencedCodeSupported)
				{
					throw new Exception ();
				}

				RequiresUnreferencedCode ();
				RequiresDynamicCode ();
				RequiresAssemblyFiles ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.Analyzer)]
			static void GuardedReturn ()
			{
				if (!TestFeatures.IsUnreferencedCodeSupported)
				{
					return;
				}

				RequiresUnreferencedCode ();
				RequiresDynamicCode ();
				RequiresAssemblyFiles ();
			}

			// Trimmer/NativeAot don't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void GuardedAssert ()
			{
				Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);

				RequiresUnreferencedCode ();
			}

			// Trimmer/NativeAot don't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void GuardedDoesNotReturnIfTrue ()
			{
				DoesNotReturnIfTrue (!TestFeatures.IsUnreferencedCodeSupported);

				RequiresUnreferencedCode ();
			}

			// Trimmer/NativeAot don't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void GuardedDoesNotReturnIfFalse ()
			{
				DoesNotReturnIfFalse (TestFeatures.IsUnreferencedCodeSupported);

				RequiresUnreferencedCode ();
			}

			// Trimmer/NativeAot don't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void GuardedDoesNotReturn ()
			{
				if (!TestFeatures.IsUnreferencedCodeSupported)
					DoesNotReturn ();

				RequiresUnreferencedCode ();
			}

			// Trimmer/NativeAot don't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void GuardedDoesNotReturnIfFalseCtor ()
			{
				new DoesNotReturnIfFalseCtor (TestFeatures.IsUnreferencedCodeSupported);

				RequiresUnreferencedCode ();
			}
		}

		class FeatureCheckBooleanExpressions
		{
			// Trimmer/NativeAot aren't able to optimize away the branch in this case.
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void And ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported && RuntimeFeature.IsDynamicCodeSupported) {
					RequiresUnreferencedCode ();
					RequiresDynamicCode ();
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void AndNot ()
			{
				if (!TestFeatures.IsUnreferencedCodeSupported && !RuntimeFeature.IsDynamicCodeSupported)
					throw null;

				RequiresUnreferencedCode ();
				RequiresDynamicCode ();
			}

			// Trimmer/NativeAot aren't able to optimize away the branch in this case.
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void NotAnd ()
			{
				if (!(TestFeatures.IsUnreferencedCodeSupported && RuntimeFeature.IsDynamicCodeSupported))
					throw null;

				RequiresUnreferencedCode ();
				RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void Or ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported || RuntimeFeature.IsDynamicCodeSupported) {
					RequiresUnreferencedCode ();
					RequiresDynamicCode ();
				}
			}

			// Trimmer/NativeAot aren't able to optimize away the branch in this case.
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void OrNot ()
			{
				if (!TestFeatures.IsUnreferencedCodeSupported || !RuntimeFeature.IsDynamicCodeSupported)
					throw null;

				RequiresUnreferencedCode ();
				RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void NotOr ()
			{
				if (!(TestFeatures.IsUnreferencedCodeSupported || RuntimeFeature.IsDynamicCodeSupported))
					throw null;

				RequiresUnreferencedCode ();
				RequiresDynamicCode ();
			}

			static void EqualsTrue ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported == true)
					RequiresUnreferencedCode ();
			}

			static void TrueEquals ()
			{
				if (true == TestFeatures.IsUnreferencedCodeSupported)
					RequiresUnreferencedCode ();
			}

			static void EqualsFalse ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported == false)
					throw null;

				RequiresUnreferencedCode ();
			}

			static void FalseEquals ()
			{
				if (false == TestFeatures.IsUnreferencedCodeSupported)
					throw null;

				RequiresUnreferencedCode ();
			}

			static void NotEqualsTrue ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported != true)
					throw null;
					
				RequiresUnreferencedCode ();
			}

			static void NotEqualsFalse ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported != false)
					RequiresUnreferencedCode ();
			}

			static void TrueNotEquals ()
			{
				if (true != TestFeatures.IsUnreferencedCodeSupported)
					throw null;

				RequiresUnreferencedCode ();
			}

			static void FalseNotEquals ()
			{
				if (false != TestFeatures.IsUnreferencedCodeSupported)
					RequiresUnreferencedCode ();
			}

			static void IsTrue ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported is true)
					RequiresUnreferencedCode ();
			}

			static void IsFalse ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported is false)
					throw null;

				RequiresUnreferencedCode ();
			}

			static void IsNotTrue ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported is not true)
					throw null;

				RequiresUnreferencedCode ();
			}

			static void IsNotFalse ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported is not false)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void Contradiction ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported && !TestFeatures.IsUnreferencedCodeSupported) {
					RequiresUnreferencedCode ();
				} else {
					RequiresUnreferencedCode ();
				}
			}

			public static void Test ()
			{
				And ();
				AndNot ();
				NotAnd ();
				Or ();
				OrNot ();
				NotOr ();
				EqualsTrue ();
				TrueEquals ();
				EqualsFalse ();
				FalseEquals ();
				NotEqualsTrue ();
				NotEqualsFalse ();
				TrueNotEquals ();
				FalseNotEquals ();
				IsTrue ();
				IsFalse ();
				IsNotTrue ();
				IsNotFalse ();
				Contradiction ();
			}
		}

		class TestFeatureChecks
		{
			static void CallTestUnreferencedCodeGuarded ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					RequiresUnreferencedCode ();
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void CallTestUnreferencedCodeUnguarded ()
			{
				RequiresUnreferencedCode ();
			}

			static void CallTestDynamicCodeGuarded ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void CallTestDynamicCodeUnguarded ()
			{
				RequiresDynamicCode ();
			}

			static void CallTestAssemblyFilesGuarded ()
			{
				if (TestFeatures.IsAssemblyFilesSupported) {
					RequiresAssemblyFiles ();
				}
			}

			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void CallTestAssemblyFilesUnguarded ()
			{
				RequiresAssemblyFiles ();
			}

			public static void Test ()
			{
				CallTestUnreferencedCodeGuarded ();
				CallTestUnreferencedCodeUnguarded ();
				CallTestDynamicCodeGuarded ();
				CallTestDynamicCodeUnguarded ();
				CallTestAssemblyFilesGuarded ();
				CallTestAssemblyFilesUnguarded ();
			}
		}

		class FeatureCheckCombinations
		{
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer)]
			// Trimmer warns because IsDynamicCodeSupported is not a constant, so the call is reachable.
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Analyzer | Tool.Trimmer)]
			static void MeetFeaturesEmptyIntersection (bool b = true)
			{
				if (b) {
					if (!TestFeatures.IsUnreferencedCodeSupported)
						throw null;
				} else {
					if (!RuntimeFeature.IsDynamicCodeSupported)
						throw null;
				}
				RequiresUnreferencedCode ();
				RequiresDynamicCode ();
			}

			// Shows that ILLink has the same branch removal as NativeAot for this pattern, when
			// the branches both use a feature check that's substituted by ILLink.
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer)]
			static void MeetFeaturesEmptyIntersection_IdenticalBranches (bool b = true)
			{
				if (b) {
					if (!TestFeatures.IsUnreferencedCodeSupported)
						throw null;
				} else {
					if (!TestFeatures.IsUnreferencedCodeSupported)
						throw null;
				}
				RequiresUnreferencedCode ();
				RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer)]
			static void MeetFeaturesIntersection (bool b = true)
			{
				if (b) {
					if (!TestFeatures.IsUnreferencedCodeSupported)
						throw null;
				} else {
					if (!RuntimeFeature.IsDynamicCodeSupported)
						throw null;
					if (!TestFeatures.IsUnreferencedCodeSupported)
						throw null;
				}
				RequiresUnreferencedCode ();
				RequiresDynamicCode ();
			}

			static void IntroduceFeature ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					if (TestFeatures.IsAssemblyFilesSupported) {
						RequiresAssemblyFiles ();
						RequiresUnreferencedCode ();
					}
				}
			}

			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.Analyzer)]
			static void RemoveFeature ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					if (TestFeatures.IsAssemblyFilesSupported) {
					} else {
						RequiresAssemblyFiles ();
						RequiresUnreferencedCode ();
					}
				}
			}

			public static void Test ()
			{
				MeetFeaturesEmptyIntersection ();
				MeetFeaturesEmptyIntersection_IdenticalBranches ();
				MeetFeaturesIntersection ();
				IntroduceFeature ();
				RemoveFeature ();
			}
		}

		class GuardedPatterns
		{
			static void MethodCall (Type t)
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					RequiresAll (t);
				}
			}

			static void Assignment (Type t)
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					RequiresAllField = t;
				}
			}

			static void ReflectionAcces ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					Action<Type> a = RequiresAll;
				}
			}

			static void FieldAccess ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					int i = ClassWithRequires.StaticField;
				}
			}

			static void GenericRequirement<T> ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					new RequiresAllGeneric<T> ();
				}
			}

			public static void Test ()
			{
				MethodCall (typeof (int));
				Assignment (typeof (int));
				ReflectionAcces ();
				FieldAccess ();
				GenericRequirement<int> ();
			}
		}

		class ExceptionalDataFlow
		{
			static void GuardedTryCatchFinally ()
			{

				if (TestFeatures.IsUnreferencedCodeSupported)
				{
					try {
						RequiresUnreferencedCode0 ();
					} catch {
						RequiresUnreferencedCode1 ();
					} finally {
						RequiresUnreferencedCode2 ();
					}
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2))]
			static void CheckInTry ()
			{
				try {
					if (TestFeatures.IsUnreferencedCodeSupported)
						RequiresUnreferencedCode0 ();
				} catch {
					RequiresUnreferencedCode1 (); // should warn
				} finally {
					RequiresUnreferencedCode2 (); // should warn
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode3))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode4))]
			static void NestedTryInCheckInTry ()
			{
				try {
					if (TestFeatures.IsUnreferencedCodeSupported) {
						try {
							RequiresUnreferencedCode0 ();
						} catch {
							RequiresUnreferencedCode1 ();
						} finally {
							RequiresUnreferencedCode2 ();
						}
					}
				} catch {
					RequiresUnreferencedCode3 ();
				} finally {
					RequiresUnreferencedCode4 ();
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode4))]
			static void NestedTryInCheckInCatch ()
			{
				try {
					RequiresUnreferencedCode0 ();
				} catch {
					if (TestFeatures.IsUnreferencedCodeSupported) {
						try {
							RequiresUnreferencedCode1 ();
						} catch {
							RequiresUnreferencedCode2 ();
						} finally {
							RequiresUnreferencedCode3 ();
						}
					}
				} finally {
					RequiresUnreferencedCode4 ();
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1))]
			static void NestedTryInCheckInFinally ()
			{
				try {
					RequiresUnreferencedCode0 ();
				} catch {
					RequiresUnreferencedCode1 ();
				} finally {
					if (TestFeatures.IsUnreferencedCodeSupported) {
						try {
							RequiresUnreferencedCode2 ();
						} catch {
							RequiresUnreferencedCode3 ();
						} finally {
							RequiresUnreferencedCode4 ();
						}
					}
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			// Trimmer/NativeAot don't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void AssertInTryNoCatch () {
				try {
					Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
				} finally {
					RequiresUnreferencedCode0 ();
				}
				RequiresUnreferencedCode1 (); // Only reachable if assert succeeded.
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2))]
			static void AssertInTryWithCatch () {
				try {
					Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
				} catch {
					RequiresUnreferencedCode0 ();
				} finally {
					RequiresUnreferencedCode1 ();
				}
				RequiresUnreferencedCode2 ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2))]
			static void AssertInCatch () {
				try {
					RequiresUnreferencedCode0 ();
				} catch {
					Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
				} finally {
					RequiresUnreferencedCode1 ();
				}
				RequiresUnreferencedCode2 ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1))]
			// Trimmer/NativeAot don't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void AssertInFinally () {
				try {
					RequiresUnreferencedCode0 ();
				} catch {
					RequiresUnreferencedCode1 ();
				} finally {
					Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
				}
				RequiresUnreferencedCode2 (); // Only reachable if assert succeeded.
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2))]
			// Trimmer/NativeAot don't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode3), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void AssertInTryNestedInTry ()
			{
				try {
					try {
						Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
					} finally {
						RequiresUnreferencedCode0 ();
					}
					RequiresUnreferencedCode1 (); // Only reachable if assert succeeded.
				} finally {
					RequiresUnreferencedCode2 (); // warning, as expected.
				}

				RequiresUnreferencedCode3 (); // Only reachable if assert succeeded.
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode3))]
			static void AssertInTryWithCatchNestedInTry ()
			{
				try {
					try {
						Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
					} catch {
					} finally {
						RequiresUnreferencedCode0 ();
					}
					RequiresUnreferencedCode1 (); // Due to catch, this can be reached if assert failed.
				} finally {
					RequiresUnreferencedCode2 (); // warning, as expected.
				}

				RequiresUnreferencedCode3 (); // Same here.
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1))]
			// Trimmer/NativeAot don't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode3), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void AssertInTryNestedInFinally ()
			{
				try {
					RequiresUnreferencedCode0 ();
				} finally {
					try {
						Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
					} finally {
						RequiresUnreferencedCode1 ();
					}
					RequiresUnreferencedCode2 (); // Only reachable if assert succeeded.
				}
				RequiresUnreferencedCode3 (); // Only reachable if assert succeeded.
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode3))]
			static void AssertInTryWithCatchNestedInFinally ()
			{
				try {
					RequiresUnreferencedCode0 ();
				} finally {
					try {
						Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
					} catch {
					} finally {
						RequiresUnreferencedCode1 ();
					}
					RequiresUnreferencedCode2 (); // Due to catch, this can be reached if assert failed.
				}
				RequiresUnreferencedCode3 (); // Same here.
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2))]
			// Trimmer/NativeAot don't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode3), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void AssertInFinallyNestedInTry () {
				try {
					try {
						RequiresUnreferencedCode0 ();
					} finally {
						Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
					}
					RequiresUnreferencedCode1 (); // Only reachable if assert succeeded.
				} finally {
					RequiresUnreferencedCode2 ();
				}
				RequiresUnreferencedCode3 (); // Only reachable if assert succeeded.
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2))]
			// Trimmer/NativeAot don't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode3), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void AssertInFinallyWithCatchNestedInTry () {
				try {
					try {
						RequiresUnreferencedCode0 ();
					} catch { // This catch makes no difference to the result.
					} finally {
						Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
					}
					RequiresUnreferencedCode1 (); // Only reachable if assert succeeded.
				} finally {
					RequiresUnreferencedCode2 ();
				}
				RequiresUnreferencedCode3 (); // Only reachable if assert succeeded.
			}


			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1))]
			// Trimmer/NativeAot don't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode3), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void AssertInFinallyNestedInFinally ()
			{
				try {
					RequiresUnreferencedCode0 ();
				} finally {
					try {
						RequiresUnreferencedCode1 ();
					} finally {
						Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
					}
					RequiresUnreferencedCode2 (); // Only reachable if assert succeeded.
				}
				RequiresUnreferencedCode3 (); // Only reachable if assertc succeeded.
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1))]
			// Trimmer/NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode3), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void AssertInFinallyWithCatchNestedInFinally ()
			{
				try {
					RequiresUnreferencedCode0 ();
				} finally {
					try {
						RequiresUnreferencedCode1 ();
					} catch { // This catch makes no difference to the result.
					} finally {
						Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
					}
					RequiresUnreferencedCode2 (); // Only reachable if assert succeeded.
				}
				RequiresUnreferencedCode3 (); // Only reachable if assertc succeeded.
			}


			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode0))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode1))]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode2))]
			static void AssertInTryWithTryFinallyInFinally ()
			{
				try {
					Debug.Assert (TestFeatures.IsUnreferencedCodeSupported);
				} finally {
					try {
						RequiresUnreferencedCode0 ();
					} finally {
						RequiresUnreferencedCode1 ();
					}
					RequiresUnreferencedCode2 ();
				}
			}

			public static void Test () {
				GuardedTryCatchFinally ();
				CheckInTry ();
				NestedTryInCheckInTry ();
				NestedTryInCheckInCatch ();
				NestedTryInCheckInFinally ();

				AssertInTryWithCatch ();
				AssertInTryNoCatch ();
				AssertInCatch ();
				AssertInFinally ();

				AssertInTryNestedInTry ();
				AssertInTryWithCatchNestedInTry ();
				AssertInTryNestedInFinally ();
				AssertInTryWithCatchNestedInFinally ();
				AssertInFinallyNestedInTry ();
				AssertInFinallyWithCatchNestedInTry ();
				AssertInFinallyNestedInFinally ();
				AssertInFinallyWithCatchNestedInFinally ();

				AssertInTryWithTryFinallyInFinally ();
			}
		}

		class CompilerGeneratedCodeDataflow
		{
			static IEnumerable<int> GuardInIterator ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					RequiresUnreferencedCode ();
					yield return 0;
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer,
				CompilerGeneratedCode = true)]
			static IEnumerable<int> StateFlowsAcrossYield ()
			{
				if (!TestFeatures.IsUnreferencedCodeSupported)
					yield break;

				yield return 0;

				RequiresUnreferencedCode ();
			}

			static async Task GuardInAsync ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					RequiresUnreferencedCode ();
					await Task.Yield ();
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot,
				CompilerGeneratedCode = true)]
			static async Task StateFlowsAcrossAwait ()
			{
				if (!TestFeatures.IsUnreferencedCodeSupported)
					return;

				await Task.Yield ();

				RequiresUnreferencedCode ();
			}

			static async IAsyncEnumerable<int> GuardInAsyncIterator ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					RequiresUnreferencedCode ();
					await Task.Yield ();
					yield return 0;
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot,
				CompilerGeneratedCode = true)]
			static async IAsyncEnumerable<int> StateFlowsAcrossAwaitAndYield ()
			{
				if (!TestFeatures.IsUnreferencedCodeSupported)
					yield break;

				await Task.Yield ();

				yield return 0;

				RequiresUnreferencedCode ();
			}

			static void GuardInLambda ()
			{
				Action a = () => {
					if (TestFeatures.IsUnreferencedCodeSupported)
						RequiresUnreferencedCode ();
				};
				a ();
			}

			static void GuardInLocalFunction ()
			{
				void LocalFunction ()
				{
					if (TestFeatures.IsUnreferencedCodeSupported)
						RequiresUnreferencedCode ();
				}
				LocalFunction ();
			}

			static void GuardedLambda ()
			{
				Action a = null;

				if (TestFeatures.IsUnreferencedCodeSupported) {
					a = [RequiresUnreferencedCode (nameof (RequiresUnreferencedCode))]
						() => RequiresUnreferencedCode ();
				}

				if (TestFeatures.IsUnreferencedCodeSupported) {
					a ();
				}
			}

			static void GuardedLocalFunction ()
			{
				[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode))]
				void LocalFunction () => RequiresUnreferencedCode ();

				if (TestFeatures.IsUnreferencedCodeSupported)
					LocalFunction ();
			}

			public static void Test ()
			{
				GuardInIterator ();
				StateFlowsAcrossYield ();
				GuardInAsync ();
				StateFlowsAcrossAwait ();
				GuardInAsyncIterator ();
				StateFlowsAcrossAwaitAndYield ();
				GuardInLambda ();
				GuardInLocalFunction ();
				GuardedLambda ();
				GuardedLocalFunction ();
			}
		}

		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode))]
		static void RequiresUnreferencedCode () {}

		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode0))]
		static void RequiresUnreferencedCode0 () {}

		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode1))]
		static void RequiresUnreferencedCode1 () {}

		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode2))]
		static void RequiresUnreferencedCode2 () {}

		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode3))]
		static void RequiresUnreferencedCode3 () {}

		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode4))]
		static void RequiresUnreferencedCode4 () {}

		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCodeBool))]
		static bool RequiresUnreferencedCodeBool () => true;

		[RequiresDynamicCode (nameof (RequiresUnreferencedCode))]
		static void RequiresDynamicCode () {}

		[RequiresAssemblyFiles (nameof (RequiresAssemblyFiles))]
		static void RequiresAssemblyFiles () {}

		static void DoesNotReturnIfTrue ([DoesNotReturnIf (true)] bool condition) {}

		static void DoesNotReturnIfFalse ([DoesNotReturnIf (false)] bool condition) {}

		class DoesNotReturnIfFalseCtor
		{
			public DoesNotReturnIfFalseCtor ([DoesNotReturnIf (false)] bool condition) {}
		}

		[DoesNotReturn]
		static void DoesNotReturn() {}

		static void RequiresAll([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type t) {}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		static Type RequiresAllField;

		[RequiresUnreferencedCode (nameof (ClassWithRequires))]
		class ClassWithRequires
		{
			public static int StaticField = 0;
		}

		class RequiresAllGeneric<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T> {}
	}
}

namespace System.Runtime.CompilerServices
{
	class RuntimeFeature
	{
		[FeatureCheck (typeof(RequiresDynamicCodeAttribute))]
		public static bool IsDynamicCodeSupported => true;
	}
}
