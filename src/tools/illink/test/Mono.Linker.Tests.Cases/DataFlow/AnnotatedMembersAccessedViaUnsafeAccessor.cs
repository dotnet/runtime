// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class AnnotatedMembersAccessedViaUnsafeAccessor
	{
		// NativeAOT has basically the best behavior in these cases - it produces the same warnings
		// as if the accessor has a direct access to the target member. So if the accessor is correctly annotated
		// there will be no warnings.
		//
		// Trimmer is the worst - due to the fact that we mark all methods with a given name, we can't compare
		// annotations and thus we need to warn on all annotated methods. In addition the accesses are modeled
		// as reflection accesses so the warning codes are "Reflection" codes. When/If we implement
		// correct target resolution, we should be able to emulate the behavior of NativeAOT
		//
		// Analyzer doesn't warn at all in these cases - analyzer simply can't resolve targets (at least sometimes)
		// and so for now we're not doing anything with UnsafeAccessor in the analyzer.

		public static void Main ()
		{
			MethodWithAnnotatedParameter (null, null);
			StaticMethodWithAnnotatedReturnValue (null);
			VirtualMethodWithAnnotatedReturnValue (null);
			AnnotatedField (null);

			MethodWithAnnotationMismatch (null, null);
			FieldWithAnnotationMismatch (null);
		}

		class Target
		{
			private static void MethodWithAnnotatedParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) { }

			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			private static Type StaticMethodWithAnnotatedReturnValue () => null;

			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			protected virtual Type VirtualMethodWithAnnotatedReturnValue () => null;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			private static Type AnnotatedField;

			private static void MethodWithAnnotationMismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) { }

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			private static Type FieldWithAnnotationMismatch;
		}

		[UnexpectedWarning ("IL2111", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/101195")]
		[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
		extern static void MethodWithAnnotatedParameter (Target target, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type);

		// No warning - reflection access to a static method with annotated return value is not a problem
		[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		extern static Type StaticMethodWithAnnotatedReturnValue (Target target);

		[UnexpectedWarning ("IL2111", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/101195")]
		[UnsafeAccessor (UnsafeAccessorKind.Method)]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		extern static Type VirtualMethodWithAnnotatedReturnValue (Target target);

		[UnexpectedWarning ("IL2110", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/101195")]
		[UnsafeAccessor (UnsafeAccessorKind.StaticField)]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		extern static ref Type AnnotatedField (Target target);

		[UnexpectedWarning ("IL2111", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/101195")]
		[ExpectedWarning ("IL2067", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101195")]
		[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
		extern static void MethodWithAnnotationMismatch (Target target, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type);

		[UnexpectedWarning ("IL2110", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/101195")]
		[ExpectedWarning ("IL2078", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101195")]
		[UnsafeAccessor (UnsafeAccessorKind.StaticField)]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		extern static ref Type FieldWithAnnotationMismatch (Target target);
	}
}
