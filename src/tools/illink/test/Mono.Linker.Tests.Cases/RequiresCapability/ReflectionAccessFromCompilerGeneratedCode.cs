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
			[ExpectedWarning ("IL3002", "--TypeWithMethodWithRequires.MethodWithRequires--", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101998", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", "--TypeWithMethodWithRequires.MethodWithRequires--", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101998", CompilerGeneratedCode = true)]
			[UnexpectedWarning ("IL2118", nameof (TypeWithMethodWithRequires.MethodWithLocalFunctionCallsRUC), "LocalFunction", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/85042", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL2111", nameof (TypeWithMethodWithRequires.MethodWithAnnotations), CompilerGeneratedCode = true)]
			static IEnumerable<int> TestIterator ()
			{
				typeof (TypeWithMethodWithRequires).RequiresAll ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("--TestIteratorWithRUC--")]
			[RequiresAssemblyFiles ("--TestIteratorWithRUC--")]
			[RequiresDynamicCode ("--TestIteratorWithRUC--")]
			static IEnumerable<int> TestIteratorWithRUC ()
			{
				typeof (TypeWithMethodWithRequires).RequiresAll ();
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--TypeWithMethodWithRequires.MethodWithRequires--", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101998", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", "--TypeWithMethodWithRequires.MethodWithRequires--", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101998", CompilerGeneratedCode = true)]
			[UnexpectedWarning ("IL2118", nameof (TypeWithMethodWithRequires.MethodWithLocalFunctionCallsRUC), "LocalFunction", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/85042", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL2111", nameof (TypeWithMethodWithRequires.MethodWithAnnotations), CompilerGeneratedCode = true)]
			static async void TestAsync ()
			{
				typeof (TypeWithMethodWithRequires).RequiresAll ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("--TestAsyncWithRUC--")]
			[RequiresAssemblyFiles ("--TestAsyncWithRUC--")]
			[RequiresDynamicCode ("--TestAsyncWithRUC--")]
			static async void TestAsyncWithRUC ()
			{
				typeof (TypeWithMethodWithRequires).RequiresAll ();
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--TestIteratorWithRUC--")]
			[ExpectedWarning ("IL3002", "--TestIteratorWithRUC--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			[ExpectedWarning ("IL3050", "--TestIteratorWithRUC--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			[ExpectedWarning ("IL2026", "--TestAsyncWithRUC--")]
			[ExpectedWarning ("IL3002", "--TestAsyncWithRUC--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			[ExpectedWarning ("IL3050", "--TestAsyncWithRUC--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			public static void Test ()
			{
				TestIterator ().GetEnumerator ().MoveNext (); // Must actually use the enumerator, otherwise NativeAOT will trim the implementation
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
				[ExpectedWarning ("IL3002", "--TypeWithMethodWithRequires.MethodWithRequires--", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101998")]
				[ExpectedWarning ("IL3050", "--TypeWithMethodWithRequires.MethodWithRequires--", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101998")]
				[UnexpectedWarning ("IL2118", nameof (TypeWithMethodWithRequires.MethodWithLocalFunctionCallsRUC), "LocalFunction", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/85042")]
				[ExpectedWarning ("IL2111", nameof (TypeWithMethodWithRequires.MethodWithAnnotations))]
				void LocalFunction ()
				{
					typeof (TypeWithMethodWithRequires).RequiresAll ();
				}
				LocalFunction ();
			}

			[ExpectedWarning ("IL2026", "--LocalFunction--")]
			[ExpectedWarning ("IL3002", "--LocalFunction--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			[ExpectedWarning ("IL3050", "--LocalFunction--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			static void TestLocalFunctionWithRUC ()
			{
				[RequiresUnreferencedCode ("--LocalFunction--")]
				[RequiresAssemblyFiles ("--LocalFunction--")]
				[RequiresDynamicCode ("--LocalFunction--")]
				void LocalFunction ()
				{
					typeof (TypeWithMethodWithRequires).RequiresAll ();
				}
				LocalFunction ();
			}

			[RequiresUnreferencedCode ("--TestLocalFunctionInMethodWithRUC--")]
			[RequiresAssemblyFiles ("--TestLocalFunctionInMethodWithRUC--")]
			[RequiresDynamicCode ("--TestLocalFunctionInMethodWithRUC--")]
			static void TestLocalFunctionInMethodWithRUC ()
			{
				void LocalFunction ()
				{
					typeof (TypeWithMethodWithRequires).RequiresAll ();
				}
				LocalFunction ();
			}

			[ExpectedWarning ("IL2026", "--TestLocalFunctionInMethodWithRUC--")]
			[ExpectedWarning ("IL3002", "--TestLocalFunctionInMethodWithRUC--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			[ExpectedWarning ("IL3050", "--TestLocalFunctionInMethodWithRUC--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
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
				[ExpectedWarning ("IL3002", "--TypeWithMethodWithRequires.MethodWithRequires--", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101998")]
				[ExpectedWarning ("IL3050", "--TypeWithMethodWithRequires.MethodWithRequires--", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101998")]
				[UnexpectedWarning ("IL2118", nameof (TypeWithMethodWithRequires.MethodWithLocalFunctionCallsRUC), "LocalFunction", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/85042")]
				[ExpectedWarning ("IL2111", nameof (TypeWithMethodWithRequires.MethodWithAnnotations))]
				() => {
					typeof (TypeWithMethodWithRequires).RequiresAll ();
				};
				lambda ();
			}

			[ExpectedWarning ("IL2026", "--TestLambdaInMethodWithRUC--")]
			[ExpectedWarning ("IL3002", "--TestLambdaInMethodWithRUC--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			[ExpectedWarning ("IL3050", "--TestLambdaInMethodWithRUC--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			static void TestLambdaWithRUC ()
			{
				var lambda =
				[RequiresUnreferencedCode ("--TestLambdaInMethodWithRUC--")]
				[RequiresAssemblyFiles ("--TestLambdaInMethodWithRUC--")]
				[RequiresDynamicCode ("--TestLambdaInMethodWithRUC--")]
				() => {
					typeof (TypeWithMethodWithRequires).RequiresAll ();
				};
				lambda ();
			}

			[ExpectedWarning ("IL2026", "--TestLambdaWithRUCLdftn--")]
			[ExpectedWarning ("IL3002", "--TestLambdaWithRUCLdftn--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			[ExpectedWarning ("IL3050", "--TestLambdaWithRUCLdftn--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			static void TestLambdaWithRUCLdftn ()
			{
				var lambda =
				[RequiresUnreferencedCode ("--TestLambdaWithRUCLdftn--")]
				[RequiresAssemblyFiles ("--TestLambdaWithRUCLdftn--")]
				[RequiresDynamicCode ("--TestLambdaWithRUCLdftn--")]
				() => {
					var _ = new Action (TypeWithMethodWithRequires.MethodWithRequires);
					var _2 = new Action<Type> (TypeWithMethodWithRequires.MethodWithAnnotations);
				};
				lambda ();
			}

			[RequiresUnreferencedCode ("--TestLambdaInMethodWithRUC--")]
			[RequiresAssemblyFiles ("--TestLambdaInMethodWithRUC--")]
			[RequiresDynamicCode ("--TestLambdaInMethodWithRUC--")]
			static void TestLambdaInMethodWithRUC ()
			{
				var lambda =
				() => {
					typeof (TypeWithMethodWithRequires).RequiresAll ();
				};
				lambda ();
			}

			[ExpectedWarning ("IL2026", "--TestLambdaInMethodWithRUC--")]
			[ExpectedWarning ("IL3002", "--TestLambdaInMethodWithRUC--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			[ExpectedWarning ("IL3050", "--TestLambdaInMethodWithRUC--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			public static void Test ()
			{
				TestLambda ();
				TestLambdaWithRUC ();
				TestLambdaWithRUCLdftn ();
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
			[ExpectedWarning ("IL3002", "--MethodWithLocalFunctionWithRUC.LocalFunction--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			[ExpectedWarning ("IL3050", "--MethodWithLocalFunctionWithRUC.LocalFunction--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
			public static void MethodWithLocalFunctionWithRUC ()
			{
				[RequiresUnreferencedCode ("--MethodWithLocalFunctionWithRUC.LocalFunction--")]
				[RequiresAssemblyFiles ("--MethodWithLocalFunctionWithRUC.LocalFunction--")]
				[RequiresDynamicCode ("--MethodWithLocalFunctionWithRUC.LocalFunction--")]
				void LocalFunction ()
				{ }
				LocalFunction ();
			}

			public static void MethodWithLocalFunctionCallsRUC ()
			{
				[ExpectedWarning ("IL2026", "--MethodWithRUC--")]
				[ExpectedWarning ("IL3002", "--MethodWithRUC--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
				[ExpectedWarning ("IL3050", "--MethodWithRUC--", Tool.NativeAot | Tool.Analyzer, "NativeAOT Specific Warnings")]
				void LocalFunction () => MethodWithRUC ();
				LocalFunction ();
			}
		}

		[RequiresUnreferencedCode ("--MethodWithRUC--")]
		[RequiresAssemblyFiles ("--MethodWithRUC--")]
		[RequiresDynamicCode ("--MethodWithRUC--")]
		static void MethodWithRUC () { }
	}
}
