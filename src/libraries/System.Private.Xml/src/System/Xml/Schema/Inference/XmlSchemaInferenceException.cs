// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Resources;
using System.Runtime.Serialization;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace System.Xml.Schema
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class XmlSchemaInferenceException : XmlSchemaException
    {
        protected XmlSchemaInferenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        public XmlSchemaInferenceException() : base(null)
        {
        }

        public XmlSchemaInferenceException(String message) : base(message, ((Exception)null), 0, 0)
        {
        }

        public XmlSchemaInferenceException(String message, Exception innerException) : base(message, innerException, 0, 0)
        {
        }

        /// <include file='doc\XmlSchemaException.uex' path='docs/doc[@for="XmlSchemaException.XmlSchemaException3"]/*' />
        public XmlSchemaInferenceException(String message, Exception innerException, int lineNumber, int linePosition) :
            base(message, innerException, lineNumber, linePosition)
        {
        }

        internal XmlSchemaInferenceException(string res, string arg) : base(res, new string[] { arg }, null, null, 0, 0, null)
        {
        }

        internal XmlSchemaInferenceException(string res, int lineNumber, int linePosition) :
            base(res, null, null, null, lineNumber, linePosition, null)
        {
        }
    }
}


