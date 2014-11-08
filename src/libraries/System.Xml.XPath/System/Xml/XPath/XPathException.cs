// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Resources;

namespace System.Xml.XPath
{
    // Represents the exception that is thrown when there is error processing an
    // XPath expression.
    public class XPathException : System.Exception
    {
        public XPathException() : this(string.Empty, (Exception)null) { }

        public XPathException(string message) : this(message, (Exception)null) { }

        public XPathException(string message, Exception innerException) : base(message, innerException) { }

        internal static XPathException Create(string res)
        {
            return new XPathException(res, (string[])null);
        }

        internal static XPathException Create(string res, string arg)
        {
            return new XPathException(res, new string[] { arg });
        }

        internal static XPathException Create(string res, string arg, string arg2)
        {
            return new XPathException(res, new string[] { arg, arg2 });
        }

        internal static XPathException Create(string res, string arg, Exception innerException)
        {
            return new XPathException(res, new string[] { arg }, innerException);
        }

        internal XPathException(string res, string arg, IXmlLineInfo lineInfo) :
            this(res, new string[] { arg }, lineInfo)
        { }

        internal XPathException(string res, string[] args, IXmlLineInfo lineInfo) :
            this(res, args, null, (lineInfo == null ? 0 : lineInfo.LineNumber), (lineInfo == null ? 0 : lineInfo.LinePosition))
        {
        }

        internal XPathException(string res, string[] args, Exception innerException, int lineNumber, int linePosition) :
            base(CreateMessage(res, args, lineNumber, linePosition), innerException)
        {
        }

        private XPathException(string res, string[] args) :
            this(res, args, (Exception)null)
        {
        }

        private XPathException(string res, string[] args, Exception inner) :
            base(CreateMessage(res, args), inner)
        {
        }

        private static string CreateUnknownResourceMessage(string res)
        {
            return SR.Format(SR.Xml_UnknownResourceString, res);
        }

        private static string CreateMessage(string res, string[] args)
        {
            try
            {
                string message = SR.Format(res, args);
                if (message == null)
                    message = CreateUnknownResourceMessage(res);
                return message;
            }
            catch (MissingManifestResourceException)
            {
                return CreateUnknownResourceMessage(res);
            }
        }

        private static string CreateMessage(string res, string[] args, int lineNumber, int linePosition)
        {
            try
            {
                string message;

                // No line information -> get resource string and return
                if (lineNumber == 0)
                {
                    message = SR.Format(res, args);
                }
                // Line information is available -> we need to append it to the error message
                else
                {
                    string lineNumberStr = lineNumber.ToString(CultureInfo.InvariantCulture);
                    string linePositionStr = linePosition.ToString(CultureInfo.InvariantCulture);

                    message = SR.Format(res, args);
                    message = SR.Format(SR.Xml_MessageWithErrorPosition, new string[] { message, lineNumberStr, linePositionStr });
                }
                return message;
            }
            catch (MissingManifestResourceException)
            {
                return CreateUnknownResourceMessage(res);
            }
        }
    }
}

