// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CultureInfo = System.Globalization.CultureInfo;
using Debug = System.Diagnostics.Debug;
using IEnumerable = System.Collections.IEnumerable;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
using Enumerable = System.Linq.Enumerable;
using IComparer = System.Collections.IComparer;
using IEqualityComparer = System.Collections.IEqualityComparer;
using StringBuilder = System.Text.StringBuilder;
using Encoding = System.Text.Encoding;
using Interlocked = System.Threading.Interlocked;
using System.Reflection;

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents a text node that contains CDATA.
    /// </summary>
    public class XCData : XText
    {
        /// <summary>
        /// Initializes a new instance of the XCData class.
        /// </summary>
        /// <param name="value">The string that contains the value of the XCData node.</param>
        public XCData(string value) : base(value) { }

        /// <summary>
        /// Initializes a new instance of the XCData class from another XCData object.
        /// </summary>
        /// <param name="other">Text node to copy from</param>
        public XCData(XCData other) : base(other) { }

        internal XCData(XmlReader r) : base(r) { }

        /// <summary>
        /// Gets the node type for this node.
        /// </summary>
        /// <remarks>
        /// This property will always return XmlNodeType.CDATA.
        /// </remarks>
        public override XmlNodeType NodeType
        {
            get
            {
                return XmlNodeType.CDATA;
            }
        }

        /// <summary>
        /// Write this <see cref="XCData"/> to the given <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to write this <see cref="XCData"/> to.
        /// </param>
        public override void WriteTo(XmlWriter writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            writer.WriteCData(text);
        }

        internal override XNode CloneNode()
        {
            return new XCData(this);
        }
    }
}
