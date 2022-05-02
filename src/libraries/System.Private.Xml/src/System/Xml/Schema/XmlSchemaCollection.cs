// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Collections;
    using System.Xml.Schema;
    using System.Runtime.Versioning;


    /// <summary>
    /// The XmlSchemaCollection contains a set of namespace URI's.
    /// Each namespace also have an associated private data cache
    /// corresponding to the XML-Data Schema or W3C XML Schema.
    /// The XmlSchemaCollection will able to load XSD and XDR schemas,
    /// and compile them into an internal "cooked schema representation".
    /// The Validate method then uses this internal representation for
    /// efficient runtime validation of any given subtree.
    /// </summary>
    [Obsolete("XmlSchemaCollection has been deprecated. Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation instead.")]
    public sealed class XmlSchemaCollection : ICollection
    {
        private readonly Hashtable _collection;
        private readonly XmlNameTable _nameTable;
        private SchemaNames? _schemaNames;
        private readonly object? _wLock;
        private readonly bool _isThreadSafe = true;
        private ValidationEventHandler? _validationEventHandler;
        private XmlResolver? _xmlResolver;


        /// <summary>
        /// Construct a new empty schema collection.
        /// </summary>
        public XmlSchemaCollection() : this(new NameTable())
        {
        }

        /// <summary>
        /// Construct a new empty schema collection with associated XmlNameTable.
        /// The XmlNameTable is used when loading schemas.
        /// </summary>
        public XmlSchemaCollection(XmlNameTable nametable!!)
        {
            _nameTable = nametable;
            _collection = Hashtable.Synchronized(new Hashtable());
            _xmlResolver = null;
            _isThreadSafe = true;
            if (_isThreadSafe)
            {
                _wLock = new object();
            }
        }

        /// <summary>
        /// Returns the number of namespaces defined in this collection
        /// (whether or not there is an actual schema associated with those namespaces or not).
        /// </summary>
        public int Count
        {
            get { return _collection.Count; }
        }

        /// <summary>
        /// The default XmlNameTable used by the XmlSchemaCollection when loading new schemas.
        /// </summary>
        public XmlNameTable NameTable
        {
            get { return _nameTable; }
        }

        public event ValidationEventHandler ValidationEventHandler
        {
            add { _validationEventHandler += value; }
            remove { _validationEventHandler -= value; }
        }

        internal XmlResolver? XmlResolver
        {
            set
            {
                _xmlResolver = value;
            }
        }


        /// <summary>
        /// Add the schema located by the given URL into the schema collection.
        /// If the given schema references other namespaces, the schemas for those other
        /// namespaces are NOT automatically loaded.
        /// </summary>
        public XmlSchema? Add(string? ns, [StringSyntax(StringSyntaxAttribute.Uri)] string uri)
        {
            if (uri == null || uri.Length == 0)
                throw new ArgumentNullException(nameof(uri));
            XmlTextReader reader = new XmlTextReader(uri, _nameTable);
            reader.XmlResolver = _xmlResolver;

            XmlSchema? schema = null;
            try
            {
                schema = Add(ns, reader, _xmlResolver);
                while (reader.Read()) ; // wellformness check
            }
            finally
            {
                reader.Close();
            }
            return schema;
        }

        public XmlSchema? Add(string? ns, XmlReader reader)
        {
            return Add(ns, reader, _xmlResolver);
        }

        /// <summary>
        /// Add the given schema into the schema collection.
        /// If the given schema references other namespaces, the schemas for those
        /// other namespaces are NOT automatically loaded.
        /// </summary>
        public XmlSchema? Add(string? ns, XmlReader reader!!, XmlResolver? resolver)
        {
            XmlNameTable readerNameTable = reader.NameTable;
            SchemaInfo schemaInfo = new SchemaInfo();

            Parser parser = new Parser(SchemaType.None, readerNameTable, GetSchemaNames(readerNameTable), _validationEventHandler);
            parser.XmlResolver = resolver;
            SchemaType schemaType;
            try
            {
                schemaType = parser.Parse(reader, ns);
            }
            catch (XmlSchemaException e)
            {
                SendValidationEvent(e);
                return null;
            }

            if (schemaType == SchemaType.XSD)
            {
                schemaInfo.SchemaType = SchemaType.XSD;
                return Add(ns, schemaInfo, parser.XmlSchema, true, resolver);
            }
            else
            {
                return Add(ns, parser.XdrSchema!, null, true, resolver);
            }
        }

        public XmlSchema? Add(XmlSchema schema)
        {
            return Add(schema, _xmlResolver);
        }

        public XmlSchema? Add(XmlSchema schema!!, XmlResolver? resolver)
        {
            SchemaInfo schemaInfo = new SchemaInfo();
            schemaInfo.SchemaType = SchemaType.XSD;
            return Add(schema.TargetNamespace, schemaInfo, schema, true, resolver);
        }

        /// <summary>
        /// Adds all the namespaces defined in the given collection
        /// (including their associated schemas) to this collection.
        /// </summary>
        public void Add(XmlSchemaCollection schema!!)
        {
            if (this == schema)
                return;
            IDictionaryEnumerator enumerator = schema._collection.GetEnumerator();
            while (enumerator.MoveNext())
            {
                XmlSchemaCollectionNode node = (XmlSchemaCollectionNode)enumerator.Value!;
                Add(node!.NamespaceURI!, node);
            }
        }


        /// <summary>
        /// Looks up the schema by its associated namespace URI
        /// </summary>
        public XmlSchema? this[string? ns]
        {
            get
            {
                XmlSchemaCollectionNode? node = (XmlSchemaCollectionNode?)_collection[(ns != null) ? ns : string.Empty];
                return (node != null) ? node.Schema : null;
            }
        }

        public bool Contains(XmlSchema schema!!)
        {
            return this[schema.TargetNamespace] != null;
        }

        public bool Contains(string? ns)
        {
            return _collection[(ns != null) ? ns : string.Empty] != null;
        }

        /// <summary>
        /// Get a IEnumerator of the XmlSchemaCollection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new XmlSchemaCollectionEnumerator(_collection);
        }

        public XmlSchemaCollectionEnumerator GetEnumerator()
        {
            return new XmlSchemaCollectionEnumerator(_collection);
        }

        void ICollection.CopyTo(Array array!!, int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            for (XmlSchemaCollectionEnumerator e = this.GetEnumerator(); e.MoveNext();)
            {
                if (index == array.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                array.SetValue(e.Current, index++);
            }
        }

        public void CopyTo(XmlSchema[] array!!, int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            for (XmlSchemaCollectionEnumerator e = this.GetEnumerator(); e.MoveNext();)
            {
                XmlSchema? schema = e.Current;
                if (schema != null)
                {
                    if (index == array.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    array[index++] = e.Current!;
                }
            }
        }

        bool ICollection.IsSynchronized
        {
            get { return true; }
        }

        object ICollection.SyncRoot
        {
            get { return this; }
        }

        int ICollection.Count
        {
            get { return _collection.Count; }
        }

        internal SchemaInfo? GetSchemaInfo(string? ns)
        {
            XmlSchemaCollectionNode? node = (XmlSchemaCollectionNode?)_collection[(ns != null) ? ns : string.Empty];
            return (node != null) ? node.SchemaInfo : null;
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

        internal XmlSchema? Add(string? ns, SchemaInfo schemaInfo, XmlSchema? schema, bool compile)
        {
            return Add(ns, schemaInfo, schema, compile, _xmlResolver);
        }

        private XmlSchema? Add(string? ns, SchemaInfo schemaInfo, XmlSchema? schema, bool compile, XmlResolver? resolver)
        {
            int errorCount = 0;
            if (schema != null)
            {
                if (schema.ErrorCount == 0 && compile)
                {
                    if (!schema.CompileSchema(this, resolver, schemaInfo, ns, _validationEventHandler, _nameTable, true))
                    {
                        errorCount = 1;
                    }

                    ns = schema.TargetNamespace == null ? string.Empty : schema.TargetNamespace;
                }
                errorCount += schema.ErrorCount;
            }
            else
            {
                errorCount += schemaInfo.ErrorCount;
                //ns = ns == null? string.Empty : NameTable.Add(ns);
                ns = NameTable.Add(ns!); //Added without checking for ns == null, since XDR cannot have null namespace
            }
            if (errorCount == 0)
            {
                XmlSchemaCollectionNode node = new XmlSchemaCollectionNode();
                node.NamespaceURI = ns;
                node.SchemaInfo = schemaInfo;
                node.Schema = schema;
                Add(ns!, node);
                return schema;
            }

            return null;
        }

        private void AddNonThreadSafe(string ns, XmlSchemaCollectionNode? node)
        {
            if (_collection[ns] != null)
                _collection.Remove(ns);
            _collection.Add(ns, node);
        }

        private void Add(string ns, XmlSchemaCollectionNode? node)
        {
            if (_isThreadSafe)
            {
                lock (_wLock!)
                {
                    AddNonThreadSafe(ns, node);
                }
            }
            else
            {
                AddNonThreadSafe(ns, node);
            }
        }

        private void SendValidationEvent(XmlSchemaException e)
        {
            if (_validationEventHandler != null)
            {
                _validationEventHandler(this, new ValidationEventArgs(e));
            }
            else
            {
                throw e;
            }
        }

        internal ValidationEventHandler? EventHandler
        {
            get
            {
                return _validationEventHandler;
            }
            set
            {
                _validationEventHandler = value;
            }
        }
    };

    internal sealed class XmlSchemaCollectionNode
    {
        private string? _namespaceUri;
        private SchemaInfo? _schemaInfo;
        private XmlSchema? _schema;

        internal string? NamespaceURI
        {
            get { return _namespaceUri; }
            set { _namespaceUri = value; }
        }

        internal SchemaInfo? SchemaInfo
        {
            get { return _schemaInfo; }
            set { _schemaInfo = value; }
        }

        internal XmlSchema? Schema
        {
            get { return _schema; }
            set { _schema = value; }
        }
    }

    public sealed class XmlSchemaCollectionEnumerator : IEnumerator
    {
        private readonly IDictionaryEnumerator _enumerator;

        internal XmlSchemaCollectionEnumerator(Hashtable collection)
        {
            _enumerator = collection.GetEnumerator();
        }

        void IEnumerator.Reset()
        {
            _enumerator.Reset();
        }

        bool IEnumerator.MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        object? IEnumerator.Current
        {
            get { return this.Current; }
        }

        public XmlSchema? Current
        {
            get
            {
                XmlSchemaCollectionNode? n = (XmlSchemaCollectionNode?)_enumerator.Value;
                if (n != null)
                    return n.Schema;
                else
                    return null;
            }
        }

        internal XmlSchemaCollectionNode? CurrentNode
        {
            get
            {
                XmlSchemaCollectionNode? n = (XmlSchemaCollectionNode?)_enumerator.Value;
                return n;
            }
        }
    }
}
