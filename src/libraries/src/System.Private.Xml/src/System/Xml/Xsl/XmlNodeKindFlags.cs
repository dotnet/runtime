// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Xml.Xsl
{
    /// <summary>
    /// the xml node kind of a QIL expression
    /// </summary>
    [Flags]
    internal enum XmlNodeKindFlags
    {
        /// <summary>
        /// no node kind
        /// </summary>
        None = 0,

        /// <summary>
        /// document node kind
        /// </summary>
        Document = 0x01,

        /// <summary>
        /// element node kind
        /// </summary>
        Element = 0x02,

        /// <summary>
        /// attribute node
        /// </summary>
        Attribute = 0x04,

        /// <summary>
        /// text node
        /// </summary>
        Text = 0x08,

        /// <summary>
        /// comment node
        /// </summary>
        Comment = 0x10,

        /// <summary>
        /// processing-instruction node
        /// </summary>
        PI = 0x20,

        /// <summary>
        /// namespace node
        /// </summary>
        Namespace = 0x40,

        // convenience types

        /// <summary>
        /// shortcut for any node that may appear in element content
        /// </summary>
        Content = Element | Comment | PI | Text,

        /// <summary>
        /// shortcut for any node kind
        /// </summary>
        Any = 0x7F,
    }
}