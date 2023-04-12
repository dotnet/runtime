// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;

namespace System.Xml.Xsl.XPath
{
    [Serializable]
    internal sealed class XPathCompileException : XslLoadException
    {
        public string? queryString;
        public int startChar;
        public int endChar;

        internal XPathCompileException(string queryString, int startChar, int endChar, string resId, params string?[]? args)
            : base(resId, args)
        {
            this.queryString = queryString;
            this.startChar = startChar;
            this.endChar = endChar;
        }

        internal XPathCompileException(string resId, params string[] args)
            : base(resId, args)
        { } // queryString will be set later

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        internal XPathCompileException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            queryString = (string)info.GetValue("QueryString", typeof(string))!;
            startChar = (int)info.GetValue("StartChar", typeof(int))!;
            endChar = (int)info.GetValue("EndChar", typeof(int))!;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("QueryString", queryString);
            info.AddValue("StartChar", startChar);
            info.AddValue("EndChar", endChar);
        }

        private enum TrimType
        {
            Left,
            Right,
            Middle,
        }

        // This function is used to prevent long quotations in error messages, SQLBUDT 222626
        private static void AppendTrimmed(StringBuilder sb, string value, int startIndex, int count, TrimType trimType)
        {
            const int TrimSize = 32;
            const string TrimMarker = "...";

            if (count <= TrimSize)
            {
                sb.Append(value, startIndex, count);
            }
            else
            {
                switch (trimType)
                {
                    case TrimType.Left:
                        sb.Append(TrimMarker);
                        sb.Append(value, startIndex + count - TrimSize, TrimSize);
                        break;
                    case TrimType.Right:
                        sb.Append(value, startIndex, TrimSize);
                        sb.Append(TrimMarker);
                        break;
                    case TrimType.Middle:
                        sb.Append(value, startIndex, TrimSize / 2);
                        sb.Append(TrimMarker);
                        sb.Append(value, startIndex + count - TrimSize / 2, TrimSize / 2);
                        break;
                }
            }
        }

        internal string? MarkOutError()
        {
            if (queryString == null || queryString.Trim(' ').Length == 0)
            {
                return null;
            }

            int len = endChar - startChar;
            StringBuilder sb = new StringBuilder();

            AppendTrimmed(sb, queryString, 0, startChar, TrimType.Left);
            if (len > 0)
            {
                sb.Append(" -->");
                AppendTrimmed(sb, queryString, startChar, len, TrimType.Middle);
            }

            sb.Append("<-- ");
            AppendTrimmed(sb, queryString, endChar, queryString.Length - endChar, TrimType.Right);

            return sb.ToString();
        }

        internal override string FormatDetailedMessage()
        {
            string message = Message;
            string? error = MarkOutError();

            if (error != null && error.Length > 0)
            {
                if (message.Length > 0)
                {
                    message += Environment.NewLine;
                }
                message += error;
            }
            return message;
        }
    }
}
