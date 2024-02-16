// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class CompilerGeneratedCodeAccessedViaReflection
	{
		public static void Main ()
		{
			IteratorStateMachines.Test ();
			AsyncStateMachines.Test ();
			AsyncIteratorStateMachines.Test ();
			Lambdas.Test ();
			LocalFunctions.Test ();

			SelfMarkingMethods.Test ();
			DelegateAccess.Test ();

			DAMReflectionAccessToCompilerGeneratedCode.Test ();
		}

		class BaseTypeWithIteratorStateMachines
		{
			public static IEnumerable<int> BaseIteratorWithCorrectDataflow ()
			{
				var t = GetAll ();
				yield return 0;
				t.RequiresAll ();
			}
		}

		[ExpectedWarning ("IL2120", "<" + nameof (BaseIteratorWithCorrectDataflow) + ">", "MoveNext",
			ProducedBy = Tool.Trimmer)]
		[ExpectedWarning ("IL2120", "<" + nameof (BaseIteratorWithCorrectDataflow) + ">", "<t>",
			ProducedBy = Tool.Trimmer)]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		class IteratorStateMachines : BaseTypeWithIteratorStateMachines
		{
			public static IEnumerable<int> IteratorWithoutDataflow ()
			{
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--",
				ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--",
				ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL2119", "<" + nameof (IteratorCallsMethodWithRequires) + ">", "MoveNext", CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer)]
			public static IEnumerable<int> IteratorCallsMethodWithRequires ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2119", "<" + nameof (IteratorWithCorrectDataflow) + ">", "MoveNext", CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2119", "<t_IteratorWithCorrectDataflow>", CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer)]
			public static IEnumerable<int> IteratorWithCorrectDataflow ()
			{
				var t_IteratorWithCorrectDataflow = GetAll ();
				yield return 0;
				t_IteratorWithCorrectDataflow.RequiresAll ();
			}

			[ExpectedWarning ("IL2119", "<" + nameof (IteratorWithIntegerDataflow) + ">", "MoveNext", CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2119", "<types>", CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer)]
			public static IEnumerable<int> IteratorWithIntegerDataflow ()
			{
				int integerLocal = 0;
				yield return 0;
				var types = new Type[] { GetWithPublicMethods (), GetWithPublicFields () };
				types[integerLocal].RequiresPublicMethods ();
			}

			[ExpectedWarning ("IL2119", "<" + nameof (IteratorWithProblematicDataflow) + ">", "MoveNext", CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2119", "<t_IteratorWithProblematicDataflow>", CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			public static IEnumerable<int> IteratorWithProblematicDataflow ()
			{
				var t_IteratorWithProblematicDataflow = GetWithPublicMethods ();
				yield return 0;
				t_IteratorWithProblematicDataflow.RequiresAll ();
			}

			[ExpectedWarning ("IL2112", nameof (RUCTypeWithIterators) + "()", "--RUCTypeWithIterators--", CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer | Tool.NativeAot)] // warning about .ctor
			[RequiresUnreferencedCode ("--RUCTypeWithIterators--")]
			class RUCTypeWithIterators
			{
				[ExpectedWarning ("IL2112", nameof (StaticIteratorCallsMethodWithRequires) + "()",
					ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
				public static IEnumerable<int> StaticIteratorCallsMethodWithRequires ()
				{
					yield return 0;
					MethodWithRequires ();
				}

				[ExpectedWarning ("IL2112", nameof (InstanceIteratorCallsMethodWithRequires) + "()",
					ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
				public IEnumerable<int> InstanceIteratorCallsMethodWithRequires ()
				{
					yield return 0;
					MethodWithRequires ();
				}
			}

			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithProblematicDataflow) + ">", "MoveNext",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorCallsMethodWithRequires) + ">", "MoveNext",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithCorrectDataflow) + ">", "MoveNext",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithIntegerDataflow) + ">", "MoveNext",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (BaseIteratorWithCorrectDataflow) + ">", "MoveNext",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithIterators) + "()", "--RUCTypeWithIterators--")]
			// Expect to see warnings about RUC on type, for all static state machine members.
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithIterators.StaticIteratorCallsMethodWithRequires) + "()", "--RUCTypeWithIterators--")]
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithIterators.InstanceIteratorCallsMethodWithRequires) + "()")]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithCorrectDataflow) + ">", "<t_IteratorWithCorrectDataflow>",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithProblematicDataflow) + ">", "<t_IteratorWithProblematicDataflow>",
				ProducedBy = Tool.Trimmer)]
			// Technically the access to IteratorWithIntegerDataflow should warn about access to the integer
			// field integerLocal, but our heuristics only warn if the field type satisfies the
			// "IsTypeInterestingForDatafllow" check. This is likely good enough because in most cases the
			// compiler-generated code will have other hoisted fields with types that _are_ interesting for dataflow.
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithIntegerDataflow) + ">", "<types>",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (BaseIteratorWithCorrectDataflow) + ">", "<t>",
				ProducedBy = Tool.Trimmer)]
			public static void Test (IteratorStateMachines test = null)
			{
				typeof (IteratorStateMachines).RequiresAll ();

				test.GetType ().RequiresAll ();
			}
		}

		class AsyncStateMachines
		{
			public static async Task AsyncWithoutDataflow ()
			{
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			public static async Task AsyncCallsMethodWithRequires ()
			{
				MethodWithRequires ();
			}

			public static async Task AsyncWithCorrectDataflow ()
			{
				var t_AsyncWithCorrectDataflow = GetAll ();
				t_AsyncWithCorrectDataflow.RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			public static async Task AsyncWithProblematicDataflow ()
			{
				var t_AsyncWithProblematicDataflow = GetWithPublicMethods ();
				t_AsyncWithProblematicDataflow.RequiresAll ();
			}

			[ExpectedWarning ("IL2118", "<" + nameof (AsyncWithProblematicDataflow) + ">", "MoveNext",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncCallsMethodWithRequires) + ">", "MoveNext",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncWithCorrectDataflow) + ">", "MoveNext",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncWithCorrectDataflow) + ">", "<t_AsyncWithCorrectDataflow>",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncWithProblematicDataflow) + ">", "<t_AsyncWithProblematicDataflow>",
				ProducedBy = Tool.Trimmer)]
			public static void Test ()
			{
				typeof (AsyncStateMachines).RequiresAll ();
			}
		}

		class AsyncIteratorStateMachines
		{
			public static async IAsyncEnumerable<int> AsyncIteratorWithoutDataflow ()
			{
				yield return await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			public static async IAsyncEnumerable<int> AsyncIteratorCallsMethodWithRequires ()
			{
				yield return await MethodAsync ();
				MethodWithRequires ();
			}

			public static async IAsyncEnumerable<int> AsyncIteratorWithCorrectDataflow ()
			{
				var t = GetAll ();
				yield return await MethodAsync ();
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			public static async IAsyncEnumerable<int> AsyncIteratorWithProblematicDataflow ()
			{
				var t = GetWithPublicMethods ();
				yield return await MethodAsync ();
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorWithProblematicDataflow) + ">", "MoveNext",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorCallsMethodWithRequires) + ">", "MoveNext",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorWithCorrectDataflow) + ">", "MoveNext",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorWithCorrectDataflow) + ">", "<t>",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorWithProblematicDataflow) + ">", "<t>",
				ProducedBy = Tool.Trimmer)]
			public static void Test ()
			{
				typeof (AsyncIteratorStateMachines).RequiresAll ();
			}
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		class Lambdas
		{
			static void LambdaWithoutDataflow ()
			{
				var lambda = () => 0;
				lambda ();
			}

			static void LambdaCallsMethodWithRequires ()
			{
				var lambda =
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaCallsMethodWithRequires) + ">",
					ProducedBy = Tool.Trimmer)]
				() => MethodWithRequires ();
				lambda ();
			}

			static void LambdaWithCorrectDataflow ()
			{
				var lambda =
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaWithCorrectDataflow) + ">",
					ProducedBy = Tool.Trimmer)]
				() => {
					var t = GetAll ();
					t.RequiresAll ();
				};
				lambda ();
			}

			[ExpectedWarning ("IL2111")]
			static void LambdaWithCorrectParameter ()
			{
				var lambda =
				([DynamicallyAccessedMembersAttribute (DynamicallyAccessedMemberTypes.All)] Type t) => {
					t.RequiresAll ();
				};
				lambda (null);
			}

			static void LambdaWithProblematicDataflow ()
			{
				var lambda =
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaWithProblematicDataflow) + ">",
					ProducedBy = Tool.Trimmer)]
				[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll))]
				() => {
					var t = GetWithPublicMethods ();
					t.RequiresAll ();
				};
				lambda ();
			}

			static void LambdaWithCapturedTypeToDAM ()
			{
				var t = GetWithPublicMethods ();
				var lambda =
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaWithCapturedTypeToDAM) + ">",
					ProducedBy = Tool.Trimmer)]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				() => {
					t.RequiresAll ();
				};
				lambda ();
			}

			static void LambdaWithCapturedInt ()
			{
				int i = 0;
				var lambda =
				() => i;
				i++;
				lambda ();
			}

			static void LambdaCallsPInvokeTakingPrimitiveType ()
			{
				var lambda = () => MethodTakingPrimitiveType (42);
				lambda ();
			}

			static void LambdaCallsPInvokeTakingObject ()
			{
				var lambda =
				[ExpectedWarning ("IL2050")]
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaCallsPInvokeTakingObject) + ">",
					ProducedBy = Tool.Trimmer)]
				() => MethodTakingObject (null);
				lambda ();
			}

			[ExpectedWarning ("IL2112", nameof (RUCTypeWithLambdas) + "()", "--RUCTypeWithLambdas--", CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[RequiresUnreferencedCode ("--RUCTypeWithLambdas--")]
			class RUCTypeWithLambdas
			{
				[ExpectedWarning ("IL2112", nameof (MethodWithLambdas), "--RUCTypeWithLambdas--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				public void MethodWithLambdas ()
				{
					var lambda =
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					() => MethodWithRequires ();

					int i = 0;
					var lambdaWithCapturedState =
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					() => {
						i++;
						MethodWithRequires ();
					};

					lambda ();
					lambdaWithCapturedState ();
				}
			}

			[ExpectedWarning ("IL2118", "<" + nameof (LambdaCallsPInvokeTakingObject) + ">",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaCallsMethodWithRequires) + ">",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithCorrectDataflow) + ">",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithProblematicDataflow) + ">",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithCapturedTypeToDAM) + ">",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithLambdas) + "()", "--RUCTypeWithLambdas--")]
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithLambdas.MethodWithLambdas) + "()", "--RUCTypeWithLambdas--")]
			public static void Test (Lambdas test = null)
			{
				typeof (Lambdas).RequiresAll ();

				test.GetType ().RequiresAll ();
			}
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		class LocalFunctions
		{
			static void LocalFunctionWithoutDataflow ()
			{
				int LocalFunction () => 0;
				LocalFunction ();
			}

			static void LocalFunctionCallsMethodWithRequires ()
			{
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionCallsMethodWithRequires) + ">",
					ProducedBy = Tool.Trimmer)]
				void LocalFunction () => MethodWithRequires ();
				LocalFunction ();
			}

			static void LocalFunctionWithCorrectDataflow ()
			{
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionWithCorrectDataflow) + ">",
					ProducedBy = Tool.Trimmer)]
				void LocalFunction ()
				{
					var t = GetAll ();
					t.RequiresAll ();
				};
				LocalFunction ();
			}

			static void LocalFunctionWithProblematicDataflow ()
			{
				[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll))]
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionWithProblematicDataflow) + ">",
					ProducedBy = Tool.Trimmer)]
				void LocalFunction ()
				{
					var t = GetWithPublicMethods ();
					t.RequiresAll ();
				};
				LocalFunction ();
			}

			static void LocalFunctionWithCapturedTypeToDAM ()
			{
				var t = GetAll ();
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionWithCapturedTypeToDAM) + ">",
					ProducedBy = Tool.Trimmer)]
				void LocalFunction ()
				{
					t.RequiresAll ();
				};
				LocalFunction ();
			}

			static void LocalFunctionWithCapturedInt ()
			{
				int i = 0;
				int LocalFunction () => i;
				i++;
				LocalFunction ();
			}

			static void LocalFunctionCallsPInvokeTakingPrimitiveType ()
			{
				void LocalFunction () => MethodTakingPrimitiveType (42);
				LocalFunction ();
			}

			static void LocalFunctionCallsPInvokeTakingObject ()
			{
				[ExpectedWarning ("IL2050")]
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionCallsPInvokeTakingObject) + ">",
					ProducedBy = Tool.Trimmer)]
				void LocalFunction () => MethodTakingObject (null);
				LocalFunction ();
			}

			[ExpectedWarning ("IL2112", nameof (RUCTypeWithLocalFunctions) + "()", CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[RequiresUnreferencedCode ("--RUCTypeWithLocalFunctions--")]
			class RUCTypeWithLocalFunctions
			{
				[ExpectedWarning ("IL2112", nameof (MethodWithLocalFunctions), "--RUCTypeWithLocalFunctions--",
					ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				public void MethodWithLocalFunctions ()
				{
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					void LocalFunction () => MethodWithRequires ();

					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					static void StaticLocalFunction () => MethodWithRequires ();

					int i = 0;
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					void LocalFunctionWithCapturedState ()
					{
						i++;
						MethodWithRequires ();
					}

					LocalFunction ();
					StaticLocalFunction ();
					LocalFunctionWithCapturedState ();
				}
			}

			[ExpectedWarning ("IL2118", nameof (LocalFunctionCallsPInvokeTakingObject),
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionCallsMethodWithRequires),
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithCorrectDataflow),
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithProblematicDataflow),
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithCapturedTypeToDAM),
				ProducedBy = Tool.Trimmer)]
			// Expect RUC warnings for static, compiler-generated code warnings for instance.
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithLocalFunctions) + "()", "--RUCTypeWithLocalFunctions--")]
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithLocalFunctions.MethodWithLocalFunctions) + "()")]
			public static void Test (LocalFunctions test = null)
			{
				typeof (LocalFunctions).RequiresAll ();

				test.GetType ().RequiresAll ();
			}
		}

		class SelfMarkingMethods
		{
			static void RequiresAllOnT<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T> () { }

			static void RequiresNonPublicMethodsOnT<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] T> () { }

			class LambdaWhichMarksItself
			{
				public static void Test ()
				{
					var a =
					() => {
						RequiresAllOnT<LambdaWhichMarksItself> ();
					};

					a ();
				}
			}

			class LocalFunctionWhichMarksItself
			{
				public static void Test ()
				{
					void LocalFunction ()
					{
						RequiresAllOnT<LocalFunctionWhichMarksItself> ();
					};

					LocalFunction ();
				}
			}

			class IteratorWhichMarksItself
			{
				public static IEnumerable<int> Test ()
				{
					yield return 0;

					RequiresAllOnT<IteratorWhichMarksItself> ();

					yield return 1;
				}
			}

			class AsyncWhichMarksItself
			{
				public static async void Test ()
				{
					await MethodAsync ();

					RequiresAllOnT<AsyncWhichMarksItself> ();

					await MethodAsync ();
				}
			}


			class MethodWhichMarksItself
			{
				static void RequiresAllOnT<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T> () { }

				public static void Test ()
				{
					RequiresAllOnT<MethodWhichMarksItself> ();
				}
			}


			class LocalFunctionWhichMarksItselfOnlyAccessedViaReflection
			{
				public static void Test ()
				{
					RequiresNonPublicMethodsOnT<ClassWithLocalFunction> ();
				}

				public class ClassWithLocalFunction
				{
					public static void MethodWithLocalFunction ()
					{
						static void LocalFunction ()
						{
							RequiresNonPublicMethodsOnT<ClassWithLocalFunction> ();
						};

						LocalFunction ();
					}
				}
			}

			public static void Test ()
			{
				LambdaWhichMarksItself.Test ();
				LocalFunctionWhichMarksItself.Test ();
				IteratorWhichMarksItself.Test ();
				AsyncWhichMarksItself.Test ();
				MethodWhichMarksItself.Test ();
				LocalFunctionWhichMarksItselfOnlyAccessedViaReflection.Test ();
			}
		}

		class DelegateAccess
		{
			static void AnnotatedMethod ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{
			}

			[ExpectedWarning ("IL2111")]
			static void TestMethodThroughDelegate ()
			{
				Action<Type> a = AnnotatedMethod;
			}

			[ExpectedWarning ("IL2111")]
			static void TestLambdaThroughDelegate ()
			{
				Action<Type> a = ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) => { };
				a (null);
			}

			[ExpectedWarning ("IL2111")]
			static void TestLocalFunctionThroughDelegate ()
			{
				Action<Type> a = LocalFunction;
				a (null);

				void LocalFunction ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
				}
			}

			static void TestGenericLocalFunctionThroughDelegate ()
			{
				Action a = LocalFunction<TestType>;
				a ();

				void LocalFunction <[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
				{
				}
			}

			public static void Test ()
			{
				TestMethodThroughDelegate ();
				TestLambdaThroughDelegate ();
				TestLocalFunctionThroughDelegate ();
				TestGenericLocalFunctionThroughDelegate ();
			}
		}

		class DAMReflectionAccessToCompilerGeneratedCode
		{
			// ldftn access - this MUST warn since the action can be called without the annotation
			[ExpectedWarning ("IL2111")]
			static void Lambda ()
			{
				Action<Type> a = ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) => {
					type.GetMethods ();
				};

				a (typeof (string));
			}

			static void LambdaOnGeneric<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
			{
				Action a = () => {
					typeof(T).GetMethods ();
				};

				a ();
			}

			static void LocalFunction ()
			{
				LocalFunctionInner (typeof (string));

				static void LocalFunctionInner ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
					type.GetMethods ();
				}
			}

			static void LocalFunctionOnGeneric<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
			{
				LocalFunctionInner ();

				static void LocalFunctionInner ()
				{
					typeof(T).GetMethods ();
				}
			}

			static IEnumerable<int> IteratorOnGeneric<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
			{
				yield return 0;
			}

			static async Task AsyncOnGeneric<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
			{
				await Task.Delay (100);
			}

			static async IAsyncEnumerable<int> AsyncIteratorOnGeneric<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
			{
				yield return 0;
				await Task.Delay (100);
			}

			[ExpectedWarning ("IL2118", "<" + nameof (LambdaOnGeneric) + ">", ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LocalFunctionOnGeneric) + ">", ProducedBy = Tool.Trimmer)]
			public static void Test ()
			{
				typeof (DAMReflectionAccessToCompilerGeneratedCode).RequiresAll ();
			}
		}

		[RequiresUnreferencedCode ("--MethodWithRequires--")]
		[RequiresAssemblyFiles ("--MethodWithRequires--")]
		[RequiresDynamicCode ("--MethodWithRequires--")]
		static void MethodWithRequires ()
		{
		}

		static async Task<int> MethodAsync ()
		{
			return await Task.FromResult (0);
		}


		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type GetWithPublicMethods () => null;

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static Type GetWithPublicFields () => null;

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		static Type GetAll () => null;

		[DllImport ("Foo")]
		static extern int MethodTakingPrimitiveType (int num);

		[DllImport ("Foo")]
		static extern void MethodTakingObject ([MarshalAs (UnmanagedType.IUnknown)] object obj);

		class TestType { }
	}
}
