// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Mono.Linker.Steps
{
    public class ResolveFromXmlStep : ProcessLinkerXmlStepBase
    {
        public ResolveFromXmlStep(Stream documentStream, string xmlDocumentLocation)
            : base(documentStream, xmlDocumentLocation)
        {
        }

        protected override void Process()
        {
            new DescriptorMarker(Context, _documentStream, _xmlDocumentLocation).Mark();
        }
    }
}
