// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	class FileScopedClasses
	{
		static void Main() {
			BasicKeptValidation.Test ();
			BasicDataFlow.Test ();
			CompilerGeneratedCodeConsistency.Test ();
		}
	}

	[ExpectedNoWarnings]
	file class BasicKeptValidation
	{
		[Kept]
		public static void Test () {
			KeptMethod ();
		}

		[Kept]
		static void KeptMethod () { }

		static void UnusedMethod () { }
	}

	[ExpectedNoWarnings]
	file class BasicDataFlow
	{
		[Kept]
		[ExpectedWarning ("IL2072")]
		public static void Test () {
			RequirePublicFields (GetNone ());
		}

		[Kept]
		static Type GetNone () => null;

		[Kept]
		static void RequirePublicFields ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type t) {}
	}

	[ExpectedNoWarnings]
	file class CompilerGeneratedCodeConsistency
	{
		[Kept]
		public static void Test () {
			TriggerDataflowAnalysis ();

			// Reference a lambda to hit the bug in https://github.com/dotnet/runtime/issues/89191.
			// The bug occurred when dataflow analysis was performed on a method in a file-scoped class,
			// where the method had references to lambdas.
			// This would produce an inconsistency between the set of methods tracked in the compiler-generated
			// state and the dataflow analysis. Dataflow analysis always sees the lambdas, but the
			// compiler-generated state handling treated file-scoped classes like compiler-generated
			// classes, and would not build a type cache of this class.
			var l =
				[Kept]
				static () => "hello";
			l();
		}

		[Kept]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static Type TriggerDataflowAnalysis () => null;
	}
}
