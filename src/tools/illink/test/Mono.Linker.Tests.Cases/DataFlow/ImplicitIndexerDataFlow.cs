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

		static void TestTupleSwapWithImplicitIndexer()
		{
			Span<int> values = stackalloc int[2];
			values[0] = 1;
			values[1] = 2;

			// This should not cause an assertion failure in the analyzer
			// Tuple swap using implicit indexer (^1, ^2) on left-hand side of assignment
			(values[^1], values[^2]) = (values[^2], values[^1]);
		}
	}
}
