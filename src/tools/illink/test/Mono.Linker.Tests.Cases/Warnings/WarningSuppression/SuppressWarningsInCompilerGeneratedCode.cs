// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

			[UnconditionalSuppressMessage ("Test", "IL2077")]
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

			[UnconditionalSuppressMessage ("Test", "IL2077")]
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
			// Suppression currently doesn't propagate to local functions

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestCallRUCMethod ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction () => RequiresUnreferencedCodeMethod ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestReflectionAccessRUCMethod ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction () => typeof (SuppressWarningsInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestLdftnOnRUCMethod ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction ()
				{ var _ = new Action (RequiresUnreferencedCodeMethod); }
			}

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestDynamicallyAccessedMethod ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026")]
				void LocalFunction () => typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2077")]
			static void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				LocalFunction ();

				[ExpectedWarning ("IL2077")]
				void LocalFunction () => unknownType.RequiresNonPublicMethods ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2091")]
				void LocalFunction () => MethodWithGenericWhichRequiresMethods<TUnknown> ();
			}

			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2091")]
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

			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestCallRUCMethodInLtftnLocalFunction ()
			{
				var _ = new Action (LocalFunction);

				[ExpectedWarning ("IL2026")]
				void LocalFunction () => RequiresUnreferencedCodeMethod ();
			}

			class DynamicallyAccessedLocalFunction
			{
				[UnconditionalSuppressMessage ("Test", "IL2026")]
				public static void TestCallRUCMethodInDynamicallyAccessedLocalFunction ()
				{
					typeof (DynamicallyAccessedLocalFunction).RequiresNonPublicMethods ();

					[ExpectedWarning ("IL2026")]
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

				[ExpectedWarning ("IL2067")]
				[UnconditionalSuppressMessage ("Test", "IL2026")] // This supresses the RequiresUnreferencedCodeMethod
				void LocalFunction (Type unknownType = null)
				{
					RequiresUnreferencedCodeMethod ();
					unknownType.RequiresNonPublicMethods ();
				}
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
				TestGenericLocalFunction<TestType> ();
				TestGenericLocalFunctionInner<TestType> ();
				TestCallRUCMethodInLtftnLocalFunction ();
				DynamicallyAccessedLocalFunction.TestCallRUCMethodInDynamicallyAccessedLocalFunction ();
				TestSuppressionOnLocalFunction ();
				TestSuppressionOnOuterAndLocalFunction ();
			}
		}

		class SuppressInLambda
		{
			// Suppression currently doesn't propagate to local functions

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestCallRUCMethod ()
			{
				Action _ = () => RequiresUnreferencedCodeMethod ();
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestReflectionAccessRUCMethod ()
			{
				Action _ = () => typeof (SuppressWarningsInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestLdftnOnRUCMethod ()
			{
				Action _ = () => { var _ = new Action (RequiresUnreferencedCodeMethod); };
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[UnconditionalSuppressMessage ("Test", "IL2026")]
			static void TestDynamicallyAccessedMethod ()
			{
				Action _ = () => typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
			}

			[ExpectedWarning ("IL2077", CompilerGeneratedCode = true)]
			[UnconditionalSuppressMessage ("Test", "IL2077")]
			static void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				Action _ = () => unknownType.RequiresNonPublicMethods ();
			}

			[ExpectedWarning ("IL2091", CompilerGeneratedCode = true)]
			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				Action _ = () => MethodWithGenericWhichRequiresMethods<TUnknown> ();
			}

			[ExpectedWarning ("IL2091", CompilerGeneratedCode = true)]
			[UnconditionalSuppressMessage ("Test", "IL2091")]
			static void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				Action _ = () => new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
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

		class SuppressInComplex
		{
			[UnconditionalSuppressMessage ("Test", "IL2026")]
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

				[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
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
