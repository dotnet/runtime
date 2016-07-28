// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    /// <include file='doc\ValidationEventArgs.uex' path='docs/doc[@for="ValidationEventArgs"]/*' />
    /// <devdoc>
    ///    Returns detailed information relating to
    ///    the ValidationEventhandler.
    /// </devdoc>
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

        /// <include file='doc\ValidationEventArgs.uex' path='docs/doc[@for="ValidationEventArgs.Severity"]/*' />
        public XmlSeverityType Severity
        {
            get { return _severity; }
        }

        /// <include file='doc\ValidationEventArgs.uex' path='docs/doc[@for="ValidationEventArgs.Exception"]/*' />
        public XmlSchemaException Exception
        {
            get { return _ex; }
        }

        /// <include file='doc\ValidationEventArgs.uex' path='docs/doc[@for="ValidationEventArgs.Message"]/*' />
        /// <devdoc>
        ///    <para>Gets the text description corresponding to the
        ///       validation error.</para>
        /// </devdoc>
        public String Message
        {
            get { return _ex.Message; }
        }
    }
}
