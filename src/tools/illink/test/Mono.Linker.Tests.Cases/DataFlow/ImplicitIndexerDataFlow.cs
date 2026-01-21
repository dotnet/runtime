// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class ImplicitIndexerDataFlow
	{
		public static void Main()
		{
			TestTupleSwapWithImplicitIndexer();
		}

		[ExpectedWarning("IL2026", "--Method--", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/116619")]
		static void TestTupleSwapWithImplicitIndexer()
		{
			Span<Type> types = stackalloc Type[2];
			types[0] = typeof(int);
			types[1] = RequiresUnreferencedCodeMethod();

			// This should not cause an assertion failure in the analyzer
			(types[^1], types[^2]) = (types[^2], types[^1]);
		}

		[System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("--Method--")]
		static Type RequiresUnreferencedCodeMethod() => typeof(string);
	}
}
