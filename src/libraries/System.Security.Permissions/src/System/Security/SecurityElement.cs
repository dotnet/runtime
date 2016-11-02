// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.s

using System.Collections;
using System.Text;
using System.Globalization;
using System.IO;
using System.Diagnostics;

namespace System.Security
{
    internal enum SecurityElementType
    {
        Regular = 0,
        Format = 1,
        Comment = 2
    }

    internal interface ISecurityElementFactory
    {
        SecurityElement CreateSecurityElement();

        object Copy();

        string GetTag();

        string Attribute(string attributeName);
    }

    [Serializable]
    sealed public class SecurityElement : ISecurityElementFactory
    {
        internal string m_strTag;
        internal string m_strText;
        private ArrayList m_lChildren;
        internal ArrayList m_lAttributes;

        internal readonly SecurityElementType m_type = SecurityElementType.Regular;
        private const int c_AttributesTypical = 4 * 2;  // 4 attributes, times 2 strings per attribute
        private const int c_ChildrenTypical = 1;

        private static readonly char[] s_tagIllegalCharacters = new char[] { ' ', '<', '>' };
        private static readonly char[] s_textIllegalCharacters = new char[] { '<', '>' };
        private static readonly char[] s_valueIllegalCharacters = new char[] { '<', '>', '\"' };
        private const string s_strIndent = "   ";

        private static readonly string[] s_escapeStringPairs = new string[]
        {
            // these must be all once character escape sequences or a new escaping algorithm is needed
            "<", "&lt;",
            ">", "&gt;",
            "\"", "&quot;",
            "\'", "&apos;",
            "&", "&amp;"
        };

        private static readonly char[] s_escapeChars = new char[] { '<', '>', '\"', '\'', '&' };

        //-------------------------- Constructors ---------------------------

        internal SecurityElement()
        {
        }

        public SecurityElement(string tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            if (!IsValidTag(tag))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Argument_InvalidElementTag, tag));

            m_strTag = tag;
            m_strText = null;
        }

        public SecurityElement(string tag, string text)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            if (!IsValidTag(tag))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Argument_InvalidElementTag, tag));

            if (text != null && !IsValidText(text))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Argument_InvalidElementText, text));

            m_strTag = tag;
            m_strText = text;
        }

        //-------------------------- Properties -----------------------------

        public string Tag
        {
            get
            {
                return m_strTag;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(Tag));

                if (!IsValidTag(value))
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Argument_InvalidElementTag, value));

                m_strTag = value;
            }
        }

        public Hashtable Attributes
        {
            get
            {
                if (m_lAttributes == null || m_lAttributes.Count == 0)
                {
                    return null;
                }
                else
                {
                    Hashtable hashtable = new Hashtable(m_lAttributes.Count / 2);

                    int iMax = m_lAttributes.Count;
                    Debug.Assert(iMax % 2 == 0, "Odd number of strings means the attr/value pairs were not added correctly");

                    for (int i = 0; i < iMax; i += 2)
                    {
                        hashtable.Add(m_lAttributes[i], m_lAttributes[i + 1]);
                    }

                    return hashtable;
                }
            }

            set
            {
                if (value == null || value.Count == 0)
                {
                    m_lAttributes = null;
                }
                else
                {
                    ArrayList list = new ArrayList(value.Count);
                    IDictionaryEnumerator enumerator = value.GetEnumerator();

                    while (enumerator.MoveNext())
                    {
                        string attrName = (string)enumerator.Key;
                        string attrValue = (string)enumerator.Value;

                        if (!IsValidAttributeName(attrName))
                            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Argument_InvalidElementName, (string)enumerator.Current));

                        if (!IsValidAttributeValue(attrValue))
                            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Argument_InvalidElementValue, (string)enumerator.Value));

                        list.Add(attrName);
                        list.Add(attrValue);
                    }

                    m_lAttributes = list;
                }
            }
        }

        public string Text
        {
            get
            {
                return Unescape(m_strText);
            }

            set
            {
                if (value == null)
                {
                    m_strText = null;
                }
                else
                {
                    if (!IsValidText(value))
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Argument_InvalidElementTag, value));

                    m_strText = value;
                }
            }
        }

        public ArrayList Children
        {
            get
            {
                ConvertSecurityElementFactories();
                return m_lChildren;
            }

            set
            {
                if (value != null)
                {
                    IEnumerator enumerator = value.GetEnumerator();

                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current == null)
                            throw new ArgumentException(SR.ArgumentNull_Child);
                    }
                }

                m_lChildren = value;
            }
        }

        internal void ConvertSecurityElementFactories()
        {
            if (m_lChildren == null)
                return;

            for (int i = 0; i < m_lChildren.Count; ++i)
            {
                ISecurityElementFactory iseFactory = m_lChildren[i] as ISecurityElementFactory;
                if (iseFactory != null && !(m_lChildren[i] is SecurityElement))
                    m_lChildren[i] = iseFactory.CreateSecurityElement();
            }
        }

        internal ArrayList InternalChildren
        {
            get
            {
                // Beware!  This array list can contain SecurityElements and other ISecurityElementFactories.
                // If you want to get a consistent SecurityElement view, call get_Children.
                return m_lChildren;
            }
        }

        //-------------------------- Public Methods -----------------------------

        internal void AddAttributeSafe(string name, string value)
        {
            if (m_lAttributes == null)
            {
                m_lAttributes = new ArrayList(c_AttributesTypical);
            }
            else
            {
                int iMax = m_lAttributes.Count;
                Debug.Assert(iMax % 2 == 0, "Odd number of strings means the attr/value pairs were not added correctly");

                for (int i = 0; i < iMax; i += 2)
                {
                    string strAttrName = (string)m_lAttributes[i];

                    if (string.Equals(strAttrName, name))
                        throw new ArgumentException(SR.Argument_AttributeNamesMustBeUnique);
                }
            }

            m_lAttributes.Add(name);
            m_lAttributes.Add(value);
        }

        public void AddAttribute(string name, string value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!IsValidAttributeName(name))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Argument_InvalidElementName, name));

            if (!IsValidAttributeValue(value))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Argument_InvalidElementValue, value));

            AddAttributeSafe(name, value);
        }

        public void AddChild(SecurityElement child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            if (m_lChildren == null)
                m_lChildren = new ArrayList(c_ChildrenTypical);

            m_lChildren.Add(child);
        }

        internal void AddChild(ISecurityElementFactory child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));
            

            if (m_lChildren == null)
                m_lChildren = new ArrayList(c_ChildrenTypical);

            m_lChildren.Add(child);
        }

        internal void AddChildNoDuplicates(ISecurityElementFactory child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            if (m_lChildren == null)
            {
                m_lChildren = new ArrayList(c_ChildrenTypical);
                m_lChildren.Add(child);
            }
            else
            {
                for (int i = 0; i < m_lChildren.Count; ++i)
                {
                    if (m_lChildren[i] == child)
                        return;
                }
                m_lChildren.Add(child);
            }
        }

        public bool Equal(SecurityElement other)
        {
            if (other == null)
                return false;

            // Check if the tags are the same
            if (!string.Equals(m_strTag, other.m_strTag))
                return false;

            // Check if the text is the same
            if (!string.Equals(m_strText, other.m_strText))
                return false;

            // Check if the attributes are the same and appear in the same
            // order.
            if (m_lAttributes == null || other.m_lAttributes == null)
            {
                if (m_lAttributes != other.m_lAttributes)
                    return false;
            }
            else
            {
                int iMax = m_lAttributes.Count;
                Debug.Assert(iMax % 2 == 0, "Odd number of strings means the attr/value pairs were not added correctly");

                // Maybe we can get away by only checking the number of attributes
                if (iMax != other.m_lAttributes.Count)
                    return false;

                for (int i = 0; i < iMax; i++)
                {
                    string lhs = (string)m_lAttributes[i];
                    string rhs = (string)other.m_lAttributes[i];

                    if (!string.Equals(lhs, rhs))
                        return false;
                }
            }

            // Finally we must check the child and make sure they are
            // equal and in the same order
            if (m_lChildren == null || other.m_lChildren == null)
            {
                if (m_lChildren != other.m_lChildren)
                    return false;
            }
            else
            {
                // Maybe we can get away by only checking the number of children
                if (m_lChildren.Count != other.m_lChildren.Count)
                    return false;

                ConvertSecurityElementFactories();
                other.ConvertSecurityElementFactories();

                IEnumerator lhs = m_lChildren.GetEnumerator();
                IEnumerator rhs = other.m_lChildren.GetEnumerator();

                SecurityElement e1, e2;
                while (lhs.MoveNext())
                {
                    rhs.MoveNext();
                    e1 = (SecurityElement)lhs.Current;
                    e2 = (SecurityElement)rhs.Current;
                    if (e1 == null || !e1.Equal(e2))
                        return false;
                }
            }
            return true;
        }

        public SecurityElement Copy()
        {
            SecurityElement element = new SecurityElement(m_strTag, m_strText);
            element.m_lChildren = m_lChildren == null ? null : new ArrayList(m_lChildren);
            element.m_lAttributes = m_lAttributes == null ? null : new ArrayList(m_lAttributes);

            return element;
        }

        public static bool IsValidTag(string tag)
        {
            if (tag == null)
                return false;

            return tag.IndexOfAny(s_tagIllegalCharacters) == -1;
        }

        public static bool IsValidText(string text)
        {
            if (text == null)
                return false;

            return text.IndexOfAny(s_textIllegalCharacters) == -1;
        }

        public static bool IsValidAttributeName(string name)
        {
            return IsValidTag(name);
        }

        public static bool IsValidAttributeValue(string value)
        {
            if (value == null)
                return false;

            return value.IndexOfAny(s_valueIllegalCharacters) == -1;
        }

        private static string GetEscapeSequence(char c)
        {
            int iMax = s_escapeStringPairs.Length;
            Debug.Assert(iMax % 2 == 0, "Odd number of strings means the attr/value pairs were not added correctly");

            for (int i = 0; i < iMax; i += 2)
            {
                string strEscSeq = s_escapeStringPairs[i];
                string strEscValue = s_escapeStringPairs[i + 1];

                if (strEscSeq[0] == c)
                    return strEscValue;
            }

            Debug.Assert(false, "Unable to find escape sequence for this character");
            return c.ToString();
        }

        public static string Escape(string str)
        {
            if (str == null)
                return null;

            StringBuilder sb = null;

            int strLen = str.Length;
            int index; // Pointer into the string that indicates the location of the current '&' character
            int newIndex = 0; // Pointer into the string that indicates the start index of the "remaining" string (that still needs to be processed).

            do
            {
                index = str.IndexOfAny(s_escapeChars, newIndex);

                if (index == -1)
                {
                    if (sb == null)
                        return str;
                    else
                    {
                        sb.Append(str, newIndex, strLen - newIndex);
                        return sb.ToString();
                    }
                }
                else
                {
                    if (sb == null)
                        sb = new StringBuilder();

                    sb.Append(str, newIndex, index - newIndex);
                    sb.Append(GetEscapeSequence(str[index]));

                    newIndex = (index + 1);
                }
            }
            while (true);

            // no normal exit is possible
        }

        private static string GetUnescapeSequence(string str, int index, out int newIndex)
        {
            int maxCompareLength = str.Length - index;

            int iMax = s_escapeStringPairs.Length;
            Debug.Assert(iMax % 2 == 0, "Odd number of strings means the attr/value pairs were not added correctly");

            for (int i = 0; i < iMax; i += 2)
            {
                string strEscSeq = s_escapeStringPairs[i];
                string strEscValue = s_escapeStringPairs[i + 1];

                int length = strEscValue.Length;

                if (length <= maxCompareLength && string.Compare(strEscValue, 0, str, index, length, StringComparison.Ordinal) == 0)
                {
                    newIndex = index + strEscValue.Length;
                    return strEscSeq;
                }
            }

            newIndex = index + 1;
            return str[index].ToString();
        }

        private static string Unescape(string str)
        {
            if (str == null)
                return null;

            StringBuilder sb = null;

            int strLen = str.Length;
            int index; // Pointer into the string that indicates the location of the current '&' character
            int newIndex = 0; // Pointer into the string that indicates the start index of the "remainging" string (that still needs to be processed).

            do
            {
                index = str.IndexOf('&', newIndex);

                if (index == -1)
                {
                    if (sb == null)
                        return str;
                    else
                    {
                        sb.Append(str, newIndex, strLen - newIndex);
                        return sb.ToString();
                    }
                }
                else
                {
                    if (sb == null)
                        sb = new StringBuilder();

                    sb.Append(str, newIndex, index - newIndex);
                    sb.Append(GetUnescapeSequence(str, index, out newIndex)); // updates the newIndex too

                }
            }
            while (true);
        }

        private delegate void ToStringHelperFunc(object obj, string str);

        private static void ToStringHelperStringBuilder(object obj, string str)
        {
            ((StringBuilder)obj).Append(str);
        }

        private static void ToStringHelperStreamWriter(object obj, string str)
        {
            ((StreamWriter)obj).Write(str);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            ToString("", sb, new ToStringHelperFunc(ToStringHelperStringBuilder));

            return sb.ToString();
        }

        internal void ToWriter(StreamWriter writer)
        {
            ToString("", writer, new ToStringHelperFunc(ToStringHelperStreamWriter));
        }

        private void ToString(string indent, object obj, ToStringHelperFunc func)
        {
            func(obj, "<");

            switch (m_type)
            {
                case SecurityElementType.Format:
                    func(obj, "?");
                    break;

                case SecurityElementType.Comment:
                    func(obj, "!");
                    break;

                default:
                    break;
            }

            func(obj, m_strTag);

            // If there are any attributes, plop those in.
            if (m_lAttributes != null && m_lAttributes.Count > 0)
            {
                func(obj, " ");

                int iMax = m_lAttributes.Count;
                Debug.Assert(iMax % 2 == 0, "Odd number of strings means the attr/value pairs were not added correctly");

                for (int i = 0; i < iMax; i += 2)
                {
                    string strAttrName = (string)m_lAttributes[i];
                    string strAttrValue = (string)m_lAttributes[i + 1];

                    func(obj, strAttrName);
                    func(obj, "=\"");
                    func(obj, strAttrValue);
                    func(obj, "\"");

                    if (i != m_lAttributes.Count - 2)
                    {
                        if (m_type == SecurityElementType.Regular)
                        {
                            func(obj, Environment.NewLine);
                        }
                        else
                        {
                            func(obj, " ");
                        }
                    }
                }
            }

            if (m_strText == null && (m_lChildren == null || m_lChildren.Count == 0))
            {
                // If we are a single tag with no children, just add the end of tag text.
                switch (m_type)
                {
                    case SecurityElementType.Comment:
                        func(obj, ">");
                        break;

                    case SecurityElementType.Format:
                        func(obj, " ?>");
                        break;

                    default:
                        func(obj, "/>");
                        break;
                }
                func(obj, Environment.NewLine);
            }
            else
            {
                // Close the current tag.
                func(obj, ">");

                // Output the text
                func(obj, m_strText);

                // Output any children.
                if (m_lChildren != null)
                {
                    ConvertSecurityElementFactories();

                    func(obj, Environment.NewLine);

                    for (int i = 0; i < m_lChildren.Count; ++i)
                    {
                        ((SecurityElement)m_lChildren[i]).ToString(string.Empty, obj, func);
                    }
                }

                // Output the closing tag
                func(obj, "</");
                func(obj, m_strTag);
                func(obj, ">");
                func(obj, Environment.NewLine);
            }
        }

        public string Attribute(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // Note: we don't check for validity here because an
            // if an invalid name is passed we simply won't find it.
            if (m_lAttributes == null)
                return null;

            // Go through all the attribute and see if we know about
            // the one we are asked for
            int iMax = m_lAttributes.Count;
            Debug.Assert(iMax % 2 == 0, "Odd number of strings means the attr/value pairs were not added correctly");

            for (int i = 0; i < iMax; i += 2)
            {
                string strAttrName = (string)m_lAttributes[i];

                if (string.Equals(strAttrName, name))
                {
                    string strAttrValue = (string)m_lAttributes[i + 1];

                    return Unescape(strAttrValue);
                }
            }

            // In the case where we didn't find it, we are expected to
            // return null
            return null;
        }

        public SecurityElement SearchForChildByTag(string tag)
        {
            // Go through all the children and see if we can
            // find the one are are asked for (matching tags)
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            // Note: we don't check for a valid tag here because
            // an invalid tag simply won't be found.    
            if (m_lChildren == null)
                return null;

            IEnumerator enumerator = m_lChildren.GetEnumerator();

            while (enumerator.MoveNext())
            {
                SecurityElement current = (SecurityElement)enumerator.Current;

                if (current != null && string.Equals(current.Tag, tag))
                    return current;
            }
            return null;
        }

        internal string SearchForTextOfLocalName(string strLocalName)
        {
            // Search on each child in order and each
            // child's child, depth-first
            if (strLocalName == null)
                throw new ArgumentNullException(nameof(strLocalName));

            // Note: we don't check for a valid tag here because
            // an invalid tag simply won't be found.    
            if (m_strTag == null)
                return null;
            if (m_strTag.Equals(strLocalName) || m_strTag.EndsWith(":" + strLocalName, StringComparison.Ordinal))
                return Unescape(m_strText);
            if (m_lChildren == null)
                return null;

            IEnumerator enumerator = m_lChildren.GetEnumerator();

            while (enumerator.MoveNext())
            {
                string current = ((SecurityElement)enumerator.Current).SearchForTextOfLocalName(strLocalName);

                if (current != null)
                    return current;
            }
            return null;
        }

        public string SearchForTextOfTag(string tag)
        {
            // Search on each child in order and each
            // child's child, depth-first
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            // Note: we don't check for a valid tag here because
            // an invalid tag simply won't be found.    
            if (string.Equals(m_strTag, tag))
                return Unescape(m_strText);
            if (m_lChildren == null)
                return null;

            IEnumerator enumerator = m_lChildren.GetEnumerator();

            ConvertSecurityElementFactories();

            while (enumerator.MoveNext())
            {
                string current = ((SecurityElement)enumerator.Current).SearchForTextOfTag(tag);

                if (current != null)
                    return current;
            }
            return null;
        }

        public static SecurityElement FromString(string xml)
        {
            if (xml == null)
                throw new ArgumentNullException(nameof(xml));

            return default(SecurityElement);
        }

        //--------------- ISecurityElementFactory implementation -----------------

        SecurityElement ISecurityElementFactory.CreateSecurityElement()
        {
            return this;
        }

        string ISecurityElementFactory.GetTag()
        {
            return ((SecurityElement)this).Tag;
        }

        object ISecurityElementFactory.Copy()
        {
            return ((SecurityElement)this).Copy();
        }

        string ISecurityElementFactory.Attribute(string attributeName)
        {
            return ((SecurityElement)this).Attribute(attributeName);
        }


    }
}
