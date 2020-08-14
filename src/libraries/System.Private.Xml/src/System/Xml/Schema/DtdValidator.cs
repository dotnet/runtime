// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
namespace System.Xml.Schema
{
    using System;
    using System.Collections;
    using System.Text;
    using System.IO;
    using System.Net;
    using System.Diagnostics;
    using System.Xml.Schema;
    using System.Xml.XPath;
    using System.Diagnostics.CodeAnalysis;

#pragma warning disable 618

    internal sealed class DtdValidator : BaseValidator
    {
        //required by ParseValue
        private class NamespaceManager : XmlNamespaceManager
        {
            public override string LookupNamespace(string prefix) { return prefix; }
        }

        private static readonly NamespaceManager s_namespaceManager = new NamespaceManager();
        private const int STACK_INCREMENT = 10;
        private HWStack _validationStack;  // validaton contexts
        private Hashtable _attPresence;
        private XmlQualifiedName _name = XmlQualifiedName.Empty;
        private Hashtable? _IDs;
        private IdRefNode? _idRefListHead;
        private readonly bool _processIdentityConstraints;

        internal DtdValidator(XmlValidatingReaderImpl reader, IValidationEventHandling eventHandling, bool processIdentityConstraints) : base(reader, null, eventHandling)
        {
            _processIdentityConstraints = processIdentityConstraints;
            Init();
        }

        [MemberNotNull(nameof(_validationStack))]
        [MemberNotNull(nameof(_name))]
        [MemberNotNull(nameof(_attPresence))]
        private void Init()
        {
            Debug.Assert(reader != null);
            _validationStack = new HWStack(STACK_INCREMENT);
            textValue = new StringBuilder();
            _name = XmlQualifiedName.Empty;
            _attPresence = new Hashtable();
            schemaInfo = new SchemaInfo();
            checkDatatype = false;
            Push(_name);
        }

        public override void Validate()
        {
            if (schemaInfo!.SchemaType == SchemaType.DTD)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        ValidateElement();
                        if (reader.IsEmptyElement)
                        {
                            goto case XmlNodeType.EndElement;
                        }
                        break;
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        if (MeetsStandAloneConstraint())
                        {
                            ValidateWhitespace();
                        }
                        break;
                    case XmlNodeType.ProcessingInstruction:
                    case XmlNodeType.Comment:
                        ValidatePIComment();
                        break;

                    case XmlNodeType.Text:          // text inside a node
                    case XmlNodeType.CDATA:         // <![CDATA[...]]>
                        ValidateText();
                        break;
                    case XmlNodeType.EntityReference:
                        if (!GenEntity(new XmlQualifiedName(reader.LocalName, reader.Prefix)))
                        {
                            ValidateText();
                        }
                        break;
                    case XmlNodeType.EndElement:
                        ValidateEndElement();
                        break;
                }
            }
            else
            {
                if (reader.Depth == 0 &&
                    reader.NodeType == XmlNodeType.Element)
                {
                    SendValidationEvent(SR.Xml_NoDTDPresent, _name.ToString(), XmlSeverityType.Warning);
                }
            }
        }

        private bool MeetsStandAloneConstraint()
        {
            if (reader.StandAlone &&                  // VC 1 - iv
                 context!.ElementDecl != null &&
                 context.ElementDecl.IsDeclaredInExternal &&
                 context.ElementDecl.ContentValidator!.ContentType == XmlSchemaContentType.ElementOnly)
            {
                SendValidationEvent(SR.Sch_StandAlone);
                return false;
            }
            return true;
        }

        private void ValidatePIComment()
        {
            // When validating with a dtd, empty elements should be lexically empty.
            if (context!.NeedValidateChildren)
            {
                if (context!.ElementDecl!.ContentValidator == ContentValidator.Empty)
                {
                    SendValidationEvent(SR.Sch_InvalidPIComment);
                }
            }
        }

        private void ValidateElement()
        {
            elementName.Init(reader.LocalName, reader.Prefix);
            if ((reader.Depth == 0) &&
                  (!schemaInfo!.DocTypeName.IsEmpty) &&
                  (!schemaInfo.DocTypeName.Equals(elementName)))
            { //VC 1
                SendValidationEvent(SR.Sch_RootMatchDocType);
            }
            else
            {
                ValidateChildElement();
            }
            ProcessElement();
        }

        private void ValidateChildElement()
        {
            Debug.Assert(reader.NodeType == XmlNodeType.Element);
            if (context!.NeedValidateChildren)
            { //i think i can get away with removing this if cond since won't make this call for documentelement
                int errorCode = 0;
                context.ElementDecl!.ContentValidator!.ValidateElement(elementName, context, out errorCode);
                if (errorCode < 0)
                {
                    XmlSchemaValidator.ElementValidationError(elementName, context, EventHandler, reader, reader.BaseURI, PositionInfo.LineNumber, PositionInfo.LinePosition, null);
                }
            }
        }

        private void ValidateStartElement()
        {
            if (context!.ElementDecl != null)
            {
                Reader.SchemaTypeObject = context.ElementDecl.SchemaType;

                if (Reader.IsEmptyElement && context.ElementDecl.DefaultValueTyped != null)
                {
                    Reader.TypedValueObject = context.ElementDecl.DefaultValueTyped;
                    context.IsNill = true; // reusing IsNill - what is this flag later used for??
                }
                if (context.ElementDecl.HasRequiredAttribute)
                {
                    _attPresence.Clear();
                }
            }

            if (Reader.MoveToFirstAttribute())
            {
                do
                {
                    try
                    {
                        reader.SchemaTypeObject = null;
                        SchemaAttDef? attnDef = context.ElementDecl!.GetAttDef(new XmlQualifiedName(reader.LocalName, reader.Prefix));
                        if (attnDef != null)
                        {
                            if (context.ElementDecl != null && context.ElementDecl.HasRequiredAttribute)
                            {
                                _attPresence.Add(attnDef.Name, attnDef);
                            }
                            Reader.SchemaTypeObject = attnDef.SchemaType;

                            if (attnDef.Datatype != null && !reader.IsDefault)
                            { //Since XmlTextReader adds default attributes, do not check again
                                // set typed value
                                CheckValue(Reader.Value, attnDef);
                            }
                        }
                        else
                        {
                            SendValidationEvent(SR.Sch_UndeclaredAttribute, reader.Name);
                        }
                    }
                    catch (XmlSchemaException e)
                    {
                        e.SetSource(Reader.BaseURI, PositionInfo.LineNumber, PositionInfo.LinePosition);
                        SendValidationEvent(e);
                    }
                } while (Reader.MoveToNextAttribute());
                Reader.MoveToElement();
            }
        }

        private void ValidateEndStartElement()
        {
            if (context!.ElementDecl!.HasRequiredAttribute)
            {
                try
                {
                    context.ElementDecl.CheckAttributes(_attPresence, Reader.StandAlone);
                }
                catch (XmlSchemaException e)
                {
                    e.SetSource(Reader.BaseURI, PositionInfo.LineNumber, PositionInfo.LinePosition);
                    SendValidationEvent(e);
                }
            }

            if (context.ElementDecl.Datatype != null)
            {
                checkDatatype = true;
                hasSibling = false;
                textString = string.Empty;
                textValue!.Length = 0;
            }
        }

        private void ProcessElement()
        {
            SchemaElementDecl? elementDecl = schemaInfo!.GetElementDecl(elementName);
            Push(elementName);
            if (elementDecl != null)
            {
                context!.ElementDecl = elementDecl;
                ValidateStartElement();
                ValidateEndStartElement();
                context.NeedValidateChildren = true;
                elementDecl.ContentValidator!.InitValidation(context);
            }
            else
            {
                SendValidationEvent(SR.Sch_UndeclaredElement, XmlSchemaValidator.QNameString(context!.LocalName!, context.Namespace!));
                context.ElementDecl = null;
            }
        }

        public override void CompleteValidation()
        {
            if (schemaInfo!.SchemaType == SchemaType.DTD)
            {
                do
                {
                    ValidateEndElement();
                } while (Pop());
                CheckForwardRefs();
            }
        }

        private void ValidateEndElement()
        {
            if (context!.ElementDecl != null)
            {
                if (context.NeedValidateChildren)
                {
                    if (!context.ElementDecl.ContentValidator!.CompleteValidation(context))
                    {
                        XmlSchemaValidator.CompleteValidationError(context, EventHandler, reader, reader.BaseURI, PositionInfo.LineNumber, PositionInfo.LinePosition, null);
                    }
                }

                if (checkDatatype)
                {
                    string stringValue = !hasSibling ? textString! : textValue!.ToString()!;  // only for identity-constraint exception reporting
                    CheckValue(stringValue, null);
                    checkDatatype = false;
                    textValue!.Length = 0; // cleanup
                    textString = string.Empty;
                }
            }
            Pop();
        }

        public override bool PreserveWhitespace
        {
            get { return context!.ElementDecl != null ? context.ElementDecl.ContentValidator!.PreserveWhitespace : false; }
        }


        private void ProcessTokenizedType(
            XmlTokenizedType ttype,
            string name
        )
        {
            switch (ttype)
            {
                case XmlTokenizedType.ID:
                    if (_processIdentityConstraints)
                    {
                        if (FindId(name) != null)
                        {
                            SendValidationEvent(SR.Sch_DupId, name);
                        }
                        else
                        {
                            AddID(name, context!.LocalName!);
                        }
                    }
                    break;
                case XmlTokenizedType.IDREF:
                    if (_processIdentityConstraints)
                    {
                        object? p = FindId(name);
                        if (p == null)
                        { // add it to linked list to check it later
                            _idRefListHead = new IdRefNode(_idRefListHead, name, this.PositionInfo.LineNumber, this.PositionInfo.LinePosition);
                        }
                    }

                    break;
                case XmlTokenizedType.ENTITY:
                    ProcessEntity(schemaInfo!, name, this, EventHandler, Reader.BaseURI, PositionInfo.LineNumber, PositionInfo.LinePosition);
                    break;
                default:
                    break;
            }
        }

        //check the contents of this attribute to ensure it is valid according to the specified attribute type.
        private void CheckValue(string value, SchemaAttDef? attdef)
        {
            try
            {
                reader.TypedValueObject = null;
                bool isAttn = attdef != null;
                XmlSchemaDatatype? dtype = isAttn ? attdef!.Datatype : context!.ElementDecl!.Datatype;
                if (dtype == null)
                {
                    return; // no reason to check
                }

                if (dtype.TokenizedType != XmlTokenizedType.CDATA)
                {
                    value = value.Trim();
                }

                object typedValue = dtype.ParseValue(value, NameTable, s_namespaceManager);
                reader.TypedValueObject = typedValue;
                // Check special types
                XmlTokenizedType ttype = dtype.TokenizedType;
                if (ttype == XmlTokenizedType.ENTITY || ttype == XmlTokenizedType.ID || ttype == XmlTokenizedType.IDREF)
                {
                    if (dtype.Variety == XmlSchemaDatatypeVariety.List)
                    {
                        string[] ss = (string[])typedValue;
                        for (int i = 0; i < ss.Length; ++i)
                        {
                            ProcessTokenizedType(dtype.TokenizedType, ss[i]);
                        }
                    }
                    else
                    {
                        ProcessTokenizedType(dtype.TokenizedType, (string)typedValue);
                    }
                }

                SchemaDeclBase decl = isAttn ? (SchemaDeclBase)attdef! : (SchemaDeclBase)context!.ElementDecl!;
                if (decl.Values != null && !decl.CheckEnumeration(typedValue))
                {
                    if (dtype.TokenizedType == XmlTokenizedType.NOTATION)
                    {
                        SendValidationEvent(SR.Sch_NotationValue, typedValue.ToString());
                    }
                    else
                    {
                        SendValidationEvent(SR.Sch_EnumerationValue, typedValue.ToString());
                    }
                }
                if (!decl.CheckValue(typedValue))
                {
                    if (isAttn)
                    {
                        SendValidationEvent(SR.Sch_FixedAttributeValue, attdef!.Name.ToString());
                    }
                    else
                    {
                        SendValidationEvent(SR.Sch_FixedElementValue, XmlSchemaValidator.QNameString(context!.LocalName!, context.Namespace!));
                    }
                }
            }
            catch (XmlSchemaException)
            {
                if (attdef != null)
                {
                    SendValidationEvent(SR.Sch_AttributeValueDataType, attdef.Name.ToString());
                }
                else
                {
                    SendValidationEvent(SR.Sch_ElementValueDataType, XmlSchemaValidator.QNameString(context!.LocalName!, context.Namespace!));
                }
            }
        }


        internal void AddID(string name, object node)
        {
            // Note: It used to be true that we only called this if _fValidate was true,
            // but due to the fact that you can now dynamically type somethign as an ID
            // that is no longer true.
            if (_IDs == null)
            {
                _IDs = new Hashtable();
            }

            _IDs.Add(name, node);
        }

        public override object? FindId(string name)
        {
            return _IDs == null ? null : _IDs[name];
        }

        private bool GenEntity(XmlQualifiedName qname)
        {
            string n = qname.Name;
            if (n[0] == '#')
            { // char entity reference
                return false;
            }
            else if (SchemaEntity.IsPredefinedEntity(n))
            {
                return false;
            }
            else
            {
                SchemaEntity? en = GetEntity(qname, false);
                if (en == null)
                {
                    // well-formness error, see xml spec [68]
                    throw new XmlException(SR.Xml_UndeclaredEntity, n);
                }
                if (!en.NData.IsEmpty)
                {
                    // well-formness error, see xml spec [68]
                    throw new XmlException(SR.Xml_UnparsedEntityRef, n);
                }

                if (reader.StandAlone && en.DeclaredInExternal)
                {
                    SendValidationEvent(SR.Sch_StandAlone);
                }
                return true;
            }
        }


        private SchemaEntity? GetEntity(XmlQualifiedName qname, bool fParameterEntity)
        {
            SchemaEntity? entity;
            if (fParameterEntity)
            {
                if (schemaInfo!.ParameterEntities.TryGetValue(qname, out entity))
                {
                    return entity;
                }
            }
            else
            {
                if (schemaInfo!.GeneralEntities.TryGetValue(qname, out entity))
                {
                    return entity;
                }
            }
            return null;
        }

        private void CheckForwardRefs()
        {
            IdRefNode? next = _idRefListHead;
            while (next != null)
            {
                if (FindId(next.Id) == null)
                {
                    SendValidationEvent(new XmlSchemaException(SR.Sch_UndeclaredId, next.Id, reader.BaseURI, next.LineNo, next.LinePos));
                }
                IdRefNode? ptr = next.Next;
                next.Next = null; // unhook each object so it is cleaned up by Garbage Collector
                next = ptr;
            }
            // not needed any more.
            _idRefListHead = null;
        }

        private void Push(XmlQualifiedName elementName)
        {
            context = (ValidationState)_validationStack.Push();
            if (context == null)
            {
                context = new ValidationState();
                _validationStack.AddToTop(context);
            }
            context.LocalName = elementName.Name;
            context.Namespace = elementName.Namespace;
            context.HasMatched = false;
            context.IsNill = false;
            context.NeedValidateChildren = false;
        }

        private bool Pop()
        {
            if (_validationStack.Length > 1)
            {
                _validationStack.Pop();
                context = (ValidationState?)_validationStack.Peek();
                return true;
            }
            return false;
        }

        public static void SetDefaultTypedValue(
            SchemaAttDef attdef,
            IDtdParserAdapter readerAdapter
        )
        {
            try
            {
                string value = attdef.DefaultValueExpanded;
                XmlSchemaDatatype dtype = attdef.Datatype;
                if (dtype == null)
                {
                    return; // no reason to check
                }
                if (dtype.TokenizedType != XmlTokenizedType.CDATA)
                {
                    value = value.Trim();
                }
                attdef.DefaultValueTyped = dtype.ParseValue(value, readerAdapter.NameTable, readerAdapter.NamespaceResolver);
            }
#if DEBUG
            catch (XmlSchemaException)
#else
            catch (Exception)
#endif
            {
                IValidationEventHandling? eventHandling = ((IDtdParserAdapterWithValidation)readerAdapter).ValidationEventHandling;
                if (eventHandling != null)
                {
                    XmlSchemaException e = new XmlSchemaException(SR.Sch_AttributeDefaultDataType, attdef.Name.ToString());
                    eventHandling.SendEvent(e, XmlSeverityType.Error);
                }
            }
        }

        public static void CheckDefaultValue(
            SchemaAttDef attdef,
            SchemaInfo sinfo,
            IValidationEventHandling eventHandling,
            string? baseUriStr
        )
        {
            try
            {
                if (baseUriStr == null)
                {
                    baseUriStr = string.Empty;
                }
                XmlSchemaDatatype dtype = attdef.Datatype;
                if (dtype == null)
                {
                    return; // no reason to check
                }
                object? typedValue = attdef.DefaultValueTyped;

                // Check special types
                XmlTokenizedType ttype = dtype.TokenizedType;
                if (ttype == XmlTokenizedType.ENTITY)
                {
                    if (dtype.Variety == XmlSchemaDatatypeVariety.List)
                    {
                        string[] ss = (string[])typedValue!;
                        for (int i = 0; i < ss.Length; ++i)
                        {
                            ProcessEntity(sinfo, ss[i], eventHandling, baseUriStr, attdef.ValueLineNumber, attdef.ValueLinePosition);
                        }
                    }
                    else
                    {
                        ProcessEntity(sinfo, (string)typedValue!, eventHandling, baseUriStr, attdef.ValueLineNumber, attdef.ValueLinePosition);
                    }
                }
                else if (ttype == XmlTokenizedType.ENUMERATION)
                {
                    if (!attdef.CheckEnumeration(typedValue!))
                    {
                        if (eventHandling != null)
                        {
                            XmlSchemaException e = new XmlSchemaException(SR.Sch_EnumerationValue, typedValue!.ToString(), baseUriStr, attdef.ValueLineNumber, attdef.ValueLinePosition);
                            eventHandling.SendEvent(e, XmlSeverityType.Error);
                        }
                    }
                }
            }
#if DEBUG
            catch (XmlSchemaException)
#else
            catch (Exception)
#endif
            {

                if (eventHandling != null)
                {
                    XmlSchemaException e = new XmlSchemaException(SR.Sch_AttributeDefaultDataType, attdef.Name.ToString());
                    eventHandling.SendEvent(e, XmlSeverityType.Error);
                }
            }
        }
    }
#pragma warning restore 618
}
