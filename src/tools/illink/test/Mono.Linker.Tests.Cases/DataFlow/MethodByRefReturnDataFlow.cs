// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

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
			var _ = AnnotatedTypeReferenceAsUnannotatedProperty;
			AssignToAnnotatedTypeReferenceProperty ();
			AssignDirectlyToAnnotatedTypeReferenceProperty ();
			AssignToCapturedAnnotatedTypeReferenceProperty ();
			TestCompoundAssignment (typeof (int));
			TestCompoundAssignmentCapture (typeof (int));
			TestCompoundAssignmentMultipleCaptures (typeof (int), typeof (int));
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

		// Correct behavior in the trimming tools, but needs to be added in analyzer
		// Bug link: https://github.com/dotnet/linker/issues/2158
		[ExpectedWarning ("IL2026", "Message for --TestType.Requires--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		static void AssignToAnnotatedTypeReference ()
		{
			ref Type typeShouldHaveAllMethods = ref ReturnAnnotatedTypeReferenceAsAnnotated ();
			typeShouldHaveAllMethods = typeof (TestTypeWithRequires); // This should apply the annotation -> cause IL2026 due to RUC method
			_annotatedField.GetMethods (); // Doesn't warn, but now contains typeof(TestType) - no warning here is correct
		}

		// Same as above for IL analysis, but this looks different to the Roslyn analyzer.
		// https://github.com/dotnet/linker/issues/2158
		[ExpectedWarning ("IL2026", "Message for --TestType.Requires--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		static void AssignDirectlyToAnnotatedTypeReference ()
		{
			ReturnAnnotatedTypeReferenceAsAnnotated () = typeof (TestTypeWithRequires);
			_annotatedField.GetMethods ();
		}

		// https://github.com/dotnet/linker/issues/2158
		[ExpectedWarning ("IL2073", nameof (GetWithPublicFields), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		static void AssignToCapturedAnnotatedTypeReference ()
		{
			// In this testcase, the Roslyn analyzer sees an assignment to a flow-capture reference.
			ReturnAnnotatedTypeReferenceAsAnnotated () = GetWithPublicMethods () ?? GetWithPublicFields ();
		}

		[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (ReturnAnnotatedTypeWithRequirements))]
		[ExpectedWarning ("IL2073", nameof (ReturnAnnotatedTypeWithRequirements), nameof (GetWithPublicFields), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		static void AssignToAnnotatedTypeReferenceWithRequirements ()
		{
			ReturnAnnotatedTypeWithRequirements (GetWithPublicMethods ()) = GetWithPublicFields ();
		}

		static ref Type AnnotatedTypeReferenceAsUnannotatedProperty => ref _annotatedField;

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static ref Type AnnotatedTypeReferenceAsAnnotatedProperty => ref _annotatedField;

		[ExpectedWarning ("IL2026", "Message for --TestType.Requires--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		static void AssignToAnnotatedTypeReferenceProperty ()
		{
			ref Type typeShouldHaveAllMethods = ref AnnotatedTypeReferenceAsAnnotatedProperty;
			typeShouldHaveAllMethods = typeof (TestTypeWithRequires);
			_annotatedField.GetMethods ();
		}

		// https://github.com/dotnet/linker/issues/2158
		[ExpectedWarning ("IL2026", "Message for --TestType.Requires--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		static void AssignDirectlyToAnnotatedTypeReferenceProperty ()
		{
			AnnotatedTypeReferenceAsAnnotatedProperty = typeof (TestTypeWithRequires);
			_annotatedField.GetMethods ();
		}

		// https://github.com/dotnet/linker/issues/2158
		[ExpectedWarning ("IL2073", nameof (GetWithPublicFields), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		static void AssignToCapturedAnnotatedTypeReferenceProperty ()
		{
			AnnotatedTypeReferenceAsAnnotatedProperty = GetWithPublicMethods () ?? GetWithPublicFields ();
		}

		static int intField;

		static ref int GetRefReturnInt ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type t) => ref intField;

		// Ensure analyzer visits the a ref return in the LHS of a compound assignment.
		[ExpectedWarning ("IL2067", nameof (GetRefReturnInt), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		public static void TestCompoundAssignment (Type t)
		{
			GetRefReturnInt (t) += 0;
		}

		// Ensure analyzer visits LHS of a compound assignment when the assignment target is a flow-capture reference.
		[ExpectedWarning ("IL2067", nameof (GetRefReturnInt), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		public static void TestCompoundAssignmentCapture (Type t, bool b = true)
		{
			GetRefReturnInt (t) += b ? 0 : 1;
		}

		// Same as above, with assignment to a flow-capture reference that references multiple captured values.
		[ExpectedWarning ("IL2067", nameof (GetRefReturnInt), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		[ExpectedWarning ("IL2067", nameof (GetRefReturnInt), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		public static void TestCompoundAssignmentMultipleCaptures (Type t, Type u, bool b = true)
		{
			(b ? ref GetRefReturnInt (t) : ref GetRefReturnInt (u)) += b ? 0 : 1;
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
