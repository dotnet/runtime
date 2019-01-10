// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    /// <summary>
    /// Returns detailed information relating to
    /// the ValidationEventhandler.
    /// </summary>
    public class ValidationEventArgs : EventArgs
    {
        private XmlSchemaException _ex;
        private XmlSeverityType _severity;

        internal ValidationEventArgs(XmlSchemaException ex) : base()
        {
            _ex = ex;
            _severity = XmlSeverityType.Error;
        }

        internal ValidationEventArgs(XmlSchemaException ex, XmlSeverityType severity) : base()
        {
            _ex = ex;
            _severity = severity;
        }

        public XmlSeverityType Severity
        {
            get { return _severity; }
        }

        public XmlSchemaException Exception
        {
            get { return _ex; }
        }

        /// <summary>
        /// Gets the text description corresponding to the validation error.
        /// </summary>
        public string Message
        {
            get { return _ex.Message; }
        }
    }
}
