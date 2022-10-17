// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
		public static void Main ()
		{
			RequireMethods (unsuppressed.GetType ());
			RequireMethods (suppressed.GetType ());
			RequireAll (annotatedAllSuppressed.GetType ());

			var t = typeof (DerivedFromSuppressed1);
			var t2 = typeof (DerivedFromUnsuppressed1);
			var t3 = typeof (SuppressedOnDerived1);
			var t4 = typeof (SuppressedBaseWarningsOnDerived);

			UseDerivedTypes ();

			// Suppressing type hierarchy warnings is unsafe because it allows
			// derived types to access annotated methods without any warnings:
			derivedFromSuppressed.GetType ().GetMethod ("RUCDerivedMethod");

		}

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
		static DerivedFromSuppressed1 derivedFromSuppressed;
		[Kept]
		static AnnotatedAllSuppressed annotatedAllSuppressed;

		[Kept]
		static void RequireMethods (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			Type type)
		{ }

		[Kept]
		static void RequireAll (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			Type type)
		{ }

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		class Unsuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on Unsuppressed--")]
			[RequiresUnreferencedCode ("--RUC on Unsuppressed--")]
			public void RUCMethod () { }
		}

		[Kept]
		[KeptBaseType (typeof (Unsuppressed))]
		class DerivedFromUnsuppressed1 : Unsuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on DerivedFromUnsuppressed1--")]
			[RequiresUnreferencedCode ("--RUC on DerivedFromUnsuppressed1--")]
			public void DerivedRUCMethod () { }
		}

		[Kept]
		[KeptBaseType (typeof (Unsuppressed))]
		class DerivedFromUnsuppressed2 : Unsuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on DerivedFromUnsuppressed2")]
			[RequiresUnreferencedCode ("--RUC on DerivedFromUnsuppressed2--")]
			public void DerivedRUCMethod () { }
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
		[UnconditionalSuppressMessage ("TrimAnalysis", "IL2112")]
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
		class DerivedFromSuppressed1 : Suppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on DerivedFromSuppressed1--")]
			[RequiresUnreferencedCode ("--RUC on DerivedFromSuppressed1--")]
			public void RUCDerivedMethod () { }
		}

		[Kept]
		[KeptBaseType (typeof (Suppressed))]
		class DerivedFromSuppressed2 : Suppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on DerivedFromSuppressed2--")]
			[RequiresUnreferencedCode ("--RUC on DerivedFromSuppressed2--")]
			public void RUCDerivedMethod () { }
		}

		[Kept]
		[KeptBaseType (typeof (Unsuppressed))]
		class SuppressedOnDerived1 : Unsuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
			[UnconditionalSuppressMessage ("TrimAnalysis", "IL2112")]
			[RequiresUnreferencedCode ("--RUC on SuppressedOnDerived1--")]
			public void DerivedRUCMethod () { }
		}

		[Kept]
		[KeptBaseType (typeof (Unsuppressed))]
		class SuppressedOnDerived2 : Unsuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
			[UnconditionalSuppressMessage ("TrimAnalysis", "IL2112")]
			[RequiresUnreferencedCode ("--RUC on SuppressedOnDerived2--")]
			public void DerivedRUCMethod () { }
		}

		[Kept]
		[KeptBaseType (typeof (Unsuppressed))]
		class SuppressedBaseWarningsOnDerived : Unsuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on SuppressedBaseWarningsOnDerived")]
			[RequiresUnreferencedCode ("--RUC on SuppressedBaseWarningsOnDerived--")]
			public void DerivedRUCMethod () { }
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		class AnnotatedAllSuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
			[UnconditionalSuppressMessage ("TrimAnalysis", "IL2114")]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public Type DAMTField;

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
			[UnconditionalSuppressMessage ("TrimAnalysis", "IL2112")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedAllSuppresed.RUCMethod--")]
			public static void RUCMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
			[UnconditionalSuppressMessage ("TrimAnalysis", "IL2114")]
			public void DAMTMethod (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type t
			)
			{ }
		}
	}
}
