// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System;
    using System.Globalization;
    using System.ComponentModel;
    using System.Xml.Serialization;
    using System.Xml.Schema;
    using System.Diagnostics;
    using System.Threading;
    using System.Security;
    using System.Net;
    using System.Reflection;
    using System.Diagnostics.CodeAnalysis;

    public class XmlSchemas : CollectionBase, IEnumerable<XmlSchema>
    {
        private XmlSchemaSet? _schemaSet;
        private Hashtable? _references;
        private SchemaObjectCache? _cache; // cached schema top-level items
        private bool _shareTypes;
        private Hashtable? _mergedSchemas;
        internal Hashtable delayedSchemas = new Hashtable();
        private bool _isCompiled;
        private static volatile XmlSchema? s_xsd;
        private static volatile XmlSchema? s_xml;

        public XmlSchema this[int index]
        {
            get { return (XmlSchema)List[index]!; }
            set { List[index] = value; }
        }

        public XmlSchema? this[string? ns]
        {
            get
            {
                IList values = (IList)SchemaSet.Schemas(ns);
                if (values.Count == 0)
                    return null;
                if (values.Count == 1)
                    return (XmlSchema?)values[0];

                throw new InvalidOperationException(SR.Format(SR.XmlSchemaDuplicateNamespace, ns));
            }
        }

        public IList GetSchemas(string? ns)
        {
            return (IList)SchemaSet.Schemas(ns);
        }

        internal SchemaObjectCache Cache
        {
            get
            {
                if (_cache == null)
                    _cache = new SchemaObjectCache();
                return _cache;
            }
        }

        internal Hashtable MergedSchemas
        {
            get
            {
                if (_mergedSchemas == null)
                    _mergedSchemas = new Hashtable();
                return _mergedSchemas;
            }
        }

        internal Hashtable References
        {
            get
            {
                if (_references == null)
                    _references = new Hashtable();
                return _references;
            }
        }

        internal XmlSchemaSet SchemaSet
        {
            get
            {
                if (_schemaSet == null)
                {
                    _schemaSet = new XmlSchemaSet();
                    _schemaSet.XmlResolver = null;
                    _schemaSet.ValidationEventHandler += new ValidationEventHandler(IgnoreCompileErrors);
                }
                return _schemaSet;
            }
        }
        internal int Add(XmlSchema schema, bool delay)
        {
            if (delay)
            {
                if (delayedSchemas[schema] == null)
                    delayedSchemas.Add(schema, schema);
                return -1;
            }
            else
            {
                return Add(schema);
            }
        }

        public int Add(XmlSchema schema)
        {
            if (List.Contains(schema))
                return List.IndexOf(schema);
            return List.Add(schema);
        }

        public int Add(XmlSchema schema, Uri? baseUri)
        {
            if (List.Contains(schema))
                return List.IndexOf(schema);
            if (baseUri != null)
                schema.BaseUri = baseUri;
            return List.Add(schema);
        }

        public void Add(XmlSchemas schemas)
        {
            foreach (XmlSchema schema in schemas)
            {
                Add(schema);
            }
        }

        public void AddReference(XmlSchema schema)
        {
            References[schema] = schema;
        }

        public void Insert(int index, XmlSchema schema)
        {
            List.Insert(index, schema);
        }

        public int IndexOf(XmlSchema schema)
        {
            return List.IndexOf(schema);
        }

        public bool Contains(XmlSchema schema)
        {
            return List.Contains(schema);
        }

        public bool Contains(string? targetNamespace)
        {
            return SchemaSet.Contains(targetNamespace);
        }

        public void Remove(XmlSchema schema)
        {
            List.Remove(schema);
        }

        public void CopyTo(XmlSchema[] array, int index)
        {
            List.CopyTo(array, index);
        }

        protected override void OnInsert(int index, object? value)
        {
            AddName((XmlSchema)value!);
        }

        protected override void OnRemove(int index, object? value)
        {
            RemoveName((XmlSchema)value!);
        }

        protected override void OnClear()
        {
            _schemaSet = null;
        }

        protected override void OnSet(int index, object? oldValue, object? newValue)
        {
            RemoveName((XmlSchema)oldValue!);
            AddName((XmlSchema)newValue!);
        }

        private void AddName(XmlSchema schema)
        {
            if (_isCompiled) throw new InvalidOperationException(SR.XmlSchemaCompiled);
            if (SchemaSet.Contains(schema))
                SchemaSet.Reprocess(schema);
            else
            {
                Prepare(schema);
                SchemaSet.Add(schema);
            }
        }

        private static void Prepare(XmlSchema schema)
        {
            // need to remove illegal <import> externals;
            ArrayList removes = new ArrayList();
            string? ns = schema.TargetNamespace;
            foreach (XmlSchemaExternal external in schema.Includes)
            {
                if (external is XmlSchemaImport)
                {
                    if (ns == ((XmlSchemaImport)external).Namespace)
                    {
                        removes.Add(external);
                    }
                }
            }
            foreach (XmlSchemaObject o in removes)
            {
                schema.Includes.Remove(o);
            }
        }

        private void RemoveName(XmlSchema schema)
        {
            SchemaSet.Remove(schema);
        }

        public object? Find(XmlQualifiedName name, Type type)
        {
            return Find(name, type, true);
        }
        internal object? Find(XmlQualifiedName name, Type type, bool checkCache)
        {
            if (!IsCompiled)
            {
                foreach (XmlSchema schema in List)
                {
                    Preprocess(schema);
                }
            }
            IList values = (IList)SchemaSet.Schemas(name.Namespace);
            if (values == null) return null;

            foreach (XmlSchema schema in values)
            {
                Preprocess(schema);

                XmlSchemaObject? ret = null;
                if (typeof(XmlSchemaType).IsAssignableFrom(type))
                {
                    ret = schema.SchemaTypes[name];
                    if (ret == null || !type.IsAssignableFrom(ret.GetType()))
                    {
                        continue;
                    }
                }
                else if (type == typeof(XmlSchemaGroup))
                {
                    ret = schema.Groups[name];
                }
                else if (type == typeof(XmlSchemaAttributeGroup))
                {
                    ret = schema.AttributeGroups[name];
                }
                else if (type == typeof(XmlSchemaElement))
                {
                    ret = schema.Elements[name];
                }
                else if (type == typeof(XmlSchemaAttribute))
                {
                    ret = schema.Attributes[name];
                }
                else if (type == typeof(XmlSchemaNotation))
                {
                    ret = schema.Notations[name];
                }
#if DEBUG
                else
                {
                    // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                    throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "XmlSchemas.Find: Invalid object type " + type.FullName));
                }
#endif

                if (ret != null && _shareTypes && checkCache && !IsReference(ret))
                    ret = Cache.AddItem(ret, name, this);
                if (ret != null)
                {
                    return ret;
                }
            }
            return null;
        }

        IEnumerator<XmlSchema> IEnumerable<XmlSchema>.GetEnumerator()
        {
            return new XmlSchemaEnumerator(this);
        }

        internal static void Preprocess(XmlSchema schema)
        {
            if (!schema.IsPreprocessed)
            {
                try
                {
                    XmlNameTable nameTable = new System.Xml.NameTable();
                    Preprocessor prep = new Preprocessor(nameTable, new SchemaNames(nameTable), null);
                    prep.SchemaLocations = new Hashtable();
                    prep.Execute(schema, schema.TargetNamespace, false);
                }
                catch (XmlSchemaException e)
                {
                    throw CreateValidationException(e, e.Message);
                }
            }
        }

        public static bool IsDataSet(XmlSchema schema)
        {
            foreach (XmlSchemaObject o in schema.Items)
            {
                if (o is XmlSchemaElement)
                {
                    XmlSchemaElement e = (XmlSchemaElement)o;
                    if (e.UnhandledAttributes != null)
                    {
                        foreach (XmlAttribute a in e.UnhandledAttributes)
                        {
                            if (a.LocalName == "IsDataSet" && a.NamespaceURI == "urn:schemas-microsoft-com:xml-msdata")
                            {
                                // currently the msdata:IsDataSet uses its own format for the boolean values
                                if (a.Value == "True" || a.Value == "true" || a.Value == "1") return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        [RequiresUnreferencedCode("calls Merge")]
        private void Merge(XmlSchema schema)
        {
            if (MergedSchemas[schema] != null)
                return;
            IList originals = (IList)SchemaSet.Schemas(schema.TargetNamespace);
            if (originals != null && originals.Count > 0)
            {
                MergedSchemas.Add(schema, schema);
                Merge(originals, schema);
            }
            else
            {
                Add(schema);
                MergedSchemas.Add(schema, schema);
            }
        }

        private static void AddImport(IList schemas, string? ns)
        {
            foreach (XmlSchema s in schemas)
            {
                bool add = true;
                foreach (XmlSchemaExternal external in s.Includes)
                {
                    if (external is XmlSchemaImport && ((XmlSchemaImport)external).Namespace == ns)
                    {
                        add = false;
                        break;
                    }
                }
                if (add)
                {
                    XmlSchemaImport import = new XmlSchemaImport();
                    import.Namespace = ns;
                    s.Includes.Add(import);
                }
            }
        }

        [RequiresUnreferencedCode("Calls MergeFailedMessage")]
        private void Merge(IList originals, XmlSchema schema)
        {
            foreach (XmlSchema s in originals)
            {
                if (schema == s)
                {
                    return;
                }
            }

            foreach (XmlSchemaExternal external in schema.Includes)
            {
                if (external is XmlSchemaImport)
                {
                    external.SchemaLocation = null;
                    if (external.Schema != null)
                    {
                        Merge(external.Schema);
                    }
                    else
                    {
                        AddImport(originals, ((XmlSchemaImport)external).Namespace);
                    }
                }
                else
                {
                    if (external.Schema == null)
                    {
                        // we do not process includes or redefines by the schemaLocation
                        if (external.SchemaLocation != null)
                        {
                            throw new InvalidOperationException(SR.Format(SR.XmlSchemaIncludeLocation, this.GetType().Name, external.SchemaLocation));
                        }
                    }
                    else
                    {
                        external.SchemaLocation = null;
                        Merge(originals, external.Schema);
                    }
                }
            }

            // bring all included items to the parent schema;
            bool[] matchedItems = new bool[schema.Items.Count];
            int count = 0;
            for (int i = 0; i < schema.Items.Count; i++)
            {
                XmlSchemaObject o = schema.Items[i];
                XmlSchemaObject? dest = Find(o, originals);
                if (dest != null)
                {
                    if (!Cache.Match(dest, o, _shareTypes))
                    {
                        throw new InvalidOperationException(MergeFailedMessage(o, dest, schema.TargetNamespace));
                    }
                    matchedItems[i] = true;
                    count++;
                }
            }
            if (count != schema.Items.Count)
            {
                XmlSchema destination = (XmlSchema)originals[0]!;
                for (int i = 0; i < schema.Items.Count; i++)
                {
                    if (!matchedItems[i])
                    {
                        destination.Items.Add(schema.Items[i]);
                    }
                }
                destination.IsPreprocessed = false;
                Preprocess(destination);
            }
        }

        private static string? ItemName(XmlSchemaObject o)
        {
            if (o is XmlSchemaNotation)
            {
                return ((XmlSchemaNotation)o).Name;
            }
            else if (o is XmlSchemaGroup)
            {
                return ((XmlSchemaGroup)o).Name;
            }
            else if (o is XmlSchemaElement)
            {
                return ((XmlSchemaElement)o).Name;
            }
            else if (o is XmlSchemaType)
            {
                return ((XmlSchemaType)o).Name;
            }
            else if (o is XmlSchemaAttributeGroup)
            {
                return ((XmlSchemaAttributeGroup)o).Name;
            }
            else if (o is XmlSchemaAttribute)
            {
                return ((XmlSchemaAttribute)o).Name;
            }
            return null;
        }

        internal static XmlQualifiedName GetParentName(XmlSchemaObject item)
        {
            while (item.Parent != null)
            {
                if (item.Parent is XmlSchemaType)
                {
                    XmlSchemaType type = (XmlSchemaType)item.Parent;
                    if (type.Name != null && type.Name.Length != 0)
                    {
                        return type.QualifiedName;
                    }
                }
                item = item.Parent;
            }
            return XmlQualifiedName.Empty;
        }

        [return: NotNullIfNotNull("o")]
        private static string? GetSchemaItem(XmlSchemaObject? o, string? ns, string? details)
        {
            if (o == null)
            {
                return null;
            }
            while (o.Parent != null && !(o.Parent is XmlSchema))
            {
                o = o.Parent;
            }
            if (ns == null || ns.Length == 0)
            {
                XmlSchemaObject tmp = o;
                while (tmp.Parent != null)
                {
                    tmp = tmp.Parent;
                }
                if (tmp is XmlSchema)
                {
                    ns = ((XmlSchema)tmp).TargetNamespace;
                }
            }
            string? item;
            if (o is XmlSchemaNotation)
            {
                item = SR.Format(SR.XmlSchemaNamedItem, ns, "notation", ((XmlSchemaNotation)o).Name, details);
            }
            else if (o is XmlSchemaGroup)
            {
                item = SR.Format(SR.XmlSchemaNamedItem, ns, "group", ((XmlSchemaGroup)o).Name, details);
            }
            else if (o is XmlSchemaElement)
            {
                XmlSchemaElement e = ((XmlSchemaElement)o);
                if (e.Name == null || e.Name.Length == 0)
                {
                    XmlQualifiedName parentName = XmlSchemas.GetParentName(o);
                    // Element reference '{0}' declared in schema type '{1}' from namespace '{2}'
                    item = SR.Format(SR.XmlSchemaElementReference, e.RefName.ToString(), parentName.Name, parentName.Namespace);
                }
                else
                {
                    item = SR.Format(SR.XmlSchemaNamedItem, ns, "element", e.Name, details);
                }
            }
            else if (o is XmlSchemaType)
            {
                item = SR.Format(SR.XmlSchemaNamedItem, ns, o.GetType() == typeof(XmlSchemaSimpleType) ? "simpleType" : "complexType", ((XmlSchemaType)o).Name, null);
            }
            else if (o is XmlSchemaAttributeGroup)
            {
                item = SR.Format(SR.XmlSchemaNamedItem, ns, "attributeGroup", ((XmlSchemaAttributeGroup)o).Name, details);
            }
            else if (o is XmlSchemaAttribute)
            {
                XmlSchemaAttribute a = ((XmlSchemaAttribute)o);
                if (a.Name == null || a.Name.Length == 0)
                {
                    XmlQualifiedName parentName = XmlSchemas.GetParentName(o);
                    // Attribure reference '{0}' declared in schema type '{1}' from namespace '{2}'
                    return SR.Format(SR.XmlSchemaAttributeReference, a.RefName.ToString(), parentName.Name, parentName.Namespace);
                }
                else
                {
                    item = SR.Format(SR.XmlSchemaNamedItem, ns, "attribute", a.Name, details);
                }
            }
            else if (o is XmlSchemaContent)
            {
                XmlQualifiedName parentName = XmlSchemas.GetParentName(o);
                // Check content definition of schema type '{0}' from namespace '{1}'. {2}
                item = SR.Format(SR.XmlSchemaContentDef, parentName.Name, parentName.Namespace, null);
            }
            else if (o is XmlSchemaExternal)
            {
                string itemType = o is XmlSchemaImport ? "import" : o is XmlSchemaInclude ? "include" : o is XmlSchemaRedefine ? "redefine" : o.GetType().Name;
                item = SR.Format(SR.XmlSchemaItem, ns, itemType, details);
            }
            else if (o is XmlSchema)
            {
                item = SR.Format(SR.XmlSchema, ns, details);
            }
            else
            {
                item = SR.Format(SR.XmlSchemaNamedItem, ns, o.GetType().Name, null, details);
            }

            return item;
        }

        [RequiresUnreferencedCode("Creates XmlSerializer")]
        private static string Dump(XmlSchemaObject o)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            XmlSerializer s = new XmlSerializer(o.GetType());
            StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
            XmlWriter xmlWriter = XmlWriter.Create(sw, settings);
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("xs", XmlSchema.Namespace);
            s.Serialize(xmlWriter, o, ns);
            return sw.ToString();
        }

        [RequiresUnreferencedCode("calls Dump")]
        private static string MergeFailedMessage(XmlSchemaObject src, XmlSchemaObject dest, string? ns)
        {
            string err = SR.Format(SR.XmlSerializableMergeItem, ns, GetSchemaItem(src, ns, null));
            err += $"{Environment.NewLine}{Dump(src)}{Environment.NewLine}{Dump(dest)}";
            return err;
        }

        internal static XmlSchemaObject? Find(XmlSchemaObject o, IList originals)
        {
            string? name = ItemName(o);
            if (name == null)
                return null;

            Type type = o.GetType();

            foreach (XmlSchema s in originals)
            {
                foreach (XmlSchemaObject item in s.Items)
                {
                    if (item.GetType() == type && name == ItemName(item))
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        public bool IsCompiled
        {
            get { return _isCompiled; }
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public void Compile(ValidationEventHandler? handler, bool fullCompile)
        {
            if (_isCompiled)
                return;

            foreach (XmlSchema s in delayedSchemas.Values)
                Merge(s);
            delayedSchemas.Clear();

            if (fullCompile)
            {
                _schemaSet = new XmlSchemaSet();
                _schemaSet.XmlResolver = null;
                _schemaSet.ValidationEventHandler += handler;

                foreach (XmlSchema s in References.Values)
                    _schemaSet.Add(s);
                int schemaCount = _schemaSet.Count;

                foreach (XmlSchema s in List)
                {
                    if (!SchemaSet.Contains(s))
                    {
                        _schemaSet.Add(s);
                        schemaCount++;
                    }
                }

                if (!SchemaSet.Contains(XmlSchema.Namespace))
                {
                    AddReference(XsdSchema);
                    _schemaSet.Add(XsdSchema);
                    schemaCount++;
                }

                if (!SchemaSet.Contains(XmlReservedNs.NsXml))
                {
                    AddReference(XmlSchema);
                    _schemaSet.Add(XmlSchema);
                    schemaCount++;
                }
                _schemaSet.Compile();
                _schemaSet.ValidationEventHandler -= handler;
                _isCompiled = _schemaSet.IsCompiled && schemaCount == _schemaSet.Count;
            }
            else
            {
                try
                {
                    XmlNameTable nameTable = new System.Xml.NameTable();
                    Preprocessor prep = new Preprocessor(nameTable, new SchemaNames(nameTable), null);
                    prep.XmlResolver = null;
                    prep.SchemaLocations = new Hashtable();
                    prep.ChameleonSchemas = new Hashtable();
                    foreach (XmlSchema schema in SchemaSet.Schemas())
                    {
                        prep.Execute(schema, schema.TargetNamespace, true);
                    }
                }
                catch (XmlSchemaException e)
                {
                    throw CreateValidationException(e, e.Message);
                }
            }
        }

        internal static Exception CreateValidationException(XmlSchemaException exception, string message)
        {
            XmlSchemaObject? source = exception.SourceSchemaObject;
            if (exception.LineNumber == 0 && exception.LinePosition == 0)
            {
                throw new InvalidOperationException(GetSchemaItem(source, null, message), exception);
            }
            else
            {
                string? ns = null;
                if (source != null)
                {
                    while (source.Parent != null)
                    {
                        source = source.Parent;
                    }
                    if (source is XmlSchema)
                    {
                        ns = ((XmlSchema)source).TargetNamespace;
                    }
                }
                throw new InvalidOperationException(SR.Format(SR.XmlSchemaSyntaxErrorDetails, ns, message, exception.LineNumber, exception.LinePosition), exception);
            }
        }

        internal static void IgnoreCompileErrors(object? sender, ValidationEventArgs args)
        {
            return;
        }

        internal static XmlSchema XsdSchema
        {
            get
            {
                if (s_xsd == null)
                {
                    s_xsd = CreateFakeXsdSchema(XmlSchema.Namespace, "schema");
                }
                return s_xsd;
            }
        }

        internal static XmlSchema XmlSchema
        {
            get
            {
                if (s_xml == null)
                {
                    s_xml = XmlSchema.Read(new StringReader(xmlSchema), null)!;
                }
                return s_xml;
            }
        }

        private static XmlSchema CreateFakeXsdSchema(string ns, string name)
        {
            /* Create fake xsd schema to fool the XmlSchema.Compiler
                <xsd:schema targetNamespace="http://www.w3.org/2001/XMLSchema" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <xsd:element name="schema">
                    <xsd:complexType />
                  </xsd:element>
                </xsd:schema>
            */
            XmlSchema schema = new XmlSchema();
            schema.TargetNamespace = ns;
            XmlSchemaElement element = new XmlSchemaElement();
            element.Name = name;
            XmlSchemaComplexType type = new XmlSchemaComplexType();
            element.SchemaType = type;
            schema.Items.Add(element);
            return schema;
        }

        [RequiresUnreferencedCode("calls GenerateSchemaGraph")]
        internal void SetCache(SchemaObjectCache cache, bool shareTypes)
        {
            _shareTypes = shareTypes;
            _cache = cache;
            if (shareTypes)
            {
                cache.GenerateSchemaGraph(this);
            }
        }

        internal bool IsReference(XmlSchemaObject type)
        {
            XmlSchemaObject parent = type;
            while (parent.Parent != null)
            {
                parent = parent.Parent;
            }
            return References.Contains(parent);
        }

        internal const string xmlSchema = @"<?xml version='1.0' encoding='UTF-8' ?>
<xs:schema targetNamespace='http://www.w3.org/XML/1998/namespace' xmlns:xs='http://www.w3.org/2001/XMLSchema' xml:lang='en'>
 <xs:attribute name='lang' type='xs:language'/>
 <xs:attribute name='space'>
  <xs:simpleType>
   <xs:restriction base='xs:NCName'>
    <xs:enumeration value='default'/>
    <xs:enumeration value='preserve'/>
   </xs:restriction>
  </xs:simpleType>
 </xs:attribute>
 <xs:attribute name='base' type='xs:anyURI'/>
 <xs:attribute name='id' type='xs:ID' />
 <xs:attributeGroup name='specialAttrs'>
  <xs:attribute ref='xml:base'/>
  <xs:attribute ref='xml:lang'/>
  <xs:attribute ref='xml:space'/>
 </xs:attributeGroup>
</xs:schema>";
    }

    public class XmlSchemaEnumerator : IEnumerator<XmlSchema>, System.Collections.IEnumerator
    {
        private readonly XmlSchemas _list;
        private int _idx;
        private readonly int _end;

        public XmlSchemaEnumerator(XmlSchemas list)
        {
            _list = list;
            _idx = -1;
            _end = list.Count - 1;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (_idx >= _end)
                return false;

            _idx++;
            return true;
        }

        public XmlSchema Current
        {
            get { return _list[_idx]; }
        }

        object System.Collections.IEnumerator.Current
        {
            get { return _list[_idx]; }
        }

        void System.Collections.IEnumerator.Reset()
        {
            _idx = -1;
        }
    }
}
