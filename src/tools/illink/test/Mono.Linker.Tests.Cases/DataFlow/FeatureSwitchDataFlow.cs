// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class FeatureSwitchDataFlow
	{
		public static void Main ()
		{
			CallFeatureUnguarded.Test ();
			CallFeatureGuarded.Test ();
			FeatureCheckBooleanExpressions.Test ();
			SupportedFeatureChecks.Test ();
			FeatureCheckCombinations.Test ();
			GuardedPatterns.Test ();
			ExceptionalDataFlow.Test ();
		}

		class CallFeatureUnguarded
		{
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void Unguarded ()
			{
				RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedIf ()
			{
				if (!RuntimeFeature.IsDynamicCodeSupported)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedElse ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported)
				{
					throw new Exception ();
				}
				else
				{
					RequiresDynamicCode ();
				}
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedAnd ()
			{
				var a = !RuntimeFeature.IsDynamicCodeSupported && RequiresDynamicCodeBool ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedOr ()
			{
				var a = RuntimeFeature.IsDynamicCodeSupported || RequiresDynamicCodeBool ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedTernary ()
			{
				var a = RuntimeFeature.IsDynamicCodeSupported ? true : RequiresDynamicCodeBool ();
				var b = !RuntimeFeature.IsDynamicCodeSupported ? RequiresDynamicCodeBool () : true;
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedThrow ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported)
				{
					throw new Exception ();
				}

				RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedAssert ()
			{
				Debug.Assert (!RuntimeFeature.IsDynamicCodeSupported);

				RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedDoesNotReturnIfTrue ()
			{
				DoesNotReturnIfTrue (RuntimeFeature.IsDynamicCodeSupported);

				RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedDoesNotReturnIfFalse ()
			{
				DoesNotReturnIfFalse (!RuntimeFeature.IsDynamicCodeSupported);

				RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void UnguardedDoesNotReturn ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported)
					DoesNotReturn ();

				RequiresDynamicCode ();
			}

			// NativeAot doesn't optimize branches away based on DoesNotReturnIfFalse
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void UnguardedDoesNotReturnIfFalseCtor ()
			{
				new DoesNotReturnIfFalseCtor (RuntimeFeature.IsDynamicCodeSupported);

				RequiresDynamicCode ();
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
				GuardedAssert ();
				GuardedDoesNotReturnIfTrue ();
				GuardedDoesNotReturnIfFalse ();
				GuardedDoesNotReturn ();
				GuardedDoesNotReturnIfFalseCtor ();
			}

			static void GuardedIf ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported)
					RequiresDynamicCode ();
			}

			static void GuardedElse ()
			{
				if (!RuntimeFeature.IsDynamicCodeSupported)
				{
					throw new Exception ();
				}
				else
				{
					RequiresDynamicCode ();
				}
			}

			static void GuardedAnd ()
			{
				var a = RuntimeFeature.IsDynamicCodeSupported && RequiresDynamicCodeBool ();
			}

			static void GuardedOr ()
			{
				var a = !RuntimeFeature.IsDynamicCodeSupported || RequiresDynamicCodeBool ();
			}

			static void GuardedTernary ()
			{
				var a = RuntimeFeature.IsDynamicCodeSupported ? RequiresDynamicCodeBool () : true;
				var b = !RuntimeFeature.IsDynamicCodeSupported ? true : RequiresDynamicCodeBool ();
			}

			static void GuardedThrow ()
			{
				if (!RuntimeFeature.IsDynamicCodeSupported)
				{
					throw new Exception ();
				}

				RequiresDynamicCode ();
			}

			// NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void GuardedAssert ()
			{
				Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);

				RequiresDynamicCode ();
			}

			// NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void GuardedDoesNotReturnIfTrue ()
			{
				DoesNotReturnIfTrue (!RuntimeFeature.IsDynamicCodeSupported);

				RequiresDynamicCode ();
			}

			// NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void GuardedDoesNotReturnIfFalse ()
			{
				DoesNotReturnIfFalse (RuntimeFeature.IsDynamicCodeSupported);

				RequiresDynamicCode ();
			}

			// NativeAot doesn't optimize branches away based on DoesNotReturnAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void GuardedDoesNotReturn ()
			{
				if (!RuntimeFeature.IsDynamicCodeSupported)
					DoesNotReturn ();

				RequiresDynamicCode ();
			}

			// NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void GuardedDoesNotReturnIfFalseCtor ()
			{
				new DoesNotReturnIfFalseCtor (RuntimeFeature.IsDynamicCodeSupported);

				RequiresDynamicCode ();
			}
		}

		class FeatureCheckBooleanExpressions
		{
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void And ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported && TestFeatures.IsUnreferencedCodeSupported) {
					RequiresDynamicCode ();
					RequiresUnreferencedCode ();
				}
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void AndNot ()
			{
				if (!RuntimeFeature.IsDynamicCodeSupported && !TestFeatures.IsUnreferencedCodeSupported)
					throw null;

				RequiresDynamicCode ();
				RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void NotAnd ()
			{
				if (!(RuntimeFeature.IsDynamicCodeSupported && TestFeatures.IsUnreferencedCodeSupported))
					throw null;

				RequiresDynamicCode ();
				RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void Or ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported || TestFeatures.IsUnreferencedCodeSupported) {
					RequiresDynamicCode ();
					RequiresUnreferencedCode ();
				}
			}

			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void OrNot ()
			{
				if (!RuntimeFeature.IsDynamicCodeSupported || !TestFeatures.IsUnreferencedCodeSupported)
					throw null;

				RequiresDynamicCode ();
				RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void NotOr ()
			{
				if (!(RuntimeFeature.IsDynamicCodeSupported || TestFeatures.IsUnreferencedCodeSupported))
					throw null;

				RequiresDynamicCode ();
				RequiresUnreferencedCode ();
			}

			static void EqualsTrue ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported == true)
					RequiresDynamicCode ();
			}

			static void TrueEquals ()
			{
				if (true == RuntimeFeature.IsDynamicCodeSupported)
					RequiresDynamicCode ();
			}

			static void EqualsFalse ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported == false)
					throw null;

				RequiresDynamicCode ();
			}

			static void FalseEquals ()
			{
				if (false == RuntimeFeature.IsDynamicCodeSupported)
					throw null;

				RequiresDynamicCode ();
			}

			static void NotEqualsTrue ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported != true)
					throw null;
					
				RequiresDynamicCode ();
			}

			static void NotEqualsFalse ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported != false)
					RequiresDynamicCode ();
			}

			static void TrueNotEquals ()
			{
				if (true != RuntimeFeature.IsDynamicCodeSupported)
					throw null;

				RequiresDynamicCode ();
			}

			static void FalseNotEquals ()
			{
				if (false != RuntimeFeature.IsDynamicCodeSupported)
					RequiresDynamicCode ();
			}

			static void IsTrue ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported is true)
					RequiresDynamicCode ();
			}

			static void IsFalse ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported is false)
					throw null;

				RequiresDynamicCode ();
			}

			static void IsNotTrue ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported is not true)
					throw null;

				RequiresDynamicCode ();
			}

			static void IsNotFalse ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported is not false)
					RequiresDynamicCode ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.NativeAot)]
			static void Contradiction ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported && !RuntimeFeature.IsDynamicCodeSupported) {
					RequiresDynamicCode ();
				} else {
					RequiresDynamicCode ();
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

		class SupportedFeatureChecks
		{
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void CallTestUnreferencedCodeGuarded ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					RequiresUnreferencedCode ();
				}
			}

			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.NativeAot)]
			static void CallTestAssemblyFilesGuarded ()
			{
				if (TestFeatures.IsAssemblyFilesSupported)
					RequiresAssemblyFiles ();
			}

			public static void Test ()
			{
				CallTestUnreferencedCodeGuarded ();
				CallTestAssemblyFilesGuarded ();
			}
		}

		class FeatureCheckCombinations
		{
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode))]
			static void MeetFeaturesEmptyIntersection (bool b = true)
			{
				if (b) {
					if (!RuntimeFeature.IsDynamicCodeSupported)
						throw null;
				} else {
					if (!TestFeatures.IsUnreferencedCodeSupported)
						throw null;
				}
				RequiresDynamicCode ();
				RequiresUnreferencedCode ();
			}

			// NativeAot assumes that IsDynamicCodeSupported returns false.
			[ExpectedWarning ("IL2026", nameof (RequiresUnreferencedCode), ProducedBy = Tool.Analyzer | Tool.Trimmer)]
			static void MeetFeaturesIntersection (bool b = true)
			{
				if (b) {
					if (!RuntimeFeature.IsDynamicCodeSupported)
						throw null;
				} else {
					if (!TestFeatures.IsUnreferencedCodeSupported)
						throw null;
					if (!RuntimeFeature.IsDynamicCodeSupported)
						throw null;
				}
				RequiresDynamicCode ();
				RequiresUnreferencedCode ();
			}

			static void IntroduceFeature ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported) {
					if (TestFeatures.IsAssemblyFilesSupported) {
						RequiresAssemblyFiles ();
						RequiresDynamicCode ();
					}
				}
			}

			// NativeAot assumes that IsDynamicCodeSupported returns false.
			[ExpectedWarning ("IL3002", nameof (RequiresAssemblyFiles), ProducedBy = Tool.Analyzer)]
			static void RemoveFeature ()
			{
				if (RuntimeFeature.IsDynamicCodeSupported) {
					if (TestFeatures.IsAssemblyFilesSupported) {
					} else {
						RequiresAssemblyFiles ();
						RequiresDynamicCode ();
					}
				}
			}

			public static void Test ()
			{
				MeetFeaturesEmptyIntersection ();
				MeetFeaturesIntersection ();
				IntroduceFeature ();
				RemoveFeature ();
			}
		}

		class GuardedPatterns
		{
			[ExpectedWarning ("IL2067", nameof (RequiresAll), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void MethodCall (Type t)
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					RequiresAll (t);
				}
			}

			[ExpectedWarning ("IL2069", nameof (RequiresAllField), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void Assignment (Type t)
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					RequiresAllField = t;
				}
			}

			[ExpectedWarning ("IL2111", nameof (RequiresAll), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void ReflectionAcces ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					Action<Type> a = RequiresAll;
				}
			}

			[ExpectedWarning ("IL2026", nameof (ClassWithRequires.StaticField), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void FieldAccess ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported) {
					int i = ClassWithRequires.StaticField;
				}
			}

			// TODO: move generic analysis to dataflow analyzer to support this if it is an actual scenario.
			[ExpectedWarning ("IL2091", nameof (RequiresAllGeneric<T>))]
			static void GenericRequirement<T> ()
			{
				if (TestFeatures.IsUnreferencedCodeSupported)
					new RequiresAllGeneric<T> ();
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

				if (RuntimeFeature.IsDynamicCodeSupported)
				{
					try {
						RequiresDynamicCode ();
					} catch {
						RequiresDynamicCode ();
					} finally {
						RequiresDynamicCode ();
					}
				}
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void CheckInTry ()
			{
				try {
					if (RuntimeFeature.IsDynamicCodeSupported)
						RequiresDynamicCode ();
				} catch {
					RequiresDynamicCode (); // should warn
				} finally {
					RequiresDynamicCode (); // should warn
				}
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode3), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode4), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void NestedTryInCheckInTry ()
			{
				try {
					if (RuntimeFeature.IsDynamicCodeSupported) {
						try {
							RequiresDynamicCode0 ();
						} catch {
							RequiresDynamicCode1 ();
						} finally {
							RequiresDynamicCode2 ();
						}
					}
				} catch {
					RequiresDynamicCode3 ();
				} finally {
					RequiresDynamicCode4 ();
				}
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode4), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void NestedTryInCheckInCatch ()
			{
				try {
					RequiresDynamicCode0 ();
				} catch {
					if (RuntimeFeature.IsDynamicCodeSupported) {
						try {
							RequiresDynamicCode1 ();
						} catch {
							RequiresDynamicCode2 ();
						} finally {
							RequiresDynamicCode3 ();
						}
					}
				} finally {
					RequiresDynamicCode4 ();
				}
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void NestedTryInCheckInFinally ()
			{
				try {
					RequiresDynamicCode0 ();
				} catch {
					RequiresDynamicCode1 ();
				} finally {
					if (RuntimeFeature.IsDynamicCodeSupported) {
						try {
							RequiresDynamicCode2 ();
						} catch {
							RequiresDynamicCode3 ();
						} finally {
							RequiresDynamicCode4 ();
						}
					}
				}
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			// NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.NativeAot)]
			static void AssertInTryNoCatch () {
				try {
					Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
				} finally {
					RequiresDynamicCode0 ();
				}
				RequiresDynamicCode1 (); // Only reachable if assert succeeded.
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode2), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void AssertInTryWithCatch () {
				try {
					Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
				} catch {
					RequiresDynamicCode0 ();
				} finally {
					RequiresDynamicCode1 ();
				}
				RequiresDynamicCode2 ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode2), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void AssertInCatch () {
				try {
					RequiresDynamicCode0 ();
				} catch {
					Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
				} finally {
					RequiresDynamicCode1 ();
				}
				RequiresDynamicCode2 ();
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			// NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode2), ProducedBy = Tool.NativeAot)]
			static void AssertInFinally () {
				try {
					RequiresDynamicCode0 ();
				} catch {
					RequiresDynamicCode1 ();
				} finally {
					Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
				}
				RequiresDynamicCode2 (); // Only reachable if assert succeeded.
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode2), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			// NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode3), ProducedBy = Tool.NativeAot)]
			static void AssertInTryNestedInTry ()
			{
				try {
					try {
						Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
					} finally {
						RequiresDynamicCode0 ();
					}
					RequiresDynamicCode1 (); // Only reachable if assert succeeded.
				} finally {
					RequiresDynamicCode2 (); // warning, as expected.
				}

				RequiresDynamicCode3 (); // Only reachable if assert succeeded.
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode2), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode3), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void AssertInTryWithCatchNestedInTry ()
			{
				try {
					try {
						Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
					} catch {
					} finally {
						RequiresDynamicCode0 ();
					}
					RequiresDynamicCode1 (); // Due to catch, this can be reached if assert failed.
				} finally {
					RequiresDynamicCode2 (); // warning, as expected.
				}

				RequiresDynamicCode3 (); // Same here.
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			// NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode2), ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode3), ProducedBy = Tool.NativeAot)]
			static void AssertInTryNestedInFinally ()
			{
				try {
					RequiresDynamicCode0 ();
				} finally {
					try {
						Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
					} finally {
						RequiresDynamicCode1 ();
					}
					RequiresDynamicCode2 (); // Only reachable if assert succeeded.
				}
				RequiresDynamicCode3 (); // Only reachable if assert succeeded.
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode2), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode3), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void AssertInTryWithCatchNestedInFinally ()
			{
				try {
					RequiresDynamicCode0 ();
				} finally {
					try {
						Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
					} catch {
					} finally {
						RequiresDynamicCode1 ();
					}
					RequiresDynamicCode2 (); // Due to catch, this can be reached if assert failed.
				}
				RequiresDynamicCode3 (); // Same here.
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode2), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			// NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode3), ProducedBy = Tool.NativeAot)]
			static void AssertInFinallyNestedInTry () {
				try {
					try {
						RequiresDynamicCode0 ();
					} finally {
						Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
					}
					RequiresDynamicCode1 (); // Only reachable if assert succeeded.
				} finally {
					RequiresDynamicCode2 ();
				}
				RequiresDynamicCode3 (); // Only reachable if assert succeeded.
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode2), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			// NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode3), ProducedBy = Tool.NativeAot)]
			static void AssertInFinallyWithCatchNestedInTry () {
				try {
					try {
						RequiresDynamicCode0 ();
					} catch { // This catch makes no difference to the result.
					} finally {
						Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
					}
					RequiresDynamicCode1 (); // Only reachable if assert succeeded.
				} finally {
					RequiresDynamicCode2 ();
				}
				RequiresDynamicCode3 (); // Only reachable if assert succeeded.
			}


			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			// NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode2), ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode3), ProducedBy = Tool.NativeAot)]
			static void AssertInFinallyNestedInFinally ()
			{
				try {
					RequiresDynamicCode0 ();
				} finally {
					try {
						RequiresDynamicCode1 ();
					} finally {
						Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
					}
					RequiresDynamicCode2 (); // Only reachable if assert succeeded.
				}
				RequiresDynamicCode3 (); // Only reachable if assertc succeeded.
			}

			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			// NativeAot doesn't optimize branches away based on DoesNotReturnIfAttribute
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode2), ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode3), ProducedBy = Tool.NativeAot)]
			static void AssertInFinallyWithCatchNestedInFinally ()
			{
				try {
					RequiresDynamicCode0 ();
				} finally {
					try {
						RequiresDynamicCode1 ();
					} catch { // This catch makes no difference to the result.
					} finally {
						Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
					}
					RequiresDynamicCode2 (); // Only reachable if assert succeeded.
				}
				RequiresDynamicCode3 (); // Only reachable if assertc succeeded.
			}


			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode0), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode1), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", nameof (RequiresDynamicCode2), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void AssertInTryWithTryFinallyInFinally ()
			{
				try {
					Debug.Assert (RuntimeFeature.IsDynamicCodeSupported);
				} finally {
					try {
						RequiresDynamicCode0 ();
					} finally {
						RequiresDynamicCode1 ();
					}
					RequiresDynamicCode2 ();
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


		[RequiresDynamicCode (nameof (RequiresDynamicCode))]
		static void RequiresDynamicCode () {}

		[RequiresDynamicCode (nameof (RequiresDynamicCode0))]
		static void RequiresDynamicCode0 () {}

		[RequiresDynamicCode (nameof (RequiresDynamicCode1))]
		static void RequiresDynamicCode1 () {}

		[RequiresDynamicCode (nameof (RequiresDynamicCode2))]
		static void RequiresDynamicCode2 () {}

		[RequiresDynamicCode (nameof (RequiresDynamicCode3))]
		static void RequiresDynamicCode3 () {}

		[RequiresDynamicCode (nameof (RequiresDynamicCode4))]
		static void RequiresDynamicCode4 () {}

		[RequiresDynamicCode (nameof (RequiresDynamicCodeBool))]
		static bool RequiresDynamicCodeBool () => true;

		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode))]
		static void RequiresUnreferencedCode () {}

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

	class TestFeatures
	{
		public static bool IsUnreferencedCodeSupported => true;
		public static bool IsAssemblyFilesSupported => true;
	}
}
