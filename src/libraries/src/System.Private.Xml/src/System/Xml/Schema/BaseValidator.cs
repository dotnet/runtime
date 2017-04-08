// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.IO;
    using System.Diagnostics;
    using System.Xml;
    using System.Text;
    using System.Collections;

#pragma warning disable 618

    internal class BaseValidator
    {
        private XmlSchemaCollection _schemaCollection;
        private IValidationEventHandling _eventHandling;
        private XmlNameTable _nameTable;
        private SchemaNames _schemaNames;
        private PositionInfo _positionInfo;
        private XmlResolver _xmlResolver;
        private Uri _baseUri;

        protected SchemaInfo schemaInfo;
        protected XmlValidatingReaderImpl reader;
        protected XmlQualifiedName elementName;
        protected ValidationState context;
        protected StringBuilder textValue;
        protected string textString;
        protected bool hasSibling;
        protected bool checkDatatype;

        public BaseValidator(BaseValidator other)
        {
            reader = other.reader;
            _schemaCollection = other._schemaCollection;
            _eventHandling = other._eventHandling;
            _nameTable = other._nameTable;
            _schemaNames = other._schemaNames;
            _positionInfo = other._positionInfo;
            _xmlResolver = other._xmlResolver;
            _baseUri = other._baseUri;
            elementName = other.elementName;
        }

        public BaseValidator(XmlValidatingReaderImpl reader, XmlSchemaCollection schemaCollection, IValidationEventHandling eventHandling)
        {
            Debug.Assert(schemaCollection == null || schemaCollection.NameTable == reader.NameTable);
            this.reader = reader;
            _schemaCollection = schemaCollection;
            _eventHandling = eventHandling;
            _nameTable = reader.NameTable;
            _positionInfo = PositionInfo.GetPositionInfo(reader);
            elementName = new XmlQualifiedName();
        }

        public XmlValidatingReaderImpl Reader
        {
            get { return reader; }
        }

        public XmlSchemaCollection SchemaCollection
        {
            get { return _schemaCollection; }
        }

        public XmlNameTable NameTable
        {
            get { return _nameTable; }
        }

        public SchemaNames SchemaNames
        {
            get
            {
                if (_schemaNames != null)
                {
                    return _schemaNames;
                }
                if (_schemaCollection != null)
                {
                    _schemaNames = _schemaCollection.GetSchemaNames(_nameTable);
                }
                else
                {
                    _schemaNames = new SchemaNames(_nameTable);
                }
                return _schemaNames;
            }
        }

        public PositionInfo PositionInfo
        {
            get { return _positionInfo; }
        }

        public XmlResolver XmlResolver
        {
            get { return _xmlResolver; }
            set { _xmlResolver = value; }
        }

        public Uri BaseUri
        {
            get { return _baseUri; }
            set { _baseUri = value; }
        }

        public ValidationEventHandler EventHandler
        {
            get { return (ValidationEventHandler)_eventHandling.EventHandler; }
        }

        public SchemaInfo SchemaInfo
        {
            get
            {
                return schemaInfo;
            }
            set
            {
                schemaInfo = value;
            }
        }

        public IDtdInfo DtdInfo
        {
            get
            {
                return schemaInfo;
            }
            set
            {
                SchemaInfo tmpSchemaInfo = value as SchemaInfo;
                if (tmpSchemaInfo == null)
                {
                    throw new XmlException(SR.Xml_InternalError, string.Empty);
                }
                this.schemaInfo = tmpSchemaInfo;
            }
        }

        public virtual bool PreserveWhitespace
        {
            get
            {
                return false;
            }
        }

        public virtual void Validate()
        {
        }

        public virtual void CompleteValidation()
        {
        }

        public virtual object FindId(string name)
        {
            return null;
        }

        public void ValidateText()
        {
            if (context.NeedValidateChildren)
            {
                if (context.IsNill)
                {
                    SendValidationEvent(SR.Sch_ContentInNill, XmlSchemaValidator.QNameString(context.LocalName, context.Namespace));
                    return;
                }
                ContentValidator contentValidator = context.ElementDecl.ContentValidator;
                XmlSchemaContentType contentType = contentValidator.ContentType;
                if (contentType == XmlSchemaContentType.ElementOnly)
                {
                    ArrayList names = contentValidator.ExpectedElements(context, false);
                    if (names == null)
                    {
                        SendValidationEvent(SR.Sch_InvalidTextInElement, XmlSchemaValidator.BuildElementName(context.LocalName, context.Namespace));
                    }
                    else
                    {
                        Debug.Assert(names.Count > 0);
                        SendValidationEvent(SR.Sch_InvalidTextInElementExpecting, new string[] { XmlSchemaValidator.BuildElementName(context.LocalName, context.Namespace), XmlSchemaValidator.PrintExpectedElements(names, false) });
                    }
                }
                else if (contentType == XmlSchemaContentType.Empty)
                {
                    SendValidationEvent(SR.Sch_InvalidTextInEmpty, string.Empty);
                }
                if (checkDatatype)
                {
                    SaveTextValue(reader.Value);
                }
            }
        }

        public void ValidateWhitespace()
        {
            if (context.NeedValidateChildren)
            {
                XmlSchemaContentType contentType = context.ElementDecl.ContentValidator.ContentType;
                if (context.IsNill)
                {
                    SendValidationEvent(SR.Sch_ContentInNill, XmlSchemaValidator.QNameString(context.LocalName, context.Namespace));
                }
                if (contentType == XmlSchemaContentType.Empty)
                {
                    SendValidationEvent(SR.Sch_InvalidWhitespaceInEmpty, string.Empty);
                }
                if (checkDatatype)
                {
                    SaveTextValue(reader.Value);
                }
            }
        }

        private void SaveTextValue(string value)
        {
            if (textString.Length == 0)
            {
                textString = value;
            }
            else
            {
                if (!hasSibling)
                {
                    textValue.Append(textString);
                    hasSibling = true;
                }
                textValue.Append(value);
            }
        }

        protected void SendValidationEvent(string code)
        {
            SendValidationEvent(code, string.Empty);
        }

        protected void SendValidationEvent(string code, string[] args)
        {
            SendValidationEvent(new XmlSchemaException(code, args, reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition));
        }

        protected void SendValidationEvent(string code, string arg)
        {
            SendValidationEvent(new XmlSchemaException(code, arg, reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition));
        }

        protected void SendValidationEvent(XmlSchemaException e)
        {
            SendValidationEvent(e, XmlSeverityType.Error);
        }

        protected void SendValidationEvent(string code, string msg, XmlSeverityType severity)
        {
            SendValidationEvent(new XmlSchemaException(code, msg, reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition), severity);
        }

        protected void SendValidationEvent(string code, string[] args, XmlSeverityType severity)
        {
            SendValidationEvent(new XmlSchemaException(code, args, reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition), severity);
        }

        protected void SendValidationEvent(XmlSchemaException e, XmlSeverityType severity)
        {
            if (_eventHandling != null)
            {
                _eventHandling.SendEvent(e, severity);
            }
            else if (severity == XmlSeverityType.Error)
            {
                throw e;
            }
        }

        protected static void ProcessEntity(SchemaInfo sinfo, string name, object sender, ValidationEventHandler eventhandler, string baseUri, int lineNumber, int linePosition)
        {
            SchemaEntity en;
            XmlSchemaException e = null;
            if (!sinfo.GeneralEntities.TryGetValue(new XmlQualifiedName(name), out en))
            {
                // validation error, see xml spec [68]
                e = new XmlSchemaException(SR.Sch_UndeclaredEntity, name, baseUri, lineNumber, linePosition);
            }
            else if (en.NData.IsEmpty)
            {
                e = new XmlSchemaException(SR.Sch_UnparsedEntityRef, name, baseUri, lineNumber, linePosition);
            }
            if (e != null)
            {
                if (eventhandler != null)
                {
                    eventhandler(sender, new ValidationEventArgs(e));
                }
                else
                {
                    throw e;
                }
            }
        }

        protected static void ProcessEntity(SchemaInfo sinfo, string name, IValidationEventHandling eventHandling, string baseUriStr, int lineNumber, int linePosition)
        {
            SchemaEntity en;
            string errorResId = null;
            if (!sinfo.GeneralEntities.TryGetValue(new XmlQualifiedName(name), out en))
            {
                // validation error, see xml spec [68]
                errorResId = SR.Sch_UndeclaredEntity;
            }
            else if (en.NData.IsEmpty)
            {
                errorResId = SR.Sch_UnparsedEntityRef;
            }
            if (errorResId != null)
            {
                XmlSchemaException e = new XmlSchemaException(errorResId, name, baseUriStr, lineNumber, linePosition);

                if (eventHandling != null)
                {
                    eventHandling.SendEvent(e, XmlSeverityType.Error);
                }
                else
                {
                    throw e;
                }
            }
        }

        public static BaseValidator CreateInstance(ValidationType valType, XmlValidatingReaderImpl reader, XmlSchemaCollection schemaCollection, IValidationEventHandling eventHandling, bool processIdentityConstraints)
        {
            switch (valType)
            {
                case ValidationType.XDR:
                    return new XdrValidator(reader, schemaCollection, eventHandling);

                case ValidationType.Schema:
                    return new XsdValidator(reader, schemaCollection, eventHandling);

                case ValidationType.DTD:
                    return new DtdValidator(reader, eventHandling, processIdentityConstraints);

                case ValidationType.Auto:
                    return new AutoValidator(reader, schemaCollection, eventHandling);

                case ValidationType.None:
                    return new BaseValidator(reader, schemaCollection, eventHandling);

                default:
                    break;
            }
            return null;
        }
    }
#pragma warning restore 618
}

