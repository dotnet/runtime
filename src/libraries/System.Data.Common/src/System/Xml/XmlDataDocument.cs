// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.XPath;

namespace System.Xml
{
    /// <summary>
    /// Represents an entire document. An XmlDataDocument can contain XML
    /// data or relational data (DataSet).
    /// </summary>
    [Obsolete("XmlDataDocument has been deprecated and is not supported.")]
    public class XmlDataDocument : XmlDocument
    {
        private const string RequiresUnreferencedCodeMessage = "XmlDataDocument is used for serialization and deserialization. Members from serialized types may be trimmed if not referenced directly.";
        private DataSet _dataSet;

        private DataSetMapper _mapper;
        internal Hashtable _pointers;         // Hastable w/ all pointer objects used by this XmlDataDocument. Hashtable are guaranteed to work OK w/ one writer and mutiple readers, so as long as we guarantee
                                              // that there is at most one thread in AddPointer we are OK.
        private int _countAddPointer;    // Approximate count of how many times AddPointer was called since the last time we removed the unused pointer objects from pointers hashtable.
        private ArrayList _columnChangeList;
        private DataRowState _rollbackState;

        private bool _fBoundToDataSet;       // true if our permanent event listeners are registered to receive DataSet events
        private bool _fBoundToDocument;      // true if our permanent event listeners are registered to receive XML events. Note that both fBoundToDataSet and fBoundToDataSet should be both true or false.
        private bool _fDataRowCreatedSpecial;    // true if our special event listener is registered to receive DataRowCreated events. Note that we either have special listeners subsribed or permanent ones (i.e. fDataRowCreatedSpecial and fBoundToDocument/fBoundToDataSet cannot be both true).
        private bool _ignoreXmlEvents;       // true if XML events should not be processed
        private bool _ignoreDataSetEvents;   // true if DataSet events should not be processed
        private bool _isFoliationEnabled;    // true if we should create and reveal the virtual nodes, false if we should reveal only the physical stored nodes
        private bool _optimizeStorage;       // false if we should only have foilated regions.
        private ElementState _autoFoliationState;    // When XmlBoundElement will foliate because of member functions, this will contain the foliation mode: usually this is
                                                     // ElementState.StrongFoliation, however when foliation occurs due to DataDocumentNavigator operations (InsertNode for example),
                                                     // it is usually ElementState.WeakFoliation
        private bool _fAssociateDataRow;     // if true, CreateElement will create and associate data rows w/ the newly created XmlBoundElement.
                                             // If false, then CreateElement will just create the XmlBoundElement nodes. This is usefull for Loading case,
                                             // when CreateElement is called by DOM.
        private object _foliationLock;
        internal const string XSI_NIL = "xsi:nil";
        internal const string XSI = "xsi";
        private bool _bForceExpandEntity;
        internal XmlAttribute _attrXml;
        internal bool _bLoadFromDataSet;
        internal bool _bHasXSINIL;

        internal void AddPointer(IXmlDataVirtualNode pointer)
        {
            Debug.Assert(_pointers.ContainsValue(pointer) == false);
            lock (_pointers)
            {
                _countAddPointer++;
                if (_countAddPointer >= 5)
                {   // 5 is choosed to be small enough to not affect perf, but high enough so we will not scan all the time
                    ArrayList al = new ArrayList();
                    foreach (DictionaryEntry entry in _pointers)
                    {
                        IXmlDataVirtualNode? temp = (IXmlDataVirtualNode?)(entry.Value);
                        Debug.Assert(temp != null);
                        if (!temp.IsInUse())
                            al.Add(temp);
                    }
                    for (int i = 0; i < al.Count; i++)
                    {
                        _pointers.Remove(al[i]!);
                    }
                    _countAddPointer = 0;
                }
                _pointers[pointer] = pointer;
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        internal void AssertPointerPresent(IXmlDataVirtualNode pointer)
        {
#if DEBUG
            object? val = _pointers[pointer];
            if (val != (object)pointer)
                Debug.Fail("Pointer not present");
#endif
        }
        // This function attaches the DataSet to XmlDataDocument
        // We also register a special listener (OnDataRowCreatedSpecial) to DataSet, so we know when we should setup all regular listeners (OnDataRowCreated, OnColumnChanging, etc).
        // We need to do this because of the following scenario:
        //  - XmlDataDocument doc = new XmlDataDocument();
        //  - DataSet ds = doc.DataSet;     // doc.DataSet creates a data-set, however does not sets-up the regular listeners.
        //  - ds.ReadXmlSchema();           // since there are regular listeners in doc that track ds schema changes, doc does not know about the new tables/columns/etc
        //  - ds.ReadXmlData();             // ds is now filled, however doc has no content (since there were no listeners for the new created DataRow's)
        // We can set-up listeners and track each change in schema, but it is more perf-friendly to do it laizily, all at once, when the first DataRow is created
        // (we rely on the fact that DataRowCreated is a DataSet wide event, rather than a DataTable event)
        [MemberNotNull(nameof(_dataSet))]
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void AttachDataSet(DataSet ds)
        {
            // You should not have already an associated dataset
            Debug.Assert(_dataSet == null);
            Debug.Assert(ds != null);
            if (ds.FBoundToDocument)
                throw new ArgumentException(SR.DataDom_MultipleDataSet);
            ds.FBoundToDocument = true;
            _dataSet = ds;
            // Register the special listener to catch the first DataRow event(s)
            BindSpecialListeners();
        }

        // after loading, all detached DataRows are synchronized with the xml tree and inserted to their tables
        // or after setting the innerxml, synchronize the rows and if created new and detached, will be inserted.
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void SyncRows(DataRow? parentRow, XmlNode node, bool fAddRowsToTable)
        {
            XmlBoundElement? be = node as XmlBoundElement;
            if (be != null)
            {
                DataRow? r = be.Row;
                if (r != null && be.ElementState == ElementState.Defoliated)
                    return; //no need of syncRow

                if (r != null)
                {
                    // get all field values.
                    SynchronizeRowFromRowElement(be);

                    // defoliate if possible
                    be.ElementState = ElementState.WeakFoliation;
                    DefoliateRegion(be);

                    if (parentRow != null)
                        SetNestedParentRow(r, parentRow);
                    if (fAddRowsToTable && r.RowState == DataRowState.Detached)
                        r.Table.Rows.Add(r);
                    parentRow = r;
                }
            }

            // Attach all rows from children nodes
            for (XmlNode? child = node.FirstChild; child != null; child = child.NextSibling)
                SyncRows(parentRow, child, fAddRowsToTable);
        }

        // All detached DataRows are synchronized with the xml tree and inserted to their tables.
        // Synchronize the rows and if created new and detached, will be inserted.
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void SyncTree(XmlNode node)
        {
            XmlBoundElement? be = null;
            _mapper.GetRegion(node, out be);
            DataRow? parentRow = null;
            bool fAddRowsToTable = IsConnected(node);

            if (be != null)
            {
                DataRow? r = be.Row;
                if (r != null && be.ElementState == ElementState.Defoliated)
                    return; //no need of syncRow

                if (r != null)
                {
                    // get all field values.
                    SynchronizeRowFromRowElement(be);

                    // defoliation will not be done on the node which is not RowElement, in case of node is externally being used
                    if (node == be)
                    {
                        // defoliate if possible
                        be.ElementState = ElementState.WeakFoliation;
                        DefoliateRegion(be);
                    }

                    if (fAddRowsToTable && r.RowState == DataRowState.Detached)
                        r.Table.Rows.Add(r);

                    parentRow = r;
                }
            }

            // Attach all rows from children nodes
            for (XmlNode? child = node.FirstChild; child != null; child = child.NextSibling)
                SyncRows(parentRow, child, fAddRowsToTable);
        }

        internal ElementState AutoFoliationState
        {
            get { return _autoFoliationState; }
            set { _autoFoliationState = value; }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void BindForLoad()
        {
            Debug.Assert(_ignoreXmlEvents == true);
            _ignoreDataSetEvents = true;
            _mapper.SetupMapping(this, _dataSet);
            if (_dataSet.Tables.Count > 0)
            {
                //at least one table
                LoadDataSetFromTree();
            }
            BindListeners();
            _ignoreDataSetEvents = false;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void Bind(bool fLoadFromDataSet)
        {
            // If we have a DocumentElement then it is illegal to call this func to load from data-set
            Debug.Assert(DocumentElement == null || !fLoadFromDataSet);

            _ignoreDataSetEvents = true;
            _ignoreXmlEvents = true;

            // Do the mapping. This could be a successive mapping in case of this scenario: xd = XmlDataDocument( emptyDataSet ); xd.Load( "file.xml" );
            _mapper.SetupMapping(this, _dataSet);

            if (DocumentElement != null)
            {
                LoadDataSetFromTree();
                BindListeners();
            }
            else if (fLoadFromDataSet)
            {
                _bLoadFromDataSet = true;
                LoadTreeFromDataSet(DataSet);
                BindListeners();
            }

            _ignoreDataSetEvents = false;
            _ignoreXmlEvents = false;
        }

        internal void Bind(DataRow r, XmlBoundElement e)
        {
            r.Element = e;
            e.Row = r;
        }

        // Binds special listeners to catch the 1st data-row created. When the 1st DataRow is created, XmlDataDocument will automatically bind all regular listeners.
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void BindSpecialListeners()
        {
            Debug.Assert(_fDataRowCreatedSpecial == false);
            Debug.Assert(_fBoundToDataSet == false && _fBoundToDocument == false);
            _dataSet.DataRowCreated += new DataRowCreatedEventHandler(OnDataRowCreatedSpecial);
            _fDataRowCreatedSpecial = true;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void UnBindSpecialListeners()
        {
            Debug.Assert(_fDataRowCreatedSpecial == true);
            _dataSet.DataRowCreated -= new DataRowCreatedEventHandler(OnDataRowCreatedSpecial);
            _fDataRowCreatedSpecial = false;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void BindListeners()
        {
            BindToDocument();
            BindToDataSet();
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void BindToDataSet()
        {
            // We could be already bound to DataSet in this scenario:
            //     xd = new XmlDataDocument( dataSetThatHasNoData ); xd.Load( "foo.xml" );
            // so we must not rebound again to it.
            if (_fBoundToDataSet)
            {
                Debug.Assert(_dataSet != null);
                return;
            }

            // Unregister the DataRowCreatedSpecial notification
            if (_fDataRowCreatedSpecial)
                UnBindSpecialListeners();

            _dataSet.Tables.CollectionChanging += new CollectionChangeEventHandler(OnDataSetTablesChanging);
            _dataSet.Relations.CollectionChanging += new CollectionChangeEventHandler(OnDataSetRelationsChanging);
            _dataSet.DataRowCreated += new DataRowCreatedEventHandler(OnDataRowCreated);
            _dataSet.PropertyChanging += new PropertyChangedEventHandler(OnDataSetPropertyChanging);

            //this is the hack for this release, should change it in the future
            _dataSet.ClearFunctionCalled += new DataSetClearEventhandler(OnClearCalled);

            if (_dataSet.Tables.Count > 0)
            {
                foreach (DataTable t in _dataSet.Tables)
                {
                    BindToTable(t);
                }
            }

            foreach (DataRelation rel in _dataSet.Relations)
            {
                rel.PropertyChanging += new PropertyChangedEventHandler(OnRelationPropertyChanging);
            }
            _fBoundToDataSet = true;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void BindToDocument()
        {
            if (!_fBoundToDocument)
            {
                NodeInserting += new XmlNodeChangedEventHandler(OnNodeInserting);
                NodeInserted += new XmlNodeChangedEventHandler(OnNodeInserted);
                NodeRemoving += new XmlNodeChangedEventHandler(OnNodeRemoving);
                NodeRemoved += new XmlNodeChangedEventHandler(OnNodeRemoved);
                NodeChanging += new XmlNodeChangedEventHandler(OnNodeChanging);
                NodeChanged += new XmlNodeChangedEventHandler(OnNodeChanged);
                _fBoundToDocument = true;
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void BindToTable(DataTable t)
        {
            t.ColumnChanged += new DataColumnChangeEventHandler(OnColumnChanged);
            t.RowChanging += new DataRowChangeEventHandler(OnRowChanging);
            t.RowChanged += new DataRowChangeEventHandler(OnRowChanged);
            t.RowDeleting += new DataRowChangeEventHandler(OnRowChanging);
            t.RowDeleted += new DataRowChangeEventHandler(OnRowChanged);
            t.PropertyChanging += new PropertyChangedEventHandler(OnTablePropertyChanging);
            t.Columns.CollectionChanging += new CollectionChangeEventHandler(OnTableColumnsChanging);

            foreach (DataColumn col in t.Columns)
            {
                // Hook column properties changes, so we can react properly to ROM changes.
                col.PropertyChanging += new PropertyChangedEventHandler(OnColumnPropertyChanging);
            }
        }

        /// <summary>
        /// Creates an element with the specified Prefix, LocalName, and
        /// NamespaceURI.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override XmlElement CreateElement(string? prefix, string localName, string? namespaceURI)
        {
            // There are three states for the document:
            //  - special listeners ON, no permananent listeners: this is when the data doc was created w/o any dataset, and the 1st time a new row/element
            //    is created we should subscribe the permenent listeners.
            //  - special listeners OFF, permanent listeners ON: this is when the data doc is loaded (from dataset or XML file) and synchronization takes place.
            //  - special listeners OFF, permanent listeners OFF: this is then the data doc is LOADING (from dataset or XML file) - the synchronization is done by code,
            //    not based on listening to events.
#if DEBUG
            // Cannot have both special and permananent listeners ON
            if (_fDataRowCreatedSpecial)
                Debug.Assert((_fBoundToDataSet == false) && (_fBoundToDocument == false));
            // fBoundToDataSet and fBoundToDocument should have the same value
            Debug.Assert(_fBoundToDataSet ? _fBoundToDocument : (!_fBoundToDocument));
#endif
            if (prefix == null)
                prefix = string.Empty;
            if (namespaceURI == null)
                namespaceURI = string.Empty;

            if (!_fAssociateDataRow)
            {
                // Loading state: create just the XmlBoundElement: the LoadTreeFromDataSet/LoadDataSetFromTree will take care of synchronization
                return new XmlBoundElement(prefix, localName, namespaceURI, this);
            }

            // This is the 1st time an element is beeing created on an empty XmlDataDocument - unbind special listeners, bind permanent ones and then go on w/
            // creation of this element
            EnsurePopulatedMode();
            Debug.Assert(_fDataRowCreatedSpecial == false);

            // Loaded state: create a DataRow, this in turn will create and associate the XmlBoundElement, which we will return.
            DataTable dt = _mapper.SearchMatchingTableSchema(localName, namespaceURI);
            if (dt != null)
            {
                DataRow row = dt.CreateEmptyRow();
                // We need to make sure all fields are DBNull
                foreach (DataColumn col in dt.Columns)
                {
                    if (col.ColumnMapping != MappingType.Hidden)
                        SetRowValueToNull(row, col);
                }
                XmlBoundElement? be = row.Element;
                Debug.Assert(be != null);
                be.Prefix = prefix;
                return be;
            }
            // No matching table schema for this element: just create the element
            return new XmlBoundElement(prefix, localName, namespaceURI, this);
        }

        public override XmlEntityReference CreateEntityReference(string name)
        {
            throw new NotSupportedException(SR.DataDom_NotSupport_EntRef);
        }

        /// <summary>
        /// Gets a DataSet that provides a relational representation of the data in this
        /// XmlDataDocument.
        /// </summary>
        public DataSet DataSet
        {
            get
            {
                return _dataSet;
            }
        }

        private void DefoliateRegion(XmlBoundElement rowElem)
        {
            // You must pass a row element (which s/b associated w/ a DataRow)
            Debug.Assert(rowElem.Row != null);

            if (!_optimizeStorage)
                return;

            if (rowElem.ElementState != ElementState.WeakFoliation)
                return;

            if (!_mapper.IsRegionRadical(rowElem))
            {
                return;
            }

            bool saveIgnore = IgnoreXmlEvents;
            IgnoreXmlEvents = true;

            rowElem.ElementState = ElementState.Defoliating;

            try
            {
                // drop all attributes
                rowElem.RemoveAllAttributes();

                XmlNode? node = rowElem.FirstChild;
                while (node != null)
                {
                    XmlNode? next = node.NextSibling;

                    XmlBoundElement? be = node as XmlBoundElement;
                    if (be != null && be.Row != null)
                        break;

                    // The node must be mapped to a column (since the region is radically structured)
                    Debug.Assert(_mapper.GetColumnSchemaForNode(rowElem, node) != null);
                    rowElem.RemoveChild(node);

                    node = next;
                }
#if DEBUG
                // All subsequent siblings must be sub-regions
                for (; node != null; node = node.NextSibling)
                {
                    Debug.Assert((node is XmlBoundElement) && (((XmlBoundElement)node).Row != null));
                }
#endif

                rowElem.ElementState = ElementState.Defoliated;
            }
            finally
            {
                IgnoreXmlEvents = saveIgnore;
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private XmlElement EnsureDocumentElement()
        {
            XmlElement? docelem = DocumentElement;
            if (docelem == null)
            {
                string docElemName = XmlConvert.EncodeLocalName(DataSet.DataSetName);
                if (docElemName == null || docElemName.Length == 0)
                    docElemName = "Xml";
                string ns = DataSet.Namespace;
                if (ns == null)
                    ns = string.Empty;
                docelem = new XmlBoundElement(string.Empty, docElemName, ns, this);
                AppendChild(docelem);
            }

            return docelem;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private XmlElement EnsureNonRowDocumentElement()
        {
            XmlElement? docElem = DocumentElement;
            if (docElem == null)
                return EnsureDocumentElement();

            DataRow? rowDocElem = GetRowFromElement(docElem);
            if (rowDocElem == null)
                return docElem;

            return DemoteDocumentElement();
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private XmlElement DemoteDocumentElement()
        {
            // Changes of Xml here should not affect ROM
            Debug.Assert(_ignoreXmlEvents == true);
            // There should be no reason to call this function if docElem is not a rowElem
            Debug.Assert(GetRowFromElement(DocumentElement) != null);

            // Remove the DocumentElement and create a new one
            XmlElement oldDocElem = DocumentElement!;
            RemoveChild(oldDocElem);
            XmlElement docElem = EnsureDocumentElement();
            docElem.AppendChild(oldDocElem);
            // We should have only one child now
            Debug.Assert(docElem.LastChild == docElem.FirstChild);
            return docElem;
        }
        // This function ensures that the special listeners are un-subscribed, the permanent listeners are subscribed and
        // CreateElement will attach DataRows to newly created XmlBoundElement.
        // It should be called when we have special listeners hooked and we need to change from the special-listeners mode to the
        // populated/permanenet mode where all listeners are correctly hooked up and the mapper is correctly set-up.
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void EnsurePopulatedMode()
        {
            // Unbind special listeners, bind permanent ones, setup the mapping, etc
#if DEBUG
            bool fDataRowCreatedSpecialOld = _fDataRowCreatedSpecial;
            bool fAssociateDataRowOld = _fAssociateDataRow;
#endif
            if (_fDataRowCreatedSpecial)
            {
                UnBindSpecialListeners();
                // If a special listener was ON, we should not have had an already set-up mapper or permanent listeners subscribed
                Debug.Assert(!_mapper.IsMapped());
                Debug.Assert(!_fBoundToDocument);
                Debug.Assert(!_fBoundToDataSet);

                _mapper.SetupMapping(this, _dataSet);
                BindListeners();

                // CreateElement should now create associate DataRows w/ new XmlBoundElement nodes
                // We should do this ONLY if we switch from special listeners to permanent listeners. The reason is
                // that DataDocumentNavigator wants to put XmlDataDocument in a batch mode, where CreateElement will just
                // create a XmlBoundElement (see DataDocumentNavigator.CloneTree)
                _fAssociateDataRow = true;
            }

            Debug.Assert(_fDataRowCreatedSpecial == false);
            Debug.Assert(_mapper.IsMapped());
            Debug.Assert(_fBoundToDataSet && _fBoundToDocument);
#if DEBUG
            // In case we EnsurePopulatedMode was called on an already populated mode, we should NOT change fAssociateDataRow
            if (fDataRowCreatedSpecialOld == false)
                Debug.Assert(fAssociateDataRowOld == _fAssociateDataRow);
#endif
        }

        // Move regions that are marked in ROM as nested children of row/rowElement as last children in XML fragment
        private void FixNestedChildren(DataRow row, XmlElement rowElement)
        {
            foreach (DataRelation dr in GetNestedChildRelations(row))
            {
                foreach (DataRow r in row.GetChildRows(dr))
                {
                    XmlElement? childElem = r.Element;
                    // childElem can be null when we create XML from DataSet (XmlDataDocument( DataSet ) is called) and we insert rowElem of the parentRow before
                    // we insert the rowElem of children rows.
                    if (childElem != null)
                    {
#if DEBUG
                        bool fIsChildConnected = IsConnected(childElem);
#endif
                        if (childElem.ParentNode != rowElement)
                        {
                            childElem.ParentNode!.RemoveChild(childElem);
                            rowElement.AppendChild(childElem);
                        }
#if DEBUG
                        // We should not have changed the connected/disconnected state of the node (since the row state did not change)
                        Debug.Assert(fIsChildConnected == IsConnected(childElem));
                        Debug.Assert(IsRowLive(r) ? IsConnected(childElem) : !IsConnected(childElem));
#endif
                    }
                }
            }
        }

        // This function accepts node params that are not row-elements. In this case, calling this function is a no-op
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void Foliate(XmlBoundElement node, ElementState newState)
        {
            Debug.Assert(newState == ElementState.WeakFoliation || newState == ElementState.StrongFoliation);
#if DEBUG
            // If we want to strong foliate one of the non-row-elem in a region, then the region MUST be strong-foliated (or there must be no region)
            // Do this only when we are not loading
            if (IsFoliationEnabled)
            {
                if (newState == ElementState.StrongFoliation && node.Row == null)
                {
                    XmlBoundElement? rowElem;
                    ElementState rowElemState = ElementState.None;
                    if (_mapper.GetRegion(node, out rowElem))
                    {
                        rowElemState = rowElem.ElementState;
                        Debug.Assert(rowElemState == ElementState.StrongFoliation || rowElemState == ElementState.WeakFoliation);
                    }
                    // Add a no-op, so we can still debug in the assert fails

#pragma warning disable 1717 // assignment to self
                    rowElemState = rowElemState;
#pragma warning restore 1717
                }
            }
#endif

            if (IsFoliationEnabled)
            {
                if (node.ElementState == ElementState.Defoliated)
                {
                    ForceFoliation(node, newState);
                }
                else if (node.ElementState == ElementState.WeakFoliation && newState == ElementState.StrongFoliation)
                {
                    // Node must be a row-elem
                    Debug.Assert(node.Row != null);
                    node.ElementState = newState;
                }
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void Foliate(XmlElement element)
        {
            if (element is XmlBoundElement)
                ((XmlBoundElement)element).Foliate(ElementState.WeakFoliation);
        }

        // Foliate rowElement region if there are DataPointers that points into it
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void FoliateIfDataPointers(DataRow row, XmlElement rowElement)
        {
            if (!IsFoliated(rowElement) && HasPointers(rowElement))
            {
                bool wasFoliationEnabled = IsFoliationEnabled;
                IsFoliationEnabled = true;
                try
                {
                    Foliate(rowElement);
                }
                finally
                {
                    IsFoliationEnabled = wasFoliationEnabled;
                }
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void EnsureFoliation(XmlBoundElement rowElem, ElementState foliation)
        {
            if (rowElem.IsFoliated) //perf reason, avoid unecessary lock.
                return;
            ForceFoliation(rowElem, foliation);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void ForceFoliation(XmlBoundElement node, ElementState newState)
        {
            lock (_foliationLock)
            {
                if (node.ElementState != ElementState.Defoliated)
                    // The region was foliated by an other thread while this thread was locked
                    return;

                // Node must be a row-elem associated w/ a non-deleted row
                Debug.Assert(node.Row != null);
                Debug.Assert(node.Row.RowState != DataRowState.Deleted);

                node.ElementState = ElementState.Foliating;

                bool saveIgnore = IgnoreXmlEvents;
                IgnoreXmlEvents = true;

                try
                {
                    XmlNode? priorNode = null;
                    DataRow row = node.Row;

                    // create new attrs & elements for row
                    // For detached rows: we are in sync w/ temp values
                    // For non-detached rows: we are in sync w/ the current values
                    // For deleted rows: we never sync
                    DataRowVersion rowVersion = (row.RowState == DataRowState.Detached) ? DataRowVersion.Proposed : DataRowVersion.Current;
                    foreach (DataColumn col in row.Table.Columns)
                    {
                        if (!IsNotMapped(col))
                        {
                            object value = row[col, rowVersion];

                            if (!Convert.IsDBNull(value))
                            {
                                if (col.ColumnMapping == MappingType.Attribute)
                                {
                                    node.SetAttribute(col.EncodedColumnName, col.Namespace, col.ConvertObjectToXml(value));
                                }
                                else
                                {
                                    XmlNode? newNode = null;
                                    if (col.ColumnMapping == MappingType.Element)
                                    {
                                        newNode = new XmlBoundElement(string.Empty, col.EncodedColumnName, col.Namespace, this);
                                        newNode.AppendChild(CreateTextNode(col.ConvertObjectToXml(value)));
                                        if (priorNode != null)
                                        {
                                            node.InsertAfter(newNode, priorNode);
                                        }
                                        else if (node.FirstChild != null)
                                        {
                                            node.InsertBefore(newNode, node.FirstChild);
                                        }
                                        else
                                        {
                                            node.AppendChild(newNode);
                                        }
                                        priorNode = newNode;
                                    }
                                    else
                                    {
                                        Debug.Assert(col.ColumnMapping == MappingType.SimpleContent);
                                        newNode = CreateTextNode(col.ConvertObjectToXml(value));
                                        if (node.FirstChild != null)
                                            node.InsertBefore(newNode, node.FirstChild);
                                        else
                                            node.AppendChild(newNode);
                                        if (priorNode == null)
                                            priorNode = newNode;
                                    }
                                }
                            }
                            else
                            {
                                if (col.ColumnMapping == MappingType.SimpleContent)
                                {
                                    XmlAttribute attr = CreateAttribute(XSI, Keywords.XSI_NIL, Keywords.XSINS);
                                    attr.Value = Keywords.TRUE;
                                    node.SetAttributeNode(attr);
                                    _bHasXSINIL = true;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    IgnoreXmlEvents = saveIgnore;
                    node.ElementState = newState;
                }
                // update all live pointers
                OnFoliated(node);
            }
        }

        //Determine best radical insert position for inserting column elements
        private XmlNode? GetColumnInsertAfterLocation(DataRow row, DataColumn col, XmlBoundElement rowElement)
        {
            XmlNode? prev = null;
            XmlNode? node = null;

            // text only columns appear first
            if (IsTextOnly(col))
                return null;

            // insert location must be after free text
            for (node = rowElement.FirstChild; node != null; prev = node, node = node.NextSibling)
            {
                if (!IsTextLikeNode(node))
                    break;
            }

            for (; node != null; prev = node, node = node.NextSibling)
            {
                // insert location must be before any non-element nodes
                if (node.NodeType != XmlNodeType.Element)
                    break;
                XmlElement? e = node as XmlElement;

                // insert location must be before any non-mapped elements or separate regions
                if (_mapper.GetRowFromElement(e) != null)
                    break;

                object? schema = _mapper.GetColumnSchemaForNode(rowElement, node);
                if (schema == null || !(schema is DataColumn))
                    break;

                // insert location must be before any columns logically after this column
                if (((DataColumn)schema).Ordinal > col.Ordinal)
                    break;
            }

            return prev;
        }

        private ArrayList GetNestedChildRelations(DataRow row)
        {
            ArrayList list = new ArrayList();

            foreach (DataRelation r in row.Table.ChildRelations)
            {
                if (r.Nested)
                    list.Add(r);
            }

            return list;
        }

        private DataRow? GetNestedParent(DataRow row)
        {
            DataRelation? relation = GetNestedParentRelation(row);
            if (relation != null)
                return row.GetParentRow(relation);
            return null;
        }

        private static DataRelation? GetNestedParentRelation(DataRow row)
        {
            DataRelation[] relations = row.Table.NestedParentRelations;
            if (relations.Length == 0)
                return null;
            return relations[0];
        }

        private DataColumn? GetTextOnlyColumn(DataRow row)
        {
#if DEBUG
            {
                // Make sure there is at most only one text column, and the text column (if present) is the one reported by row.Table.XmlText
                DataColumnCollection columns = row.Table.Columns;
                int cCols = columns.Count;
                int cTextCols = 0;
                for (int iCol = 0; iCol < cCols; iCol++)
                {
                    DataColumn c = columns[iCol];
                    if (IsTextOnly(c))
                    {
                        Debug.Assert(c == row.Table.XmlText);
                        ++cTextCols;
                    }
                }
                Debug.Assert(cTextCols == 0 || cTextCols == 1);
                if (cTextCols == 0)
                    Debug.Assert(row.Table.XmlText == null);
            }
#endif
            return row.Table.XmlText;
        }

        /// <summary>
        /// Retrieves the DataRow associated with the specified XmlElement.
        /// </summary>
        public DataRow? GetRowFromElement(XmlElement? e)
        {
            return _mapper.GetRowFromElement(e);
        }

        private XmlNode? GetRowInsertBeforeLocation(DataRow row, XmlElement rowElement, XmlNode parentElement)
        {
            DataRow refRow = row;
            int i = 0;
            int pos;

            // Find position
            // int pos = row.Table.Rows[row];
            for (i = 0; i < row.Table.Rows.Count; i++)
                if (row == row.Table.Rows[i])
                    break;
            pos = i;

            DataRow? parentRow = GetNestedParent(row);
            for (i = pos + 1; i < row.Table.Rows.Count; i++)
            {
                refRow = row.Table.Rows[i];
                if (GetNestedParent(refRow) == parentRow && GetElementFromRow(refRow).ParentNode == parentElement)
                    break;
            }

            if (i < row.Table.Rows.Count)
                return GetElementFromRow(refRow);
            else
                return null;
        }


        /// <summary>
        /// Retrieves the XmlElement associated with the specified DataRow.
        /// </summary>
        public XmlElement GetElementFromRow(DataRow r)
        {
            XmlBoundElement? be = r.Element;
            Debug.Assert(be != null);
            return be;
        }

        internal bool HasPointers(XmlNode node)
        {
            while (true)
            {
                try
                {
                    if (_pointers.Count > 0)
                    {
                        object? pointer = null;
                        foreach (DictionaryEntry entry in _pointers)
                        {
                            pointer = entry.Value;
                            Debug.Assert(pointer != null);
                            if (((IXmlDataVirtualNode)pointer).IsOnNode(node))
                                return true;
                        }
                    }
                    return false;
                }
                catch (Exception e) when (Data.Common.ADP.IsCatchableExceptionType(e))
                {
                    // This can happens only when some threads are creating navigators (thus modifying this.pointers) while other threads are in the foreach loop.
                }
            }
            //should never get to this point due to while (true) loop
        }

        internal bool IgnoreXmlEvents
        {
            get { return _ignoreXmlEvents; }
            set { _ignoreXmlEvents = value; }
        }

        internal bool IgnoreDataSetEvents
        {
            get { return _ignoreDataSetEvents; }
            set { _ignoreDataSetEvents = value; }
        }

        private bool IsFoliated(XmlElement element)
        {
            if (element is XmlBoundElement)
            {
                return ((XmlBoundElement)element).IsFoliated;
            }

            return true;
        }
        private bool IsFoliated(XmlBoundElement be)
        {
            return be.IsFoliated;
        }

        internal bool IsFoliationEnabled
        {
            get { return _isFoliationEnabled; }
            set { _isFoliationEnabled = value; }
        }

        // This creates a tree and synchronize ROM w/ the created tree.
        // It requires the populated mode to be on - in case we are not in populated mode, it will make the XmlDataDocument be in populated mode.
        // It takes advantage of the fAssociateDataRow flag for populated mode, which allows creation of XmlBoundElement w/o associating DataRow objects.
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal XmlNode CloneTree(DataPointer other)
        {
            EnsurePopulatedMode();

            bool oldIgnoreDataSetEvents = _ignoreDataSetEvents;
            bool oldIgnoreXmlEvents = _ignoreXmlEvents;
            bool oldFoliationEnabled = IsFoliationEnabled;
            bool oldAssociateDataRow = _fAssociateDataRow;

            // Caller should ensure that the EnforceConstraints == false. See 60486 for more info about why this was changed from DataSet.EnforceConstraints = false to an assert.
            Debug.Assert(DataSet.EnforceConstraints == false);
            XmlNode newNode;

            try
            {
                _ignoreDataSetEvents = true;
                _ignoreXmlEvents = true;
                IsFoliationEnabled = false;
                _fAssociateDataRow = false;

                // Create the diconnected tree based on the other navigator
                newNode = CloneTreeInternal(other);
                Debug.Assert(newNode != null);

                // Synchronize DataSet from XML
                LoadRows(null, newNode);
                SyncRows(null, newNode, false);
            }
            finally
            {
                _ignoreDataSetEvents = oldIgnoreDataSetEvents;
                _ignoreXmlEvents = oldIgnoreXmlEvents;
                IsFoliationEnabled = oldFoliationEnabled;
                _fAssociateDataRow = oldAssociateDataRow;
            }
            return newNode;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private XmlNode CloneTreeInternal(DataPointer other)
        {
            Debug.Assert(_ignoreDataSetEvents == true);
            Debug.Assert(_ignoreXmlEvents == true);
            Debug.Assert(IsFoliationEnabled == false);

            // Create the diconnected tree based on the other navigator
            XmlNode newNode = CloneNode(other);

            DataPointer dp = new DataPointer(other);
            try
            {
                dp.AddPointer();
                if (newNode.NodeType == XmlNodeType.Element)
                {
                    int cAttributes = dp.AttributeCount;
                    for (int i = 0; i < cAttributes; i++)
                    {
                        dp.MoveToOwnerElement();
                        if (dp.MoveToAttribute(i))
                        {
                            newNode.Attributes!.Append((XmlAttribute)CloneTreeInternal(dp));
                        }
                    }

                    dp.MoveTo(other);
                }

                for (bool fMore = dp.MoveToFirstChild(); fMore; fMore = dp.MoveToNextSibling())
                    newNode.AppendChild(CloneTreeInternal(dp));
            }
            finally
            {
                dp.SetNoLongerUse();
            }

            return newNode;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override XmlNode CloneNode(bool deep)
        {
            XmlDataDocument clone = (XmlDataDocument)(base.CloneNode(false));
            clone.Init(DataSet.Clone());

            clone._dataSet.EnforceConstraints = _dataSet.EnforceConstraints;
            Debug.Assert(clone.FirstChild == null);
            if (deep)
            {
                DataPointer dp = new DataPointer(this, this);
                try
                {
                    dp.AddPointer();
                    for (bool fMore = dp.MoveToFirstChild(); fMore; fMore = dp.MoveToNextSibling())
                    {
                        XmlNode cloneNode;
                        if (dp.NodeType == XmlNodeType.Element)
                            cloneNode = clone.CloneTree(dp);
                        else
                            cloneNode = clone.CloneNode(dp);
                        clone.AppendChild(cloneNode);
                    }
                }
                finally
                {
                    dp.SetNoLongerUse();
                }
            }

            return clone;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private XmlNode CloneNode(DataPointer dp) =>
            dp.NodeType switch
            {
                //for the nodes without value and have no children
                XmlNodeType.DocumentFragment => CreateDocumentFragment(),
                XmlNodeType.DocumentType => CreateDocumentType(dp.Name, dp.PublicId, dp.SystemId, dp.InternalSubset),
                XmlNodeType.XmlDeclaration => CreateXmlDeclaration(dp.Version!, dp.Encoding, dp.Standalone),

                //for the nodes with value but no children
                XmlNodeType.Text => CreateTextNode(dp.Value),
                XmlNodeType.CDATA => CreateCDataSection(dp.Value),
                XmlNodeType.ProcessingInstruction => CreateProcessingInstruction(dp.Name, dp.Value!),
                XmlNodeType.Comment => CreateComment(dp.Value),
                XmlNodeType.Whitespace => CreateWhitespace(dp.Value),
                XmlNodeType.SignificantWhitespace => CreateSignificantWhitespace(dp.Value),

                //for the nodes that don't have values, but might have children -- only clone the node and leave the children untouched
                XmlNodeType.Element => CreateElement(dp.Prefix, dp.LocalName, dp.NamespaceURI),
                XmlNodeType.Attribute => CreateAttribute(dp.Prefix, dp.LocalName, dp.NamespaceURI),
                XmlNodeType.EntityReference => CreateEntityReference(dp.Name),

                _ => throw new InvalidOperationException(SR.Format(SR.DataDom_CloneNode, dp.NodeType.ToString())),
            };

        internal static bool IsTextLikeNode(XmlNode n)
        {
            switch (n.NodeType)
            {
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    return true;

                case XmlNodeType.EntityReference:
                    Debug.Fail("Found entity reference");
                    return false;

                default:
                    return false;
            }
        }

        internal bool IsNotMapped(DataColumn c)
        {
            return DataSetMapper.IsNotMapped(c);
        }

        private bool IsSame(DataColumn c, int recNo1, int recNo2)
        {
            if (c.Compare(recNo1, recNo2) == 0)
                return true;

            return false;
        }

        internal bool IsTextOnly(DataColumn c)
        {
            return c.ColumnMapping == MappingType.SimpleContent;
        }


        /// <summary>
        /// Loads the XML document from the specified file.
        /// </summary>
        public override void Load(string filename)
        {
            _bForceExpandEntity = true;
            base.Load(filename);
            _bForceExpandEntity = false;
        }

        /// <summary>
        /// Loads the XML document from the specified Stream.
        /// </summary>
        public override void Load(Stream inStream)
        {
            _bForceExpandEntity = true;
            base.Load(inStream);
            _bForceExpandEntity = false;
        }

        /// <summary>
        /// Loads the XML document from the specified TextReader.
        /// </summary>
        public override void Load(TextReader txtReader)
        {
            _bForceExpandEntity = true;
            base.Load(txtReader);
            _bForceExpandEntity = false;
        }

        /// <summary>
        /// Loads the XML document from the specified XmlReader.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override void Load(XmlReader reader)
        {
            if (FirstChild != null)
                throw new InvalidOperationException(SR.DataDom_MultipleLoad);

            try
            {
                _ignoreXmlEvents = true;

                // Unhook the DataRowCreatedSpecial listener, since we no longer base on the first created DataRow to do the Bind
                if (_fDataRowCreatedSpecial)
                    UnBindSpecialListeners();

                // We should NOT create DataRow objects when calling XmlDataDocument.CreateElement
                _fAssociateDataRow = false;
                // Foliation s/b disabled
                _isFoliationEnabled = false;

                //now if we load from file we need to set the ExpandEntity flag to ExpandEntities
                if (_bForceExpandEntity)
                {
                    Debug.Assert(reader is XmlTextReader);
                    ((XmlTextReader)reader).EntityHandling = EntityHandling.ExpandEntities;
                }
                base.Load(reader);
                BindForLoad();
            }
            finally
            {
                _ignoreXmlEvents = false;
                _isFoliationEnabled = true;
                _autoFoliationState = ElementState.StrongFoliation;
                _fAssociateDataRow = true;
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void LoadDataSetFromTree()
        {
            _ignoreDataSetEvents = true;
            _ignoreXmlEvents = true;
            bool wasFoliationEnabled = IsFoliationEnabled;
            IsFoliationEnabled = false;
            bool saveEnforce = _dataSet.EnforceConstraints;
            _dataSet.EnforceConstraints = false;

            try
            {
                Debug.Assert(DocumentElement != null);
                LoadRows(null, DocumentElement);
                SyncRows(null, DocumentElement, true);

                _dataSet.EnforceConstraints = saveEnforce;
            }
            finally
            {
                _ignoreDataSetEvents = false;
                _ignoreXmlEvents = false;
                IsFoliationEnabled = wasFoliationEnabled;
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void LoadTreeFromDataSet(DataSet ds)
        {
            _ignoreDataSetEvents = true;
            _ignoreXmlEvents = true;
            bool wasFoliationEnabled = IsFoliationEnabled;
            IsFoliationEnabled = false;
            _fAssociateDataRow = false;

            DataTable[] orderedTables = OrderTables(ds);
            // problem is after we add support for Namespace  for DataTable, when inferring we do not guarantee that table would be
            // in the same sequence that they were in XML because of namespace, some would be on different schema, so since they
            // won't be in the same sequence as in XML, we may end up with having a child table, before its parent (which is not doable
            // with XML; and this happend because they are in different namespace)
            // this kind of problems are known and please see comment in "OnNestedParentChange"
            // so to fix it in general, we try to iterate over ordered tables instead of going over all tables in DataTableCollection with their own sequence

            try
            {
                for (int i = 0; i < orderedTables.Length; i++)
                {
                    DataTable t = orderedTables[i];
                    foreach (DataRow r in t.Rows)
                    {
                        Debug.Assert(r.Element == null);
                        AttachBoundElementToDataRow(r);

                        switch (r.RowState)
                        {
                            case DataRowState.Added:
                            case DataRowState.Unchanged:
                            case DataRowState.Modified:
                                OnAddRow(r);
                                break;
                            case DataRowState.Deleted:
                                // Nothing to do (the row already has an associated element as a fragment
                                break;
                            case DataRowState.Detached:
                                Debug.Fail("We should not get rows in this state");
                                break;
                            default:
                                Debug.Fail("Unknown row state");
                                break;
                        }
                    }
                }
            }
            finally
            {
                _ignoreDataSetEvents = false;
                _ignoreXmlEvents = false;
                IsFoliationEnabled = wasFoliationEnabled;
                _fAssociateDataRow = true;
            }
        }

        // load all data from tree structre into datarows
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void LoadRows(XmlBoundElement? rowElem, XmlNode node)
        {
            Debug.Assert(node != null);

            XmlBoundElement? be = node as XmlBoundElement;
            if (be != null)
            {
                DataTable? dt = _mapper.SearchMatchingTableSchema(rowElem, be);

                if (dt != null)
                {
                    DataRow? r = GetRowFromElement(be);
                    Debug.Assert(r == null);
                    // If the rowElement was just created and has an un-initialized
                    if (be.ElementState == ElementState.None)
                        be.ElementState = ElementState.WeakFoliation;
                    r = dt.CreateEmptyRow();
                    Bind(r, be);

                    // the region rowElem is now be
                    Debug.Assert(be.Row != null);
                    rowElem = be;
                }
            }
            // recurse down for children
            for (XmlNode? child = node.FirstChild; child != null; child = child.NextSibling)
                LoadRows(rowElem, child);
        }

        internal DataSetMapper Mapper
        {
            get
            {
                return _mapper;
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void OnDataRowCreated(object oDataSet, DataRow row)
        {
            Debug.Assert(row.RowState == DataRowState.Detached);
            OnNewRow(row);
        }

        internal void OnClearCalled(object oDataSet, DataTable? table)
        {
            throw new NotSupportedException(SR.DataDom_NotSupport_Clear);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void OnDataRowCreatedSpecial(object oDataSet, DataRow row)
        {
            Debug.Assert(row.RowState == DataRowState.Detached);

            // Register the regular events and un-register this one
            Bind(true);
            // Pass the event to the regular listener
            OnNewRow(row);
        }

        // Called when a new DataRow is created
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void OnNewRow(DataRow row)
        {
            Debug.Assert(row.Element == null);
            // Allow New state also because we are calling this function from
            Debug.Assert(row.RowState == DataRowState.Detached);

            AttachBoundElementToDataRow(row);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private XmlBoundElement AttachBoundElementToDataRow(DataRow row)
        {
            Debug.Assert(row.Element == null);
            DataTable table = row.Table;
            // We shoould NOT call CreateElement here, since CreateElement will create and attach a new DataRow to the element
            XmlBoundElement rowElement = new XmlBoundElement(string.Empty, table.EncodedTableName, table.Namespace, this);
            rowElement.IsEmpty = false;
            Bind(row, rowElement);
            rowElement.ElementState = ElementState.Defoliated;
            return rowElement;
        }

        private bool NeedXSI_NilAttr(DataRow row)
        {
            DataTable tb = row.Table;
            Debug.Assert(tb != null);
            if (tb._xmlText == null)
                return false;
            object value = row[tb._xmlText];
            return (Convert.IsDBNull(value));
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnAddRow(DataRow row)
        {
            // Xml operations in this func should not trigger ROM operations
            Debug.Assert(_ignoreXmlEvents == true);

            XmlBoundElement rowElement = (XmlBoundElement)(GetElementFromRow(row));
            Debug.Assert(rowElement != null);

            if (NeedXSI_NilAttr(row) && !rowElement.IsFoliated)
                //we need to foliate it because we need to add one more attribute xsi:nil = true;
                ForceFoliation(rowElement, AutoFoliationState);

            Debug.Assert(rowElement != null);
            DataRow? rowDocElem = GetRowFromElement(DocumentElement);
            if (rowDocElem != null)
            {
                DataRow? parentRow = GetNestedParent(row);
                if (parentRow == null)
                    DemoteDocumentElement();
            }
            EnsureDocumentElement().AppendChild(rowElement);

            // Move the children of the row under
            FixNestedChildren(row, rowElement);
            OnNestedParentChange(row, rowElement, null);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnColumnValueChanged(DataRow row, DataColumn col, XmlBoundElement rowElement)
        {
            if (IsNotMapped(col))
            {
                goto lblDoNestedRelationSync;
            }

            object value = row[col];

            if (col.ColumnMapping == MappingType.SimpleContent && Convert.IsDBNull(value) && !rowElement.IsFoliated)
            {
                ForceFoliation(rowElement, ElementState.WeakFoliation);
            }
            else
            {
                // no need to sync if not foliated
                if (!IsFoliated(rowElement))
                {
#if DEBUG
                    // If the new value is null, we should be already foliated if there is a DataPointer that points to the column
                    // (see OnRowChanging, case DataRowAction.Change)
                    if (Convert.IsDBNull(row[col, DataRowVersion.Current]))
                    {
                        try
                        {
                            if (_pointers.Count > 0)
                            {
                                object? pointer = null;
                                foreach (DictionaryEntry entry in _pointers)
                                {
                                    pointer = entry.Value;
                                    Debug.Assert((pointer != null) && !((IXmlDataVirtualNode)pointer).IsOnColumn(col));
                                }
                            }
                        }
                        catch (Exception e) when (Data.Common.ADP.IsCatchableExceptionType(e))
                        {
                            // We may get an exception if we are in foreach and a new pointer has been added to this.pointers. When this happens, we will skip this check and ignore the exceptions
                        }
                    }
#endif
                    goto lblDoNestedRelationSync;
                }
            }

            if (IsTextOnly(col))
            {
                if (Convert.IsDBNull(value))
                {
                    value = string.Empty;
                    //make sure that rowElement has Attribute xsi:nil and its value is true
                    XmlAttribute? attr = rowElement.GetAttributeNode(XSI_NIL);
                    if (attr == null)
                    {
                        attr = CreateAttribute(XSI, Keywords.XSI_NIL, Keywords.XSINS);
                        attr.Value = Keywords.TRUE;
                        rowElement.SetAttributeNode(attr);
                        _bHasXSINIL = true;
                    }
                    else
                        attr.Value = Keywords.TRUE;
                }
                else
                {
                    //make sure that if rowElement has Attribute xsi:nil, its value is false
                    XmlAttribute? attr = rowElement.GetAttributeNode(XSI_NIL);
                    if (attr != null)
                        attr.Value = Keywords.FALSE;
                }
                ReplaceInitialChildText(rowElement, col.ConvertObjectToXml(value));
                goto lblDoNestedRelationSync;
            }

            // update the attribute that maps to the column
            bool fFound = false;

            // Find the field node and set it's value
            if (col.ColumnMapping == MappingType.Attribute)
            {
                foreach (XmlAttribute attr in rowElement.Attributes)
                {
                    if (attr.LocalName == col.EncodedColumnName && attr.NamespaceURI == col.Namespace)
                    {
                        if (Convert.IsDBNull(value))
                        {
                            attr.OwnerElement!.Attributes.Remove(attr);
                        }
                        else
                        {
                            attr.Value = col.ConvertObjectToXml(value);
                        }
                        fFound = true;
                        break;
                    }
                }

                // create new attribute if we didn't find one.
                if (!fFound && !Convert.IsDBNull(value))
                {
                    rowElement.SetAttribute(col.EncodedColumnName, col.Namespace, col.ConvertObjectToXml(value));
                }
            }
            else
            {
                // update elements that map to the column...
                RegionIterator iter = new RegionIterator(rowElement);
                bool fMore = iter.Next();
                while (fMore)
                {
                    if (iter.CurrentNode!.NodeType == XmlNodeType.Element)
                    {
                        XmlElement e = (XmlElement)iter.CurrentNode;
                        Debug.Assert(e != null);
                        //we should skip the subregion
                        XmlBoundElement? be = e as XmlBoundElement;
                        if (be != null && be.Row != null)
                        {
                            fMore = iter.NextRight(); //skip over the sub-region
                            continue;
                        }
                        if (e.LocalName == col.EncodedColumnName && e.NamespaceURI == col.Namespace)
                        {
                            fFound = true;
                            if (Convert.IsDBNull(value))
                            {
                                PromoteNonValueChildren(e);
                                fMore = iter.NextRight();
                                e.ParentNode!.RemoveChild(e);
                                // keep looking for more matching elements
                                continue;
                            }
                            else
                            {
                                ReplaceInitialChildText(e, col.ConvertObjectToXml(value));
                                //make sure that if the Element has Attribute xsi:nil, its value is false
                                XmlAttribute? attr = e.GetAttributeNode(XSI_NIL);
                                if (attr != null)
                                    attr.Value = Keywords.FALSE;
                                // no need to look any further.
                                goto lblDoNestedRelationSync;
                            }
                        }
                    }
                    fMore = iter.Next();
                }

                // create new element if we didn't find one.
                if (!fFound && !Convert.IsDBNull(value))
                {
                    XmlElement newElem = new XmlBoundElement(string.Empty, col.EncodedColumnName, col.Namespace, this);
                    newElem.AppendChild(CreateTextNode(col.ConvertObjectToXml(value)));

                    XmlNode? elemBefore = GetColumnInsertAfterLocation(row, col, rowElement);
                    if (elemBefore != null)
                    {
                        rowElement.InsertAfter(newElem, elemBefore);
                    }
                    else if (rowElement.FirstChild != null)
                    {
                        rowElement.InsertBefore(newElem, rowElement.FirstChild);
                    }
                    else
                    {
                        rowElement.AppendChild(newElem);
                    }
                }
            }
        lblDoNestedRelationSync:
            // Change the XML to conform to the (potentially) change in parent nested relation
            DataRelation? relation = GetNestedParentRelation(row);
            if (relation != null)
            {
                Debug.Assert(relation.ChildTable == row.Table);
                if (relation.ChildKey.ContainsColumn(col))
                    OnNestedParentChange(row, rowElement, col);
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnColumnChanged(object sender, DataColumnChangeEventArgs args)
        {
            // You should not be able to make DataRow field changes if the DataRow is deleted
            Debug.Assert(args.Row.RowState != DataRowState.Deleted);

            if (_ignoreDataSetEvents)
                return;

            bool wasIgnoreXmlEvents = _ignoreXmlEvents;
            _ignoreXmlEvents = true;
            bool wasFoliationEnabled = IsFoliationEnabled;
            IsFoliationEnabled = false;

            try
            {
                DataRow row = args.Row;
                DataColumn? col = args.Column;

                if (row.RowState == DataRowState.Detached)
                {
                    XmlBoundElement? be = row.Element;
                    Debug.Assert(be != null);
                    if (be.IsFoliated)
                    {
                        // Need to sync changes from ROM to DOM
                        OnColumnValueChanged(row, col!, be);
                    }
                }
            }
            finally
            {
                IsFoliationEnabled = wasFoliationEnabled;
                _ignoreXmlEvents = wasIgnoreXmlEvents;
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnColumnValuesChanged(DataRow row, XmlBoundElement rowElement)
        {
            Debug.Assert(row != null);
            Debug.Assert(rowElement != null);

            // If user has cascading relationships, then columnChangeList will contains the changed columns only for the last row beeing cascaded
            // but there will be multiple ROM events
            if (_columnChangeList.Count > 0)
            {
                if (((DataColumn)(_columnChangeList[0]!)).Table == row.Table)
                {
                    foreach (DataColumn c in _columnChangeList)
                        OnColumnValueChanged(row, c, rowElement);
                }
                else
                {
                    foreach (DataColumn c in row.Table.Columns)
                        OnColumnValueChanged(row, c, rowElement);
                }
            }
            else
            {
                foreach (DataColumn c in row.Table.Columns)
                    OnColumnValueChanged(row, c, rowElement);
            }
            _columnChangeList.Clear();
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnDeleteRow(DataRow row, XmlBoundElement rowElement)
        {
            // IgnoreXmlEvents s/b on since we are manipulating the XML tree and we not want this to reflect in ROM view.
            Debug.Assert(_ignoreXmlEvents == true);
            // Special case when rowElem is document element: we create a new docElem, move the current one as a child of
            // the new created docElem, then process as if the docElem is not a rowElem
            if (rowElement == DocumentElement)
                DemoteDocumentElement();

            PromoteInnerRegions(rowElement);
            rowElement.ParentNode!.RemoveChild(rowElement);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnDeletingRow(DataRow row, XmlBoundElement rowElement)
        {
            // Note that this function is beeing called even if ignoreDataSetEvents == true.

            // Foliate, so we can be able to preserve the nodes even if the DataRow has no longer values for the crtRecord.
            if (IsFoliated(rowElement))
                return;

            bool wasIgnoreXmlEvents = IgnoreXmlEvents;
            IgnoreXmlEvents = true;
            bool wasFoliationEnabled = IsFoliationEnabled;
            IsFoliationEnabled = true;
            try
            {
                Foliate(rowElement);
            }
            finally
            {
                IsFoliationEnabled = wasFoliationEnabled;
                IgnoreXmlEvents = wasIgnoreXmlEvents;
            }
        }

        private void OnFoliated(XmlNode node)
        {
            while (true)
            {
                try
                {
                    if (_pointers.Count > 0)
                    {
                        foreach (DictionaryEntry entry in _pointers)
                        {
                            object? pointer = entry.Value;
                            Debug.Assert(pointer != null);
                            ((IXmlDataVirtualNode)pointer).OnFoliated(node);
                        }
                    }
                    return;
                }
                catch (Exception e) when (Data.Common.ADP.IsCatchableExceptionType(e))
                {
                    // This can happens only when some threads are creating navigators (thus modifying this.pointers) while other threads are in the foreach loop.
                    // Solution is to re-try OnFoliated.
                }
            }
            // You should never get here in regular cases
        }

        private DataColumn? FindAssociatedParentColumn(DataRelation relation, DataColumn childCol)
        {
            DataColumn[] columns = relation.ChildKey.ColumnsReference;
            for (int i = 0; i < columns.Length; i++)
            {
                if (childCol == columns[i])
                    return relation.ParentKey.ColumnsReference[i];
            }
            return null;
        }

        // Change the childElement position in the tree to conform to the parent nested relationship in ROM
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnNestedParentChange(DataRow child, XmlBoundElement childElement, DataColumn? childCol)
        {
            Debug.Assert(child.Element == childElement && childElement.Row == child);
            // This function is (and s/b) called as a result of ROM changes, therefore XML changes done here should not be sync-ed to ROM
            Debug.Assert(_ignoreXmlEvents == true);
#if DEBUG
            // In order to check that this move does not change the connected/disconnected state of the node
            bool fChildElementConnected = IsConnected(childElement);
#endif
            DataRow? parentRowInTree;
            if (childElement == DocumentElement || childElement.ParentNode == null)
                parentRowInTree = null;
            else
                parentRowInTree = GetRowFromElement((XmlElement)childElement.ParentNode);
            DataRow? parentRowInRelation = GetNestedParent(child!);

            if (parentRowInTree != parentRowInRelation)
            {
                if (parentRowInRelation != null)
                {
                    XmlElement newParent = GetElementFromRow(parentRowInRelation);
                    newParent.AppendChild(childElement);
                }
                else
                {
                    // no parent? Maybe the parentRow is during changing or childCol is the ID is set to null ( detached from the parent row ).
                    DataRelation? relation = GetNestedParentRelation(child!);
                    if (childCol == null || relation == null || Convert.IsDBNull(child[childCol]))
                    {
                        EnsureNonRowDocumentElement().AppendChild(childElement);
                    }
                    else
                    {
                        DataColumn? colInParent = FindAssociatedParentColumn(relation, childCol);
                        Debug.Assert(colInParent != null);
                        object? comparedValue = colInParent.ConvertValue(child[childCol]);
                        if (parentRowInTree!._tempRecord != -1 && colInParent.CompareValueTo(parentRowInTree._tempRecord, comparedValue) != 0)
                        {
                            EnsureNonRowDocumentElement().AppendChild(childElement);
                        }
                        //else do nothing because its original parentRowInRelation will be changed so that this row will still be its child
                    }
                }
            }
#if DEBUG
            // We should not have changed the connected/disconnected state of the node (since the row state did not change) -- IOW if the original childElem was in dis-connected
            // state and corresponded to a detached/deleted row, by adding it to the main tree we become inconsistent (since we have now a deleted/detached row in the main tree)
            // Same goes when we remove a node from connected tree to make it a child of a row-node corresponding to a non-live row.
            Debug.Assert(fChildElementConnected == IsConnected(childElement));
            Debug.Assert(IsRowLive(child) ? IsConnected(childElement) : !IsConnected(childElement));
#endif
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnNodeChanged(object sender, XmlNodeChangedEventArgs args)
        {
            if (_ignoreXmlEvents)
                return;

            bool wasIgnoreDataSetEvents = _ignoreDataSetEvents;
            bool wasIgnoreXmlEvents = _ignoreXmlEvents;
            bool wasFoliationEnabled = IsFoliationEnabled;
            _ignoreDataSetEvents = true;
            _ignoreXmlEvents = true;
            IsFoliationEnabled = false;
            bool fEnableCascading = DataSet._fEnableCascading;
            DataSet._fEnableCascading = false;

            try
            {
                // okay to allow text node value changes when bound.
                XmlBoundElement? rowElement = null;

                Debug.Assert(DataSet.EnforceConstraints == false);

                if (_mapper.GetRegion(args.Node, out rowElement))
                {
                    SynchronizeRowFromRowElement(rowElement);
                }
            }
            finally
            {
                _ignoreDataSetEvents = wasIgnoreDataSetEvents;
                _ignoreXmlEvents = wasIgnoreXmlEvents;
                IsFoliationEnabled = wasFoliationEnabled;
                DataSet._fEnableCascading = fEnableCascading;
            }
        }

        private void OnNodeChanging(object sender, XmlNodeChangedEventArgs args)
        {
            if (_ignoreXmlEvents)
                return;
            if (DataSet.EnforceConstraints != false)
                throw new InvalidOperationException(SR.DataDom_EnforceConstraintsShouldBeOff);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnNodeInserted(object sender, XmlNodeChangedEventArgs args)
        {
            if (_ignoreXmlEvents)
                return;

            bool wasIgnoreDataSetEvents = _ignoreDataSetEvents;
            bool wasIgnoreXmlEvents = _ignoreXmlEvents;
            bool wasFoliationEnabled = IsFoliationEnabled;
            _ignoreDataSetEvents = true;
            _ignoreXmlEvents = true;
            IsFoliationEnabled = false;

            Debug.Assert(DataSet.EnforceConstraints == false);

            bool fEnableCascading = DataSet._fEnableCascading;
            DataSet._fEnableCascading = false;

            try
            {
                // Handle both new node inserted and 2nd part of a move operation.
                XmlNode node = args.Node!;
                XmlNode? oldParent = args.OldParent;
                XmlNode? newParent = args.NewParent;

                // The code bellow assumes a move operation is fired by DOM in 2 steps: a Remvoe followed by an Insert - this is the 2nd part, the Insert.
                Debug.Assert(oldParent == null);
                if (IsConnected(newParent))
                {
                    // Inserting a node to connected tree
                    OnNodeInsertedInTree(node);
                }
                else
                {
                    // Inserting a node to disconnected tree
                    OnNodeInsertedInFragment(node);
                }
            }
            finally
            {
                _ignoreDataSetEvents = wasIgnoreDataSetEvents;
                _ignoreXmlEvents = wasIgnoreXmlEvents;
                IsFoliationEnabled = wasFoliationEnabled;
                DataSet._fEnableCascading = fEnableCascading;
            }
        }

        private void OnNodeInserting(object sender, XmlNodeChangedEventArgs args)
        {
            if (_ignoreXmlEvents)
                return;
            if (DataSet.EnforceConstraints != false)
                throw new InvalidOperationException(SR.DataDom_EnforceConstraintsShouldBeOff);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnNodeRemoved(object sender, XmlNodeChangedEventArgs args)
        {
            if (_ignoreXmlEvents)
                return;

            bool wasIgnoreDataSetEvents = _ignoreDataSetEvents;
            bool wasIgnoreXmlEvents = _ignoreXmlEvents;
            bool wasFoliationEnabled = IsFoliationEnabled;
            _ignoreDataSetEvents = true;
            _ignoreXmlEvents = true;
            IsFoliationEnabled = false;

            Debug.Assert(DataSet.EnforceConstraints == false);

            bool fEnableCascading = DataSet._fEnableCascading;
            DataSet._fEnableCascading = false;

            try
            {
                XmlNode node = args.Node!;
                XmlNode? oldParent = args.OldParent;
                Debug.Assert(args.NewParent == null);

                if (IsConnected(oldParent))
                {
                    // Removing from connected tree to disconnected tree
                    OnNodeRemovedFromTree(node, oldParent);
                }
                else
                {
                    // Removing from disconnected tree to disconnected tree: just sync the old region
                    OnNodeRemovedFromFragment(node, oldParent);
                }
            }
            finally
            {
                _ignoreDataSetEvents = wasIgnoreDataSetEvents;
                _ignoreXmlEvents = wasIgnoreXmlEvents;
                IsFoliationEnabled = wasFoliationEnabled;
                DataSet._fEnableCascading = fEnableCascading;
            }
        }

        private void OnNodeRemoving(object sender, XmlNodeChangedEventArgs args)
        {
            if (_ignoreXmlEvents)
                return;
            if (DataSet.EnforceConstraints != false)
                throw new InvalidOperationException(SR.DataDom_EnforceConstraintsShouldBeOff);
        }

        // Node was removed from connected tree to disconnected tree
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnNodeRemovedFromTree(XmlNode node, XmlNode? oldParent)
        {
            XmlBoundElement? oldRowElem;

            // Synchronize values from old region
            if (_mapper.GetRegion(oldParent, out oldRowElem))
                SynchronizeRowFromRowElement(oldRowElem);

            // Disconnect all regions, starting w/ node (if it is a row-elem)
            XmlBoundElement? rowElem = node as XmlBoundElement;
            if (rowElem != null && rowElem.Row != null)
                EnsureDisconnectedDataRow(rowElem);
            TreeIterator iter = new TreeIterator(node);
            for (bool fMore = iter.NextRowElement(); fMore; fMore = iter.NextRowElement())
            {
                rowElem = (XmlBoundElement)(iter.CurrentNode!);
                EnsureDisconnectedDataRow(rowElem);
            }

            // Assert that all sub-regions are disconnected
            AssertNonLiveRows(node);
        }

        // Node was removed from the disconnected tree to disconnected tree
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnNodeRemovedFromFragment(XmlNode node, XmlNode? oldParent)
        {
            XmlBoundElement? oldRowElem;

            if (_mapper.GetRegion(oldParent, out oldRowElem))
            {
                // Sync the old region if it is not deleted
                DataRow row = oldRowElem.Row!;
                // Since the old region was disconnected, then the row can be only Deleted or Detached
                Debug.Assert(!IsRowLive(row));
                if (oldRowElem.Row!.RowState == DataRowState.Detached)
                    SynchronizeRowFromRowElement(oldRowElem);
            }

            // Need to set nested for the sub-regions (if node is a row-elem, we need to set it just for itself)
            XmlBoundElement? be = node as XmlBoundElement;
            if (be != null && be.Row != null)
            {
                Debug.Assert(!IsRowLive(be.Row));
                SetNestedParentRegion(be, null);
            }
            else
            {
                // Set nested parent to null for all child regions
                TreeIterator iter = new TreeIterator(node);
                for (bool fMore = iter.NextRowElement(); fMore; fMore = iter.NextRightRowElement())
                {
                    XmlBoundElement rowElemChild = (XmlBoundElement)(iter.CurrentNode!);
                    SetNestedParentRegion(rowElemChild, null);
                }
            }

            // Assert that all sub-regions are disconnected
            AssertNonLiveRows(node);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnRowChanged(object sender, DataRowChangeEventArgs args)
        {
            if (_ignoreDataSetEvents)
                return;

            _ignoreXmlEvents = true;
            bool wasFoliationEnabled = IsFoliationEnabled;
            IsFoliationEnabled = false;

            try
            {
                DataRow row = args.Row;
                XmlBoundElement? rowElement = row.Element;
                // We should have an associated row-elem created when the DataRow was created (or at the load time)
                Debug.Assert(rowElement != null);

                switch (args.Action)
                {
                    case DataRowAction.Add:
                        OnAddRow(row);
                        break;

                    case DataRowAction.Delete:
                        OnDeleteRow(row, rowElement);
                        break;

                    case DataRowAction.Rollback:
                        switch (_rollbackState)
                        {
                            case DataRowState.Deleted:
                                OnUndeleteRow(row, rowElement);
                                UpdateAllColumns(row, rowElement);
                                break;

                            case DataRowState.Added:
                                rowElement.ParentNode!.RemoveChild(rowElement);
                                break;

                            case DataRowState.Modified:
                                OnColumnValuesChanged(row, rowElement);
                                break;
                        }
                        break;

                    case DataRowAction.Change:
                        OnColumnValuesChanged(row, rowElement);
                        break;

                    case DataRowAction.Commit:
                        if (row.RowState == DataRowState.Detached)
                        {
                            //by now, all the descendent of the element that is not of this region should have been promoted already
                            rowElement.RemoveAll();
                        }
                        break;
                    default:
                        break;
                }
            }
            finally
            {
                IsFoliationEnabled = wasFoliationEnabled;
                _ignoreXmlEvents = false;
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnRowChanging(object sender, DataRowChangeEventArgs args)
        {
            // We foliate the region each time the assocaited row gets deleted
            DataRow row = args.Row;
            if (args.Action == DataRowAction.Delete && row.Element != null)
            {
                OnDeletingRow(row, row.Element);
                return;
            }

            if (_ignoreDataSetEvents)
                return;

            bool wasFoliationEnabled = IsFoliationEnabled;
            IsFoliationEnabled = false;

            try
            {
                _ignoreXmlEvents = true;

                XmlElement rowElement = GetElementFromRow(row);

                int nRec1 = -1;
                int nRec2 = -1;

                if (rowElement != null)
                {
                    switch (args.Action)
                    {
                        case DataRowAction.Add:
                            // DataRow is beeing added to the table (Table.Rows.Add is beeing called)
                            break;

                        case DataRowAction.Delete:
                            // DataRow is being deleted
                            //    - state transition from New (AKA PendingInsert) to Detached (AKA Created)
                            //    - state transition from Unchanged to Deleted (AKA PendingDelete)
                            //    - state transition from Modified (AKA PendingChange) to Delete (AKA PendingDelete)
                            Debug.Fail("This should have been handled above, irrespective of ignoreDataSetEvents value (true or false)");
                            break;

                        case DataRowAction.Rollback:
                            // DataRow gets reverted to previous values (by calling DataRow.RejectChanges):
                            //    - state transition from Detached (AKA Created) to Detached (AKA Created)
                            //    - state transition from New (AKA PendingInsert) to Detached (AKA Created)
                            //    - state transition from Modified (AKA PendingChange) to Unchanged
                            //    - state transition from Deleted (AKA PendingDelete) to Unchanged
                            _rollbackState = row.RowState;
                            switch (_rollbackState)
                            {
                                case DataRowState.Deleted:
                                    break;

                                case DataRowState.Detached:
                                    break;

                                case DataRowState.Added:
                                    break;

                                case DataRowState.Modified:
                                    _columnChangeList.Clear();
                                    nRec1 = row.GetRecordFromVersion(DataRowVersion.Original);
                                    nRec2 = row.GetRecordFromVersion(DataRowVersion.Current);
                                    foreach (DataColumn c in row.Table.Columns)
                                    {
                                        if (!IsSame(c, nRec1, nRec2))
                                            _columnChangeList.Add(c);
                                    }
                                    break;
                            }
                            break;

                        case DataRowAction.Change:
                            // A DataRow field is beeing changed
                            //    - state transition from New (AKA PendingInsert) to New (AKA PendingInsert)
                            //    - state transition from Unchanged to Modified (AKA PendingChange)
                            //    - state transition from Modified (AKA PendingChange) to Modified (AKA PendingChange)
                            _columnChangeList.Clear();
                            nRec1 = row.GetRecordFromVersion(DataRowVersion.Proposed);
                            nRec2 = row.GetRecordFromVersion(DataRowVersion.Current);
                            foreach (DataColumn c in row.Table.Columns)
                            {
                                object proposedValue = row[c, DataRowVersion.Proposed];
                                object currentValue = row[c, DataRowVersion.Current];
                                // Foliate if proposedValue is DBNull; this way the DataPointer objects will point to a disconnected fragment after
                                // the DBNull value is beeing set
                                if (Convert.IsDBNull(proposedValue) && !Convert.IsDBNull(currentValue))
                                {
                                    // Foliate only for non-hidden columns (since hidden cols are not represented in XML)
                                    if (c.ColumnMapping != MappingType.Hidden)
                                        FoliateIfDataPointers(row, rowElement);
                                }
                                if (!IsSame(c, nRec1, nRec2))
                                    _columnChangeList.Add(c);
                            }
                            break;

                        case DataRowAction.Commit:
                            break;
                    }
                }
            }
            finally
            {
                _ignoreXmlEvents = false;
                IsFoliationEnabled = wasFoliationEnabled;
            }
        }

        private void OnDataSetPropertyChanging(object? oDataSet, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "DataSetName")
                throw new InvalidOperationException(SR.DataDom_DataSetNameChange);
        }
        private void OnColumnPropertyChanging(object? oColumn, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "ColumnName")
                throw new InvalidOperationException(SR.DataDom_ColumnNameChange);
            if (args.PropertyName == "Namespace")
                throw new InvalidOperationException(SR.DataDom_ColumnNamespaceChange);
            if (args.PropertyName == "ColumnMapping")
                throw new InvalidOperationException(SR.DataDom_ColumnMappingChange);
        }
        private void OnTablePropertyChanging(object? oTable, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "TableName")
                throw new InvalidOperationException(SR.DataDom_TableNameChange);
            if (args.PropertyName == "Namespace")
                throw new InvalidOperationException(SR.DataDom_TableNamespaceChange);
        }
        private void OnTableColumnsChanging(object? oColumnsCollection, CollectionChangeEventArgs args)
        {
            // args.Action is one of CollectionChangeAction.Add, CollectionChangeAction.Remove or CollectionChangeAction.Refresh
            // args.Element is one of either the column (for Add and Remove actions or null, if the entire colection of columns is changing)

            // Disallow changing the columns collection (since we are subscribed only in populated mode, we allow changes in any state but non-populated mode)
            throw new InvalidOperationException(SR.DataDom_TableColumnsChange);
        }

        private void OnDataSetTablesChanging(object? oTablesCollection, CollectionChangeEventArgs args)
        {
            // args.Action is one of CollectionChangeAction.Add, CollectionChangeAction.Remove or CollectionChangeAction.Refresh
            // args.Element is a table

            // Disallow changing the tables collection (since we are subscribed only in populated mode, we allow changes in any state but non-populated mode)
            throw new InvalidOperationException(SR.DataDom_DataSetTablesChange);
        }

        private void OnDataSetRelationsChanging(object? oRelationsCollection, CollectionChangeEventArgs args)
        {
            // args.Action is one of CollectionChangeAction.Add, CollectionChangeAction.Remove or CollectionChangeAction.Refresh
            // args.Element is a DataRelation

            // Disallow changing the tables collection if there is data loaded and there are nested relationship that are added/refreshed
            DataRelation? rel = (DataRelation?)(args.Element);
            if (rel != null && rel.Nested)
                throw new InvalidOperationException(SR.DataDom_DataSetNestedRelationsChange);

            // If Add and Remove, we should already been throwing if .Nested == false
            Debug.Assert(!(args.Action == CollectionChangeAction.Add || args.Action == CollectionChangeAction.Remove) || rel!.Nested == false);
            if (args.Action == CollectionChangeAction.Refresh)
            {
                foreach (DataRelation relTemp in (DataRelationCollection)oRelationsCollection!)
                {
                    if (relTemp.Nested)
                    {
                        throw new InvalidOperationException(SR.DataDom_DataSetNestedRelationsChange);
                    }
                }
            }
        }

        private void OnRelationPropertyChanging(object? oRelationsCollection, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Nested")
                throw new InvalidOperationException(SR.DataDom_DataSetNestedRelationsChange);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnUndeleteRow(DataRow row, XmlElement rowElement)
        {
            XmlNode? refRow;
            XmlElement parent;

            // make certain we weren't place somewhere else.
            if (rowElement.ParentNode != null)
                rowElement.ParentNode.RemoveChild(rowElement);

            // Find the parent of RowNode to be inserted
            DataRow? parentRowInRelation = GetNestedParent(row);
            if (parentRowInRelation == null)
            {
                parent = EnsureNonRowDocumentElement();
            }
            else
                parent = GetElementFromRow(parentRowInRelation);

            if ((refRow = GetRowInsertBeforeLocation(row, rowElement, parent)) != null)
                parent.InsertBefore(rowElement, refRow);
            else
                parent.AppendChild(rowElement);

            FixNestedChildren(row, rowElement);
        }

        // Promote the rowElemChild node/region after prevSibling node (as the next sibling)
        private void PromoteChild(XmlNode child, XmlNode prevSibling)
        {
            // It makes no sense to move rowElemChild on the same level
            Debug.Assert(child.ParentNode != prevSibling.ParentNode);
            // prevSibling must have a parent, since we want to add a sibling to it
            Debug.Assert(prevSibling.ParentNode != null);
            Debug.Assert(IsFoliationEnabled == false);
            Debug.Assert(IgnoreXmlEvents == true);
            // Should not insert after docElem node
            Debug.Assert(prevSibling != DocumentElement);

            if (child.ParentNode != null)
                child.ParentNode.RemoveChild(child);

            Debug.Assert(child.ParentNode == null);
            prevSibling.ParentNode.InsertAfter(child, prevSibling);
        }

        // Promote child regions under parent as next siblings of parent
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void PromoteInnerRegions(XmlNode parent)
        {
            Debug.Assert(parent != null);
            Debug.Assert(parent.NodeType != XmlNodeType.Attribute);   // We need to get the grand-parent region
            Debug.Assert(parent != DocumentElement);                  // We cannot promote children of the DocumentElement

            XmlNode prevSibling = parent;
            XmlBoundElement? parentRegionRowElem;
            _mapper.GetRegion(parent.ParentNode, out parentRegionRowElem);

            TreeIterator iter = new TreeIterator(parent);
            bool fMore = iter.NextRowElement();
            while (fMore)
            {
                Debug.Assert(iter.CurrentNode is XmlBoundElement && ((XmlBoundElement)(iter.CurrentNode)).Row != null);
                XmlBoundElement rowElemChild = (XmlBoundElement)(iter.CurrentNode);
                fMore = iter.NextRightRowElement();
                PromoteChild(rowElemChild, prevSibling);
                SetNestedParentRegion(rowElemChild, parentRegionRowElem);
            }
        }

        private void PromoteNonValueChildren(XmlNode parent)
        {
            Debug.Assert(parent != null);
            XmlNode prevSibling = parent;
            XmlNode? child = parent.FirstChild;
            bool bTextLikeNode = true;
            XmlNode? nextSibling = null;
            while (child != null)
            {
                nextSibling = child.NextSibling;
                if (!bTextLikeNode || !IsTextLikeNode(child))
                {
                    bTextLikeNode = false;
                    nextSibling = child.NextSibling;
                    PromoteChild(child, prevSibling);
                    prevSibling = child;
                }
                child = nextSibling;
            }
        }

        private void RemoveInitialTextNodes(XmlNode? node)
        {
            while (node != null && IsTextLikeNode(node))
            {
                XmlNode? sibling = node.NextSibling;
                node.ParentNode!.RemoveChild(node);
                node = sibling;
            }
        }

        private void ReplaceInitialChildText(XmlNode parent, string value)
        {
            XmlNode? n = parent.FirstChild;

            // don't consider whitespace when replacing initial text
            while (n != null && n.NodeType == XmlNodeType.Whitespace)
                n = n.NextSibling;

            if (n != null)
            {
                if (n.NodeType == XmlNodeType.Text)
                    n.Value = value;
                else
                    n = parent.InsertBefore(CreateTextNode(value), n);
                RemoveInitialTextNodes(n!.NextSibling);
            }
            else
            {
                parent.AppendChild(CreateTextNode(value));
            }
        }

        internal XmlNode? SafeFirstChild(XmlNode n)
        {
            XmlBoundElement? be = n as XmlBoundElement;
            if (be != null)
                return be.SafeFirstChild;
            else
                //other type of node should be already foliated.
                return n.FirstChild;
        }

        internal XmlNode? SafeNextSibling(XmlNode n)
        {
            XmlBoundElement? be = n as XmlBoundElement;
            if (be != null)
                return be.SafeNextSibling;
            else
                //other type of node should be already foliated.
                return n.NextSibling;
        }

        internal XmlNode? SafePreviousSibling(XmlNode n)
        {
            XmlBoundElement? be = n as XmlBoundElement;
            if (be != null)
                return be.SafePreviousSibling;
            else
                //other type of node should be already foliated.
                return n.PreviousSibling;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal static void SetRowValueToNull(DataRow row, DataColumn col)
        {
            Debug.Assert(col.ColumnMapping != MappingType.Hidden);

            if (!(row.IsNull(col)))
            {
                row[col] = DBNull.Value;
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal static void SetRowValueFromXmlText(DataRow row, DataColumn col, string xmlText)
        {
            Debug.Assert(xmlText != null);
            Debug.Assert(row.Table.DataSet!.EnforceConstraints == false);
            object oVal;
            try
            {
                oVal = col.ConvertXmlToObject(xmlText);
                // This func does not set the field value to null - call SetRowValueToNull in order to do so
                Debug.Assert(oVal != null && !(oVal is DBNull));
            }
            catch (Exception e) when (Data.Common.ADP.IsCatchableExceptionType(e))
            {
                // Catch data-type errors and set ROM to Unspecified value
                SetRowValueToNull(row, col);
                return;
            }

            if (!oVal.Equals(row[col]))
                row[col] = oVal;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void SynchronizeRowFromRowElement(XmlBoundElement rowElement)
        {
            SynchronizeRowFromRowElement(rowElement, null);
        }

        // Sync row fields w/ values from rowElem region.
        // If rowElemList is != null, all subregions of rowElem are appended to it.
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void SynchronizeRowFromRowElement(XmlBoundElement rowElement, ArrayList? rowElemList)
        {
            DataRow? row = rowElement.Row;
            Debug.Assert(row != null);

            // No synchronization needed for deleted rows
            if (row.RowState == DataRowState.Deleted)
                return;

            row.BeginEdit();
#if DEBUG
            try
            {
#endif
                SynchronizeRowFromRowElementEx(rowElement, rowElemList);
#if DEBUG
            }
            catch
            {
                Debug.Fail("We should not get any exceptions because we always handle data-type conversion");
                throw;
            }
#endif
#if DEBUG
            try
            {
#endif
                row.EndEdit();
#if DEBUG
            }
            catch
            {
                Debug.Fail("We should not get any exceptions because DataSet.EnforceConstraints should be always off");
                throw;
            }
#endif
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void SynchronizeRowFromRowElementEx(XmlBoundElement rowElement, ArrayList? rowElemList)
        {
            Debug.Assert(rowElement != null);
            Debug.Assert(rowElement.Row != null);
            Debug.Assert(DataSet.EnforceConstraints == false);

            DataRow row = rowElement.Row;
            Debug.Assert(row != null);

            Hashtable foundColumns = new Hashtable();
            string xsi_attrVal = string.Empty;

            RegionIterator iter = new RegionIterator(rowElement);
            bool fMore;
            // If present, fill up the TextOnly column
            DataColumn? column = GetTextOnlyColumn(row);
            if (column != null)
            {
                foundColumns[column] = column;
                string value;
                fMore = iter.NextInitialTextLikeNodes(out value);
                if (value.Length == 0 && (((xsi_attrVal = rowElement.GetAttribute(XSI_NIL)) == "1") || xsi_attrVal == "true"))
                    row[column] = DBNull.Value;
                else
                    SetRowValueFromXmlText(row, column, value);
            }
            else
                fMore = iter.Next();

            // Fill up the columns mapped to an element
            while (fMore)
            {
                XmlElement? e = iter.CurrentNode as XmlElement;
                if (e == null)
                {
                    fMore = iter.Next();
                    continue;
                }

                XmlBoundElement? be = e as XmlBoundElement;
                if (be != null && be.Row != null)
                {
                    if (rowElemList != null)
                        rowElemList.Add(e);
                    // Skip over sub-regions
                    fMore = iter.NextRight();
                    continue;
                }

                DataColumn? c = _mapper.GetColumnSchemaForNode(rowElement, e);
                if (c != null)
                {
                    Debug.Assert(c.Table == row.Table);
                    if (foundColumns[c] == null)
                    {
                        foundColumns[c] = c;
                        string value;
                        fMore = iter.NextInitialTextLikeNodes(out value);
                        if (value.Length == 0 && (((xsi_attrVal = e.GetAttribute(XSI_NIL)) == "1") || xsi_attrVal == "true"))
                            row[c] = DBNull.Value;
                        else
                            SetRowValueFromXmlText(row, c, value);
                        continue;
                    }
                }

                fMore = iter.Next();
            }

            //
            // Walk the attributes to find attributes that map to columns.
            //
            foreach (XmlAttribute attr in rowElement.Attributes)
            {
                DataColumn? c = _mapper.GetColumnSchemaForNode(rowElement, attr);

                if (c != null)
                {
                    if (foundColumns[c] == null)
                    {
                        foundColumns[c] = c;
                        SetRowValueFromXmlText(row, c, attr.Value);
                    }
                }
            }

            // Null all columns values that aren't represented in the tree
            foreach (DataColumn c in row.Table.Columns)
            {
                if (foundColumns[c] == null && !IsNotMapped(c))
                {
                    if (!c.AutoIncrement)
                        SetRowValueToNull(row, c);
                    else
                        c.Init(row._tempRecord);
                }
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void UpdateAllColumns(DataRow row, XmlBoundElement rowElement)
        {
            foreach (DataColumn c in row.Table.Columns)
            {
                OnColumnValueChanged(row, c, rowElement);
            }
        }

        /// <summary>
        /// Initializes a new instance of the XmlDataDocument class.
        /// </summary>
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public XmlDataDocument() : base(new XmlDataImplementation())
        {
            Init();
            AttachDataSet(new DataSet());
            _dataSet.EnforceConstraints = false;
        }

        /// <summary>
        /// Initializes a new instance of the XmlDataDocument class with the specified
        /// DataSet.
        /// </summary>
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public XmlDataDocument(DataSet dataset) : base(new XmlDataImplementation())
        {
            Init(dataset);
        }

        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        internal XmlDataDocument(XmlImplementation imp) : base(imp)
        {
            // This constructor is used by XmlDataImplementation.CreateDocument(), which
            // exposes it as XmlDocument. The methods using these fields are never called.
            _dataSet = null!;
            _pointers = null!;
            _columnChangeList = null!;
            _mapper = null!;
            _foliationLock = null!;
            _attrXml = null!;
        }

        [MemberNotNull(nameof(_pointers))]
        [MemberNotNull(nameof(_columnChangeList))]
        [MemberNotNull(nameof(_mapper))]
        [MemberNotNull(nameof(_foliationLock))]
        [MemberNotNull(nameof(_attrXml))]
        private void Init()
        {
            _pointers = new Hashtable();
            _countAddPointer = 0;
            _columnChangeList = new ArrayList();
            _ignoreDataSetEvents = false;
            _isFoliationEnabled = true;
            _optimizeStorage = true;
            _fDataRowCreatedSpecial = false;
            _autoFoliationState = ElementState.StrongFoliation;
            _fAssociateDataRow = true; //this needs to be true for newly created elements should have associated datarows
            _mapper = new DataSetMapper();
            _foliationLock = new object();
            _ignoreXmlEvents = true;
            _attrXml = CreateAttribute("xmlns", "xml", XPathNodePointer.StrReservedXmlns);
            _attrXml.Value = XPathNodePointer.StrReservedXml;
            _ignoreXmlEvents = false;
        }

        [MemberNotNull(nameof(_pointers))]
        [MemberNotNull(nameof(_columnChangeList))]
        [MemberNotNull(nameof(_mapper))]
        [MemberNotNull(nameof(_foliationLock))]
        [MemberNotNull(nameof(_attrXml))]
        [MemberNotNull(nameof(_dataSet))]
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void Init(DataSet ds)
        {
            if (ds == null)
                throw new ArgumentException(SR.DataDom_DataSetNull);
            Init();
            if (ds.FBoundToDocument)
                throw new ArgumentException(SR.DataDom_MultipleDataSet);
            ds.FBoundToDocument = true;
            _dataSet = ds;
            Bind(true);
        }

        private bool IsConnected(XmlNode? node)
        {
            while (true)
            {
                if (node == null)
                    return false;
                if (node == this)
                    return true;

                XmlAttribute? attr = node as XmlAttribute;
                if (attr != null)
                    node = attr.OwnerElement;
                else
                    node = node.ParentNode;
            }
        }
        private bool IsRowLive(DataRow row)
        {
            return (row.RowState & (DataRowState.Added | DataRowState.Unchanged | DataRowState.Modified)) != 0;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private static void SetNestedParentRow(DataRow childRow, DataRow? parentRow)
        {
            DataRelation? rel = GetNestedParentRelation(childRow);
            //we should not set this row's parentRow if the table doesn't match.
            if (rel != null)
            {
                if (parentRow == null || rel.ParentKey.Table != parentRow.Table)
                    childRow.SetParentRow(null, rel);
                else
                    childRow.SetParentRow(parentRow, rel);
            }
        }

        // A node (node) was inserted into the main tree (connected) from oldParent==null state
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnNodeInsertedInTree(XmlNode node)
        {
            XmlBoundElement? be;
            ArrayList rowElemList = new ArrayList();
            if (_mapper.GetRegion(node, out be))
            {
                if (be == node)
                {
                    OnRowElementInsertedInTree(be, rowElemList);
                }
                else
                {
                    OnNonRowElementInsertedInTree(node, be, rowElemList);
                }
            }
            else
            {
                // We only need to sync the embedded sub-regions
                TreeIterator iter = new TreeIterator(node);
                for (bool fMore = iter.NextRowElement(); fMore; fMore = iter.NextRightRowElement())
                    rowElemList.Add(iter.CurrentNode);
            }

            // Process subregions, so they make transition from disconnected to connected tree
            while (rowElemList.Count > 0)
            {
                Debug.Assert(rowElemList[0] != null && rowElemList[0] is XmlBoundElement);
                XmlBoundElement? subRowElem = (XmlBoundElement?)(rowElemList[0]);
                rowElemList.RemoveAt(0);
                // Expect rowElem to have a DataTable schema, since it is a sub-region
                Debug.Assert(subRowElem != null);
                OnRowElementInsertedInTree(subRowElem, rowElemList);
            }

            // Assert that all sub-regions are assoc w/ "live" rows
            AssertLiveRows(node);
        }
        // "node" was inserting into a disconnected tree from oldParent==null state
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnNodeInsertedInFragment(XmlNode node)
        {
            XmlBoundElement? be;
            if (_mapper.GetRegion(node, out be))
            {
                if (be == node)
                {
                    Debug.Assert(!IsRowLive(be.Row!));
                    SetNestedParentRegion(be);
                }
                else
                {
                    ArrayList rowElemList = new ArrayList();
                    OnNonRowElementInsertedInFragment(node, be, rowElemList);
                    // Set nested parent for the 1st level subregions (they should already be associated w/ Deleted or Detached rows)
                    while (rowElemList.Count > 0)
                    {
                        Debug.Assert(rowElemList[0] != null && rowElemList[0] is XmlBoundElement);
                        XmlBoundElement subRowElem = (XmlBoundElement)(rowElemList[0]!);
                        rowElemList.RemoveAt(0);
                        SetNestedParentRegion(subRowElem, be);
                    }
                }

                // Check to make sure all sub-regions are disconnected
                AssertNonLiveRows(node);

                return;
            }

            // Nothing to do, since the node belongs to no region

            // Check to make sure all sub-regions are disconnected
            AssertNonLiveRows(node);
        }

        // A row-elem was inserted into the connected tree (connected) from oldParent==null state
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnRowElementInsertedInTree(XmlBoundElement rowElem, ArrayList rowElemList)
        {
            Debug.Assert(rowElem.Row != null);

            DataRow row = rowElem.Row;
            DataRowState rowState = row.RowState;

            switch (rowState)
            {
                case DataRowState.Detached:
#if DEBUG
                    try
                    {
                        Debug.Assert(row.Table.DataSet!.EnforceConstraints == false);
#endif
                        row.Table.Rows.Add(row);
                        SetNestedParentRegion(rowElem);
#if DEBUG
                    }
                    catch
                    {
                        Debug.Fail("We should not get any exceptions here");
                        throw;
                    }
#endif
                    // Add all sub-regions to the list if the caller needs this
                    if (rowElemList != null)
                    {
                        RegionIterator iter = new RegionIterator(rowElem);
                        for (bool fMore = iter.NextRowElement(); fMore; fMore = iter.NextRightRowElement())
                            rowElemList.Add(iter.CurrentNode);
                    }
                    break;
                case DataRowState.Deleted:
#if DEBUG
                    try
                    {
                        Debug.Assert(row.Table.DataSet!.EnforceConstraints == false);
#endif
                        // Change the row status to be alive (unchanged)
                        row.RejectChanges();
                        // Set ROM from XML
                        SynchronizeRowFromRowElement(rowElem, rowElemList);
                        // Set nested parent data row according to where is the row positioned in the tree
                        SetNestedParentRegion(rowElem);
#if DEBUG
                    }
                    catch
                    {
                        Debug.Fail("We should not get any exceptions here");
                        throw;
                    }
#endif
                    break;
                default:
                    Debug.Fail("Handle your case above");
                    break;
            }
            Debug.Assert(IsRowLive(rowElem.Row));
        }

        // Disconnect the DataRow associated w/ the rowElem region
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void EnsureDisconnectedDataRow(XmlBoundElement rowElem)
        {
            Debug.Assert(rowElem.Row != null);

            DataRow row = rowElem.Row;
            DataRowState rowState = row.RowState;

            switch (rowState)
            {
                case DataRowState.Detached:
#if DEBUG
                    try
                    {
                        Debug.Assert(row.Table.DataSet!.EnforceConstraints == false);
#endif
                        SetNestedParentRegion(rowElem);
#if DEBUG
                    }
                    catch
                    {
                        Debug.Fail("We should not get any exceptions here");
                        throw;
                    }
#endif
                    break;

                case DataRowState.Deleted:
                    // Nothing to do: moving a region associated w/ a deleted row to another disconnected tree is a NO-OP.
                    break;

                case DataRowState.Unchanged:
                case DataRowState.Modified:
                    EnsureFoliation(rowElem, ElementState.WeakFoliation);
                    row.Delete();
                    break;

                case DataRowState.Added:
                    EnsureFoliation(rowElem, ElementState.WeakFoliation);
                    row.Delete();
                    SetNestedParentRegion(rowElem);
                    break;

                default:
                    Debug.Fail("Handle your case above");
                    break;
            }

            Debug.Assert(!IsRowLive(rowElem.Row));
        }


        // A non-row-elem was inserted into the connected tree (connected) from oldParent==null state
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnNonRowElementInsertedInTree(XmlNode node, XmlBoundElement rowElement, ArrayList rowElemList)
        {
            // non-row-elem is beeing inserted
            DataRow? row = rowElement.Row;
            // Region should already have an associated data row (otherwise how was the original row-elem inserted ?)
            Debug.Assert(row != null);
            SynchronizeRowFromRowElement(rowElement);
            if (rowElemList != null)
            {
                TreeIterator iter = new TreeIterator(node);
                for (bool fMore = iter.NextRowElement(); fMore; fMore = iter.NextRightRowElement())
                    rowElemList.Add(iter.CurrentNode);
            }
        }

        // A non-row-elem was inserted into disconnected tree (fragment) from oldParent==null state (i.e. was disconnected)
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void OnNonRowElementInsertedInFragment(XmlNode node, XmlBoundElement rowElement, ArrayList rowElemList)
        {
            // non-row-elem is beeing inserted
            DataRow? row = rowElement.Row;
            // Region should already have an associated data row (otherwise how was the original row-elem inserted ?)
            Debug.Assert(row != null);
            // Since oldParent == null, the only 2 row states should have been Detached or Deleted
            Debug.Assert(row.RowState == DataRowState.Detached || row.RowState == DataRowState.Deleted);

            if (row.RowState == DataRowState.Detached)
                SynchronizeRowFromRowElementEx(rowElement, rowElemList);
            // Nothing to do if the row is deleted (there is no sync-ing from XML to ROM for deleted rows)
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void SetNestedParentRegion(XmlBoundElement childRowElem)
        {
            Debug.Assert(childRowElem.Row != null);

            XmlBoundElement? parentRowElem;
            _mapper.GetRegion(childRowElem.ParentNode, out parentRowElem);
            SetNestedParentRegion(childRowElem, parentRowElem);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void SetNestedParentRegion(XmlBoundElement childRowElem, XmlBoundElement? parentRowElem)
        {
            DataRow childRow = childRowElem.Row!;
            if (parentRowElem == null)
            {
                SetNestedParentRow(childRow, null);
                return;
            }

            Debug.Assert(parentRowElem.Row != null);
            DataRow parentRow = parentRowElem.Row;
            // We should set it only if there is a nested relationship between this child and parent regions
            DataRelation[] relations = childRow.Table.NestedParentRelations;
            if (relations.Length != 0 && relations[0].ParentTable == parentRow.Table) // just backward compatable
            {
                SetNestedParentRow(childRow, parentRow);
            }
            else
            {
                SetNestedParentRow(childRow, null);
            }
        }

        internal static bool IsTextNode(XmlNodeType nt)
        {
            switch (nt)
            {
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    return true;
                default:
                    return false;
            }
        }

        /*
        internal static bool IsWhiteSpace(char ch) {
            switch ( ch ) {
                case '\u0009' :
                case '\u000a' :
                case '\u000d' :
                case '\u0020' :
                    return true;
                default :
                    return false;
            }
        }

        internal static bool IsOnlyWhitespace( string str ) {
            if (str != null) {
                for (int index = 0; index < str.Length; index ++) {
                    if (! IsWhiteSpace(str[index]))
                        return false;
                }
            }
            return true;
        }
        */
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        protected override XPathNavigator? CreateNavigator(XmlNode node)
        {
            Debug.Assert(node.OwnerDocument == this || node == this);
            if (XPathNodePointer.s_xmlNodeType_To_XpathNodeType_Map[(int)(node.NodeType)] == -1)
                return null;
            if (IsTextNode(node.NodeType))
            {
                XmlNode? parent = node.ParentNode;
                if (parent != null && parent.NodeType == XmlNodeType.Attribute)
                    return null;
                else
                {
#if DEBUG
                    //if current node is a text node, its parent node has to be foliated
                    XmlBoundElement? be = node.ParentNode as XmlBoundElement;
                    if (be != null)
                        Debug.Assert(be.IsFoliated);
#endif
                    XmlNode? prevSib = node.PreviousSibling;
                    while (prevSib != null && IsTextNode(prevSib.NodeType))
                    {
                        node = prevSib;
                        prevSib = SafePreviousSibling(node);
                    }
                }
            }
            return new DataDocumentXPathNavigator(this, node);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void AssertLiveRows(XmlNode node)
        {
            bool wasFoliationEnabled = IsFoliationEnabled;
            IsFoliationEnabled = false;
            try
            {
                XmlBoundElement? rowElement = node as XmlBoundElement;
                if (rowElement != null && rowElement.Row != null)
                    Debug.Assert(IsRowLive(rowElement.Row));
                TreeIterator iter = new TreeIterator(node);
                for (bool fMore = iter.NextRowElement(); fMore; fMore = iter.NextRowElement())
                {
                    rowElement = iter.CurrentNode as XmlBoundElement;
                    Debug.Assert(rowElement!.Row != null);
                    Debug.Assert(IsRowLive(rowElement.Row));
                }
            }
            finally
            {
                IsFoliationEnabled = wasFoliationEnabled;
            }
        }
        [System.Diagnostics.Conditional("DEBUG")]
        private void AssertNonLiveRows(XmlNode node)
        {
            bool wasFoliationEnabled = IsFoliationEnabled;
            IsFoliationEnabled = false;
            try
            {
                XmlBoundElement? rowElement = node as XmlBoundElement;
                if (rowElement != null && rowElement.Row != null)
                    Debug.Assert(!IsRowLive(rowElement.Row));
                TreeIterator iter = new TreeIterator(node);
                for (bool fMore = iter.NextRowElement(); fMore; fMore = iter.NextRowElement())
                {
                    rowElement = iter.CurrentNode as XmlBoundElement;
                    Debug.Assert(rowElement!.Row != null);
                    Debug.Assert(!IsRowLive(rowElement.Row));
                }
            }
            finally
            {
                IsFoliationEnabled = wasFoliationEnabled;
            }
        }

        public override XmlElement? GetElementById(string elemId)
        {
            throw new NotSupportedException(SR.DataDom_NotSupport_GetElementById);
        }
        public override XmlNodeList GetElementsByTagName(string name)
        {
            // Retrieving nodes from the returned nodelist may cause foliation which causes new nodes to be created,
            // so the System.Xml iterator will throw if this happens during iteration. To avoid this, foliate everything
            // before iteration, so iteration will not cause foliation (and as a result of this, creation of new nodes).
            XmlNodeList tempNodeList = base.GetElementsByTagName(name);

            _ = tempNodeList.Count;
            return tempNodeList;
        }

        //  after adding Namespace support foir datatable, DataSet does not guarantee that infered tabels would be in the same sequence as they rae in XML, because
        //  of Namespace. if a table is in different namespace than its children and DataSet, that table would efinetely be added to DataSet after its children. Its By Design
        // so in order to maintain backward compatability, we reorder the copy of the datatable collection and use it
        private DataTable[] OrderTables(DataSet? ds)
        {
            DataTable[]? retValue = null;
            if (ds == null || ds.Tables.Count == 0)
            {
                retValue = Array.Empty<DataTable>();
            }
            else if (TablesAreOrdered(ds))
            {
                retValue = new DataTable[ds.Tables.Count];
                ds.Tables.CopyTo(retValue, 0);
                // XDD assumes PArent table exist before its child, if it does not we won't be handle the case
                // same as Everett
            }

            if (null == retValue)
            {
                retValue = new DataTable[ds!.Tables.Count];
                List<DataTable> tableList = new List<DataTable>();
                // first take the root tables that have no parent
                foreach (DataTable dt in ds.Tables)
                {
                    if (dt.ParentRelations.Count == 0)
                    {
                        tableList.Add(dt);
                    }
                }

                if (tableList.Count > 0)
                { // if we have some  table inside;
                    foreach (DataTable dt in ds.Tables)
                    {
                        if (IsSelfRelatedDataTable(dt))
                        {
                            tableList.Add(dt);
                        }
                    }
                    for (int readPos = 0; readPos < tableList.Count; readPos++)
                    {
                        Debug.Assert(tableList[readPos] != null, "Temp Array is not supposed to reach to null");
                        foreach (DataRelation r in tableList[readPos].ChildRelations)
                        {
                            DataTable childTable = r.ChildTable;
                            if (!tableList.Contains(childTable))
                                tableList.Add(childTable);
                        }
                    }
                    tableList.CopyTo(retValue);
                }
                else
                {//there will not be  any in case just if we have circular relation dependency, just copy as they are in tablecollection use CopyTo of the collection
                    ds.Tables.CopyTo(retValue, 0);
                }
            }
            return retValue;
        }
        private bool IsSelfRelatedDataTable(DataTable rootTable)
        {
            List<DataTable> tableList = new List<DataTable>();
            bool retValue = false;
            foreach (DataRelation r in rootTable.ChildRelations)
            {
                DataTable childTable = r.ChildTable;
                if (childTable == rootTable)
                {
                    retValue = true;
                    break;
                }
                else if (!tableList.Contains(childTable))
                {
                    tableList.Add(childTable);
                }
            }
            if (!retValue)
            {
                for (int counter = 0; counter < tableList.Count; counter++)
                {
                    foreach (DataRelation r in tableList[counter].ChildRelations)
                    {
                        DataTable childTable = r.ChildTable;
                        if (childTable == rootTable)
                        {
                            retValue = true;
                            break;
                        }
                        else if (!tableList.Contains(childTable))
                        {
                            tableList.Add(childTable);
                        }
                    }
                    if (retValue)
                    {
                        break;
                    }
                }
            }
            return retValue;
        }
        private bool TablesAreOrdered(DataSet ds)
        {
            foreach (DataTable dt in ds.Tables)
            {
                if (dt.Namespace != ds.Namespace)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
