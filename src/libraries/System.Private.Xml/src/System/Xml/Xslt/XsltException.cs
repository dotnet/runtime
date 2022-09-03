// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;
using System.Runtime.Serialization;
using System.Xml.XPath;

namespace System.Xml.Xsl
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class XsltException : SystemException
    {
        private readonly string _res;
        private readonly string[]? _args;
        private readonly string? _sourceUri;
        private readonly int _lineNumber;
        private readonly int _linePosition;

        // message != null for V1 & V2 exceptions deserialized in Whidbey
        // message == null for created V2 exceptions; the exception message is stored in Exception._message
        private readonly string? _message;

        protected XsltException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _res = (string)info.GetValue("res", typeof(string))!;
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
                _message = CreateMessage(_res, _args, _sourceUri, _lineNumber, _linePosition);
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

        public XsltException() : this(string.Empty, (Exception?)null) { }

        public XsltException(string message) : this(message, (Exception?)null) { }

        public XsltException(string message, Exception? innerException) :
            this(SR.Xml_UserException, new string?[] { message }, null, 0, 0, innerException)
        {
        }

        internal static XsltException Create(string res, params string?[] args)
        {
            return new XsltException(res, args, null, 0, 0, null);
        }

        internal static XsltException Create(string res, string?[] args, Exception inner)
        {
            return new XsltException(res, args, null, 0, 0, inner);
        }

        internal XsltException(string res, string?[] args, string? sourceUri, int lineNumber, int linePosition, Exception? inner)
            : base(CreateMessage(res, args, sourceUri, lineNumber, linePosition), inner)
        {
            HResult = HResults.XmlXslt;
            _res = res;
            _sourceUri = sourceUri;
            _lineNumber = lineNumber;
            _linePosition = linePosition;
        }

        public virtual string? SourceUri
        {
            get { return _sourceUri; }
        }

        public virtual int LineNumber
        {
            get { return _lineNumber; }
        }

        public virtual int LinePosition
        {
            get { return _linePosition; }
        }

        public override string Message
        {
            get
            {
                return _message ?? base.Message;
            }
        }

        private static string CreateMessage(string res, string?[]? args, string? sourceUri, int lineNumber, int linePosition)
        {
            try
            {
                string message = FormatMessage(res, args);
                if (res != SR.Xslt_CompileError && lineNumber != 0)
                {
                    message += $" {FormatMessage(SR.Xml_ErrorFilePosition, sourceUri, lineNumber.ToString(CultureInfo.InvariantCulture), linePosition.ToString(CultureInfo.InvariantCulture))}";
                }
                return message;
            }
            catch (MissingManifestResourceException)
            {
                return $"UNKNOWN({res})";
            }
        }

        [return: NotNullIfNotNull(nameof(key))]
        private static string? FormatMessage(string? key, params string?[]? args)
        {
            string? message = key;
            if (message != null && args != null)
            {
                message = string.Format(CultureInfo.InvariantCulture, message, args);
            }
            return message;
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class XsltCompileException : XsltException
    {
        protected XsltCompileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        public XsltCompileException() : base() { }

        public XsltCompileException(string message) : base(message) { }

        public XsltCompileException(string message, Exception innerException) : base(message, innerException) { }

        public XsltCompileException(Exception inner, string sourceUri, int lineNumber, int linePosition) :
            base(
                lineNumber != 0 ? SR.Xslt_CompileError : SR.Xslt_CompileError2,
                new string[] { sourceUri, lineNumber.ToString(CultureInfo.InvariantCulture), linePosition.ToString(CultureInfo.InvariantCulture) },
                sourceUri, lineNumber, linePosition, inner
            )
        { }
    }
}
