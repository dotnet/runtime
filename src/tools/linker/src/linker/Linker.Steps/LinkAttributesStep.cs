// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Mono.Linker.Steps
{
	public class LinkAttributesStep : ProcessLinkerXmlStepBase
	{
		public LinkAttributesStep (Stream documentStream, string xmlDocumentLocation)
			: base (documentStream, xmlDocumentLocation)
		{
		}

		protected override void Process ()
		{
			new LinkAttributesParser (Context, _documentStream, _xmlDocumentLocation).Parse (Context.CustomAttributes.PrimaryAttributeInfo);
		}
	}
}