// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Globalization;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;
    using System.Collections;

    internal class ReaderOutput : XmlReader, RecordOutput
    {
        private Processor _processor;
        private XmlNameTable _nameTable;

        // Main node + Fields Collection
        private RecordBuilder _builder;
        private BuilderInfo _mainNode;
        private ArrayList _attributeList;
        private int _attributeCount;
        private BuilderInfo _attributeValue;

        // OutputScopeManager
        private OutputScopeManager _manager;

        // Current position in the list
        private int _currentIndex;
        private BuilderInfo _currentInfo;

        // Reader state
        private ReadState _state = ReadState.Initial;
        private bool _haveRecord;

        // Static default record
        private static BuilderInfo s_DefaultInfo = new BuilderInfo();

        private XmlEncoder _encoder = new XmlEncoder();
        private XmlCharType _xmlCharType = XmlCharType.Instance;

        internal ReaderOutput(Processor processor)
        {
            Debug.Assert(processor != null);
            Debug.Assert(processor.NameTable != null);

            _processor = processor;
            _nameTable = processor.NameTable;

            Reset();
        }

        // XmlReader abstract methods implementation
        public override XmlNodeType NodeType
        {
            get
            {
                CheckCurrentInfo();
                return _currentInfo.NodeType;
            }
        }

        public override string Name
        {
            get
            {
                CheckCurrentInfo();
                string prefix = Prefix;
                string localName = LocalName;

                if (prefix != null && prefix.Length > 0)
                {
                    if (localName.Length > 0)
                    {
                        return _nameTable.Add(prefix + ":" + localName);
                    }
                    else
                    {
                        return prefix;
                    }
                }
                else
                {
                    return localName;
                }
            }
        }

        public override string LocalName
        {
            get
            {
                CheckCurrentInfo();
                return _currentInfo.LocalName;
            }
        }

        public override string NamespaceURI
        {
            get
            {
                CheckCurrentInfo();
                return _currentInfo.NamespaceURI;
            }
        }

        public override string Prefix
        {
            get
            {
                CheckCurrentInfo();
                return _currentInfo.Prefix;
            }
        }

        public override bool HasValue
        {
            get
            {
                return XmlReader.HasValueInternal(NodeType);
            }
        }

        public override string Value
        {
            get
            {
                CheckCurrentInfo();
                return _currentInfo.Value;
            }
        }

        public override int Depth
        {
            get
            {
                CheckCurrentInfo();
                return _currentInfo.Depth;
            }
        }

        public override string BaseURI
        {
            get
            {
                return string.Empty;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                CheckCurrentInfo();
                return _currentInfo.IsEmptyTag;
            }
        }

        public override char QuoteChar
        {
            get { return _encoder.QuoteChar; }
        }

        public override bool IsDefault
        {
            get { return false; }
        }

        public override XmlSpace XmlSpace
        {
            get { return _manager != null ? _manager.XmlSpace : XmlSpace.None; }
        }

        public override string XmlLang
        {
            get { return _manager != null ? _manager.XmlLang : string.Empty; }
        }

        // Attribute Accessors

        public override int AttributeCount
        {
            get { return _attributeCount; }
        }

        public override string GetAttribute(string name)
        {
            int ordinal;
            if (FindAttribute(name, out ordinal))
            {
                Debug.Assert(ordinal >= 0);
                return ((BuilderInfo)_attributeList[ordinal]).Value;
            }
            else
            {
                Debug.Assert(ordinal == -1);
                return null;
            }
        }

        public override string GetAttribute(string localName, string namespaceURI)
        {
            int ordinal;
            if (FindAttribute(localName, namespaceURI, out ordinal))
            {
                Debug.Assert(ordinal >= 0);
                return ((BuilderInfo)_attributeList[ordinal]).Value;
            }
            else
            {
                Debug.Assert(ordinal == -1);
                return null;
            }
        }

        public override string GetAttribute(int i)
        {
            BuilderInfo attribute = GetBuilderInfo(i);
            return attribute.Value;
        }

        public override string this[int i]
        {
            get { return GetAttribute(i); }
        }

        public override string this[string name, string namespaceURI]
        {
            get { return GetAttribute(name, namespaceURI); }
        }

        public override bool MoveToAttribute(string name)
        {
            int ordinal;
            if (FindAttribute(name, out ordinal))
            {
                Debug.Assert(ordinal >= 0);
                SetAttribute(ordinal);
                return true;
            }
            else
            {
                Debug.Assert(ordinal == -1);
                return false;
            }
        }

        public override bool MoveToAttribute(string localName, string namespaceURI)
        {
            int ordinal;
            if (FindAttribute(localName, namespaceURI, out ordinal))
            {
                Debug.Assert(ordinal >= 0);
                SetAttribute(ordinal);
                return true;
            }
            else
            {
                Debug.Assert(ordinal == -1);
                return false;
            }
        }

        public override void MoveToAttribute(int i)
        {
            if (i < 0 || _attributeCount <= i)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }
            SetAttribute(i);
        }

        public override bool MoveToFirstAttribute()
        {
            if (_attributeCount <= 0)
            {
                Debug.Assert(_attributeCount == 0);
                return false;
            }
            else
            {
                SetAttribute(0);
                return true;
            }
        }

        public override bool MoveToNextAttribute()
        {
            if (_currentIndex + 1 < _attributeCount)
            {
                SetAttribute(_currentIndex + 1);
                return true;
            }
            return false;
        }

        public override bool MoveToElement()
        {
            if (NodeType == XmlNodeType.Attribute || _currentInfo == _attributeValue)
            {
                SetMainNode();
                return true;
            }
            return false;
        }

        // Moving through the Stream

        public override bool Read()
        {
            Debug.Assert(_processor != null || _state == ReadState.Closed);

            if (_state != ReadState.Interactive)
            {
                if (_state == ReadState.Initial)
                {
                    _state = ReadState.Interactive;
                }
                else
                {
                    return false;
                }
            }

            while (true)
            { // while -- to ignor empty whitespace nodes.
                if (_haveRecord)
                {
                    _processor.ResetOutput();
                    _haveRecord = false;
                }

                _processor.Execute();

                if (_haveRecord)
                {
                    CheckCurrentInfo();
                    // check text nodes on whitespace;
                    switch (this.NodeType)
                    {
                        case XmlNodeType.Text:
                            if (_xmlCharType.IsOnlyWhitespace(this.Value))
                            {
                                _currentInfo.NodeType = XmlNodeType.Whitespace;
                                goto case XmlNodeType.Whitespace;
                            }
                            Debug.Assert(this.Value.Length != 0, "It whould be Whitespace in this case");
                            break;
                        case XmlNodeType.Whitespace:
                            if (this.Value.Length == 0)
                            {
                                continue;                          // ignoring emty text nodes
                            }
                            if (this.XmlSpace == XmlSpace.Preserve)
                            {
                                _currentInfo.NodeType = XmlNodeType.SignificantWhitespace;
                            }
                            break;
                    }
                }
                else
                {
                    Debug.Assert(_processor.ExecutionDone);
                    _state = ReadState.EndOfFile;
                    Reset();
                }

                return _haveRecord;
            }
        }

        public override bool EOF
        {
            get { return _state == ReadState.EndOfFile; }
        }

        public override void Close()
        {
            _processor = null;
            _state = ReadState.Closed;
            Reset();
        }

        public override ReadState ReadState
        {
            get { return _state; }
        }

        // Whole Content Read Methods
        public override string ReadString()
        {
            string result = string.Empty;

            if (NodeType == XmlNodeType.Element || NodeType == XmlNodeType.Attribute || _currentInfo == _attributeValue)
            {
                if (_mainNode.IsEmptyTag)
                {
                    return result;
                }
                if (!Read())
                {
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                }
            }

            StringBuilder sb = null;
            bool first = true;

            while (true)
            {
                switch (NodeType)
                {
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        //              case XmlNodeType.CharacterEntity:
                        if (first)
                        {
                            result = this.Value;
                            first = false;
                        }
                        else
                        {
                            if (sb == null)
                            {
                                sb = new StringBuilder(result);
                            }
                            sb.Append(this.Value);
                        }
                        if (!Read())
                            throw new InvalidOperationException(SR.Xml_InvalidOperation);
                        break;
                    default:
                        return (sb == null) ? result : sb.ToString();
                }
            }
        }

        public override string ReadInnerXml()
        {
            if (ReadState == ReadState.Interactive)
            {
                if (NodeType == XmlNodeType.Element && !IsEmptyElement)
                {
                    StringOutput output = new StringOutput(_processor);
                    output.OmitXmlDecl();
                    int depth = Depth;

                    Read();                 // skeep  begin Element
                    while (depth < Depth)
                    { // process content
                        Debug.Assert(_builder != null);
                        output.RecordDone(_builder);
                        Read();
                    }
                    Debug.Assert(NodeType == XmlNodeType.EndElement);
                    Read();                 // skeep end element

                    output.TheEnd();
                    return output.Result;
                }
                else if (NodeType == XmlNodeType.Attribute)
                {
                    return _encoder.AttributeInnerXml(Value);
                }
                else
                {
                    Read();
                }
            }
            return string.Empty;
        }

        public override string ReadOuterXml()
        {
            if (ReadState == ReadState.Interactive)
            {
                if (NodeType == XmlNodeType.Element)
                {
                    StringOutput output = new StringOutput(_processor);
                    output.OmitXmlDecl();
                    bool emptyElement = IsEmptyElement;
                    int depth = Depth;
                    // process current record
                    output.RecordDone(_builder);
                    Read();
                    // process internal elements & text nodes
                    while (depth < Depth)
                    {
                        Debug.Assert(_builder != null);
                        output.RecordDone(_builder);
                        Read();
                    }
                    // process end element
                    if (!emptyElement)
                    {
                        output.RecordDone(_builder);
                        Read();
                    }

                    output.TheEnd();
                    return output.Result;
                }
                else if (NodeType == XmlNodeType.Attribute)
                {
                    return _encoder.AttributeOuterXml(Name, Value);
                }
                else
                {
                    Read();
                }
            }
            return string.Empty;
        }

        //
        // Nametable and Namespace Helpers
        //

        public override XmlNameTable NameTable
        {
            get
            {
                Debug.Assert(_nameTable != null);
                return _nameTable;
            }
        }

        public override string LookupNamespace(string prefix)
        {
            prefix = _nameTable.Get(prefix);

            if (_manager != null && prefix != null)
            {
                return _manager.ResolveNamespace(prefix);
            }
            return null;
        }

        public override void ResolveEntity()
        {
            Debug.Assert(NodeType != XmlNodeType.EntityReference);

            if (NodeType != XmlNodeType.EntityReference)
            {
                throw new InvalidOperationException(SR.Xml_InvalidOperation);
            }
        }

        public override bool ReadAttributeValue()
        {
            if (ReadState != ReadState.Interactive || NodeType != XmlNodeType.Attribute)
            {
                return false;
            }

            if (_attributeValue == null)
            {
                _attributeValue = new BuilderInfo();
                _attributeValue.NodeType = XmlNodeType.Text;
            }
            if (_currentInfo == _attributeValue)
            {
                return false;
            }

            _attributeValue.Value = _currentInfo.Value;
            _attributeValue.Depth = _currentInfo.Depth + 1;
            _currentInfo = _attributeValue;

            return true;
        }

        //
        // RecordOutput interface method implementation
        //

        public Processor.OutputResult RecordDone(RecordBuilder record)
        {
            _builder = record;
            _mainNode = record.MainNode;
            _attributeList = record.AttributeList;
            _attributeCount = record.AttributeCount;
            _manager = record.Manager;

            _haveRecord = true;
            SetMainNode();

            return Processor.OutputResult.Interrupt;
        }

        public void TheEnd()
        {
            // nothing here, was taken care of by RecordBuilder
        }

        //
        // Implementation internals
        //

        private void SetMainNode()
        {
            _currentIndex = -1;
            _currentInfo = _mainNode;
        }

        private void SetAttribute(int attrib)
        {
            Debug.Assert(0 <= attrib && attrib < _attributeCount);
            Debug.Assert(0 <= attrib && attrib < _attributeList.Count);
            Debug.Assert(_attributeList[attrib] is BuilderInfo);

            _currentIndex = attrib;
            _currentInfo = (BuilderInfo)_attributeList[attrib];
        }

        private BuilderInfo GetBuilderInfo(int attrib)
        {
            if (attrib < 0 || _attributeCount <= attrib)
            {
                throw new ArgumentOutOfRangeException(nameof(attrib));
            }

            Debug.Assert(_attributeList[attrib] is BuilderInfo);

            return (BuilderInfo)_attributeList[attrib];
        }

        private bool FindAttribute(string localName, string namespaceURI, out int attrIndex)
        {
            if (namespaceURI == null)
            {
                namespaceURI = string.Empty;
            }
            if (localName == null)
            {
                localName = string.Empty;
            }

            for (int index = 0; index < _attributeCount; index++)
            {
                Debug.Assert(_attributeList[index] is BuilderInfo);

                BuilderInfo attribute = (BuilderInfo)_attributeList[index];
                if (attribute.NamespaceURI == namespaceURI && attribute.LocalName == localName)
                {
                    attrIndex = index;
                    return true;
                }
            }

            attrIndex = -1;
            return false;
        }

        private bool FindAttribute(string name, out int attrIndex)
        {
            if (name == null)
            {
                name = string.Empty;
            }

            for (int index = 0; index < _attributeCount; index++)
            {
                Debug.Assert(_attributeList[index] is BuilderInfo);

                BuilderInfo attribute = (BuilderInfo)_attributeList[index];
                if (attribute.Name == name)
                {
                    attrIndex = index;
                    return true;
                }
            }

            attrIndex = -1;
            return false;
        }

        private void Reset()
        {
            _currentIndex = -1;
            _currentInfo = s_DefaultInfo;
            _mainNode = s_DefaultInfo;
            _manager = null;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void CheckCurrentInfo()
        {
            Debug.Assert(_currentInfo != null);
            Debug.Assert(_attributeCount == 0 || _attributeList != null);
            Debug.Assert((_currentIndex == -1) == (_currentInfo == _mainNode));
            Debug.Assert((_currentIndex == -1) || (_currentInfo == _attributeValue || _attributeList[_currentIndex] is BuilderInfo && _attributeList[_currentIndex] == _currentInfo));
        }

        private class XmlEncoder
        {
            private StringBuilder _buffer = null;
            private XmlTextEncoder _encoder = null;

            private void Init()
            {
                _buffer = new StringBuilder();
                _encoder = new XmlTextEncoder(new StringWriter(_buffer, CultureInfo.InvariantCulture));
            }

            public string AttributeInnerXml(string value)
            {
                if (_encoder == null) Init();
                _buffer.Length = 0;       // clean buffer
                _encoder.StartAttribute(/*save:*/false);
                _encoder.Write(value);
                _encoder.EndAttribute();
                return _buffer.ToString();
            }

            public string AttributeOuterXml(string name, string value)
            {
                if (_encoder == null) Init();
                _buffer.Length = 0;       // clean buffer
                _buffer.Append(name);
                _buffer.Append('=');
                _buffer.Append(QuoteChar);
                _encoder.StartAttribute(/*save:*/false);
                _encoder.Write(value);
                _encoder.EndAttribute();
                _buffer.Append(QuoteChar);
                return _buffer.ToString();
            }

            public char QuoteChar
            {
                get { return '"'; }
            }
        }
    }
}
