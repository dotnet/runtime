// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System.Xml;

    public interface IXmlTextParser
    {
        bool Normalized { get; set; }

        WhitespaceHandling WhitespaceHandling { get; set; }
    }
}
