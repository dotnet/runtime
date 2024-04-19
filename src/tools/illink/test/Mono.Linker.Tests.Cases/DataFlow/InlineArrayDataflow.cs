// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class InlineArrayDataflow
	{
		public static void Main()
		{
			AccessPrimitiveTypeArray ();
			AccessUnannotatedTypeArray ();
			AccessAnnotatedTypeArray ();
		}

		public int TestProperty { get; set; }

		[InlineArray (5)]
		struct PrimitiveTypeArray
		{
			public BindingFlags value;
		}

		// This case will fallback to not understanding the binding flags and will end up marking all properties
		static void AccessPrimitiveTypeArray ()
		{
			PrimitiveTypeArray a = new PrimitiveTypeArray ();
			ref var item = ref a[1];
			item = BindingFlags.Public;

			typeof (InlineArrayDataflow).GetProperty (nameof (TestProperty), a[1]);
		}

		[InlineArray (5)]
		struct UnannotatedTypeArray
		{
			public Type value;
		}

		[ExpectedWarning ("IL2065", "GetProperty")]
		static void AccessUnannotatedTypeArray ()
		{
			UnannotatedTypeArray a = new UnannotatedTypeArray ();
			ref var item = ref a[2];
			item = typeof (InlineArrayDataflow);

			a[2].GetProperty (nameof (TestProperty));
		}

		[InlineArray (5)]
		struct AnnotatedTypeArray
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
			public Type value;
		}

		// Currently tracking of annotations on inline array values is not implemented
		[ExpectedWarning("IL2065", "GetProperty")]
		static void AccessAnnotatedTypeArray ()
		{
			AnnotatedTypeArray a = new AnnotatedTypeArray ();
			ref var item = ref a[3];
			item = typeof (InlineArrayDataflow);

			a[3].GetProperty (nameof (TestProperty));
		}
	}
}
