// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace System.Xml
{
    /// <summary>
    /// This writer implements XmlOutputMethod.AutoDetect.  If the first element is "html", then output will be
    /// directed to an Html writer.  Otherwise, output will be directed to an Xml writer.
    /// </summary>
    internal sealed class XmlAutoDetectWriter : XmlRawWriter, IRemovableWriter
    {
        private XmlRawWriter? _wrapped;
        private OnRemoveWriter? _onRemove;
        private readonly XmlWriterSettings _writerSettings;
        private readonly XmlEventCache _eventCache;           // Cache up events until first StartElement is encountered
        private readonly TextWriter? _textWriter;
        private readonly Stream? _strm;

        //-----------------------------------------------
        // Constructors
        //-----------------------------------------------

        private XmlAutoDetectWriter(XmlWriterSettings writerSettings)
        {
            Debug.Assert(writerSettings.OutputMethod == XmlOutputMethod.AutoDetect);

            _writerSettings = (XmlWriterSettings)writerSettings.Clone();
            _writerSettings.ReadOnly = true;

            // Start caching all events
            _eventCache = new XmlEventCache(string.Empty, true);
        }

        public XmlAutoDetectWriter(TextWriter textWriter, XmlWriterSettings writerSettings)
            : this(writerSettings)
        {
            _textWriter = textWriter;
        }

        public XmlAutoDetectWriter(Stream strm, XmlWriterSettings writerSettings)
            : this(writerSettings)
        {
            _strm = strm;
        }


        //-----------------------------------------------
        // IRemovableWriter interface
        //-----------------------------------------------

        /// <summary>
        /// This writer will raise this event once it has determined whether to replace itself with the Html or Xml writer.
        /// </summary>
        public OnRemoveWriter? OnRemoveWriterEvent
        {
            get { return _onRemove; }
            set { _onRemove = value; }
        }


        //-----------------------------------------------
        // XmlWriter interface
        //-----------------------------------------------

        public override XmlWriterSettings Settings
        {
            get { return _writerSettings; }
        }

        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteDocType(name, pubid, sysid, subset);
        }

        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            if (_wrapped == null)
            {
                // This is the first time WriteStartElement has been called, so create the Xml or Html writer
                if (ns!.Length == 0 && IsHtmlTag(localName))
                    CreateWrappedWriter(XmlOutputMethod.Html);
                else
                    CreateWrappedWriter(XmlOutputMethod.Xml);
            }

            _wrapped.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteEndAttribute()
        {
            Debug.Assert(_wrapped != null);
            _wrapped.WriteEndAttribute();
        }

        public override void WriteCData(string? text)
        {
            if (TextBlockCreatesWriter(text))
                _wrapped.WriteCData(text);
            else
                _eventCache.WriteCData(text);
        }

        public override void WriteComment(string? text)
        {
            if (_wrapped == null)
                _eventCache.WriteComment(text);
            else
                _wrapped.WriteComment(text);
        }

        public override void WriteProcessingInstruction(string name, string? text)
        {
            if (_wrapped == null)
                _eventCache.WriteProcessingInstruction(name, text);
            else
                _wrapped.WriteProcessingInstruction(name, text);
        }

        public override void WriteWhitespace(string? ws)
        {
            if (_wrapped == null)
                _eventCache.WriteWhitespace(ws);
            else
                _wrapped.WriteWhitespace(ws);
        }

        public override void WriteString(string? text)
        {
            if (TextBlockCreatesWriter(text))
                _wrapped.WriteString(text);
            else
                _eventCache.WriteString(text);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            WriteString(new string(buffer, index, count));
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            WriteRaw(new string(buffer, index, count));
        }

        public override void WriteRaw(string data)
        {
            if (TextBlockCreatesWriter(data))
                _wrapped.WriteRaw(data);
            else
                _eventCache.WriteRaw(data);
        }

        public override void WriteEntityRef(string name)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteEntityRef(name);
        }

        public override void WriteCharEntity(char ch)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteCharEntity(ch);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteBase64(buffer, index, count);
        }

        public override void WriteBinHex(byte[] buffer, int index, int count)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteBinHex(buffer, index, count);
        }

        public override void Close()
        {
            // Flush any cached events to an Xml writer
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.Close();
        }

        public override void Flush()
        {
            // Flush any cached events to an Xml writer
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.Flush();
        }

        public override void WriteValue(object value)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(string? value)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(bool value)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(DateTime value)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(DateTimeOffset value)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(double value)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(float value)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(decimal value)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(int value)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteValue(value);
        }

        public override void WriteValue(long value)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteValue(value);
        }

        //-----------------------------------------------
        // XmlRawWriter interface
        //-----------------------------------------------

        internal override IXmlNamespaceResolver? NamespaceResolver
        {
            get
            {
                return this._resolver;
            }
            set
            {
                this._resolver = value;

                if (_wrapped == null)
                    _eventCache.NamespaceResolver = value;
                else
                    _wrapped.NamespaceResolver = value;
            }
        }

        internal override void WriteXmlDeclaration(XmlStandalone standalone)
        {
            // Forces xml writer to be created
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteXmlDeclaration(standalone);
        }

        internal override void WriteXmlDeclaration(string xmldecl)
        {
            // Forces xml writer to be created
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteXmlDeclaration(xmldecl);
        }

        internal override void StartElementContent()
        {
            Debug.Assert(_wrapped != null);
            _wrapped.StartElementContent();
        }

        internal override void WriteEndElement(string prefix, string localName, string ns)
        {
            Debug.Assert(_wrapped != null);
            _wrapped.WriteEndElement(prefix, localName, ns);
        }

        internal override void WriteFullEndElement(string prefix, string localName, string ns)
        {
            Debug.Assert(_wrapped != null);
            _wrapped.WriteFullEndElement(prefix, localName, ns);
        }

        internal override void WriteNamespaceDeclaration(string prefix, string ns)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteNamespaceDeclaration(prefix, ns);
        }

        internal override bool SupportsNamespaceDeclarationInChunks
        {
            get
            {
                return _wrapped!.SupportsNamespaceDeclarationInChunks;
            }
        }

        internal override void WriteStartNamespaceDeclaration(string prefix)
        {
            EnsureWrappedWriter(XmlOutputMethod.Xml);
            _wrapped.WriteStartNamespaceDeclaration(prefix);
        }

        internal override void WriteEndNamespaceDeclaration()
        {
            _wrapped!.WriteEndNamespaceDeclaration();
        }

        //-----------------------------------------------
        // Helper methods
        //-----------------------------------------------

        /// <summary>
        /// Return true if "tagName" == "html" (case-insensitive).
        /// </summary>
        private static bool IsHtmlTag(string tagName) =>
            tagName.Equals("html", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// If a wrapped writer has not yet been created, create one.
        /// </summary>
        [MemberNotNull(nameof(_wrapped))]
        private void EnsureWrappedWriter(XmlOutputMethod outMethod)
        {
            if (_wrapped == null)
                CreateWrappedWriter(outMethod);
        }

        /// <summary>
        /// If the specified text consist only of whitespace, then cache the whitespace, as it is not enough to
        /// force the creation of a wrapped writer.  Otherwise, create a wrapped writer if one has not yet been
        /// created and return true.
        /// </summary>
        [MemberNotNullWhen(true, nameof(_wrapped))]
        private bool TextBlockCreatesWriter(string? textBlock)
        {
            if (_wrapped == null)
            {
                // Whitespace-only text blocks aren't enough to determine Xml vs. Html
                if (XmlCharType.IsOnlyWhitespace(textBlock))
                {
                    return false;
                }

                // Non-whitespace text block selects Xml method
                CreateWrappedWriter(XmlOutputMethod.Xml);
            }

            return true;
        }

        /// <summary>
        /// Create either the Html or Xml writer and send any cached events to it.
        /// </summary>
        [MemberNotNull(nameof(_wrapped))]
        private void CreateWrappedWriter(XmlOutputMethod outMethod)
        {
            Debug.Assert(_wrapped == null);

            // Create either the Xml or Html writer
            _writerSettings.ReadOnly = false;
            _writerSettings.OutputMethod = outMethod;

            // If Indent was not set by the user, then default to True for Html
            if (outMethod == XmlOutputMethod.Html && _writerSettings.IndentInternal == TriState.Unknown)
                _writerSettings.Indent = true;

            _writerSettings.ReadOnly = true;

            if (_textWriter != null)
                _wrapped = ((XmlWellFormedWriter)XmlWriter.Create(_textWriter, _writerSettings)).RawWriter!;
            else
                _wrapped = ((XmlWellFormedWriter)XmlWriter.Create(_strm!, _writerSettings)).RawWriter!;

            // Send cached events to the new writer
            _eventCache.EndEvents();
            _eventCache.EventsToWriter(_wrapped);

            // Send OnRemoveWriter event
            if (_onRemove != null)
                (this._onRemove)(_wrapped);
        }
    }
}
