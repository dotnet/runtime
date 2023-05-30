// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{

	[SetupLinkAttributesFile ("RequiresViaXml.attributes.xml")]
	[SetupLinkerDescriptorFile ("RequiresViaXml.descriptor.xml")]
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class RequiresViaXml
	{

		// The second attribute is added through link attribute XML
		[RequiresUnreferencedCode ("Message for --MethodWithDuplicateRequiresAttribute--")]
		[ExpectedWarning ("IL2027", "RequiresUnreferencedCodeAttribute", nameof (MethodWithDuplicateRequiresAttribute), ProducedBy = Tool.Trimmer)]
		static void MethodWithDuplicateRequiresAttribute ()
		{
		}

		[ExpectedWarning ("IL2026", "--MethodWithDuplicateRequiresAttribute--")]
		[ExpectedWarning ("IL2026", "--MethodWithDuplicateRequiresAttribute--")]
		static void TestDuplicateRequiresAttribute ()
		{
			MethodWithDuplicateRequiresAttribute ();
			// A second callsite should not introduce extra warnings about the duplicate attributes.
			MethodWithDuplicateRequiresAttribute ();
		}


		[RequiresUnreferencedCode ("Message for --RequiresOnlyViaDescriptor--")]
		static void RequiresOnlyViaDescriptor ()
		{
		}

		[RequiresUnreferencedCode ("Message for --RequiresOnFieldOnlyViaDescriptor--")]
		class RequiresOnFieldOnlyViaDescriptor
		{
			public static int Field;
		}

		public static void Main ()
		{
			TestDuplicateRequiresAttribute ();
		}
	}
}
