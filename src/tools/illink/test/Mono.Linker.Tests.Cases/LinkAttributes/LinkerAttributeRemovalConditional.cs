// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SetupLinkAttributesFile ("LinkerAttributeRemovalConditional.xml")]
	[IgnoreLinkAttributes (false)]
	class LinkerAttributeRemovalConditional
	{
		public static void Main ()
		{
			Kept_1 ();
			Kept_2 ();
			Kept_3 ();
			Removed_1 ();
			Removed_2 ();
			Removed_3 ();
			Removed_4 ();
			Kept_4 ();
			Kept_5 ();
			Removed_5 ();
			Removed_6 ();
			Kept_6 ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestConditionalRemoveAttribute))]
		[TestConditionalRemove ()]
		static void Kept_1 ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestConditionalRemoveAttribute))]
		[TestConditionalRemove ("my-value", "string value")]
		static void Kept_2 ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestConditionalRemoveAttribute))]
		[TestConditionalRemove (1, true)]
		static void Kept_3 ()
		{
		}

		[Kept]
		[TestConditionalRemove ("remove0", "string value")]
		static void Removed_1 ()
		{
		}

		[Kept]
		[TestConditionalRemove (100, '1')]
		[TestConditionalRemove ("remove1", '1', 3)]
		static void Removed_2 ()
		{
		}

		[Kept]
		[TestConditionalRemove ("remove0", 'k', 0)] // It's removed because the converted string value matches
		static void Removed_3 ()
		{
		}

		[Kept]
		[TestConditionalRemove ("remove2", "remove3")]
		static void Removed_4 ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestConditionalRemoveAttribute))]
		[TestConditionalRemove ("remove2", "unmatched second arg")]
		static void Kept_4 ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestConditionalRemoveAttribute))]
		[TestConditionalRemove ((int) 99, "remove3")]
		static void Kept_5 () // Kept because int arg in constructor doesn't match long arg in xml
		{
		}

		[Kept]
		[TestConditionalRemove (72, "a", "b", "c", "d", "e")]
		static void Removed_5 ()
		{
		}

		[Kept]
		[TestConditionalRemove (new int[] { 1, 2, 3 }, "remove4")]
		static void Removed_6 ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestConditionalRemoveAttribute))]
		[TestConditionalRemove (new int[] { 1, 2, 3, 4 }, "remove4")]
		static void Kept_6 ()
		{
		}
	}

	[Kept]
	[KeptBaseType (typeof (System.Attribute))]
	[KeptAttributeAttribute (typeof (AttributeUsageAttribute))]
	[AttributeUsage (AttributeTargets.All, AllowMultiple = true)]
	class TestConditionalRemoveAttribute : Attribute
	{
		[Kept]
		public TestConditionalRemoveAttribute ()
		{
		}

		[Kept]
		// Any usage with "remove0" key is removed
		public TestConditionalRemoveAttribute (string key, string value)
		{
		}

		// Any usage with 100 key is removed	
		// Any usage with "remove1" key is removed
		public TestConditionalRemoveAttribute (object key, char value, int ivalue)
		{
		}

		[Kept]
		public TestConditionalRemoveAttribute (int key, object value)
		{
		}

		public TestConditionalRemoveAttribute (int key, [KeptAttributeAttribute (typeof (ParamArrayAttribute))] params object[] values)
		{
		}

		[Kept]
		public TestConditionalRemoveAttribute (int[] intArray, string str)
		{
		}
	}
}
