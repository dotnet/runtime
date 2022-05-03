// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using StringBuilder = System.Text.StringBuilder;

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
        private string? _version;
        private string? _encoding;
        private string? _standalone;

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
        public XDeclaration(string? version, string? encoding, string? standalone)
        {
            _version = version;
            _encoding = encoding;
            _standalone = standalone;
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
            ArgumentNullException.ThrowIfNull(other);

            _version = other._version;
            _encoding = other._encoding;
            _standalone = other._standalone;
        }

        internal XDeclaration(XmlReader r)
        {
            _version = r.GetAttribute("version");
            _encoding = r.GetAttribute("encoding");
            _standalone = r.GetAttribute("standalone");
            r.Read();
        }

        /// <summary>
        /// Gets or sets the encoding for this document.
        /// </summary>
        public string? Encoding
        {
            get { return _encoding; }
            set { _encoding = value; }
        }

        /// <summary>
        /// Gets or sets the standalone property for this document.
        /// </summary>
        /// <remarks>
        /// The valid values for standalone are "yes" or "no".
        /// </remarks>
        public string? Standalone
        {
            get { return _standalone; }
            set { _standalone = value; }
        }

        /// <summary>
        /// Gets or sets the version property for this document.
        /// </summary>
        /// <remarks>
        /// The value is usually "1.0".
        /// </remarks>
        public string? Version
        {
            get { return _version; }
            set { _version = value; }
        }

        /// <summary>
        /// Provides a formatted string.
        /// </summary>
        /// <returns>A formatted XML string.</returns>
        public override string ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            sb.Append("<?xml");
            if (_version != null)
            {
                sb.Append(" version=\"");
                sb.Append(_version);
                sb.Append('\"');
            }
            if (_encoding != null)
            {
                sb.Append(" encoding=\"");
                sb.Append(_encoding);
                sb.Append('\"');
            }
            if (_standalone != null)
            {
                sb.Append(" standalone=\"");
                sb.Append(_standalone);
                sb.Append('\"');
            }
            sb.Append("?>");
            return StringBuilderCache.GetStringAndRelease(sb);
        }
    }
}
