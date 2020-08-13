// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Xml.Schema;
using System.Collections;
using System.Diagnostics;

namespace System.Xml
{
    internal partial class XmlWrappingWriter : XmlWriter
    {
        //
        // Fields
        //
        protected XmlWriter writer;

        //
        // Constructor
        //
        internal XmlWrappingWriter(XmlWriter baseWriter)
        {
            Debug.Assert(baseWriter != null);
            this.writer = baseWriter;
        }

        //
        // XmlWriter implementation
        //
        public override XmlWriterSettings? Settings { get { return writer.Settings; } }
        public override WriteState WriteState { get { return writer.WriteState; } }
        public override XmlSpace XmlSpace { get { return writer.XmlSpace; } }
        public override string? XmlLang { get { return writer.XmlLang; } }


        public override void WriteStartDocument()
        {
            writer.WriteStartDocument();
        }

        public override void WriteStartDocument(bool standalone)
        {
            writer.WriteStartDocument(standalone);
        }

        public override void WriteEndDocument()
        {
            writer.WriteEndDocument();
        }

        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            writer.WriteDocType(name, pubid, sysid, subset);
        }

        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            writer.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteEndElement()
        {
            writer.WriteEndElement();
        }

        public override void WriteFullEndElement()
        {
            writer.WriteFullEndElement();
        }

        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            writer.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteEndAttribute()
        {
            writer.WriteEndAttribute();
        }

        public override void WriteCData(string? text)
        {
            writer.WriteCData(text);
        }

        public override void WriteComment(string? text)
        {
            writer.WriteComment(text);
        }

        public override void WriteProcessingInstruction(string name, string? text)
        {
            writer.WriteProcessingInstruction(name, text);
        }

        public override void WriteEntityRef(string name)
        {
            writer.WriteEntityRef(name);
        }

        public override void WriteCharEntity(char ch)
        {
            writer.WriteCharEntity(ch);
        }

        public override void WriteWhitespace(string? ws)
        {
            writer.WriteWhitespace(ws);
        }

        public override void WriteString(string? text)
        {
            writer.WriteString(text);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            writer.WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            writer.WriteChars(buffer, index, count);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            writer.WriteRaw(buffer, index, count);
        }

        public override void WriteRaw(string data)
        {
            writer.WriteRaw(data);
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            writer.WriteBase64(buffer, index, count);
        }

        public override void Close()
        {
            writer.Close();
        }

        public override void Flush()
        {
            writer.Flush();
        }

        public override string? LookupPrefix(string ns)
        {
            return writer.LookupPrefix(ns);
        }

        public override void WriteValue(object value)
        {
            writer.WriteValue(value);
        }

        public override void WriteValue(string? value)
        {
            writer.WriteValue(value);
        }

        public override void WriteValue(bool value)
        {
            writer.WriteValue(value);
        }

        public override void WriteValue(DateTime value)
        {
            writer.WriteValue(value);
        }

        public override void WriteValue(DateTimeOffset value)
        {
            writer.WriteValue(value);
        }

        public override void WriteValue(double value)
        {
            writer.WriteValue(value);
        }

        public override void WriteValue(float value)
        {
            writer.WriteValue(value);
        }

        public override void WriteValue(decimal value)
        {
            writer.WriteValue(value);
        }

        public override void WriteValue(int value)
        {
            writer.WriteValue(value);
        }

        public override void WriteValue(long value)
        {
            writer.WriteValue(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((IDisposable)writer).Dispose();
            }
        }
    }
}
