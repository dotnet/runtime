// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Xsl.Xslt;

namespace System.Xml.Xsl.Runtime
{
    using Reflection;

    /// <summary>
    /// The context of a query consists of all user-provided information which influences the operation of the
    /// query. The context manages the following information:
    ///
    ///   1. Input data sources, including the default data source if one exists
    ///   2. Extension objects
    ///   3. External parameters
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class XmlQueryContext
    {
        private readonly XmlQueryRuntime _runtime;
        private readonly XPathNavigator _defaultDataSource;
        private readonly XmlResolver _dataSources;
        private readonly Hashtable _dataSourceCache;
        private readonly XsltArgumentList _argList;
        private XmlExtensionFunctionTable _extFuncsLate;
        private readonly WhitespaceRuleLookup _wsRules;
        private readonly QueryReaderSettings _readerSettings; // If we create reader out of stream we will use these settings

        /// <summary>
        /// This constructor is internal so that external users cannot construct it (and therefore we do not have to test it separately).
        /// </summary>
        internal XmlQueryContext(XmlQueryRuntime runtime, object defaultDataSource, XmlResolver dataSources, XsltArgumentList argList, WhitespaceRuleLookup wsRules)
        {
            _runtime = runtime;
            _dataSources = dataSources;
            _dataSourceCache = new Hashtable();
            _argList = argList;
            _wsRules = wsRules;

            if (defaultDataSource is XmlReader)
            {
                _readerSettings = new QueryReaderSettings((XmlReader)defaultDataSource);
            }
            else
            {
                // Consider allowing users to set DefaultReaderSettings in XsltArgumentList
                // readerSettings = argList.DefaultReaderSettings;
                _readerSettings = new QueryReaderSettings(new NameTable());
            }

            if (defaultDataSource is string)
            {
                // Load the default document from a Uri
                _defaultDataSource = GetDataSource(defaultDataSource as string, null);

                if (_defaultDataSource == null)
                    throw new XslTransformException(SR.XmlIl_UnknownDocument, defaultDataSource as string);
            }
            else if (defaultDataSource != null)
            {
                _defaultDataSource = ConstructDocument(defaultDataSource, null, null);
            }
        }


        //-----------------------------------------------
        // Input data sources
        //-----------------------------------------------

        /// <summary>
        /// Returns the name table that should be used in the query to atomize search names and to load
        /// new documents.
        /// </summary>
        public XmlNameTable QueryNameTable
        {
            get { return _readerSettings.NameTable; }
        }

        /// <summary>
        /// Returns the name table used by the default data source, or null if there is no default data source.
        /// </summary>
        public XmlNameTable DefaultNameTable
        {
            get { return _defaultDataSource != null ? _defaultDataSource.NameTable : null; }
        }

        /// <summary>
        /// Return the document which is queried by default--i.e. no data source is explicitly selected in the query.
        /// </summary>
        public XPathNavigator DefaultDataSource
        {
            get
            {
                // Throw exception if there is no default data source to return
                if (_defaultDataSource == null)
                    throw new XslTransformException(SR.XmlIl_NoDefaultDocument, string.Empty);

                return _defaultDataSource;
            }
        }

        /// <summary>
        /// Fetch the data source specified by "uriRelative" and "uriBase" from the XmlResolver that the user provided.
        /// If the resolver returns a stream or reader, create an instance of XPathDocument.  If the resolver returns an
        /// XPathNavigator, return the navigator.  Throw an exception if no data source was found.
        /// </summary>
        public XPathNavigator GetDataSource(string uriRelative, string uriBase)
        {
            object input;
            Uri uriResolvedBase, uriResolved;
            XPathNavigator nav = null;

            try
            {
                // If the data source has already been retrieved, then return the data source from the cache.
                uriResolvedBase = (uriBase != null) ? _dataSources.ResolveUri(null, uriBase) : null;
                uriResolved = _dataSources.ResolveUri(uriResolvedBase, uriRelative);
                if (uriResolved != null)
                    nav = _dataSourceCache[uriResolved] as XPathNavigator;

                if (nav == null)
                {
                    // Get the entity from the resolver and ensure it is cached as a document
                    input = _dataSources.GetEntity(uriResolved, null, null);

                    if (input != null)
                    {
                        // Construct a document from the entity and add the document to the cache
                        nav = ConstructDocument(input, uriRelative, uriResolved);
                        _dataSourceCache.Add(uriResolved, nav);
                    }
                }
            }
            catch (XslTransformException)
            {
                // Don't need to wrap XslTransformException
                throw;
            }
            catch (Exception e)
            {
                if (!XmlException.IsCatchableException(e))
                {
                    throw;
                }
                throw new XslTransformException(e, SR.XmlIl_DocumentLoadError, uriRelative);
            }

            return nav;
        }

        /// <summary>
        /// Ensure that "dataSource" is cached as an XPathDocument and return a navigator over the document.
        /// </summary>
        private XPathNavigator ConstructDocument(object dataSource, string uriRelative, Uri uriResolved)
        {
            Debug.Assert(dataSource != null, "GetType() below assumes dataSource is not null");
            Stream stream = dataSource as Stream;
            if (stream != null)
            {
                // Create document from stream
                XmlReader reader = _readerSettings.CreateReader(stream, uriResolved != null ? uriResolved.ToString() : null);

                try
                {
                    // Create WhitespaceRuleReader if whitespace should be stripped
                    return new XPathDocument(WhitespaceRuleReader.CreateReader(reader, _wsRules), XmlSpace.Preserve).CreateNavigator();
                }
                finally
                {
                    // Always close reader that was opened here
                    reader.Close();
                }
            }
            else if (dataSource is XmlReader)
            {
                // Create document from reader
                // Create WhitespaceRuleReader if whitespace should be stripped
                return new XPathDocument(WhitespaceRuleReader.CreateReader(dataSource as XmlReader, _wsRules), XmlSpace.Preserve).CreateNavigator();
            }
            else if (dataSource is IXPathNavigable)
            {
                if (_wsRules != null)
                    throw new XslTransformException(SR.XmlIl_CantStripNav, string.Empty);

                return (dataSource as IXPathNavigable).CreateNavigator();
            }

            Debug.Assert(uriRelative != null, "Relative URI should not be null");
            throw new XslTransformException(SR.XmlIl_CantResolveEntity, uriRelative, dataSource.GetType().ToString());
        }


        //-----------------------------------------------
        // External parameters
        //-----------------------------------------------

        /// <summary>
        /// Get a named parameter from the external argument list.  Return null if no argument list was provided, or if
        /// there is no parameter by that name.
        /// </summary>
        public object GetParameter(string localName, string namespaceUri)
        {
            return (_argList != null) ? _argList.GetParam(localName, namespaceUri) : null;
        }


        //-----------------------------------------------
        // Extension objects
        //-----------------------------------------------

        /// <summary>
        /// Return the extension object that is mapped to the specified namespace, or null if no object is mapped.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = XsltArgumentList.ExtensionObjectSuppresion)]
        public object GetLateBoundObject(string namespaceUri)
        {
            return (_argList != null) ? _argList.GetExtensionObject(namespaceUri) : null;
        }

        /// <summary>
        /// Return true if the late bound object identified by "namespaceUri" contains a method that matches "name".
        /// </summary>
        [RequiresUnreferencedCode(Scripts.ExtensionFunctionCannotBeStaticallyAnalyzed)]
        public bool LateBoundFunctionExists(string name, string namespaceUri)
        {
            object instance;

            if (_argList == null)
                return false;

            instance = _argList.GetExtensionObject(namespaceUri);
            if (instance == null)
                return false;

            return new XmlExtensionFunction(name, namespaceUri, -1, instance.GetType(), XmlQueryRuntime.LateBoundFlags).CanBind();
        }

        /// <summary>
        /// Get a late-bound extension object from the external argument list.  Bind to a method on the object and invoke it,
        /// passing "args" as arguments.
        /// </summary>
        [RequiresUnreferencedCode(Scripts.ExtensionFunctionCannotBeStaticallyAnalyzed)]
        public IList<XPathItem> InvokeXsltLateBoundFunction(string name, string namespaceUri, IList<XPathItem>[] args)
        {
            object instance;
            object[] objActualArgs;
            XmlQueryType xmlTypeFormalArg;
            Type clrTypeFormalArg;
            object objRet;

            // Get external object instance from argument list (throw if either the list or the instance doesn't exist)
            instance = (_argList != null) ? _argList.GetExtensionObject(namespaceUri) : null;
            if (instance == null)
                throw new XslTransformException(SR.XmlIl_UnknownExtObj, namespaceUri);

            // Bind to a method on the instance object
            if (_extFuncsLate == null)
                _extFuncsLate = new XmlExtensionFunctionTable();

            // Bind to the instance, looking for a matching method (throws if no matching method)
            XmlExtensionFunction extFunc = _extFuncsLate.Bind(name, namespaceUri, args.Length, instance.GetType(), XmlQueryRuntime.LateBoundFlags);

            // Create array which will contain the actual arguments
            objActualArgs = new object[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                // 1. Assume that the input value can only have one of the following 5 Xslt types:
                //      xs:double, xs:string, xs:boolean, node* (can be rtf)
                // 2. Convert each Rtf value to a NodeSet containing one node.  Now the value may only have one of the 4 Xslt types.
                // 3. Convert from one of the 4 Xslt internal types to the Xslt internal type which is closest to the formal
                //    argument's Xml type (inferred from the Clr type of the formal argument).

                xmlTypeFormalArg = extFunc.GetXmlArgumentType(i);
                switch (xmlTypeFormalArg.TypeCode)
                {
                    case XmlTypeCode.Boolean: objActualArgs[i] = XsltConvert.ToBoolean(args[i]); break;
                    case XmlTypeCode.Double: objActualArgs[i] = XsltConvert.ToDouble(args[i]); break;
                    case XmlTypeCode.String: objActualArgs[i] = XsltConvert.ToString(args[i]); break;
                    case XmlTypeCode.Node:
                        if (xmlTypeFormalArg.IsSingleton)
                            objActualArgs[i] = XsltConvert.ToNode(args[i]);
                        else
                            objActualArgs[i] = XsltConvert.ToNodeSet(args[i]);
                        break;
                    case XmlTypeCode.Item:
                        objActualArgs[i] = args[i];
                        break;
                    default:
                        Debug.Fail("This XmlTypeCode should never be inferred from a Clr type: " + xmlTypeFormalArg.TypeCode);
                        break;
                }

                // 4. Change the Clr representation to the Clr type of the formal argument
                clrTypeFormalArg = extFunc.GetClrArgumentType(i);
                if (xmlTypeFormalArg.TypeCode == XmlTypeCode.Item || !clrTypeFormalArg.IsAssignableFrom(objActualArgs[i].GetType()))
                    objActualArgs[i] = _runtime.ChangeTypeXsltArgument(xmlTypeFormalArg, objActualArgs[i], clrTypeFormalArg);
            }

            // 1. Invoke the late bound method
            objRet = extFunc.Invoke(instance, objActualArgs);

            // 2. Convert to IList<XPathItem>
            if (objRet == null && extFunc.ClrReturnType == XsltConvert.VoidType)
                return XmlQueryNodeSequence.Empty;

            return (IList<XPathItem>)_runtime.ChangeTypeXsltResult(XmlQueryTypeFactory.ItemS, objRet);
        }


        //-----------------------------------------------
        // Event
        //-----------------------------------------------

        /// <summary>
        /// Fire the XsltMessageEncounteredEvent, passing the specified text as the message.
        /// </summary>
        public void OnXsltMessageEncountered(string message)
        {
            XsltMessageEncounteredEventHandler onMessage = (_argList != null) ? _argList.xsltMessageEncountered : null;

            if (onMessage != null)
                onMessage(this, new XmlILQueryEventArgs(message));
        }
    }

    /// <summary>
    /// Simple implementation of XsltMessageEncounteredEventArgs.
    /// </summary>
    internal sealed class XmlILQueryEventArgs : XsltMessageEncounteredEventArgs
    {
        private readonly string _message;

        public XmlILQueryEventArgs(string message)
        {
            _message = message;
        }

        public override string Message
        {
            get { return _message; }
        }
    }
}
