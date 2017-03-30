// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System;
    using System.Threading;
    using System.Collections;
    using System.Xml.Schema;
    using System.Runtime.Versioning;


    /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection"]/*' />
    /// <devdoc>
    ///    <para>The XmlSchemaCollection contains a set of namespace URI's.
    ///       Each namespace also have an associated private data cache
    ///       corresponding to the XML-Data Schema or W3C XML Schema.
    ///       The XmlSchemaCollection will able to load XSD and XDR schemas,
    ///       and compile them into an internal "cooked schema representation".
    ///       The Validate method then uses this internal representation for
    ///       efficient runtime validation of any given subtree.</para>
    /// </devdoc>
    [Obsolete("Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation. http://go.microsoft.com/fwlink/?linkid=14202")]
    public sealed class XmlSchemaCollection : ICollection
    {
        private Hashtable _collection;
        private XmlNameTable _nameTable;
        private SchemaNames _schemaNames;
        private object _wLock;
        private bool _isThreadSafe = true;
        private ValidationEventHandler _validationEventHandler = null;
        private XmlResolver _xmlResolver = null;


        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.XmlSchemaCollection"]/*' />
        /// <devdoc>
        ///    <para>Construct a new empty schema collection.</para>
        /// </devdoc>
        public XmlSchemaCollection() : this(new NameTable())
        {
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.XmlSchemaCollection1"]/*' />
        /// <devdoc>
        ///    <para>Construct a new empty schema collection with associated XmlNameTable.
        ///       The XmlNameTable is used when loading schemas</para>
        /// </devdoc>
        public XmlSchemaCollection(XmlNameTable nametable)
        {
            if (nametable == null)
            {
                throw new ArgumentNullException(nameof(nametable));
            }
            _nameTable = nametable;
            _collection = Hashtable.Synchronized(new Hashtable());
            _xmlResolver = null;
            _isThreadSafe = true;
            if (_isThreadSafe)
            {
                _wLock = new object();
            }
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.Count"]/*' />
        /// <devdoc>
        ///    <para>Returns the number of namespaces defined in this collection
        ///       (whether or not there is an actual schema associated with those namespaces or not).</para>
        /// </devdoc>
        public int Count
        {
            get { return _collection.Count; }
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.NameTable"]/*' />
        /// <devdoc>
        ///    <para>The default XmlNameTable used by the XmlSchemaCollection when loading new schemas.</para>
        /// </devdoc>
        public XmlNameTable NameTable
        {
            get { return _nameTable; }
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.ValidationEventHandler"]/*' />
        public event ValidationEventHandler ValidationEventHandler
        {
            add { _validationEventHandler += value; }
            remove { _validationEventHandler -= value; }
        }

        internal XmlResolver XmlResolver
        {
            set
            {
                _xmlResolver = value;
            }
        }


        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.Add"]/*' />
        /// <devdoc>
        ///    <para>Add the schema located by the given URL into the schema collection.
        ///       If the given schema references other namespaces, the schemas for those other
        ///       namespaces are NOT automatically loaded.</para>
        /// </devdoc>
        public XmlSchema Add(string ns, string uri)
        {
            if (uri == null || uri.Length == 0)
                throw new ArgumentNullException(nameof(uri));
            XmlTextReader reader = new XmlTextReader(uri, _nameTable);
            reader.XmlResolver = _xmlResolver;

            XmlSchema schema = null;
            try
            {
                schema = Add(ns, reader, _xmlResolver);
                while (reader.Read()) ;// wellformness check
            }
            finally
            {
                reader.Close();
            }
            return schema;
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.Add4"]/*' />
        public XmlSchema Add(String ns, XmlReader reader)
        {
            return Add(ns, reader, _xmlResolver);
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.Add1"]/*' />
        /// <devdoc>
        ///    <para>Add the given schema into the schema collection.
        ///       If the given schema references other namespaces, the schemas for those
        ///       other namespaces are NOT automatically loaded.</para>
        /// </devdoc>
        public XmlSchema Add(String ns, XmlReader reader, XmlResolver resolver)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
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
                SchemaInfo xdrSchema = parser.XdrSchema;
                return Add(ns, parser.XdrSchema, null, true, resolver);
            }
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.Add2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchema Add(XmlSchema schema)
        {
            return Add(schema, _xmlResolver);
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.Add5"]/*' />
	    public XmlSchema Add(XmlSchema schema, XmlResolver resolver)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            SchemaInfo schemaInfo = new SchemaInfo();
            schemaInfo.SchemaType = SchemaType.XSD;
            return Add(schema.TargetNamespace, schemaInfo, schema, true, resolver);
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.Add3"]/*' />
        /// <devdoc>
        ///    <para>Adds all the namespaces defined in the given collection
        ///       (including their associated schemas) to this collection.</para>
        /// </devdoc>
        public void Add(XmlSchemaCollection schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (this == schema)
                return;
            IDictionaryEnumerator enumerator = schema._collection.GetEnumerator();
            while (enumerator.MoveNext())
            {
                XmlSchemaCollectionNode node = (XmlSchemaCollectionNode)enumerator.Value;
                Add(node.NamespaceURI, node);
            }
        }


        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.this"]/*' />
        /// <devdoc>
        ///    <para>Looks up the schema by it's associated namespace URI</para>
        /// </devdoc>
        public XmlSchema this[string ns]
        {
            get
            {
                XmlSchemaCollectionNode node = (XmlSchemaCollectionNode)_collection[(ns != null) ? ns : string.Empty];
                return (node != null) ? node.Schema : null;
            }
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.Contains"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(XmlSchema schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }
            return this[schema.TargetNamespace] != null;
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.Contains1"]/*' />
        public bool Contains(string ns)
        {
            return _collection[(ns != null) ? ns : string.Empty] != null;
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.IEnumerable.GetEnumerator"]/*' />
        /// <internalonly/>
        /// <devdoc>
        /// Get a IEnumerator of the XmlSchemaCollection.
        /// </devdoc>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new XmlSchemaCollectionEnumerator(_collection);
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.GetEnumerator"]/*' />
        public XmlSchemaCollectionEnumerator GetEnumerator()
        {
            return new XmlSchemaCollectionEnumerator(_collection);
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.ICollection.CopyTo"]/*' />
        /// <internalonly/>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
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

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(XmlSchema[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            for (XmlSchemaCollectionEnumerator e = this.GetEnumerator(); e.MoveNext();)
            {
                XmlSchema schema = e.Current;
                if (schema != null)
                {
                    if (index == array.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    array[index++] = e.Current;
                }
            }
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.ICollection.IsSynchronized"]/*' />
        /// <internalonly/>
        bool ICollection.IsSynchronized
        {
            get { return true; }
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.ICollection.SyncRoot"]/*' />
        /// <internalonly/>
        object ICollection.SyncRoot
        {
            get { return this; }
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollection.ICollection.Count"]/*' />
        /// <internalonly/>
        int ICollection.Count
        {
            get { return _collection.Count; }
        }

        internal SchemaInfo GetSchemaInfo(string ns)
        {
            XmlSchemaCollectionNode node = (XmlSchemaCollectionNode)_collection[(ns != null) ? ns : string.Empty];
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

        internal XmlSchema Add(string ns, SchemaInfo schemaInfo, XmlSchema schema, bool compile)
        {
            return Add(ns, schemaInfo, schema, compile, _xmlResolver);
        }

        private XmlSchema Add(string ns, SchemaInfo schemaInfo, XmlSchema schema, bool compile, XmlResolver resolver)
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
                ns = NameTable.Add(ns); //Added without checking for ns == null, since XDR cannot have null namespace
            }
            if (errorCount == 0)
            {
                XmlSchemaCollectionNode node = new XmlSchemaCollectionNode();
                node.NamespaceURI = ns;
                node.SchemaInfo = schemaInfo;
                node.Schema = schema;
                Add(ns, node);
                return schema;
            }
            return null;
        }

        private void AddNonThreadSafe(string ns, XmlSchemaCollectionNode node)
        {
            if (_collection[ns] != null)
                _collection.Remove(ns);
            _collection.Add(ns, node);
        }

        private void Add(string ns, XmlSchemaCollectionNode node)
        {
            if (_isThreadSafe)
            {
                lock (_wLock)
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

        internal ValidationEventHandler EventHandler
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
        private String _namespaceUri;
        private SchemaInfo _schemaInfo;
        private XmlSchema _schema;

        internal String NamespaceURI
        {
            get { return _namespaceUri; }
            set { _namespaceUri = value; }
        }

        internal SchemaInfo SchemaInfo
        {
            get { return _schemaInfo; }
            set { _schemaInfo = value; }
        }

        internal XmlSchema Schema
        {
            get { return _schema; }
            set { _schema = value; }
        }
    }


    /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollectionEnumerator"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public sealed class XmlSchemaCollectionEnumerator : IEnumerator
    {
        private IDictionaryEnumerator _enumerator;

        internal XmlSchemaCollectionEnumerator(Hashtable collection)
        {
            _enumerator = collection.GetEnumerator();
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollectionEnumerator.IEnumerator.Reset"]/*' />
        /// <internalonly/>
        void IEnumerator.Reset()
        {
            _enumerator.Reset();
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollectionEnumerator.IEnumerator.MoveNext"]/*' />
        /// <internalonly/>
        bool IEnumerator.MoveNext()
        {
            return _enumerator.MoveNext();
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollectionEnumerator.MoveNext"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollectionEnumerator.IEnumerator.Current"]/*' />
        /// <internalonly/>
        object IEnumerator.Current
        {
            get { return this.Current; }
        }

        /// <include file='doc\XmlSchemaCollection.uex' path='docs/doc[@for="XmlSchemaCollectionEnumerator.Current"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchema Current
        {
            get
            {
                XmlSchemaCollectionNode n = (XmlSchemaCollectionNode)_enumerator.Value;
                if (n != null)
                    return n.Schema;
                else
                    return null;
            }
        }

        internal XmlSchemaCollectionNode CurrentNode
        {
            get
            {
                XmlSchemaCollectionNode n = (XmlSchemaCollectionNode)_enumerator.Value;
                return n;
            }
        }
    }
}
