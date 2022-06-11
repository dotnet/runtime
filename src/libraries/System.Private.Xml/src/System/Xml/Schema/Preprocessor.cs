// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.IO;
    using System.Threading;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Runtime.Versioning;
    using System.Reflection;

    internal enum Compositor
    {
        Root,
        Include,
        Import,
        Redefine
    };

    internal sealed class RedefineEntry
    {
        internal XmlSchemaRedefine redefine;
        internal XmlSchema schemaToUpdate;

        public RedefineEntry(XmlSchemaRedefine external, XmlSchema schema)
        {
            redefine = external;
            schemaToUpdate = schema;
        }
    }

    internal sealed class Preprocessor : BaseProcessor
    {
        private string? _xmlns;
        private string? _nsXsi;
        private string? _targetNamespace;

        private XmlSchema? _rootSchema;
        private XmlSchema? _currentSchema;

        private XmlSchemaForm _elementFormDefault;
        private XmlSchemaForm _attributeFormDefault;
        private XmlSchemaDerivationMethod _blockDefault;
        private XmlSchemaDerivationMethod _finalDefault;

        /*Dictionary<Uri, XmlSchema> schemaLocations;
        Dictionary<ChameleonKey, XmlSchema> chameleonSchemas;*/
        private Hashtable? _schemaLocations;
        private Hashtable? _chameleonSchemas;

        private readonly Hashtable _referenceNamespaces;
        private readonly Hashtable _processedExternals;
        private readonly SortedList _lockList;

        private XmlReaderSettings? _readerSettings;

        // For redefines
        private XmlSchema? _rootSchemaForRedefine;
        private ArrayList? _redefinedList;

        private static XmlSchema? s_builtInSchemaForXmlNS;

        private const XmlSchemaDerivationMethod schemaBlockDefaultAllowed = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Substitution;
        private const XmlSchemaDerivationMethod schemaFinalDefaultAllowed = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union;
        private const XmlSchemaDerivationMethod elementBlockAllowed = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Substitution;
        private const XmlSchemaDerivationMethod elementFinalAllowed = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension;
        private const XmlSchemaDerivationMethod simpleTypeFinalAllowed = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union;
        private const XmlSchemaDerivationMethod complexTypeBlockAllowed = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension;
        private const XmlSchemaDerivationMethod complexTypeFinalAllowed = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension;

        private XmlResolver? _xmlResolver;

        public Preprocessor(XmlNameTable nameTable, SchemaNames schemaNames, ValidationEventHandler? eventHandler)
            : this(nameTable, schemaNames, eventHandler, new XmlSchemaCompilationSettings())
        { }

        public Preprocessor(XmlNameTable nameTable, SchemaNames schemaNames, ValidationEventHandler? eventHandler, XmlSchemaCompilationSettings compilationSettings)
            : base(nameTable, schemaNames, eventHandler, compilationSettings)
        {
            _referenceNamespaces = new Hashtable();
            _processedExternals = new Hashtable();
            _lockList = new SortedList();
        }

        public bool Execute(XmlSchema schema, string? targetNamespace, bool loadExternals)
        {
            _rootSchema = schema; // Need to lock main schema here
            _xmlns = NameTable.Add("xmlns");
            _nsXsi = NameTable.Add(XmlReservedNs.NsXsi);

            _rootSchema.ImportedSchemas.Clear();
            _rootSchema.ImportedNamespaces.Clear();

            //Add root schema to the schemaLocations table
            if (_rootSchema.BaseUri != null)
            {
                if (_schemaLocations![_rootSchema.BaseUri] == null)
                {
                    _schemaLocations.Add(_rootSchema.BaseUri, _rootSchema);
                }
            }
            //Check targetNamespace for rootSchema
            if (_rootSchema.TargetNamespace != null)
            {
                if (targetNamespace == null)
                {
                    targetNamespace = _rootSchema.TargetNamespace;
                }
                else if (targetNamespace != _rootSchema.TargetNamespace)
                {
                    SendValidationEvent(SR.Sch_MismatchTargetNamespaceEx, targetNamespace, _rootSchema.TargetNamespace, _rootSchema);
                }
            }
            else if (targetNamespace != null && targetNamespace.Length != 0)
            { //if schema.TargetNamespace == null & targetNamespace != null, we will force the schema components into targetNamespace
                _rootSchema = GetChameleonSchema(targetNamespace, _rootSchema); //Chameleon include at top-level
            }
            if (loadExternals && _xmlResolver != null)
            {
                LoadExternals(_rootSchema);
            }

            BuildSchemaList(_rootSchema);
            int schemaIndex = 0;
            XmlSchema listSchema;
            try
            {
                //Acquire locks on all schema objects; Need to lock only on pre-created schemas and not parsed schemas
                for (schemaIndex = 0; schemaIndex < _lockList.Count; schemaIndex++)
                {
                    listSchema = (XmlSchema)_lockList.GetByIndex(schemaIndex)!;
                    Monitor.Enter(listSchema);
                    listSchema.IsProcessing = false; //Reset processing flag from LoadExternals
                }
                //Preprocess
                _rootSchemaForRedefine = _rootSchema;
                Preprocess(_rootSchema, targetNamespace, _rootSchema.ImportedSchemas);
                if (_redefinedList != null)
                { //If there were redefines
                    for (int i = 0; i < _redefinedList.Count; ++i)
                    {
                        PreprocessRedefine((RedefineEntry)_redefinedList[i]!);
                    }
                }
            }
            finally
            { //Releasing locks in finally block
                if (schemaIndex == _lockList.Count)
                {
                    schemaIndex--;
                }

                for (; schemaIndex >= 0; schemaIndex--)
                {
                    listSchema = (XmlSchema)_lockList.GetByIndex(schemaIndex)!;
                    listSchema.IsProcessing = false; //Reset processing flag from Preprocess
                    if (listSchema == Preprocessor.GetBuildInSchema())
                    { //dont re-set compiled flags for xml namespace schema
                        Monitor.Exit(listSchema);
                        continue;
                    }
                    listSchema.IsCompiledBySet = false;
                    listSchema.IsPreprocessed = !HasErrors;
                    Monitor.Exit(listSchema); //Release locks on all schema objects
                }
            }
            _rootSchema.IsPreprocessed = !HasErrors; //For chameleon at top-level
            return !HasErrors;
        }

        private static void Cleanup(XmlSchema schema)
        {
            if (schema == Preprocessor.GetBuildInSchema())
            {
                return;
            }
            schema.Attributes.Clear();
            schema.AttributeGroups.Clear();
            schema.SchemaTypes.Clear();
            schema.Elements.Clear();
            schema.Groups.Clear();
            schema.Notations.Clear();
            schema.Ids.Clear();
            schema.IdentityConstraints.Clear();
            schema.IsRedefined = false;
            schema.IsCompiledBySet = false;
        }

        private static void CleanupRedefine(XmlSchemaExternal include)
        {
            XmlSchemaRedefine rdef = (include as XmlSchemaRedefine)!;
            rdef.AttributeGroups.Clear();
            rdef.Groups.Clear();
            rdef.SchemaTypes.Clear();
        }

        internal XmlResolver? XmlResolver
        {
            set
            {
                _xmlResolver = value;
            }
        }

        internal XmlReaderSettings ReaderSettings
        {
            get
            {
                if (_readerSettings == null)
                {
                    _readerSettings = new XmlReaderSettings();
                    _readerSettings.DtdProcessing = DtdProcessing.Prohibit;
                }
                return _readerSettings;
            }
            set
            {
                _readerSettings = value;
            }
        }

        //internal Dictionary<Uri, XmlSchema> SchemaLocations {
        internal Hashtable SchemaLocations
        {
            set
            {
                _schemaLocations = value;
            }
        }


        //internal Dictionary<ChameleonKey, XmlSchema> ChameleonSchemas {
        internal Hashtable ChameleonSchemas
        {
            set
            {
                _chameleonSchemas = value;
            }
        }

        internal XmlSchema? RootSchema
        {
            get
            {
                return _rootSchema; //This is required to get back a cloned chameleon
            }
        }

        private void BuildSchemaList(XmlSchema schema)
        {
            if (_lockList.Contains(schema.SchemaId))
            {
                return;
            }
            _lockList.Add(schema.SchemaId, schema);
            for (int i = 0; i < schema.Includes.Count; ++i)
            {
                XmlSchemaExternal ext = (XmlSchemaExternal)schema.Includes[i];
                if (ext.Schema != null)
                {
                    BuildSchemaList(ext.Schema);
                }
            }
        }

        // SxS: This method uses resource names read from source document and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        private void LoadExternals(XmlSchema schema)
        {
            if (schema.IsProcessing)
            {
                return;
            }
            schema.IsProcessing = true;
            for (int i = 0; i < schema.Includes.Count; ++i)
            {
                Uri? includeLocation = null;
                //CASE 1: If the Schema object of the include has been set
                XmlSchemaExternal include = (XmlSchemaExternal)schema.Includes[i];
                XmlSchema? includedSchema = include.Schema;
                if (includedSchema != null)
                {
                    // already loaded
                    includeLocation = includedSchema.BaseUri;
                    if (includeLocation != null && _schemaLocations![includeLocation] == null)
                    {
                        _schemaLocations.Add(includeLocation, includedSchema);
                    }
                    LoadExternals(includedSchema);
                    continue;
                }

                //CASE 2: Try & Parse schema from the provided location
                string? schemaLocation = include.SchemaLocation;
                Uri? ruri = null;
                Exception? innerException = null;
                if (schemaLocation != null)
                {
                    try
                    {
                        ruri = ResolveSchemaLocationUri(schema, schemaLocation);
                    }
                    catch (Exception e)
                    {
                        ruri = null;
                        innerException = e;
                    }
                }

                if (include.Compositor == Compositor.Import)
                {
                    XmlSchemaImport import = (include as XmlSchemaImport)!;
                    Debug.Assert(import != null);
                    string importNS = import.Namespace ?? string.Empty;
                    if (!schema.ImportedNamespaces.Contains(importNS))
                    {
                        schema.ImportedNamespaces.Add(importNS);
                    }
                    //CASE 2.1: If the imported namespace is the XML namespace,
                    // If the parent schemaSet already has schema for XML ns loaded, use that
                    // Else if the location is null use the built-in one
                    // else go through regular processing of parsing from location
                    if (importNS == XmlReservedNs.NsXml)
                    {
                        if (ruri == null)
                        { //Resolved location is null, hence get the built-in one
                            include.Schema = Preprocessor.GetBuildInSchema();
                            continue;
                        }
                    }
                }

                //CASE 3: Parse schema from the provided location
                if (ruri == null)
                {
                    if (schemaLocation != null)
                    {
                        SendValidationEvent(new XmlSchemaException(SR.Sch_InvalidIncludeLocation, null, innerException, include.SourceUri, include.LineNumber, include.LinePosition, include), XmlSeverityType.Warning);
                    }
                    continue;
                }

                if (_schemaLocations![ruri] == null)
                { // Only if location already not processed
                    object? obj;
                    try
                    {
                        obj = GetSchemaEntity(ruri);
                    }
                    catch (Exception eInner)
                    {
                        innerException = eInner;
                        obj = null;
                    }

                    if (obj != null)
                    {
                        include.BaseUri = ruri;
                        Type returnType = obj.GetType();
                        if (typeof(XmlSchema).IsAssignableFrom(returnType))
                        { //To handle XmlSchema and all its derived types
                            include.Schema = (XmlSchema)obj;
                            _schemaLocations.Add(ruri, include.Schema);
                            LoadExternals(include.Schema);
                        }
                        else
                        {
                            XmlReader? reader = null;
                            if (returnType.IsSubclassOf(typeof(Stream)))
                            {
                                _readerSettings!.CloseInput = true;
                                _readerSettings.XmlResolver = _xmlResolver;
                                reader = XmlReader.Create((Stream)obj, _readerSettings, ruri.ToString());
                            }
                            else if (returnType.IsSubclassOf(typeof(XmlReader)))
                            {
                                reader = (XmlReader)obj;
                            }
                            else if (returnType.IsSubclassOf(typeof(TextReader)))
                            {
                                _readerSettings!.CloseInput = true;
                                _readerSettings.XmlResolver = _xmlResolver;
                                reader = XmlReader.Create((TextReader)obj, _readerSettings, ruri.ToString());
                            }

                            if (reader == null)
                            {
                                SendValidationEvent(SR.Sch_InvalidIncludeLocation, include, XmlSeverityType.Warning);
                                continue;
                            }

                            try
                            {
                                Parser parser = new Parser(SchemaType.XSD, NameTable, SchemaNames, EventHandler);
                                parser.Parse(reader, null);
                                while (reader.Read()) ; // wellformness check
                                includedSchema = parser.XmlSchema;
                                include.Schema = includedSchema;
                                _schemaLocations.Add(ruri, includedSchema);
                                LoadExternals(includedSchema!);
                            }
                            catch (XmlSchemaException e)
                            {
                                SendValidationEvent(SR.Sch_CannotLoadSchemaLocation, schemaLocation, e.Message, e.SourceUri, e.LineNumber, e.LinePosition);
                            }
                            catch (Exception eInner)
                            {
                                SendValidationEvent(new XmlSchemaException(SR.Sch_InvalidIncludeLocation, null, eInner, include.SourceUri, include.LineNumber, include.LinePosition, include), XmlSeverityType.Warning);
                            }
                            finally
                            {
                                reader.Close();
                            }
                        }
                    }
                    else
                    {
                        SendValidationEvent(new XmlSchemaException(SR.Sch_InvalidIncludeLocation, null, innerException, include.SourceUri, include.LineNumber, include.LinePosition, include), XmlSeverityType.Warning);
                    }
                }
                else
                {
                    // Location already in table and now seeing duplicate import / include
                    include.Schema = (XmlSchema)_schemaLocations[ruri]!; // Set schema object even for duplicates
                }
            }
        }


        internal static XmlSchema GetBuildInSchema()
        {
            if (s_builtInSchemaForXmlNS == null)
            {
                XmlSchema tempSchema = new XmlSchema();
                tempSchema.TargetNamespace = XmlReservedNs.NsXml;
                tempSchema.Namespaces.Add("xml", XmlReservedNs.NsXml);

                XmlSchemaAttribute lang = new XmlSchemaAttribute();
                lang.Name = "lang";
                lang.SchemaTypeName = new XmlQualifiedName("language", XmlReservedNs.NsXs);
                tempSchema.Items.Add(lang);

                XmlSchemaAttribute xmlbase = new XmlSchemaAttribute();
                xmlbase.Name = "base";
                xmlbase.SchemaTypeName = new XmlQualifiedName("anyURI", XmlReservedNs.NsXs);
                tempSchema.Items.Add(xmlbase);

                XmlSchemaAttribute space = new XmlSchemaAttribute();
                space.Name = "space";
                XmlSchemaSimpleType type = new XmlSchemaSimpleType();
                XmlSchemaSimpleTypeRestriction r = new XmlSchemaSimpleTypeRestriction();
                r.BaseTypeName = new XmlQualifiedName("NCName", XmlReservedNs.NsXs);
                XmlSchemaEnumerationFacet space_default = new XmlSchemaEnumerationFacet();
                space_default.Value = "default";
                r.Facets.Add(space_default);
                XmlSchemaEnumerationFacet space_preserve = new XmlSchemaEnumerationFacet();
                space_preserve.Value = "preserve";
                r.Facets.Add(space_preserve);
                type.Content = r;
                space.SchemaType = type;
                space.DefaultValue = "preserve";
                tempSchema.Items.Add(space);

                XmlSchemaAttributeGroup attributeGroup = new XmlSchemaAttributeGroup();
                attributeGroup.Name = "specialAttrs";
                XmlSchemaAttribute langRef = new XmlSchemaAttribute();
                langRef.RefName = new XmlQualifiedName("lang", XmlReservedNs.NsXml);
                attributeGroup.Attributes.Add(langRef);
                XmlSchemaAttribute spaceRef = new XmlSchemaAttribute();
                spaceRef.RefName = new XmlQualifiedName("space", XmlReservedNs.NsXml);
                attributeGroup.Attributes.Add(spaceRef);
                XmlSchemaAttribute baseRef = new XmlSchemaAttribute();
                baseRef.RefName = new XmlQualifiedName("base", XmlReservedNs.NsXml);
                attributeGroup.Attributes.Add(baseRef);
                tempSchema.Items.Add(attributeGroup);
                tempSchema.IsPreprocessed = true;
                tempSchema.CompileSchemaInSet(new NameTable(), null, null); //compile built-in schema

                Interlocked.CompareExchange<XmlSchema?>(ref s_builtInSchemaForXmlNS, tempSchema, null);
            }
            return s_builtInSchemaForXmlNS;
        }

        private void BuildRefNamespaces(XmlSchema schema)
        {
            _referenceNamespaces.Clear();
            XmlSchemaImport import;
            string? ns;

            //Add XSD namespace
            _referenceNamespaces.Add(XmlReservedNs.NsXs, XmlReservedNs.NsXs);

            for (int i = 0; i < schema.Includes.Count; ++i)
            {
                XmlSchemaExternal include = (XmlSchemaExternal)schema.Includes[i];
                if (include is XmlSchemaImport)
                {
                    import = (include as XmlSchemaImport)!;
                    ns = import.Namespace;
                    if (ns == null)
                    {
                        ns = string.Empty;
                    }
                    if (_referenceNamespaces[ns] == null)
                        _referenceNamespaces.Add(ns, ns);
                }
            }

            //Add the schema's targetnamespace
            string? tns = schema.TargetNamespace;
            if (tns == null)
            {
                tns = string.Empty;
            }
            if (_referenceNamespaces[tns] == null)
            {
                _referenceNamespaces.Add(tns, tns);
            }
        }

        private void ParseUri(string uri, string code, XmlSchemaObject sourceSchemaObject)
        {
            try
            {
                XmlConvert.ToUri(uri);  // can throw
            }
            catch (FormatException eInner)
            {
                SendValidationEvent(code, new string[] { uri }, eInner, sourceSchemaObject);
            }
        }

        private void Preprocess(XmlSchema schema, string? targetNamespace, ArrayList imports)
        {
            XmlSchema? prevRootSchemaForRedefine;
            if (schema.IsProcessing)
            {
                return;
            }
            schema.IsProcessing = true;

            string? tns = schema.TargetNamespace;
            if (tns != null)
            {
                schema.TargetNamespace = tns = NameTable.Add(tns);
                if (tns.Length == 0)
                {
                    SendValidationEvent(SR.Sch_InvalidTargetNamespaceAttribute, schema);
                }
                else
                {
                    ParseUri(tns, SR.Sch_InvalidNamespace, schema);
                }
            }
            if (schema.Version != null)
            {
                XmlSchemaDatatype tokenDt = DatatypeImplementation.GetSimpleTypeFromTypeCode(XmlTypeCode.Token).Datatype!;
                object? version;
                Exception? exception = tokenDt.TryParseValue(schema.Version, null, null, out version);
                if (exception != null)
                {
                    SendValidationEvent(SR.Sch_AttributeValueDataTypeDetailed, new string[] { "version", schema.Version, tokenDt.TypeCodeString, exception.Message }, exception, schema);
                }
                else
                {
                    schema.Version = (string)version!;
                }
            }

            //Begin processing the schema after checking targetNamespace and verifying chameleon
            Cleanup(schema);

            for (int i = 0; i < schema.Includes.Count; ++i)
            {
                XmlSchemaExternal include = (XmlSchemaExternal)schema.Includes[i];
                XmlSchema? externalSchema = include.Schema;
                SetParent(include, schema);
                PreprocessAnnotation(include);
                string? loc = include.SchemaLocation;
                if (loc != null)
                {
                    ParseUri(loc, SR.Sch_InvalidSchemaLocation, include);
                }
                else if ((include.Compositor == Compositor.Include || include.Compositor == Compositor.Redefine) && externalSchema == null)
                {
                    SendValidationEvent(SR.Sch_MissRequiredAttribute, "schemaLocation", include);
                }

                switch (include.Compositor)
                {
                    case Compositor.Import:
                        XmlSchemaImport import = (include as XmlSchemaImport)!;
                        string? importNS = import.Namespace;
                        if (importNS == schema.TargetNamespace)
                        {
                            SendValidationEvent(SR.Sch_ImportTargetNamespace, include);
                        }
                        if (externalSchema != null)
                        {
                            if (importNS != externalSchema.TargetNamespace)
                            {
                                SendValidationEvent(SR.Sch_MismatchTargetNamespaceImport, importNS, externalSchema.TargetNamespace, import);
                            }
                            //SetParent(externalSchema, import);
                            prevRootSchemaForRedefine = _rootSchemaForRedefine;
                            _rootSchemaForRedefine = externalSchema; //Make the imported schema the root schema for redefines
                            Preprocess(externalSchema, importNS, imports);
                            _rootSchemaForRedefine = prevRootSchemaForRedefine; //Reset the root schema for redefines
                        }
                        else
                        {
                            if (importNS != null)
                            {
                                if (importNS.Length == 0)
                                {
                                    SendValidationEvent(SR.Sch_InvalidNamespaceAttribute, importNS, include);
                                }
                                else
                                {
                                    ParseUri(importNS, SR.Sch_InvalidNamespace, include);
                                }
                            }
                        }
                        break;
                    case Compositor.Include:
                        XmlSchema? includedSchema = include.Schema;
                        if (includedSchema != null)
                        {
                            //SetParent(includedSchema, include);
                            goto default;
                        }
                        break;

                    case Compositor.Redefine:
                        if (externalSchema != null)
                        {
                            //SetParent(externalSchema, include);
                            CleanupRedefine(include);
                            goto default;
                        }
                        break;

                    default: //For include, redefine common case
                        if (externalSchema!.TargetNamespace != null)
                        {
                            if (schema.TargetNamespace != externalSchema.TargetNamespace)
                            { //namespaces for includes should be the same
                                SendValidationEvent(SR.Sch_MismatchTargetNamespaceInclude, externalSchema.TargetNamespace, schema.TargetNamespace, include);
                            }
                        }
                        else if (targetNamespace != null && targetNamespace.Length != 0)
                        { //Chameleon redefine
                            externalSchema = GetChameleonSchema(targetNamespace, externalSchema);
                            include.Schema = externalSchema; //Reset the schema property to the cloned schema
                        }
                        Preprocess(externalSchema, schema.TargetNamespace, imports);
                        break;
                }
            }

            //Begin processing the current schema passed to preprocess
            //Build the namespaces that can be referenced in the current schema
            _currentSchema = schema;
            BuildRefNamespaces(schema);
            ValidateIdAttribute(schema);

            _targetNamespace = targetNamespace ?? string.Empty;

            SetSchemaDefaults(schema);

            _processedExternals.Clear();
            XmlSchemaExternal external;
            for (int i = 0; i < schema.Includes.Count; i++)
            {
                external = (XmlSchemaExternal)schema.Includes[i];
                XmlSchema? includedSchema = external.Schema;
                if (includedSchema != null)
                {
                    switch (external.Compositor)
                    {
                        case Compositor.Include:
                            if (_processedExternals[includedSchema] != null)
                            {
                                continue; //Already processed this included schema;
                            }
                            _processedExternals.Add(includedSchema, external);
                            CopyIncludedComponents(includedSchema, schema);
                            break;

                        case Compositor.Redefine:
                            if (_redefinedList == null)
                            {
                                _redefinedList = new ArrayList();
                            }

                            _redefinedList.Add(new RedefineEntry((external as XmlSchemaRedefine)!, _rootSchemaForRedefine!));
                            if (_processedExternals[includedSchema] != null)
                            {
                                continue; // Already processed this included schema;
                            }

                            _processedExternals.Add(includedSchema, external);
                            CopyIncludedComponents(includedSchema, schema);
                            break;

                        case Compositor.Import:
                            if (includedSchema != _rootSchema)
                            {
                                XmlSchemaImport import = (external as XmlSchemaImport)!;
                                string importNS = import.Namespace ?? string.Empty;
                                if (!imports.Contains(includedSchema))
                                {
                                    imports.Add(includedSchema);
                                }
                                if (!_rootSchema!.ImportedNamespaces.Contains(importNS))
                                {
                                    _rootSchema.ImportedNamespaces.Add(importNS);
                                }
                            }
                            break;

                        default:
                            Debug.Fail($"Unexpected compositor {external.Compositor}");
                            break;
                    }
                }
                else if (external.Compositor == Compositor.Redefine)
                {
                    XmlSchemaRedefine redefine = (external as XmlSchemaRedefine)!;
                    if (redefine.BaseUri == null)
                    {
                        for (int j = 0; j < redefine.Items.Count; ++j)
                        {
                            if (!(redefine.Items[j] is XmlSchemaAnnotation))
                            {
                                SendValidationEvent(SR.Sch_RedefineNoSchema, redefine);
                                break;
                            }
                        }
                    }
                }

                ValidateIdAttribute(external);
            }

            List<XmlSchemaObject> removeItemsList = new List<XmlSchemaObject>();
            XmlSchemaObjectCollection schemaItems = schema.Items;
            for (int i = 0; i < schemaItems.Count; ++i)
            {
                SetParent(schemaItems[i], schema);
                if (schemaItems[i] is XmlSchemaAttribute attribute)
                {
                    PreprocessAttribute(attribute);
                    AddToTable(schema.Attributes, attribute.QualifiedName, attribute);
                }
                else if (schemaItems[i] is XmlSchemaAttributeGroup attributeGroup)
                {
                    PreprocessAttributeGroup(attributeGroup);
                    AddToTable(schema.AttributeGroups, attributeGroup.QualifiedName, attributeGroup);
                }
                else if (schemaItems[i] is XmlSchemaComplexType complexType)
                {
                    PreprocessComplexType(complexType, false);
                    AddToTable(schema.SchemaTypes, complexType.QualifiedName, complexType);
                }
                else if (schemaItems[i] is XmlSchemaSimpleType simpleType)
                {
                    PreprocessSimpleType(simpleType, false);
                    AddToTable(schema.SchemaTypes, simpleType.QualifiedName, simpleType);
                }
                else if (schemaItems[i] is XmlSchemaElement element)
                {
                    PreprocessElement(element);
                    AddToTable(schema.Elements, element.QualifiedName, element);
                }
                else if (schemaItems[i] is XmlSchemaGroup group)
                {
                    PreprocessGroup(group);
                    AddToTable(schema.Groups, group.QualifiedName, group);
                }
                else if (schemaItems[i] is XmlSchemaNotation notation)
                {
                    PreprocessNotation(notation);
                    AddToTable(schema.Notations, notation.QualifiedName, notation);
                }
                else if (schemaItems[i] is XmlSchemaAnnotation schemaAnnotation)
                {
                    PreprocessAnnotation(schemaAnnotation);
                }
                else
                {
                    SendValidationEvent(SR.Sch_InvalidCollection, (XmlSchemaObject)schemaItems[i]);
                    removeItemsList.Add(schemaItems[i]);
                }
            }

            for (int i = 0; i < removeItemsList.Count; ++i)
            {
                schema.Items.Remove(removeItemsList[i]);
            }
        }

        private void CopyIncludedComponents(XmlSchema includedSchema, XmlSchema schema)
        {
            foreach (XmlSchemaElement? element in includedSchema.Elements.Values)
            {
                AddToTable(schema.Elements, element!.QualifiedName, element);
            }

            foreach (XmlSchemaAttribute? attribute in includedSchema.Attributes.Values)
            {
                AddToTable(schema.Attributes, attribute!.QualifiedName, attribute);
            }

            foreach (XmlSchemaGroup? group in includedSchema.Groups.Values)
            {
                AddToTable(schema.Groups, group!.QualifiedName, group);
            }

            foreach (XmlSchemaAttributeGroup? attributeGroup in includedSchema.AttributeGroups.Values)
            {
                AddToTable(schema.AttributeGroups, attributeGroup!.QualifiedName, attributeGroup);
            }

            foreach (XmlSchemaType? type in includedSchema.SchemaTypes.Values)
            {
                AddToTable(schema.SchemaTypes, type!.QualifiedName, type);
            }

            foreach (XmlSchemaNotation? notation in includedSchema.Notations.Values)
            {
                AddToTable(schema.Notations, notation!.QualifiedName, notation);
            }
        }

        private void PreprocessRedefine(RedefineEntry redefineEntry)
        {
            XmlSchemaRedefine redefine = redefineEntry.redefine;
            XmlSchema originalSchema = redefine.Schema!;

            _currentSchema = GetParentSchema(redefine)!; // Set this for correct schema context in ValidateIdAttribute & ValidateQNameAttribute for redefines
            Debug.Assert(_currentSchema != null);
            SetSchemaDefaults(_currentSchema);

            if (originalSchema.IsRedefined)
            {
                SendValidationEvent(SR.Sch_MultipleRedefine, redefine, XmlSeverityType.Warning);
                return;
            }
            originalSchema.IsRedefined = true;

            XmlSchema schemaToUpdate = redefineEntry.schemaToUpdate;
            ArrayList includesOfRedefine = new ArrayList();
            GetIncludedSet(originalSchema, includesOfRedefine);
            string targetNS = schemaToUpdate.TargetNamespace ?? string.Empty;

            XmlSchemaObjectCollection items = redefine.Items;
            for (int i = 0; i < items.Count; ++i)
            {
                SetParent(items[i], redefine);
                XmlSchemaGroup? group = items[i] as XmlSchemaGroup;
                if (group != null)
                {
                    PreprocessGroup(group);
                    group.QualifiedName.SetNamespace(targetNS); //Since PreprocessGroup will use this.targetNamespace and that will be that of the root schema's
                    if (redefine.Groups[group.QualifiedName] != null)
                    {
                        SendValidationEvent(SR.Sch_GroupDoubleRedefine, group);
                    }
                    else
                    {
                        AddToTable(redefine.Groups, group.QualifiedName, group);
                        XmlSchemaGroup? originalGroup = (XmlSchemaGroup?)schemaToUpdate.Groups[group.QualifiedName];
                        XmlSchema? parentSchema = GetParentSchema(originalGroup);
                        if (originalGroup == null || (parentSchema != originalSchema && !includesOfRedefine.Contains(parentSchema)))
                        {
                            SendValidationEvent(SR.Sch_ComponentRedefineNotFound, "<group>", group.QualifiedName.ToString(), group);
                        }
                        else
                        {
                            group.Redefined = originalGroup;
                            schemaToUpdate.Groups.Insert(group.QualifiedName, group);
                            CheckRefinedGroup(group);
                        }
                    }
                }
                else if (items[i] is XmlSchemaAttributeGroup)
                {
                    XmlSchemaAttributeGroup attributeGroup = (XmlSchemaAttributeGroup)items[i];
                    PreprocessAttributeGroup(attributeGroup);
                    attributeGroup.QualifiedName.SetNamespace(targetNS); //Since PreprocessAttributeGroup will use this.targetNamespace and that will be that of the root schema's
                    if (redefine.AttributeGroups[attributeGroup.QualifiedName] != null)
                    {
                        SendValidationEvent(SR.Sch_AttrGroupDoubleRedefine, attributeGroup);
                    }
                    else
                    {
                        AddToTable(redefine.AttributeGroups, attributeGroup.QualifiedName, attributeGroup);
                        XmlSchemaAttributeGroup? originalAttrGroup = (XmlSchemaAttributeGroup?)schemaToUpdate.AttributeGroups[attributeGroup.QualifiedName];
                        XmlSchema? parentSchema = GetParentSchema(originalAttrGroup);
                        if (originalAttrGroup == null || (parentSchema != originalSchema && !includesOfRedefine.Contains(parentSchema)))
                        {
                            SendValidationEvent(SR.Sch_ComponentRedefineNotFound, "<attributeGroup>", attributeGroup.QualifiedName.ToString(), attributeGroup);
                        }
                        else
                        {
                            attributeGroup.Redefined = originalAttrGroup;
                            schemaToUpdate.AttributeGroups.Insert(attributeGroup.QualifiedName, attributeGroup);
                            CheckRefinedAttributeGroup(attributeGroup);
                        }
                    }
                }
                else if (items[i] is XmlSchemaComplexType)
                {
                    XmlSchemaComplexType complexType = (XmlSchemaComplexType)items[i];
                    PreprocessComplexType(complexType, false);
                    complexType.QualifiedName.SetNamespace(targetNS); //Since PreprocessComplexType will use this.targetNamespace and that will be that of the root schema's
                    if (redefine.SchemaTypes[complexType.QualifiedName] != null)
                    {
                        SendValidationEvent(SR.Sch_ComplexTypeDoubleRedefine, complexType);
                    }
                    else
                    {
                        AddToTable(redefine.SchemaTypes, complexType.QualifiedName, complexType);
                        XmlSchemaType? originalType = (XmlSchemaType?)schemaToUpdate.SchemaTypes[complexType.QualifiedName];
                        XmlSchema? parentSchema = GetParentSchema(originalType);
                        if (originalType == null || (parentSchema != originalSchema && !includesOfRedefine.Contains(parentSchema)))
                        {
                            SendValidationEvent(SR.Sch_ComponentRedefineNotFound, "<complexType>", complexType.QualifiedName.ToString(), complexType);
                        }
                        else if (originalType is XmlSchemaComplexType)
                        {
                            complexType.Redefined = originalType;
                            schemaToUpdate.SchemaTypes.Insert(complexType.QualifiedName, complexType);
                            CheckRefinedComplexType(complexType);
                        }
                        else
                        {
                            SendValidationEvent(SR.Sch_SimpleToComplexTypeRedefine, complexType);
                        }
                    }
                }
                else if (items[i] is XmlSchemaSimpleType)
                {
                    XmlSchemaSimpleType simpleType = (XmlSchemaSimpleType)items[i];
                    PreprocessSimpleType(simpleType, false);
                    simpleType.QualifiedName.SetNamespace(targetNS); //Since PreprocessSimpleType will use this.targetNamespace and that will be that of the root schema's
                    if (redefine.SchemaTypes[simpleType.QualifiedName] != null)
                    {
                        SendValidationEvent(SR.Sch_SimpleTypeDoubleRedefine, simpleType);
                    }
                    else
                    {
                        AddToTable(redefine.SchemaTypes, simpleType.QualifiedName, simpleType);
                        XmlSchemaType? originalType = (XmlSchemaType?)schemaToUpdate.SchemaTypes[simpleType.QualifiedName];
                        XmlSchema? parentSchema = GetParentSchema(originalType);
                        if (originalType == null || (parentSchema != originalSchema && !includesOfRedefine.Contains(parentSchema)))
                        {
                            SendValidationEvent(SR.Sch_ComponentRedefineNotFound, "<simpleType>", simpleType.QualifiedName.ToString(), simpleType);
                        }
                        else if (originalType is XmlSchemaSimpleType)
                        {
                            simpleType.Redefined = originalType;
                            schemaToUpdate.SchemaTypes.Insert(simpleType.QualifiedName, simpleType);
                            CheckRefinedSimpleType(simpleType);
                        }
                        else
                        {
                            SendValidationEvent(SR.Sch_ComplexToSimpleTypeRedefine, simpleType);
                        }
                    }
                }
            }
        }

        private void GetIncludedSet(XmlSchema schema, ArrayList includesList)
        {
            if (includesList.Contains(schema))
            {
                return;
            }
            includesList.Add(schema);
            for (int i = 0; i < schema.Includes.Count; ++i)
            {
                XmlSchemaExternal external = (XmlSchemaExternal)schema.Includes[i];
                if (external.Compositor == Compositor.Include || external.Compositor == Compositor.Redefine)
                {
                    if (external.Schema != null)
                    {
                        GetIncludedSet(external.Schema, includesList);
                    }
                }
            }
        }

        internal static XmlSchema? GetParentSchema(XmlSchemaObject? currentSchemaObject)
        {
            XmlSchema? parentSchema = null;
            Debug.Assert((currentSchemaObject as XmlSchema) == null); // The current object should not be schema
            while (parentSchema == null && currentSchemaObject != null)
            {
                currentSchemaObject = currentSchemaObject.Parent;
                parentSchema = currentSchemaObject as XmlSchema;
            }

            return parentSchema;
        }

        private void SetSchemaDefaults(XmlSchema schema)
        {
            if (schema.BlockDefault == XmlSchemaDerivationMethod.All)
            {
                _blockDefault = XmlSchemaDerivationMethod.All;
            }
            else if (schema.BlockDefault == XmlSchemaDerivationMethod.None)
            {
                _blockDefault = XmlSchemaDerivationMethod.Empty;
            }
            else
            {
                if ((schema.BlockDefault & ~schemaBlockDefaultAllowed) != 0)
                {
                    SendValidationEvent(SR.Sch_InvalidBlockDefaultValue, schema);
                }
                _blockDefault = schema.BlockDefault & schemaBlockDefaultAllowed;
            }
            if (schema.FinalDefault == XmlSchemaDerivationMethod.All)
            {
                _finalDefault = XmlSchemaDerivationMethod.All;
            }
            else if (schema.FinalDefault == XmlSchemaDerivationMethod.None)
            {
                _finalDefault = XmlSchemaDerivationMethod.Empty;
            }
            else
            {
                if ((schema.FinalDefault & ~schemaFinalDefaultAllowed) != 0)
                {
                    SendValidationEvent(SR.Sch_InvalidFinalDefaultValue, schema);
                }
                _finalDefault = schema.FinalDefault & schemaFinalDefaultAllowed;
            }
            _elementFormDefault = schema.ElementFormDefault;
            if (_elementFormDefault == XmlSchemaForm.None)
            {
                _elementFormDefault = XmlSchemaForm.Unqualified;
            }
            _attributeFormDefault = schema.AttributeFormDefault;
            if (_attributeFormDefault == XmlSchemaForm.None)
            {
                _attributeFormDefault = XmlSchemaForm.Unqualified;
            }
        }

        private int CountGroupSelfReference(XmlSchemaObjectCollection items, XmlQualifiedName name, XmlSchemaGroup? redefined)
        {
            int count = 0;
            for (int i = 0; i < items.Count; ++i)
            {
                XmlSchemaGroupRef? groupRef = items[i] as XmlSchemaGroupRef;
                if (groupRef != null)
                {
                    if (groupRef.RefName == name)
                    {
                        groupRef.Redefined = redefined;
                        if (groupRef.MinOccurs != decimal.One || groupRef.MaxOccurs != decimal.One)
                        {
                            SendValidationEvent(SR.Sch_MinMaxGroupRedefine, groupRef);
                        }
                        count++;
                    }
                }
                else if (items[i] is XmlSchemaGroupBase)
                {
                    count += CountGroupSelfReference(((XmlSchemaGroupBase)items[i]).Items, name, redefined);
                }
                if (count > 1)
                {
                    break;
                }
            }
            return count;
        }

        private void CheckRefinedGroup(XmlSchemaGroup group)
        {
            int count = 0;
            if (group.Particle != null)
            {
                count = CountGroupSelfReference(group.Particle.Items, group.QualifiedName, group.Redefined);
            }
            if (count > 1)
            {
                SendValidationEvent(SR.Sch_MultipleGroupSelfRef, group);
            }
            group.SelfReferenceCount = count;
        }

        private void CheckRefinedAttributeGroup(XmlSchemaAttributeGroup attributeGroup)
        {
            int count = 0;
            for (int i = 0; i < attributeGroup.Attributes.Count; ++i)
            {
                XmlSchemaAttributeGroupRef? attrGroupRef = attributeGroup.Attributes[i] as XmlSchemaAttributeGroupRef;
                if (attrGroupRef != null && attrGroupRef.RefName == attributeGroup.QualifiedName)
                {
                    count++;
                }
            }
            if (count > 1)
            {
                SendValidationEvent(SR.Sch_MultipleAttrGroupSelfRef, attributeGroup);
            }
            attributeGroup.SelfReferenceCount = count;
        }

        private void CheckRefinedSimpleType(XmlSchemaSimpleType stype)
        {
            if (stype.Content != null && stype.Content is XmlSchemaSimpleTypeRestriction)
            {
                XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)stype.Content;
                if (restriction.BaseTypeName == stype.QualifiedName)
                {
                    return;
                }
            }
            SendValidationEvent(SR.Sch_InvalidTypeRedefine, stype);
        }

        private void CheckRefinedComplexType(XmlSchemaComplexType ctype)
        {
            if (ctype.ContentModel != null)
            {
                XmlQualifiedName? baseName;
                if (ctype.ContentModel is XmlSchemaComplexContent)
                {
                    XmlSchemaComplexContent content = (XmlSchemaComplexContent)ctype.ContentModel;
                    if (content.Content is XmlSchemaComplexContentRestriction)
                    {
                        baseName = ((XmlSchemaComplexContentRestriction)content.Content).BaseTypeName;
                    }
                    else
                    {
                        baseName = ((XmlSchemaComplexContentExtension)content.Content!).BaseTypeName;
                    }
                }
                else
                {
                    XmlSchemaSimpleContent content = (XmlSchemaSimpleContent)ctype.ContentModel;
                    if (content.Content is XmlSchemaSimpleContentRestriction)
                    {
                        baseName = ((XmlSchemaSimpleContentRestriction)content.Content).BaseTypeName;
                    }
                    else
                    {
                        baseName = ((XmlSchemaSimpleContentExtension)content.Content!).BaseTypeName;
                    }
                }

                if (baseName == ctype.QualifiedName)
                {
                    return;
                }
            }

            SendValidationEvent(SR.Sch_InvalidTypeRedefine, ctype);
        }

        private void PreprocessAttribute(XmlSchemaAttribute attribute)
        {
            if (attribute.Name != null)
            {
                ValidateNameAttribute(attribute);
                attribute.SetQualifiedName(new XmlQualifiedName(attribute.Name, _targetNamespace));
            }
            else
            {
                SendValidationEvent(SR.Sch_MissRequiredAttribute, "name", attribute);
            }
            if (attribute.Use != XmlSchemaUse.None)
            {
                SendValidationEvent(SR.Sch_ForbiddenAttribute, "use", attribute);
            }
            if (attribute.Form != XmlSchemaForm.None)
            {
                SendValidationEvent(SR.Sch_ForbiddenAttribute, "form", attribute);
            }
            PreprocessAttributeContent(attribute);
            ValidateIdAttribute(attribute);
        }

        private void PreprocessLocalAttribute(XmlSchemaAttribute attribute)
        {
            if (attribute.Name != null)
            { // name
                ValidateNameAttribute(attribute);
                PreprocessAttributeContent(attribute);
                attribute.SetQualifiedName(new XmlQualifiedName(attribute.Name, (attribute.Form == XmlSchemaForm.Qualified || (attribute.Form == XmlSchemaForm.None && _attributeFormDefault == XmlSchemaForm.Qualified)) ? _targetNamespace : null));
            }
            else
            { // ref
                PreprocessAnnotation(attribute); //set parent of annotation child of ref
                if (attribute.RefName.IsEmpty)
                {
                    SendValidationEvent(SR.Sch_AttributeNameRef, "???", attribute);
                }
                else
                {
                    ValidateQNameAttribute(attribute, "ref", attribute.RefName);
                }
                if (!attribute.SchemaTypeName.IsEmpty ||
                    attribute.SchemaType != null ||
                    attribute.Form != XmlSchemaForm.None /*||
                    attribute.DefaultValue != null ||
                    attribute.FixedValue != null*/
                )
                {
                    SendValidationEvent(SR.Sch_InvalidAttributeRef, attribute);
                }
                attribute.SetQualifiedName(attribute.RefName);
            }
            ValidateIdAttribute(attribute);
        }

        private void PreprocessAttributeContent(XmlSchemaAttribute attribute)
        {
            PreprocessAnnotation(attribute);

            if (Ref.Equal(_currentSchema!.TargetNamespace, _nsXsi))
            {
                SendValidationEvent(SR.Sch_TargetNamespaceXsi, attribute);
            }

            if (!attribute.RefName.IsEmpty)
            {
                SendValidationEvent(SR.Sch_ForbiddenAttribute, "ref", attribute);
            }
            if (attribute.DefaultValue != null && attribute.FixedValue != null)
            {
                SendValidationEvent(SR.Sch_DefaultFixedAttributes, attribute);
            }
            if (attribute.DefaultValue != null && attribute.Use != XmlSchemaUse.Optional && attribute.Use != XmlSchemaUse.None)
            {
                SendValidationEvent(SR.Sch_OptionalDefaultAttribute, attribute);
            }
            if (attribute.Name == _xmlns)
            {
                SendValidationEvent(SR.Sch_XmlNsAttribute, attribute);
            }
            if (attribute.SchemaType != null)
            {
                SetParent(attribute.SchemaType, attribute);
                if (!attribute.SchemaTypeName.IsEmpty)
                {
                    SendValidationEvent(SR.Sch_TypeMutualExclusive, attribute);
                }
                PreprocessSimpleType(attribute.SchemaType, true);
            }
            if (!attribute.SchemaTypeName.IsEmpty)
            {
                ValidateQNameAttribute(attribute, "type", attribute.SchemaTypeName);
            }
        }

        private void PreprocessAttributeGroup(XmlSchemaAttributeGroup attributeGroup)
        {
            if (attributeGroup.Name != null)
            {
                ValidateNameAttribute(attributeGroup);
                attributeGroup.SetQualifiedName(new XmlQualifiedName(attributeGroup.Name, _targetNamespace));
            }
            else
            {
                SendValidationEvent(SR.Sch_MissRequiredAttribute, "name", attributeGroup);
            }
            PreprocessAttributes(attributeGroup.Attributes, attributeGroup.AnyAttribute, attributeGroup);
            PreprocessAnnotation(attributeGroup);
            ValidateIdAttribute(attributeGroup);
        }

        private void PreprocessElement(XmlSchemaElement element)
        {
            if (element.Name != null)
            {
                ValidateNameAttribute(element);
                element.SetQualifiedName(new XmlQualifiedName(element.Name, _targetNamespace));
            }
            else
            {
                SendValidationEvent(SR.Sch_MissRequiredAttribute, "name", element);
            }
            PreprocessElementContent(element);

            if (element.Final == XmlSchemaDerivationMethod.All)
            {
                element.SetFinalResolved(XmlSchemaDerivationMethod.All);
            }
            else if (element.Final == XmlSchemaDerivationMethod.None)
            {
                if (_finalDefault == XmlSchemaDerivationMethod.All)
                {
                    element.SetFinalResolved(XmlSchemaDerivationMethod.All);
                }
                else
                {
                    element.SetFinalResolved(_finalDefault & elementFinalAllowed);
                }
            }
            else
            {
                if ((element.Final & ~elementFinalAllowed) != 0)
                {
                    SendValidationEvent(SR.Sch_InvalidElementFinalValue, element);
                }
                element.SetFinalResolved(element.Final & elementFinalAllowed);
            }
            if (element.Form != XmlSchemaForm.None)
            {
                SendValidationEvent(SR.Sch_ForbiddenAttribute, "form", element);
            }
            if (element.MinOccursString != null)
            {
                SendValidationEvent(SR.Sch_ForbiddenAttribute, "minOccurs", element);
            }
            if (element.MaxOccursString != null)
            {
                SendValidationEvent(SR.Sch_ForbiddenAttribute, "maxOccurs", element);
            }
            if (!element.SubstitutionGroup.IsEmpty)
            {
                ValidateQNameAttribute(element, "type", element.SubstitutionGroup);
            }
            ValidateIdAttribute(element);
        }

        private void PreprocessLocalElement(XmlSchemaElement element)
        {
            if (element.Name != null)
            { // name
                ValidateNameAttribute(element);
                PreprocessElementContent(element);
                element.SetQualifiedName(new XmlQualifiedName(element.Name, (element.Form == XmlSchemaForm.Qualified || (element.Form == XmlSchemaForm.None && _elementFormDefault == XmlSchemaForm.Qualified)) ? _targetNamespace : null));
            }
            else
            { // ref
                PreprocessAnnotation(element); //Check annotation child for ref and set parent
                if (element.RefName.IsEmpty)
                {
                    SendValidationEvent(SR.Sch_ElementNameRef, element);
                }
                else
                {
                    ValidateQNameAttribute(element, "ref", element.RefName);
                }
                if (!element.SchemaTypeName.IsEmpty ||
                    element.HasAbstractAttribute ||
                    element.Block != XmlSchemaDerivationMethod.None ||
                    element.SchemaType != null ||
                    element.HasConstraints ||
                    element.DefaultValue != null ||
                    element.Form != XmlSchemaForm.None ||
                    element.FixedValue != null ||
                    element.HasNillableAttribute)
                {
                    SendValidationEvent(SR.Sch_InvalidElementRef, element);
                }
                if (element.DefaultValue != null && element.FixedValue != null)
                {
                    SendValidationEvent(SR.Sch_DefaultFixedAttributes, element);
                }
                element.SetQualifiedName(element.RefName);
            }
            if (element.MinOccurs > element.MaxOccurs)
            {
                element.MinOccurs = decimal.Zero;
                SendValidationEvent(SR.Sch_MinGtMax, element);
            }
            if (element.HasAbstractAttribute)
            {
                SendValidationEvent(SR.Sch_ForbiddenAttribute, "abstract", element);
            }
            if (element.Final != XmlSchemaDerivationMethod.None)
            {
                SendValidationEvent(SR.Sch_ForbiddenAttribute, "final", element);
            }
            if (!element.SubstitutionGroup.IsEmpty)
            {
                SendValidationEvent(SR.Sch_ForbiddenAttribute, "substitutionGroup", element);
            }
            ValidateIdAttribute(element);
        }

        private void PreprocessElementContent(XmlSchemaElement element)
        {
            PreprocessAnnotation(element); //Set parent for Annotation child of element
            if (!element.RefName.IsEmpty)
            {
                SendValidationEvent(SR.Sch_ForbiddenAttribute, "ref", element);
            }
            if (element.Block == XmlSchemaDerivationMethod.All)
            {
                element.SetBlockResolved(XmlSchemaDerivationMethod.All);
            }
            else if (element.Block == XmlSchemaDerivationMethod.None)
            {
                if (_blockDefault == XmlSchemaDerivationMethod.All)
                {
                    element.SetBlockResolved(XmlSchemaDerivationMethod.All);
                }
                else
                {
                    element.SetBlockResolved(_blockDefault & elementBlockAllowed);
                }
            }
            else
            {
                if ((element.Block & ~elementBlockAllowed) != 0)
                {
                    SendValidationEvent(SR.Sch_InvalidElementBlockValue, element);
                }
                element.SetBlockResolved(element.Block & elementBlockAllowed);
            }
            if (element.SchemaType != null)
            {
                SetParent(element.SchemaType, element); //Set parent for simple / complex type child of element
                if (!element.SchemaTypeName.IsEmpty)
                {
                    SendValidationEvent(SR.Sch_TypeMutualExclusive, element);
                }
                if (element.SchemaType is XmlSchemaComplexType)
                {
                    PreprocessComplexType((XmlSchemaComplexType)element.SchemaType, true);
                }
                else
                {
                    PreprocessSimpleType((XmlSchemaSimpleType)element.SchemaType, true);
                }
            }
            if (!element.SchemaTypeName.IsEmpty)
            {
                ValidateQNameAttribute(element, "type", element.SchemaTypeName);
            }
            if (element.DefaultValue != null && element.FixedValue != null)
            {
                SendValidationEvent(SR.Sch_DefaultFixedAttributes, element);
            }

            for (int i = 0; i < element.Constraints.Count; ++i)
            {
                XmlSchemaIdentityConstraint identityConstraint = (XmlSchemaIdentityConstraint)element.Constraints[i];
                SetParent(identityConstraint, element);
                PreprocessIdentityConstraint(identityConstraint);
            }
        }

        private void PreprocessIdentityConstraint(XmlSchemaIdentityConstraint constraint)
        {
            bool valid = true;
            PreprocessAnnotation(constraint); //Set parent of annotation child of key/keyref/unique
            if (constraint.Name != null)
            {
                ValidateNameAttribute(constraint);
                constraint.SetQualifiedName(new XmlQualifiedName(constraint.Name, _targetNamespace));
            }
            else
            {
                SendValidationEvent(SR.Sch_MissRequiredAttribute, "name", constraint);
                valid = false;
            }

            if (_rootSchema!.IdentityConstraints[constraint.QualifiedName] != null)
            {
                SendValidationEvent(SR.Sch_DupIdentityConstraint, constraint.QualifiedName.ToString(), constraint);
                valid = false;
            }
            else
            {
                _rootSchema.IdentityConstraints.Add(constraint.QualifiedName, constraint);
            }

            if (constraint.Selector == null)
            {
                SendValidationEvent(SR.Sch_IdConstraintNoSelector, constraint);
                valid = false;
            }

            if (constraint.Fields.Count == 0)
            {
                SendValidationEvent(SR.Sch_IdConstraintNoFields, constraint);
                valid = false;
            }

            if (constraint is XmlSchemaKeyref)
            {
                XmlSchemaKeyref keyref = (XmlSchemaKeyref)constraint;
                if (keyref.Refer.IsEmpty)
                {
                    SendValidationEvent(SR.Sch_IdConstraintNoRefer, constraint);
                    valid = false;
                }
                else
                {
                    ValidateQNameAttribute(keyref, "refer", keyref.Refer);
                }
            }

            if (valid)
            {
                ValidateIdAttribute(constraint);
                ValidateIdAttribute(constraint.Selector!);
                SetParent(constraint.Selector!, constraint);

                for (int i = 0; i < constraint.Fields.Count; ++i)
                {
                    SetParent(constraint.Fields[i], constraint);
                    ValidateIdAttribute(constraint.Fields[i]);
                }
            }
        }

        private void PreprocessSimpleType(XmlSchemaSimpleType simpleType, bool local)
        {
            if (local)
            {
                if (simpleType.Name != null)
                {
                    SendValidationEvent(SR.Sch_ForbiddenAttribute, "name", simpleType);
                }
            }
            else
            {
                if (simpleType.Name != null)
                {
                    ValidateNameAttribute(simpleType);
                    simpleType.SetQualifiedName(new XmlQualifiedName(simpleType.Name, _targetNamespace));
                }
                else
                {
                    SendValidationEvent(SR.Sch_MissRequiredAttribute, "name", simpleType);
                }

                if (simpleType.Final == XmlSchemaDerivationMethod.All)
                {
                    simpleType.SetFinalResolved(XmlSchemaDerivationMethod.All);
                }
                else if (simpleType.Final == XmlSchemaDerivationMethod.None)
                {
                    if (_finalDefault == XmlSchemaDerivationMethod.All)
                    {
                        simpleType.SetFinalResolved(XmlSchemaDerivationMethod.All);
                    }
                    else
                    {
                        simpleType.SetFinalResolved(_finalDefault & simpleTypeFinalAllowed);
                    }
                }
                else
                {
                    if ((simpleType.Final & ~simpleTypeFinalAllowed) != 0)
                    {
                        SendValidationEvent(SR.Sch_InvalidSimpleTypeFinalValue, simpleType);
                    }
                    simpleType.SetFinalResolved(simpleType.Final & simpleTypeFinalAllowed);
                }
            }

            if (simpleType.Content == null)
            {
                SendValidationEvent(SR.Sch_NoSimpleTypeContent, simpleType);
            }
            else if (simpleType.Content is XmlSchemaSimpleTypeRestriction)
            {
                XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)simpleType.Content;
                //SetParent
                SetParent(restriction, simpleType);
                for (int i = 0; i < restriction.Facets.Count; ++i)
                {
                    SetParent(restriction.Facets[i], restriction);
                }

                if (restriction.BaseType != null)
                {
                    if (!restriction.BaseTypeName.IsEmpty)
                    {
                        SendValidationEvent(SR.Sch_SimpleTypeRestRefBase, restriction);
                    }
                    PreprocessSimpleType(restriction.BaseType, true);
                }
                else
                {
                    if (restriction.BaseTypeName.IsEmpty)
                    {
                        SendValidationEvent(SR.Sch_SimpleTypeRestRefBaseNone, restriction);
                    }
                    else
                    {
                        ValidateQNameAttribute(restriction, "base", restriction.BaseTypeName);
                    }
                }
                PreprocessAnnotation(restriction); //set parent of annotation child of simple type restriction
                ValidateIdAttribute(restriction);
            }
            else if (simpleType.Content is XmlSchemaSimpleTypeList)
            {
                XmlSchemaSimpleTypeList list = (XmlSchemaSimpleTypeList)simpleType.Content;
                SetParent(list, simpleType);

                if (list.ItemType != null)
                {
                    if (!list.ItemTypeName.IsEmpty)
                    {
                        SendValidationEvent(SR.Sch_SimpleTypeListRefBase, list);
                    }
                    SetParent(list.ItemType, list);
                    PreprocessSimpleType(list.ItemType, true);
                }
                else
                {
                    if (list.ItemTypeName.IsEmpty)
                    {
                        SendValidationEvent(SR.Sch_SimpleTypeListRefBaseNone, list);
                    }
                    else
                    {
                        ValidateQNameAttribute(list, "itemType", list.ItemTypeName);
                    }
                }
                PreprocessAnnotation(list); //set parent of annotation child of simple type list
                ValidateIdAttribute(list);
            }
            else
            { // union
                XmlSchemaSimpleTypeUnion union1 = (XmlSchemaSimpleTypeUnion)simpleType.Content;
                SetParent(union1, simpleType);

                int baseTypeCount = union1.BaseTypes.Count;
                if (union1.MemberTypes != null)
                {
                    baseTypeCount += union1.MemberTypes.Length;
                    XmlQualifiedName[] qNames = union1.MemberTypes;
                    for (int i = 0; i < qNames.Length; ++i)
                    {
                        ValidateQNameAttribute(union1, "memberTypes", qNames[i]);
                    }
                }
                if (baseTypeCount == 0)
                {
                    SendValidationEvent(SR.Sch_SimpleTypeUnionNoBase, union1);
                }
                for (int i = 0; i < union1.BaseTypes.Count; ++i)
                {
                    XmlSchemaSimpleType type = (XmlSchemaSimpleType)union1.BaseTypes[i];
                    SetParent(type, union1);
                    PreprocessSimpleType(type, true);
                }
                PreprocessAnnotation(union1); //set parent of annotation child of simple type union
                ValidateIdAttribute(union1);
            }
            ValidateIdAttribute(simpleType);
        }

        private void PreprocessComplexType(XmlSchemaComplexType complexType, bool local)
        {
            if (local)
            {
                if (complexType.Name != null)
                {
                    SendValidationEvent(SR.Sch_ForbiddenAttribute, "name", complexType);
                }
            }
            else
            {
                if (complexType.Name != null)
                {
                    ValidateNameAttribute(complexType);
                    complexType.SetQualifiedName(new XmlQualifiedName(complexType.Name, _targetNamespace));
                }
                else
                {
                    SendValidationEvent(SR.Sch_MissRequiredAttribute, "name", complexType);
                }
                if (complexType.Block == XmlSchemaDerivationMethod.All)
                {
                    complexType.SetBlockResolved(XmlSchemaDerivationMethod.All);
                }
                else if (complexType.Block == XmlSchemaDerivationMethod.None)
                {
                    complexType.SetBlockResolved(_blockDefault & complexTypeBlockAllowed);
                }
                else
                {
                    if ((complexType.Block & ~complexTypeBlockAllowed) != 0)
                    {
                        SendValidationEvent(SR.Sch_InvalidComplexTypeBlockValue, complexType);
                    }
                    complexType.SetBlockResolved(complexType.Block & complexTypeBlockAllowed);
                }
                if (complexType.Final == XmlSchemaDerivationMethod.All)
                {
                    complexType.SetFinalResolved(XmlSchemaDerivationMethod.All);
                }
                else if (complexType.Final == XmlSchemaDerivationMethod.None)
                {
                    if (_finalDefault == XmlSchemaDerivationMethod.All)
                    {
                        complexType.SetFinalResolved(XmlSchemaDerivationMethod.All);
                    }
                    else
                    {
                        complexType.SetFinalResolved(_finalDefault & complexTypeFinalAllowed);
                    }
                }
                else
                {
                    if ((complexType.Final & ~complexTypeFinalAllowed) != 0)
                    {
                        SendValidationEvent(SR.Sch_InvalidComplexTypeFinalValue, complexType);
                    }
                    complexType.SetFinalResolved(complexType.Final & complexTypeFinalAllowed);
                }
            }

            if (complexType.ContentModel != null)
            {
                SetParent(complexType.ContentModel, complexType); //SimpleContent / complexCotent
                PreprocessAnnotation(complexType.ContentModel);

                if (complexType.Particle != null || complexType.Attributes != null)
                {
                    // this is illegal
                }
                if (complexType.ContentModel is XmlSchemaSimpleContent)
                {
                    XmlSchemaSimpleContent content = (XmlSchemaSimpleContent)complexType.ContentModel;
                    if (content.Content == null)
                    {
                        if (complexType.QualifiedName == XmlQualifiedName.Empty)
                        {
                            SendValidationEvent(SR.Sch_NoRestOrExt, complexType);
                        }
                        else
                        {
                            SendValidationEvent(SR.Sch_NoRestOrExtQName, complexType.QualifiedName.Name, complexType.QualifiedName.Namespace, complexType);
                        }
                    }
                    else
                    {
                        SetParent(content.Content, content);   //simplecontent extension / restriction
                        PreprocessAnnotation(content.Content); //annotation child of simple extension / restriction

                        if (content.Content is XmlSchemaSimpleContentExtension)
                        {
                            XmlSchemaSimpleContentExtension contentExtension = (XmlSchemaSimpleContentExtension)content.Content;
                            if (contentExtension.BaseTypeName.IsEmpty)
                            {
                                SendValidationEvent(SR.Sch_MissAttribute, "base", contentExtension);
                            }
                            else
                            {
                                ValidateQNameAttribute(contentExtension, "base", contentExtension.BaseTypeName);
                            }
                            PreprocessAttributes(contentExtension.Attributes, contentExtension.AnyAttribute, contentExtension);
                            ValidateIdAttribute(contentExtension);
                        }
                        else
                        { //XmlSchemaSimpleContentRestriction
                            XmlSchemaSimpleContentRestriction contentRestriction = (XmlSchemaSimpleContentRestriction)content.Content;
                            if (contentRestriction.BaseTypeName.IsEmpty)
                            {
                                SendValidationEvent(SR.Sch_MissAttribute, "base", contentRestriction);
                            }
                            else
                            {
                                ValidateQNameAttribute(contentRestriction, "base", contentRestriction.BaseTypeName);
                            }
                            if (contentRestriction.BaseType != null)
                            {
                                SetParent(contentRestriction.BaseType, contentRestriction);
                                PreprocessSimpleType(contentRestriction.BaseType, true);
                            }
                            PreprocessAttributes(contentRestriction.Attributes, contentRestriction.AnyAttribute, contentRestriction);
                            ValidateIdAttribute(contentRestriction);
                        }
                    }
                    ValidateIdAttribute(content);
                }
                else
                { // XmlSchemaComplexContent
                    XmlSchemaComplexContent content = (XmlSchemaComplexContent)complexType.ContentModel;
                    if (content.Content == null)
                    {
                        if (complexType.QualifiedName == XmlQualifiedName.Empty)
                        {
                            SendValidationEvent(SR.Sch_NoRestOrExt, complexType);
                        }
                        else
                        {
                            SendValidationEvent(SR.Sch_NoRestOrExtQName, complexType.QualifiedName.Name, complexType.QualifiedName.Namespace, complexType);
                        }
                    }
                    else
                    {
                        if (!content.HasMixedAttribute && complexType.IsMixed)
                        {
                            content.IsMixed = true; // fixup
                        }
                        SetParent(content.Content, content);   //complexcontent extension / restriction
                        PreprocessAnnotation(content.Content); //Annotation child of extension / restriction

                        if (content.Content is XmlSchemaComplexContentExtension)
                        {
                            XmlSchemaComplexContentExtension contentExtension = (XmlSchemaComplexContentExtension)content.Content;
                            if (contentExtension.BaseTypeName.IsEmpty)
                            {
                                SendValidationEvent(SR.Sch_MissAttribute, "base", contentExtension);
                            }
                            else
                            {
                                ValidateQNameAttribute(contentExtension, "base", contentExtension.BaseTypeName);
                            }
                            if (contentExtension.Particle != null)
                            {
                                SetParent(contentExtension.Particle, contentExtension); // Group / all / choice / sequence
                                PreprocessParticle(contentExtension.Particle);
                            }
                            PreprocessAttributes(contentExtension.Attributes, contentExtension.AnyAttribute, contentExtension);
                            ValidateIdAttribute(contentExtension);
                        }
                        else
                        {
                            XmlSchemaComplexContentRestriction contentRestriction = (XmlSchemaComplexContentRestriction)content.Content;
                            if (contentRestriction.BaseTypeName.IsEmpty)
                            {
                                SendValidationEvent(SR.Sch_MissAttribute, "base", contentRestriction);
                            }
                            else
                            {
                                ValidateQNameAttribute(contentRestriction, "base", contentRestriction.BaseTypeName);
                            }

                            if (contentRestriction.Particle != null)
                            {
                                SetParent(contentRestriction.Particle, contentRestriction); // Group / all / choice / sequence
                                PreprocessParticle(contentRestriction.Particle);
                            }

                            PreprocessAttributes(contentRestriction.Attributes, contentRestriction.AnyAttribute, contentRestriction);
                            ValidateIdAttribute(contentRestriction);
                        }
                        ValidateIdAttribute(content);
                    }
                }
            }
            else
            {
                if (complexType.Particle != null)
                {
                    SetParent(complexType.Particle, complexType);
                    PreprocessParticle(complexType.Particle);
                }
                PreprocessAttributes(complexType.Attributes, complexType.AnyAttribute, complexType);
            }
            ValidateIdAttribute(complexType);
        }

        private void PreprocessGroup(XmlSchemaGroup group)
        {
            if (group.Name != null)
            {
                ValidateNameAttribute(group);
                group.SetQualifiedName(new XmlQualifiedName(group.Name, _targetNamespace));
            }
            else
            {
                SendValidationEvent(SR.Sch_MissRequiredAttribute, "name", group);
            }
            if (group.Particle == null)
            {
                SendValidationEvent(SR.Sch_NoGroupParticle, group);
                return;
            }
            if (group.Particle.MinOccursString != null)
            {
                SendValidationEvent(SR.Sch_ForbiddenAttribute, "minOccurs", group.Particle);
            }
            if (group.Particle.MaxOccursString != null)
            {
                SendValidationEvent(SR.Sch_ForbiddenAttribute, "maxOccurs", group.Particle);
            }

            PreprocessParticle(group.Particle);
            PreprocessAnnotation(group); //Set parent of annotation child of group
            ValidateIdAttribute(group);
        }

        private void PreprocessNotation(XmlSchemaNotation notation)
        {
            if (notation.Name != null)
            {
                ValidateNameAttribute(notation);
                notation.QualifiedName = new XmlQualifiedName(notation.Name, _targetNamespace);
            }
            else
            {
                SendValidationEvent(SR.Sch_MissRequiredAttribute, "name", notation);
            }
            if (notation.Public == null && notation.System == null)
            {
                SendValidationEvent(SR.Sch_MissingPublicSystemAttribute, notation);
            }
            else
            {
                if (notation.Public != null)
                {
                    try
                    {
                        XmlConvert.VerifyTOKEN(notation.Public); // can throw
                    }
                    catch (XmlException eInner)
                    {
                        SendValidationEvent(SR.Sch_InvalidPublicAttribute, new string[] { notation.Public }, eInner, notation);
                    }
                }
                if (notation.System != null)
                {
                    ParseUri(notation.System, SR.Sch_InvalidSystemAttribute, notation);
                }
            }
            PreprocessAnnotation(notation); //Set parent of annotation child of notation
            ValidateIdAttribute(notation);
        }


        private void PreprocessParticle(XmlSchemaParticle particle)
        {
            XmlSchemaObjectCollection items;
            if (particle is XmlSchemaAll)
            {
                if (particle.MinOccurs != decimal.Zero && particle.MinOccurs != decimal.One)
                {
                    particle.MinOccurs = decimal.One;
                    SendValidationEvent(SR.Sch_InvalidAllMin, particle);
                }
                if (particle.MaxOccurs != decimal.One)
                {
                    particle.MaxOccurs = decimal.One;
                    SendValidationEvent(SR.Sch_InvalidAllMax, particle);
                }
                items = ((XmlSchemaAll)particle).Items;
                for (int i = 0; i < items.Count; ++i)
                {
                    XmlSchemaElement element = (XmlSchemaElement)items[i];
                    if (element.MaxOccurs != decimal.Zero && element.MaxOccurs != decimal.One)
                    {
                        element.MaxOccurs = decimal.One;
                        SendValidationEvent(SR.Sch_InvalidAllElementMax, element);
                    }
                    SetParent(element, particle);
                    PreprocessLocalElement(element);
                }
            }
            else
            {
                if (particle.MinOccurs > particle.MaxOccurs)
                {
                    particle.MinOccurs = particle.MaxOccurs;
                    SendValidationEvent(SR.Sch_MinGtMax, particle);
                }
                if (particle is XmlSchemaChoice)
                {
                    items = ((XmlSchemaChoice)particle).Items;
                    for (int i = 0; i < items.Count; ++i)
                    {
                        SetParent(items[i], particle);
                        XmlSchemaElement? element = items[i] as XmlSchemaElement;
                        if (element != null)
                        {
                            PreprocessLocalElement(element);
                        }
                        else
                        {
                            PreprocessParticle((XmlSchemaParticle)items[i]);
                        }
                    }
                }
                else if (particle is XmlSchemaSequence)
                {
                    items = ((XmlSchemaSequence)particle).Items;
                    for (int i = 0; i < items.Count; ++i)
                    {
                        SetParent(items[i], particle);
                        XmlSchemaElement? element = items[i] as XmlSchemaElement;
                        if (element != null)
                        {
                            PreprocessLocalElement(element);
                        }
                        else
                        {
                            PreprocessParticle((XmlSchemaParticle)items[i]);
                        }
                    }
                }
                else if (particle is XmlSchemaGroupRef)
                {
                    XmlSchemaGroupRef groupRef = (XmlSchemaGroupRef)particle;
                    if (groupRef.RefName.IsEmpty)
                    {
                        SendValidationEvent(SR.Sch_MissAttribute, "ref", groupRef);
                    }
                    else
                    {
                        ValidateQNameAttribute(groupRef, "ref", groupRef.RefName);
                    }
                }
                else if (particle is XmlSchemaAny)
                {
                    try
                    {
                        ((XmlSchemaAny)particle).BuildNamespaceList(_targetNamespace!);
                    }
                    catch (FormatException fe)
                    {
                        SendValidationEvent(SR.Sch_InvalidAnyDetailed, new string[] { fe.Message }, fe, particle);
                    }
                }
            }
            PreprocessAnnotation(particle); //set parent of annotation child of group / all/ choice / sequence
            ValidateIdAttribute(particle);
        }

        private void PreprocessAttributes(XmlSchemaObjectCollection attributes, XmlSchemaAnyAttribute? anyAttribute, XmlSchemaObject parent)
        {
            for (int i = 0; i < attributes.Count; ++i)
            {
                SetParent(attributes[i], parent);
                XmlSchemaAttribute? attr = attributes[i] as XmlSchemaAttribute;
                if (attr != null)
                {
                    PreprocessLocalAttribute(attr);
                }
                else
                { // XmlSchemaAttributeGroupRef
                    XmlSchemaAttributeGroupRef attributeGroupRef = (XmlSchemaAttributeGroupRef)attributes[i];
                    if (attributeGroupRef.RefName.IsEmpty)
                    {
                        SendValidationEvent(SR.Sch_MissAttribute, "ref", attributeGroupRef);
                    }
                    else
                    {
                        ValidateQNameAttribute(attributeGroupRef, "ref", attributeGroupRef.RefName);
                    }
                    PreprocessAnnotation(attributes[i]); //set parent of annotation child of attributeGroupRef
                    ValidateIdAttribute(attributes[i]);
                }
            }

            if (anyAttribute != null)
            {
                try
                {
                    SetParent(anyAttribute, parent);
                    PreprocessAnnotation(anyAttribute); //set parent of annotation child of any attribute
                    anyAttribute.BuildNamespaceList(_targetNamespace!);
                }
                catch (FormatException fe)
                {
                    SendValidationEvent(SR.Sch_InvalidAnyDetailed, new string[] { fe.Message }, fe, anyAttribute);
                }

                ValidateIdAttribute(anyAttribute);
            }
        }

        private void ValidateIdAttribute(XmlSchemaObject xso)
        {
            if (xso.IdAttribute != null)
            {
                try
                {
                    xso.IdAttribute = NameTable.Add(XmlConvert.VerifyNCName(xso.IdAttribute));
                }
                catch (XmlException ex)
                {
                    SendValidationEvent(SR.Sch_InvalidIdAttribute, new string[] { ex.Message }, ex, xso);
                    return;
                }
                catch (ArgumentNullException)
                {
                    SendValidationEvent(SR.Sch_InvalidIdAttribute, SR.Sch_NullValue, xso);
                    return;
                }
                try
                {
                    _currentSchema!.Ids.Add(xso.IdAttribute, xso);
                }
                catch (ArgumentException)
                {
                    SendValidationEvent(SR.Sch_DupIdAttribute, xso);
                }
            }
        }

        private void ValidateNameAttribute(XmlSchemaObject xso)
        {
            string? name = xso.NameAttribute;
            if (name == null || name.Length == 0)
            {
                SendValidationEvent(SR.Sch_InvalidNameAttributeEx, null, SR.Sch_NullValue, xso);
            }

            //Normalize whitespace since NCName has whitespace facet="collapse"
            name = XmlComplianceUtil.NonCDataNormalize(name ?? string.Empty);
            int len = ValidateNames.ParseNCName(name, 0);
            if (len != name.Length)
            {
                // If the string is not a valid NCName, then throw or return false
                string[] invCharArgs = XmlException.BuildCharExceptionArgs(name, len);
                string innerStr = SR.Format(SR.Xml_BadNameCharWithPos, invCharArgs[0], invCharArgs[1], len);
                SendValidationEvent(SR.Sch_InvalidNameAttributeEx, name, innerStr, xso);
            }
            else
            {
                xso.NameAttribute = NameTable.Add(name);
            }
        }

        private void ValidateQNameAttribute(XmlSchemaObject xso, string attributeName, XmlQualifiedName value)
        {
            try
            {
                value.Verify();
                value.Atomize(NameTable);
                if (_currentSchema!.IsChameleon && value.Namespace.Length == 0)
                {
                    value.SetNamespace(_currentSchema.TargetNamespace); //chameleon schemas are clones that have correct targetNamespace set
                }
                if (_referenceNamespaces[value.Namespace] == null)
                {
                    SendValidationEvent(SR.Sch_UnrefNS, value.Namespace, xso, XmlSeverityType.Warning);
                }
            }
            catch (FormatException fx)
            {
                SendValidationEvent(SR.Sch_InvalidAttribute, new string[] { attributeName, fx.Message }, fx, xso);
            }
            catch (XmlException ex)
            {
                SendValidationEvent(SR.Sch_InvalidAttribute, new string[] { attributeName, ex.Message }, ex, xso);
            }
        }

        private Uri? ResolveSchemaLocationUri(XmlSchema enclosingSchema, string location)
        {
            if (location.Length == 0)
            {
                return null;
            }

            return _xmlResolver!.ResolveUri(enclosingSchema.BaseUri, location);
        }

        private object? GetSchemaEntity(Uri ruri)
        {
            return _xmlResolver!.GetEntity(ruri, null, null);
        }

        private XmlSchema GetChameleonSchema(string targetNamespace, XmlSchema schema)
        {
            ChameleonKey cKey = new ChameleonKey(targetNamespace, schema);
            XmlSchema? chameleonSchema = (XmlSchema?)_chameleonSchemas![cKey]; // Need not clone if a schema for that namespace already exists
            if (chameleonSchema == null)
            {
                chameleonSchema = schema.DeepClone(); // It is ok that we dont lock the clone since no one else has access to it yet
                chameleonSchema.IsChameleon = true;
                chameleonSchema.TargetNamespace = targetNamespace;
                _chameleonSchemas.Add(cKey, chameleonSchema);
                chameleonSchema.SourceUri = schema.SourceUri;
                // Handle the original schema that was added to lockList before cloning occurred
                schema.IsProcessing = false; // Since we cloned it for the chameleon
            }

            return chameleonSchema;
        }

        private static void SetParent(XmlSchemaObject child, XmlSchemaObject parent)
        {
            child.Parent = parent;
        }

        private void PreprocessAnnotation(XmlSchemaObject schemaObject)
        {
            XmlSchemaAnnotation? annotation;
            if (schemaObject is XmlSchemaAnnotated annotated)
            {
                annotation = annotated.Annotation;
                if (annotation != null)
                {
                    PreprocessAnnotation(annotation);
                    annotation.Parent = schemaObject;
                }
            }
        }

        private void PreprocessAnnotation(XmlSchemaAnnotation annotation)
        {
            ValidateIdAttribute(annotation);
            for (int i = 0; i < annotation.Items.Count; ++i)
            {
                annotation.Items[i].Parent = annotation; //Can be documentation or appInfo
            }
        }
    };
} // namespace System.Xml
