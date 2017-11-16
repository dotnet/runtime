// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Resources;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace System.Xml.Schema
{
    /// <include file='doc\XmlSchemaException.uex' path='docs/doc[@for="XmlSchemaException"]/*' />
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class XmlSchemaValidationException : XmlSchemaException
    {
        private Object _sourceNodeObject;

        /// <include file='doc\XmlSchemaException.uex' path='docs/doc[@for="XmlSchemaException.XmlSchemaException5"]/*' />
        protected XmlSchemaValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <include file='doc\XmlSchemaException.uex' path='docs/doc[@for="XmlSchemaException.GetObjectData"]/*' />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        /// <include file='doc\XmlSchemaException.uex' path='docs/doc[@for="XmlSchemaException.XmlSchemaException1"]/*' />
        public XmlSchemaValidationException() : base(null)
        {
        }

        /// <include file='doc\XmlSchemaException.uex' path='docs/doc[@for="XmlSchemaException.XmlSchemaException2"]/*' />
        public XmlSchemaValidationException(String message) : base(message, ((Exception)null), 0, 0)
        {
        }

        /// <include file='doc\XmlSchemaException.uex' path='docs/doc[@for="XmlSchemaException.XmlSchemaException0"]/*' />
        public XmlSchemaValidationException(String message, Exception innerException) : base(message, innerException, 0, 0)
        {
        }

        /// <include file='doc\XmlSchemaException.uex' path='docs/doc[@for="XmlSchemaException.XmlSchemaException3"]/*' />
        public XmlSchemaValidationException(String message, Exception innerException, int lineNumber, int linePosition) :
            base(message, innerException, lineNumber, linePosition)
        {
        }

        internal XmlSchemaValidationException(string res, string arg, string sourceUri, int lineNumber, int linePosition) :
            base(res, new string[] { arg }, null, sourceUri, lineNumber, linePosition, null)
        {
        }

        internal XmlSchemaValidationException(string res, string[] args, string sourceUri, int lineNumber, int linePosition) :
            base(res, args, null, sourceUri, lineNumber, linePosition, null)
        {
        }

        internal XmlSchemaValidationException(string res, string[] args, Exception innerException, string sourceUri, int lineNumber, int linePosition) :
            base(res, args, innerException, sourceUri, lineNumber, linePosition, null)
        {
        }

        internal XmlSchemaValidationException(string res, string[] args, object sourceNode) :
            base(res, args, null, null, 0, 0, null)
        {
            _sourceNodeObject = sourceNode;
        }

        /// <include file='doc\XmlSchemaException.uex' path='docs/doc[@for="XmlSchemaException.SourceUri"]/*' />
        public Object SourceObject
        {
            get { return _sourceNodeObject; }
        }

        protected internal void SetSourceObject(Object sourceObject)
        {
            _sourceNodeObject = sourceObject;
        }
    };
} // namespace System.Xml.Schema


