// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml
{
    // Specifies how whitespace is handled in XmlTextReader.
    public enum WhitespaceHandling
    {
        // Return all Whitespace and SignificantWhitespace nodes. This is the default.
        All = 0,

        // Return just SignificantWhitespace, i.e. whitespace nodes that are in scope of xml:space="preserve"
        Significant = 1,

        // Do not return any Whitespace or SignificantWhitespace nodes.
        None = 2
    }
}
