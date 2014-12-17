// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace System.Xml
{
    // Provides text-manipulation methods that are used by several classes.
    public abstract class XmlCharacterData : XmlLinkedNode
    {
        string data;

        //base(doc) will throw exception if doc is null.
        protected internal XmlCharacterData(string data, XmlDocument doc) : base(doc)
        {
            this.data = data;
        }

        // Gets or sets the value of the node.
        public override String Value
        {
            get { return Data; }
            set { Data = value; }
        }

        // Gets or sets the concatenated values of the node and
        // all its children.
        public override string InnerText
        {
            get { return Value; }
            set { Value = value; }
        }

        // Contains this node's data.
        public virtual string Data
        {
            get
            {
                if (data != null)
                {
                    return data;
                }
                else
                {
                    return String.Empty;
                }
            }

            set
            {
                XmlNode parent = ParentNode;
                XmlNodeChangedEventArgs args = GetEventArgs(this, parent, parent, this.data, value, XmlNodeChangedAction.Change);

                if (args != null)
                    BeforeEvent(args);

                data = value;

                if (args != null)
                    AfterEvent(args);
            }
        }

        // Gets the length of the data, in characters.
        public virtual int Length
        {
            get
            {
                if (data != null)
                {
                    return data.Length;
                }
                return 0;
            }
        }

        // Retrieves a substring of the full string from the specified range.
        public virtual String Substring(int offset, int count)
        {
            int len = data != null ? data.Length : 0;
            if (len > 0)
            {
                if (len < (offset + count))
                {
                    count = len - offset;
                }
                return data.Substring(offset, count);
            }
            return String.Empty;
        }

        // Appends the specified string to the end of the character
        // data of the node.
        public virtual void AppendData(String strData)
        {
            XmlNode parent = ParentNode;
            int capacity = data != null ? data.Length : 0;
            if (strData != null) capacity += strData.Length;
            string newValue = new StringBuilder(capacity).Append(data).Append(strData).ToString();
            XmlNodeChangedEventArgs args = GetEventArgs(this, parent, parent, data, newValue, XmlNodeChangedAction.Change);

            if (args != null)
                BeforeEvent(args);

            this.data = newValue;

            if (args != null)
                AfterEvent(args);
        }

        // Insert the specified string at the specified character offset.
        public virtual void InsertData(int offset, string strData)
        {
            XmlNode parent = ParentNode;
            int capacity = data != null ? data.Length : 0;
            if (strData != null) capacity += strData.Length;
            string newValue = new StringBuilder(capacity).Append(data).Insert(offset, strData).ToString();
            XmlNodeChangedEventArgs args = GetEventArgs(this, parent, parent, data, newValue, XmlNodeChangedAction.Change);
            if (args != null)
                BeforeEvent(args);

            this.data = newValue;

            if (args != null)
                AfterEvent(args);
        }

        // Remove a range of characters from the node.
        public virtual void DeleteData(int offset, int count)
        {
            //Debug.Assert(offset >= 0 && offset <= Length);

            int len = data != null ? data.Length : 0;
            if (len > 0)
            {
                if (len < (offset + count))
                {
                    count = Math.Max(len - offset, 0);
                }
            }

            string newValue = new StringBuilder(data).Remove(offset, count).ToString();
            XmlNode parent = ParentNode;
            XmlNodeChangedEventArgs args = GetEventArgs(this, parent, parent, data, newValue, XmlNodeChangedAction.Change);

            if (args != null)
                BeforeEvent(args);

            this.data = newValue;

            if (args != null)
                AfterEvent(args);
        }

        // Replace the specified number of characters starting at the specified offset with the
        // specified string.
        public virtual void ReplaceData(int offset, int count, String strData)
        {
            int len = data != null ? data.Length : 0;
            if (len > 0)
            {
                if (len < (offset + count))
                {
                    count = Math.Max(len - offset, 0);
                }
            }

            StringBuilder temp = new StringBuilder(data).Remove(offset, count);
            string newValue = temp.Insert(offset, strData).ToString();

            XmlNode parent = ParentNode;
            XmlNodeChangedEventArgs args = GetEventArgs(this, parent, parent, data, newValue, XmlNodeChangedAction.Change);

            if (args != null)
                BeforeEvent(args);

            this.data = newValue;

            if (args != null)
                AfterEvent(args);
        }

        internal bool CheckOnData(string data)
        {
            return XmlCharType.Instance.IsOnlyWhitespace(data);
        }
    }
}

