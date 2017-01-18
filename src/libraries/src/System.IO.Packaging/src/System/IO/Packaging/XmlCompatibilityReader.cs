// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Threading;

namespace System.IO.Packaging
{
    // <returns>
    // true if xmlNamespace is recognized
    // </returns>
    // <param name=xmlNamespace>
    // the namespace to be checked
    // </param>
    // <param name=newXmlNamespace>
    // if the passed in namespace is subsumed, then newXmlNamespace returns the subsuming namespace.
    // </param>
    internal delegate bool IsXmlNamespaceSupportedCallback(string xmlNamespace, out string newXmlNamespace);
    internal delegate void HandleElementCallback(int elementDepth, ref bool more);
    internal delegate void HandleAttributeCallback(int elementDepth);

    internal sealed class XmlCompatibilityReader : XmlWrappingReader
    {
        #region Construction
        public XmlCompatibilityReader(XmlReader baseReader)
            : base(baseReader)
        {
            _compatibilityScope = new CompatibilityScope(null, -1, this);

            foreach (string xmlNamespace in s_predefinedNamespaces)
            {
                AddKnownNamespace(xmlNamespace);
                _namespaceMap[xmlNamespace] = xmlNamespace;
                Reader.NameTable.Add(xmlNamespace);
            }

            _elementHandler.Add(AlternateContent, new HandleElementCallback(HandleAlternateContent));
            _elementHandler.Add(Choice, new HandleElementCallback(HandleChoice));
            _elementHandler.Add(Fallback, new HandleElementCallback(HandleFallback));

            _attributeHandler.Add(Ignorable, new HandleAttributeCallback(HandleIgnorable));
            _attributeHandler.Add(MustUnderstand, new HandleAttributeCallback(HandleMustUnderstand));
            _attributeHandler.Add(ProcessContent, new HandleAttributeCallback(HandleProcessContent));
            _attributeHandler.Add(PreserveElements, new HandleAttributeCallback(HandlePreserveElements));
            _attributeHandler.Add(PreserveAttributes, new HandleAttributeCallback(HandlePreserveAttributes));
        }

        public XmlCompatibilityReader(XmlReader baseReader,
            IsXmlNamespaceSupportedCallback isXmlNamespaceSupported)
            : this(baseReader)
        {
            _namespaceCallback = isXmlNamespaceSupported;
        }

        public XmlCompatibilityReader(XmlReader baseReader,
            IsXmlNamespaceSupportedCallback isXmlNamespaceSupported,
            IEnumerable<string> supportedNamespaces)
            : this(baseReader, isXmlNamespaceSupported)
        {
            foreach (string xmlNamespace in supportedNamespaces)
            {
                AddKnownNamespace(xmlNamespace);
                _namespaceMap[xmlNamespace] = xmlNamespace;
            }
        }

#if !PBTCOMPILER
        public XmlCompatibilityReader(XmlReader baseReader,
            IEnumerable<string> supportedNamespaces)
            : this(baseReader, null, supportedNamespaces)
        {
        }
#endif
        #endregion Construction

        #region Public Methods
        /// <summary>
        /// replaces all future references of namespace URI 'oldNamespace' with 'newNamespace'
        /// </summary>
        /// <param name="newNamespace">
        /// the namespace to subsume with
        /// </param>
        /// <param name="oldNamespace">
        /// the namespace to be subsumed
        /// </param>
        public void DeclareNamespaceCompatibility(string newNamespace, string oldNamespace)
        {
            if (newNamespace != oldNamespace)
            {
                // indicate that newNamespace subsumes another namespace
                AddSubsumingNamespace(newNamespace);

                // If newNamespace is mapped to a namespace,
                string tempNamespace;
                if (_namespaceMap.TryGetValue(newNamespace, out tempNamespace))
                {
                    // If we have mapped newNamespace already get the newest name.
                    // We don't have to do this recursively because of the code below
                    // ensures the map always refers to the newest namespace.
                    newNamespace = tempNamespace;
                }

                if (IsSubsumingNamespace(oldNamespace))
                {
                    // if we are mapping what was used as a new namespace to a newer name,
                    // scan the _newNamespaces dictionary and update the entries. We collect
                    // a list to avoid updating the dictionary during enumeration.
                    List<string> keysToUpdate = new List<string>();

                    foreach (KeyValuePair<string, string> pair in _namespaceMap)
                    {
                        if (pair.Value == oldNamespace)
                        {
                            keysToUpdate.Add(pair.Key);
                        }
                    }

                    foreach (string key in keysToUpdate)
                    {
                        _namespaceMap[key] = newNamespace;
                    }
                }
            }

            _namespaceMap[oldNamespace] = newNamespace;
        }

        /// <summary>
        /// Reads the next node from the stream.
        /// </summary>
        /// <returns>
        /// true if the next node was read successfully; false if there are no more nodes to read.
        /// </returns>
        public override bool Read()
        {
            // Previous element was an empty element. So if we pushed a scope, then get rid of the scope first.
            if (_isPreviousElementEmpty)
            {
                _isPreviousElementEmpty = false;
                ScanForEndCompatibility(_previousElementDepth);
            }

            bool more = Reader.Read(); //passed as ref arg to ReadStartElement and ReadEndElement
            bool result = false;

            while (more)
            {
                switch (Reader.NodeType)
                {
                    case XmlNodeType.Element:
                        {
                            // if the element read should be ignored, read the next element
                            if (!ReadStartElement(ref more))
                            {
                                continue;
                            }
                            break;
                        }
                    case XmlNodeType.EndElement:
                        {
                            // if the element read should be ignored, read the next element
                            if (!ReadEndElement(ref more))
                            {
                                continue;
                            }
                            break;
                        }
                }

                // if the element was read successfully and was not ignored, break and return true
                result = true;
                break;
            }

            return result;
        }

        /// <summary>
        /// Used to handle 'start element' tags.  These are actually
        /// just called 'element' tags, the 'start' is just for clarity
        /// </summary>
        /// <param name="more">
        /// is set to true if there is the document contains more elements, false if the end of the
        /// document has been reached.
        /// </param>
        /// <returns>
        /// true if an element was read that should not be ignored
        /// false if the element read should be ignored or the end of document has been reached
        /// </returns>
        private bool ReadStartElement(ref bool more)
        {
            // when processing elements, the Reader may advance to another element or attribute,
            // so we save the values of the current element here
            int elementDepth = Reader.Depth;
            int depthOffset = _depthOffset;
            bool isEmpty = Reader.IsEmptyElement;
            string namespaceName = NamespaceURI;
            bool result = false;

            if (object.ReferenceEquals(namespaceName, CompatibilityUri))
            {
                // if the element is a markup-compatibility element, we get the appropriate handler for
                // the element type, and call the appropriate delegate.  If the element is not recognized
                // we throw an exception.
                string elementName = Reader.LocalName;
                HandleElementCallback elementCB;
                if (!_elementHandler.TryGetValue(elementName, out elementCB))
                {
                    Error(SR.XCRUnknownCompatElement, elementName);
                }
                elementCB(elementDepth, ref more);
            }
            // handle non-markup-compatibility elements
            else
            {
                // check for markup-compatibility attributes and namespaces that should be ignored
                ScanForCompatibility(elementDepth);

                if (ShouldIgnoreNamespace(namespaceName))
                {
                    if (Scope.ShouldProcessContent(namespaceName, Reader.LocalName))
                    {
                        // if the current element is unknown and has been marked Ignorable and ProcessContent,
                        // then read the next element, and increase depth offset
                        if (Scope.Depth == elementDepth)
                        {
                            // if the current element pushed a scope, mark the scope as InProcessContent to
                            // note that for certain logic this scope's parent should be checked
                            Scope.InProcessContent = true;
                        }
                        _depthOffset++;
                        more = Reader.Read();
                    }
                    else
                    {
                        // if element should be ignored but not processed, check to see if scope must be popped,
                        // then skip to the next element after the end tag of the current element
                        ScanForEndCompatibility(elementDepth);
                        Reader.Skip();
                    }
                }
                else
                {
                    if (Scope.InAlternateContent)
                    {
                        // if this element is the child of an AlternateContent element, then throw an exception.
                        Error(SR.XCRInvalidACChild, Reader.Name);
                    }

                    result = true;
                }
            }

            // if the element is empty (e.g. "<a ... />" and we pushed a scope then we need to set a flag 
            // to get rid of the scope when we hit the next element.
            // We also need to store the current elementDepth.
            if (isEmpty)
            {
                _isPreviousElementEmpty = true;
                _previousElementDepth = elementDepth;
                _depthOffset = depthOffset;
            }

            return result;
        }

        /// <summary>
        /// Used to handle any end element tag
        /// </summary>
        /// <param name="more">
        /// is set to true if there is the document contains more elements, false if the end of the
        /// document has been reached.
        /// </param>
        /// <returns>
        /// true if an element was read that should not be ignored
        /// false if the element read should be ignored or the end of document has been reached
        /// </returns>
        private bool ReadEndElement(ref bool more)
        {
            // when reading attributes, the reader's depth increases, so for consistency
            // we store the depth before reading any attributes
            int elementDepth = Reader.Depth;
            string namespaceName = NamespaceURI;
            bool result = false;  // return value

            if (object.ReferenceEquals(namespaceName, CompatibilityUri))
            {
                // if the element is a markup-compatibility element, pop a scope, decrement the
                // depth offset and read the next element.
                string elementName = Reader.LocalName;
                if (object.ReferenceEquals(elementName, AlternateContent))
                {
                    if (!Scope.ChoiceSeen)
                    {
                        // if the current element was a </mc:AlternateContent>, without any Choice
                        // element children, throw an exception
                        Error(SR.XCRChoiceNotFound);
                    }
                }
                _depthOffset--;
                PopScope();  //we know we can pop, so no need to scan
                more = Reader.Read();
            }
            else
            {
                if (ShouldIgnoreNamespace(namespaceName))
                {
                    // if current element is Ignorable, then to be on it, it must have been marked
                    // ProcessContent.  Pop a scope if the corresponding start element pushed a scope a
                    // scope, decrement the depth offset and read the next element.
                    Debug.Assert(Scope.ShouldProcessContent(namespaceName, Reader.LocalName));
                    ScanForEndCompatibility(elementDepth);
                    _depthOffset--;
                    more = Reader.Read();
                }
                else
                {
                    ScanForEndCompatibility(elementDepth);
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the value of the attribute with the specified index.
        /// </summary>
        /// <param name="i">
        /// The index of the attribute. The index is zero-based. (The first attribute has index 0.)
        /// </param>
        /// <returns>
        /// The value of the specified attribute. If the attribute is not found, a null reference is returned.
        /// </returns>
        public override string GetAttribute(int i)
        {
            string result = null;

            if (_ignoredAttributeCount == 0)
            {
                // if the current element should not ignored any of its attributes, skip extra logic
                result = Reader.GetAttribute(i);
            }
            else
            {
                SaveReaderPosition();

                // move to 'i'th attribute, get its value
                MoveToAttribute(i);
                result = Reader.Value;

                RestoreReaderPosition();
            }

            return result;
        }

        /// <summary>
        /// Gets the value of the attribute with the specified name.
        /// </summary>
        /// <param name="name">
        /// The qualified name of the attribute.
        /// </param>
        /// <returns>
        /// The value of the specified attribute. If the attribute is not found, a null reference is returned.
        /// </returns>
        public override string GetAttribute(string name)
        {
            string result = null;

            if (_ignoredAttributeCount == 0)
            {
                // if the current element should not ignored any attributes, call Reader method
                result = Reader.GetAttribute(name);
            }
            else
            {
                SaveReaderPosition();

                // move to "name" attribute
                if (MoveToAttribute(name))
                {
                    result = Reader.Value;
                    RestoreReaderPosition();
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the value of the attribute with the specified local name and namespace URI.
        /// </summary>
        /// <param name="localName">
        /// The local name of the attribute.
        /// </param>
        /// <param name="namespaceURI">
        /// The namespace URI of the attribute.
        /// </param>
        /// <returns>
        /// The value of the specified attribute. If the attribute is not found, a null reference is returned.
        /// </returns>
        public override string GetAttribute(string localName, string namespaceURI)
        {
            string result = null;

            if (_ignoredAttributeCount == 0 || !ShouldIgnoreNamespace(namespaceURI))
            {
                // if the current element does not have any attributes that should be ignored or
                // the namespace provided is not ignorable, call Reader method
                result = Reader.GetAttribute(localName, namespaceURI);
            }

            return result;
        }

        /// <summary>
        /// Gets the value of the attribute with the specified index.
        /// </summary>
        /// <param name="i">
        /// The index of the attribute. The index is zero-based. (The first attribute has index 0.)
        /// </param>
        /// <returns>
        /// true if the attribute is found; otherwise, false. If false, the reader's position does not change.
        /// </returns>
        public override void MoveToAttribute(int i)
        {
            if (_ignoredAttributeCount == 0)
            {
                // if the current element should not ignored any attributes, call Reader method
                Reader.MoveToAttribute(i);
            }
            else if (i < 0 || i >= AttributeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }
            else
            {
                // move Reader to first attribute and iterate until 'i'th element found
                Reader.MoveToFirstAttribute();

                while (true)
                {
                    if (!ShouldIgnoreNamespace(NamespaceURI))
                    {
                        // if attribute should not be ignored, decrement 'i', if i == 0 we've found element
                        if (i-- == 0)
                        {
                            break;
                        }
                    }

                    Reader.MoveToNextAttribute();
                }
            }
        }

        /// <summary>
        /// Moves to the attribute with the specified name.
        /// </summary>
        /// <param name="name">
        /// The qualified name of the attribute.
        /// </param>
        /// <returns>
        /// true if the attribute is found; otherwise, false. If false, the reader's position does not change.
        /// </returns>
        public override bool MoveToAttribute(string name)
        {
            bool result;

            if (_ignoredAttributeCount == 0)
            {
                // if the current element should not ignored any attributes, call Reader method
                result = Reader.MoveToAttribute(name);
            }
            else
            {
                SaveReaderPosition();

                result = Reader.MoveToAttribute(name);
                if (result && ShouldIgnoreNamespace(NamespaceURI))
                {
                    // if attribute should be ignored, return false and restore state
                    result = false;
                    RestoreReaderPosition();
                }
            }

            return result;
        }

        /// <summary>
        /// Moves to the attribute with the specified local name and namespace URI.
        /// </summary>
        /// <param name="localName">
        /// The local name of the attribute.
        /// </param>
        /// <param name="namespaceURI">
        /// The namespace URI of the attribute.
        /// </param>
        /// <returns>
        /// true if the attribute is found; otherwise, false. If false, the reader's position does not change.
        /// </returns>
        public override bool MoveToAttribute(string localName, string namespaceURI)
        {
            bool result;

            if (_ignoredAttributeCount == 0)
            {
                // if the current element should not ignored any attributes, call Reader method
                result = Reader.MoveToAttribute(localName, namespaceURI);
            }
            else
            {
                SaveReaderPosition();

                result = Reader.MoveToAttribute(localName, namespaceURI);

                if (result && ShouldIgnoreNamespace(namespaceURI))
                {
                    result = false;
                    RestoreReaderPosition();
                }
            }
            return result;
        }

        /// <summary>
        /// Moves to the first attribute.
        /// </summary>
        /// <returns>
        /// true if an attribute exists (the reader moves to the first attribute);
        /// otherwise, false (the position of the reader does not change).
        /// </returns>
        public override bool MoveToFirstAttribute()
        {
            bool result = HasAttributes;

            if (result)
            {
                MoveToAttribute(0);
            }

            return result;
        }

        /// <summary>
        /// Moves to the next attribute.
        /// </summary>
        /// <returns>
        /// true if there is a next attribute; false if there are no more attributes.
        /// </returns>
        public override bool MoveToNextAttribute()
        {
            bool result;

            if (_ignoredAttributeCount == 0)
            {
                // if the current element should not ignored any attributes, call Reader method
                result = Reader.MoveToNextAttribute();
            }
            else
            {
                SaveReaderPosition();

                result = Reader.MoveToNextAttribute();

                if (result)
                {
                    result = SkipToKnownAttribute();

                    if (!result)
                    {
                        // if no more attributes exist that should not be ignored, return false and restore state
                        RestoreReaderPosition();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves a namespace prefix in the current element's scope.
        /// </summary>
        /// <param name="prefix">
        /// The prefix whose namespace URI you want to resolve. To match the default namespace,
        /// pass an empty string. This string does not have to be atomized.
        /// </param>
        /// <returns>
        /// The namespace URI to which the prefix maps or a null reference if no matching prefix is found.
        /// </returns>
        public override string LookupNamespace(string prefix)
        {
            string namespaceName = Reader.LookupNamespace(prefix);

            if (namespaceName != null)
            {
                namespaceName = GetMappedNamespace(namespaceName);
            }

            return namespaceName;
        }

        #endregion Public Methods

        #region Public Properties

        /// <summary>
        /// This override is to ensure that the value
        /// for the xmlns attribute reflects all the
        /// compatibility (subsuming) rules.
        /// </summary>
        public override string Value
        {
            get
            {
                // Look for xmlns
                if (String.Equals(XmlnsDeclaration, Reader.LocalName, StringComparison.Ordinal))
                {
                    return LookupNamespace(String.Empty);
                }
                // Look for xmlns: ...
                else if (String.Equals(XmlnsDeclaration, Reader.Prefix, StringComparison.Ordinal))
                {
                    return LookupNamespace(Reader.LocalName);
                }

                return Reader.Value;
            }
        }

        /// <summary>
        /// Gets the namespace URI (as defined in the W3C Namespace specification) of the node
        /// on which the reader is positioned.
        /// </summary>
        public override string NamespaceURI
        {
            get
            {
                return GetMappedNamespace(Reader.NamespaceURI);
            }
        }

        /// <summary>
        /// Gets the depth of the current node in the XML document.
        /// </summary>
        public override int Depth
        {
            get
            {
                return Reader.Depth - _depthOffset;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current node has any attributes
        /// </summary>
        public override bool HasAttributes
        {
            get
            {
                return AttributeCount != 0;
            }
        }

        /// <summary>
        /// Gets the number of attributes on the current node.
        /// </summary>
        public override int AttributeCount
        {
            get
            {
                return Reader.AttributeCount - _ignoredAttributeCount;
            }
        }

        #endregion Public Properties

        #region Private Methods

        private void SaveReaderPosition()
        {
            // Save current state so we can go back to the same spot if this fails
            _inAttribute = (Reader.NodeType == XmlNodeType.Attribute);
            _currentName = Reader.Name;
        }

        private void RestoreReaderPosition()
        {
            // Restore reader state from SaveReaderPosition
            if (_inAttribute)
            {
                Reader.MoveToAttribute(_currentName);
            }
            else
            {
                Reader.MoveToElement();
            }
        }

        /// <summary>
        /// Retrieves the correctly mapped namespace from the namespace provided
        /// </summary>
        /// <param name="namespaceName">
        /// The name of the namespace to retrieve the mapping of
        /// </param>
        /// <returns>
        /// The name of the mapped namespace.
        /// </returns>
        private string GetMappedNamespace(string namespaceName)
        {
            string mappedNamespace;

            // if the namespace is not null, get the mapped namespace (which may be itself)
            if (!_namespaceMap.TryGetValue(namespaceName, out mappedNamespace))
            {
                // if the namespace has not yet been mapped, map it
                mappedNamespace = MapNewNamespace(namespaceName);
            }
            else if (mappedNamespace == null)
            {
                // if the mapped namespace is null, then the namespace was not supported, just return
                // the given namespace
                mappedNamespace = namespaceName;
            }

            return mappedNamespace;
        }

        /// <summary>
        /// Adds the namespace to the namespace map.  The default is to map the namespace to itself.
        /// The namespace is mapped to the value returned by the callback, if a callback exists and the
        /// callback returns a subsuming namespace.
        /// </summary>
        /// <param name="namespaceName">
        /// The name of the namespace to be mapped.
        /// </param>
        /// <returns>
        /// The name of the mapped namespace.
        /// </returns>
        private string MapNewNamespace(string namespaceName)
        {
            if (_namespaceCallback != null)
            {
                string mappedNamespace;

                // the callback returns whether the namespace is supported, and mappedNamespace is the
                // namespace subsuming the namespace passed in.
                bool isSupported = _namespaceCallback(namespaceName, out mappedNamespace);

                if (isSupported)
                {
                    AddKnownNamespace(namespaceName);

                    if (String.IsNullOrEmpty(mappedNamespace) || namespaceName == mappedNamespace)
                    {
                        _namespaceMap[namespaceName] = namespaceName;
                    }
                    else
                    {
                        // subsume namespace with mappedNamespace.
                        string tempNamespace;

                        if (!_namespaceMap.TryGetValue(mappedNamespace, out tempNamespace))
                        {
                            // If the namespace is known, but doesn't have a map, that means we're
                            // already in the process of calling MapNewNamespace on it, i.e. we have
                            // a cycle
                            if (IsNamespaceKnown(mappedNamespace))
                            {
                                Error(SR.XCRCompatCycle, mappedNamespace);
                            }

                            // mappedNamespace has not been mapped, so map it
                            tempNamespace = MapNewNamespace(mappedNamespace);
                        }

                        DeclareNamespaceCompatibility(tempNamespace, namespaceName);
                        namespaceName = tempNamespace;
                    }
                }
                else
                {
                    // if the namespace is not supported, we enter null into the namespaceMap as a placeholder
                    // so that we do not call the callback again on this namespace.
                    _namespaceMap[namespaceName] = null;
                }
            }

            return namespaceName;
        }

        /// <summary>
        /// Used to determine whether a given namespace subsumes another namespace
        /// </summary>
        /// <param name="namespaceName">
        /// The name of the namespace to be checked.
        /// </param>
        /// <returns>
        /// true if the namespace subsumes another namespace; false otherwise
        /// </returns>
        private bool IsSubsumingNamespace(string namespaceName)
        {
            return (_subsumingNamespaces == null ? false : _subsumingNamespaces.ContainsKey(namespaceName));
        }

        /// <summary>
        /// Used to specify that a namespace subsumes another namespace
        /// </summary>
        /// <param name="namespaceName">
        /// The name of the namespace to be added.
        /// </param>
        private void AddSubsumingNamespace(string namespaceName)
        {
            if (_subsumingNamespaces == null)
                _subsumingNamespaces = new Dictionary<string, object>();
            _subsumingNamespaces[namespaceName] = null;
        }

        /// <summary>
        /// Used to determine whether a given namespace is known/supported
        /// </summary>
        /// <param name="namespaceName">
        /// The name of the namespace to be checked.
        /// </param>
        /// <returns>
        /// true if the namespace is known/supported; false otherwise
        /// </returns>
        private bool IsNamespaceKnown(string namespaceName)
        {
            return (_knownNamespaces == null ? false : _knownNamespaces.ContainsKey(namespaceName));
        }

        /// <summary>
        /// Used to specify that a namespace is known or supported
        /// </summary>
        /// <param name="namespaceName">
        /// The name of the namespace to be added.
        /// </param>
        private void AddKnownNamespace(string namespaceName)
        {
            if (_knownNamespaces == null)
                _knownNamespaces = new Dictionary<string, object>();
            _knownNamespaces[namespaceName] = null;
        }

        /// <summary>
        /// Used to determine whether a given namespace should be ignored.  A namespace should be ignored if:
        /// EITHER
        /// a) the namespace is not known/supported and has been marked Ignorable
        /// OR
        /// b) the namespace is the markup-compatibility namespace
        /// </summary>
        /// <param name="namespaceName">
        /// The name of the prefix to be checked.
        /// </param>
        /// <returns>
        /// true if the namespace should be ignored; false otherwise
        /// </returns>
        private bool ShouldIgnoreNamespace(string namespaceName)
        {
            bool result;
            if (IsNamespaceKnown(namespaceName))
            {
                result = object.ReferenceEquals(namespaceName, CompatibilityUri);
            }
            else
            {
                result = Scope.CanIgnore(namespaceName);
            }
            return result;
        }

        /// <summary>
        /// breaks up a space-delineated string into namespace/element pairs
        /// </summary>
        /// <param name="content">
        /// the string to be parsed
        /// </param>
        /// <param name="callerContext">
        /// The calling element, used in case of an error
        /// </param>
        /// <returns>
        /// the list of namespace/element pairs
        /// </returns>
        private IEnumerable<NamespaceElementPair> ParseContentToNamespaceElementPair(string content, string callerContext)
        {
            foreach (string pair in content.Trim().Split(' '))
            {
                // check each non-null, non-empty space-delineated namespace/element pair
                if (!String.IsNullOrEmpty(pair))
                {
                    int colonIndex = pair.IndexOf(':');
                    int length = pair.Length;

                    if (colonIndex <= 0 || colonIndex >= length - 1 || colonIndex != pair.LastIndexOf(':'))
                    {
                        // if string does not have a ':', if the last character in the string is a ':'
                        // or if the string contains more than one ':', throw an exception
                        Error(SR.XCRInvalidFormat, callerContext);
                    }

                    string prefix = pair.Substring(0, colonIndex);
                    string elementName = pair.Substring(colonIndex + 1, length - 1 - colonIndex);
                    string namespaceName = LookupNamespace(prefix);

                    if (namespaceName == null)
                    {
                        // if a prefix does not map to a namespace, throw an exception
                        Error(SR.XCRUndefinedPrefix, prefix);
                    }
                    else if (elementName != "*" && !IsName(elementName))
                    {
                        // if the element's name is not valid XML, throw an exception
                        Error(SR.XCRInvalidXMLName, pair);
                    }
                    else
                    {
                        yield return new NamespaceElementPair(namespaceName, elementName);
                    }
                }
            }
        }

        /// <summary>
        /// converts a string of space-delineated prefixes into a list of namespaces
        /// </summary>
        /// <param name="prefixes">
        /// the string to be parsed
        /// </param>
        /// <returns>
        /// the list of namespace/element pairs
        /// </returns>
        private IEnumerable<string> PrefixesToNamespaces(string prefixes)
        {
            foreach (string prefix in prefixes.Trim().Split(' '))
            {
                // check each non-null, non-empty space-delineated prefix
                if (!String.IsNullOrEmpty(prefix))
                {
                    string namespaceUri = LookupNamespace(prefix);

                    if (namespaceUri == null)
                    {
                        // if a prefix does not map to a namespace, throw an exception
                        Error(SR.XCRUndefinedPrefix, prefix);
                    }
                    else
                    {
                        yield return namespaceUri;
                    }
                }
            }
        }

        /// <summary>
        /// advances the reader to the next known namespace/attribute pair
        /// </summary>
        /// <returns>
        /// true if a known namespace/attribute pair was found
        /// </returns>
        private bool SkipToKnownAttribute()
        {
            bool result = true;
            while (result && ShouldIgnoreNamespace(NamespaceURI))
            {
                result = Reader.MoveToNextAttribute();
            }
            return result;
        }

        /// <summary>
        /// Scans the current element for compatibility attributes.  Pushes a new
        /// scope onto the stack under the following conditions:
        /// 1) Ignorable or MustUnderstand attribute read
        /// 2) current element has not previously declared an Ignorable or
        ///    MustUnderstand attribute
        ///
        /// However, if a last condition is not fulfilled, then the scope is popped off
        /// before the function returns
        /// 3) current element is not empty
        ///
        /// stores in _ignoredAttributeCount the number of attributes on the current element
        /// that should be ignored, for the sake of improving perf in attribute-related
        /// methods/properties
        /// </summary>
        /// <param name="elementDepth">
        /// the depth of the Reader at the element currently being processed
        /// </param>
        private void ScanForCompatibility(int elementDepth)
        {
            bool onAttribute = Reader.MoveToFirstAttribute();

            _ignoredAttributeCount = 0;

            if (onAttribute)
            {
                _attributePosition = 0; // we count the attribute index in case we see Ignorable

                do
                {
                    string namespaceName = NamespaceURI;

                    if (ShouldIgnoreNamespace(namespaceName))
                    {
                        // check each attribute's namespace to see if it should be ignored
                        if (object.ReferenceEquals(namespaceName, CompatibilityUri))
                        {
                            // if the attribute is in the markup-compatibility namespace
                            // find and call the appropriate attribute handler callback.
                            string attributeName = Reader.LocalName;
                            HandleAttributeCallback attributeCB;
                            if (!_attributeHandler.TryGetValue(attributeName, out attributeCB))
                            {
                                Error(SR.XCRUnknownCompatAttrib, attributeName);
                            }
                            attributeCB(elementDepth);
                        }

                        _ignoredAttributeCount++;
                    }

                    onAttribute = Reader.MoveToNextAttribute();
                    _attributePosition++; // we count the attribute index in case we see Ignorable
                } while (onAttribute);

                if (Scope.Depth == elementDepth)
                {
                    // if this element pushed a scope, then we need to do a sanity check
                    Scope.Verify();
                }

                // move the reader back to the element for the client
                Reader.MoveToElement();
            }
        }


        /// <summary>
        /// pops a scope if the end of a compatibility region.
        /// </summary>
        /// <param name="elementDepth">
        /// the depth of the Reader at the element currently being processed
        /// </param>
        private void ScanForEndCompatibility(int elementDepth)
        {
            if (elementDepth == Scope.Depth)
            {
                // if the current element's depth equals the depth of the top-level scope, then pop
                PopScope();
            }
        }

        /// <summary>
        /// pushes a new scope onto the stack with a depth passed as an arg.
        /// PushScope does not push a scope if the top scope on the stack is not a lower depth.
        /// </summary>
        /// <param name="elementDepth">
        /// the depth of the Reader at the element currently being processed
        /// </param>
        private void PushScope(int elementDepth)
        {
            if (_compatibilityScope.Depth < elementDepth)
            {
                // if the current element has already pushed a scope, then don't push another one
                _compatibilityScope = new CompatibilityScope(_compatibilityScope, elementDepth, this);
            }
        }

        /// <summary>
        /// pops a scope off the top of the stack.
        /// PopScope *always* pops, it does not check the depth before doing so
        /// </summary>
        private void PopScope()
        {
            _compatibilityScope = _compatibilityScope.Previous;
        }

        /// <summary>
        /// handles mc:AlternateContent element
        ///
        /// a good way to think of AlternateContent blocks is as a switch/case
        /// statement.  The AlternateContent tag is like switch, Choice is like
        /// case, and Fallback is like default.
        /// </summary>
        /// <param name="elementDepth">
        /// the depth of the Reader at the element currently being processed
        /// </param>
        /// <param name="more">
        /// returns whether the Reader has more to be read
        /// </param>
        private void HandleAlternateContent(int elementDepth, ref bool more)
        {
            if (Scope.InAlternateContent)
            {
                // the only valid tags within <AlternateContent> ... </> are
                // Choice and Fallback
                Error(SR.Format(SR.XCRInvalidACChild, Reader.Name));
            }
            if (Reader.IsEmptyElement)
            {
                // AlternateContent blocks must have a Choice, so they can't be empty
                Error(SR.XCRChoiceNotFound);
            }

            // check for markup-compatibility attributes, then push an AlternateContent scope
            ScanForCompatibility(elementDepth);
            PushScope(elementDepth);

            Scope.InAlternateContent = true;
            _depthOffset++;
            more = Reader.Read();
        }

        /// <summary>
        /// handles mc:Choice element
        ///
        /// a good way to think of AlternateContent blocks is as a switch/case
        /// statement.  The AlternateContent tag is like switch, Choice is like
        /// case, and Fallback is like default.
        /// </summary>
        /// <param name="elementDepth">
        /// the depth of the Reader at the element currently being processed
        /// </param>
        /// <param name="more">
        /// returns whether the Reader has more to be read
        /// </param>
        private void HandleChoice(int elementDepth, ref bool more)
        {
            if (!Scope.InAlternateContent)
            {
                // Choice must be the child of AlternateContent
                Error(SR.XCRChoiceOnlyInAC);
            }
            if (Scope.FallbackSeen)
            {
                // Choice cannot occur after Fallback
                Error(SR.XCRChoiceAfterFallback);
            }

            string requiresValue = Reader.GetAttribute(Requires);

            if (requiresValue == null)
            {
                // Choice must have a requires attribute
                Error(SR.XCRRequiresAttribNotFound);
            }
            if (String.IsNullOrEmpty(requiresValue))
            {
                // Requires attribute may not be empty
                Error(SR.XCRInvalidRequiresAttribute);
            }

            CompatibilityScope scope = Scope;

            // check for markup-compatibility attributes
            ScanForCompatibility(elementDepth);

            if (AttributeCount != 1)
            {
                // Choice may not have any attribute that should not be ignored other than Requires
                // get first non-markup-compatibility, non-Requires attribute
                MoveToFirstAttribute();
                if (Reader.LocalName == Requires)
                {
                    MoveToNextAttribute();
                }
                string attributeName = Reader.LocalName;
                MoveToElement();

                Error(SR.XCRInvalidAttribInElement, attributeName, Choice);
            }

            if (scope.ChoiceTaken)
            {
                // a previous choice was valid, so pop any scope pushed and
                // skip to next attribute after </mc:Choice>
                ScanForEndCompatibility(elementDepth);
                Reader.Skip();
            }
            else
            {
                // mark AlternateContent as having seen a choice
                scope.ChoiceSeen = true;

                bool allKnown = true;
                bool somethingSeen = false;

                foreach (string namespaceUri in PrefixesToNamespaces(requiresValue))
                {
                    somethingSeen = true;
                    if (!IsNamespaceKnown(namespaceUri))
                    {
                        // if any attribute in the Requires value is unknown, then do not take this choice
                        allKnown = false;
                        break;
                    }
                }

                if (!somethingSeen)
                {
                    // if the Requires value does not contain a valid prefix/namespace, throw an exception
                    Error(SR.XCRInvalidRequiresAttribute);
                }

                if (allKnown)
                {
                    // if all namespace in the Requires value are known, then this is the Choice taken.
                    // Mark AlternateContent scope as having taken a choice
                    scope.ChoiceTaken = true;

                    // we push a scope here as a place holder, because AlternateContent
                    // scopes do not allow child elements other than Choice and Fallback
                    PushScope(elementDepth);
                    _depthOffset++;
                    more = Reader.Read();
                }
                else
                {
                    // this is not the choice taken, so pop any scope pushed and
                    // skip to next attribute after </mc:Choice>
                    ScanForEndCompatibility(elementDepth);
                    Reader.Skip();
                }
            }
        }

        /// <summary>
        /// handles mc:Fallback element
        ///
        /// a good way to think of AlternateContent blocks is as a switch/case
        /// statement.  The AlternateContent tag is like switch, Choice is like
        /// case, and Fallback is like default.
        /// </summary>
        /// <param name="elementDepth">
        /// the depth of the Reader at the element currently being processed
        /// </param>
        /// <param name="more">
        /// returns whether the Reader has more to be read
        /// </param>
        private void HandleFallback(int elementDepth, ref bool more)
        {
            if (!Scope.InAlternateContent)
            {
                // Fallback must be the child of AlternateContent
                Error(SR.XCRFallbackOnlyInAC);
            }
            if (!Scope.ChoiceSeen)
            {
                // AlternateContent block must contain a Choice element
                Error(SR.XCRChoiceNotFound);
            }
            if (Scope.FallbackSeen)
            {
                // AlternateContent block may only contain one Fallback child
                Error(SR.XCRMultipleFallbackFound);
            }

            // mark scope as having a fallback
            Scope.FallbackSeen = true;
            bool choiceTaken = Scope.ChoiceTaken;

            // check for markup-compatibility attributes
            ScanForCompatibility(elementDepth);

            if (AttributeCount != 0)
            {
                // Fallback may not have any attribute that should not be ignored
                // get first non-markup-compatibility attribute
                MoveToFirstAttribute();
                string attributeName = Reader.LocalName;
                MoveToElement();

                Error(SR.XCRInvalidAttribInElement, attributeName, Fallback);
            }

            if (choiceTaken)
            {
                // a choice was valid, so ignore contents
                ScanForEndCompatibility(elementDepth);
                Reader.Skip();
            }
            else
            {
                // this is the content that will be used, so push a scope
                if (!Reader.IsEmptyElement)
                {
                    // we push a scope here as a place holder, because AlternateContent
                    // scopes do not allow child elements other than Choice and Fallback
                    PushScope(elementDepth);
                    _depthOffset++;
                }
                more = Reader.Read();
            }
        }

        /// <summary>
        /// handles mc:Ignorable="foo" attribute
        ///
        /// Ignorable is used to indicate that the namespace the prefix is mapped to can
        /// be ignored, i.e. when the namespace/element or namespace/attribute occurs it
        /// is not returned by the reader.
        /// </summary>
        private void HandleIgnorable(int elementDepth)
        {
            PushScope(elementDepth);

            foreach (string namespaceUri in PrefixesToNamespaces(Reader.Value))
            {
                Scope.Ignorable(namespaceUri);
            }

            // Just in case one of the namespaces that preceded the Ignorable declaration
            // was an ignorable namespace, we have to recompute _ignoredAttributeCount.
            // No need to check if we haven't yet had any non-ignored attributes.
            if (_ignoredAttributeCount < _attributePosition)
            {
                _ignoredAttributeCount = 0;
                Reader.MoveToFirstAttribute();

                for (int i = 0; i < _attributePosition; i++)
                {
                    if (ShouldIgnoreNamespace(Reader.NamespaceURI))
                    {
                        _ignoredAttributeCount++;
                    }

                    Reader.MoveToNextAttribute();
                }
            }
        }

        /// <summary>
        /// handles mc:MustUnderstand="foo" attribute
        ///
        /// MustUnderstand is used to indicate that the namespace the prefix is mapped to
        /// cannot be handled, and if it is not understood an exception is thrown
        /// </summary>
        private void HandleMustUnderstand(int elementDepth)
        {
            foreach (string namespaceUri in PrefixesToNamespaces(Reader.Value))
            {
                if (!IsNamespaceKnown(namespaceUri))
                {
                    Error(SR.XCRMustUnderstandFailed, namespaceUri);
                }
            }
        }

        /// <summary>
        /// handles mc:ProcessContent="foo:bar" attribute
        ///
        /// ProcessContent is used to indicate that an ignorable namespace has some
        /// elements that should be skipped, but contain child elements that should be processed.
        ///
        /// The wildcard token ("foo:*") indicates that the children of any element in that
        /// namespace should be processed.
        /// </summary>
        private void HandleProcessContent(int elementDepth)
        {
            PushScope(elementDepth);

            foreach (NamespaceElementPair pair in ParseContentToNamespaceElementPair(Reader.Value, _processContent))
            {
                Scope.ProcessContent(pair.namespaceName, pair.itemName);
            }
        }

        /// <summary>
        /// handles mc:PreserveElements="foo:bar" attribute
        ///
        /// functionality is supported, but not implemented
        /// </summary>
        private void HandlePreserveElements(int elementDepth)
        {
            PushScope(elementDepth);

            foreach (NamespaceElementPair pair in ParseContentToNamespaceElementPair(Reader.Value, _preserveElements))
            {
                Scope.PreserveElement(pair.namespaceName, pair.itemName);
            }
        }

        /// <summary>
        /// handles mc:PreserveAttributes="foo:bar" attribute
        ///
        /// functionality is supported, but not implemented
        /// </summary>
        private void HandlePreserveAttributes(int elementDepth)
        {
            PushScope(elementDepth);

            foreach (NamespaceElementPair pair in ParseContentToNamespaceElementPair(Reader.Value, _preserveAttributes))
            {
                Scope.PreserveAttribute(pair.namespaceName, pair.itemName);
            }
        }

        /// <summary>
        /// helper method to generate an exception
        /// </summary>
        private void Error(string message, params object[] args)
        {
            IXmlLineInfo info = Reader as IXmlLineInfo;
            throw new XmlException(string.Format(CultureInfo.InvariantCulture, message, args), null, info == null ? 1 : info.LineNumber,
                info == null ? 1 : info.LinePosition);
        }
        #endregion Private Methods

        #region Private Properties
        private CompatibilityScope Scope
        {
            get
            {
                return _compatibilityScope;
            }
        }

        private string AlternateContent
        {
            get
            {
                if (_alternateContent == null)
                {
                    _alternateContent = Reader.NameTable.Add("AlternateContent");
                }
                return _alternateContent;
            }
        }

        private string Choice
        {
            get
            {
                if (_choice == null)
                {
                    _choice = Reader.NameTable.Add("Choice");
                }
                return _choice;
            }
        }

        private string Fallback
        {
            get
            {
                if (_fallback == null)
                {
                    _fallback = Reader.NameTable.Add("Fallback");
                }
                return _fallback;
            }
        }

        private string Requires
        {
            get
            {
                if (_requires == null)
                {
                    _requires = Reader.NameTable.Add("Requires");
                }
                return _requires;
            }
        }

        private string Ignorable
        {
            get
            {
                if (_ignorable == null)
                {
                    _ignorable = Reader.NameTable.Add("Ignorable");
                }
                return _ignorable;
            }
        }

        private string MustUnderstand
        {
            get
            {
                if (_mustUnderstand == null)
                {
                    _mustUnderstand = Reader.NameTable.Add("MustUnderstand");
                }
                return _mustUnderstand;
            }
        }

        private string ProcessContent
        {
            get
            {
                if (_processContent == null)
                {
                    _processContent = Reader.NameTable.Add("ProcessContent");
                }
                return _processContent;
            }
        }

        private string PreserveElements
        {
            get
            {
                if (_preserveElements == null)
                {
                    _preserveElements = Reader.NameTable.Add("PreserveElements");
                }
                return _preserveElements;
            }
        }

        private string PreserveAttributes
        {
            get
            {
                if (_preserveAttributes == null)
                {
                    _preserveAttributes = Reader.NameTable.Add("PreserveAttributes");
                }
                return _preserveAttributes;
            }
        }

        private string CompatibilityUri
        {
            get
            {
                if (_compatibilityUri == null)
                {
                    _compatibilityUri = Reader.NameTable.Add(MarkupCompatibilityURI);
                }
                return _compatibilityUri;
            }
        }
        #endregion Private Properties
        #region Nested Classes
        private struct NamespaceElementPair
        {
            public string namespaceName;
            public string itemName;

            public NamespaceElementPair(string namespaceName, string itemName)
            {
                this.namespaceName = namespaceName;
                this.itemName = itemName;
            }
        }

        /// <summary>
        /// CompatibilityScopes are used to handle markup-compatibility elements and attributes.
        /// Each scope stores the "previous" or parent scope, its depth, and an associated XmlCompatibilityReader.
        /// At a particular Reader depth, only one scope should be pushed.
        /// </summary>
        private class CompatibilityScope
        {
            private CompatibilityScope _previous;
            private int _depth;
            private bool _fallbackSeen;
            private bool _inAlternateContent;
            private bool _inProcessContent;
            private bool _choiceTaken;
            private bool _choiceSeen;
            private XmlCompatibilityReader _reader;
            private Dictionary<string, object> _ignorables;
            private Dictionary<string, ProcessContentSet> _processContents;
            private Dictionary<string, PreserveItemSet> _preserveElements;
            private Dictionary<string, PreserveItemSet> _preserveAttributes;

            public CompatibilityScope(CompatibilityScope previous, int depth, XmlCompatibilityReader reader)
            {
                _previous = previous;
                _depth = depth;
                _reader = reader;
            }

            public CompatibilityScope Previous
            {
                get
                {
                    return _previous;
                }
            }

            public int Depth
            {
                get
                {
                    return _depth;
                }
            }

            public bool FallbackSeen
            {
                get
                {
                    bool result;
                    if (_inProcessContent && _previous != null)
                    {
                        result = _previous.FallbackSeen;
                    }
                    else
                    {
                        result = _fallbackSeen;
                    }
                    return result;
                }
                set
                {
                    if (_inProcessContent && _previous != null)
                    {
                        _previous.FallbackSeen = value;
                    }
                    else
                    {
                        _fallbackSeen = value;
                    }
                }
            }

            public bool InAlternateContent
            {
                get
                {
                    bool result;
                    if (_inProcessContent && _previous != null)
                    {
                        result = _previous.InAlternateContent;
                    }
                    else
                    {
                        result = _inAlternateContent;
                    }
                    return result;
                }
                set
                {
                    _inAlternateContent = value;
                }
            }

            public bool InProcessContent
            {
                set
                {
                    _inProcessContent = value;
                }
            }

            public bool ChoiceTaken
            {
                get
                {
                    bool result;
                    if (_inProcessContent && _previous != null)
                    {
                        result = _previous.ChoiceTaken;
                    }
                    else
                    {
                        result = _choiceTaken;
                    }
                    return result;
                }
                set
                {
                    if (_inProcessContent && _previous != null)
                    {
                        _previous.ChoiceTaken = value;
                    }
                    else
                    {
                        _choiceTaken = value;
                    }
                }
            }

            public bool ChoiceSeen
            {
                get
                {
                    bool result;
                    if (_inProcessContent && _previous != null)
                    {
                        result = _previous.ChoiceSeen;
                    }
                    else
                    {
                        result = _choiceSeen;
                    }
                    return result;
                }
                set
                {
                    if (_inProcessContent && _previous != null)
                    {
                        _previous.ChoiceSeen = value;
                    }
                    else
                    {
                        _choiceSeen = value;
                    }
                }
            }

            public bool CanIgnore(string namespaceName)
            {
                bool result = IsIgnorableAtCurrentScope(namespaceName);

                if (!result && _previous != null)
                {
                    result = _previous.CanIgnore(namespaceName);
                }

                return result;
            }

            public bool IsIgnorableAtCurrentScope(string namespaceName)
            {
                return _ignorables != null && _ignorables.ContainsKey(namespaceName);
            }

            public bool ShouldProcessContent(string namespaceName, string elementName)
            {
                bool result = false;
                ProcessContentSet set;
                if (_processContents != null && _processContents.TryGetValue(namespaceName, out set))
                {
                    result = set.ShouldProcessContent(elementName);
                }
                else if (_previous != null)
                {
                    result = _previous.ShouldProcessContent(namespaceName, elementName);
                }

                return result;
            }

            public void Ignorable(string namespaceName)
            {
                if (_ignorables == null)
                {
                    _ignorables = new Dictionary<string, object>();
                }
                _ignorables[namespaceName] = null; // we don't care about value, just key
            }

            public void ProcessContent(string namespaceName, string elementName)
            {
                if (_processContents == null)
                {
                    _processContents = new Dictionary<string, ProcessContentSet>();
                }
                ProcessContentSet processContentSet;
                if (!_processContents.TryGetValue(namespaceName, out processContentSet))
                {
                    processContentSet = new ProcessContentSet(namespaceName, _reader);
                    _processContents.Add(namespaceName, processContentSet);
                }
                processContentSet.Add(elementName);
            }

            public void PreserveElement(string namespaceName, string elementName)
            {
                if (_preserveElements == null)
                {
                    _preserveElements = new Dictionary<string, PreserveItemSet>();
                }
                PreserveItemSet preserveElementSet;
                if (!_preserveElements.TryGetValue(namespaceName, out preserveElementSet))
                {
                    preserveElementSet = new PreserveItemSet(namespaceName, _reader);
                    _preserveElements.Add(namespaceName, preserveElementSet);
                }
                preserveElementSet.Add(elementName);
            }

            public void PreserveAttribute(string namespaceName, string attributeName)
            {
                if (_preserveAttributes == null)
                {
                    _preserveAttributes = new Dictionary<string, PreserveItemSet>();
                }
                PreserveItemSet preserveAttributeSet;
                if (!_preserveAttributes.TryGetValue(namespaceName, out preserveAttributeSet))
                {
                    preserveAttributeSet = new PreserveItemSet(namespaceName, _reader);
                    _preserveAttributes.Add(namespaceName, preserveAttributeSet);
                }
                preserveAttributeSet.Add(attributeName);
            }

            public void Verify()
            {
                // Check process content
                if (_processContents != null)
                {
                    foreach (string key in _processContents.Keys)
                    {
                        if (!IsIgnorableAtCurrentScope(key))
                        {
                            _reader.Error(SR.XCRNSProcessContentNotIgnorable, key);
                        }
                    }
                }
                // Check preserve elements
                if (_preserveElements != null)
                {
                    foreach (string key in _preserveElements.Keys)
                    {
                        if (!IsIgnorableAtCurrentScope(key))
                        {
                            _reader.Error(SR.XCRNSPreserveNotIgnorable, key);
                        }
                    }
                }
                // Check preserve attributes
                if (_preserveAttributes != null)
                {
                    foreach (string key in _preserveAttributes.Keys)
                    {
                        if (!IsIgnorableAtCurrentScope(key))
                        {
                            _reader.Error(SR.XCRNSPreserveNotIgnorable, key);
                        }
                    }
                }
            }
        }

        private class ProcessContentSet
        {
            private bool _all;
            private string _namespaceName;
            private XmlCompatibilityReader _reader;
            private Dictionary<string, object> _names;

            public ProcessContentSet(string namespaceName, XmlCompatibilityReader reader)
            {
                _namespaceName = namespaceName;
                _reader = reader;
            }

            public bool ShouldProcessContent(string elementName)
            {
                return _all || (_names != null && _names.ContainsKey(elementName));
            }

            public void Add(string elementName)
            {
                if (ShouldProcessContent(elementName))
                {
                    if (elementName == "*")
                    {
                        _reader.Error(SR.XCRDuplicateWildcardProcessContent, _namespaceName);
                    }
                    else
                    {
                        _reader.Error(SR.XCRDuplicateProcessContent, _namespaceName, elementName);
                    }
                }

                if (elementName == "*")
                {
                    if (_names != null)
                    {
                        _reader.Error(SR.XCRInvalidProcessContent, _namespaceName);
                    }
                    else
                    {
                        _all = true;
                    }
                }
                else
                {
                    if (_names == null)
                    {
                        _names = new Dictionary<string, object>();
                    }

                    _names[elementName] = null; // we don't care about value, just key
                }
            }
        }

        private class PreserveItemSet
        {
            private bool _all;
            private string _namespaceName;
            private XmlCompatibilityReader _reader;
            private Dictionary<string, string> _names;

            public PreserveItemSet(string namespaceName, XmlCompatibilityReader reader)
            {
                _namespaceName = namespaceName;
                _reader = reader;
            }

            public bool ShouldPreserveItem(string itemName)
            {
                return _all || (_names != null && _names.ContainsKey(itemName));
            }

            public void Add(string itemName)
            {
                if (ShouldPreserveItem(itemName))
                {
                    if (itemName == "*")
                    {
                        _reader.Error(SR.XCRDuplicateWildcardPreserve, _namespaceName);
                    }
                    else
                    {
                        _reader.Error(SR.XCRDuplicatePreserve, itemName, _namespaceName);
                    }
                }

                if (itemName == "*")
                {
                    if (_names != null)
                    {
                        _reader.Error(SR.XCRInvalidPreserve, _namespaceName);
                    }
                    else
                    {
                        _all = true;
                    }
                }
                else
                {
                    if (_names == null)
                    {
                        _names = new Dictionary<string, string>();
                    }

                    _names.Add(itemName, itemName);
                }
            }
        }
        #endregion Nested Classes

        #region Private Fields
        private bool _inAttribute; // for Save/Restore ReaderPosition
        private string _currentName; // for Save/Restore ReaderPosition
        private IsXmlNamespaceSupportedCallback _namespaceCallback;
        private Dictionary<string, object> _knownNamespaces;
        private Dictionary<string, string> _namespaceMap = new Dictionary<string, string>();
        private Dictionary<string, object> _subsumingNamespaces;
        private Dictionary<string, HandleElementCallback> _elementHandler = new Dictionary<string, HandleElementCallback>();
        private Dictionary<string, HandleAttributeCallback> _attributeHandler = new Dictionary<string, HandleAttributeCallback>();
        private int _depthOffset; // offset for Depth method, to account for elements that should be ignored by client
        private int _ignoredAttributeCount;
        private int _attributePosition; // used for ScanForCompatibility / HandleIgnorable
        private string _compatibilityUri;
        private string _alternateContent;
        private string _choice;
        private string _fallback;
        private string _requires;
        private string _ignorable;
        private string _mustUnderstand;
        private string _processContent;
        private string _preserveElements;
        private string _preserveAttributes;
        private CompatibilityScope _compatibilityScope;

        private bool _isPreviousElementEmpty;
        private int _previousElementDepth;

        private const string XmlnsDeclaration = "xmlns";
        private const string MarkupCompatibilityURI = "http://schemas.openxmlformats.org/markup-compatibility/2006";

        private static string[] s_predefinedNamespaces = new string[4] {
            "http://www.w3.org/2000/xmlns/",
            "http://www.w3.org/XML/1998/namespace",
            "http://www.w3.org/2001/XMLSchema-instance",
            MarkupCompatibilityURI
        };
        #endregion Private Fields
    }
}
