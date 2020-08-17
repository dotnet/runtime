// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Threading.Tasks;
using System.Xml.XPath;

namespace System.Xml
{
    internal class XmlAsyncCheckWriter : XmlWriter
    {
        private readonly XmlWriter _coreWriter;
        private Task _lastTask = Task.CompletedTask;

        internal XmlWriter CoreWriter
        {
            get
            {
                return _coreWriter;
            }
        }

        public XmlAsyncCheckWriter(XmlWriter writer)
        {
            _coreWriter = writer;
        }

        private void CheckAsync()
        {
            if (!_lastTask.IsCompleted)
            {
                throw new InvalidOperationException(SR.Xml_AsyncIsRunningException);
            }
        }

        #region Sync Methods, Properties Check

        public override XmlWriterSettings Settings
        {
            get
            {
                XmlWriterSettings? settings = _coreWriter.Settings;

                if (null != settings)
                {
                    settings = settings.Clone();
                }
                else
                {
                    settings = new XmlWriterSettings();
                }

                settings.Async = true;

                settings.ReadOnly = true;
                return settings;
            }
        }

        public override void WriteStartDocument()
        {
            CheckAsync();
            _coreWriter.WriteStartDocument();
        }

        public override void WriteStartDocument(bool standalone)
        {
            CheckAsync();
            _coreWriter.WriteStartDocument(standalone);
        }

        public override void WriteEndDocument()
        {
            CheckAsync();
            _coreWriter.WriteEndDocument();
        }

        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            CheckAsync();
            _coreWriter.WriteDocType(name, pubid, sysid, subset);
        }

        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            CheckAsync();
            _coreWriter.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteEndElement()
        {
            CheckAsync();
            _coreWriter.WriteEndElement();
        }

        public override void WriteFullEndElement()
        {
            CheckAsync();
            _coreWriter.WriteFullEndElement();
        }

        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            CheckAsync();
            _coreWriter.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteEndAttribute()
        {
            CheckAsync();
            _coreWriter.WriteEndAttribute();
        }

        public override void WriteCData(string? text)
        {
            CheckAsync();
            _coreWriter.WriteCData(text);
        }

        public override void WriteComment(string? text)
        {
            CheckAsync();
            _coreWriter.WriteComment(text);
        }

        public override void WriteProcessingInstruction(string name, string? text)
        {
            CheckAsync();
            _coreWriter.WriteProcessingInstruction(name, text);
        }

        public override void WriteEntityRef(string name)
        {
            CheckAsync();
            _coreWriter.WriteEntityRef(name);
        }

        public override void WriteCharEntity(char ch)
        {
            CheckAsync();
            _coreWriter.WriteCharEntity(ch);
        }

        public override void WriteWhitespace(string? ws)
        {
            CheckAsync();
            _coreWriter.WriteWhitespace(ws);
        }

        public override void WriteString(string? text)
        {
            CheckAsync();
            _coreWriter.WriteString(text);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            CheckAsync();
            _coreWriter.WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            CheckAsync();
            _coreWriter.WriteChars(buffer, index, count);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            CheckAsync();
            _coreWriter.WriteRaw(buffer, index, count);
        }

        public override void WriteRaw(string data)
        {
            CheckAsync();
            _coreWriter.WriteRaw(data);
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            CheckAsync();
            _coreWriter.WriteBase64(buffer, index, count);
        }

        public override void WriteBinHex(byte[] buffer, int index, int count)
        {
            CheckAsync();
            _coreWriter.WriteBinHex(buffer, index, count);
        }

        public override WriteState WriteState
        {
            get
            {
                CheckAsync();
                return _coreWriter.WriteState;
            }
        }

        public override void Close()
        {
            CheckAsync();
            _coreWriter.Close();
        }

        public override void Flush()
        {
            CheckAsync();
            _coreWriter.Flush();
        }

        public override string? LookupPrefix(string ns)
        {
            CheckAsync();
            return _coreWriter.LookupPrefix(ns);
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                CheckAsync();
                return _coreWriter.XmlSpace;
            }
        }

        public override string? XmlLang
        {
            get
            {
                CheckAsync();
                return _coreWriter.XmlLang;
            }
        }

        public override void WriteNmToken(string name)
        {
            CheckAsync();
            _coreWriter.WriteNmToken(name);
        }

        public override void WriteName(string name)
        {
            CheckAsync();
            _coreWriter.WriteName(name);
        }

        public override void WriteQualifiedName(string localName, string? ns)
        {
            CheckAsync();
            _coreWriter.WriteQualifiedName(localName, ns);
        }

        public override void WriteValue(object value)
        {
            CheckAsync();
            _coreWriter.WriteValue(value);
        }

        public override void WriteValue(string? value)
        {
            CheckAsync();
            _coreWriter.WriteValue(value);
        }

        public override void WriteValue(bool value)
        {
            CheckAsync();
            _coreWriter.WriteValue(value);
        }

        public override void WriteValue(DateTime value)
        {
            CheckAsync();
            _coreWriter.WriteValue(value);
        }

        public override void WriteValue(DateTimeOffset value)
        {
            CheckAsync();
            _coreWriter.WriteValue(value);
        }

        public override void WriteValue(double value)
        {
            CheckAsync();
            _coreWriter.WriteValue(value);
        }

        public override void WriteValue(float value)
        {
            CheckAsync();
            _coreWriter.WriteValue(value);
        }

        public override void WriteValue(decimal value)
        {
            CheckAsync();
            _coreWriter.WriteValue(value);
        }

        public override void WriteValue(int value)
        {
            CheckAsync();
            _coreWriter.WriteValue(value);
        }

        public override void WriteValue(long value)
        {
            CheckAsync();
            _coreWriter.WriteValue(value);
        }

        public override void WriteAttributes(XmlReader reader, bool defattr)
        {
            CheckAsync();
            _coreWriter.WriteAttributes(reader, defattr);
        }

        public override void WriteNode(XmlReader reader, bool defattr)
        {
            CheckAsync();
            _coreWriter.WriteNode(reader, defattr);
        }

        public override void WriteNode(XPathNavigator navigator, bool defattr)
        {
            CheckAsync();
            _coreWriter.WriteNode(navigator, defattr);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CheckAsync();
                //since it is protected method, we can't call coreWriter.Dispose(disposing).
                //Internal, it is always called to Dispose(true). So call coreWriter.Dispose() is OK.
                _coreWriter.Dispose();
            }
        }

        #endregion

        #region Async Methods

        public override Task WriteStartDocumentAsync()
        {
            CheckAsync();
            var task = _coreWriter.WriteStartDocumentAsync();
            _lastTask = task;
            return task;
        }

        public override Task WriteStartDocumentAsync(bool standalone)
        {
            CheckAsync();
            var task = _coreWriter.WriteStartDocumentAsync(standalone);
            _lastTask = task;
            return task;
        }

        public override Task WriteEndDocumentAsync()
        {
            CheckAsync();
            var task = _coreWriter.WriteEndDocumentAsync();
            _lastTask = task;
            return task;
        }

        public override Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset)
        {
            CheckAsync();
            var task = _coreWriter.WriteDocTypeAsync(name, pubid, sysid, subset);
            _lastTask = task;
            return task;
        }

        public override Task WriteStartElementAsync(string? prefix, string localName, string? ns)
        {
            CheckAsync();
            var task = _coreWriter.WriteStartElementAsync(prefix, localName, ns);
            _lastTask = task;
            return task;
        }

        public override Task WriteEndElementAsync()
        {
            CheckAsync();
            var task = _coreWriter.WriteEndElementAsync();
            _lastTask = task;
            return task;
        }

        public override Task WriteFullEndElementAsync()
        {
            CheckAsync();
            var task = _coreWriter.WriteFullEndElementAsync();
            _lastTask = task;
            return task;
        }

        protected internal override Task WriteStartAttributeAsync(string? prefix, string localName, string? ns)
        {
            CheckAsync();
            var task = _coreWriter.WriteStartAttributeAsync(prefix, localName, ns);
            _lastTask = task;
            return task;
        }

        protected internal override Task WriteEndAttributeAsync()
        {
            CheckAsync();
            var task = _coreWriter.WriteEndAttributeAsync();
            _lastTask = task;
            return task;
        }

        public override Task WriteCDataAsync(string? text)
        {
            CheckAsync();
            var task = _coreWriter.WriteCDataAsync(text);
            _lastTask = task;
            return task;
        }

        public override Task WriteCommentAsync(string? text)
        {
            CheckAsync();
            var task = _coreWriter.WriteCommentAsync(text);
            _lastTask = task;
            return task;
        }

        public override Task WriteProcessingInstructionAsync(string name, string text)
        {
            CheckAsync();
            var task = _coreWriter.WriteProcessingInstructionAsync(name, text);
            _lastTask = task;
            return task;
        }

        public override Task WriteEntityRefAsync(string name)
        {
            CheckAsync();
            var task = _coreWriter.WriteEntityRefAsync(name);
            _lastTask = task;
            return task;
        }

        public override Task WriteCharEntityAsync(char ch)
        {
            CheckAsync();
            var task = _coreWriter.WriteCharEntityAsync(ch);
            _lastTask = task;
            return task;
        }

        public override Task WriteWhitespaceAsync(string? ws)
        {
            CheckAsync();
            var task = _coreWriter.WriteWhitespaceAsync(ws);
            _lastTask = task;
            return task;
        }

        public override Task WriteStringAsync(string? text)
        {
            CheckAsync();
            var task = _coreWriter.WriteStringAsync(text);
            _lastTask = task;
            return task;
        }

        public override Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
        {
            CheckAsync();
            var task = _coreWriter.WriteSurrogateCharEntityAsync(lowChar, highChar);
            _lastTask = task;
            return task;
        }

        public override Task WriteCharsAsync(char[] buffer, int index, int count)
        {
            CheckAsync();
            var task = _coreWriter.WriteCharsAsync(buffer, index, count);
            _lastTask = task;
            return task;
        }

        public override Task WriteRawAsync(char[] buffer, int index, int count)
        {
            CheckAsync();
            var task = _coreWriter.WriteRawAsync(buffer, index, count);
            _lastTask = task;
            return task;
        }

        public override Task WriteRawAsync(string data)
        {
            CheckAsync();
            var task = _coreWriter.WriteRawAsync(data);
            _lastTask = task;
            return task;
        }

        public override Task WriteBase64Async(byte[] buffer, int index, int count)
        {
            CheckAsync();
            var task = _coreWriter.WriteBase64Async(buffer, index, count);
            _lastTask = task;
            return task;
        }

        public override Task WriteBinHexAsync(byte[] buffer, int index, int count)
        {
            CheckAsync();
            var task = _coreWriter.WriteBinHexAsync(buffer, index, count);
            _lastTask = task;
            return task;
        }

        public override Task FlushAsync()
        {
            CheckAsync();
            var task = _coreWriter.FlushAsync();
            _lastTask = task;
            return task;
        }

        public override Task WriteNmTokenAsync(string name)
        {
            CheckAsync();
            var task = _coreWriter.WriteNmTokenAsync(name);
            _lastTask = task;
            return task;
        }

        public override Task WriteNameAsync(string name)
        {
            CheckAsync();
            var task = _coreWriter.WriteNameAsync(name);
            _lastTask = task;
            return task;
        }

        public override Task WriteQualifiedNameAsync(string localName, string? ns)
        {
            CheckAsync();
            var task = _coreWriter.WriteQualifiedNameAsync(localName, ns);
            _lastTask = task;
            return task;
        }

        public override Task WriteAttributesAsync(XmlReader reader, bool defattr)
        {
            CheckAsync();
            var task = _coreWriter.WriteAttributesAsync(reader, defattr);
            _lastTask = task;
            return task;
        }

        public override Task WriteNodeAsync(XmlReader reader, bool defattr)
        {
            CheckAsync();
            var task = _coreWriter.WriteNodeAsync(reader, defattr);
            _lastTask = task;
            return task;
        }

        public override Task WriteNodeAsync(XPathNavigator navigator, bool defattr)
        {
            CheckAsync();
            var task = _coreWriter.WriteNodeAsync(navigator, defattr);
            _lastTask = task;
            return task;
        }

        protected override ValueTask DisposeAsyncCore()
        {
            CheckAsync();
            return _coreWriter.DisposeAsync();
        }

        #endregion
    }
}
