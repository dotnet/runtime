// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.XPath;

namespace Mono.Linker.Steps
{
	public class ProcessLinkerXmlStepBase : BaseStep
	{
		protected readonly XPathDocument _document;
		protected readonly string _xmlDocumentLocation;

		public ProcessLinkerXmlStepBase (XPathDocument document, string xmlDocumentLocation)
		{
			_document = document;
			_xmlDocumentLocation = xmlDocumentLocation;
		}
	}
}