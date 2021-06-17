// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class RequiresUnreferencedCodeInCompilerGeneratedCode
	{
		public static void Main ()
		{
			WarnInIteratorBody.Test ();
			SuppressInIteratorBody.Test ();

			WarnInAsyncBody.Test ();
			SuppressInAsyncBody.Test ();

			WarnInAsyncIteratorBody.Test ();
			SuppressInAsyncIteratorBody.Test ();

			WarnInLocalFunction.Test ();
			SuppressInLocalFunction.Test ();

			WarnInLambda.Test ();
			SuppressInLambda.Test ();

			WarnInComplex.Test ();
			SuppressInComplex.Test ();

			StateMachinesOnlyReferencedViaReflection.Test ();

			ComplexCases.AsyncBodyCallingRUCMethod.Test ();
			ComplexCases.GenericAsyncBodyCallingRUCMethod.Test ();
			ComplexCases.GenericAsyncEnumerableBodyCallingRUCWithAnnotations.Test ();
		}

		class WarnInIteratorBody
		{
			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestCallBeforeYieldReturn ()
			{
				RequiresUnreferencedCodeMethod ();
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestCallAfterYieldReturn ()
			{
				yield return 0;
				RequiresUnreferencedCodeMethod ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static IEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				yield return 1;
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestLdftn ()
			{
				yield return 0;
				yield return 1;
				var action = new Action (RequiresUnreferencedCodeMethod);
			}

			[ExpectedWarning ("IL2026", "--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static IEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				yield return 0;
				yield return 1;
			}

			public static void Test ()
			{
				TestCallBeforeYieldReturn ();
				TestCallAfterYieldReturn ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInIteratorBody
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestCall ()
			{
				RequiresUnreferencedCodeMethod ();
				yield return 0;
				RequiresUnreferencedCodeMethod ();
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestLdftn ()
			{
				yield return 0;
				yield return 1;
				var action = new Action (RequiresUnreferencedCodeMethod);
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				yield return 0;
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestGenericMethodParameterRequirement<TUnknown> ()
			{
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				yield return 0;
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestCall ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInAsyncBody
		{
			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static async void TestCallBeforeYieldReturn ()
			{
				RequiresUnreferencedCodeMethod ();
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static async void TestCallAfterYieldReturn ()
			{
				await MethodAsync ();
				RequiresUnreferencedCodeMethod ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static async void TestReflectionAccess ()
			{
				await MethodAsync ();
				typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static async void TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (RequiresUnreferencedCodeMethod);
			}

			[ExpectedWarning ("IL2026", "--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static async void TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			public static void Test ()
			{
				TestCallBeforeYieldReturn ();
				TestCallAfterYieldReturn ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInAsyncBody
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestCall ()
			{
				RequiresUnreferencedCodeMethod ();
				await MethodAsync ();
				RequiresUnreferencedCodeMethod ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestReflectionAccess ()
			{
				await MethodAsync ();
				typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (RequiresUnreferencedCodeMethod);
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				await MethodAsync ();
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestCall ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInAsyncIteratorBody
		{
			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static async IAsyncEnumerable<int> TestCallBeforeYieldReturn ()
			{
				await MethodAsync ();
				RequiresUnreferencedCodeMethod ();
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static async IAsyncEnumerable<int> TestCallAfterYieldReturn ()
			{
				yield return 0;
				RequiresUnreferencedCodeMethod ();
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static async IAsyncEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				await MethodAsync ();
				typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
				yield return 1;
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static async IAsyncEnumerable<int> TestLdftn ()
			{
				await MethodAsync ();
				yield return 0;
				var action = new Action (RequiresUnreferencedCodeMethod);
			}

			[ExpectedWarning ("IL2026", "--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static async IAsyncEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				yield return 0;
				await MethodAsync ();
			}

			public static void Test ()
			{
				TestCallBeforeYieldReturn ();
				TestCallAfterYieldReturn ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInAsyncIteratorBody
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestCall ()
			{
				RequiresUnreferencedCodeMethod ();
				await MethodAsync ();
				yield return 0;
				RequiresUnreferencedCodeMethod ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestReflectionAccess ()
			{
				await MethodAsync ();
				yield return 0;
				typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (RequiresUnreferencedCodeMethod);
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				yield return 0;
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				await MethodAsync ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestGenericMethodParameterRequirement<TUnknown> ()
			{
				yield return 0;
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				yield return 0;
				await MethodAsync ();
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestCall ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInLocalFunction
		{
			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static void TestCall ()
			{
				LocalFunction ();

				void LocalFunction () => RequiresUnreferencedCodeMethod ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static void TestCallWithClosure (int p = 0)
			{
				LocalFunction ();

				void LocalFunction ()
				{
					p++;
					RequiresUnreferencedCodeMethod ();
				}
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static void TestReflectionAccess ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static void TestLdftn ()
			{
				LocalFunction ();

				void LocalFunction ()
				{
					var action = new Action (RequiresUnreferencedCodeMethod);
				}
			}

			[ExpectedWarning ("IL2026", "--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static void TestDynamicallyAccessedMethod ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
			}

			public static void Test ()
			{
				TestCall ();
				TestCallWithClosure ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInLocalFunction
		{
			// RequiresUnreferencedCode doesn't propagate into local functions yet
			// so its suppression effect also doesn't propagate

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCall ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction () => RequiresUnreferencedCodeMethod ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCallWithClosure (int p = 0)
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction ()
				{
					p++;
					RequiresUnreferencedCodeMethod ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestReflectionAccess ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction () => typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestLdftn ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction ()
				{
					var action = new Action (RequiresUnreferencedCodeMethod);
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestDynamicallyAccessedMethod ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction () => typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				LocalFunction ();

				[ExpectedWarning ("IL2077")]
				void LocalFunction () => unknownType.RequiresNonPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2091")]
				void LocalFunction () => MethodWithGenericWhichRequiresMethods<TUnknown> ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2091")]
				void LocalFunction () => new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericLocalFunction<TUnknown> ()
			{
				LocalFunction<TUnknown> ();

				void LocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
				{
					typeof (T).RequiresPublicMethods ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericLocalFunctionInner<TUnknown> ()
			{
				LocalFunction<TUnknown> ();

				[ExpectedWarning ("IL2087")]
				void LocalFunction<TSecond> ()
				{
					typeof (TUnknown).RequiresPublicMethods ();
					typeof (TSecond).RequiresPublicMethods ();
				}
			}

			static void TestGenericLocalFunctionWithAnnotations<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
			{
				LocalFunction<TPublicMethods> ();

				void LocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TInnerPublicMethods> ()
				{
					typeof (TPublicMethods).RequiresPublicMethods ();
					typeof (TInnerPublicMethods).RequiresPublicMethods ();
				}
			}

			static void TestGenericLocalFunctionWithAnnotationsAndClosure<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> (int p = 0)
			{
				LocalFunction<TPublicMethods> ();

				void LocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TInnerPublicMethods> ()
				{
					p++;
					typeof (TPublicMethods).RequiresPublicMethods ();
					typeof (TInnerPublicMethods).RequiresPublicMethods ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCallRUCMethodInLtftnLocalFunction ()
			{
				var _ = new Action (LocalFunction);

				[ExpectedWarning ("IL2026")]
				void LocalFunction () => RequiresUnreferencedCodeMethod ();
			}

			class DynamicallyAccessedLocalFunction
			{
				[RequiresUnreferencedCode ("Suppress in body")]
				public static void TestCallRUCMethodInDynamicallyAccessedLocalFunction ()
				{
					typeof (DynamicallyAccessedLocalFunction).RequiresNonPublicMethods ();

					[ExpectedWarning ("IL2026")]
					void LocalFunction () => RequiresUnreferencedCodeMethod ();
				}
			}

			[ExpectedWarning ("IL2026")]
			static void TestSuppressionLocalFunction ()
			{
				LocalFunction (); // This will produce a warning since the location function has RUC on it

				[RequiresUnreferencedCode ("Suppress in body")]
				void LocalFunction (Type unknownType = null)
				{
					RequiresUnreferencedCodeMethod ();
					unknownType.RequiresNonPublicMethods ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestSuppressionOnOuterAndLocalFunction ()
			{
				LocalFunction ();

				[RequiresUnreferencedCode ("Suppress in body")]
				void LocalFunction (Type unknownType = null)
				{
					RequiresUnreferencedCodeMethod ();
					unknownType.RequiresNonPublicMethods ();
				}
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestCall ();
				TestCallWithClosure ();
				TestReflectionAccess ();
				TestLdftn ();
				TestMethodParameterWithRequirements ();
				TestDynamicallyAccessedMethod ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
				TestGenericLocalFunction<TestType> ();
				TestGenericLocalFunctionInner<TestType> ();
				TestGenericLocalFunctionWithAnnotations<TestType> ();
				TestGenericLocalFunctionWithAnnotationsAndClosure<TestType> ();
				TestCallRUCMethodInLtftnLocalFunction ();
				DynamicallyAccessedLocalFunction.TestCallRUCMethodInDynamicallyAccessedLocalFunction ();
				TestSuppressionLocalFunction ();
				TestSuppressionOnOuterAndLocalFunction ();
			}
		}

		class WarnInLambda
		{
			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static void TestCall ()
			{
				Action _ = () => RequiresUnreferencedCodeMethod ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static void TestCallWithClosure (int p = 0)
			{
				Action _ = () => {
					p++;
					RequiresUnreferencedCodeMethod ();
				};
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static void TestReflectionAccess ()
			{
				Action _ = () => {
					typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
						.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
						.Invoke (null, new object[] { });
				};
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static void TestLdftn ()
			{
				Action _ = () => {
					var action = new Action (RequiresUnreferencedCodeMethod);
				};
			}

			[ExpectedWarning ("IL2026", "--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static void TestDynamicallyAccessedMethod ()
			{
				Action _ = () => {
					typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				};
			}

			public static void Test ()
			{
				TestCall ();
				TestCallWithClosure ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInLambda
		{
			// RequiresUnreferencedCode doesn't propagate into lambdas

			// C# currently doesn't allow attributes on lambdas
			// - This would be useful as a workaround for the limitation as RUC could be applied to the lambda directly
			// - Would be useful for testing - have to use the CompilerGeneratedCode = true trick instead

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCall ()
			{
				Action _ = () => RequiresUnreferencedCodeMethod ();
			}

			// The warning is currently not detected by roslyn analyzer since it doesn't analyze DAM yet
			[ExpectedWarning ("IL2067", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCallWithReflectionAnalysisWarning ()
			{
				// This should not produce warning because the RUC
				Action<Type> _ = (t) => t.RequiresPublicMethods ();
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCallWithClosure (int p = 0)
			{
				Action _ = () => {
					p++;
					RequiresUnreferencedCodeMethod ();
				};
			}

			// Analyzer doesn't recognize reflection access - so doesn't warn in this case
			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestReflectionAccess ()
			{
				Action _ = () => {
					typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
						.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
						.Invoke (null, new object[] { });
				};
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestLdftn ()
			{
				Action _ = () => {
					var action = new Action (RequiresUnreferencedCodeMethod);
				};
			}

			// Analyzer doesn't apply DAM - so won't see this warnings
			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestDynamicallyAccessedMethod ()
			{
				Action _ = () => {
					typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				};
			}

			// The warning is currently not detected by roslyn analyzer since it doesn't analyze DAM yet
			[ExpectedWarning ("IL2077", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				Action _ = () => unknownType.RequiresNonPublicMethods ();
			}

			// The warning is currently not detected by roslyn analyzer since it doesn't analyze DAM yet
			[ExpectedWarning ("IL2091", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				Action _ = () => {
					MethodWithGenericWhichRequiresMethods<TUnknown> ();
				};
			}

			// The warning is currently not detected by roslyn analyzer since it doesn't analyze DAM yet
			[ExpectedWarning ("IL2091", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				Action _ = () => {
					new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				};
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestCall ();
				TestCallWithReflectionAnalysisWarning ();
				TestCallWithClosure ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInComplex
		{
			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static async void TestIteratorLocalFunctionInAsync ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					RequiresUnreferencedCodeMethod ();
					yield return 1;
				}
			}

			[ExpectedWarning ("IL2026", "--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ()
			{
				yield return 1;
				MethodWithGenericWhichRequiresMethods<TypeWithRUCMethod> ();
			}

			public static void Test ()
			{
				TestIteratorLocalFunctionInAsync ();
				TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ();
			}
		}

		class SuppressInComplex
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestIteratorLocalFunctionInAsync ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				[RequiresUnreferencedCode ("Suppress in local function")]
				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					RequiresUnreferencedCodeMethod ();
					yield return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestIteratorLocalFunctionInAsyncWithoutInner ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					RequiresUnreferencedCodeMethod ();
					yield return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ()
			{
				MethodWithGenericWhichRequiresMethods<TypeWithRUCMethod> ();
				yield return 0;
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestIteratorLocalFunctionInAsync ();
				TestIteratorLocalFunctionInAsyncWithoutInner ();
				TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ();
			}
		}

		class StateMachinesOnlyReferencedViaReflection
		{
			[RequiresUnreferencedCode ("RUC to suppress")]
			static IEnumerable<int> TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress ()
			{
				yield return 0;
				RequiresUnreferencedCodeMethod ();
			}

			[RequiresUnreferencedCode ("RUC to suppress")]
			static async void TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress ()
			{
				await MethodAsync ();
				RequiresUnreferencedCodeMethod ();
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestIteratorOnlyReferencedViaReflectionWhichShouldWarn ()
			{
				yield return 0;
				RequiresUnreferencedCodeMethod ();
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			static async void TestAsyncOnlyReferencedViaReflectionWhichShouldWarn ()
			{
				await MethodAsync ();
				RequiresUnreferencedCodeMethod ();
			}

			[ExpectedWarning ("IL2026", "RUC to suppress", GlobalAnalysisOnly = true)]
			public static void Test ()
			{
				// This is not a 100% reliable test, since in theory it can be marked in any order and so it could happen that the
				// user method is marked before the nested state machine gets marked. But it's the best we can do right now.
				// (Note that currently linker will mark the state machine first actually so the test is effective).
				typeof (StateMachinesOnlyReferencedViaReflection).RequiresAll ();
			}
		}

		class ComplexCases
		{
			public class AsyncBodyCallingRUCMethod
			{
				[RequiresUnreferencedCode ("")]
				static Task<object> MethodWithRUCAsync (Type type)
				{
					return Task.FromResult (new object ());
				}

				[RequiresUnreferencedCode ("ParentSuppression")]
				static async Task<object> AsyncMethodCallingRUC (Type type)
				{
					using (var diposable = await GetDisposableAsync ()) {
						return await MethodWithRUCAsync (type);
					}
				}

				[ExpectedWarning ("IL2026", "ParentSuppression")]
				public static void Test ()
				{
					AsyncMethodCallingRUC (typeof (object));
				}
			}

			public class GenericAsyncBodyCallingRUCMethod
			{
				[RequiresUnreferencedCode ("")]
				static ValueTask<TValue> MethodWithRUCAsync<TValue> ()
				{
					return ValueTask.FromResult (default (TValue));
				}

				[RequiresUnreferencedCode ("ParentSuppression")]
				static async Task<T> AsyncMethodCallingRUC<T> ()
				{
					using (var disposable = await GetDisposableAsync ()) {
						return await MethodWithRUCAsync<T> ();
					}
				}

				[ExpectedWarning ("IL2026", "ParentSuppression")]
				public static void Test ()
				{
					AsyncMethodCallingRUC<object> ();
				}
			}

			public class GenericAsyncEnumerableBodyCallingRUCWithAnnotations
			{
				class RUCOnCtor
				{
					[RequiresUnreferencedCode ("")]
					public RUCOnCtor ()
					{
					}
				}

				[RequiresUnreferencedCode ("ParentSuppression")]
				static IAsyncEnumerable<TValue> AsyncEnumMethodCallingRUC<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TValue> ()
				{
					return CreateAsync ();

					[RequiresUnreferencedCode ("")]
					static async IAsyncEnumerable<TValue> CreateAsync ()
					{
						await MethodAsync ();
						new RUCOnCtor ();
						yield return default (TValue);
					}
				}

				[ExpectedWarning ("IL2026", "ParentSuppression")]
				public static void Test ()
				{
					AsyncEnumMethodCallingRUC<object> ();
				}
			}

			class Disposable : IDisposable { public void Dispose () { } }

			static Task<Disposable> GetDisposableAsync () { return Task.FromResult (new Disposable ()); }
		}

		static async Task<int> MethodAsync ()
		{
			return await Task.FromResult (0);
		}

		[RequiresUnreferencedCode ("--RequiresUnreferencedCodeMethod--")]
		static void RequiresUnreferencedCodeMethod ()
		{
		}

		class TypeWithRUCMethod
		{
			[RequiresUnreferencedCode ("--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--")]
			static void RequiresUnreferencedCodeMethod ()
			{
			}
		}

		static void MethodWithGenericWhichRequiresMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] T> ()
		{
		}

		class TypeWithGenericWhichRequiresNonPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicFields)] T> { }

		class TestType { }
	}
}
