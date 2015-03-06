// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    // Specifies how whitespace is handled in XmlTextReader.
    internal enum WhitespaceHandling
    {
        // Return all Whitespace and SignificantWhitespace nodes. This is the default.
        All = 0,

        // Return just SignificantWhitespace, i.e. whitespace nodes that are in scope of xml:space="preserve"
        Significant = 1,

        // Do not return any Whitespace or SignificantWhitespace nodes.
        None = 2
    }
}
