// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Xsl.Xslt;

namespace System.Xml.Xsl
{
    internal class XslTransformException : XsltException
    {
        public XslTransformException(Exception inner, string res, params string[] args)
            : base(CreateMessage(res, args), inner)
        { }

        public XslTransformException(string message)
            : base(CreateMessage(message, null), null)
        { }

        internal XslTransformException(string res, params string[] args)
            : this(null, res, args)
        { }

        internal static string CreateMessage(string res, params string[] args)
        {
            string message = null;

            try
            {
                if (args == null)
                {
                    message = res;
                }
                else
                {
                    message = string.Format(res, args);
                }
            }
            catch (MissingManifestResourceException)
            {
            }

            if (message != null)
            {
                return message;
            }

            StringBuilder sb = new StringBuilder(res);
            if (args != null && args.Length > 0)
            {
                Debug.Fail("Resource string '" + res + "' was not found");
                sb.Append('(');
                sb.Append(args[0]);
                for (int idx = 1; idx < args.Length; idx++)
                {
                    sb.Append(", ");
                    sb.Append(args[idx]);
                }
                sb.Append(')');
            }
            return sb.ToString();
        }

        internal virtual string FormatDetailedMessage()
        {
            return Message;
        }

        public override string ToString()
        {
            string result = this.GetType().FullName;
            string info = FormatDetailedMessage();
            if (info != null && info.Length > 0)
            {
                result += ": " + info;
            }
            if (InnerException != null)
            {
                result += " ---> " + InnerException.ToString() + Environment.NewLine + "   " + CreateMessage(SR.Xml_EndOfInnerExceptionStack);
            }
            if (StackTrace != null)
            {
                result += Environment.NewLine + StackTrace;
            }
            return result;
        }
    }

    internal class XslLoadException : XslTransformException
    {
        private ISourceLineInfo _lineInfo;

        internal XslLoadException(string res, params string[] args)
            : base(null, res, args)
        { }

        internal XslLoadException(Exception inner, ISourceLineInfo lineInfo)
            : base(inner, SR.Xslt_CompileError2, null)
        {
            SetSourceLineInfo(lineInfo);
        }

        internal XslLoadException(CompilerError error)
            : base(SR.Xml_UserException, new string[] { error.ErrorText })
        {
            int errorLine = error.Line;
            int errorColumn = error.Column;

            if (errorLine == 0)
            {
                // If the compiler reported error on Line 0 - ignore columns, 
                //   0 means it doesn't know where the error was and our SourceLineInfo
                //   expects either all zeroes or all non-zeroes
                errorColumn = 0;
            }
            else
            {
                if (errorColumn == 0)
                {
                    // In this situation the compiler returned for example Line 10, Column 0.
                    // This means that the compiler knows the line of the error, but it doesn't
                    //   know (or support) the column part of the location.
                    // Since we don't allow column 0 (as it's invalid), let's turn it into 1 (the begining of the line)
                    errorColumn = 1;
                }
            }

            SetSourceLineInfo(new SourceLineInfo(error.FileName, errorLine, errorColumn, errorLine, errorColumn));
        }

        internal void SetSourceLineInfo(ISourceLineInfo lineInfo)
        {
            Debug.Assert(lineInfo == null || lineInfo.Uri != null);
            _lineInfo = lineInfo;
        }

        public override string SourceUri
        {
            get { return _lineInfo != null ? _lineInfo.Uri : null; }
        }

        public override int LineNumber
        {
            get { return _lineInfo != null ? _lineInfo.Start.Line : 0; }
        }

        public override int LinePosition
        {
            get { return _lineInfo != null ? _lineInfo.Start.Pos : 0; }
        }

        private static string AppendLineInfoMessage(string message, ISourceLineInfo lineInfo)
        {
            if (lineInfo != null)
            {
                string fileName = SourceLineInfo.GetFileName(lineInfo.Uri);
                string lineInfoMessage = CreateMessage(SR.Xml_ErrorFilePosition, fileName, lineInfo.Start.Line.ToString(CultureInfo.InvariantCulture), lineInfo.Start.Pos.ToString(CultureInfo.InvariantCulture));
                if (lineInfoMessage != null && lineInfoMessage.Length > 0)
                {
                    if (message.Length > 0 && !XmlCharType.Instance.IsWhiteSpace(message[message.Length - 1]))
                    {
                        message += " ";
                    }
                    message += lineInfoMessage;
                }
            }
            return message;
        }

        internal static string CreateMessage(ISourceLineInfo lineInfo, string res, params string[] args)
        {
            return AppendLineInfoMessage(CreateMessage(res, args), lineInfo);
        }

        internal override string FormatDetailedMessage()
        {
            return AppendLineInfoMessage(Message, _lineInfo);
        }
    }
}
