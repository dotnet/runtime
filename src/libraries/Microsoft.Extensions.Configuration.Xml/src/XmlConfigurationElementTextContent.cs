// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration.Xml
{
    internal sealed class XmlConfigurationElementTextContent
    {
        public XmlConfigurationElementTextContent(string textContent, int? linePosition, int? lineNumber)
        {
            ThrowHelper.ThrowIfNull(textContent);

            TextContent = textContent;
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }

        public string TextContent { get; }

        public int? LineNumber { get; }

        public int? LinePosition { get; }
    }
}
