// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.DataFlow;
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
			ProducedBy = ProducedBy.Trimmer)]
		[ExpectedWarning ("IL2120", "<" + nameof (BaseIteratorWithCorrectDataflow) + ">", "<t>",
			ProducedBy = ProducedBy.Trimmer)]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		class IteratorStateMachines : BaseTypeWithIteratorStateMachines
		{
			public static IEnumerable<int> IteratorWithoutDataflow ()
			{
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--",
				ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--",
				ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2119", "<" + nameof (IteratorCallsMethodWithRequires) + ">", "MoveNext", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static IEnumerable<int> IteratorCallsMethodWithRequires ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2119", "<" + nameof (IteratorWithCorrectDataflow) + ">", "MoveNext", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2119", "<t_IteratorWithCorrectDataflow>", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static IEnumerable<int> IteratorWithCorrectDataflow ()
			{
				var t_IteratorWithCorrectDataflow = GetAll ();
				yield return 0;
				t_IteratorWithCorrectDataflow.RequiresAll ();
			}

			[ExpectedWarning ("IL2119", "<" + nameof (IteratorWithIntegerDataflow) + ">", "MoveNext", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2119", "<types>", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static IEnumerable<int> IteratorWithIntegerDataflow ()
			{
				int integerLocal = 0;
				yield return 0;
				var types = new Type[] { GetWithPublicMethods (), GetWithPublicFields () };
				types[integerLocal].RequiresPublicMethods ();
			}

			[ExpectedWarning ("IL2119", "<" + nameof (IteratorWithProblematicDataflow) + ">", "MoveNext", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2119", "<t_IteratorWithProblematicDataflow>", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			public static IEnumerable<int> IteratorWithProblematicDataflow ()
			{
				var t_IteratorWithProblematicDataflow = GetWithPublicMethods ();
				yield return 0;
				t_IteratorWithProblematicDataflow.RequiresAll ();
			}

			[ExpectedWarning ("IL2112", nameof (RUCTypeWithIterators) + "()", "--RUCTypeWithIterators--", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[RequiresUnreferencedCode ("--RUCTypeWithIterators--")]
			class RUCTypeWithIterators
			{
				[ExpectedWarning ("IL2112", nameof (StaticIteratorCallsMethodWithRequires), "--RUCTypeWithIterators--",
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2112", "<" + nameof (StaticIteratorCallsMethodWithRequires) + ">", "--RUCTypeWithIterators--", CompilerGeneratedCode = true,
					ProducedBy = ProducedBy.Trimmer)] // state machine ctor
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				public static IEnumerable<int> StaticIteratorCallsMethodWithRequires ()
				{
					yield return 0;
					MethodWithRequires ();
				}

				// BUG: this should also give IL2112 for the InstanceIteratorCallsMethodWithRequires state machine constructor.
				// https://github.com/dotnet/linker/issues/2806
				// [ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithIterators.InstanceIteratorCallsMethodWithRequires) + ">")]
				// With that, the IL2119 warning should also go away.
				[ExpectedWarning ("IL2119", "<" + nameof (InstanceIteratorCallsMethodWithRequires) + ">", "MoveNext", CompilerGeneratedCode = true,
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				public IEnumerable<int> InstanceIteratorCallsMethodWithRequires ()
				{
					yield return 0;
					MethodWithRequires ();
				}
			}

			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithProblematicDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorCallsMethodWithRequires) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithCorrectDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithIntegerDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (BaseIteratorWithCorrectDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithIterators) + "()", "--RUCTypeWithIterators--")]
			// Expect to see warnings about RUC on type, for all static state machine members.
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithIterators.StaticIteratorCallsMethodWithRequires) + "()", "--RUCTypeWithIterators--")]
			[ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithIterators.StaticIteratorCallsMethodWithRequires) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			// BUG: this should also give IL2026 for the InstanceIteratorCallsMethodWithRequires state machine constructor.
			// https://github.com/dotnet/linker/issues/2806
			// [ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithIterators.InstanceIteratorCallsMethodWithRequires) + ">")]
			// With that, the IL2118 warning should also go away.
			[ExpectedWarning ("IL2118", "<" + nameof (RUCTypeWithIterators.InstanceIteratorCallsMethodWithRequires) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithCorrectDataflow) + ">", "<t_IteratorWithCorrectDataflow>",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithProblematicDataflow) + ">", "<t_IteratorWithProblematicDataflow>",
				ProducedBy = ProducedBy.Trimmer)]
			// Technically the access to IteratorWithIntegerDataflow should warn about access to the integer
			// field integerLocal, but our heuristics only warn if the field type satisfies the
			// "IsTypeInterestingForDatafllow" check. This is likely good enough because in most cases the
			// compiler-generated code will have other hoisted fields with types that _are_ interesting for dataflow.
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithIntegerDataflow) + ">", "<types>",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (BaseIteratorWithCorrectDataflow) + ">", "<t>",
				ProducedBy = ProducedBy.Trimmer)]
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
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
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
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncCallsMethodWithRequires) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncWithCorrectDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncWithCorrectDataflow) + ">", "<t_AsyncWithCorrectDataflow>",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncWithProblematicDataflow) + ">", "<t_AsyncWithProblematicDataflow>",
				ProducedBy = ProducedBy.Trimmer)]
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
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
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
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorCallsMethodWithRequires) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorWithCorrectDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorWithCorrectDataflow) + ">", "<t>",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorWithProblematicDataflow) + ">", "<t>",
				ProducedBy = ProducedBy.Trimmer)]
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
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaCallsMethodWithRequires) + ">",
					ProducedBy = ProducedBy.Trimmer)]
				() => MethodWithRequires ();
				lambda ();
			}

			static void LambdaWithCorrectDataflow ()
			{
				var lambda =
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaWithCorrectDataflow) + ">",
					ProducedBy = ProducedBy.Trimmer)]
				() => {
					var t = GetAll ();
					t.RequiresAll ();
				};
				lambda ();
			}

			[ExpectedWarning ("IL2111", "<" + nameof (LambdaWithCorrectParameter) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			static void LambdaWithCorrectParameter ()
			{
				var lambda =
				[ExpectedWarning ("IL2114", "<" + nameof (LambdaWithCorrectParameter) + ">",
					ProducedBy = ProducedBy.Trimmer)]
				([DynamicallyAccessedMembersAttribute (DynamicallyAccessedMemberTypes.All)] Type t) => {
					t.RequiresAll ();
				};
				lambda (null);
			}

			static void LambdaWithProblematicDataflow ()
			{
				var lambda =
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaWithProblematicDataflow) + ">",
					ProducedBy = ProducedBy.Trimmer)]
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
					ProducedBy = ProducedBy.Trimmer)]
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
					ProducedBy = ProducedBy.Trimmer)]
				() => MethodTakingObject (null);
				lambda ();
			}

			[ExpectedWarning ("IL2112", nameof (RUCTypeWithLambdas) + "()", "--RUCTypeWithLambdas--", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[RequiresUnreferencedCode ("--RUCTypeWithLambdas--")]
			class RUCTypeWithLambdas
			{
				public void MethodWithLambdas ()
				{
					var lambda =
					[ExpectedWarning ("IL2119", "<" + nameof (MethodWithLambdas) + ">",
						ProducedBy = ProducedBy.Trimmer)]
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					() => MethodWithRequires ();

					int i = 0;
					var lambdaWithCapturedState =
					[ExpectedWarning ("IL2119", "<" + nameof (MethodWithLambdas) + ">",
						ProducedBy = ProducedBy.Trimmer)]
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					() => {
						i++;
						MethodWithRequires ();
					};

					lambda ();
					lambdaWithCapturedState ();
				}
			}

			[ExpectedWarning ("IL2118", "<" + nameof (LambdaCallsPInvokeTakingObject) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaCallsMethodWithRequires) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithCorrectDataflow) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2111", "<" + nameof (LambdaWithCorrectParameter) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithProblematicDataflow) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithCapturedTypeToDAM) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			// Expect RUC warnings for static, compiler-generated code warnings for instance.
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithLambdas) + "()", "--RUCTypeWithLambdas--")]
			[ExpectedWarning ("IL2118", "<" + nameof (RUCTypeWithLambdas.MethodWithLambdas) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (RUCTypeWithLambdas.MethodWithLambdas) + ">",
				ProducedBy = ProducedBy.Trimmer)]
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
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionCallsMethodWithRequires) + ">",
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => MethodWithRequires ();
				LocalFunction ();
			}

			static void LocalFunctionWithCorrectDataflow ()
			{
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionWithCorrectDataflow) + ">",
					ProducedBy = ProducedBy.Trimmer)]
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
					ProducedBy = ProducedBy.Trimmer)]
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
					ProducedBy = ProducedBy.Trimmer)]
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
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => MethodTakingObject (null);
				LocalFunction ();
			}

			[ExpectedWarning ("IL2112", nameof (RUCTypeWithLocalFunctions) + "()", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[RequiresUnreferencedCode ("--RUCTypeWithLocalFunctions--")]
			class RUCTypeWithLocalFunctions
			{
				public void MethodWithLocalFunctions ()
				{
					[ExpectedWarning ("IL2112", "<" + nameof (MethodWithLocalFunctions) + ">",
						ProducedBy = ProducedBy.Trimmer)]
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					void LocalFunction () => MethodWithRequires ();

					[ExpectedWarning ("IL2112", "<" + nameof (MethodWithLocalFunctions) + ">",
						ProducedBy = ProducedBy.Trimmer)]
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					static void StaticLocalFunction () => MethodWithRequires ();

					int i = 0;
					[ExpectedWarning ("IL2112", "<" + nameof (MethodWithLocalFunctions) + ">",
						ProducedBy = ProducedBy.Trimmer)]
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
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
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionCallsMethodWithRequires),
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithCorrectDataflow),
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithProblematicDataflow),
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithCapturedTypeToDAM),
				ProducedBy = ProducedBy.Trimmer)]
			// Expect RUC warnings for static, compiler-generated code warnings for instance.
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithLocalFunctions) + "()", "--RUCTypeWithLocalFunctions--")]
			[ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithLocalFunctions.MethodWithLocalFunctions) + ">", "LocalFunctionWithCapturedState",
				ProducedBy = ProducedBy.Trimmer)] // displayclass ctor
			[ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithLocalFunctions.MethodWithLocalFunctions) + ">", "StaticLocalFunction",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithLocalFunctions.MethodWithLocalFunctions) + ">", "LocalFunction",
				ProducedBy = ProducedBy.Trimmer)]
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
	}
}
