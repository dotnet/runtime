// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	public class TypeHierarchySuppressions
	{
		// https://github.com/mono/linker/issues/2136
		// Warnings should originate from the types (or rather their members, with the
		// proposed behavior), and the type suppressions should silence the relevant
		// warnings.

		// Should originate from types instead
		[ExpectedWarning ("IL2026", "--RUC on Unsuppressed--")]
		[ExpectedWarning ("IL2026", "--RUC on DerivedFromUnsuppressed1--")]

		// Should be suppressed by type-level suppression
		[ExpectedWarning ("IL2026", "--RUC on Suppressed--")]
		[ExpectedWarning ("IL2026", "--RUC on SuppressedOnDerived1--")]
		[ExpectedWarning ("IL2026", "--RUC on DerivedFromSuppressed1--")]
		public static void Main ()
		{
			RequireMethods (unsuppressed.GetType ());
			RequireMethods (suppressed.GetType ());

			var t = typeof (DerivedFromSuppressed1);
			var t2 = typeof (DerivedFromUnsuppressed1);
			var t3 = typeof (SuppressedOnDerived1);

			UseDerivedTypes ();
		}

		// Referencing these types in a separate method ensures that they get
		// marked after applying annotations on the base type.
		[Kept]
		static void UseDerivedTypes ()
		{
			var t = typeof (DerivedFromUnsuppressed2);
			var t2 = typeof (DerivedFromSuppressed2);
			var t3 = typeof (SuppressedOnDerived2);
		}

		[Kept]
		static Unsuppressed unsuppressed;

		[Kept]
		static Suppressed suppressed;

		[Kept]
		static void RequireMethods (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			Type type)
		{ }

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		// https://github.com/mono/linker/issues/2136
		// [ExpectedWarning ("IL2026", "--RUC on Unsuppressed--")]
		class Unsuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on Unsuppressed--")]
			public void RUCMethod () { }
		}

		[Kept]
		[KeptBaseType (typeof (Unsuppressed))]
		// https://github.com/mono/linker/issues/2136
		// [ExpectedWarning ("IL2026", "--RUC on DerivedFromUnsuppressed1--")]
		class DerivedFromUnsuppressed1 : Unsuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on DerivedFromUnsuppressed1--")]
			public void DerivedRUCMethod () { }
		}

		[Kept]
		[KeptBaseType (typeof (Unsuppressed))]
		[ExpectedWarning ("IL2026", "--RUC on DerivedFromUnsuppressed2")]
		// https://github.com/mono/linker/issues/2136
		// Should originate from the base type instead
		[ExpectedWarning ("IL2026", "--RUC on Unsuppressed--")]
		class DerivedFromUnsuppressed2 : Unsuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on DerivedFromUnsuppressed2--")]
			public void DerivedRUCMethod () { }
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
		[UnconditionalSuppressMessage ("TrimAnalysis", "IL2026")]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		class Suppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on Suppressed--")]
			public void RUCMethod () { }
		}

		[Kept]
		[KeptBaseType (typeof (Suppressed))]
		// https://github.com/mono/linker/issues/2136
		// [ExpectedWarning ("IL2026", "--RUC on DerivedFromSuppressed1--")]
		class DerivedFromSuppressed1 : Suppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on DerivedFromSuppressed1--")]
			public void RUCDerivedMethod () { }
		}

		[Kept]
		[KeptBaseType (typeof (Suppressed))]
		[ExpectedWarning ("IL2026", "--RUC on DerivedFromSuppressed2--")]
		// https://github.com/mono/linker/issues/2136
		[ExpectedWarning ("IL2026", "--RUC on Suppressed--")]
		class DerivedFromSuppressed2 : Suppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on DerivedFromSuppressed2--")]
			public void RUCDerivedMethod () { }
		}

		[Kept]
		[KeptBaseType (typeof (Unsuppressed))]
		[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
		[UnconditionalSuppressMessage ("TrimAnalysis", "IL2026")]
		class SuppressedOnDerived1 : Unsuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on SuppressedOnDerived1--")]
			public void DerivedRUCMethod () { }
		}

		[Kept]
		[KeptBaseType (typeof (Unsuppressed))]
		[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
		[UnconditionalSuppressMessage ("TrimAnalysis", "IL2026")]
		class SuppressedOnDerived2 : Unsuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on SuppressedOnDerived2--")]
			public void DerivedRUCMethod () { }
		}
	}
}
