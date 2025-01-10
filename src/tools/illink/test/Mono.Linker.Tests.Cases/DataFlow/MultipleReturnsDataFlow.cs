// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	// Optimized code makes it easier to produce a method body with multiple return statements in IL.
	[SetupCompileArgument ("/optimize+")]
	public class MultipleReturnsDataFlow
	{
		public static void Main ()
		{
			MultipleReturnsOfSameValueMismatch ();
		}

		static Type GetUnannotatedType () => null;

		[ExpectedWarning ("IL2073",
			nameof (MultipleReturnsDataFlow) + "." + nameof (MultipleReturnsOfSameValueMismatch))]
		[ExpectedWarning ("IL2073",
			nameof (MultipleReturnsDataFlow) + "." + nameof (MultipleReturnsOfSameValueMismatch))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		static Type MultipleReturnsOfSameValueMismatch (int condition = 0)
		{
			var value = GetUnannotatedType ();
			if (condition is 0)
				return value;

			if (condition is 1)
				return null;

			return GetUnannotatedType ();
		}
	}
}
