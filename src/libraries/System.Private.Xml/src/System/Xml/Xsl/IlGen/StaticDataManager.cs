// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.IlGen
{
    /// <summary>
    /// This internal class maintains a list of unique values.  Each unique value is assigned a unique ID, which can
    /// be used to quickly access the value, since it corresponds to the value's position in the list.
    /// </summary>
    internal sealed class UniqueList<T> where T : notnull
    {
        private readonly Dictionary<T, int> _lookup = new Dictionary<T, int>();
        private readonly List<T> _list = new List<T>();

        /// <summary>
        /// If "value" is already in the list, do not add it.  Return the unique ID of the value in the list.
        /// </summary>
        public int Add(T value)
        {
            if (!_lookup.TryGetValue(value, out int id))
            {
                // The value does not yet exist, so add it to the list
                id = _list.Count;
                _lookup.Add(value, id);
                _list.Add(value);
            }

            return id;
        }

        /// <summary>
        /// Return an array of the unique values.
        /// </summary>
        public T[] ToArray()
        {
            return _list.ToArray();
        }
    }


    /// <summary>
    /// Manages all static data that is used by the runtime.  This includes:
    ///   1. All NCName and QName atoms that will be used at run-time
    ///   2. All QName filters that will be used at run-time
    ///   3. All Xml types that will be used at run-time
    ///   4. All global variables and parameters
    /// </summary>
    internal sealed class StaticDataManager
    {
        private UniqueList<string>? _uniqueNames;
        private UniqueList<Int32Pair>? _uniqueFilters;
        private List<StringPair[]>? _prefixMappingsList;
        private List<string>? _globalNames;
        private UniqueList<EarlyBoundInfo>? _earlyInfo;
        private UniqueList<XmlQueryType>? _uniqueXmlTypes;
        private UniqueList<XmlCollation>? _uniqueCollations;

        /// <summary>
        /// Add "name" to the list of unique names that are used by this query.  Return the index of
        /// the unique name in the list.
        /// </summary>
        public int DeclareName(string name)
        {
            _uniqueNames ??= new UniqueList<string>();

            return _uniqueNames.Add(name);
        }

        /// <summary>
        /// Return an array of all names that are used by the query (null if no names).
        /// </summary>
        public string[]? Names
        {
            get { return _uniqueNames?.ToArray(); }
        }

        /// <summary>
        /// Add a name filter to the list of unique filters that are used by this query.  Return the index of
        /// the unique filter in the list.
        /// </summary>
        public int DeclareNameFilter(string locName, string nsUri)
        {
            _uniqueFilters ??= new UniqueList<Int32Pair>();

            return _uniqueFilters.Add(new Int32Pair(DeclareName(locName), DeclareName(nsUri)));
        }

        /// <summary>
        /// Return an array of all name filters, where each name filter is represented as a pair of integer offsets (localName, namespaceUri)
        /// into the Names array (null if no name filters).
        /// </summary>
        public Int32Pair[]? NameFilters
        {
            get { return _uniqueFilters?.ToArray(); }
        }

        /// <summary>
        /// Add a list of QilExpression NamespaceDeclarations to an array of strings (prefix followed by namespace URI).
        /// Return index of the prefix mappings within this array.
        /// </summary>
        public int DeclarePrefixMappings(IList<QilNode> list)
        {
            StringPair[] prefixMappings;

            // Fill mappings array
            prefixMappings = new StringPair[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                // Each entry in mappings array must be a constant NamespaceDeclaration
                QilBinary ndNmspDecl = (QilBinary)list[i];
                Debug.Assert(ndNmspDecl != null);
                Debug.Assert(ndNmspDecl.Left is QilLiteral && ndNmspDecl.Right is QilLiteral);

                prefixMappings[i] = new StringPair((string)(QilLiteral)ndNmspDecl.Left, (string)(QilLiteral)ndNmspDecl.Right);
            }

            // Add mappings to list and return index
            _prefixMappingsList ??= new List<StringPair[]>();

            _prefixMappingsList.Add(prefixMappings);
            return _prefixMappingsList.Count - 1;
        }

        /// <summary>
        /// Return an array of all prefix mappings that are used by the query to compute names (null if no mappings).
        /// </summary>
        public StringPair[][]? PrefixMappingsList
        {
            get { return _prefixMappingsList?.ToArray(); }
        }

        /// <summary>
        /// Declare a new global variable or parameter.
        /// </summary>
        public int DeclareGlobalValue(string name)
        {
            int idx;

            _globalNames ??= new List<string>();

            idx = _globalNames.Count;
            _globalNames.Add(name);
            return idx;
        }

        /// <summary>
        /// Return an array containing the names of all global variables and parameters.
        /// </summary>
        public string[]? GlobalNames
        {
            get { return _globalNames?.ToArray(); }
        }

        /// <summary>
        /// Add early bound information to a list that is used by this query.  Return the index of
        /// the early bound information in the list.
        /// </summary>
        public int DeclareEarlyBound(string namespaceUri, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type ebType)
        {
            _earlyInfo ??= new UniqueList<EarlyBoundInfo>();

            return _earlyInfo.Add(new EarlyBoundInfo(namespaceUri, ebType));
        }

        /// <summary>
        /// Return an array of all early bound information that is used by the query (null if none is used).
        /// </summary>
        public EarlyBoundInfo[]? EarlyBound => _earlyInfo?.ToArray();

        /// <summary>
        /// Add "type" to the list of unique types that are used by this query.  Return the index of
        /// the unique type in the list.
        /// </summary>
        public int DeclareXmlType(XmlQueryType type)
        {
            _uniqueXmlTypes ??= new UniqueList<XmlQueryType>();

            XmlQueryTypeFactory.CheckSerializability(type);
            return _uniqueXmlTypes.Add(type);
        }

        /// <summary>
        /// Return an array of all types that are used by the query (null if no names).
        /// </summary>
        public XmlQueryType[]? XmlTypes
        {
            get { return _uniqueXmlTypes?.ToArray(); }
        }

        /// <summary>
        /// Add "collation" to the list of unique collations that are used by this query.  Return the index of
        /// the unique collation in the list.
        /// </summary>
        public int DeclareCollation(string collation)
        {
            _uniqueCollations ??= new UniqueList<XmlCollation>();

            return _uniqueCollations.Add(XmlCollation.Create(collation));
        }

        /// <summary>
        /// Return an array of all collations that are used by the query (null if no names).
        /// </summary>
        public XmlCollation[]? Collations
        {
            get { return _uniqueCollations?.ToArray(); }
        }
    }
}
