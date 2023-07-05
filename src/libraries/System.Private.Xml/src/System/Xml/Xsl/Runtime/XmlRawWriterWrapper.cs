// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// This internal class implements the XmlRawWriter interface by passing all calls to a wrapped XmlWriter implementation.
    /// </summary>
    internal sealed class XmlRawWriterWrapper : XmlRawWriter
    {
        private readonly XmlWriter _wrapped;

        public XmlRawWriterWrapper(XmlWriter writer)
        {
            _wrapped = writer;
        }


        //-----------------------------------------------
        // XmlWriter interface
        //-----------------------------------------------

        public override XmlWriterSettings? Settings
        {
            get { return _wrapped.Settings; }
        }

        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            _wrapped.WriteDocType(name, pubid, sysid, subset);
        }

        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            _wrapped.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            _wrapped.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteEndAttribute()
        {
            _wrapped.WriteEndAttribute();
        }

        public override void WriteCData(string? text)
        {
            _wrapped.WriteCData(text);
        }

        public override void WriteComment(string? text)
        {
            _wrapped.WriteComment(text);
        }

        public override void WriteProcessingInstruction(string name, string? text)
        {
            _wrapped.WriteProcessingInstruction(name, text);
        }

        public override void WriteWhitespace(string? ws)
        {
            _wrapped.WriteWhitespace(ws);
        }

        public override void WriteString(string? text)
        {
            _wrapped.WriteString(text);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            _wrapped.WriteChars(buffer, index, count);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            _wrapped.WriteRaw(buffer, index, count);
        }

        public override void WriteRaw(string data)
        {
            _wrapped.WriteRaw(data);
        }

        public override void WriteEntityRef(string name)
        {
            _wrapped.WriteEntityRef(name);
        }

        public override void WriteCharEntity(char ch)
        {
            _wrapped.WriteCharEntity(ch);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            _wrapped.WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override void Close()
        {
            _wrapped.Close();
        }

        public override void Flush()
        {
            _wrapped.Flush();
        }

        public override void WriteValue(object value)
        {
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(string? value)
        {
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(bool value)
        {
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(DateTime value)
        {
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(float value)
        {
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(decimal value)
        {
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(double value)
        {
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(int value)
        {
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(long value)
        {
            _wrapped.WriteValue(value);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    ((IDisposable)_wrapped).Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }


        //-----------------------------------------------
        // XmlRawWriter interface
        //-----------------------------------------------

        /// <summary>
        /// No-op.
        /// </summary>
        internal override void WriteXmlDeclaration(XmlStandalone standalone)
        {
        }

        /// <summary>
        /// No-op.
        /// </summary>
        internal override void WriteXmlDeclaration(string xmldecl)
        {
        }

        /// <summary>
        /// No-op.
        /// </summary>
        internal override void StartElementContent()
        {
        }

        /// <summary>
        /// Forward to WriteEndElement().
        /// </summary>
        internal override void WriteEndElement(string? prefix, string localName, string? ns)
        {
            _wrapped.WriteEndElement();
        }

        /// <summary>
        /// Forward to WriteFullEndElement().
        /// </summary>
        internal override void WriteFullEndElement(string prefix, string localName, string ns)
        {
            _wrapped.WriteFullEndElement();
        }

        /// <summary>
        /// Forward to WriteAttribute();
        /// </summary>
        internal override void WriteNamespaceDeclaration(string prefix, string ns)
        {
            if (prefix.Length == 0)
                _wrapped.WriteAttributeString(string.Empty, "xmlns", XmlReservedNs.NsXmlNs, ns);
            else
                _wrapped.WriteAttributeString("xmlns", prefix, XmlReservedNs.NsXmlNs, ns);
        }
    }
}
