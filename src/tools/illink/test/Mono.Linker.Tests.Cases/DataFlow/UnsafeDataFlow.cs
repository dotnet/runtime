// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SetupCompileArgument ("/unsafe")]
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class UnsafeDataFlow
	{
		public static void Main ()
		{
			TestReadFromPointer ();
			TestWriteToPointer ();
			TestWriteToStackAllocedStruct ();
		}

		// We don't analyze the pointer manipulation, so it should produce a warning
		// about reading an unknown type, without crashing the analyzer.
		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll))]
		static unsafe void TestReadFromPointer ()
		{
			int i = 6;
			int* pI = &i;
			Type[] arr = new Type[] { GetWithPublicMethods () };
			arr[*pI].RequiresAll ();
		}

		// We don't analyze the pointer manipulation, so it should produce a warning
		// about reading an unknown type, without crashing the analyzer.
		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll))]
		static unsafe void TestWriteToPointer ()
		{
			int i = 6;
			int* pI = &i;
			*pI = 0;
			Type[] arr = new Type[] { GetWithPublicMethods () };
			arr[i].RequiresAll ();
		}

		// We don't analyze the stackalloc'd struct member, so it should produce a warning
		// about reading an unknown type, without crashing the analyzer.
		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll))]
		static unsafe void TestWriteToStackAllocedStruct ()
		{
			var stackArr = stackalloc S[1];
			stackArr[0] = new S {
				I = 0
			};
			Type[] arr = new Type[] { GetWithPublicMethods () };
			arr[stackArr[0].I].RequiresAll ();
		}

		struct S
		{
			public int I;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type GetWithPublicMethods () => null;
	}
}
