// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class BuilderInfo
    {
        private string? _name;
        private string _localName;
        private string _namespaceURI;
        private string _prefix;

        private XmlNodeType _nodeType;
        private int _depth;
        private bool _isEmptyTag;

        internal string?[] TextInfo = new string[4];
        internal int TextInfoCount;

        internal bool search;
        internal HtmlElementProps? htmlProps;
        internal HtmlAttributeProps? htmlAttrProps;

        internal BuilderInfo()
        {
            Initialize(string.Empty, string.Empty, string.Empty);
        }

        [MemberNotNull(nameof(_prefix))]
        [MemberNotNull(nameof(_localName))]
        [MemberNotNull(nameof(_namespaceURI))]
        internal void Initialize(string prefix, string name, string nspace)
        {
            _prefix = prefix;
            _localName = name;
            _namespaceURI = nspace;
            _name = null;
            this.htmlProps = null;
            this.htmlAttrProps = null;
            this.TextInfoCount = 0;
        }

        [MemberNotNull(nameof(_prefix))]
        [MemberNotNull(nameof(_localName))]
        [MemberNotNull(nameof(_namespaceURI))]
        internal void Initialize(BuilderInfo src)
        {
            _prefix = src.Prefix;
            _localName = src.LocalName;
            _namespaceURI = src.NamespaceURI;
            _name = null;
            _depth = src.Depth;
            _nodeType = src.NodeType;
            this.htmlProps = src.htmlProps;
            this.htmlAttrProps = src.htmlAttrProps;

            this.TextInfoCount = 0;
            EnsureTextInfoSize(src.TextInfoCount);
            src.TextInfo.CopyTo(this.TextInfo, 0);
            this.TextInfoCount = src.TextInfoCount;
        }

        private void EnsureTextInfoSize(int newSize)
        {
            if (this.TextInfo.Length < newSize)
            {
                string[] newArr = new string[newSize * 2];
                Array.Copy(this.TextInfo, newArr, this.TextInfoCount);
                this.TextInfo = newArr;
            }
        }

        internal BuilderInfo Clone()
        {
            BuilderInfo info = new BuilderInfo();
            info.Initialize(this);
            Debug.Assert(info.NodeType != XmlNodeType.Text || XmlCharType.IsOnlyWhitespace(info.Value));
            return info;
        }

        internal string Name
        {
            get
            {
                if (_name == null)
                {
                    string prefix = Prefix;
                    string localName = LocalName;

                    if (prefix != null && 0 < prefix.Length)
                    {
                        if (localName.Length > 0)
                        {
                            _name = $"{prefix}:{localName}";
                        }
                        else
                        {
                            _name = prefix;
                        }
                    }
                    else
                    {
                        _name = localName;
                    }
                }
                return _name;
            }
        }

        internal string LocalName
        {
            get { return _localName; }
            set { _localName = value; }
        }
        internal string NamespaceURI
        {
            get { return _namespaceURI; }
            set { _namespaceURI = value; }
        }
        internal string Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }

        // The true value of this object is a list of TextInfo
        // Value.get merges them together but discards each node's escape info
        // Value.set clears this object, and appends the new, single string
        internal string Value
        {
            get
            {
                switch (this.TextInfoCount)
                {
                    case 0: return string.Empty;
                    case 1: return this.TextInfo[0]!;
                    default:
                        int size = 0;
                        for (int i = 0; i < this.TextInfoCount; i++)
                        {
                            string? ti = this.TextInfo[i];
                            if (ti == null) continue; // ignore disableEscaping
                            size += ti.Length;
                        }
                        StringBuilder sb = new StringBuilder(size);
                        for (int i = 0; i < this.TextInfoCount; i++)
                        {
                            string? ti = this.TextInfo[i];
                            if (ti == null) continue; // ignore disableEscaping
                            sb.Append(ti);
                        }
                        return sb.ToString();
                }
            }
            set
            {
                this.TextInfoCount = 0;
                ValueAppend(value, /*disableEscaping:*/false);
            }
        }

        internal void ValueAppend(string? s, bool disableEscaping)
        {
            if (s == null || s.Length == 0)
            {
                return;
            }
            EnsureTextInfoSize(this.TextInfoCount + (disableEscaping ? 2 : 1));
            if (disableEscaping)
            {
                this.TextInfo[this.TextInfoCount++] = null;
            }
            this.TextInfo[this.TextInfoCount++] = s;
        }

        internal XmlNodeType NodeType
        {
            get { return _nodeType; }
            set { _nodeType = value; }
        }
        internal int Depth
        {
            get { return _depth; }
            set { _depth = value; }
        }
        internal bool IsEmptyTag
        {
            get { return _isEmptyTag; }
            set { _isEmptyTag = value; }
        }
    }
}
