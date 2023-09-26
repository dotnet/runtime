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
	[ExpectedNoWarnings]
	public class ConstructorDataFlow
	{
		public static void Main ()
		{
			DataFlowInConstructor.Test ();
			DataFlowInStaticConstructor.Test ();
		}

		class DataFlowInConstructor
		{
			[ExpectedWarning ("IL2072", nameof (GetUnknown), nameof (RequireAll))]
			public DataFlowInConstructor ()
			{
				RequireAll (GetUnknown ());
			}

			[ExpectedWarning ("IL2072", nameof (GetUnknown), nameof (RequireAll), CompilerGeneratedCode = true)]
			int field = RequireAll (GetUnknown ());

			[ExpectedWarning ("IL2072", nameof (GetUnknown), nameof (RequireAll), CompilerGeneratedCode = true)]
			int Property { get; } = RequireAll (GetUnknown ());

			// The analyzer dataflow visitor asserts that we only see a return value
			// inside of an IMethodSymbol. This testcase checks that we don't hit asserts
			// in case the return statement is in a lambda owned by a field.
			// When the lambda is analyzed, the OwningSymbol is still an IMethodSymbol
			// (the symbol representing the lambda, not the field).
			int fieldWithReturnStatementInInitializer = Execute(
				[ExpectedWarning ("IL2072", nameof (GetUnknown), nameof (RequireAll))]
				() => {
					return RequireAll (GetUnknown ());
				});

			// When analyzer visits the lambda, its containing symbol is the compiler-generated
			// backing field of the property, not the property itself.
			Func<int> PropertyWithReturnStatementInInitializer { get; } =
			[ExpectedWarning ("IL2072", nameof (GetUnknown), nameof (RequireAll))]
			() => {
				return RequireAll (GetUnknown ());
			};

			// For property accessors, the containing symbol is the accessor method.
			Func<int> PropertyWithReturnStatementInGetter =>
			[ExpectedWarning ("IL2072", nameof (GetUnknown), nameof (RequireAll))]
			() => {
				return RequireAll (GetUnknown ());
			};

			static int Execute(Func<int> f) => f();

			public static void Test ()
			{
				var instance = new DataFlowInConstructor ();
				var _ = instance.PropertyWithReturnStatementInGetter;
			}
		}

		class DataFlowInStaticConstructor
		{
			[ExpectedWarning ("IL2072", nameof (GetUnknown), nameof (RequireAll))]
			static DataFlowInStaticConstructor ()
			{
				RequireAll (GetUnknown ());
			}

			[ExpectedWarning ("IL2072", nameof (GetUnknown), nameof (RequireAll), CompilerGeneratedCode = true)]
			static int field = RequireAll (GetUnknown ());

			[ExpectedWarning ("IL2072", nameof (GetUnknown), nameof (RequireAll), CompilerGeneratedCode = true)]
			static int Property { get; } = RequireAll (GetUnknown ());

			public static void Test ()
			{
			}
		}

		static Type GetUnknown () => null;

		static int RequireAll ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type) => 0;
	}
}
