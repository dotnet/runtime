// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Resources;
using System.Runtime.Serialization;
using System.Globalization;
using System.Diagnostics;

namespace System.Xml.Schema
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class XmlSchemaException : SystemException
    {
        private readonly string? _res;
        private readonly string?[]? _args;
        private string? _sourceUri;
        private int _lineNumber;
        private int _linePosition;

        private XmlSchemaObject? _sourceSchemaObject;

        // message != null for V1 exceptions deserialized in Whidbey
        // message == null for V2 or higher exceptions; the exception message is stored on the base class (Exception._message)
        private readonly string? _message;

        protected XmlSchemaException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _res = (string?)info.GetValue("res", typeof(string));
            _args = (string[]?)info.GetValue("args", typeof(string[]));
            _sourceUri = (string?)info.GetValue("sourceUri", typeof(string));
            _lineNumber = (int)info.GetValue("lineNumber", typeof(int))!;
            _linePosition = (int)info.GetValue("linePosition", typeof(int))!;

            // deserialize optional members
            string? version = null;
            foreach (SerializationEntry e in info)
            {
                if (e.Name == "version")
                {
                    version = (string?)e.Value;
                }
            }

            if (version == null)
            {
                // deserializing V1 exception
                _message = CreateMessage(_res!, _args);
            }
            else
            {
                // deserializing V2 or higher exception -> exception message is serialized by the base class (Exception._message)
                _message = null;
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("res", _res);
            info.AddValue("args", _args);
            info.AddValue("sourceUri", _sourceUri);
            info.AddValue("lineNumber", _lineNumber);
            info.AddValue("linePosition", _linePosition);
            info.AddValue("version", "2.0");
        }

        public XmlSchemaException() : this(null)
        {
        }

        public XmlSchemaException(string? message) : this(message, ((Exception?)null), 0, 0)
        {
#if DEBUG
            Debug.Assert(message == null || !message.StartsWith("Sch_", StringComparison.Ordinal), "Do not pass a resource here!");
#endif
        }

        public XmlSchemaException(string? message, Exception? innerException) : this(message, innerException, 0, 0)
        {
        }

        public XmlSchemaException(string? message, Exception? innerException, int lineNumber, int linePosition) :
            this((message == null ? SR.Sch_DefaultException : SR.Xml_UserException), new string?[] { message }, innerException, null, lineNumber, linePosition, null)
        {
        }

        internal XmlSchemaException(string? res, string?[]? args) :
            this(res, args, null, null, 0, 0, null)
        { }

        internal XmlSchemaException(string? res, string? arg) :
            this(res, new string?[] { arg }, null, null, 0, 0, null)
        { }

        internal XmlSchemaException(string? res, string? arg, string? sourceUri, int lineNumber, int linePosition) :
            this(res, new string?[] { arg }, null, sourceUri, lineNumber, linePosition, null)
        { }

        internal XmlSchemaException(string? res, string? sourceUri, int lineNumber, int linePosition) :
            this(res, (string?[]?)null, null, sourceUri, lineNumber, linePosition, null)
        { }

        internal XmlSchemaException(string? res, string?[]? args, string? sourceUri, int lineNumber, int linePosition) :
            this(res, args, null, sourceUri, lineNumber, linePosition, null)
        { }

        internal XmlSchemaException(string? res, XmlSchemaObject source) :
            this(res, (string?[]?)null, source)
        { }

        internal XmlSchemaException(string? res, string? arg, XmlSchemaObject source) :
            this(res, new string?[] { arg }, source)
        { }

        internal XmlSchemaException(string? res, string?[]? args, XmlSchemaObject source) :
            this(res, args, null, source.SourceUri, source.LineNumber, source.LinePosition, source)
        { }

        internal XmlSchemaException(string? res, string?[]? args, Exception? innerException, string? sourceUri, int lineNumber, int linePosition, XmlSchemaObject? source) :
            base(CreateMessage(res, args), innerException)
        {
            HResult = HResults.XmlSchema;
            _res = res;
            _args = args;
            _sourceUri = sourceUri;
            _lineNumber = lineNumber;
            _linePosition = linePosition;
            _sourceSchemaObject = source;
        }

        internal static string? CreateMessage(string? res, string?[]? args)
        {
            try
            {
                if (args == null)
                {
                    return res;
                }

                return string.Format(res ?? string.Empty, args);
            }
            catch (MissingManifestResourceException)
            {
                return $"UNKNOWN({res})";
            }
        }

        internal string? GetRes
        {
            get
            {
                return _res;
            }
        }

        internal string?[]? Args
        {
            get
            {
                return _args;
            }
        }

        public string? SourceUri
        {
            get { return _sourceUri; }
        }

        public int LineNumber
        {
            get { return _lineNumber; }
        }

        public int LinePosition
        {
            get { return _linePosition; }
        }

        public XmlSchemaObject? SourceSchemaObject
        {
            get { return _sourceSchemaObject; }
        }

        /*internal static XmlSchemaException Create(string res) { //Since internal overload with res string will clash with public constructor that takes in a message
            return new XmlSchemaException(res, (string[])null, null, null, 0, 0, null);
        }*/

        internal void SetSource(string? sourceUri, int lineNumber, int linePosition)
        {
            _sourceUri = sourceUri;
            _lineNumber = lineNumber;
            _linePosition = linePosition;
        }

        internal void SetSchemaObject(XmlSchemaObject source)
        {
            _sourceSchemaObject = source;
        }

        internal void SetSource(XmlSchemaObject source)
        {
            _sourceSchemaObject = source;
            _sourceUri = source.SourceUri;
            _lineNumber = source.LineNumber;
            _linePosition = source.LinePosition;
        }

        public override string Message
        {
            get
            {
                return _message ?? base.Message;
            }
        }
    };
}
