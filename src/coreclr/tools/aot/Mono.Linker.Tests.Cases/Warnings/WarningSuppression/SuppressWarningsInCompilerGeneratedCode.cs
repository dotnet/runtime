// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class SuppressWarningsInCompilerGeneratedCode
	{
		public static void Main ()
		{
			SuppressInIteratorBody.Test ();
			SuppressInAsyncBody.Test ();
			SuppressInLocalFunction.Test ();
			SuppressInLambda.Test ();
			SuppressInComplex.Test ();
		}
		class SuppressInIteratorBody
		{
			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static IEnumerable<int> TestCallRUCMethod ()
			{
				RequiresUnreferencedCodeMethod ();
				yield return 0;
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static IEnumerable<int> TestReflectionAccessRUCMethod ()
			{
				yield return 0;
				typeof (SuppressWarningsInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				yield return 0;
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static IEnumerable<int> TestLdftnOnRUCMethod ()
			{
				yield return 0;
				var _ = new Action (RequiresUnreferencedCodeMethod);
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static IEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				yield return 0;
			}

			[UnconditionalSuppressMessage ("Test", "IL2067")]
			static IEnumerable<int> TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				yield return 0;
			}

			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static IEnumerable<int> TestGenericMethodParameterRequirement<TUnknown> ()
			{
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				yield return 0;
			}

			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static IEnumerable<int> TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				yield return 0;
			}

			public static void Test ()
			{
				TestCallRUCMethod ();
				TestReflectionAccessRUCMethod ();
				TestLdftnOnRUCMethod ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class SuppressInAsyncBody
		{
			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static async void TestCallRUCMethod ()
			{
				RequiresUnreferencedCodeMethod ();
				await MethodAsync ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static async void TestReflectionAccessRUCMethod ()
			{
				await MethodAsync ();
				typeof (SuppressWarningsInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static async void TestLdftnOnRUCMethod ()
			{
				await MethodAsync ();
				var _ = new Action (RequiresUnreferencedCodeMethod);
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static async void TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2067")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static async void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				await MethodAsync ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static async void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				await MethodAsync ();
			}

			public static void Test ()
			{
				TestCallRUCMethod ();
				TestReflectionAccessRUCMethod ();
				TestLdftnOnRUCMethod ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class SuppressInLocalFunction
		{
			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestCallRUCMethod ()
			{
				LocalFunction ();

				void LocalFunction () => RequiresUnreferencedCodeMethod ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2121", Justification = "The IL2026 warning is issued only by the analyzer, for the linker this suppression is redundant.")]
			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestCallRUCMethodUnused ()
			{
				void LocalFunction () => RequiresUnreferencedCodeMethod ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestReflectionAccessRUCMethod ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (SuppressWarningsInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestLdftnOnRUCMethod ()
			{
				LocalFunction ();

				void LocalFunction ()
				{ var _ = new Action (RequiresUnreferencedCodeMethod); }
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestDynamicallyAccessedMethod ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2067")]
			static void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				LocalFunction ();

				void LocalFunction () => unknownType.RequiresNonPublicMethods ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				LocalFunction ();

				void LocalFunction () => MethodWithGenericWhichRequiresMethods<TUnknown> ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				LocalFunction ();

				void LocalFunction () => new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static void TestGenericLocalFunction<TUnknown> ()
			{
				LocalFunction<TUnknown> ();

				void LocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
				{
					typeof (T).RequiresPublicMethods ();
				}
			}

			[UnconditionalSuppressMessage ("Test", "IL2087")]
			static void TestGenericLocalFunctionInner<TUnknown> ()
			{
				LocalFunction<TUnknown> ();

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

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestCallRUCMethodInLtftnLocalFunction ()
			{
				var _ = new Action (LocalFunction);

				void LocalFunction () => RequiresUnreferencedCodeMethod ();
			}

			class DynamicallyAccessedLocalFunction
			{
				[ExpectedWarning ("IL2118", "LocalFunction", ProducedBy = ProducedBy.Trimmer)]
				[UnconditionalSuppressMessage ("Test", "IL2026")]
				public static void TestCallRUCMethodInDynamicallyAccessedLocalFunction ()
				{
					typeof (DynamicallyAccessedLocalFunction).RequiresNonPublicMethods ();

					LocalFunction ();

					void LocalFunction () => RequiresUnreferencedCodeMethod ();
				}
			}

			static void TestSuppressionOnLocalFunction ()
			{
				LocalFunction ();

				[UnconditionalSuppressMessage ("Test", "IL2026")] // This supresses the RequiresUnreferencedCodeMethod
				void LocalFunction (Type unknownType = null)
				{
					RequiresUnreferencedCodeMethod ();
				}
			}

			[UnconditionalSuppressMessage ("Test", "IL2067")] // This suppresses the unknownType.RequiresNonPublicMethods
			static void TestSuppressionOnOuterAndLocalFunction ()
			{
				LocalFunction ();

				[UnconditionalSuppressMessage ("Test", "IL2026")] // This supresses the RequiresUnreferencedCodeMethod
				void LocalFunction (Type unknownType = null)
				{
					RequiresUnreferencedCodeMethod ();
					unknownType.RequiresNonPublicMethods ();
				}
			}

			class TestSuppressionOnOuterWithSameName
			{
				public static void Test ()
				{
					Outer ();
					Outer (0);
				}

				[UnconditionalSuppressMessage ("Test", "IL2121")]
				[UnconditionalSuppressMessage ("Test", "IL2026")]
				static void Outer ()
				{
					// Even though this method has the same name as Outer(int i),
					// it should not suppress warnings originating from compiler-generated
					// code for the lambda contained in Outer(int i).
				}

				static void Outer (int i)
				{
					LocalFunction ();

					[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--")]
					void LocalFunction () => RequiresUnreferencedCodeMethod ();
				}
			}

			public static void Test ()
			{
				TestCallRUCMethod ();
				TestCallRUCMethodUnused ();
				TestReflectionAccessRUCMethod ();
				TestLdftnOnRUCMethod ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
				TestGenericLocalFunction<TestType> ();
				TestGenericLocalFunctionInner<TestType> ();
				TestGenericLocalFunctionWithAnnotations<TestType> ();
				TestGenericLocalFunctionWithAnnotationsAndClosure<TestType> ();
				TestCallRUCMethodInLtftnLocalFunction ();
				DynamicallyAccessedLocalFunction.TestCallRUCMethodInDynamicallyAccessedLocalFunction ();
				TestSuppressionOnLocalFunction ();
				TestSuppressionOnOuterAndLocalFunction ();
				TestSuppressionOnOuterWithSameName.Test ();
			}
		}

		class SuppressInLambda
		{
			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestCallRUCMethod ()
			{
				Action lambda =
				() => RequiresUnreferencedCodeMethod ();

				lambda ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestCallRUCMethodUnused ()
			{
				Action _ =
				() => RequiresUnreferencedCodeMethod ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestReflectionAccessRUCMethod ()
			{
				Action _ =
				() => typeof (SuppressWarningsInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestLdftnOnRUCMethod ()
			{
				Action _ =
				() => { var _ = new Action (RequiresUnreferencedCodeMethod); };
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestDynamicallyAccessedMethod ()
			{
				Action _ =
				() => typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2067")]
			static void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				Action _ =
				() => unknownType.RequiresNonPublicMethods ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				Action _ =
				() => MethodWithGenericWhichRequiresMethods<TUnknown> ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				Action _ =
				() => new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
			}

			class DynamicallyAccessedLambda
			{
				[ExpectedWarning ("IL2118", nameof (TestCallRUCMethodInDynamicallyAccessedLambda), ProducedBy = ProducedBy.Trimmer)]
				[UnconditionalSuppressMessage ("Test", "IL2026")]
				public static void TestCallRUCMethodInDynamicallyAccessedLambda ()
				{
					typeof (DynamicallyAccessedLambda).RequiresAll ();

					Action lambda = () => RequiresUnreferencedCodeMethod ();

					lambda ();
				}
			}

			class DynamicallyAccessedLambdaUnused
			{
				[ExpectedWarning ("IL2118", nameof (TestCallRUCMethodInDynamicallyAccessedLambda), ProducedBy = ProducedBy.Trimmer)]
				[UnconditionalSuppressMessage ("Test", "IL2026")]
				public static void TestCallRUCMethodInDynamicallyAccessedLambda ()
				{
					typeof (DynamicallyAccessedLambdaUnused).RequiresAll ();

					Action _ = () => RequiresUnreferencedCodeMethod ();
				}
			}

			static void TestSuppressionOnLambda ()
			{
				var lambda =
				// https://github.com/dotnet/roslyn/issues/59746
				[ExpectedWarning ("IL2026", ProducedBy = ProducedBy.Analyzer)]
				[UnconditionalSuppressMessage ("Test", "IL2026")]
				() => RequiresUnreferencedCodeMethod ();

				lambda ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2067")]
			static void TestSuppressionOnOuterAndLambda ()
			{
				var lambda =
				// https://github.com/dotnet/roslyn/issues/59746
				[ExpectedWarning ("IL2026", ProducedBy = ProducedBy.Analyzer)]
				[UnconditionalSuppressMessage ("Test", "IL2026")]
				(Type unknownType) => {
					RequiresUnreferencedCodeMethod ();
					unknownType.RequiresNonPublicMethods ();
				};

				lambda (null);
			}

			class TestSuppressionOnOuterWithSameName
			{
				public static void Test ()
				{
					Outer ();
					Outer (0);
				}

				[UnconditionalSuppressMessage ("Test", "IL2121")]
				[UnconditionalSuppressMessage ("Test", "IL2026")]
				static void Outer ()
				{
					// Even though this method has the same name as Outer(int i),
					// it should not suppress warnings originating from compiler-generated
					// code for the lambda contained in Outer(int i).
				}

				static void Outer (int i)
				{
					var lambda =
					[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--")]
					() => RequiresUnreferencedCodeMethod ();

					lambda ();
				}
			}

			public static void Test ()
			{
				TestCallRUCMethod ();
				TestCallRUCMethodUnused ();
				TestReflectionAccessRUCMethod ();
				TestLdftnOnRUCMethod ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
				DynamicallyAccessedLambda.TestCallRUCMethodInDynamicallyAccessedLambda ();
				DynamicallyAccessedLambdaUnused.TestCallRUCMethodInDynamicallyAccessedLambda ();
				TestSuppressionOnLambda ();
				TestSuppressionOnOuterAndLambda ();
				TestSuppressionOnOuterWithSameName.Test ();

			}
		}

		class SuppressInComplex
		{
			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestIteratorLocalFunction ()
			{
				LocalFunction ();

				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					RequiresUnreferencedCodeMethod ();
					yield return 1;
				}
			}

			static async void TestIteratorLocalFunctionInAsync ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				[UnconditionalSuppressMessage ("Test", "IL2026")]
				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					RequiresUnreferencedCodeMethod ();
					yield return 1;
				}
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static async void TestIteratorLocalFunctionInAsyncWithoutInner ()
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

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ()
			{
				MethodWithGenericWhichRequiresMethods<TypeWithRUCMethod> ();
				yield return 0;
			}

			public static void Test ()
			{
				TestIteratorLocalFunction ();
				TestIteratorLocalFunctionInAsync ();
				TestIteratorLocalFunctionInAsyncWithoutInner ();
				TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ();
			}
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
