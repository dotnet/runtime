// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class RequiresDynamicCodeForMakeGenericType
	{
		public static void Main()
		{
			TestWithReferenceTypeCheck();
			TestWithValueTypeCheck();
			TestWithUnknownType(typeof(int));
			TestWithClassConstraint<string>();
			TestWithStructConstraint<int>();
			TestWithNoConstraint<string>();
		}

		class Generic<T>
		{
		}

		// Should NOT warn - we know t is not a value type
		static void TestWithReferenceTypeCheck()
		{
			Type t = typeof(string);
			if (!t.IsValueType)
			{
				typeof(Generic<>).MakeGenericType(t);
			}
		}

		// Should warn - we know t is a value type
		[ExpectedWarning("IL3050", "MakeGenericType", Tool.NativeAot, "")]
		static void TestWithValueTypeCheck()
		{
			Type t = typeof(int);
			if (t.IsValueType)
			{
				typeof(Generic<>).MakeGenericType(t);
			}
		}

		// Should warn - we don't know what type t is
		[ExpectedWarning("IL3050", "MakeGenericType", Tool.NativeAot, "")]
		static void TestWithUnknownType(Type t)
		{
			typeof(Generic<>).MakeGenericType(t);
		}

		// Should NOT warn - T has class constraint (reference type)
		static void TestWithClassConstraint<T>() where T : class
		{
			typeof(Generic<>).MakeGenericType(typeof(T));
		}

		// Should warn - T has struct constraint (value type)
		[ExpectedWarning("IL3050", "MakeGenericType", Tool.NativeAot, "")]
		static void TestWithStructConstraint<T>() where T : struct
		{
			typeof(Generic<>).MakeGenericType(typeof(T));
		}

		// Should warn - T has no constraint
		[ExpectedWarning("IL3050", "MakeGenericType", Tool.NativeAot, "")]
		static void TestWithNoConstraint<T>()
		{
			typeof(Generic<>).MakeGenericType(typeof(T));
		}
	}
}
