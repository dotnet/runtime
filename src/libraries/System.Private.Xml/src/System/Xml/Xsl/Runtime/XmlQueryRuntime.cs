// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl.IlGen;
using System.Xml.Xsl.Xslt;
using MS.Internal.Xml.XPath;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// XmlQueryRuntime is passed as the first parameter to all generated query methods.
    ///
    /// XmlQueryRuntime contains runtime support for generated ILGen queries:
    ///   1. Stack of output writers (stack handles nested document construction)
    ///   2. Manages list of all xml types that are used within the query
    ///   3. Manages list of all atomized names that are used within the query
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class XmlQueryRuntime
    {
        // Early-Bound Library Objects
        private readonly XmlQueryContext _ctxt;
        private XsltLibrary _xsltLib;
        private readonly EarlyBoundInfo[] _earlyInfo;
        private readonly object[] _earlyObjects;

        // Global variables and parameters
        private readonly string[] _globalNames;
        private readonly object[] _globalValues;

        // Names, prefix mappings, and name filters
        private readonly XmlNameTable _nameTableQuery;
        private readonly string[] _atomizedNames;             // Names after atomization
        private readonly XmlNavigatorFilter[] _filters;       // Name filters (contain atomized names)
        private readonly StringPair[][] _prefixMappingsList;  // Lists of prefix mappings (used to resolve computed names)

        // Xml types
        private readonly XmlQueryType[] _types;

        // Collations
        private readonly XmlCollation[] _collations;

        // Document ordering
        private readonly DocumentOrderComparer _docOrderCmp;

        // Indexes
        private ArrayList[] _indexes;

        // Output construction
        private XmlQueryOutput _output;
        private readonly Stack<XmlQueryOutput> _stkOutput;


        //-----------------------------------------------
        // Constructors
        //-----------------------------------------------

        /// <summary>
        /// This constructor is internal so that external users cannot construct it (and therefore we do not have to test it separately).
        /// </summary>
        internal XmlQueryRuntime(XmlQueryStaticData data, object defaultDataSource, XmlResolver dataSources, XsltArgumentList argList, XmlSequenceWriter seqWrt)
        {
            Debug.Assert(data != null);
            string[] names = data.Names;
            Int32Pair[] filters = data.Filters;
            WhitespaceRuleLookup wsRules;
            int i;

            // Early-Bound Library Objects
            wsRules = (data.WhitespaceRules != null && data.WhitespaceRules.Count != 0) ? new WhitespaceRuleLookup(data.WhitespaceRules) : null;
            _ctxt = new XmlQueryContext(this, defaultDataSource, dataSources, argList, wsRules);
            _xsltLib = null;
            _earlyInfo = data.EarlyBound;
            _earlyObjects = (_earlyInfo != null) ? new object[_earlyInfo.Length] : null;

            // Global variables and parameters
            _globalNames = data.GlobalNames;
            _globalValues = (_globalNames != null) ? new object[_globalNames.Length] : null;

            // Names
            _nameTableQuery = _ctxt.QueryNameTable;
            _atomizedNames = null;

            if (names != null)
            {
                // Atomize all names in "nameTableQuery".  Use names from the default data source's
                // name table when possible.
                XmlNameTable nameTableDefault = _ctxt.DefaultNameTable;
                _atomizedNames = new string[names.Length];

                if (nameTableDefault != _nameTableQuery && nameTableDefault != null)
                {
                    // Ensure that atomized names from the default data source are added to the
                    // name table used in this query
                    for (i = 0; i < names.Length; i++)
                    {
                        string name = nameTableDefault.Get(names[i]);
                        _atomizedNames[i] = _nameTableQuery.Add(name ?? names[i]);
                    }
                }
                else
                {
                    // Enter names into nametable used in this query
                    for (i = 0; i < names.Length; i++)
                        _atomizedNames[i] = _nameTableQuery.Add(names[i]);
                }
            }

            // Name filters
            _filters = null;
            if (filters != null)
            {
                // Construct name filters.  Each pair of integers in the filters[] array specifies the
                // (localName, namespaceUri) of the NameFilter to be created.
                _filters = new XmlNavigatorFilter[filters.Length];

                for (i = 0; i < filters.Length; i++)
                    _filters[i] = XmlNavNameFilter.Create(_atomizedNames[filters[i].Left], _atomizedNames[filters[i].Right]);
            }

            // Prefix maping lists
            _prefixMappingsList = data.PrefixMappingsList;

            // Xml types
            _types = data.Types;

            // Xml collations
            _collations = data.Collations;

            // Document ordering
            _docOrderCmp = new DocumentOrderComparer();

            // Indexes
            _indexes = null;

            // Output construction
            _stkOutput = new Stack<XmlQueryOutput>(16);
            _output = new XmlQueryOutput(this, seqWrt);
        }


        //-----------------------------------------------
        // Debugger Utility Methods
        //-----------------------------------------------

        /// <summary>
        /// Return array containing the names of all the global variables and parameters used in this query, in this format:
        ///     {namespace}prefix:local-name
        /// </summary>
        public string[] DebugGetGlobalNames()
        {
            return _globalNames;
        }

        /// <summary>
        /// Get the value of a global value having the specified name.  Always return the global value as a list of XPathItem.
        /// Return null if there is no global value having the specified name.
        /// </summary>
        public IList DebugGetGlobalValue(string name)
        {
            for (int idx = 0; idx < _globalNames.Length; idx++)
            {
                if (_globalNames[idx] == name)
                {
                    Debug.Assert(IsGlobalComputed(idx), "Cannot get the value of a global value until it has been computed.");
                    Debug.Assert(_globalValues[idx] is IList<XPathItem>, "Only debugger should call this method, and all global values should have type item* in debugging scenarios.");
                    return (IList)_globalValues[idx];
                }
            }
            return null;
        }

        /// <summary>
        /// Set the value of a global value having the specified name.  If there is no such value, this method is a no-op.
        /// </summary>
        public void DebugSetGlobalValue(string name, object value)
        {
            for (int idx = 0; idx < _globalNames.Length; idx++)
            {
                if (_globalNames[idx] == name)
                {
                    Debug.Assert(IsGlobalComputed(idx), "Cannot get the value of a global value until it has been computed.");
                    Debug.Assert(_globalValues[idx] is IList<XPathItem>, "Only debugger should call this method, and all global values should have type item* in debugging scenarios.");

                    // Always convert "value" to a list of XPathItem using the item* converter
                    _globalValues[idx] = (IList<XPathItem>)XmlAnyListConverter.ItemList.ChangeType(value, typeof(XPathItem[]), null);
                    break;
                }
            }
        }

        /// <summary>
        /// Convert sequence to it's appropriate XSLT type and return to caller.
        /// </summary>
        public object DebugGetXsltValue(IList seq)
        {
            if (seq != null && seq.Count == 1)
            {
                XPathItem item = seq[0] as XPathItem;
                if (item != null && !item.IsNode)
                {
                    return item.TypedValue;
                }
                else if (item is RtfNavigator)
                {
                    return ((RtfNavigator)item).ToNavigator();
                }
            }

            return seq;
        }


        //-----------------------------------------------
        // Early-Bound Library Objects
        //-----------------------------------------------

        internal const BindingFlags EarlyBoundFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        internal const BindingFlags LateBoundFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        /// <summary>
        /// Return the object that manages external user context information such as data sources, parameters, extension objects, etc.
        /// </summary>
        public XmlQueryContext ExternalContext
        {
            get { return _ctxt; }
        }

        /// <summary>
        /// Return the object that manages the state needed to implement various Xslt functions.
        /// </summary>
        public XsltLibrary XsltFunctions
        {
            get
            {
                if (_xsltLib == null)
                {
                    _xsltLib = new XsltLibrary(this);
                }

                return _xsltLib;
            }
        }

        /// <summary>
        /// Get the early-bound extension object identified by "index".  If it does not yet exist, create an instance using the
        /// corresponding ConstructorInfo.
        /// </summary>
        public object GetEarlyBoundObject(int index)
        {
            object obj;
            Debug.Assert(_earlyObjects != null && index < _earlyObjects.Length, "Early bound object does not exist");

            obj = _earlyObjects[index];
            if (obj == null)
            {
                // Early-bound object does not yet exist, so create it now
                obj = _earlyInfo[index].CreateObject();
                _earlyObjects[index] = obj;
            }

            return obj;
        }

        /// <summary>
        /// Return true if the early bound object identified by "namespaceUri" contains a method that matches "name".
        /// </summary>
        [RequiresUnreferencedCode(Scripts.ExtensionFunctionCannotBeStaticallyAnalyzed)]
        public bool EarlyBoundFunctionExists(string name, string namespaceUri)
        {
            if (_earlyInfo == null)
                return false;

            for (int idx = 0; idx < _earlyInfo.Length; idx++)
            {
                if (namespaceUri == _earlyInfo[idx].NamespaceUri)
                    return new XmlExtensionFunction(name, namespaceUri, -1, _earlyInfo[idx].EarlyBoundType, EarlyBoundFlags).CanBind();
            }

            return false;
        }


        //-----------------------------------------------
        // Global variables and parameters
        //-----------------------------------------------

        /// <summary>
        /// Return true if the global value specified by idxValue was previously computed.
        /// </summary>
        public bool IsGlobalComputed(int index)
        {
            return _globalValues[index] != null;
        }

        /// <summary>
        /// Return the value that is bound to the global variable or parameter specified by idxValue.
        /// If the value has not yet been computed, then compute it now and store it in this.globalValues.
        /// </summary>
        public object GetGlobalValue(int index)
        {
            Debug.Assert(IsGlobalComputed(index), "Cannot get the value of a global value until it has been computed.");
            return _globalValues[index];
        }

        /// <summary>
        /// Return the value that is bound to the global variable or parameter specified by idxValue.
        /// If the value has not yet been computed, then compute it now and store it in this.globalValues.
        /// </summary>
        public void SetGlobalValue(int index, object value)
        {
            Debug.Assert(!IsGlobalComputed(index), "Global value should only be set once.");
            _globalValues[index] = value;
        }


        //-----------------------------------------------
        // Names, prefix mappings, and name filters
        //-----------------------------------------------

        /// <summary>
        /// Return the name table used to atomize all names used by the query.
        /// </summary>
        public XmlNameTable NameTable
        {
            get { return _nameTableQuery; }
        }

        /// <summary>
        /// Get the atomized name at the specified index in the array of names.
        /// </summary>
        public string GetAtomizedName(int index)
        {
            Debug.Assert(_atomizedNames != null);
            return _atomizedNames[index];
        }

        /// <summary>
        /// Get the name filter at the specified index in the array of filters.
        /// </summary>
        public XmlNavigatorFilter GetNameFilter(int index)
        {
            Debug.Assert(_filters != null);
            return _filters[index];
        }

        /// <summary>
        /// XPathNodeType.All: Filters all nodes
        /// XPathNodeType.Attribute: Filters attributes
        /// XPathNodeType.Namespace: Not allowed
        /// XPathNodeType.XXX: Filters all nodes *except* those having XPathNodeType.XXX
        /// </summary>
        public XmlNavigatorFilter GetTypeFilter(XPathNodeType nodeType)
        {
            if (nodeType == XPathNodeType.All)
                return XmlNavNeverFilter.Create();

            if (nodeType == XPathNodeType.Attribute)
                return XmlNavAttrFilter.Create();

            return XmlNavTypeFilter.Create(nodeType);
        }

        /// <summary>
        /// Parse the specified tag name (foo:bar) and resolve the resulting prefix.  If the prefix cannot be resolved,
        /// then throw an error.  Return an XmlQualifiedName.
        /// </summary>
        public XmlQualifiedName ParseTagName(string tagName, int indexPrefixMappings)
        {
            string prefix, localName, ns;

            // Parse the tagName as a prefix, localName pair and resolve the prefix
            ParseTagName(tagName, indexPrefixMappings, out prefix, out localName, out ns);
            return new XmlQualifiedName(localName, ns);
        }

        /// <summary>
        /// Parse the specified tag name (foo:bar).  Return an XmlQualifiedName consisting of the parsed local name
        /// and the specified namespace.
        /// </summary>
        public XmlQualifiedName ParseTagName(string tagName, string ns)
        {
            string prefix, localName;

            // Parse the tagName as a prefix, localName pair
            ValidateNames.ParseQNameThrow(tagName, out prefix, out localName);
            return new XmlQualifiedName(localName, ns);
        }

        /// <summary>
        /// Parse the specified tag name (foo:bar) and resolve the resulting prefix.  If the prefix cannot be resolved,
        /// then throw an error.  Return the prefix, localName, and namespace URI.
        /// </summary>
        internal void ParseTagName(string tagName, int idxPrefixMappings, out string prefix, out string localName, out string ns)
        {
            Debug.Assert(_prefixMappingsList != null);

            // Parse the tagName as a prefix, localName pair
            ValidateNames.ParseQNameThrow(tagName, out prefix, out localName);

            // Map the prefix to a namespace URI
            ns = null;
            foreach (StringPair pair in _prefixMappingsList[idxPrefixMappings])
            {
                if (prefix == pair.Left)
                {
                    ns = pair.Right;
                    break;
                }
            }

            // Throw exception if prefix could not be resolved
            if (ns == null)
            {
                // Check for mappings that are always in-scope
                if (prefix.Length == 0)
                    ns = "";
                else if (prefix.Equals("xml"))
                    ns = XmlReservedNs.NsXml;
                // It is not correct to resolve xmlns prefix in XPath but removing it would be a breaking change.
                else if (prefix.Equals("xmlns"))
                    ns = XmlReservedNs.NsXmlNs;
                else
                    throw new XslTransformException(SR.Xslt_InvalidPrefix, prefix);
            }
        }

        /// <summary>
        /// Return true if the nav1's LocalName and NamespaceURI properties equal nav2's corresponding properties.
        /// </summary>
        public bool IsQNameEqual(XPathNavigator n1, XPathNavigator n2)
        {
            if ((object)n1.NameTable == (object)n2.NameTable)
            {
                // Use atomized comparison
                return (object)n1.LocalName == (object)n2.LocalName && (object)n1.NamespaceURI == (object)n2.NamespaceURI;
            }

            return (n1.LocalName == n2.LocalName) && (n1.NamespaceURI == n2.NamespaceURI);
        }

        /// <summary>
        /// Return true if the specified navigator's LocalName and NamespaceURI properties equal the argument names.
        /// </summary>
        public bool IsQNameEqual(XPathNavigator navigator, int indexLocalName, int indexNamespaceUri)
        {
            if ((object)navigator.NameTable == (object)_nameTableQuery)
            {
                // Use atomized comparison
                return ((object)GetAtomizedName(indexLocalName) == (object)navigator.LocalName &&
                        (object)GetAtomizedName(indexNamespaceUri) == (object)navigator.NamespaceURI);
            }

            // Use string comparison
            return (GetAtomizedName(indexLocalName) == navigator.LocalName) && (GetAtomizedName(indexNamespaceUri) == navigator.NamespaceURI);
        }

        /// <summary>
        /// Get the Xml query type at the specified index in the array of types.
        /// </summary>
        internal XmlQueryType GetXmlType(int idxType)
        {
            Debug.Assert(_types != null);
            return _types[idxType];
        }

        /// <summary>
        /// Forward call to ChangeTypeXsltArgument(XmlQueryType, object, Type).
        /// </summary>
        public object ChangeTypeXsltArgument(int indexType, object value, Type destinationType)
        {
            return ChangeTypeXsltArgument(GetXmlType(indexType), value, destinationType);
        }

        /// <summary>
        /// Convert from the Clr type of "value" to Clr type "destinationType" using V1 Xslt rules.
        /// These rules include converting any Rtf values to Nodes.
        /// </summary>
        internal object ChangeTypeXsltArgument(XmlQueryType xmlType, object value, Type destinationType)
        {
            Debug.Assert(XmlILTypeHelper.GetStorageType(xmlType).IsAssignableFrom(value.GetType()),
                         "Values passed to ChangeTypeXsltArgument should be in ILGen's default Clr representation.");

            Debug.Assert(destinationType == XsltConvert.ObjectType || !destinationType.IsAssignableFrom(value.GetType()),
                         $"No need to call ChangeTypeXsltArgument since value is already assignable to destinationType {destinationType}");

            switch (xmlType.TypeCode)
            {
                case XmlTypeCode.String:
                    if (destinationType == XsltConvert.DateTimeType)
                        value = XsltConvert.ToDateTime((string)value);
                    break;

                case XmlTypeCode.Double:
                    if (destinationType != XsltConvert.DoubleType)
                        value = Convert.ChangeType(value, destinationType, CultureInfo.InvariantCulture);
                    break;

                case XmlTypeCode.Node:
                    Debug.Assert(xmlType != XmlQueryTypeFactory.Node && xmlType != XmlQueryTypeFactory.NodeS,
                                 "Rtf values should have been eliminated by caller.");

                    if (destinationType == XsltConvert.XPathNodeIteratorType)
                    {
                        value = new XPathArrayIterator((IList)value);
                    }
                    else if (destinationType == XsltConvert.XPathNavigatorArrayType)
                    {
                        // Copy sequence to XPathNavigator[]
                        IList<XPathNavigator> seq = (IList<XPathNavigator>)value;
                        XPathNavigator[] navArray = new XPathNavigator[seq.Count];

                        for (int i = 0; i < seq.Count; i++)
                            navArray[i] = seq[i];

                        value = navArray;
                    }
                    break;

                case XmlTypeCode.Item:
                    {
                        // Only typeof(object) is supported as a destination type
                        if (destinationType != XsltConvert.ObjectType)
                            throw new XslTransformException(SR.Xslt_UnsupportedClrType, destinationType.Name);

                        // Convert to default, backwards-compatible representation
                        //   1. NodeSet: System.Xml.XPath.XPathNodeIterator
                        //   2. Rtf: System.Xml.XPath.XPathNavigator
                        //   3. Other:   Default V1 representation
                        IList<XPathItem> seq = (IList<XPathItem>)value;
                        if (seq.Count == 1)
                        {
                            XPathItem item = seq[0];

                            if (item.IsNode)
                            {
                                // Node or Rtf
                                RtfNavigator rtf = item as RtfNavigator;
                                if (rtf != null)
                                    value = rtf.ToNavigator();
                                else
                                    value = new XPathArrayIterator((IList)value);
                            }
                            else
                            {
                                // Atomic value
                                value = item.TypedValue;
                            }
                        }
                        else
                        {
                            // Nodeset
                            value = new XPathArrayIterator((IList)value);
                        }
                        break;
                    }
            }

            Debug.Assert(destinationType.IsAssignableFrom(value.GetType()), $"ChangeType from type {value.GetType().Name} to type {destinationType.Name} failed");
            return value;
        }

        /// <summary>
        /// Forward call to ChangeTypeXsltResult(XmlQueryType, object)
        /// </summary>
        public object ChangeTypeXsltResult(int indexType, object value)
        {
            return ChangeTypeXsltResult(GetXmlType(indexType), value);
        }

        /// <summary>
        /// Convert from the Clr type of "value" to the default Clr type that ILGen uses to represent the xml type, using
        /// the conversion rules of the xml type.
        /// </summary>
        internal object ChangeTypeXsltResult(XmlQueryType xmlType, object value)
        {
            if (value == null)
                throw new XslTransformException(SR.Xslt_ItemNull, string.Empty);

            switch (xmlType.TypeCode)
            {
                case XmlTypeCode.String:
                    if (value.GetType() == XsltConvert.DateTimeType)
                        value = XsltConvert.ToString((DateTime)value);
                    break;

                case XmlTypeCode.Double:
                    if (value.GetType() != XsltConvert.DoubleType)
                        value = ((IConvertible)value).ToDouble(null);

                    break;

                case XmlTypeCode.Node:
                    if (!xmlType.IsSingleton)
                    {
                        XPathArrayIterator iter = value as XPathArrayIterator;

                        // Special-case XPathArrayIterator in order to avoid copies
                        if (iter != null && iter.AsList is XmlQueryNodeSequence)
                        {
                            value = iter.AsList as XmlQueryNodeSequence;
                        }
                        else
                        {
                            // Iterate over list and ensure it only contains nodes
                            XmlQueryNodeSequence seq = new XmlQueryNodeSequence();
                            IList list = value as IList;

                            if (list != null)
                            {
                                for (int i = 0; i < list.Count; i++)
                                    seq.Add(EnsureNavigator(list[i]));
                            }
                            else
                            {
                                foreach (object o in (IEnumerable)value)
                                    seq.Add(EnsureNavigator(o));
                            }

                            value = seq;
                        }

                        // Always sort node-set by document order
                        value = ((XmlQueryNodeSequence)value).DocOrderDistinct(_docOrderCmp);
                    }
                    break;

                case XmlTypeCode.Item:
                    {
                        Type sourceType = value.GetType();
                        IXPathNavigable navigable;

                        // If static type is item, then infer type based on dynamic value
                        switch (XsltConvert.InferXsltType(sourceType).TypeCode)
                        {
                            case XmlTypeCode.Boolean:
                                value = new XmlQueryItemSequence(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Boolean), value));
                                break;

                            case XmlTypeCode.Double:
                                value = new XmlQueryItemSequence(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Double), ((IConvertible)value).ToDouble(null)));
                                break;

                            case XmlTypeCode.String:
                                if (sourceType == XsltConvert.DateTimeType)
                                    value = new XmlQueryItemSequence(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String), XsltConvert.ToString((DateTime)value)));
                                else
                                    value = new XmlQueryItemSequence(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String), value));
                                break;

                            case XmlTypeCode.Node:
                                // Support XPathNavigator[]
                                value = ChangeTypeXsltResult(XmlQueryTypeFactory.NodeS, value);
                                break;

                            case XmlTypeCode.Item:
                                // Support XPathNodeIterator
                                if (value is XPathNodeIterator)
                                {
                                    value = ChangeTypeXsltResult(XmlQueryTypeFactory.NodeS, value);
                                    break;
                                }

                                // Support IXPathNavigable and XPathNavigator
                                navigable = value as IXPathNavigable;
                                if (navigable != null)
                                {
                                    if (value is XPathNavigator)
                                        value = new XmlQueryNodeSequence((XPathNavigator)value);
                                    else
                                        value = new XmlQueryNodeSequence(navigable.CreateNavigator());
                                    break;
                                }

                                throw new XslTransformException(SR.Xslt_UnsupportedClrType, sourceType.Name);
                        }
                        break;
                    }
            }

            Debug.Assert(XmlILTypeHelper.GetStorageType(xmlType).IsAssignableFrom(value.GetType()), $"Xml type {xmlType} is not represented in ILGen as {value.GetType().Name}");

            return value;
        }

        /// <summary>
        /// Ensure that "value" is a navigator and not null.
        /// </summary>
        private static XPathNavigator EnsureNavigator(object value)
        {
            XPathNavigator nav = value as XPathNavigator;

            if (nav == null)
                throw new XslTransformException(SR.Xslt_ItemNull, string.Empty);

            return nav;
        }

        /// <summary>
        /// Return true if the type of every item in "seq" matches the xml type identified by "idxType".
        /// </summary>
        public bool MatchesXmlType(IList<XPathItem> seq, int indexType)
        {
            XmlQueryType typBase = GetXmlType(indexType);

            XmlQueryCardinality card = seq.Count switch
            {
                0 => XmlQueryCardinality.Zero,
                1 => XmlQueryCardinality.One,
                _ => XmlQueryCardinality.More,
            };

            if (!(card <= typBase.Cardinality))
                return false;

            typBase = typBase.Prime;
            for (int i = 0; i < seq.Count; i++)
            {
                if (!CreateXmlType(seq[0]).IsSubtypeOf(typBase))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Return true if the type of "item" matches the xml type identified by "idxType".
        /// </summary>
        public bool MatchesXmlType(XPathItem item, int indexType)
        {
            return CreateXmlType(item).IsSubtypeOf(GetXmlType(indexType));
        }

        /// <summary>
        /// Return true if the type of "seq" is a subtype of a singleton type identified by "code".
        /// </summary>
        public bool MatchesXmlType(IList<XPathItem> seq, XmlTypeCode code)
        {
            if (seq.Count != 1)
                return false;

            return MatchesXmlType(seq[0], code);
        }

        /// <summary>
        /// Return true if the type of "item" is a subtype of the type identified by "code".
        /// </summary>
        public bool MatchesXmlType(XPathItem item, XmlTypeCode code)
        {
            // All atomic type codes appear after AnyAtomicType
            if (code > XmlTypeCode.AnyAtomicType)
                return !item.IsNode && item.XmlType.TypeCode == code;

            // Handle node code and AnyAtomicType
            switch (code)
            {
                case XmlTypeCode.AnyAtomicType: return !item.IsNode;
                case XmlTypeCode.Node: return item.IsNode;
                case XmlTypeCode.Item: return true;
                default:
                    if (!item.IsNode)
                        return false;

                    switch (((XPathNavigator)item).NodeType)
                    {
                        case XPathNodeType.Root: return code == XmlTypeCode.Document;
                        case XPathNodeType.Element: return code == XmlTypeCode.Element;
                        case XPathNodeType.Attribute: return code == XmlTypeCode.Attribute;
                        case XPathNodeType.Namespace: return code == XmlTypeCode.Namespace;
                        case XPathNodeType.Text: return code == XmlTypeCode.Text;
                        case XPathNodeType.SignificantWhitespace: return code == XmlTypeCode.Text;
                        case XPathNodeType.Whitespace: return code == XmlTypeCode.Text;
                        case XPathNodeType.ProcessingInstruction: return code == XmlTypeCode.ProcessingInstruction;
                        case XPathNodeType.Comment: return code == XmlTypeCode.Comment;
                    }
                    break;
            }

            Debug.Fail("XmlTypeCode " + code + " was not fully handled.");
            return false;
        }

        /// <summary>
        /// Create an XmlQueryType that represents the type of "item".
        /// </summary>
        private XmlQueryType CreateXmlType(XPathItem item)
        {
            if (item.IsNode)
            {
                // Rtf
                RtfNavigator rtf = item as RtfNavigator;
                if (rtf != null)
                    return XmlQueryTypeFactory.Node;

                // Node
                XPathNavigator nav = (XPathNavigator)item;
                switch (nav.NodeType)
                {
                    case XPathNodeType.Root:
                    case XPathNodeType.Element:
                        if (nav.XmlType == null)
                            return XmlQueryTypeFactory.Type(nav.NodeType, XmlQualifiedNameTest.New(nav.LocalName, nav.NamespaceURI), XmlSchemaComplexType.UntypedAnyType, false);

                        return XmlQueryTypeFactory.Type(nav.NodeType, XmlQualifiedNameTest.New(nav.LocalName, nav.NamespaceURI), nav.XmlType, nav.SchemaInfo.SchemaElement.IsNillable);

                    case XPathNodeType.Attribute:
                        if (nav.XmlType == null)
                            return XmlQueryTypeFactory.Type(nav.NodeType, XmlQualifiedNameTest.New(nav.LocalName, nav.NamespaceURI), DatatypeImplementation.UntypedAtomicType, false);

                        return XmlQueryTypeFactory.Type(nav.NodeType, XmlQualifiedNameTest.New(nav.LocalName, nav.NamespaceURI), nav.XmlType, false);
                }

                return XmlQueryTypeFactory.Type(nav.NodeType, XmlQualifiedNameTest.Wildcard, XmlSchemaComplexType.AnyType, false);
            }

            // Atomic value
            return XmlQueryTypeFactory.Type((XmlSchemaSimpleType)item.XmlType, true);
        }


        //-----------------------------------------------
        // Xml collations
        //-----------------------------------------------

        /// <summary>
        /// Get a collation that was statically created.
        /// </summary>
        public XmlCollation GetCollation(int index)
        {
            Debug.Assert(_collations != null);
            return _collations[index];
        }

        /// <summary>
        /// Create a collation from a string.
        /// </summary>
        public XmlCollation CreateCollation(string collation)
        {
            return XmlCollation.Create(collation);
        }


        //-----------------------------------------------
        // Document Ordering and Identity
        //-----------------------------------------------

        /// <summary>
        /// Compare the relative positions of two navigators.  Return -1 if navThis is before navThat, 1 if after, and
        /// 0 if they are positioned to the same node.
        /// </summary>
        public int ComparePosition(XPathNavigator navigatorThis, XPathNavigator navigatorThat)
        {
            return _docOrderCmp.Compare(navigatorThis, navigatorThat);
        }

        /// <summary>
        /// Get a comparer which guarantees a stable ordering among nodes, even those from different documents.
        /// </summary>
        public IList<XPathNavigator> DocOrderDistinct(IList<XPathNavigator> seq)
        {
            if (seq.Count <= 1)
                return seq;

            XmlQueryNodeSequence nodeSeq = (XmlQueryNodeSequence)seq;
            if (nodeSeq == null)
                nodeSeq = new XmlQueryNodeSequence(seq);

            return nodeSeq.DocOrderDistinct(_docOrderCmp);
        }

        /// <summary>
        /// Generate a unique string identifier for the specified node.  Do this by asking the navigator for an identifier
        /// that is unique within the document, and then prepend a document index.
        /// </summary>
        public string GenerateId(XPathNavigator navigator)
        {
            return string.Concat("ID", _docOrderCmp.GetDocumentIndex(navigator).ToString(CultureInfo.InvariantCulture), navigator.UniqueId);
        }


        //-----------------------------------------------
        // Indexes
        //-----------------------------------------------

        /// <summary>
        /// If an index having the specified Id has already been created over the "context" document, then return it
        /// in "index" and return true.  Otherwise, create a new, empty index and return false.
        /// </summary>
        public bool FindIndex(XPathNavigator context, int indexId, out XmlILIndex index)
        {
            XPathNavigator navRoot;
            ArrayList docIndexes;
            Debug.Assert(context != null);

            // Get root of document
            navRoot = context.Clone();
            navRoot.MoveToRoot();

            // Search pre-existing indexes in order to determine whether the specified index has already been created
            if (_indexes != null && indexId < _indexes.Length)
            {
                docIndexes = (ArrayList)_indexes[indexId];
                if (docIndexes != null)
                {
                    // Search for an index defined over the specified document
                    for (int i = 0; i < docIndexes.Count; i += 2)
                    {
                        // If we find a matching document, then return the index saved in the next slot
                        if (((XPathNavigator)docIndexes[i]).IsSamePosition(navRoot))
                        {
                            index = (XmlILIndex)docIndexes[i + 1];
                            return true;
                        }
                    }
                }
            }

            // Return a new, empty index
            index = new XmlILIndex();
            return false;
        }

        /// <summary>
        /// Add a newly built index over the specified "context" document to the existing collection of indexes.
        /// </summary>
        public void AddNewIndex(XPathNavigator context, int indexId, XmlILIndex index)
        {
            XPathNavigator navRoot;
            ArrayList docIndexes;
            Debug.Assert(context != null);

            // Get root of document
            navRoot = context.Clone();
            navRoot.MoveToRoot();

            // Ensure that a slot exists for the new index
            if (_indexes == null)
            {
                _indexes = new ArrayList[indexId + 4];
            }
            else if (indexId >= _indexes.Length)
            {
                // Resize array
                ArrayList[] indexesNew = new ArrayList[indexId + 4];
                Array.Copy(_indexes, indexesNew, _indexes.Length);
                _indexes = indexesNew;
            }

            docIndexes = (ArrayList)_indexes[indexId];
            if (docIndexes == null)
            {
                docIndexes = new ArrayList();
                _indexes[indexId] = docIndexes;
            }

            docIndexes.Add(navRoot);
            docIndexes.Add(index);
        }


        //-----------------------------------------------
        // Output construction
        //-----------------------------------------------

        /// <summary>
        /// Get output writer object.
        /// </summary>
        public XmlQueryOutput Output
        {
            get { return _output; }
        }

        /// <summary>
        /// Start construction of a nested sequence of items. Return a new XmlQueryOutput that will be
        /// used to construct this new sequence.
        /// </summary>
        public void StartSequenceConstruction(out XmlQueryOutput output)
        {
            // Push current writer
            _stkOutput.Push(_output);

            // Create new writers
            output = _output = new XmlQueryOutput(this, new XmlCachedSequenceWriter());
        }

        /// <summary>
        /// End construction of a nested sequence of items and return the items as an IList{XPathItem}
        /// internal class.  Return previous XmlQueryOutput.
        /// </summary>
        public IList<XPathItem> EndSequenceConstruction(out XmlQueryOutput output)
        {
            IList<XPathItem> seq = ((XmlCachedSequenceWriter)_output.SequenceWriter).ResultSequence;

            // Restore previous XmlQueryOutput
            output = _output = _stkOutput.Pop();

            return seq;
        }

        /// <summary>
        /// Start construction of an Rtf. Return a new XmlQueryOutput object that will be used to construct this Rtf.
        /// </summary>
        public void StartRtfConstruction(string baseUri, out XmlQueryOutput output)
        {
            // Push current writer
            _stkOutput.Push(_output);

            // Create new XmlQueryOutput over an Rtf writer
            output = _output = new XmlQueryOutput(this, new XmlEventCache(baseUri, true));
        }

        /// <summary>
        /// End construction of an Rtf and return it as an RtfNavigator.  Return previous XmlQueryOutput object.
        /// </summary>
        public XPathNavigator EndRtfConstruction(out XmlQueryOutput output)
        {
            XmlEventCache events;

            events = (XmlEventCache)_output.Writer;

            // Restore previous XmlQueryOutput
            output = _output = _stkOutput.Pop();

            // Return Rtf as an RtfNavigator
            events.EndEvents();
            return new RtfTreeNavigator(events, _nameTableQuery);
        }

        /// <summary>
        /// Construct a new RtfTextNavigator from the specified "text".  This is much more efficient than calling
        /// StartNodeConstruction(), StartRtf(), WriteString(), EndRtf(), and EndNodeConstruction().
        /// </summary>
        public XPathNavigator TextRtfConstruction(string text, string baseUri)
        {
            return new RtfTextNavigator(text, baseUri);
        }


        //-----------------------------------------------
        // Miscellaneous
        //-----------------------------------------------

        /// <summary>
        /// Report query execution information to event handler.
        /// </summary>
        public void SendMessage(string message)
        {
            _ctxt.OnXsltMessageEncountered(message);
        }

        /// <summary>
        /// Throw an Xml exception having the specified message text.
        /// </summary>
        public void ThrowException(string text)
        {
            throw new XslTransformException(text);
        }

        /// <summary>
        /// Position navThis to the same location as navThat.
        /// </summary>
        internal static XPathNavigator SyncToNavigator(XPathNavigator navigatorThis, XPathNavigator navigatorThat)
        {
            if (navigatorThis == null || !navigatorThis.MoveTo(navigatorThat))
                return navigatorThat.Clone();

            return navigatorThis;
        }

        /// <summary>
        /// Function is called in Debug mode on each time context node change.
        /// </summary>
        public static int OnCurrentNodeChanged(XPathNavigator currentNode)
        {
            IXmlLineInfo lineInfo = currentNode as IXmlLineInfo;

            // In case of a namespace node, check whether it is inherited or locally defined
            if (lineInfo != null && !(currentNode.NodeType == XPathNodeType.Namespace && IsInheritedNamespace(currentNode)))
            {
                OnCurrentNodeChanged2(currentNode.BaseURI, lineInfo.LineNumber, lineInfo.LinePosition);
            }
            return 0;
        }

        // 'true' if current Namespace "inherited" from it's parent. Not defined locally.
        private static bool IsInheritedNamespace(XPathNavigator node)
        {
            Debug.Assert(node.NodeType == XPathNodeType.Namespace);
            XPathNavigator nav = node.Clone();
            if (nav.MoveToParent())
            {
                if (nav.MoveToFirstNamespace(XPathNamespaceScope.Local))
                {
                    do
                    {
                        if ((object)nav.LocalName == (object)node.LocalName)
                        {
                            return false;
                        }
                    } while (nav.MoveToNextNamespace(XPathNamespaceScope.Local));
                }
            }
            return true;
        }


        private static void OnCurrentNodeChanged2(string baseUri, int lineNumber, int linePosition) { }
    }
}
