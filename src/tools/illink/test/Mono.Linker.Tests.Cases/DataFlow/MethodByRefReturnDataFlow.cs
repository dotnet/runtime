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
	public class MethodByRefReturnDataFlow
	{
		public static void Main ()
		{
			ReturnAnnotatedTypeReferenceAsUnannotated ();
			AssignToAnnotatedTypeReference ();
			AssignDirectlyToAnnotatedTypeReference ();
			AssignToCapturedAnnotatedTypeReference ();
			AssignToAnnotatedTypeReferenceWithRequirements ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type _annotatedField;

		// This should warn, as assiging to the return ref Type will assign value to the annotated field
		// but the annotation is not propagated
		// https://github.com/dotnet/linker/issues/2158
		static ref Type ReturnAnnotatedTypeReferenceAsUnannotated () { return ref _annotatedField; }

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static ref Type ReturnAnnotatedTypeReferenceAsAnnotated () { return ref _annotatedField; }

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static ref Type ReturnAnnotatedTypeWithRequirements ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type t) => ref _annotatedField;

		// Correct behavior in the linker, but needs to be added in analyzer
		// Bug link: https://github.com/dotnet/linker/issues/2158
		[ExpectedWarning ("IL2026", "Message for --TestType.Requires--", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
		static void AssignToAnnotatedTypeReference ()
		{
			ref Type typeShouldHaveAllMethods = ref ReturnAnnotatedTypeReferenceAsAnnotated ();
			typeShouldHaveAllMethods = typeof (TestTypeWithRequires); // This should apply the annotation -> cause IL2026 due to RUC method
			_annotatedField.GetMethods (); // Doesn't warn, but now contains typeof(TestType) - no warning here is correct
		}

		// Same as above for IL analysis, but this looks different to the Roslyn analyzer.
		// https://github.com/dotnet/linker/issues/2158
		[ExpectedWarning ("IL2026", "Message for --TestType.Requires--", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
		static void AssignDirectlyToAnnotatedTypeReference ()
		{
			ReturnAnnotatedTypeReferenceAsAnnotated () = typeof (TestTypeWithRequires);
			_annotatedField.GetMethods ();
		}

		// https://github.com/dotnet/linker/issues/2158
		[ExpectedWarning ("IL2073", nameof (GetWithPublicFields), ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
		static void AssignToCapturedAnnotatedTypeReference ()
		{
			// In this testcase, the Roslyn analyzer sees an assignment to a flow-capture reference.
			ReturnAnnotatedTypeReferenceAsAnnotated () = GetWithPublicMethods () ?? GetWithPublicFields ();
		}

		[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (ReturnAnnotatedTypeWithRequirements))]
		[ExpectedWarning ("IL2073", nameof (ReturnAnnotatedTypeWithRequirements), nameof (GetWithPublicFields), ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
		static void AssignToAnnotatedTypeReferenceWithRequirements ()
		{
			ReturnAnnotatedTypeWithRequirements (GetWithPublicMethods ()) = GetWithPublicFields ();
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type GetWithPublicMethods () => null;

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static Type GetWithPublicFields () => null;

		public class TestTypeWithRequires
		{
			[RequiresUnreferencedCode ("Message for --TestType.Requires--")]
			public static void Requires () { }
		}
	}
}
