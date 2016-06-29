// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

using CultureInfo = System.Globalization.CultureInfo;

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents a class that allows elements to be streamed
    /// on input and output.
    /// </summary>
    public class XStreamingElement
    {
        internal XName name;
        internal object content;

        /// <summary>
        ///  Creates a <see cref="XStreamingElement"/> node with a given name
        /// </summary>
        /// <param name="name">The name to assign to the new <see cref="XStreamingElement"/> node</param>
        public XStreamingElement(XName name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            this.name = name;
        }

        /// <summary>
        /// Creates a <see cref="XStreamingElement"/> node with a given name and content
        /// </summary>
        /// <param name="name">The name to assign to the new <see cref="XStreamingElement"/> node</param>
        /// <param name="content">The content to assign to the new <see cref="XStreamingElement"/> node</param>
        public XStreamingElement(XName name, object content)
            : this(name)
        {
            this.content = content is List<object> ? new object[] { content } : content;
        }

        /// <summary>
        /// Creates a <see cref="XStreamingElement"/> node with a given name and content
        /// </summary>
        /// <param name="name">The name to assign to the new <see cref="XStreamingElement"/> node</param>
        /// <param name="content">An array containing content to assign to the new <see cref="XStreamingElement"/> node</param>
        public XStreamingElement(XName name, params object[] content)
            : this(name)
        {
            this.content = content;
        }

        /// <summary>
        /// Gets or sets the name of this streaming element.
        /// </summary>
        public XName Name
        {
            get
            {
                return name;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                name = value;
            }
        }

        /// <summary>
        /// Add content to an <see cref="XStreamingElement"/>
        /// </summary>
        /// <param name="content">Object containing content to add</param>
        public void Add(object content)
        {
            if (content != null)
            {
                List<object> list = this.content as List<object>;
                if (list == null)
                {
                    list = new List<object>();
                    if (this.content != null) list.Add(this.content);
                    this.content = list;
                }
                list.Add(content);
            }
        }

        /// <summary>
        /// Add content to an <see cref="XStreamingElement"/>
        /// </summary>
        /// <param name="content">array of objects containing content to add</param>
        public void Add(params object[] content)
        {
            Add((object)content);
        }

        /// <summary>
        /// Save the contents of an <see cref="XStreamingElement"/> to a <see cref="Stream"/>
        /// with formatting.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to write to </param>      
        public void Save(Stream stream)
        {
            Save(stream, SaveOptions.None);
        }

        /// <summary>
        /// Save the contents of an <see cref="XStreamingElement"/> to a <see cref="Stream"/>,
        /// optionally formatting.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to write to </param>
        /// <param name="options">
        /// If SaveOptions.DisableFormatting is enabled the output is not indented.
        /// If SaveOptions.OmitDuplicateNamespaces is enabled duplicate namespace declarations will be removed.
        /// </param>
        public void Save(Stream stream, SaveOptions options)
        {
            XmlWriterSettings ws = XNode.GetXmlWriterSettings(options);
            using (XmlWriter w = XmlWriter.Create(stream, ws))
            {
                Save(w);
            }
        }

        /// <summary>
        /// Save the contents of an <see cref="XStreamingElement"/> to a text writer
        /// with formatting.
        /// </summary>
        /// <param name="textWriter"><see cref="TextWriter"/> to write to </param>      
        public void Save(TextWriter textWriter)
        {
            Save(textWriter, SaveOptions.None);
        }

        /// <summary>
        /// Save the contents of an <see cref="XStreamingElement"/> to a text writer
        /// optionally formatting.
        /// </summary>
        /// <param name="textWriter"><see cref="TextWriter"/> to write to </param>
        /// <param name="options">
        /// If SaveOptions.DisableFormatting is enabled the output is not indented.
        /// If SaveOptions.OmitDuplicateNamespaces is enabled duplicate namespace declarations will be removed.
        /// </param>
        public void Save(TextWriter textWriter, SaveOptions options)
        {
            XmlWriterSettings ws = XNode.GetXmlWriterSettings(options);
            using (XmlWriter w = XmlWriter.Create(textWriter, ws))
            {
                Save(w);
            }
        }

        /// <summary>
        /// Save the contents of an <see cref="XStreamingElement"/> to an XML writer, not preserving whitespace
        /// </summary>
        /// <param name="writer"><see cref="XmlWriter"/> to write to </param>    
        public void Save(XmlWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            writer.WriteStartDocument();
            WriteTo(writer);
            writer.WriteEndDocument();
        }

        /// <summary>
        /// Save an <see cref="XStreamingElement"/> to a file with formatting.
        /// </summary>
        /// <param name="fileName">Name of file to write content to</param>
        public void Save(string fileName)
        {
            Save(fileName, SaveOptions.None);
        }

        /// <summary>
        /// Save an <see cref="XStreamingElement"/> to a file, optionally formatting. 
        /// </summary>
        /// <param name="fileName">Name of file to write content to</param>
        /// <param name="options">
        /// If SaveOptions.DisableFormatting is enabled the output is not indented.
        /// If SaveOptions.OmitDuplicateNamespaces is enabled duplicate namespace declarations will be removed.
        /// </param>
        public void Save(string fileName, SaveOptions options)
        {
            XmlWriterSettings ws = XNode.GetXmlWriterSettings(options);
            using (XmlWriter w = XmlWriter.Create(fileName, ws))
            {
                Save(w);
            }
        }

        /// <summary>
        /// Get the XML content of an <see cref="XStreamingElement"/> as a 
        /// formatted string.
        /// </summary>
        /// <returns>The XML text as a formatted string</returns>
        public override string ToString()
        {
            return GetXmlString(SaveOptions.None);
        }

        /// <summary>
        /// Gets the XML content of this streaming element as a string.
        /// </summary>
        /// <param name="options">
        /// If SaveOptions.DisableFormatting is enabled the content is not indented.
        /// If SaveOptions.OmitDuplicateNamespaces is enabled duplicate namespace declarations will be removed.
        /// </param>
        /// <returns>An XML string</returns>
        public string ToString(SaveOptions options)
        {
            return GetXmlString(options);
        }

        /// <summary>
        /// Write this <see cref="XStreamingElement"/> to an <see cref="XmlWriter"/>
        /// </summary>
        /// <param name="writer"></param>
        public void WriteTo(XmlWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            new StreamingElementWriter(writer).WriteStreamingElement(this);
        }

        private string GetXmlString(SaveOptions o)
        {
            using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
            {
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.OmitXmlDeclaration = true;
                if ((o & SaveOptions.DisableFormatting) == 0) ws.Indent = true;
                if ((o & SaveOptions.OmitDuplicateNamespaces) != 0) ws.NamespaceHandling |= NamespaceHandling.OmitDuplicates;
                using (XmlWriter w = XmlWriter.Create(sw, ws))
                {
                    WriteTo(w);
                }
                return sw.ToString();
            }
        }
    }
}
