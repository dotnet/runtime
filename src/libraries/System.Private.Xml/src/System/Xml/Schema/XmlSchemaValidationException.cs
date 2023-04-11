// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Resources;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace System.Xml.Schema
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class XmlSchemaValidationException : XmlSchemaException
    {
        private object? _sourceNodeObject;

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected XmlSchemaValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        public XmlSchemaValidationException() : base(null)
        {
        }

        public XmlSchemaValidationException(string? message) : base(message, ((Exception?)null), 0, 0)
        {
        }

        public XmlSchemaValidationException(string? message, Exception? innerException) : base(message, innerException, 0, 0)
        {
        }

        public XmlSchemaValidationException(string? message, Exception? innerException, int lineNumber, int linePosition) :
            base(message, innerException, lineNumber, linePosition)
        {
        }

        internal XmlSchemaValidationException(string? res, string? arg, string? sourceUri, int lineNumber, int linePosition) :
            base(res, new string?[] { arg }, null, sourceUri, lineNumber, linePosition, null)
        {
        }

        internal XmlSchemaValidationException(string? res, string?[]? args, string? sourceUri, int lineNumber, int linePosition) :
            base(res, args, null, sourceUri, lineNumber, linePosition, null)
        {
        }

        internal XmlSchemaValidationException(string? res, string?[]? args, Exception? innerException, string? sourceUri, int lineNumber, int linePosition) :
            base(res, args, innerException, sourceUri, lineNumber, linePosition, null)
        {
        }

        internal XmlSchemaValidationException(string? res, string?[]? args, object sourceNode) :
            base(res, args, null, null, 0, 0, null)
        {
            _sourceNodeObject = sourceNode;
        }

        public object? SourceObject
        {
            get { return _sourceNodeObject; }
        }

        protected internal void SetSourceObject(object? sourceObject)
        {
            _sourceNodeObject = sourceObject;
        }
    };
} // namespace System.Xml.Schema
