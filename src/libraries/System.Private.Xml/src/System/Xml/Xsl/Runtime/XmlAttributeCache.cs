// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.Runtime
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
    using System.Xml.Schema;


    /// <summary>
    /// This writer supports only writer methods which write attributes.  Attributes are stored in a
    /// data structure until StartElementContent() is called, at which time the attributes are flushed
    /// to the wrapped writer.  In the case of duplicate attributes, the last attribute's value is used.
    /// </summary>
    internal sealed class XmlAttributeCache : XmlRawWriter, IRemovableWriter
    {
        private XmlRawWriter _wrapped;
        private OnRemoveWriter _onRemove;        // Event handler that is called when cached attributes are flushed to wrapped writer
        private AttrNameVal[] _arrAttrs;         // List of cached attribute names and value parts
        private int _numEntries;                 // Number of attributes in the cache
        private int _idxLastName;                // The entry containing the name of the last attribute to be cached
        private int _hashCodeUnion;              // Set of hash bits that can quickly guarantee a name is not a duplicate

        /// <summary>
        /// Initialize the cache.  Use this method instead of a constructor in order to reuse the cache.
        /// </summary>
        public void Init(XmlRawWriter wrapped)
        {
            SetWrappedWriter(wrapped);

            // Clear attribute list
            _numEntries = 0;
            _idxLastName = 0;
            _hashCodeUnion = 0;
        }

        /// <summary>
        /// Return the number of cached attributes.
        /// </summary>
        public int Count
        {
            get { return _numEntries; }
        }


        //-----------------------------------------------
        // IRemovableWriter interface
        //-----------------------------------------------

        /// <summary>
        /// This writer will raise this event once cached attributes have been flushed in order to signal that the cache
        /// no longer needs to be part of the pipeline.
        /// </summary>
        public OnRemoveWriter OnRemoveWriterEvent
        {
            get { return _onRemove; }
            set { _onRemove = value; }
        }

        /// <summary>
        /// The wrapped writer will callback on this method if it wishes to remove itself from the pipeline.
        /// </summary>
        private void SetWrappedWriter(XmlRawWriter writer)
        {
            // If new writer might remove itself from pipeline, have it callback on this method when its ready to go
            IRemovableWriter removable = writer as IRemovableWriter;
            if (removable != null)
                removable.OnRemoveWriterEvent = SetWrappedWriter;

            _wrapped = writer;
        }


        //-----------------------------------------------
        // XmlWriter interface
        //-----------------------------------------------

        /// <summary>
        /// Add an attribute to the cache.  If an attribute if the same name already exists, replace it.
        /// </summary>
        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            int hashCode;
            int idx = 0;
            Debug.Assert(localName != null && localName.Length != 0 && prefix != null && ns != null);

            // Compute hashcode based on first letter of the localName
            hashCode = (1 << ((int)localName[0] & 31));

            // If the hashcode is not in the union, then name will not be found by a scan
            if ((_hashCodeUnion & hashCode) != 0)
            {
                // The name may or may not be present, so scan for it
                Debug.Assert(_numEntries != 0);

                do
                {
                    if (_arrAttrs[idx].IsDuplicate(localName, ns, hashCode))
                        break;

                    // Next attribute name
                    idx = _arrAttrs[idx].NextNameIndex;
                }
                while (idx != 0);
            }
            else
            {
                // Insert hashcode into union
                _hashCodeUnion |= hashCode;
            }

            // Insert new attribute; link attribute names together in a list
            EnsureAttributeCache();
            if (_numEntries != 0)
                _arrAttrs[_idxLastName].NextNameIndex = _numEntries;
            _idxLastName = _numEntries++;
            _arrAttrs[_idxLastName].Init(prefix, localName, ns, hashCode);
        }

        /// <summary>
        /// No-op.
        /// </summary>
        public override void WriteEndAttribute()
        {
        }

        /// <summary>
        /// Pass through namespaces to underlying writer.  If any attributes have been cached, flush them.
        /// </summary>
        internal override void WriteNamespaceDeclaration(string prefix, string ns)
        {
            FlushAttributes();
            _wrapped.WriteNamespaceDeclaration(prefix, ns);
        }

        /// <summary>
        /// Add a block of text to the cache.  This text block makes up some or all of the untyped string
        /// value of the current attribute.
        /// </summary>
        public override void WriteString(string text)
        {
            Debug.Assert(text != null);
            Debug.Assert(_arrAttrs != null && _numEntries != 0);
            EnsureAttributeCache();
            _arrAttrs[_numEntries++].Init(text);
        }

        /// <summary>
        /// All other WriteValue methods are implemented by XmlWriter to delegate to WriteValue(object) or WriteValue(string), so
        /// only these two methods need to be implemented.
        /// </summary>
        public override void WriteValue(object value)
        {
            Debug.Assert(value is XmlAtomicValue, "value should always be an XmlAtomicValue, as XmlAttributeCache is only used by XmlQueryOutput");
            Debug.Assert(_arrAttrs != null && _numEntries != 0);
            EnsureAttributeCache();
            _arrAttrs[_numEntries++].Init((XmlAtomicValue)value);
        }

        public override void WriteValue(string value)
        {
            WriteValue(value);
        }

        /// <summary>
        /// Send cached, non-overridden attributes to the specified writer.  Calling this method has
        /// the side effect of clearing the attribute cache.
        /// </summary>
        internal override void StartElementContent()
        {
            FlushAttributes();

            // Call StartElementContent on wrapped writer
            _wrapped.StartElementContent();
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            Debug.Fail("Should never be called on XmlAttributeCache.");
        }
        internal override void WriteEndElement(string prefix, string localName, string ns)
        {
            Debug.Fail("Should never be called on XmlAttributeCache.");
        }
        public override void WriteComment(string text)
        {
            Debug.Fail("Should never be called on XmlAttributeCache.");
        }
        public override void WriteProcessingInstruction(string name, string text)
        {
            Debug.Fail("Should never be called on XmlAttributeCache.");
        }
        public override void WriteEntityRef(string name)
        {
            Debug.Fail("Should never be called on XmlAttributeCache.");
        }

        /// <summary>
        /// Forward call to wrapped writer.
        /// </summary>
        public override void Close()
        {
            _wrapped.Close();
        }

        /// <summary>
        /// Forward call to wrapped writer.
        /// </summary>
        public override void Flush()
        {
            _wrapped.Flush();
        }


        //-----------------------------------------------
        // Helper methods
        //-----------------------------------------------

        private void FlushAttributes()
        {
            int idx = 0, idxNext;
            string localName;

            while (idx != _numEntries)
            {
                // Get index of next attribute's name (0 if this is the last attribute)
                idxNext = _arrAttrs[idx].NextNameIndex;
                if (idxNext == 0)
                    idxNext = _numEntries;

                // If localName is null, then this is a duplicate attribute that has been marked as "deleted"
                localName = _arrAttrs[idx].LocalName;
                if (localName != null)
                {
                    string prefix = _arrAttrs[idx].Prefix;
                    string ns = _arrAttrs[idx].Namespace;

                    _wrapped.WriteStartAttribute(prefix, localName, ns);

                    // Output all of this attribute's text or typed values
                    while (++idx != idxNext)
                    {
                        string text = _arrAttrs[idx].Text;

                        if (text != null)
                            _wrapped.WriteString(text);
                        else
                            _wrapped.WriteValue(_arrAttrs[idx].Value);
                    }

                    _wrapped.WriteEndAttribute();
                }
                else
                {
                    // Skip over duplicate attributes
                    idx = idxNext;
                }
            }

            // Notify event listener that attributes have been flushed
            if (_onRemove != null)
                _onRemove(_wrapped);
        }

        private struct AttrNameVal
        {
            private string _localName;
            private string _prefix;
            private string _namespaceName;
            private string _text;
            private XmlAtomicValue _value;
            private int _hashCode;
            private int _nextNameIndex;

            public string LocalName { get { return _localName; } }
            public string Prefix { get { return _prefix; } }
            public string Namespace { get { return _namespaceName; } }
            public string Text { get { return _text; } }
            public XmlAtomicValue Value { get { return _value; } }
            public int NextNameIndex { get { return _nextNameIndex; } set { _nextNameIndex = value; } }

            /// <summary>
            /// Cache an attribute's name and type.
            /// </summary>
            public void Init(string prefix, string localName, string ns, int hashCode)
            {
                _localName = localName;
                _prefix = prefix;
                _namespaceName = ns;
                _hashCode = hashCode;
                _nextNameIndex = 0;
            }

            /// <summary>
            /// Cache all or part of the attribute's string value.
            /// </summary>
            public void Init(string text)
            {
                _text = text;
                _value = null;
            }

            /// <summary>
            /// Cache all or part of the attribute's typed value.
            /// </summary>
            public void Init(XmlAtomicValue value)
            {
                _text = null;
                _value = value;
            }

            /// <summary>
            /// Returns true if this attribute has the specified name (and thus is a duplicate).
            /// </summary>
            public bool IsDuplicate(string localName, string ns, int hashCode)
            {
                // If attribute is not marked as deleted
                if (_localName != null)
                {
                    // And if hash codes match,
                    if (_hashCode == hashCode)
                    {
                        // And if local names match,
                        if (_localName.Equals(localName))
                        {
                            // And if namespaces match,
                            if (_namespaceName.Equals(ns))
                            {
                                // Then found duplicate attribute, so mark the attribute as deleted
                                _localName = null;
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
        }

#if DEBUG
        private const int DefaultCacheSize = 2;
#else
        private const int DefaultCacheSize = 32;
#endif

        /// <summary>
        /// Ensure that attribute array has been created and is large enough for at least one
        /// additional entry.
        /// </summary>
        private void EnsureAttributeCache()
        {
            if (_arrAttrs == null)
            {
                // Create caching array
                _arrAttrs = new AttrNameVal[DefaultCacheSize];
            }
            else if (_numEntries >= _arrAttrs.Length)
            {
                // Resize caching array
                Debug.Assert(_numEntries == _arrAttrs.Length);
                AttrNameVal[] arrNew = new AttrNameVal[_numEntries * 2];
                Array.Copy(_arrAttrs, arrNew, _numEntries);
                _arrAttrs = arrNew;
            }
        }
    }
}
