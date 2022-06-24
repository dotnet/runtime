// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Resources;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Runtime.Serialization;

namespace System.Xml
{
    /// <devdoc>
    ///    <para>Returns detailed information about the last parse error, including the error
    ///       number, line number, character position, and a text description.</para>
    /// </devdoc>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class XmlException : SystemException
    {
        private readonly string _res;
        private readonly string?[]? _args; // this field is not used, it's here just V1.1 serialization compatibility
        private readonly int _lineNumber;
        private readonly int _linePosition;

        private readonly string? _sourceUri;

        // message != null for V1 exceptions deserialized in Whidbey
        // message == null for V2 or higher exceptions; the exception message is stored on the base class (Exception._message)
        private readonly string? _message;

        protected XmlException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _res = (string)info.GetValue("res", typeof(string))!;
            _args = (string?[])info.GetValue("args", typeof(string[]))!;
            _lineNumber = (int)info.GetValue("lineNumber", typeof(int))!;
            _linePosition = (int)info.GetValue("linePosition", typeof(int))!;

            // deserialize optional members
            _sourceUri = string.Empty;
            string? version = null;
            foreach (SerializationEntry e in info)
            {
                switch (e.Name)
                {
                    case "sourceUri":
                        _sourceUri = (string?)e.Value;
                        break;
                    case "version":
                        version = (string?)e.Value;
                        break;
                }
            }

            if (version == null)
            {
                // deserializing V1 exception
                _message = CreateMessage(_res, _args, _lineNumber, _linePosition);
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
            info.AddValue("lineNumber", _lineNumber);
            info.AddValue("linePosition", _linePosition);
            info.AddValue("sourceUri", _sourceUri);
            info.AddValue("version", "2.0");
        }

        //provided to meet the ECMA standards
        public XmlException() : this(null)
        {
        }

        //provided to meet the ECMA standards
        public XmlException(string? message) : this(message, ((Exception?)null), 0, 0)
        {
            Debug.Assert(message == null || !message.StartsWith("Xml_", StringComparison.Ordinal), "Do not pass a resource here!");
        }

        //provided to meet ECMA standards
        public XmlException(string? message, Exception? innerException) : this(message, innerException, 0, 0)
        {
        }

        //provided to meet ECMA standards
        public XmlException(string? message, Exception? innerException, int lineNumber, int linePosition) :
            this(message, innerException, lineNumber, linePosition, null)
        {
        }

        internal XmlException(string? message, Exception? innerException, int lineNumber, int linePosition, string? sourceUri) :
            base(FormatUserMessage(message, lineNumber, linePosition), innerException)
        {
            HResult = HResults.Xml;
            _res = (message == null ? SR.Xml_DefaultException : SR.Xml_UserException);
            _args = new string?[] { message };
            _sourceUri = sourceUri;
            _lineNumber = lineNumber;
            _linePosition = linePosition;
        }

        internal XmlException(string res, string?[]? args) :
            this(res, args, null, 0, 0, null)
        { }

        internal XmlException(string res, string? arg) :
            this(res, new string?[] { arg }, null, 0, 0, null)
        { }

        internal XmlException(string res, string? arg, string? sourceUri) :
            this(res, new string?[] { arg }, null, 0, 0, sourceUri)
        { }

        internal XmlException(string res, string? arg, IXmlLineInfo? lineInfo) :
            this(res, new string?[] { arg }, lineInfo, null)
        { }

        internal XmlException(string res, string? arg, Exception? innerException, IXmlLineInfo? lineInfo) :
            this(res, new string?[] { arg }, innerException, (lineInfo == null ? 0 : lineInfo.LineNumber), (lineInfo == null ? 0 : lineInfo.LinePosition), null)
        { }

        internal XmlException(string res, string?[]? args, IXmlLineInfo? lineInfo) :
            this(res, args, lineInfo, null)
        { }

        internal XmlException(string res, string?[]? args, IXmlLineInfo? lineInfo, string? sourceUri) :
            this(res, args, null, (lineInfo == null ? 0 : lineInfo.LineNumber), (lineInfo == null ? 0 : lineInfo.LinePosition), sourceUri)
        {
        }

        internal XmlException(string res, string? arg, int lineNumber, int linePosition) :
            this(res, new string?[] { arg }, null, lineNumber, linePosition, null)
        { }

        internal XmlException(string res, string? arg, int lineNumber, int linePosition, string? sourceUri) :
            this(res, new string?[] { arg }, null, lineNumber, linePosition, sourceUri)
        { }

        internal XmlException(string res, string?[]? args, int lineNumber, int linePosition) :
            this(res, args, null, lineNumber, linePosition, null)
        { }

        internal XmlException(string res, string?[]? args, int lineNumber, int linePosition, string? sourceUri) :
            this(res, args, null, lineNumber, linePosition, sourceUri)
        { }

        internal XmlException(string res, string?[]? args, Exception? innerException, int lineNumber, int linePosition) :
            this(res, args, innerException, lineNumber, linePosition, null)
        { }

        internal XmlException(string res, string?[]? args, Exception? innerException, int lineNumber, int linePosition, string? sourceUri) :
            base(CreateMessage(res, args, lineNumber, linePosition), innerException)
        {
            HResult = HResults.Xml;
            _res = res;
            _args = args;
            _sourceUri = sourceUri;
            _lineNumber = lineNumber;
            _linePosition = linePosition;
        }

        private static string FormatUserMessage(string? message, int lineNumber, int linePosition)
        {
            if (message == null)
            {
                return CreateMessage(SR.Xml_DefaultException, null, lineNumber, linePosition);
            }
            else
            {
                if (lineNumber == 0 && linePosition == 0)
                {
                    // do not reformat the message when not needed
                    return message;
                }
                else
                {
                    // add line information
                    return CreateMessage(SR.Xml_UserException, new string[] { message }, lineNumber, linePosition);
                }
            }
        }

        private static string CreateMessage(string res, string?[]? args, int lineNumber, int linePosition)
        {
            try
            {
                string message = (args == null) ? res : string.Format(res, args);

                // Line information is available -> we need to append it to the error message
                if (lineNumber != 0)
                {
                    string lineNumberStr = lineNumber.ToString(CultureInfo.InvariantCulture);
                    string linePositionStr = linePosition.ToString(CultureInfo.InvariantCulture);

                    message = SR.Format(SR.Xml_MessageWithErrorPosition, new string[] { message, lineNumberStr, linePositionStr });
                }
                return message;
            }
            catch (MissingManifestResourceException)
            {
                return $"UNKNOWN({res})";
            }
        }

        internal static string[] BuildCharExceptionArgs(string data, int invCharIndex)
        {
            return BuildCharExceptionArgs(data[invCharIndex], invCharIndex + 1 < data.Length ? data[invCharIndex + 1] : '\0');
        }

        internal static string[] BuildCharExceptionArgs(char[] data, int length, int invCharIndex)
        {
            Debug.Assert(invCharIndex < data.Length);
            Debug.Assert(invCharIndex < length);
            Debug.Assert(length <= data.Length);

            return BuildCharExceptionArgs(data[invCharIndex], invCharIndex + 1 < length ? data[invCharIndex + 1] : '\0');
        }

        internal static string[] BuildCharExceptionArgs(char invChar, char nextChar)
        {
            string[] aStringList = new string[2];

            // for surrogate characters include both high and low char in the message so that a full character is displayed
            if (XmlCharType.IsHighSurrogate(invChar) && nextChar != 0)
            {
                int combinedChar = XmlCharType.CombineSurrogateChar(nextChar, invChar);
                aStringList[0] = new string(stackalloc char[] { invChar, nextChar });
                aStringList[1] = $"0x{combinedChar:X2}";
            }
            else
            {
                // don't include 0 character in the string - in means eof-of-string in native code, where this may bubble up to
                if ((int)invChar == 0)
                {
                    aStringList[0] = ".";
                }
                else
                {
                    aStringList[0] = invChar.ToString();
                }
                aStringList[1] = $"0x{(int)invChar:X2}";
            }
            return aStringList;
        }

        public int LineNumber
        {
            get { return _lineNumber; }
        }

        public int LinePosition
        {
            get { return _linePosition; }
        }

        public string? SourceUri
        {
            get { return _sourceUri; }
        }

        public override string Message
        {
            get
            {
                return _message ?? base.Message;
            }
        }

        internal string ResString
        {
            get
            {
                return _res;
            }
        }

        internal static bool IsCatchableException(Exception e)
        {
            Debug.Assert(e != null, "Unexpected null exception");
            return !(
                e is OutOfMemoryException ||
                e is NullReferenceException
            );
        }
    };
}
