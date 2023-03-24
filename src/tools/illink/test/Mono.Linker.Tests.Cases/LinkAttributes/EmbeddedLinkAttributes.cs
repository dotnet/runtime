// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SkipKeptItemsValidation]
	[SetupCompileResource ("EmbeddedLinkAttributes.xml", "ILLink.LinkAttributes.xml")]
	[IgnoreLinkAttributes (false)]
	[RemovedResourceInAssembly ("test.exe", "ILLink.LinkAttributes.xml")]
	[ExpectedNoWarnings]
	class EmbeddedLinkAttributes
	{
		public static void Main ()
		{
			var instance = new EmbeddedLinkAttributes ();

			instance.ReadFromInstanceField ();
			instance.ReadFromInstanceField2 ();
		}

		Type _typeWithPublicParameterlessConstructor;

		[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		private void ReadFromInstanceField ()
		{
			_typeWithPublicParameterlessConstructor.RequiresPublicParameterlessConstructor ();
			_typeWithPublicParameterlessConstructor.RequiresPublicConstructors ();
			_typeWithPublicParameterlessConstructor.RequiresNonPublicConstructors ();
		}

		Type _typeWithPublicFields;

		[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		private void ReadFromInstanceField2 ()
		{
			_typeWithPublicFields.RequiresPublicConstructors ();
			_typeWithPublicFields.RequiresPublicFields ();
		}
	}
}
