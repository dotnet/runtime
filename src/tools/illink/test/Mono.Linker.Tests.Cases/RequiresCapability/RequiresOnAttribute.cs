// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class RequiresOnAttribute
	{
		// Using these as a simple way to suppress all warning in Main
		// it causes lot of warning due to DAM marking everything in the class.
		[RequiresUnreferencedCode ("main")]
		[RequiresDynamicCode ("main")]
		[RequiresAssemblyFiles ("main")]
		public static void Main ()
		{
			TestRequiresOnAttributeOnGenericParameter ();
			new TypeWithAttributeWhichRequires ();
			MethodWithAttributeWhichRequires ();
			_fieldWithAttributeWhichRequires = 0;
			PropertyWithAttributeWhichRequires = false;
			TestMethodWhichRequiresWithAttributeWhichRequires ();
			RequiresTriggeredByAttributeUsage.Test ();

			// Accessing the members to test via direct calls/access is not enough
			// in NativeAOT custom attributes are only looked at if the member is
			// reflection enabled - so there has to be a reflection access to it somewhere.
			// On the other hand the analyzer reports RDC/RAF only on direct access. So we need to have "both"
			typeof (RequiresOnAttribute).RequiresAll ();
		}

		class AttributeWhichRequiresAttribute : Attribute
		{
			[RequiresUnreferencedCode ("Message for --AttributeWhichRequiresAttribute.ctor--")]
			[RequiresAssemblyFiles ("Message for --AttributeWhichRequiresAttribute.ctor--")]
			[RequiresDynamicCode ("Message for --AttributeWhichRequiresAttribute.ctor--")]
			public AttributeWhichRequiresAttribute ()
			{
			}
		}

		class AttributeWhichRequiresOnPropertyAttribute : Attribute
		{
			public AttributeWhichRequiresOnPropertyAttribute ()
			{
			}

			public bool PropertyWhichRequires {
				get => false;

				[RequiresUnreferencedCode ("--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
				[RequiresAssemblyFiles ("--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
				[RequiresDynamicCode ("--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
				set { }
			}
		}

		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		class GenericTypeWithAttributedParameter<[AttributeWhichRequires] T>
		{
			public static void TestMethod () { }
		}

		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		static void GenericMethodWithAttributedParameter<[AttributeWhichRequires] T> () { }

		static void TestRequiresOnAttributeOnGenericParameter ()
		{
			GenericTypeWithAttributedParameter<int>.TestMethod ();
			GenericMethodWithAttributedParameter<int> ();
		}

		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[AttributeWhichRequires]
		[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
		class TypeWithAttributeWhichRequires
		{
		}

		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[AttributeWhichRequires]
		[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
		static void MethodWithAttributeWhichRequires () { }

		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[AttributeWhichRequires]
		[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
		static int _fieldWithAttributeWhichRequires;

		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[AttributeWhichRequires]
		[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
		static bool PropertyWithAttributeWhichRequires { get; set; }

		[AttributeWhichRequires]
		[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
		[RequiresUnreferencedCode ("--MethodWhichRequiresWithAttributeWhichRequires--")]
		[RequiresAssemblyFiles ("--MethodWhichRequiresWithAttributeWhichRequires--")]
		[RequiresDynamicCode ("--MethodWhichRequiresWithAttributeWhichRequires--")]
		static void MethodWhichRequiresWithAttributeWhichRequires () { }

		[ExpectedWarning ("IL2026", "--MethodWhichRequiresWithAttributeWhichRequires--")]
		[ExpectedWarning ("IL3002", "--MethodWhichRequiresWithAttributeWhichRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--MethodWhichRequiresWithAttributeWhichRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		static void TestMethodWhichRequiresWithAttributeWhichRequires ()
		{
			MethodWhichRequiresWithAttributeWhichRequires ();
		}

		class RequiresTriggeredByAttributeUsage
		{
			class AttributeWhichMarksPublicMethods : Attribute
			{
				public AttributeWhichMarksPublicMethods ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) { }
			}

			class TypeWithMethodWhichRequires
			{
				[RequiresUnreferencedCode ("--TypeWithMethodWhichRequires--")]
				[RequiresDynamicCode ("--TypeWithMethodWhichRequires--")]
				[RequiresAssemblyFiles ("--TypeWithMethodWhichRequires--")]
				public void MethodWhichRequires () { }
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWhichRequires--")]
			[ExpectedWarning ("IL3002", "--TypeWithMethodWhichRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TypeWithMethodWhichRequires--", ProducedBy = Tool.NativeAot)]
			[AttributeWhichMarksPublicMethods (typeof(TypeWithMethodWhichRequires))]
			static void ShouldWarn()
			{
			}

			[RequiresUnreferencedCode ("test")]
			[RequiresDynamicCode ("test")]
			[RequiresAssemblyFiles ("test")]
			[AttributeWhichMarksPublicMethods (typeof (TypeWithMethodWhichRequires))]
			static void SuppressedDueToRequires()
			{
			}

			[UnconditionalSuppressMessage("test", "IL2026")]
			[UnconditionalSuppressMessage ("test", "IL3002")]
			[UnconditionalSuppressMessage ("test", "IL3050")]
			public static void Test()
			{
				ShouldWarn ();
				SuppressedDueToRequires ();
			}
		}
	}
}
