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

			[ExpectedWarning ("IL2074", nameof (GetUnknown), nameof (annotatedField), CompilerGeneratedCode = true)]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			Type annotatedField = GetUnknown ();

			[ExpectedWarning ("IL2074", nameof (GetUnknown), nameof (AnnotatedProperty), CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer | Tool.NativeAot)] // https://github.com/dotnet/runtime/issues/93277
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			Type AnnotatedProperty { get; } = GetUnknown ();

			[ExpectedWarning ("IL2074", nameof (GetUnknown), nameof (AnnotatedPropertyWithSetter), CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer | Tool.NativeAot)] // https://github.com/dotnet/runtime/issues/93277
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			Type AnnotatedPropertyWithSetter { get; set; } = GetUnknown ();

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

			int fieldWithThrowStatementInInitializer = string.Empty.Length == 0 ? throw new Exception() : 0;

			int PropertyWithThrowStatementInInitializer { get; } = string.Empty.Length == 0 ? throw new Exception() : 0;

			[ExpectedWarning ("IL2067", nameof (TryGetUnknown), nameof (RequireAll), CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer | Tool.NativeAot)] // https://github.com/dotnet/linker/issues/2158
			int fieldWithLocalReferenceInInitializer = TryGetUnknown (out var type) ? RequireAll (type) : 0;

			[ExpectedWarning ("IL2067", nameof (TryGetUnknown), nameof (RequireAll), CompilerGeneratedCode = true,
				ProducedBy = Tool.Trimmer | Tool.NativeAot)] // https://github.com/dotnet/linker/issues/2158
			int PropertyWithLocalReferenceInInitializer { get; } = TryGetUnknown (out var type) ? RequireAll (type) : 0;

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

		static bool TryGetUnknown (out Type type)
		{
			type = null;
			return true;
		}

		static int RequireAll ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type) => 0;
	}
}
