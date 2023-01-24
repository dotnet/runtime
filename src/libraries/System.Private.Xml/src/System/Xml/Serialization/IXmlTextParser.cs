// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;

namespace System.Xml.Serialization
{
    public interface IXmlTextParser
    {
        bool Normalized { get; set; }

        WhitespaceHandling WhitespaceHandling { get; set; }
    }
}
