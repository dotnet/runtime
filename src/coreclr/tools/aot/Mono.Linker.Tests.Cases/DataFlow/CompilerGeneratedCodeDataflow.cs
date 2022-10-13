// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SetupCompileArgument ("/unsafe")]
	[ExpectedNoWarnings]
	public class CompilerGeneratedCodeDataflow
	{
		public static void Main ()
		{
			Iterator.Test ();
			Async.Test ();
			AsyncIterator.Test ();
			LocalFunction.Test ();
			Lambda.Test ();
			Complex.Test ();
		}

		class Iterator
		{
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
				CompilerGeneratedCode = true)]
			static IEnumerable<int> FlowAcrossYieldReturn ()
			{
				Type t = GetWithPublicMethods ();
				yield return 0;
				t.RequiresAll ();
			}

			// Linker tracks all assignments of hoisted locals, so this produces warnings.
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresPublicFields), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresPublicMethods), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
			static IEnumerable<int> NoFlowAcrossYieldReturn ()
			{
				Type t = GetWithPublicMethods ();
				t.RequiresPublicMethods ();
				yield return 0;
				t = GetWithPublicFields ();
				t.RequiresPublicFields ();
			}

			[ExpectedWarning ("IL2067", "publicMethodsType", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			static IEnumerable<int> UseParameterBeforeYieldReturn ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type publicMethodsType = null)
			{
				publicMethodsType.RequiresAll ();
				yield return 0;
			}

			[ExpectedWarning ("IL2067", "unknownType", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			static IEnumerable<int> UseUnannotatedParameterBeforeYieldReturn (Type unknownType = null)
			{
				unknownType.RequiresAll ();
				yield return 0;
			}

			[ExpectedWarning ("IL2067", "publicMethodsType", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			static IEnumerable<int> FlowParameterAcrossYieldReturn ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type publicMethodsType = null)
			{
				yield return 0;
				publicMethodsType.RequiresAll ();
			}

			[ExpectedWarning ("IL2067", "unknownType", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			static IEnumerable<int> FlowUnannotatedParameterAcrossYieldReturn (Type unknownType = null)
			{
				yield return 0;
				unknownType.RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			// Linker includes backwards branches for hoisted locals, by virtue of tracking all assignments.
			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			static IEnumerable<int> FlowAcrossYieldReturnWithBackwardsBranch (int n = 0)
			{
				Type t = GetWithPublicMethods ();
				for (int i = 0; i < n; i++) {
					yield return 0;
					t.RequiresAll ();
					yield return 1;
					t = GetWithPublicFields ();
				}
			}

			public static void Test ()
			{
				FlowAcrossYieldReturn ().GetEnumerator ().MoveNext (); // Has to call MoveNext otherwise AOT will actually remove it
				NoFlowAcrossYieldReturn ();
				NoFlowAcrossYieldReturn ();
				UseParameterBeforeYieldReturn ();
				UseUnannotatedParameterBeforeYieldReturn ();
				FlowParameterAcrossYieldReturn ();
				FlowUnannotatedParameterAcrossYieldReturn ();
				FlowAcrossYieldReturnWithBackwardsBranch ();
			}
		}

		class Async
		{
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
				CompilerGeneratedCode = true)]
			static async void FlowAcrossAwait ()
			{
				Type t = GetWithPublicMethods ();
				await MethodAsync ();
				t.RequiresAll ();
			}

			// Linker tracks all assignments of hoisted locals, so this produces warnings.
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresPublicFields), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresPublicMethods), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
			static async void NoFlowAcrossAwait ()
			{
				Type t = GetWithPublicMethods ();
				t.RequiresPublicMethods ();
				await MethodAsync ();
				t = GetWithPublicFields ();
				t.RequiresPublicFields ();
			}

			[ExpectedWarning ("IL2067", "publicMethodsType", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			static async void FlowParameterAcrossAwait ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type publicMethodsType = null)
			{
				await MethodAsync ();
				publicMethodsType.RequiresAll ();
			}

			public static void Test ()
			{
				FlowAcrossAwait ();
				NoFlowAcrossAwait ();
				FlowParameterAcrossAwait ();
			}
		}

		class AsyncIterator
		{
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			static async IAsyncEnumerable<int> FlowAcrossAwaitAndYieldReturn ()
			{
				Type t = GetWithPublicMethods ();
				await MethodAsync ();
				yield return 0;
				t.RequiresAll ();
			}

			public static void Test ()
			{
				FlowAcrossAwaitAndYieldReturn ();
			}
		}

		class LocalFunction
		{
			static void WarningsInBody ()
			{
				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				static void LocalFunction ()
				{
					Type t = GetWithPublicMethods ();
					t.RequiresAll ();
				}

				LocalFunction ();
			}

			static void WarningsInBodyUnused ()
			{
				// Trimmer doesn't warn because this is unused code.
				static void LocalFunction ()
				{
					Type t = GetWithPublicMethods ();
					t.RequiresAll ();
				}
			}

			static void ReadCapturedVariable ()
			{
				Type t = GetWithPublicMethods ();
				LocalFunction ();

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				void LocalFunction ()
				{
					t.RequiresAll ();
				}
			}

			static void ReadMergedCapturedVariable (bool b = false)
			{
				Type t;
				if (b) {
					t = GetWithPublicMethods ();
				} else {
					t = GetWithPublicFields ();
				}

				LocalFunction ();

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
				void LocalFunction () => t.RequiresAll ();
			}

			static void ReadCapturedVariableInMultipleBranches (bool b = false)
			{
				Type t;
				if (b) {
					t = GetWithPublicMethods ();
					LocalFunction ();
				} else {
					t = GetWithPublicFields ();
					LocalFunction ();
				}

				LocalFunction ();

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
				void LocalFunction () => t.RequiresAll ();
			}

			static void ReadCapturedVariableInMultipleBranchesDistinct (bool b = false)
			{
				Type t;
				if (b) {
					t = GetWithPublicMethods ();
					LocalFunctionRequiresMethods ();
				} else {
					t = GetWithPublicFields ();
					LocalFunctionRequiresFields ();
				}

				// We include all writes, including ones that can't reach the local function invocation.
				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresPublicFields))]
				void LocalFunctionRequiresFields () => t.RequiresPublicFields ();
				// We include all writes, including ones that can't reach the local function invocation.
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
				void LocalFunctionRequiresMethods () => t.RequiresPublicMethods ();
			}

			static void ReadCapturedVariableWithBackwardsBranch (int i = 0)
			{
				Type t = GetWithPublicMethods ();
				while (true) {
					LocalFunction ();
					if (i++ == 5)
						break;
					t = GetWithPublicFields ();
				}

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				// Linker includes backwards branches for hoisted locals, by virtue of tracking all assignments.
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
				void LocalFunction () => t.RequiresAll ();
			}

			static void ReadCapturedVariableInMultipleFunctions ()
			{
				Type t = GetWithPublicMethods ();
				LocalFunction ();

				CallerOfLocalFunction ();

				void CallerOfLocalFunction ()
				{
					t = GetWithPublicFields ();
					LocalFunction ();
				}

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
				void LocalFunction () => t.RequiresAll ();
			}

			static void ReadCapturedVariableInCallGraphCycle ()
			{
				Type t = GetUnknownType ();
				A ();

				void A ()
				{
					t = GetWithPublicMethods ();
					LocalFunction ();
					B ();
				}

				void B ()
				{
					t = GetWithPublicFields ();
					LocalFunction ();
					A ();
				}

				[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (DataFlowTypeExtensions.RequiresAll))]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
				void LocalFunction () => t.RequiresAll ();
			}

			public static void ReadCapturedParameter (Type tParameter = null)
			{
				LocalFunction ();

				[ExpectedWarning ("IL2067", nameof (ReadCapturedParameter), "tParameter", nameof (DataFlowTypeExtensions.RequiresAll))]
				void LocalFunction () => tParameter.RequiresAll ();
			}

			public static void ReadCapturedAnnotatedParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type tParameter = null)
			{
				LocalFunction ();

				[ExpectedWarning ("IL2067", nameof (ReadCapturedAnnotatedParameter), "tParameter", nameof (DataFlowTypeExtensions.RequiresAll))]
				void LocalFunction () => tParameter.RequiresAll ();
			}

			public static void ReadCapturedParameterAfterWrite (Type tParameter = null)
			{
				tParameter = GetWithPublicMethods ();
				LocalFunction ();

				// We produce dataflow warnings for the unknown parameter even though it has been overwritten
				// with a value that satisfies the requirement.
				[ExpectedWarning ("IL2067", "tParameter", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
				void LocalFunction () => tParameter.RequiresPublicMethods ();
			}

			[ExpectedWarning ("IL2072", "tParameter", nameof (GetWithPublicFields), ProducedBy = ProducedBy.Analyzer)]
			public static void ReadCapturedParameterAfterWriteMismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type tParameter = null)
			{
				tParameter = GetWithPublicFields ();
				LocalFunction ();

				[ExpectedWarning ("IL2067", "tParameter", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
				void LocalFunction () => tParameter.RequiresPublicFields ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
			static void ReadCapturedVariableWithUnhoistedLocals ()
			{
				Type t = GetWithPublicMethods ();
				Type notCaptured = GetWithPublicFields ();
				LocalFunction ();
				notCaptured.RequiresAll ();

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				void LocalFunction () => t.RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
			// We include all writes, including ones that can't reach the local function invocation.
			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
			static void WriteCapturedVariable ()
			{
				Type t = GetWithPublicFields ();
				LocalFunction ();
				t.RequiresAll ();

				void LocalFunction () => t = GetWithPublicMethods ();
			}

			public static void Test ()
			{
				WarningsInBody ();
				WarningsInBodyUnused ();
				ReadCapturedVariable ();
				ReadMergedCapturedVariable ();
				ReadCapturedVariableInMultipleBranches ();
				ReadCapturedVariableInMultipleBranchesDistinct ();
				ReadCapturedVariableInMultipleFunctions ();
				ReadCapturedVariableInCallGraphCycle ();
				ReadCapturedVariableWithBackwardsBranch ();
				ReadCapturedParameter ();
				ReadCapturedAnnotatedParameter ();
				ReadCapturedParameterAfterWrite ();
				ReadCapturedParameterAfterWriteMismatch ();
				ReadCapturedVariableWithUnhoistedLocals ();
				WriteCapturedVariable ();
			}
		}

		class Lambda
		{
			static void WarningsInBody ()
			{
				var lambda =
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				() => {
					Type t = GetWithPublicMethods ();
					t.RequiresAll ();
				};

				lambda ();
			}

			static void WarningsInBodyUnused ()
			{
				var lambda =
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				() => {
					Type t = GetWithPublicMethods ();
					t.RequiresAll ();
				};
			}

			static void ReadCapturedVariable ()
			{
				Type t = GetWithPublicMethods ();
				Action lambda =
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				() => t.RequiresAll ();
				lambda ();
			}

			static void ReadCapturedVariableAfterWriteAfterDefinition ()
			{
				Type t = GetWithPublicFields ();

				Action lambda =
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				() => t.RequiresAll ();

				t = GetWithPublicMethods ();
				lambda ();
			}

			public static void ReadCapturedParameter (Type tParameter = null)
			{
				var lambda =
					[ExpectedWarning ("IL2067", nameof (ReadCapturedParameter), "tParameter", nameof (DataFlowTypeExtensions.RequiresAll))]
				() => tParameter.RequiresAll ();

				lambda ();
			}

			public static void ReadCapturedAnnotatedParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type tParameter = null)
			{
				var lambda =
					[ExpectedWarning ("IL2067", nameof (ReadCapturedAnnotatedParameter), "tParameter", nameof (DataFlowTypeExtensions.RequiresAll))]
				() => tParameter.RequiresAll ();

				lambda ();
			}

			public static void ReadCapturedParameterAfterWrite (Type tParameter = null)
			{
				tParameter = GetWithPublicMethods ();
				var lambda =
					// We produce dataflow warnings for the unknown parameter even though it has been overwritten
					// with a value that satisfies the requirement.
					[ExpectedWarning ("IL2067", "tParameter", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
				() => tParameter.RequiresPublicMethods ();
				lambda ();
			}

			[ExpectedWarning ("IL2072", "tParameter", nameof (GetWithPublicFields), ProducedBy = ProducedBy.Analyzer)]
			public static void ReadCapturedParameterAfterWriteMismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type tParameter = null)
			{
				tParameter = GetWithPublicFields ();
				var lambda =
					[ExpectedWarning ("IL2067", "tParameter", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
				() => tParameter.RequiresPublicFields ();
				lambda ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
			static void ReadCapturedVariableWithUnhoistedLocals ()
			{
				Type t = GetWithPublicMethods ();
				Type notCaptured = GetWithPublicFields ();
				Action lambda =
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				() => t.RequiresAll ();
				lambda ();
				notCaptured.RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
			// We include all writes, including ones that can't reach the local function invocation.
			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
			static void WriteCapturedVariable ()
			{
				Type t = GetWithPublicFields ();
				Action lambda = () => t = GetWithPublicMethods ();
				lambda ();
				t.RequiresAll ();
			}

			public static void Test ()
			{
				WarningsInBody ();
				WarningsInBodyUnused ();
				ReadCapturedVariable ();
				ReadCapturedVariableAfterWriteAfterDefinition ();
				ReadCapturedParameter ();
				ReadCapturedAnnotatedParameter ();
				ReadCapturedParameterAfterWrite ();
				ReadCapturedParameterAfterWriteMismatch ();
				ReadCapturedVariableWithUnhoistedLocals ();
				WriteCapturedVariable ();
			}
		}

		class Complex
		{
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			static IEnumerable<int> IteratorWithLocalFunctions ()
			{
				Type t = GetWithPublicMethods ();
				LocalFunction ();

				yield return 0;

				LocalFunction ();
				t = GetWithPublicFields ();
				LocalFunction ();
				t.RequiresAll ();

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
				void LocalFunction () => t.RequiresAll ();
			}

			static void NestedLocalFunctions ()
			{
				Type t = GetWithPublicMethods ();

				OuterLocalFunction ();

				void OuterLocalFunction ()
				{
					InnerLocalFunction ();

					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
					void InnerLocalFunction ()
					{
						t.RequiresAll ();
					}
				}
			}

			static void NestedLambdas ()
			{
				Type t = GetWithPublicMethods ();

				var outerLambda =
				() => {
					var innerLambda =
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
					() => t.RequiresAll ();

					innerLambda ();
				};

				outerLambda ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
			static void LocalFunctionInTryRegion ()
			{
				Type t = GetWithPublicMethods ();

				// This reproduces a bug where the enclosing try region of the local function
				// is part of a different control flow graph (that of the containing method).
				// Add a few basic blocks so that the first basic block of the try would throw
				// an IndexOutOfRangeException if used to index into the local function's basic blocks.
				var r = new Random ();
				int i = 0;
				if (r.Next () == 0)
					i++;
				if (r.Next () == 0)
					i++;
				if (r.Next () == 0)
					i++;

				try {
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
					[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
					void LocalFunction ()
					{
						t = GetWithPublicFields ();
						t.RequiresAll ();
					}

					LocalFunction ();
				} finally {
					t.RequiresAll ();
				}
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
			static void LocalFunctionInFinallyRegion ()
			{
				Type t;

				// This reproduces a bug where the enclosing try region of the local function
				// is part of a different control flow graph (that of the containing method).
				// Add a few basic blocks so that the first basic block of the try would throw
				// an IndexOutOfRangeException if used to index into the local function's basic blocks.
				var r = new Random ();
				int i = 0;
				if (r.Next () == 0)
					i++;
				if (r.Next () == 0)
					i++;
				if (r.Next () == 0)
					i++;

				try {
					t = GetWithPublicMethods ();
				} finally {
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
					[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
					void LocalFunction ()
					{
						t = GetWithPublicFields ();
						t.RequiresAll ();
					}

					LocalFunction ();
					t.RequiresAll ();
				}
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
			static void LambdaInTryRegion ()
			{
				Type t = GetWithPublicMethods ();

				// This reproduces a bug where the enclosing try region of the local function
				// is part of a different control flow graph (that of the containing method).
				// Add a few basic blocks so that the first basic block of the try would throw
				// an IndexOutOfRangeException if used to index into the local function's basic blocks.
				var r = new Random ();
				int i = 0;
				if (r.Next () == 0)
					i++;
				if (r.Next () == 0)
					i++;
				if (r.Next () == 0)
					i++;

				try {
					var lambda =
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
					[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
					() => {
						t = GetWithPublicFields ();
						t.RequiresAll ();
					};

					lambda ();
				} finally {
					t.RequiresAll ();
				}
			}

			public static void Test ()
			{
				IteratorWithLocalFunctions ();
				NestedLocalFunctions ();
				NestedLambdas ();
				LocalFunctionInTryRegion ();
				LocalFunctionInFinallyRegion ();
				LambdaInTryRegion ();
			}
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type GetWithPublicMethods () => null;

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static Type GetWithPublicFields () => null;

		static Type GetUnknownType () => null;

		static async Task<int> MethodAsync ()
		{
			return await Task.FromResult (0);
		}

		struct TestStruct
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			public Type TypeWithPublicFields => null;
		}
	}
}
