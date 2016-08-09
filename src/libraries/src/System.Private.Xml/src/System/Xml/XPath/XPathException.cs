// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Resources;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace System.Xml.XPath
{
    // Represents the exception that is thrown when there is error processing an
    // XPath expression.
    public class XPathException : Exception
    {
        // we need to keep this members for V1 serialization compatibility
        private string _res;
        private string[] _args;

        // message != null for V1 & V2 exceptions deserialized in Whidbey
        // message == null for created V2 exceptions; the exception message is stored in Exception._message
        private string _message;

        public XPathException() : this(string.Empty, (Exception)null) { }

        public XPathException(string message) : this(message, (Exception)null) { }

        public XPathException(string message, Exception innerException) :
            this(SR.Xml_UserException, new string[] { message }, innerException)
        {
        }

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

        private XPathException(string res, string[] args) :
            this(res, args, null)
        {
        }

        private XPathException(string res, string[] args, Exception inner) :
            base(CreateMessage(res, args), inner)
        {
            HResult = HResults.XmlXPath;
            _res = res;
            _args = args;
        }

        private static string CreateMessage(string res, string[] args)
        {
            try
            {
                string message = args == null ? res : string.Format(res, args);
                if (message == null)
                    message = "UNKNOWN(" + res + ")";
                return message;
            }
            catch (MissingManifestResourceException)
            {
                return "UNKNOWN(" + res + ")";
            }
        }

        public override string Message
        {
            get
            {
                return (_message == null) ? base.Message : _message;
            }
        }
    }
}

