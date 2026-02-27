// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class ImplicitIndexerDataFlow
	{
		public static void Main()
		{
			TestTupleSwapWithImplicitIndexer();
			TestDeconstructIndexerReference();
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

		// Similar to DeconstructVariablePropertyReference in ConstructedTypesDataFlow.cs
		// but using an indexer reference (with implicit index ^1) instead of a property reference on the LHS.
		// The analyzer doesn't fully support deconstruction assignments yet (https://github.com/dotnet/runtime/issues/123767)
		// so we expect warnings similar to the property reference case.
		[ExpectedWarning("IL2062", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/123767")]
		[ExpectedWarning("IL2087", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/123767")]
		static void TestDeconstructIndexerReference()
		{
			Type[] types = new Type[2];
			types[0] = GetTypeWithRequirements();
			
			object instance;
			// Deconstruction with indexer reference on LHS using implicit index operator
			// This should behave similarly to property reference deconstruction
			(types[^1], instance) = GetUnannotatedType();
			types[^1].RequiresPublicMethods();
		}

		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type GetTypeWithRequirements() => typeof(int);

		static (Type type, object instance) GetUnannotatedType() => (typeof(string), null);
	}
}
