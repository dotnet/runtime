// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Mono.Linker.Steps
{
	public class ProcessLinkerXmlStepBase : BaseStep
	{
		protected readonly string _xmlDocumentLocation;
		protected readonly Stream _documentStream;

		public ProcessLinkerXmlStepBase (Stream documentStream, string xmlDocumentLocation)
		{
			_documentStream = documentStream;
			_xmlDocumentLocation = xmlDocumentLocation;
		}
	}
}
