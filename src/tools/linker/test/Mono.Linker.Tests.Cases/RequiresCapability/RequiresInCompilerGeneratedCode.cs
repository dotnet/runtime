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
	public class RequiresInCompilerGeneratedCode
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

			ComplexCases.AsyncBodyCallingMethodWithRequires.Test ();
			ComplexCases.GenericAsyncBodyCallingMethodWithRequires.Test ();
			ComplexCases.GenericAsyncEnumerableBodyCallingRequiresWithAnnotations.Test ();
		}

		class WarnInIteratorBody
		{
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestCallBeforeYieldReturn ()
			{
				MethodWithRequires ();
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestCallAfterYieldReturn ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static IEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				yield return 1;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestLdftn ()
			{
				yield return 0;
				yield return 1;
				var action = new Action (MethodWithRequires);
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static IEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
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
				MethodWithRequires ();
				yield return 0;
				MethodWithRequires ();
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestLdftn ()
			{
				yield return 0;
				yield return 1;
				var action = new Action (MethodWithRequires);
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
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
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static async void TestCallBeforeYieldReturn ()
			{
				MethodWithRequires ();
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static async void TestCallAfterYieldReturn ()
			{
				await MethodAsync ();
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static async void TestReflectionAccess ()
			{
				await MethodAsync ();
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static async void TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (MethodWithRequires);
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static async void TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
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
				MethodWithRequires ();
				await MethodAsync ();
				MethodWithRequires ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestReflectionAccess ()
			{
				await MethodAsync ();
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (MethodWithRequires);
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
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
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static async IAsyncEnumerable<int> TestCallBeforeYieldReturn ()
			{
				await MethodAsync ();
				MethodWithRequires ();
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static async IAsyncEnumerable<int> TestCallAfterYieldReturn ()
			{
				yield return 0;
				MethodWithRequires ();
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static async IAsyncEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				await MethodAsync ();
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
				yield return 1;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static async IAsyncEnumerable<int> TestLdftn ()
			{
				await MethodAsync ();
				yield return 0;
				var action = new Action (MethodWithRequires);
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static async IAsyncEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
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
				MethodWithRequires ();
				await MethodAsync ();
				yield return 0;
				MethodWithRequires ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestReflectionAccess ()
			{
				await MethodAsync ();
				yield return 0;
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (MethodWithRequires);
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
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
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static void TestCall ()
			{
				LocalFunction ();

				void LocalFunction () => MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static void TestCallWithClosure (int p = 0)
			{
				LocalFunction ();

				void LocalFunction ()
				{
					p++;
					MethodWithRequires ();
				}
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static void TestReflectionAccess ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static void TestLdftn ()
			{
				LocalFunction ();

				void LocalFunction ()
				{
					var action = new Action (MethodWithRequires);
				}
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static void TestDynamicallyAccessedMethod ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
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
			// Requires doesn't propagate into local functions yet
			// so its suppression effect also doesn't propagate

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCall ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction () => MethodWithRequires ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCallWithClosure (int p = 0)
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction ()
				{
					p++;
					MethodWithRequires ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestReflectionAccess ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction () => typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestLdftn ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction ()
				{
					var action = new Action (MethodWithRequires);
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestDynamicallyAccessedMethod ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction () => typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
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
			static void TestCallMethodWithRequiresInLtftnLocalFunction ()
			{
				var _ = new Action (LocalFunction);

				[ExpectedWarning ("IL2026")]
				void LocalFunction () => MethodWithRequires ();
			}

			class DynamicallyAccessedLocalFunction
			{
				[RequiresUnreferencedCode ("Suppress in body")]
				public static void TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction ()
				{
					typeof (DynamicallyAccessedLocalFunction).RequiresNonPublicMethods ();

					[ExpectedWarning ("IL2026")]
					void LocalFunction () => MethodWithRequires ();
				}
			}

			[ExpectedWarning ("IL2026")]
			static void TestSuppressionLocalFunction ()
			{
				LocalFunction (); // This will produce a warning since the location function has Requires on it

				[RequiresUnreferencedCode ("Suppress in body")]
				void LocalFunction (Type unknownType = null)
				{
					MethodWithRequires ();
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
					MethodWithRequires ();
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
				TestCallMethodWithRequiresInLtftnLocalFunction ();
				DynamicallyAccessedLocalFunction.TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction ();
				TestSuppressionLocalFunction ();
				TestSuppressionOnOuterAndLocalFunction ();
			}
		}

		class WarnInLambda
		{
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static void TestCall ()
			{
				Action _ = () => MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static void TestCallWithClosure (int p = 0)
			{
				Action _ = () => {
					p++;
					MethodWithRequires ();
				};
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static void TestReflectionAccess ()
			{
				Action _ = () => {
					typeof (RequiresInCompilerGeneratedCode)
						.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
						.Invoke (null, new object[] { });
				};
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static void TestLdftn ()
			{
				Action _ = () => {
					var action = new Action (MethodWithRequires);
				};
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static void TestDynamicallyAccessedMethod ()
			{
				Action _ = () => {
					typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
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
			// Requires doesn't propagate into lambdas

			// C# currently doesn't allow attributes on lambdas
			// - This would be useful as a workaround for the limitation as Requires could be applied to the lambda directly
			// - Would be useful for testing - have to use the CompilerGeneratedCode = true trick instead

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCall ()
			{
				Action _ = () => MethodWithRequires ();
			}

			// The warning is currently not detected by roslyn analyzer since it doesn't analyze DAM yet
			[ExpectedWarning ("IL2067", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCallWithReflectionAnalysisWarning ()
			{
				// This should not produce warning because the Requires
				Action<Type> _ = (t) => t.RequiresPublicMethods ();
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCallWithClosure (int p = 0)
			{
				Action _ = () => {
					p++;
					MethodWithRequires ();
				};
			}

			// Analyzer doesn't recognize reflection access - so doesn't warn in this case
			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestReflectionAccess ()
			{
				Action _ = () => {
					typeof (RequiresInCompilerGeneratedCode)
						.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
						.Invoke (null, new object[] { });
				};
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestLdftn ()
			{
				Action _ = () => {
					var action = new Action (MethodWithRequires);
				};
			}

			// Analyzer doesn't apply DAM - so won't see this warnings
			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestDynamicallyAccessedMethod ()
			{
				Action _ = () => {
					typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				};
			}

			// The warning is currently not detected by roslyn analyzer since it doesn't analyze DAM yet
			[ExpectedWarning ("IL2077", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				Action _ = () => unknownType.RequiresNonPublicMethods ();
			}

			// The warning is currently not detected by roslyn analyzer since it doesn't analyze DAM yet
			[ExpectedWarning ("IL2091", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				Action _ = () => {
					MethodWithGenericWhichRequiresMethods<TUnknown> ();
				};
			}

			// The warning is currently not detected by roslyn analyzer since it doesn't analyze DAM yet
			[ExpectedWarning ("IL2091", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
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
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			static async void TestIteratorLocalFunctionInAsync ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					MethodWithRequires ();
					yield return 1;
				}
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ()
			{
				yield return 1;
				MethodWithGenericWhichRequiresMethods<TypeWithMethodWithRequires> ();
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
					MethodWithRequires ();
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
					MethodWithRequires ();
					yield return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ()
			{
				MethodWithGenericWhichRequiresMethods<TypeWithMethodWithRequires> ();
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
			[RequiresUnreferencedCode ("Requires to suppress")]
			static IEnumerable<int> TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[RequiresUnreferencedCode ("Requires to suppress")]
			static async void TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress ()
			{
				await MethodAsync ();
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestIteratorOnlyReferencedViaReflectionWhichShouldWarn ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			static async void TestAsyncOnlyReferencedViaReflectionWhichShouldWarn ()
			{
				await MethodAsync ();
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "Requires to suppress", ProducedBy = ProducedBy.Trimmer)]
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
			public class AsyncBodyCallingMethodWithRequires
			{
				[RequiresUnreferencedCode ("")]
				static Task<object> MethodWithRequiresAsync (Type type)
				{
					return Task.FromResult (new object ());
				}

				[RequiresUnreferencedCode ("ParentSuppression")]
				static async Task<object> AsyncMethodCallingRequires (Type type)
				{
					using (var diposable = await GetDisposableAsync ()) {
						return await MethodWithRequiresAsync (type);
					}
				}

				[ExpectedWarning ("IL2026", "ParentSuppression")]
				public static void Test ()
				{
					AsyncMethodCallingRequires (typeof (object));
				}
			}

			public class GenericAsyncBodyCallingMethodWithRequires
			{
				[RequiresUnreferencedCode ("")]
				static ValueTask<TValue> MethodWithRequiresAsync<TValue> ()
				{
					return ValueTask.FromResult (default (TValue));
				}

				[RequiresUnreferencedCode ("ParentSuppression")]
				static async Task<T> AsyncMethodCallingRequires<T> ()
				{
					using (var disposable = await GetDisposableAsync ()) {
						return await MethodWithRequiresAsync<T> ();
					}
				}

				[ExpectedWarning ("IL2026", "ParentSuppression")]
				public static void Test ()
				{
					AsyncMethodCallingRequires<object> ();
				}
			}

			public class GenericAsyncEnumerableBodyCallingRequiresWithAnnotations
			{
				class RequiresOnCtor
				{
					[RequiresUnreferencedCode ("")]
					public RequiresOnCtor ()
					{
					}
				}

				[RequiresUnreferencedCode ("ParentSuppression")]
				static IAsyncEnumerable<TValue> AsyncEnumMethodCallingRequires<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TValue> ()
				{
					return CreateAsync ();

					[RequiresUnreferencedCode ("")]
					static async IAsyncEnumerable<TValue> CreateAsync ()
					{
						await MethodAsync ();
						new RequiresOnCtor ();
						yield return default (TValue);
					}
				}

				[ExpectedWarning ("IL2026", "ParentSuppression")]
				public static void Test ()
				{
					AsyncEnumMethodCallingRequires<object> ();
				}
			}

			class Disposable : IDisposable { public void Dispose () { } }

			static Task<Disposable> GetDisposableAsync () { return Task.FromResult (new Disposable ()); }
		}

		static async Task<int> MethodAsync ()
		{
			return await Task.FromResult (0);
		}

		[RequiresUnreferencedCode ("--MethodWithRequires--")]
		static void MethodWithRequires ()
		{
		}

		class TypeWithMethodWithRequires
		{
			[RequiresUnreferencedCode ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			static void MethodWithRequires ()
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
