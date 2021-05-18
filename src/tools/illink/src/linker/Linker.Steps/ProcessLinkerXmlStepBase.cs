// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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