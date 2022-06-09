// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Schema
{
    /// <summary>
    /// The XmlSchemaSet contains a set of namespace URI's.
    /// Each namespace also have an associated private data cache
    /// corresponding to the XML-Data Schema or W3C XML Schema.
    /// The XmlSchemaSet will able to load only XSD schemas,
    /// and compile them into an internal "cooked schema representation".
    /// The Validate method then uses this internal representation for
    /// efficient runtime validation of any given subtree.
    /// </summary>
    public class XmlSchemaSet
    {
        private readonly XmlNameTable _nameTable;
        private SchemaNames? _schemaNames;
        private readonly SortedList _schemas;              // List of source schemas

        //Event handling
        private readonly ValidationEventHandler _internalEventHandler;
        private ValidationEventHandler? _eventHandler;

        private bool _isCompiled;

        //Dictionary<Uri, XmlSchema> schemaLocations;
        //Dictionary<ChameleonKey, XmlSchema> chameleonSchemas;
        private readonly Hashtable _schemaLocations;
        private readonly Hashtable _chameleonSchemas;

        private readonly Hashtable _targetNamespaces;
        private bool _compileAll;

        //Cached Compiled Info
        private SchemaInfo _cachedCompiledInfo;

        //Reader settings to parse schema
        private readonly XmlReaderSettings _readerSettings;
        private XmlSchema? _schemaForSchema;  //Only one schema for schema per set

        //Schema compilation settings
        private XmlSchemaCompilationSettings _compilationSettings;

        internal XmlSchemaObjectTable? elements;
        internal XmlSchemaObjectTable? attributes;
        internal XmlSchemaObjectTable? schemaTypes;
        internal XmlSchemaObjectTable? substitutionGroups;
        private XmlSchemaObjectTable? _typeExtensions;

        //Thread safety
        private object? _internalSyncObject;
        internal object InternalSyncObject
        {
            get
            {
                if (_internalSyncObject == null)
                {
                    object o = new object();
                    Interlocked.CompareExchange<object?>(ref _internalSyncObject, o, null);
                }

                return _internalSyncObject;
            }
        }

        //Constructors

        /// <summary>
        /// Construct a new empty schema schemas.
        /// </summary>
        public XmlSchemaSet() : this(new NameTable())
        {
        }

        /// <summary>
        /// Construct a new empty schema schemas with associated XmlNameTable.
        /// The XmlNameTable is used when loading schemas.
        /// </summary>
        public XmlSchemaSet(XmlNameTable nameTable)
        {
            ArgumentNullException.ThrowIfNull(nameTable);

            _nameTable = nameTable;
            _schemas = new SortedList();

            /*schemaLocations = new Dictionary<Uri, XmlSchema>();
            chameleonSchemas = new Dictionary<ChameleonKey, XmlSchema>();*/
            _schemaLocations = new Hashtable();
            _chameleonSchemas = new Hashtable();
            _targetNamespaces = new Hashtable();
            _internalEventHandler = new ValidationEventHandler(InternalValidationCallback);
            _eventHandler = _internalEventHandler;

            _readerSettings = new XmlReaderSettings();

            // we don't have to check XmlReaderSettings.EnableLegacyXmlSettings() here because the following
            // code will return same result either we are running on v4.5 or later
            if (_readerSettings.GetXmlResolver() == null)
            {
                // The created resolver will be used in the schema validation only
                _readerSettings.XmlResolver = new XmlUrlResolver();
                _readerSettings.IsXmlResolverSet = false;
            }

            _readerSettings.NameTable = nameTable;
            _readerSettings.DtdProcessing = DtdProcessing.Prohibit;

            _compilationSettings = new XmlSchemaCompilationSettings();
            _cachedCompiledInfo = new SchemaInfo();
            _compileAll = true;
        }


        //Public Properties
        /// <summary>
        /// The default XmlNameTable used by the XmlSchemaSet when loading new schemas.
        /// </summary>
        public XmlNameTable NameTable
        {
            get { return _nameTable; }
        }

        public event ValidationEventHandler ValidationEventHandler
        {
            add
            {
                _eventHandler -= _internalEventHandler;
                _eventHandler += value;
                if (_eventHandler == null)
                {
                    _eventHandler = _internalEventHandler;
                }
            }
            remove
            {
                _eventHandler -= value;
                if (_eventHandler == null)
                {
                    _eventHandler = _internalEventHandler;
                }
            }
        }

        /// <summary>
        /// IsCompiled is true when the schema set is in compiled state.
        /// </summary>
        public bool IsCompiled
        {
            get
            {
                return _isCompiled;
            }
        }

        public XmlResolver? XmlResolver
        {
            set
            {
                _readerSettings.XmlResolver = value;
            }
        }

        public XmlSchemaCompilationSettings CompilationSettings
        {
            get
            {
                return _compilationSettings;
            }
            set
            {
                _compilationSettings = value;
            }
        }

        /// <summary>
        /// Returns the count of schemas in the set.
        /// </summary>
        public int Count
        {
            get
            {
                return _schemas.Count;
            }
        }

        public XmlSchemaObjectTable GlobalElements
        {
            get
            {
                if (elements == null)
                {
                    elements = new XmlSchemaObjectTable();
                }
                return elements;
            }
        }

        public XmlSchemaObjectTable GlobalAttributes
        {
            get
            {
                if (attributes == null)
                {
                    attributes = new XmlSchemaObjectTable();
                }
                return attributes;
            }
        }

        public XmlSchemaObjectTable GlobalTypes
        {
            get
            {
                if (schemaTypes == null)
                {
                    schemaTypes = new XmlSchemaObjectTable();
                }
                return schemaTypes;
            }
        }

        internal XmlSchemaObjectTable SubstitutionGroups
        {
            get
            {
                if (substitutionGroups == null)
                {
                    substitutionGroups = new XmlSchemaObjectTable();
                }
                return substitutionGroups;
            }
        }

        /// <summary>
        /// Table of all types extensions
        /// </summary>
        internal Hashtable SchemaLocations
        {
            get
            {
                return _schemaLocations;
            }
        }

        /// <summary>
        /// Table of all types extensions
        /// </summary>
        internal XmlSchemaObjectTable TypeExtensions
        {
            get
            {
                if (_typeExtensions == null)
                {
                    _typeExtensions = new XmlSchemaObjectTable();
                }
                return _typeExtensions;
            }
        }
        //Public Methods

        /// <summary>
        /// Add the schema located by the given URL into the schema schemas.
        /// If the given schema references other namespaces, the schemas for those other
        /// namespaces are NOT automatically loaded.
        /// </summary>
        public XmlSchema? Add(string? targetNamespace, string schemaUri)
        {
            if (schemaUri == null || schemaUri.Length == 0)
            {
                throw new ArgumentNullException(nameof(schemaUri));
            }

            if (targetNamespace != null)
            {
                targetNamespace = XmlComplianceUtil.CDataNormalize(targetNamespace);
            }

            XmlSchema? schema = null;
            lock (InternalSyncObject)
            {
                //Check if schema from url has already been added
                XmlResolver? tempResolver = _readerSettings.GetXmlResolver();
                if (tempResolver == null)
                {
                    tempResolver = new XmlUrlResolver();
                }
                Uri tempSchemaUri = tempResolver.ResolveUri(null, schemaUri);
                if (IsSchemaLoaded(tempSchemaUri, targetNamespace, out schema))
                {
                    return schema;
                }
                else
                {
                    //Url already not processed; Load SOM from url
                    XmlReader reader = XmlReader.Create(schemaUri, _readerSettings);
                    try
                    {
                        schema = Add(targetNamespace, ParseSchema(targetNamespace, reader));
                        while (reader.Read()) ; // wellformness check;
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
            }

            return schema;
        }

        /// <summary>
        /// Add the given schema into the schema schemas.
        /// If the given schema references other namespaces, the schemas for those
        /// other namespaces are NOT automatically loaded.
        /// </summary>
        public XmlSchema? Add(string? targetNamespace, XmlReader schemaDocument)
        {
            ArgumentNullException.ThrowIfNull(schemaDocument);

            if (targetNamespace != null)
            {
                targetNamespace = XmlComplianceUtil.CDataNormalize(targetNamespace);
            }
            lock (InternalSyncObject)
            {
                XmlSchema? schema = null;
                Uri schemaUri = new Uri(schemaDocument.BaseURI!, UriKind.RelativeOrAbsolute);
                if (IsSchemaLoaded(schemaUri, targetNamespace, out schema))
                {
                    return schema;
                }
                else
                {
                    DtdProcessing dtdProcessing = _readerSettings.DtdProcessing;
                    SetDtdProcessing(schemaDocument);
                    schema = Add(targetNamespace, ParseSchema(targetNamespace, schemaDocument));
                    _readerSettings.DtdProcessing = dtdProcessing; //reset dtdProcessing setting
                    return schema;
                }
            }
        }


        /// <summary>
        /// Adds all the namespaces defined in the given schemas
        /// (including their associated schemas) to this schemas.
        /// </summary>
        public void Add(XmlSchemaSet schemas)
        {
            ArgumentNullException.ThrowIfNull(schemas);

            if (this == schemas)
            {
                return;
            }
            bool thisLockObtained = false;
            bool schemasLockObtained = false;
            try
            {
                SpinWait spinner = default;
                while (true)
                {
                    Monitor.TryEnter(InternalSyncObject, ref thisLockObtained);
                    if (thisLockObtained)
                    {
                        Monitor.TryEnter(schemas.InternalSyncObject, ref schemasLockObtained);
                        if (schemasLockObtained)
                        {
                            break;
                        }
                        else
                        {
                            Monitor.Exit(InternalSyncObject); //Give up this lock and try both again
                            thisLockObtained = false;
                            spinner.SpinOnce(); //Let the thread that holds the lock run
                            continue;
                        }
                    }
                }

                XmlSchema? currentSchema;
                if (schemas.IsCompiled)
                {
                    CopyFromCompiledSet(schemas);
                }
                else
                {
                    bool remove = false;
                    string? tns = null;
                    foreach (XmlSchema? schema in schemas.SortedSchemas.Values)
                    {
                        tns = schema!.TargetNamespace;
                        if (tns == null)
                        {
                            tns = string.Empty;
                        }
                        if (_schemas.ContainsKey(schema.SchemaId) || FindSchemaByNSAndUrl(schema.BaseUri, tns, null) != null)
                        { //Do not already existing url
                            continue;
                        }
                        currentSchema = Add(schema.TargetNamespace, schema);
                        if (currentSchema == null)
                        {
                            remove = true;
                            break;
                        }
                    }
                    //Remove all from the set if even one schema in the passed in set is not preprocessed.
                    if (remove)
                    {
                        foreach (XmlSchema? schema in schemas.SortedSchemas.Values)
                        { //Remove all previously added schemas from the set
                            _schemas.Remove(schema!.SchemaId); //Might remove schema that was already there and was not added thru this operation
                            _schemaLocations.Remove(schema.BaseUri!);
                        }
                    }
                }
            }
            finally
            { //release locks on sets
                if (thisLockObtained)
                {
                    Monitor.Exit(InternalSyncObject);
                }
                if (schemasLockObtained)
                {
                    Monitor.Exit(schemas.InternalSyncObject);
                }
            }
        }

        public XmlSchema? Add(XmlSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);

            lock (InternalSyncObject)
            {
                if (_schemas.ContainsKey(schema.SchemaId))
                {
                    return schema;
                }

                return Add(schema.TargetNamespace, schema);
            }
        }

        public XmlSchema? Remove(XmlSchema schema)
        {
            return Remove(schema, true);
        }

        public bool RemoveRecursive(XmlSchema schemaToRemove)
        {
            ArgumentNullException.ThrowIfNull(schemaToRemove);

            if (!_schemas.ContainsKey(schemaToRemove.SchemaId))
            {
                return false;
            }

            lock (InternalSyncObject)
            { //Need to lock here so that remove cannot be called while the set is being compiled
                if (_schemas.ContainsKey(schemaToRemove.SchemaId))
                { //Need to check again
                    //Build disallowedNamespaces list
                    Hashtable disallowedNamespaces = new Hashtable();
                    disallowedNamespaces.Add(GetTargetNamespace(schemaToRemove), schemaToRemove);
                    string importedNS;
                    for (int i = 0; i < schemaToRemove.ImportedNamespaces.Count; i++)
                    {
                        importedNS = (string)schemaToRemove.ImportedNamespaces[i]!;
                        if (disallowedNamespaces[importedNS] == null)
                        {
                            disallowedNamespaces.Add(importedNS, importedNS);
                        }
                    }

                    //Removal list is all schemas imported by this schema directly or indirectly
                    //Need to check if other schemas in the set import schemaToRemove / any of its imports
                    ArrayList needToCheckSchemaList = new ArrayList();
                    XmlSchema? mainSchema;
                    for (int i = 0; i < _schemas.Count; i++)
                    {
                        mainSchema = (XmlSchema)_schemas.GetByIndex(i)!;
                        if (mainSchema == schemaToRemove ||
                            schemaToRemove.ImportedSchemas.Contains(mainSchema))
                        {
                            continue;
                        }
                        needToCheckSchemaList.Add(mainSchema);
                    }

                    mainSchema = null;
                    for (int i = 0; i < needToCheckSchemaList.Count; i++)
                    { //Perf: Not using nested foreach here
                        mainSchema = (XmlSchema)needToCheckSchemaList[i]!;

                        if (mainSchema.ImportedNamespaces.Count > 0)
                        {
                            foreach (string? tns in disallowedNamespaces.Keys)
                            {
                                if (mainSchema.ImportedNamespaces.Contains(tns))
                                {
                                    SendValidationEvent(new XmlSchemaException(SR.Sch_SchemaNotRemoved, string.Empty), XmlSeverityType.Warning);
                                    return false;
                                }
                            }
                        }
                    }

                    Remove(schemaToRemove, true);
                    for (int i = 0; i < schemaToRemove.ImportedSchemas.Count; ++i)
                    {
                        XmlSchema impSchema = (XmlSchema)schemaToRemove.ImportedSchemas[i]!;
                        Remove(impSchema, true);
                    }
                    return true;
                }
            }
            return false;
        }

        public bool Contains(string? targetNamespace)
        {
            if (targetNamespace == null)
            {
                targetNamespace = string.Empty;
            }

            return _targetNamespaces[targetNamespace] != null;
        }

        public bool Contains(XmlSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);

            return _schemas.ContainsValue(schema);
        }

        public void Compile()
        {
            if (_isCompiled)
            {
                return;
            }
            if (_schemas.Count == 0)
            {
                ClearTables(); //Clear any previously present compiled state left by calling just Remove() on the set
                _cachedCompiledInfo = new SchemaInfo();
                _isCompiled = true;
                _compileAll = false;
                return;
            }
            lock (InternalSyncObject)
            {
                if (!_isCompiled)
                { //Locking before checking isCompiled to avoid problems with double locking
                    Compiler compiler = new Compiler(_nameTable, _eventHandler, _schemaForSchema, _compilationSettings);
                    SchemaInfo newCompiledInfo = new SchemaInfo();
                    int schemaIndex = 0;
                    if (!_compileAll)
                    { //if we are not compiling everything again, Move the pre-compiled schemas to the compiler's tables
                        compiler.ImportAllCompiledSchemas(this);
                    }
                    try
                    { //First thing to do in the try block is to acquire locks since finally will try to release them.
                        //If we don't acquire the locks first, and an exception occurs in the code before the locking code, then Threading.SynchronizationLockException will be thrown
                        //when attempting to release it in the finally block
                        XmlSchema currentSchema;
                        XmlSchema xmlNSSchema = Preprocessor.GetBuildInSchema();
                        for (schemaIndex = 0; schemaIndex < _schemas.Count; schemaIndex++)
                        {
                            currentSchema = (XmlSchema)_schemas.GetByIndex(schemaIndex)!;

                            //Lock schema to be compiled
                            Monitor.Enter(currentSchema);
                            if (!currentSchema.IsPreprocessed)
                            {
                                SendValidationEvent(new XmlSchemaException(SR.Sch_SchemaNotPreprocessed, string.Empty), XmlSeverityType.Error);
                                _isCompiled = false;
                                return;
                            }
                            if (currentSchema.IsCompiledBySet)
                            {
                                if (!_compileAll)
                                {
                                    continue;
                                }
                                else if ((object)currentSchema == (object)xmlNSSchema)
                                { // prepare for xml namespace schema without cleanup
                                    compiler.Prepare(currentSchema, false);
                                    continue;
                                }
                            }
                            compiler.Prepare(currentSchema, true);
                        }

                        _isCompiled = compiler.Execute(this, newCompiledInfo);
                        if (_isCompiled)
                        {
                            if (!_compileAll)
                            {
                                newCompiledInfo.Add(_cachedCompiledInfo, _eventHandler); //Add all the items from the old to the new compiled object
                            }
                            _compileAll = false;
                            _cachedCompiledInfo = newCompiledInfo; //Replace the compiled info in the set after successful compilation
                        }
                    }
                    finally
                    {
                        //Release locks on all schemas
                        XmlSchema currentSchema;
                        if (schemaIndex == _schemas.Count)
                        {
                            schemaIndex--;
                        }
                        for (int i = schemaIndex; i >= 0; i--)
                        {
                            currentSchema = (XmlSchema)_schemas.GetByIndex(i)!;
                            if (currentSchema == Preprocessor.GetBuildInSchema())
                            { //dont re-set compiled flags for xml namespace schema
                                Monitor.Exit(currentSchema);
                                continue;
                            }

                            currentSchema.IsCompiledBySet = _isCompiled;
                            Monitor.Exit(currentSchema);
                        }
                    }
                }
            }
            return;
        }

        public XmlSchema Reprocess(XmlSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);

            // Due to bug 644477 - this method is tightly coupled (THE CODE IS BASICALLY COPIED) to Remove, Add and AddSchemaToSet
            // methods. If you change anything here *make sure* to update Remove/Add/AddSchemaToSet method(s) accordingly.
            // The only difference is that we don't touch .schemas collection here to not break a code like this:
            // foreach (XmlSchema s in schemaset.schemas) { schemaset.Reprocess(s); }
            // This is by purpose.
            if (!_schemas.ContainsKey(schema.SchemaId))
            {
                throw new ArgumentException(SR.Sch_SchemaDoesNotExist, nameof(schema));
            }
            XmlSchema originalSchema = schema;
            lock (InternalSyncObject)
            { //Lock set so that set cannot be compiled in another thread
                // This code is copied from method:
                // Remove(XmlSchema schema, bool forceCompile)
                // If you changed anything here go and change the same in Remove(XmlSchema schema, bool forceCompile) method
                #region Copied from Remove(XmlSchema schema, bool forceCompile)

                RemoveSchemaFromGlobalTables(schema);
                RemoveSchemaFromCaches(schema);
                if (schema.BaseUri != null)
                {
                    _schemaLocations.Remove(schema.BaseUri);
                }
                string tns = GetTargetNamespace(schema);
                if (Schemas(tns).Count == 0)
                { //This is the only schema for that namespace
                    _targetNamespaces.Remove(tns);
                }
                _isCompiled = false;
                _compileAll = true; //Force compilation of the whole set; This is when the set is not completely thread-safe

                #endregion //Copied from Remove(XmlSchema schema, bool forceCompile)


                // This code is copied from method:
                // Add(string targetNamespace, XmlSchema schema)
                // If you changed anything here go and change the same in Add(string targetNamespace, XmlSchema schema) method
                #region Copied from Add(string targetNamespace, XmlSchema schema)

                if (schema.ErrorCount != 0)
                { //Schema with parsing errors cannot be loaded
                    return originalSchema;
                }
                if (PreprocessSchema(ref schema, schema.TargetNamespace))
                { //No perf opt for already compiled schemas
                    // This code is copied from method:
                    // AddSchemaToSet(XmlSchema schema)
                    // If you changed anything here go and change the same in AddSchemaToSet(XmlSchema schema) method
                    #region Copied from AddSchemaToSet(XmlSchema schema)

                    //Add to targetNamespaces table
                    if (_targetNamespaces[tns] == null)
                    {
                        _targetNamespaces.Add(tns, tns);
                    }
                    if (_schemaForSchema == null && tns == XmlReservedNs.NsXs && schema.SchemaTypes[DatatypeImplementation.QnAnyType] != null)
                    { //it has xs:anyType
                        _schemaForSchema = schema;
                    }
                    for (int i = 0; i < schema.ImportedSchemas.Count; ++i)
                    {    //Once preprocessed external schemas property is set
                        XmlSchema s = (XmlSchema)schema.ImportedSchemas[i]!;
                        if (!_schemas.ContainsKey(s.SchemaId))
                        {
                            _schemas.Add(s.SchemaId, s);
                        }
                        tns = GetTargetNamespace(s);
                        if (_targetNamespaces[tns] == null)
                        {
                            _targetNamespaces.Add(tns, tns);
                        }
                        if (_schemaForSchema == null && tns == XmlReservedNs.NsXs && schema.SchemaTypes[DatatypeImplementation.QnAnyType] != null)
                        { //it has xs:anyType
                            _schemaForSchema = schema;
                        }
                    }
                    #endregion //Copied from AddSchemaToSet(XmlSchema schema)
                    return schema;
                }
                #endregion // Copied from Add(string targetNamespace, XmlSchema schema)

                return originalSchema;
            }
        }

        public void CopyTo(XmlSchema[] schemas, int index)
        {
            ArgumentNullException.ThrowIfNull(schemas);

            if (index < 0 || index > schemas.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(index));
            _schemas.Values.CopyTo(schemas, index);
        }

        public ICollection Schemas()
        {
            return _schemas.Values;
        }

        public ICollection Schemas(string? targetNamespace)
        {
            ArrayList tnsSchemas = new ArrayList();
            XmlSchema currentSchema;
            if (targetNamespace == null)
            {
                targetNamespace = string.Empty;
            }
            for (int i = 0; i < _schemas.Count; i++)
            {
                currentSchema = (XmlSchema)_schemas.GetByIndex(i)!;
                if (GetTargetNamespace(currentSchema) == targetNamespace)
                {
                    tnsSchemas.Add(currentSchema);
                }
            }
            return tnsSchemas;
        }

        //Internal Methods

        private XmlSchema? Add(string? targetNamespace, XmlSchema? schema)
        {
            // Due to bug 644477 - this method is tightly coupled (THE CODE IS BASICALLY COPIED) to Reprocess
            // method. If you change anything here *make sure* to update Reprocess method accordingly.

            if (schema == null || schema.ErrorCount != 0)
            { //Schema with parsing errors cannot be loaded
                return null;
            }

            // This code is copied to method:
            // Reprocess(XmlSchema schema)
            // If you changed anything here go and change the same in Reprocess(XmlSchema schema) method
            if (PreprocessSchema(ref schema, targetNamespace))
            { //No perf opt for already compiled schemas
                AddSchemaToSet(schema);
                _isCompiled = false;
                return schema;
            }

            return null;
        }

#if TRUST_COMPILE_STATE
        private void AddCompiledSchema(XmlSchema schema) {
            if (schema.IsCompiledBySet ) { //trust compiled state always if it is not a chameleon schema
                VerifyTables();
                SchemaInfo newCompiledInfo = new SchemaInfo();
                XmlSchemaObjectTable substitutionGroupsTable = null;
                if (!AddToCompiledInfo(schema, newCompiledInfo, ref substitutionGroupsTable)) { //Error while adding main schema
                    return null;
                }
                foreach (XmlSchema impSchema in schema.ImportedSchemas) {
                    if (!AddToCompiledInfo(impSchema, newCompiledInfo, ref substitutionGroupsTable)) { //Error while adding imports
                        return null;
                    }
                }
                newCompiledInfo.Add(cachedCompiledInfo, eventHandler); //Add existing compiled info
                cachedCompiledInfo = newCompiledInfo;
                if (substitutionGroupsTable != null) {
                    ProcessNewSubstitutionGroups(substitutionGroupsTable, true);
                }
                if (schemas.Count == 0) { //If its the first compiled schema being added, then set doesnt need to be compiled
                    isCompiled = true;
                    compileAll = false;
                }
                AddSchemaToSet(schema);
                return schema;
            }
        }

        private bool AddToCompiledInfo(XmlSchema schema, SchemaInfo newCompiledInfo, ref XmlSchemaObjectTable substTable) {
            //Add schema's compiled tables to the set
            if (schema.BaseUri != null && schemaLocations[schema.BaseUri] == null) { //Update schemaLocations table
                schemaLocations.Add(schema.BaseUri, schema);
            }

            foreach (XmlSchemaElement element in schema.Elements.Values) {
                if (!AddToTable(elements, element.QualifiedName, element)) {
                    RemoveSchemaFromGlobalTables(schema);
                    return false;
                }
                XmlQualifiedName head = element.SubstitutionGroup;
                if (!head.IsEmpty) {
                    if (substTable == null) {
                        substTable = new XmlSchemaObjectTable();
                    }
                    XmlSchemaSubstitutionGroup substitutionGroup = (XmlSchemaSubstitutionGroup)substTable[head];
                    if (substitutionGroup == null) {
                        substitutionGroup = new XmlSchemaSubstitutionGroup();
                        substitutionGroup.Examplar = head;
                        substTable.Add(head, substitutionGroup);
                    }
                    ArrayList members = substitutionGroup.Members;
                    if (!members.Contains(element)) { //Members might contain element if the same schema is included and imported through different paths. Imp, hence will be added to set directly
                        members.Add(element);
                    }
                }
            }
            foreach (XmlSchemaAttribute attribute in schema.Attributes.Values) {
                if (!AddToTable(attributes, attribute.QualifiedName, attribute)) {
                    RemoveSchemaFromGlobalTables(schema);
                    return false;
                }
            }
            foreach (XmlSchemaType schemaType in schema.SchemaTypes.Values) {
                if (!AddToTable(schemaTypes, schemaType.QualifiedName, schemaType)) {
                    RemoveSchemaFromGlobalTables(schema);
                    return false;
                }
            }
            schema.AddCompiledInfo(newCompiledInfo);

            return true;
        }
#endif

        //For use by the validator when loading schemaLocations in the instance
        internal void Add(string? targetNamespace, XmlReader reader, Hashtable validatedNamespaces)
        {
            ArgumentNullException.ThrowIfNull(reader);

            if (targetNamespace == null)
            {
                targetNamespace = string.Empty;
            }
            if (validatedNamespaces[targetNamespace] != null)
            {
                if (FindSchemaByNSAndUrl(new Uri(reader.BaseURI!, UriKind.RelativeOrAbsolute), targetNamespace, null) != null)
                {
                    return;
                }
                else
                {
                    throw new XmlSchemaException(SR.Sch_ComponentAlreadySeenForNS, targetNamespace);
                }
            }

            //Not locking set as this will not be accessible outside the validator
            XmlSchema? schema;
            if (IsSchemaLoaded(new Uri(reader.BaseURI!, UriKind.RelativeOrAbsolute), targetNamespace, out _))
            {
                return;
            }
            else
            { //top-level schema not present for same url
                schema = ParseSchema(targetNamespace, reader);

                //Store the previous locations
                DictionaryEntry[] oldLocations = new DictionaryEntry[_schemaLocations.Count];
                _schemaLocations.CopyTo(oldLocations, 0);

                //Add to set
                Add(targetNamespace, schema!);
                if (schema!.ImportedSchemas.Count > 0)
                { //Check imports
                    string? tns;
                    for (int i = 0; i < schema.ImportedSchemas.Count; ++i)
                    {
                        XmlSchema impSchema = (XmlSchema)schema.ImportedSchemas[i]!;
                        tns = impSchema.TargetNamespace;
                        if (tns == null)
                        {
                            tns = string.Empty;
                        }
                        if (validatedNamespaces[tns] != null && (FindSchemaByNSAndUrl(impSchema.BaseUri, tns, oldLocations) == null))
                        {
                            RemoveRecursive(schema);
                            throw new XmlSchemaException(SR.Sch_ComponentAlreadySeenForNS, tns);
                        }
                    }
                }
            }
        }

        internal XmlSchema? FindSchemaByNSAndUrl(Uri? schemaUri, string ns, DictionaryEntry[]? locationsTable)
        {
            if (schemaUri == null || schemaUri.OriginalString.Length == 0)
            {
                return null;
            }

            XmlSchema? schema = null;
            if (locationsTable == null)
            {
                schema = (XmlSchema)_schemaLocations[schemaUri]!;
            }
            else
            {
                for (int i = 0; i < locationsTable.Length; i++)
                {
                    if (schemaUri.Equals(locationsTable[i].Key))
                    {
                        schema = (XmlSchema)locationsTable[i].Value!;
                        break;
                    }
                }
            }

            if (schema != null)
            {
                Debug.Assert(ns != null);
                string tns = schema.TargetNamespace ?? string.Empty;
                if (tns == ns)
                {
                    return schema;
                }
                else if (tns.Length == 0)
                { //There could be a chameleon for same ns
                    // It is OK to pass in the schema we have found so far, since it must have the schemaUri we're looking for
                    // (we found it that way above) and it must be the original chameleon schema (the one without target ns)
                    // as we don't add the chameleon copies into the locations tables above.
                    Debug.Assert(schema.BaseUri!.Equals(schemaUri));
                    ChameleonKey cKey = new ChameleonKey(ns, schema);
                    schema = (XmlSchema)_chameleonSchemas[cKey]!; //Need not clone if a schema for that namespace already exists
                }
                else
                {
                    schema = null;
                }
            }

            return schema;
        }

        private void SetDtdProcessing(XmlReader reader)
        {
            if (reader.Settings != null)
            {
                _readerSettings.DtdProcessing = reader.Settings.DtdProcessing;
            }
            else
            {
                XmlTextReader? v1Reader = reader as XmlTextReader;
                if (v1Reader != null)
                {
                    _readerSettings.DtdProcessing = v1Reader.DtdProcessing;
                }
            }
        }

        private void AddSchemaToSet(XmlSchema schema)
        {
            // Due to bug 644477 - this method is tightly coupled (THE CODE IS BASICALLY COPIED) to Reprocess
            // method. If you change anything here *make sure* to update Reprocess method accordingly.

            _schemas.Add(schema.SchemaId, schema);
            //Add to targetNamespaces table

            // This code is copied to method:
            // Reprocess(XmlSchema schema)
            // If you changed anything here go and change the same in Reprocess(XmlSchema schema) method
            #region This code is copied to Reprocess(XmlSchema schema) method

            string tns = GetTargetNamespace(schema);
            if (_targetNamespaces[tns] == null)
            {
                _targetNamespaces.Add(tns, tns);
            }
            if (_schemaForSchema == null && tns == XmlReservedNs.NsXs && schema.SchemaTypes[DatatypeImplementation.QnAnyType] != null)
            { //it has xs:anyType
                _schemaForSchema = schema;
            }
            for (int i = 0; i < schema.ImportedSchemas.Count; ++i)
            {    //Once preprocessed external schemas property is set
                XmlSchema s = (XmlSchema)schema.ImportedSchemas[i]!;
                if (!_schemas.ContainsKey(s.SchemaId))
                {
                    _schemas.Add(s.SchemaId, s);
                }
                tns = GetTargetNamespace(s);
                if (_targetNamespaces[tns] == null)
                {
                    _targetNamespaces.Add(tns, tns);
                }
                if (_schemaForSchema == null && tns == XmlReservedNs.NsXs && schema.SchemaTypes[DatatypeImplementation.QnAnyType] != null)
                { //it has xs:anyType
                    _schemaForSchema = schema;
                }
            }

            #endregion // This code is copied to Reprocess(XmlSchema schema) method
        }

        private void ProcessNewSubstitutionGroups(XmlSchemaObjectTable substitutionGroupsTable, bool resolve)
        {
            foreach (XmlSchemaSubstitutionGroup? substGroup in substitutionGroupsTable.Values)
            {
                if (resolve)
                { //Resolve substitutionGroups within this schema
                    ResolveSubstitutionGroup(substGroup!, substitutionGroupsTable);
                }

                //Add or Merge new substitutionGroups with those that already exist in the set
                XmlQualifiedName head = substGroup!.Examplar;
                XmlSchemaSubstitutionGroup? oldSubstGroup = (XmlSchemaSubstitutionGroup?)substitutionGroups![head];
                if (oldSubstGroup != null)
                {
                    for (int i = 0; i < substGroup.Members.Count; ++i)
                    {
                        if (!oldSubstGroup.Members.Contains(substGroup.Members[i]))
                        {
                            oldSubstGroup.Members.Add(substGroup.Members[i]);
                        }
                    }
                }
                else
                {
                    AddToTable(substitutionGroups, head, substGroup);
                }
            }
        }

        private void ResolveSubstitutionGroup(XmlSchemaSubstitutionGroup substitutionGroup, XmlSchemaObjectTable substTable)
        {
            List<XmlSchemaElement>? newMembers = null;
            XmlSchemaElement headElement = (XmlSchemaElement)elements![substitutionGroup.Examplar]!;
            if (substitutionGroup.Members.Contains(headElement))
            {// already checked
                return;
            }
            for (int i = 0; i < substitutionGroup.Members.Count; ++i)
            {
                XmlSchemaElement element = (XmlSchemaElement)substitutionGroup.Members[i]!;

                //Chain to other head's that are members of this head's substGroup
                XmlSchemaSubstitutionGroup? g = (XmlSchemaSubstitutionGroup?)substTable[element.QualifiedName];
                if (g != null)
                {
                    ResolveSubstitutionGroup(g, substTable);
                    for (int j = 0; j < g.Members.Count; ++j)
                    {
                        XmlSchemaElement element1 = (XmlSchemaElement)g.Members[j]!;
                        if (element1 != element)
                        { //Exclude the head
                            if (newMembers == null)
                            {
                                newMembers = new List<XmlSchemaElement>();
                            }
                            newMembers.Add(element1);
                        }
                    }
                }
            }
            if (newMembers != null)
            {
                for (int i = 0; i < newMembers.Count; ++i)
                {
                    substitutionGroup.Members.Add(newMembers[i]);
                }
            }
            substitutionGroup.Members.Add(headElement);
        }

        internal XmlSchema? Remove(XmlSchema schema, bool forceCompile)
        {
            ArgumentNullException.ThrowIfNull(schema);

            // Due to bug 644477 - this method is tightly coupled (THE CODE IS BASICALLY COPIED) to Reprocess
            // method. If you change anything here *make sure* to update Reprocess method accordingly.
            lock (InternalSyncObject)
            { //Need to lock here so that remove cannot be called while the set is being compiled
                if (_schemas.ContainsKey(schema.SchemaId))
                {
                    // This code is copied to method:
                    // Reprocess(XmlSchema schema)
                    // If you changed anything here go and change the same in Reprocess(XmlSchema schema) method
                    #region This code is copied to Reprocess(XmlSchema schema) method

                    if (forceCompile)
                    {
                        RemoveSchemaFromGlobalTables(schema);
                        RemoveSchemaFromCaches(schema);
                    }
                    _schemas.Remove(schema.SchemaId);
                    if (schema.BaseUri != null)
                    {
                        _schemaLocations.Remove(schema.BaseUri);
                    }
                    string tns = GetTargetNamespace(schema);
                    if (Schemas(tns).Count == 0)
                    { //This is the only schema for that namespace
                        _targetNamespaces.Remove(tns);
                    }
                    if (forceCompile)
                    {
                        _isCompiled = false;
                        _compileAll = true; //Force compilation of the whole set; This is when the set is not completely thread-safe
                    }
                    return schema;

                    #endregion // This code is copied to Reprocess(XmlSchema schema) method
                }
            }

            return null;
        }

        private void ClearTables()
        {
            GlobalElements.Clear();
            GlobalAttributes.Clear();
            GlobalTypes.Clear();
            SubstitutionGroups.Clear();
            TypeExtensions.Clear();
        }

        internal bool PreprocessSchema(ref XmlSchema schema, string? targetNamespace)
        {
            Preprocessor prep = new Preprocessor(_nameTable, GetSchemaNames(_nameTable), _eventHandler, _compilationSettings);
            prep.XmlResolver = _readerSettings.GetXmlResolver_CheckConfig();
            prep.ReaderSettings = _readerSettings;
            prep.SchemaLocations = _schemaLocations;
            prep.ChameleonSchemas = _chameleonSchemas;
            bool hasErrors = prep.Execute(schema, targetNamespace, true);
            schema = prep.RootSchema!; //For any root level chameleon cloned
            return hasErrors;
        }

        internal XmlSchema? ParseSchema(string? targetNamespace, XmlReader reader)
        {
            XmlNameTable readerNameTable = reader.NameTable;
            SchemaNames schemaNames = GetSchemaNames(readerNameTable);
            Parser parser = new Parser(SchemaType.XSD, readerNameTable, schemaNames, _eventHandler);
            parser.XmlResolver = _readerSettings.GetXmlResolver_CheckConfig();
            SchemaType schemaType;
            try
            {
                schemaType = parser.Parse(reader, targetNamespace);
            }
            catch (XmlSchemaException e)
            {
                SendValidationEvent(e, XmlSeverityType.Error);
                return null;
            }
            return parser.XmlSchema;
        }

        internal void CopyFromCompiledSet(XmlSchemaSet otherSet)
        {
            XmlSchema currentSchema;
            SortedList copyFromList = otherSet.SortedSchemas;
            bool setIsCompiled = _schemas.Count == 0 ? true : false;
            ArrayList existingSchemas = new ArrayList();

            SchemaInfo newCompiledInfo = new SchemaInfo();
            Uri? baseUri;
            for (int i = 0; i < copyFromList.Count; i++)
            {
                currentSchema = (XmlSchema)copyFromList.GetByIndex(i)!;
                baseUri = currentSchema.BaseUri;
                if (_schemas.ContainsKey(currentSchema.SchemaId) || (baseUri != null && baseUri.OriginalString.Length != 0 && _schemaLocations[baseUri] != null))
                {
                    existingSchemas.Add(currentSchema);
                    continue;
                }
                _schemas.Add(currentSchema.SchemaId, currentSchema);
                if (baseUri != null && baseUri.OriginalString.Length != 0)
                {
                    _schemaLocations.Add(baseUri, currentSchema);
                }
                string tns = GetTargetNamespace(currentSchema);
                if (_targetNamespaces[tns] == null)
                {
                    _targetNamespaces.Add(tns, tns);
                }
            }

            VerifyTables();
            foreach (XmlSchemaElement? element in otherSet.GlobalElements.Values)
            {
                if (!AddToTable(elements!, element!.QualifiedName, element))
                {
                    goto RemoveAll;
                }
            }
            foreach (XmlSchemaAttribute? attribute in otherSet.GlobalAttributes.Values)
            {
                if (!AddToTable(attributes!, attribute!.QualifiedName, attribute))
                {
                    goto RemoveAll;
                }
            }
            foreach (XmlSchemaType? schemaType in otherSet.GlobalTypes.Values)
            {
                if (!AddToTable(schemaTypes!, schemaType!.QualifiedName, schemaType))
                {
                    goto RemoveAll;
                }
            }
            ProcessNewSubstitutionGroups(otherSet.SubstitutionGroups, false);

            newCompiledInfo.Add(_cachedCompiledInfo, _eventHandler); //Add all the items from the old to the new compiled object
            newCompiledInfo.Add(otherSet.CompiledInfo, _eventHandler);
            _cachedCompiledInfo = newCompiledInfo; //Replace the compiled info in the set after successful compilation
            if (setIsCompiled)
            {
                _isCompiled = true;
                _compileAll = false;
            }
            return;

        RemoveAll:
            foreach (XmlSchema? schemaToRemove in copyFromList.Values)
            {
                if (!existingSchemas.Contains(schemaToRemove))
                {
                    Remove(schemaToRemove!, false);
                }
            }

            foreach (XmlSchemaElement? elementToRemove in otherSet.GlobalElements.Values)
            {
                if (!existingSchemas.Contains((XmlSchema?)elementToRemove!.Parent))
                {
                    elements!.Remove(elementToRemove.QualifiedName);
                }
            }

            foreach (XmlSchemaAttribute? attributeToRemove in otherSet.GlobalAttributes.Values)
            {
                if (!existingSchemas.Contains((XmlSchema?)attributeToRemove!.Parent))
                {
                    attributes!.Remove(attributeToRemove.QualifiedName);
                }
            }

            foreach (XmlSchemaType? schemaTypeToRemove in otherSet.GlobalTypes.Values)
            {
                if (!existingSchemas.Contains((XmlSchema?)schemaTypeToRemove!.Parent))
                {
                    schemaTypes!.Remove(schemaTypeToRemove.QualifiedName);
                }
            }
        }

        internal SchemaInfo CompiledInfo
        {
            get
            {
                return _cachedCompiledInfo;
            }
        }

        internal XmlReaderSettings ReaderSettings
        {
            get
            {
                return _readerSettings;
            }
        }

        internal XmlResolver? GetResolver()
        {
            return _readerSettings.GetXmlResolver_CheckConfig();
        }

        internal ValidationEventHandler? GetEventHandler()
        {
            return _eventHandler;
        }

        internal SchemaNames GetSchemaNames(XmlNameTable nt)
        {
            if (_nameTable != nt)
            {
                return new SchemaNames(nt);
            }
            else
            {
                if (_schemaNames == null)
                {
                    _schemaNames = new SchemaNames(_nameTable);
                }
                return _schemaNames;
            }
        }

        internal bool IsSchemaLoaded(Uri schemaUri, string? targetNamespace, out XmlSchema? schema)
        {
            if (targetNamespace == null)
            {
                targetNamespace = string.Empty;
            }
            if (GetSchemaByUri(schemaUri, out schema))
            {
                if (_schemas.ContainsKey(schema.SchemaId) && (targetNamespace.Length == 0 || targetNamespace == schema.TargetNamespace))
                { //schema is present in set
                    //Schema found
                }
                else if (schema.TargetNamespace == null)
                { //If schema not in set or namespace doesnt match, then it might be a chameleon
                    XmlSchema? chameleonSchema = FindSchemaByNSAndUrl(schemaUri, targetNamespace, null);
                    if (chameleonSchema != null && _schemas.ContainsKey(chameleonSchema.SchemaId))
                    {
                        schema = chameleonSchema;
                    }
                    else
                    {
                        schema = Add(targetNamespace, schema);
                    }
                }
                else if (targetNamespace.Length != 0 && targetNamespace != schema.TargetNamespace)
                {
                    SendValidationEvent(new XmlSchemaException(SR.Sch_MismatchTargetNamespaceEx, new string[] { targetNamespace, schema.TargetNamespace }), XmlSeverityType.Error);
                    schema = null;
                }
                else
                {
                    //If here, schema not present in set but in loc and might be added in loc through an earlier include
                    //S.TNS != null && ( tns == null or tns == s.TNS)
                    AddSchemaToSet(schema);
                }

                return true; //Schema Found
            }

            return false;
        }

        internal bool GetSchemaByUri(Uri schemaUri, [NotNullWhen(true)] out XmlSchema? schema)
        {
            schema = null;
            if (schemaUri == null || schemaUri.OriginalString.Length == 0)
            {
                return false;
            }

            schema = (XmlSchema?)_schemaLocations[schemaUri];
            if (schema != null)
            {
                return true;
            }

            return false;
        }

        internal static string GetTargetNamespace(XmlSchema schema)
        {
            return schema.TargetNamespace ?? string.Empty;
        }

        internal SortedList SortedSchemas
        {
            get
            {
                return _schemas;
            }
        }

        //Private Methods
        private void RemoveSchemaFromCaches(XmlSchema schema)
        {
            //Remove From ChameleonSchemas and schemaLocations cache
            List<XmlSchema> reprocessList = new List<XmlSchema>();
            schema.GetExternalSchemasList(reprocessList, schema);
            for (int i = 0; i < reprocessList.Count; ++i)
            { //Remove schema from schemaLocations & chameleonSchemas tables
                if (reprocessList[i].BaseUri != null && reprocessList[i].BaseUri!.OriginalString.Length != 0)
                {
                    _schemaLocations.Remove(reprocessList[i].BaseUri!);
                }

                //Remove from chameleon table
                ICollection chameleonKeys = _chameleonSchemas.Keys;
                ArrayList removalList = new ArrayList();
                foreach (ChameleonKey? cKey in chameleonKeys)
                {
                    if (cKey!.chameleonLocation.Equals(reprocessList[i].BaseUri))
                    {
                        // The key will have the originalSchema set to null if the location was not empty
                        //   otherwise we need to care about it as there may be more chameleon schemas without
                        //   a base URI and we want to remove only those which were created as a clone of the one
                        //   we're removing.
                        if (cKey.originalSchema == null || Ref.ReferenceEquals(cKey.originalSchema, reprocessList[i]))
                        {
                            removalList.Add(cKey);
                        }
                    }
                }

                for (int j = 0; j < removalList.Count; ++j)
                {
                    _chameleonSchemas.Remove(removalList[j]!);
                }
            }
        }

        private void RemoveSchemaFromGlobalTables(XmlSchema schema)
        {
            if (_schemas.Count == 0)
            {
                return;
            }

            VerifyTables();
            foreach (XmlSchemaElement? elementToRemove in schema.Elements.Values)
            {
                XmlSchemaElement? elem = (XmlSchemaElement?)elements![elementToRemove!.QualifiedName];
                if (elem == elementToRemove)
                {
                    elements.Remove(elementToRemove.QualifiedName);
                }
            }

            foreach (XmlSchemaAttribute? attributeToRemove in schema.Attributes.Values)
            {
                XmlSchemaAttribute? attr = (XmlSchemaAttribute?)attributes![attributeToRemove!.QualifiedName];
                if (attr == attributeToRemove)
                {
                    attributes.Remove(attributeToRemove.QualifiedName);
                }
            }

            foreach (XmlSchemaType? schemaTypeToRemove in schema.SchemaTypes.Values)
            {
                XmlSchemaType? schemaType = (XmlSchemaType?)schemaTypes![schemaTypeToRemove!.QualifiedName];
                if (schemaType == schemaTypeToRemove)
                {
                    schemaTypes.Remove(schemaTypeToRemove.QualifiedName);
                }
            }
        }

        private bool AddToTable(XmlSchemaObjectTable table, XmlQualifiedName qname, XmlSchemaObject item)
        {
            if (qname.Name.Length == 0)
            {
                return true;
            }

            XmlSchemaObject? existingObject = (XmlSchemaObject?)table[qname];
            if (existingObject != null)
            {
                if (existingObject == item || existingObject.SourceUri == item.SourceUri)
                {
                    return true;
                }

                string code = string.Empty;
                if (item is XmlSchemaComplexType)
                {
                    code = SR.Sch_DupComplexType;
                }
                else if (item is XmlSchemaSimpleType)
                {
                    code = SR.Sch_DupSimpleType;
                }
                else if (item is XmlSchemaElement)
                {
                    code = SR.Sch_DupGlobalElement;
                }
                else if (item is XmlSchemaAttribute)
                {
                    if (qname.Namespace == XmlReservedNs.NsXml)
                    {
                        XmlSchema schemaForXmlNS = Preprocessor.GetBuildInSchema();
                        XmlSchemaObject builtInAttribute = schemaForXmlNS.Attributes[qname]!;
                        if (existingObject == builtInAttribute)
                        { //replace built-in one
                            table.Insert(qname, item);
                            return true;
                        }
                        else if (item == builtInAttribute)
                        { //trying to overwrite customer's component with built-in, ignore built-in
                            return true;
                        }
                    }

                    code = SR.Sch_DupGlobalAttribute;
                }

                SendValidationEvent(new XmlSchemaException(code, qname.ToString()), XmlSeverityType.Error);
                return false;
            }
            else
            {
                table.Add(qname, item);
                return true;
            }
        }

        private void VerifyTables()
        {
            if (elements == null)
            {
                elements = new XmlSchemaObjectTable();
            }
            if (attributes == null)
            {
                attributes = new XmlSchemaObjectTable();
            }
            if (schemaTypes == null)
            {
                schemaTypes = new XmlSchemaObjectTable();
            }
            if (substitutionGroups == null)
            {
                substitutionGroups = new XmlSchemaObjectTable();
            }
        }

        private void InternalValidationCallback(object? sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Error)
            {
                throw e.Exception;
            }
        }

        private void SendValidationEvent(XmlSchemaException e, XmlSeverityType severity)
        {
            if (_eventHandler != null)
            {
                _eventHandler(this, new ValidationEventArgs(e, severity));
            }
            else
            {
                throw e;
            }
        }
    };
}
