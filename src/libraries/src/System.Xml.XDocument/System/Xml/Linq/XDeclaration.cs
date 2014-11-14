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
    /// Represents an XML declaration.
    /// </summary>
    /// <remarks>
    /// An XML declaration is used to declare the XML version,
    /// the encoding, and whether or not the XML document is standalone.
    /// </remarks>
    public class XDeclaration
    {
        string version;
        string encoding;
        string standalone;

        /// <summary>
        /// Initializes a new instance of the <see cref="XDeclaration"/> class from the
        /// specified version, encoding, and standalone properties.
        /// </summary>
        /// <param name="version">
        /// The version of the XML, usually "1.0".
        /// </param>
        /// <param name="encoding">
        /// The encoding for the XML document.
        /// </param>
        /// <param name="standalone">
        /// Specifies whether the XML is standalone or requires external entities
        /// to be resolved.
        /// </param>
        public XDeclaration(string version, string encoding, string standalone)
        {
            this.version = version;
            this.encoding = encoding;
            this.standalone = standalone;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="XDeclaration"/> class
        /// from another <see cref="XDeclaration"/> object.
        /// </summary>
        /// <param name="other">
        /// The <see cref="XDeclaration"/> used to initialize this <see cref="XDeclaration"/> object.
        /// </param>
        public XDeclaration(XDeclaration other)
        {
            if (other == null) throw new ArgumentNullException("other");
            version = other.version;
            encoding = other.encoding;
            standalone = other.standalone;
        }

        internal XDeclaration(XmlReader r)
        {
            version = r.GetAttribute("version");
            encoding = r.GetAttribute("encoding");
            standalone = r.GetAttribute("standalone");
            r.Read();
        }

        /// <summary>
        /// Gets or sets the encoding for this document.
        /// </summary>
        public string Encoding
        {
            get { return encoding; }
            set { encoding = value; }
        }

        /// <summary>
        /// Gets or sets the standalone property for this document.
        /// </summary>
        /// <remarks>
        /// The valid values for standalone are "yes" or "no".
        /// </remarks>
        public string Standalone
        {
            get { return standalone; }
            set { standalone = value; }
        }

        /// <summary>
        /// Gets or sets the version property for this document.
        /// </summary>
        /// <remarks>
        /// The value is usually "1.0".
        /// </remarks>
        public string Version
        {
            get { return version; }
            set { version = value; }
        }

        /// <summary>
        /// Provides a formatted string.
        /// </summary>
        /// <returns>A formatted XML string.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("<?xml");
            if (version != null)
            {
                sb.Append(" version=\"");
                sb.Append(version);
                sb.Append('\"');
            }
            if (encoding != null)
            {
                sb.Append(" encoding=\"");
                sb.Append(encoding);
                sb.Append('\"');
            }
            if (standalone != null)
            {
                sb.Append(" standalone=\"");
                sb.Append(standalone);
                sb.Append('\"');
            }
            sb.Append("?>");
            return sb.ToString();
        }
    }
}
