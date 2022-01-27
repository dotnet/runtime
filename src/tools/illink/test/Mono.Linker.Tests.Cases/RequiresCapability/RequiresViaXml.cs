// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
	// [LogContains ("--RequiresOnlyViaDescriptor--")]  // https://github.com/dotnet/linker/issues/2103
	[ExpectedWarning ("IL2026", "RequiresOnFieldOnlyViaDescriptor.Field", FileName = "RequiresViaXml.descriptor.xml", ProducedBy = ProducedBy.Trimmer)]
	class RequiresViaXml
	{

		// The second attribute is added through link attribute XML
		[RequiresUnreferencedCode ("Message for --MethodWithDuplicateRequiresAttribute--")]
		[ExpectedWarning ("IL2027", "RequiresUnreferencedCodeAttribute", nameof (MethodWithDuplicateRequiresAttribute), ProducedBy = ProducedBy.Trimmer)]
		[ExpectedWarning ("IL2027", "RequiresUnreferencedCodeAttribute", nameof (MethodWithDuplicateRequiresAttribute), ProducedBy = ProducedBy.Trimmer)]
		static void MethodWithDuplicateRequiresAttribute ()
		{
		}

		[ExpectedWarning ("IL2026", "--MethodWithDuplicateRequiresAttribute--")]
		static void TestDuplicateRequiresAttribute ()
		{
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
