// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
	public class ReflectionAccessFromCompilerGeneratedCode
	{
		public static void Main ()
		{
			ReflectionAccessFromStateMachine.Test ();
			ReflectionAccessFromLocalFunction.Test ();
			ReflectionAccessFromLambda.Test ();
		}

		class ReflectionAccessFromStateMachine
		{
			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL2026", "--MethodWithLocalFunctionWithRUC.LocalFunction--", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (TypeWithMethodWithRequires.MethodWithLocalFunctionCallsRUC), "LocalFunction", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2111", nameof (TypeWithMethodWithRequires.MethodWithAnnotations), CompilerGeneratedCode = true)]
			static IEnumerable<int> TestIterator ()
			{
				typeof (TypeWithMethodWithRequires).RequiresAll ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("--TestIteratorWithRUC--")]
			static IEnumerable<int> TestIteratorWithRUC ()
			{
				typeof (TypeWithMethodWithRequires).RequiresAll ();
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL2026", "--MethodWithLocalFunctionWithRUC.LocalFunction--", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (TypeWithMethodWithRequires.MethodWithLocalFunctionCallsRUC), "LocalFunction", CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2111", nameof (TypeWithMethodWithRequires.MethodWithAnnotations), CompilerGeneratedCode = true)]
			static async void TestAsync ()
			{
				typeof (TypeWithMethodWithRequires).RequiresAll ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("--TestAsyncWithRUC--")]
			static async void TestAsyncWithRUC ()
			{
				typeof (TypeWithMethodWithRequires).RequiresAll ();
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--TestIteratorWithRUC--")]
			[ExpectedWarning ("IL2026", "--TestAsyncWithRUC--")]
			public static void Test ()
			{
				TestIterator ();
				TestIteratorWithRUC ();
				TestAsync ();
				TestAsyncWithRUC ();
			}
		}

		class ReflectionAccessFromLocalFunction
		{
			static void TestLocalFunction ()
			{
				[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--")]
				[ExpectedWarning ("IL2026", "--MethodWithLocalFunctionWithRUC.LocalFunction--",
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2118", nameof (TypeWithMethodWithRequires.MethodWithLocalFunctionCallsRUC), "LocalFunction",
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2111", nameof (TypeWithMethodWithRequires.MethodWithAnnotations))]
				void LocalFunction ()
				{
					typeof (TypeWithMethodWithRequires).RequiresAll ();
				}
				LocalFunction ();
			}

			[ExpectedWarning ("IL2026", "--LocalFunction--")]
			static void TestLocalFunctionWithRUC ()
			{
				[RequiresUnreferencedCode ("--LocalFunction--")]
				void LocalFunction ()
				{
					typeof (TypeWithMethodWithRequires).RequiresAll ();
				}
				LocalFunction ();
			}

			[RequiresUnreferencedCode ("--TestLocalFunctionInMethodWithRUC--")]
			static void TestLocalFunctionInMethodWithRUC ()
			{
				void LocalFunction ()
				{
					typeof (TypeWithMethodWithRequires).RequiresAll ();
				}
				LocalFunction ();
			}

			[ExpectedWarning ("IL2026", "--TestLocalFunctionInMethodWithRUC--")]
			public static void Test ()
			{
				TestLocalFunction ();
				TestLocalFunctionWithRUC ();
				TestLocalFunctionInMethodWithRUC ();
			}
		}

		class ReflectionAccessFromLambda
		{
			static void TestLambda ()
			{
				var lambda =
				[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--")]
				[ExpectedWarning ("IL2026", "--MethodWithLocalFunctionWithRUC.LocalFunction--",
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2118", nameof (TypeWithMethodWithRequires.MethodWithLocalFunctionCallsRUC), "LocalFunction",
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2111", nameof (TypeWithMethodWithRequires.MethodWithAnnotations))]
				() => {
					typeof (TypeWithMethodWithRequires).RequiresAll ();
				};
				lambda ();
			}

			[ExpectedWarning ("IL2026", "--TestLambdaInMethodWithRUC--")]
			static void TestLambdaWithRUC ()
			{
				var lambda =
				[RequiresUnreferencedCode ("--TestLambdaInMethodWithRUC--")]
				() => {
					typeof (TypeWithMethodWithRequires).RequiresAll ();
				};
				lambda ();
			}

			[RequiresUnreferencedCode ("--TestLambdaInMethodWithRUC--")]
			static void TestLambdaInMethodWithRUC ()
			{
				var lambda =
				() => {
					typeof (TypeWithMethodWithRequires).RequiresAll ();
				};
				lambda ();
			}

			[ExpectedWarning ("IL2026", "--TestLambdaInMethodWithRUC--")]
			public static void Test ()
			{
				TestLambda ();
				TestLambdaWithRUC ();
				TestLambdaInMethodWithRUC ();
			}
		}

		static async Task<int> MethodAsync ()
		{
			return await Task.FromResult (0);
		}

		class TypeWithMethodWithRequires
		{
			[RequiresUnreferencedCode ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			[RequiresAssemblyFiles ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			[RequiresDynamicCode ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			public static void MethodWithRequires ()
			{
			}

			public static void MethodWithAnnotations ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type t) { }

			[ExpectedWarning ("IL2026", "--MethodWithLocalFunctionWithRUC.LocalFunction--")]
			public static void MethodWithLocalFunctionWithRUC ()
			{
				[RequiresUnreferencedCode ("--MethodWithLocalFunctionWithRUC.LocalFunction--")]
				void LocalFunction ()
				{ }
				LocalFunction ();
			}

			public static void MethodWithLocalFunctionCallsRUC ()
			{
				[ExpectedWarning ("IL2026", "--MethodWithRUC--")]
				void LocalFunction () => MethodWithRUC ();
				LocalFunction ();
			}
		}

		[RequiresUnreferencedCode ("--MethodWithRUC--")]
		static void MethodWithRUC () { }
	}
}
