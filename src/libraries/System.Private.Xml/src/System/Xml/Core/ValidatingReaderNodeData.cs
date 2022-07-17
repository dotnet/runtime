// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Xml.Schema;
using System.Diagnostics;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    internal sealed class ValidatingReaderNodeData
    {
        private string _localName;
        private string _namespaceUri;
        private string _prefix;
        private string? _nameWPrefix;

        private string _rawValue;
        private string? _originalStringValue;  // Original value
        private int _depth;
        private AttributePSVIInfo? _attributePSVIInfo;  //Used only for default attributes
        private XmlNodeType _nodeType;

        private int _lineNo;
        private int _linePos;

        public ValidatingReaderNodeData()
        {
            Clear(XmlNodeType.None);
        }

        public ValidatingReaderNodeData(XmlNodeType nodeType)
        {
            Clear(nodeType);
        }

        public string LocalName
        {
            get
            {
                return _localName;
            }
            set
            {
                _localName = value;
            }
        }

        public string Namespace
        {
            get
            {
                return _namespaceUri;
            }
            set
            {
                _namespaceUri = value;
            }
        }

        public string Prefix
        {
            get
            {
                return _prefix;
            }
            set
            {
                _prefix = value;
            }
        }

        public string GetAtomizedNameWPrefix(XmlNameTable nameTable)
        {
            if (_nameWPrefix == null)
            {
                if (_prefix.Length == 0)
                {
                    _nameWPrefix = _localName;
                }
                else
                {
                    _nameWPrefix = nameTable.Add($"{_prefix}:{_localName}");
                }
            }

            return _nameWPrefix;
        }

        public int Depth
        {
            get
            {
                return _depth;
            }
            set
            {
                _depth = value;
            }
        }

        public string RawValue
        {
            get
            {
                return _rawValue;
            }
            set
            {
                _rawValue = value;
            }
        }

        public string? OriginalStringValue
        {
            get
            {
                return _originalStringValue;
            }
            set
            {
                _originalStringValue = value;
            }
        }

        public XmlNodeType NodeType
        {
            get
            {
                return _nodeType;
            }
            set
            {
                _nodeType = value;
            }
        }

        public AttributePSVIInfo? AttInfo
        {
            get
            {
                return _attributePSVIInfo;
            }
            set
            {
                _attributePSVIInfo = value;
            }
        }

        public int LineNumber
        {
            get
            {
                return _lineNo;
            }
        }

        public int LinePosition
        {
            get
            {
                return _linePos;
            }
        }

        [MemberNotNull(nameof(_localName))]
        [MemberNotNull(nameof(_prefix))]
        [MemberNotNull(nameof(_namespaceUri))]
        [MemberNotNull(nameof(_rawValue))]
        internal void Clear(XmlNodeType nodeType)
        {
            _nodeType = nodeType;
            _localName = string.Empty;
            _prefix = string.Empty;
            _namespaceUri = string.Empty;
            _rawValue = string.Empty;

            _attributePSVIInfo?.Reset();

            _nameWPrefix = null;
            _lineNo = 0;
            _linePos = 0;
        }

        internal void SetLineInfo(int lineNo, int linePos)
        {
            _lineNo = lineNo;
            _linePos = linePos;
        }

        internal void SetLineInfo(IXmlLineInfo? lineInfo)
        {
            if (lineInfo != null)
            {
                _lineNo = lineInfo.LineNumber;
                _linePos = lineInfo.LinePosition;
            }
        }

        internal void SetItemData(string localName, string prefix, string ns, int depth)
        {
            _localName = localName;
            _prefix = prefix;
            _namespaceUri = ns;
            _depth = depth;
            _rawValue = string.Empty;
        }

        internal void SetItemData(string value)
        {
            SetItemData(value, value);
        }

        internal void SetItemData(string value, string? originalStringValue)
        {
            _rawValue = value;
            _originalStringValue = originalStringValue;
        }
    }
}
