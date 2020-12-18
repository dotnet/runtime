// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration.Xml
{
    internal class XmlConfigurationElementTextContent
    {
        public XmlConfigurationElementTextContent(string textContent, string lineInfo)
        {
            TextContent = textContent ?? throw new ArgumentNullException(nameof(textContent));
            LineInfo = lineInfo ?? throw new ArgumentNullException(nameof(lineInfo));
        }

        public string TextContent { get; }

        public string LineInfo { get; }
    }
}
