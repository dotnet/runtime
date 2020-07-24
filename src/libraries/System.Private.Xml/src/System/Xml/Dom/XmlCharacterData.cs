// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.XPath;

namespace System.Xml
{
    // Provides text-manipulation methods that are used by several classes.
    public abstract class XmlCharacterData : XmlLinkedNode
    {
        private string? _data;

        //base(doc) will throw exception if doc is null.
        protected internal XmlCharacterData(string? data, XmlDocument doc) : base(doc)
        {
            _data = data;
        }

        // Gets or sets the value of the node.
        public override string? Value
        {
            get { return Data; }
            set { Data = value; }
        }

        // Gets or sets the concatenated values of the node and
        // all its children.
        public override string InnerText
        {
            get { return Value!; }
            set { Value = value; }
        }

        // Contains this node's data.
        [AllowNull]
        public virtual string Data
        {
            get
            {
                if (_data != null)
                {
                    return _data;
                }
                else
                {
                    return string.Empty;
                }
            }

            set
            {
                XmlNode? parent = ParentNode;
                XmlNodeChangedEventArgs? args = GetEventArgs(this, parent, parent, _data, value, XmlNodeChangedAction.Change);

                if (args != null)
                    BeforeEvent(args);

                _data = value;

                if (args != null)
                    AfterEvent(args);
            }
        }

        // Gets the length of the data, in characters.
        public virtual int Length
        {
            get
            {
                if (_data != null)
                {
                    return _data.Length;
                }
                return 0;
            }
        }

        // Retrieves a substring of the full string from the specified range.
        public virtual string Substring(int offset, int count)
        {
            int len = _data != null ? _data.Length : 0;
            if (len > 0)
            {
                if (len < (offset + count))
                {
                    count = len - offset;
                }

                return _data!.Substring(offset, count);
            }
            return string.Empty;
        }

        // Appends the specified string to the end of the character
        // data of the node.
        public virtual void AppendData(string? strData)
        {
            XmlNode? parent = ParentNode;
            int capacity = _data != null ? _data.Length : 0;
            if (strData != null) capacity += strData.Length;
            string newValue = new StringBuilder(capacity).Append(_data).Append(strData).ToString();
            XmlNodeChangedEventArgs? args = GetEventArgs(this, parent, parent, _data, newValue, XmlNodeChangedAction.Change);

            if (args != null)
                BeforeEvent(args);

            _data = newValue;

            if (args != null)
                AfterEvent(args);
        }

        // Insert the specified string at the specified character offset.
        public virtual void InsertData(int offset, string? strData)
        {
            XmlNode? parent = ParentNode;
            int capacity = _data != null ? _data.Length : 0;
            if (strData != null) capacity += strData.Length;
            string newValue = new StringBuilder(capacity).Append(_data).Insert(offset, strData).ToString();
            XmlNodeChangedEventArgs? args = GetEventArgs(this, parent, parent, _data, newValue, XmlNodeChangedAction.Change);
            if (args != null)
                BeforeEvent(args);

            _data = newValue;

            if (args != null)
                AfterEvent(args);
        }

        // Remove a range of characters from the node.
        public virtual void DeleteData(int offset, int count)
        {
            //Debug.Assert(offset >= 0 && offset <= Length);

            int len = _data != null ? _data.Length : 0;
            if (len > 0)
            {
                if (len < (offset + count))
                {
                    count = Math.Max(len - offset, 0);
                }
            }

            string newValue = new StringBuilder(_data).Remove(offset, count).ToString();
            XmlNode? parent = ParentNode;
            XmlNodeChangedEventArgs? args = GetEventArgs(this, parent, parent, _data, newValue, XmlNodeChangedAction.Change);

            if (args != null)
                BeforeEvent(args);

            _data = newValue;

            if (args != null)
                AfterEvent(args);
        }

        // Replace the specified number of characters starting at the specified offset with the
        // specified string.
        public virtual void ReplaceData(int offset, int count, string? strData)
        {
            int len = _data != null ? _data.Length : 0;
            if (len > 0)
            {
                if (len < (offset + count))
                {
                    count = Math.Max(len - offset, 0);
                }
            }

            StringBuilder temp = new StringBuilder(_data).Remove(offset, count);
            string newValue = temp.Insert(offset, strData).ToString();

            XmlNode? parent = ParentNode;
            XmlNodeChangedEventArgs? args = GetEventArgs(this, parent, parent, _data, newValue, XmlNodeChangedAction.Change);

            if (args != null)
                BeforeEvent(args);

            _data = newValue;

            if (args != null)
                AfterEvent(args);
        }

        internal bool CheckOnData(string? data)
        {
            return XmlCharType.Instance.IsOnlyWhitespace(data);
        }

        internal bool DecideXPNodeTypeForTextNodes(XmlNode node, ref XPathNodeType xnt)
        {
            //returns true - if all siblings of the node are processed else returns false.
            //The reference XPathNodeType argument being passed in is the watermark that
            //changes according to the siblings nodetype and will contain the correct
            //nodetype when it returns.

            XmlNode? n = node;
            Debug.Assert(XmlDocument.IsTextNode(n.NodeType) || (n.ParentNode != null && n.ParentNode.NodeType == XmlNodeType.EntityReference));
            while (n != null)
            {
                switch (n.NodeType)
                {
                    case XmlNodeType.Whitespace:
                        break;
                    case XmlNodeType.SignificantWhitespace:
                        xnt = XPathNodeType.SignificantWhitespace;
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        xnt = XPathNodeType.Text;
                        return false;
                    case XmlNodeType.EntityReference:
                        if (!DecideXPNodeTypeForTextNodes(n.FirstChild!, ref xnt))
                        {
                            return false;
                        }
                        break;
                    default:
                        return false;
                }
                n = n.NextSibling;
            }
            return true;
        }
    }
}
