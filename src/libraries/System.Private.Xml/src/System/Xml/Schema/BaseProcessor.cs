// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Text;
using System.Diagnostics;

namespace System.Xml.Schema
{
    internal class BaseProcessor
    {
        private readonly XmlNameTable _nameTable;
        private SchemaNames? _schemaNames;
        private readonly ValidationEventHandler? _eventHandler;
        private readonly XmlSchemaCompilationSettings? _compilationSettings;
        private int _errorCount;
        private readonly string _nsXml;

        public BaseProcessor(XmlNameTable nameTable, SchemaNames? schemaNames, ValidationEventHandler? eventHandler)
            : this(nameTable, schemaNames, eventHandler, new XmlSchemaCompilationSettings())
        { } //Use the default for XmlSchemaCollection

        public BaseProcessor(XmlNameTable nameTable, SchemaNames? schemaNames, ValidationEventHandler? eventHandler, XmlSchemaCompilationSettings? compilationSettings)
        {
            Debug.Assert(nameTable != null);
            _nameTable = nameTable;
            _schemaNames = schemaNames;
            _eventHandler = eventHandler;
            _compilationSettings = compilationSettings;
            _nsXml = nameTable.Add(XmlReservedNs.NsXml);
        }

        protected XmlNameTable NameTable
        {
            get { return _nameTable; }
        }

        protected SchemaNames SchemaNames => _schemaNames ??= new SchemaNames(_nameTable);

        protected ValidationEventHandler? EventHandler
        {
            get { return _eventHandler; }
        }

        protected XmlSchemaCompilationSettings? CompilationSettings
        {
            get { return _compilationSettings; }
        }

        protected bool HasErrors
        {
            get { return _errorCount != 0; }
        }

        protected void AddToTable(XmlSchemaObjectTable table, XmlQualifiedName qname, XmlSchemaObject item)
        {
            if (qname.Name.Length == 0)
            {
                return;
            }
            XmlSchemaObject? existingObject = (XmlSchemaObject?)table[qname];

            if (existingObject != null)
            {
                if (existingObject == item)
                {
                    return;
                }
                string code = SR.Sch_DupGlobalElement;
                if (item is XmlSchemaAttributeGroup)
                {
                    string ns = _nameTable.Add(qname.Namespace);
                    if (Ref.Equal(ns, _nsXml))
                    { //Check for xml namespace
                        XmlSchema schemaForXmlNS = Preprocessor.GetBuildInSchema();
                        XmlSchemaObject? builtInAttributeGroup = schemaForXmlNS.AttributeGroups[qname];
                        if ((object)existingObject == (object?)builtInAttributeGroup)
                        {
                            table.Insert(qname, item);
                            return;
                        }
                        else if ((object)item == (object?)builtInAttributeGroup)
                        { //trying to overwrite customer's component with built-in, ignore built-in
                            return;
                        }
                    }
                    else if (IsValidAttributeGroupRedefine(existingObject, item, table))
                    { //check for redefines
                        return;
                    }
                    code = SR.Sch_DupAttributeGroup;
                }
                else if (item is XmlSchemaAttribute)
                {
                    string ns = _nameTable.Add(qname.Namespace);
                    if (Ref.Equal(ns, _nsXml))
                    {
                        XmlSchema schemaForXmlNS = Preprocessor.GetBuildInSchema();
                        XmlSchemaObject? builtInAttribute = schemaForXmlNS.Attributes[qname];
                        if ((object)existingObject == (object?)builtInAttribute)
                        { //replace built-in one
                            table.Insert(qname, item);
                            return;
                        }
                        else if ((object)item == (object?)builtInAttribute)
                        { //trying to overwrite customer's component with built-in, ignore built-in
                            return;
                        }
                    }
                    code = SR.Sch_DupGlobalAttribute;
                }
                else if (item is XmlSchemaSimpleType)
                {
                    if (IsValidTypeRedefine(existingObject, item, table))
                    {
                        return;
                    }
                    code = SR.Sch_DupSimpleType;
                }
                else if (item is XmlSchemaComplexType)
                {
                    if (IsValidTypeRedefine(existingObject, item, table))
                    {
                        return;
                    }
                    code = SR.Sch_DupComplexType;
                }
                else if (item is XmlSchemaGroup)
                {
                    if (IsValidGroupRedefine(existingObject, item, table))
                    { //check for redefines
                        return;
                    }
                    code = SR.Sch_DupGroup;
                }
                else if (item is XmlSchemaNotation)
                {
                    code = SR.Sch_DupNotation;
                }
                else if (item is XmlSchemaIdentityConstraint)
                {
                    code = SR.Sch_DupIdentityConstraint;
                }
                else
                {
                    Debug.Assert(item is XmlSchemaElement);
                }
                SendValidationEvent(code, qname.ToString(), item);
            }
            else
            {
                table.Add(qname, item);
            }
        }

        private static bool IsValidAttributeGroupRedefine(XmlSchemaObject existingObject, XmlSchemaObject item, XmlSchemaObjectTable table)
        {
            XmlSchemaAttributeGroup attGroup = (item as XmlSchemaAttributeGroup)!;
            XmlSchemaAttributeGroup existingAttGroup = (existingObject as XmlSchemaAttributeGroup)!;
            if (existingAttGroup == attGroup.Redefined)
            { //attribute group is the redefinition of existingObject
                if (existingAttGroup.AttributeUses.Count == 0)
                { //If the existing one is not already compiled, then replace.
                    table.Insert(attGroup.QualifiedName, attGroup); //Update with redefined entry
                    return true;
                }
            }
            else if (existingAttGroup.Redefined == attGroup)
            { //Redefined type already exists in the set, original type is added after redefined type, ignore the original type
                return true;
            }
            return false;
        }

        private static bool IsValidGroupRedefine(XmlSchemaObject existingObject, XmlSchemaObject item, XmlSchemaObjectTable table)
        {
            XmlSchemaGroup group = (item as XmlSchemaGroup)!;
            XmlSchemaGroup existingGroup = (existingObject as XmlSchemaGroup)!;
            if (existingGroup == group.Redefined)
            { //group is the redefinition of existingObject
                if (existingGroup.CanonicalParticle == null)
                { //If the existing one is not already compiled, then replace.
                    table.Insert(group.QualifiedName, group); //Update with redefined entry
                    return true;
                }
            }
            else if (existingGroup.Redefined == group)
            { //Redefined type already exists in the set, original type is added after redefined type, ignore the original type
                return true;
            }
            return false;
        }

        private static bool IsValidTypeRedefine(XmlSchemaObject existingObject, XmlSchemaObject item, XmlSchemaObjectTable table)
        {
            XmlSchemaType schemaType = (item as XmlSchemaType)!;
            XmlSchemaType existingType = (existingObject as XmlSchemaType)!;
            if (existingType == schemaType.Redefined)
            { //schemaType is the redefinition of existingObject
                if (existingType.ElementDecl == null)
                { //If the existing one is not already compiled, then replace.
                    table.Insert(schemaType.QualifiedName, schemaType); //Update with redefined entry
                    return true;
                }
            }
            else if (existingType.Redefined == schemaType)
            { //Redefined type already exists in the set, original type is added after redefined type, ignore the original type
                return true;
            }
            return false;
        }

        protected void SendValidationEvent(string code, XmlSchemaObject source)
        {
            SendValidationEvent(new XmlSchemaException(code, source), XmlSeverityType.Error);
        }

        protected void SendValidationEvent(string code, string? msg, XmlSchemaObject source)
        {
            SendValidationEvent(new XmlSchemaException(code, msg, source), XmlSeverityType.Error);
        }

        protected void SendValidationEvent(string code, string? msg1, string? msg2, XmlSchemaObject source)
        {
            SendValidationEvent(new XmlSchemaException(code, new string?[] { msg1, msg2 }, source), XmlSeverityType.Error);
        }

        protected void SendValidationEvent(string code, string?[]? args, Exception innerException, XmlSchemaObject source)
        {
            SendValidationEvent(new XmlSchemaException(code, args, innerException, source.SourceUri, source.LineNumber, source.LinePosition, source), XmlSeverityType.Error);
        }

        protected void SendValidationEvent(string code, string? msg1, string? msg2, string? sourceUri, int lineNumber, int linePosition)
        {
            SendValidationEvent(new XmlSchemaException(code, new string?[] { msg1, msg2 }, sourceUri, lineNumber, linePosition), XmlSeverityType.Error);
        }

        protected void SendValidationEvent(string code, XmlSchemaObject source, XmlSeverityType severity)
        {
            SendValidationEvent(new XmlSchemaException(code, source), severity);
        }

        protected void SendValidationEvent(XmlSchemaException e)
        {
            SendValidationEvent(e, XmlSeverityType.Error);
        }

        protected void SendValidationEvent(string code, string? msg, XmlSchemaObject source, XmlSeverityType severity)
        {
            SendValidationEvent(new XmlSchemaException(code, msg, source), severity);
        }

        protected void SendValidationEvent(XmlSchemaException e, XmlSeverityType severity)
        {
            if (severity == XmlSeverityType.Error)
            {
                _errorCount++;
            }
            if (_eventHandler != null)
            {
                _eventHandler(null, new ValidationEventArgs(e, severity));
            }
            else if (severity == XmlSeverityType.Error)
            {
                throw e;
            }
        }

        protected void SendValidationEventNoThrow(XmlSchemaException e, XmlSeverityType severity)
        {
            if (severity == XmlSeverityType.Error)
            {
                _errorCount++;
            }
            _eventHandler?.Invoke(null, new ValidationEventArgs(e, severity));
        }
    };
} // namespace System.Xml
