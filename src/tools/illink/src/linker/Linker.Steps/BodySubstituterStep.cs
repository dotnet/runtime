// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Mono.Linker.Steps
{
	public class BodySubstituterStep : ProcessLinkerXmlStepBase
	{
		public BodySubstituterStep (Stream documentStream, string xmlDocumentLocation)
			: base (documentStream, xmlDocumentLocation)
		{
		}

		protected override void Process ()
		{
			new BodySubstitutionParser (Context, _documentStream, _xmlDocumentLocation).Parse (Context.Annotations.MemberActions.PrimarySubstitutionInfo);
		}
	}
}
