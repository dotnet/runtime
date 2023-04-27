// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using DAM = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;
using DAMT = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class RefFieldDataFlow
	{
		[Kept]
		// Bug for the IL2069's here: https://github.com/dotnet/runtime/issues/85464
		[ExpectedWarning ("IL2069", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2069", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2069", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2069", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		public static void Main ()
		{
			RefFieldWithMethods withMethods = new (ref fieldWithMethods);
			RefFieldWithFields withFields = new (ref fieldWithFields);
			RefFieldWithMethodsAndFields withMethodsAndFields = new (ref fieldWithMethodsAndFields);
			RefFieldUnannotated unannotated = new (ref field);

			AssignLocals<int, int, int, int> (withMethods);
			AssignRefToLocals<int, int, int, int> (withMethods);
			AssignRefLocals<int, int, int, int> (withMethods, withFields, withMethodsAndFields, unannotated);
			AssignRefsToFields (withMethods);
			AssignParameters (withMethods, typeof (int), typeof (int), typeof (int), typeof (int));
			AssignRefParameters<int, int, int, int> (withMethods, ref field, ref fieldWithMethods, ref fieldWithFields, ref fieldWithMethodsAndFields);
			AssignFields (withMethods, null, null, null, null);
			AssignRefFields (withMethods, unannotated, withMethods, withFields, withMethodsAndFields);
			AssignReturns<int, int, int, int> (withMethods);
			AssignRefReturns<int, int, int, int> (withMethods);
			AssignProperties (withMethods);
			AssignRefProperties (withMethods);
		}
		static Type field = typeof (int);

		[DAM (DAMT.PublicMethods)]
		static Type fieldWithMethods = typeof (int);

		[DAM (DAMT.PublicFields)]
		static Type fieldWithFields = typeof (int);

		[DAM (DAMT.PublicMethods | DAMT.PublicFields)]
		static Type fieldWithMethodsAndFields = typeof (int);

		[ExpectedWarning ("IL2089", "T", "T")]
		[ExpectedWarning ("IL2089", "T", "TF")]
		static void AssignLocals<
			T,
			[DAM (DAMT.PublicMethods)] TM,
			[DAM (DAMT.PublicFields)] TF,
			[DAM (DAMT.PublicFields | DAMT.PublicMethods)] TMF>
			(RefFieldWithMethods target)
		{
			var t = typeof (T);
			target.T = t; // Warn

			t = typeof (TM);
			target.T = t; // Okay

			t = typeof (TF);
			target.T = t; // Warn

			t = typeof (TMF);
			target.T = t; // Okay
		}

		[ExpectedWarning ("IL2089", "T")]
		[ExpectedWarning ("IL2089", "TF")]
		static void AssignRefToLocals<
			T,
			[DAM (DAMT.PublicMethods)] TM,
			[DAM (DAMT.PublicFields)] TF,
			[DAM (DAMT.PublicFields | DAMT.PublicMethods)] TMF>
			(scoped RefFieldWithMethods target)
		{
			var t = typeof (T);
			target.T = ref t; // Warn

			var tf = typeof (TF);
			target.T = ref tf; // Warn

			var tm = typeof (TM);
			target.T = ref tm;
			tm = typeof (TF); // This is a hole that doesn't warn

			var tmf = typeof (TMF);
			target.T = ref tmf;
			target.T = typeof (TM);
			tmf = typeof (TF); // This is a hole that doesn't warn but assigns a misannotated value to target.T
		}

		[ExpectedWarning ("IL2089", "RefFieldWithMethods", "T", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2089", "RefFieldWithFields", "T", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2089", "RefFieldWithMethodsAndFields", "T", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2089", "RefFieldWithMethodsAndFields", "T", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2089", "RefFieldWithFields", "T", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		static void AssignRefLocals<
			T,
			[DAM (DAMT.PublicMethods)] TM,
			[DAM (DAMT.PublicFields)] TF,
			[DAM (DAMT.PublicFields | DAMT.PublicFields)] TMF>
			(scoped RefFieldWithMethods withMethods,
			 scoped RefFieldWithFields withFields,
			 scoped RefFieldWithMethodsAndFields withMethodsAndFields,
			 scoped RefFieldUnannotated unannotated)
		{
			ref Type t = ref unannotated.T;
			// The following create holes where the local can assign a misannotated value to the annotated ref field
			ref Type tm = ref withMethods.T;
			ref Type tf = ref withFields.T;
			ref Type tmf = ref withMethodsAndFields.T;

			// Okay
			t = typeof (T);
			tf = typeof (TF);
			tm = typeof (TM);
			tmf = typeof (TMF);

			t = typeof (T);
			tm = typeof (T); // Hole: assigns unannotated T to withMethods.T
			tf = typeof (T); // Hole: assigns unannotated T to withFields.T
			tmf = typeof (T); // Hole: assigns unannotated T to withMethodsAndFields.T

			t = ref tf;
			t = typeof (T); // Hole: assigns unannotated T to withFields.T
		}

		[ExpectedWarning ("IL2079", "UnannotatedField.T")]
		static void AssignRefsToFields (RefFieldWithMethods target)
		{
			var x = new UnannotatedField { T = typeof (int) };
			target.T = ref x.T; // Warn

			var withMethods = new FieldWithMethods { T = typeof (int) };
			target.T = ref withMethods.T; // Okay

			var withMethodsAndFields = new FieldWithMethodsAndFields { T = typeof (int) };
			target.T = ref withMethodsAndFields.T; // Creates hole
			target.T = withMethods.T; // Hole: assigns a value with Methods to a value annotated with Methods and Fields
		}

		[ExpectedWarning ("IL2069", "RefFieldWithMethods.T", "param")]
		[ExpectedWarning ("IL2069", "RefFieldWithMethods.T", "param")]
		[ExpectedWarning ("IL2069", "RefFieldWithMethods.T", "paramWithFields")]
		[ExpectedWarning ("IL2069", "RefFieldWithMethods.T", "paramWithFields")]
		static void AssignParameters (scoped RefFieldWithMethods target,
			Type param,
			[DAM (DAMT.PublicMethods)] Type paramWithMethods,
			[DAM (DAMT.PublicFields)] Type paramWithFields,
			[DAM (DAMT.PublicMethods | DAMT.PublicFields)] Type paramWithMethodsAndFields)
		{
			target.T = param; // Warn
			target.T = ref param; // Warn

			target.T = paramWithMethods;
			target.T = ref paramWithMethods;

			target.T = paramWithFields; // Warn
			target.T = ref paramWithFields; // Warn

			target.T = paramWithMethodsAndFields; // Okay
			target.T = ref paramWithMethodsAndFields; // Creates hole
			target.T = paramWithMethods; // Hole: assigns value with Methods to a value annotated with Methods and Fields
		}

		[ExpectedWarning ("IL2069", "RefFieldWithMethods.T", "param")]
		[ExpectedWarning ("IL2069", "RefFieldWithMethods.T", "paramWithFields")]
		[ExpectedWarning ("IL2077", "paramWithMethodsAndFields", "RefFieldWithMethods.T")]
		// ILLink doesn't recognize ldind.ref
		// https://github.com/dotnet/runtime/issues/85465
		// IL2064's are bugs - shouldn't be unknown values
		[ExpectedWarning ("IL2064", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2064", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2064", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2064", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2064", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2069", "RefFieldWithMethods.T", "param", ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2069", "RefFieldWithMethods.T", "paramWithFields", ProducedBy = Tool.Analyzer)]
		static void AssignRefParameters<
			T,
			[DAM (DAMT.PublicMethods)] TM,
			[DAM (DAMT.PublicFields)] TF,
			[DAM (DAMT.PublicFields | DAMT.PublicMethods)] TMF>
			(scoped RefFieldWithMethods target,
			scoped ref Type param,
			[DAM (DAMT.PublicMethods)] scoped ref Type paramWithMethods,
			[DAM (DAMT.PublicFields)] scoped ref Type paramWithFields,
			[DAM (DAMT.PublicMethods | DAMT.PublicFields)] scoped ref Type paramWithMethodsAndFields)
		{
			target.T = param; // Warn
			target.T = ref param; // Warn

			target.T = paramWithMethods;
			target.T = ref paramWithMethods;

			target.T = paramWithFields; // Warn
			target.T = ref paramWithFields; // Warn

			target.T = paramWithMethodsAndFields; // Okay
			target.T = ref paramWithMethodsAndFields; // Creates hole
			target.T = paramWithMethods; // Hole: assigns value with Methods to a value annotated with Methods and Fields

			param = ref target.T; // Creates hole
			param = typeof (T); // Hole: assigns unannotated value to location pointed to by annotated ref field

			paramWithMethodsAndFields = ref target.T; // Warn
		}

		[ExpectedWarning ("IL2079", "RefFieldWithMethods.T", "UnannotatedField.T")]
		[ExpectedWarning ("IL2079", "RefFieldWithMethods.T", "UnannotatedField.T")]
		[ExpectedWarning ("IL2079", "RefFieldWithMethods.T", "FieldWithFields.T")]
		[ExpectedWarning ("IL2079", "RefFieldWithMethods.T", "FieldWithFields.T")]
		static void AssignFields (RefFieldWithMethods target, UnannotatedField unannotated, FieldWithMethods withMethods, FieldWithFields withFields, FieldWithMethodsAndFields withMethodsAndFields)
		{
			target.T = unannotated.T; // Warn
			target.T = withMethods.T;
			target.T = withFields.T; // Warn
			target.T = withMethodsAndFields.T;

			target.T = ref unannotated.T; // Warn
			target.T = ref withMethods.T;
			target.T = ref withFields.T; // Warn
			target.T = ref withMethodsAndFields.T; // Creates hole
			target.T = withMethods.T; // Hole: assigns value with methods to location pointed to by ref field with methods and fields
		}

		[ExpectedWarning ("IL2079", "RefFieldWithMethods.T", "RefFieldUnannotated.T")]
		[ExpectedWarning ("IL2079", "RefFieldWithMethods.T", "RefFieldWithFields.T")]
		[ExpectedWarning ("IL2079", "RefFieldWithMethods.T", "RefFieldUnannotated.T", ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2079", "RefFieldWithMethods.T", "RefFieldWithFields.T", ProducedBy = Tool.Analyzer)]
		// IL2064's are bugs - shouldn't be unknown values
		// https://github.com/dotnet/runtime/issues/85465
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = unannotated.T;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = withMethods.T;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = withFields.T;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = withMethodsAndFields.T;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = withMethodsAndFields.T;
		static void AssignRefFields (
			RefFieldWithMethods target,
			RefFieldUnannotated unannotated,
			RefFieldWithMethods withMethods,
			RefFieldWithFields withFields,
			RefFieldWithMethodsAndFields withMethodsAndFields)
		{
			target.T = unannotated.T; // Warn
			target.T = withMethods.T;
			target.T = withFields.T; // Warn
			target.T = withMethodsAndFields.T;

			target.T = ref unannotated.T; // Warn
			target.T = ref withMethods.T;
			target.T = ref withFields.T; // Warn
			target.T = ref withMethodsAndFields.T; // Creates hole
			target.T = withMethods.T; // Hole: assigns value with methods to location pointed to by ref field with methods and fields
		}

		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "GetRefUnannotated")]
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "GetRefWithFields")]
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "GetRefUnannotated")]
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "GetRefWithFields")]
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "GetRefUnannotated", ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "GetRefWithFields", ProducedBy = Tool.Analyzer)]
		// IL2064's are bugs - shouldn't be unknown values
		// https://github.com/dotnet/runtime/issues/85465
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = t;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = t;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = t;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = t;
		static void AssignRefReturns<
			T,
			[DAM (DAMT.PublicMethods)] TM,
			[DAM (DAMT.PublicFields)] TF,
			[DAM (DAMT.PublicFields | DAMT.PublicMethods)] TMF>
			(RefFieldWithMethods target)
		{
			target.T = ref GetRefUnannotated (); // Warn
			target.T = ref GetRefWithMethods ();
			target.T = ref GetRefWithFields (); // Warn
			target.T = ref GetRefWithMethodsAndFields (); // Creates hole
			target.T = typeof (TM);

			ref Type t = ref GetRefUnannotated ();
			target.T = t; // Warn
			target.T = ref t; // Warn

			t = ref GetRefWithMethods ();
			target.T = t;
			target.T = ref t;

			t = ref GetRefWithFields ();
			target.T = t; // Warn
			target.T = ref t; // Warn

			t = ref GetRefWithMethodsAndFields ();
			target.T = t; // Bug: Warns with IL2064
			target.T = ref t; // Creates hole
			target.T = typeof (TM);
		}

		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "GetUnannotated")]
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "GetWithFields")]
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "GetUnannotated")]
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "GetWithFields")]
		static void AssignReturns<
			T,
			[DAM (DAMT.PublicMethods)] TM,
			[DAM (DAMT.PublicFields)] TF,
			[DAM (DAMT.PublicFields | DAMT.PublicMethods)] TMF>
			(scoped RefFieldWithMethods target)
		{
			Type t = GetUnannotated ();
			target.T = t; // Warn
			target.T = ref t; // Warn

			t = GetWithMethods ();
			target.T = t;
			target.T = ref t;

			t = GetWithFields ();
			target.T = t; // Warn
			target.T = ref t; // Warn

			t = GetWithMethodsAndFields ();
			target.T = t; // Okay
			target.T = ref t; // Creates hole
			target.T = typeof (TM);
		}

		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "PropUnannotated.T")]
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "PropWithFields.T")]
		static void AssignProperties (RefFieldWithMethods target,
			PropUnannotated unannotated = null,
			PropWithMethods withMethods = null,
			PropWithFields withFields = null,
			PropWithMethodsAndFields withMethodsAndFields = null)
		{
			target.T = unannotated.T; // Warn
			target.T = withMethods.T; // Okay
			target.T = withFields.T; // Warn
			target.T = withMethodsAndFields.T;// Okay
		}

		// target.T = x.T
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "RefPropUnannotated.T", ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "RefPropWithFields.T", ProducedBy = Tool.Analyzer)]
		// target.T = t;
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "RefPropUnannotated.T", ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "RefPropWithFields.T", ProducedBy = Tool.Analyzer)]
		// target.T = ref x.T
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "RefPropUnannotated.T")]
		[ExpectedWarning ("IL2074", "RefFieldWithMethods.T", "RefPropWithFields.T")]
		// ref Type t = ref x.T; target.T = t;
		// IL2064's are bugs - shouldn't be unknown values
		// https://github.com/dotnet/runtime/issues/85465
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = unannotated.T;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = withMethods.T;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = withFields.T;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = withMethodsAndFieldswithMtho.T;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = withMethods.T;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = t;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = t;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = t;
		[ExpectedWarning ("IL2064", "RefFieldWithMethods.T", ProducedBy = Tool.Trimmer | Tool.NativeAot)] // target.T = t;
		static void AssignRefProperties (RefFieldWithMethods target,
			RefPropUnannotated unannotated = null,
			RefPropWithMethods withMethods = null,
			RefPropWithFields withFields = null,
			RefPropWithMethodsAndFields withMethodsAndFields = null)
		{
			// All cause IL2064 -- ILLink doesn't recognize ldind.ref
			target.T = unannotated.T; // Warn
			target.T = withMethods.T; // Okay
			target.T = withFields.T; // Warn
			target.T = withMethodsAndFields.T;// Okay

			target.T = ref unannotated.T; // Warn
			target.T = ref withMethods.T; // Okay
			target.T = ref withFields.T; // Warn
			target.T = ref withMethodsAndFields.T; // Creates hole
			target.T = withMethods.T; // Assigns a value with methods to a location with methods and fields


			ref Type t = ref unannotated.T;
			target.T = t; // Okay
			t = ref withMethods.T;
			target.T = t; // Creates hole
			t = ref withFields.T;
			target.T = t; // Creates hole
			t = ref withMethodsAndFields.T;
			target.T = t; // Creates hole
		}

		class PropUnannotated
		{
			public Type T { get; set; }
		}
		class PropWithMethods
		{
			[DAM (DAMT.PublicMethods)]
			public Type T { get; set; }
		}
		class PropWithFields
		{
			[DAM (DAMT.PublicFields)]
			public Type T { get; set; }
		}
		class PropWithMethodsAndFields
		{
			[DAM (DAMT.PublicMethods | DAMT.PublicFields)]
			public Type T { get; set; }
		}
		class RefPropUnannotated
		{
			Type _field = null;

			public ref Type T { get => ref _field; }
		}
		class RefPropWithMethods
		{
			[DAM (DAMT.PublicMethods)]
			Type _field = null;

			[DAM (DAMT.PublicMethods)]
			public ref Type T { get => ref _field; }
		}
		class RefPropWithFields
		{
			[DAM (DAMT.PublicFields)]
			Type _field = null;

			[DAM (DAMT.PublicFields)]
			public ref Type T { get => ref _field; }
		}
		class RefPropWithMethodsAndFields
		{
			[DAM (DAMT.PublicFields | DAMT.PublicMethods)]
			Type _field = null;

			[DAM (DAMT.PublicFields | DAMT.PublicMethods)]
			public ref Type T { get => ref _field; }
		}
		class UnannotatedField
		{
			public Type T;
		}

		class FieldWithMethods
		{
			[DAM (DAMT.PublicMethods)]
			public Type T;
		}

		class FieldWithFields
		{
			[DAM (DAMT.PublicFields)]
			public Type T;
		}

		class FieldWithMethodsAndFields
		{
			[DAM (DAMT.PublicMethods | DAMT.PublicFields)]
			public Type T;
		}

		static Type GetUnannotated ()
			=> throw new NotImplementedException ();

		static ref Type GetRefUnannotated ()
			=> throw new NotImplementedException ();

		[return: DAM (DAMT.PublicMethods)]
		static Type GetWithMethods ()
			=> throw new NotImplementedException ();

		[return: DAM (DAMT.PublicMethods)]
		static ref Type GetRefWithMethods ()
			=> throw new NotImplementedException ();

		[return: DAM (DAMT.PublicFields)]
		static Type GetWithFields ()
			=> throw new NotImplementedException ();

		[return: DAM (DAMT.PublicFields)]
		static ref Type GetRefWithFields ()
			=> throw new NotImplementedException ();

		[return: DAM (DAMT.PublicFields | DAMT.PublicMethods)]
		public static Type GetWithMethodsAndFields ()
			=> throw new NotImplementedException ();

		[return: DAM (DAMT.PublicFields | DAMT.PublicMethods)]
		public static ref Type GetRefWithMethodsAndFields ()
			=> throw new NotImplementedException ();

		[Kept]
		ref struct RefFieldUnannotated
		{
			[Kept]
			public ref Type T;

			[Kept]
			public RefFieldUnannotated (ref Type t)
			{
				T = ref t;
			}
		}

		[Kept]
		ref struct RefFieldWithMethods
		{
			[Kept]
			[DynamicallyAccessedMembers (DAMT.PublicMethods)]
			public ref Type T;

			[Kept]
			public RefFieldWithMethods ([DynamicallyAccessedMembers (DAMT.PublicMethods)] ref Type t)
			{
				T = ref t;
			}
		}

		[Kept]
		ref struct RefFieldWithFields
		{
			[Kept]
			[DynamicallyAccessedMembers (DAMT.PublicFields)]
			public ref Type T;

			[Kept]
			public RefFieldWithFields ([DynamicallyAccessedMembers (DAMT.PublicFields)] ref Type t)
			{
				T = ref t;
			}
		}

		[Kept]
		ref struct RefFieldWithMethodsAndFields
		{
			[Kept]
			[DynamicallyAccessedMembers (DAMT.PublicFields | DAMT.PublicMethods)]
			public ref Type T;

			[Kept]
			public RefFieldWithMethodsAndFields ([DynamicallyAccessedMembers (DAMT.PublicFields | DAMT.PublicMethods)] ref Type t)
			{
				T = ref t;
			}
		}
	}
}
