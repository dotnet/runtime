// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class MethodByRefReturnDataFlow
	{
		public static void Main ()
		{
			ReturnAnnotatedTypeReferenceAsUnannotated ();
			AssignToAnnotatedTypeReference ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type _annotatedField;

		// This should warn, as assiging to the return ref Type will assign value to the annotated field
		// but the annotation is not propagated
		// https://github.com/dotnet/linker/issues/2158
		// [ExpectedWarning("IL????")]
		static ref Type ReturnAnnotatedTypeReferenceAsUnannotated () { return ref _annotatedField; }

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static ref Type ReturnAnnotatedTypeReferenceAsAnnotated () { return ref _annotatedField; }

		// https://github.com/dotnet/linker/issues/2158
		// [ExpectedWarning("IL2026", "Message for --TestType.Requires--")]
		static void AssignToAnnotatedTypeReference ()
		{
			ref Type typeShouldHaveAllMethods = ref ReturnAnnotatedTypeReferenceAsAnnotated ();
			typeShouldHaveAllMethods = typeof (TestTypeWithRequires); // This should apply the annotation -> cause IL2026 due to RUC method
			_annotatedField.GetMethods (); // Doesn't warn, but now contains typeof(TestType) - no warning here is correct
		}

		public class TestTypeWithRequires
		{
			[RequiresUnreferencedCode ("Message for --TestType.Requires--")]
			public static void Requires () { }
		}
	}
}