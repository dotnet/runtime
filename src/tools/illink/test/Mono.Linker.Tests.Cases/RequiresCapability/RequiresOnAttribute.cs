// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class RequiresOnAttribute
	{
		public static void Main ()
		{
			TestRequiresOnAttributeOnGenericParameter ();
			new TypeWithAttributeWhichRequires ();
			MethodWithAttributeWhichRequires ();
			_fieldWithAttributeWhichRequires = 0;
			PropertyWithAttributeWhichRequires = false;
			TestMethodWhichRequiresWithAttributeWhichRequires ();
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
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
		class GenericTypeWithAttributedParameter<[AttributeWhichRequires] T>
		{
			public static void TestMethod () { }
		}

		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
		static void GenericMethodWithAttributedParameter<[AttributeWhichRequires] T> () { }

		static void TestRequiresOnAttributeOnGenericParameter ()
		{
			GenericTypeWithAttributedParameter<int>.TestMethod ();
			GenericMethodWithAttributedParameter<int> ();
		}

		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
		[AttributeWhichRequires]
		[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
		class TypeWithAttributeWhichRequires
		{
		}

		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
		[AttributeWhichRequires]
		[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
		static void MethodWithAttributeWhichRequires () { }

		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
		[AttributeWhichRequires]
		[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
		static int _fieldWithAttributeWhichRequires;

		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
		[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
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
		[ExpectedWarning ("IL3002", "--MethodWhichRequiresWithAttributeWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--MethodWhichRequiresWithAttributeWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestMethodWhichRequiresWithAttributeWhichRequires ()
		{
			MethodWhichRequiresWithAttributeWhichRequires ();
		}
	}
}
