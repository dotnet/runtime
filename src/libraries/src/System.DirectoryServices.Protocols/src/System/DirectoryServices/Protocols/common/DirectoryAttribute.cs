// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Xml;
    using System.Collections;
    using System.ComponentModel;
    using System.Globalization;
    using System.Diagnostics;
    using System.Text;

    public class DirectoryAttribute : CollectionBase
    {
        private string _attributeName = "";
        internal bool isSearchResult = false;
        // does not request Unicode byte order mark prefix be emitted, but turn on error detection
        private static UTF8Encoding s_utf8EncoderWithErrorDetection = new UTF8Encoding(false, true);
        // with no error detection on
        private static UTF8Encoding s_encoder = new UTF8Encoding();

        public DirectoryAttribute()
        {
            Utility.CheckOSVersion();
        }

        public DirectoryAttribute(string name, string value) : this(name, (object)value)
        {
        }

        public DirectoryAttribute(string name, byte[] value) : this(name, (object)value)
        {
        }

        public DirectoryAttribute(string name, Uri value) : this(name, (object)value)
        {
        }

        internal DirectoryAttribute(string name, object value) : this()
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (value == null)
                throw new ArgumentNullException("value");

            // set the name
            Name = name;

            // set the value;
            Add(value);
        }

        public DirectoryAttribute(string name, params object[] values) : this()
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (values == null)
                throw new ArgumentNullException("values");

            // set the name
            Name = name;

            // set the value;
            for (int i = 0; i < values.Length; i++)
            {
                Add(values[i]);
            }
        }

        internal DirectoryAttribute(XmlElement node)
        {
            // retrieve attribute name
            string primaryXPath = "@dsml:name";
            string secondaryXPath = "@name";
            XmlNamespaceManager dsmlNS = NamespaceUtils.GetDsmlNamespaceManager();

            XmlAttribute attrName = (XmlAttribute)node.SelectSingleNode(primaryXPath, dsmlNS);

            if (attrName == null)
            {
                // try it without the namespace qualifier, in case the sender omitted it
                attrName = (XmlAttribute)node.SelectSingleNode(secondaryXPath, dsmlNS);

                if (attrName == null)
                {
                    // the element doesn't have a associated dn
                    throw new DsmlInvalidDocumentException(Res.GetString(Res.MissingSearchResultEntryAttributeName));
                }

                _attributeName = attrName.Value;
            }
            else
            {
                _attributeName = attrName.Value;
            }

            // retrieve attribute value
            XmlNodeList nodeList = node.SelectNodes("dsml:value", dsmlNS);

            if (nodeList.Count != 0)
            {
                foreach (XmlNode valueNode in nodeList)
                {
                    Debug.Assert(valueNode is XmlElement);

                    XmlAttribute valueType = (XmlAttribute)valueNode.SelectSingleNode("@xsi:type", dsmlNS);

                    if (valueType == null)
                    {
                        // value type is string
                        Add(valueNode.InnerText);
                    }
                    else
                    {
                        // value type is string
                        if (string.Compare(valueType.Value, "xsd:string", StringComparison.OrdinalIgnoreCase) == 0)
                            Add(valueNode.InnerText);
                        else if (string.Compare(valueType.Value, "xsd:base64Binary", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            string base64EncodedValue = valueNode.InnerText;
                            byte[] binaryValue;
                            try
                            {
                                binaryValue = System.Convert.FromBase64String(base64EncodedValue);
                            }
                            catch (FormatException)
                            {
                                // server returned invalid base64
                                throw new DsmlInvalidDocumentException(Res.GetString(Res.BadBase64Value));
                            }

                            Add(binaryValue);
                        }
                        else if (string.Compare(valueType.Value, "xsd:anyURI", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            Uri uri = new Uri(valueNode.InnerText);
                            Add(uri);
                        }
                    }
                }
            }
        }

        public string Name
        {
            get
            {
                return _attributeName;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _attributeName = value;
            }
        }

        public object[] GetValues(Type valuesType)
        {
            // user wants to return binary value
            if (valuesType == typeof(byte[]))
            {
                int count = List.Count;
                byte[][] results = new byte[count][];

                for (int i = 0; i < count; i++)
                {
                    if (List[i] is string)
                        results[i] = s_encoder.GetBytes((string)List[i]);
                    else if (List[i] is byte[])
                    {
                        results[i] = (byte[])List[i];
                    }
                    else
                        throw new NotSupportedException(Res.GetString(Res.DirectoryAttributeConversion));
                }

                return results;
            }
            // user wants to return string value
            else if (valuesType == typeof(string))
            {
                int count = List.Count;
                string[] results = new string[count];

                for (int i = 0; i < count; i++)
                {
                    if (List[i] is string)
                        results[i] = (string)List[i];
                    else if (List[i] is byte[])
                        results[i] = s_encoder.GetString((byte[])List[i]);
                    else
                        throw new NotSupportedException(Res.GetString(Res.DirectoryAttributeConversion));
                }

                return results;
            }
            else
                throw new ArgumentException(Res.GetString(Res.ValidDirectoryAttributeType), "valuesType");
        }

        public object this[int index]
        {
            get
            {
                if (!isSearchResult)
                {
                    return List[index];
                }
                else
                {
                    byte[] temp = List[index] as byte[];
                    if (temp != null)
                    {
                        try
                        {
                            return s_utf8EncoderWithErrorDetection.GetString(temp);
                        }
                        catch (ArgumentException)
                        {
                            return List[index];
                        }
                    }
                    else
                    {
                        // should not happen
                        return List[index];
                    }
                }
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                else if ((value is string) || (value is byte[]) || (value is Uri))
                    List[index] = value;
                else
                    throw new ArgumentException(Res.GetString(Res.ValidValueType), "value");
            }
        }

        public int Add(byte[] value)
        {
            return Add((object)value);
        }

        public int Add(string value)
        {
            return Add((object)value);
        }

        public int Add(Uri value)
        {
            return Add((object)value);
        }

        internal int Add(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (!(value is string) && !(value is byte[]) && !(value is Uri))
                throw new ArgumentException(Res.GetString(Res.ValidValueType), "value");

            return List.Add(value);
        }

        public void AddRange(object[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            if (!(values is string[]) && !(values is byte[][]) && !(values is Uri[]))
                throw new ArgumentException(Res.GetString(Res.ValidValuesType), "values");

            for (int i = 0; i < values.Length; i++)
                if (values[i] == null)
                    throw new ArgumentException(Res.GetString(Res.NullValueArray), "values");

            InnerList.AddRange(values);
        }

        public bool Contains(object value)
        {
            return List.Contains(value);
        }

        public void CopyTo(object[] array, int index)
        {
            List.CopyTo(array, index);
        }

        public int IndexOf(object value)
        {
            return List.IndexOf(value);
        }

        public void Insert(int index, byte[] value)
        {
            Insert(index, (object)value);
        }

        public void Insert(int index, string value)
        {
            Insert(index, (object)value);
        }

        public void Insert(int index, Uri value)
        {
            Insert(index, (object)value);
        }

        private void Insert(int index, object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            List.Insert(index, value);
        }

        public void Remove(object value)
        {
            List.Remove(value);
        }

        protected override void OnValidate(Object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (!(value is string) && !(value is byte[]) && !(value is Uri))
                throw new ArgumentException(Res.GetString(Res.ValidValueType), "value");
        }

        internal void ToXmlNodeCommon(XmlElement elemBase)
        {
            XmlDocument doc = elemBase.OwnerDocument;

            XmlAttribute attrName = doc.CreateAttribute("name", null);
            attrName.InnerText = Name;
            elemBase.Attributes.Append(attrName);

            // now create <value> child elements for each attribute value
            if (Count != 0)
            {
                foreach (object o in InnerList)
                {
                    XmlElement elemValue = doc.CreateElement("value", DsmlConstants.DsmlUri);

                    // Do the proper encoding, based on whether the object is a byte array
                    // or something else (encoded as a string)
                    if (o is byte[])
                    {
                        // base64 binary encoding
                        elemValue.InnerText = System.Convert.ToBase64String((byte[])o);

                        // append the "xsi:type=xsd:base64Binary" tag
                        XmlAttribute attrXsiType = doc.CreateAttribute("xsi:type", DsmlConstants.XsiUri);
                        attrXsiType.InnerText = "xsd:base64Binary";
                        elemValue.Attributes.Append(attrXsiType);
                    }
                    else if (o is Uri)
                    {
                        elemValue.InnerText = o.ToString();

                        // append the "xsi:type=xsd:anyURI" tag
                        XmlAttribute attrXsiType = doc.CreateAttribute("xsi:type", DsmlConstants.XsiUri);
                        attrXsiType.InnerText = "xsd:anyURI";
                        elemValue.Attributes.Append(attrXsiType);
                    }
                    else
                    {
                        // string encoding
                        elemValue.InnerText = o.ToString();

                        // append the space tag
                        if (elemValue.InnerText.StartsWith(" ", StringComparison.Ordinal) || elemValue.InnerText.EndsWith(" ", StringComparison.Ordinal))
                        {
                            XmlAttribute attrXsiType = doc.CreateAttribute("xml:space");
                            attrXsiType.InnerText = "preserve";
                            elemValue.Attributes.Append(attrXsiType);
                        }
                    }

                    elemBase.AppendChild(elemValue);
                }
            }
        }

        internal XmlElement ToXmlNode(XmlDocument doc, string elementName)
        {
            // create the <attr> element
            XmlElement elemAttr = doc.CreateElement(elementName, DsmlConstants.DsmlUri);

            // attach the "name" attribute and the <value> child elements
            ToXmlNodeCommon(elemAttr);

            return elemAttr;
        }
    }

    public class DirectoryAttributeModification : DirectoryAttribute
    {
        private DirectoryAttributeOperation _attributeOperation = DirectoryAttributeOperation.Replace;

        public DirectoryAttributeModification()
        {
        }

        public DirectoryAttributeOperation Operation
        {
            get
            {
                return _attributeOperation;
            }
            set
            {
                if (value < DirectoryAttributeOperation.Add || value > DirectoryAttributeOperation.Replace)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(DirectoryAttributeOperation));

                _attributeOperation = value;
            }
        }

        internal XmlElement ToXmlNode(XmlDocument doc)
        {
            // create the <modification> element
            XmlElement elemAttrMod = doc.CreateElement("modification", DsmlConstants.DsmlUri);

            // attach the "name" attribute and the <value> child elements
            ToXmlNodeCommon(elemAttrMod);

            // attach the "operation" attribute
            XmlAttribute attrName = doc.CreateAttribute("operation", null);

            switch (Operation)
            {
                case DirectoryAttributeOperation.Replace:
                    attrName.InnerText = "replace";
                    break;

                case DirectoryAttributeOperation.Add:
                    attrName.InnerText = "add";
                    break;

                case DirectoryAttributeOperation.Delete:
                    attrName.InnerText = "delete";
                    break;

                default:
                    throw new InvalidEnumArgumentException("Operation", (int)Operation, typeof(DirectoryAttributeOperation));
            }

            elemAttrMod.Attributes.Append(attrName);

            return elemAttrMod;
        }
    }

    public class SearchResultAttributeCollection : DictionaryBase
    {
        internal SearchResultAttributeCollection() { }

        public DirectoryAttribute this[string attributeName]
        {
            get
            {
                if (attributeName == null)
                    throw new ArgumentNullException("attributeName");

                object objectName = attributeName.ToLower(CultureInfo.InvariantCulture);
                return (DirectoryAttribute)InnerHashtable[objectName];
            }
        }

        public ICollection AttributeNames
        {
            get { return Dictionary.Keys; }
        }

        public ICollection Values
        {
            get
            {
                return Dictionary.Values;
            }
        }

        internal void Add(string name, DirectoryAttribute value)
        {
            Dictionary.Add(name.ToLower(CultureInfo.InvariantCulture), value);
        }

        public bool Contains(string attributeName)
        {
            if (attributeName == null)
                throw new ArgumentNullException("attributeName");

            object objectName = attributeName.ToLower(CultureInfo.InvariantCulture);
            return Dictionary.Contains(objectName);
        }

        public void CopyTo(DirectoryAttribute[] array, int index)
        {
            Dictionary.Values.CopyTo((Array)array, index);
        }
    }

    public class DirectoryAttributeCollection : CollectionBase
    {
        public DirectoryAttributeCollection()
        {
            Utility.CheckOSVersion();
        }

        public DirectoryAttribute this[int index]
        {
            get
            {
                return (DirectoryAttribute)List[index];
            }
            set
            {
                if (value == null)
                    throw new ArgumentException(Res.GetString(Res.NullDirectoryAttributeCollection));

                List[index] = value;
            }
        }

        public int Add(DirectoryAttribute attribute)
        {
            if (attribute == null)
                throw new ArgumentException(Res.GetString(Res.NullDirectoryAttributeCollection));

            return List.Add(attribute);
        }

        public void AddRange(DirectoryAttribute[] attributes)
        {
            if (attributes == null)
                throw new ArgumentNullException("attributes");

            foreach (DirectoryAttribute c in attributes)
            {
                if (c == null)
                {
                    throw new ArgumentException(Res.GetString(Res.NullDirectoryAttributeCollection));
                }
            }

            InnerList.AddRange(attributes);
        }

        public void AddRange(DirectoryAttributeCollection attributeCollection)
        {
            if (attributeCollection == null)
            {
                throw new ArgumentNullException("attributeCollection");
            }
            int currentCount = attributeCollection.Count;
            for (int i = 0; i < currentCount; i = ((i) + (1)))
            {
                this.Add(attributeCollection[i]);
            }
        }

        public bool Contains(DirectoryAttribute value)
        {
            return List.Contains(value);
        }

        public void CopyTo(DirectoryAttribute[] array, int index)
        {
            List.CopyTo(array, index);
        }

        public int IndexOf(DirectoryAttribute value)
        {
            return List.IndexOf(value);
        }

        public void Insert(int index, DirectoryAttribute value)
        {
            if (value == null)
                throw new ArgumentException(Res.GetString(Res.NullDirectoryAttributeCollection));

            List.Insert(index, value);
        }

        public void Remove(DirectoryAttribute value)
        {
            List.Remove(value);
        }

        protected override void OnValidate(Object value)
        {
            if (value == null)
                throw new ArgumentException(Res.GetString(Res.NullDirectoryAttributeCollection));

            if (!(value is DirectoryAttribute))
                throw new ArgumentException(Res.GetString(Res.InvalidValueType, "DirectoryAttribute"), "value");
        }
    }

    public class DirectoryAttributeModificationCollection : CollectionBase
    {
        public DirectoryAttributeModificationCollection()
        {
            Utility.CheckOSVersion();
        }

        public DirectoryAttributeModification this[int index]
        {
            get
            {
                return (DirectoryAttributeModification)List[index];
            }
            set
            {
                if (value == null)
                    throw new ArgumentException(Res.GetString(Res.NullDirectoryAttributeCollection));

                List[index] = value;
            }
        }

        public int Add(DirectoryAttributeModification attribute)
        {
            if (attribute == null)
                throw new ArgumentException(Res.GetString(Res.NullDirectoryAttributeCollection));

            return List.Add(attribute);
        }

        public void AddRange(DirectoryAttributeModification[] attributes)
        {
            if (attributes == null)
                throw new ArgumentNullException("attributes");

            foreach (DirectoryAttributeModification c in attributes)
            {
                if (c == null)
                {
                    throw new ArgumentException(Res.GetString(Res.NullDirectoryAttributeCollection));
                }
            }

            InnerList.AddRange(attributes);
        }

        public void AddRange(DirectoryAttributeModificationCollection attributeCollection)
        {
            if (attributeCollection == null)
            {
                throw new ArgumentNullException("attributeCollection");
            }
            int currentCount = attributeCollection.Count;
            for (int i = 0; i < currentCount; i = ((i) + (1)))
            {
                this.Add(attributeCollection[i]);
            }
        }

        public bool Contains(DirectoryAttributeModification value)
        {
            return List.Contains(value);
        }

        public void CopyTo(DirectoryAttributeModification[] array, int index)
        {
            List.CopyTo(array, index);
        }

        public int IndexOf(DirectoryAttributeModification value)
        {
            return List.IndexOf(value);
        }

        public void Insert(int index, DirectoryAttributeModification value)
        {
            if (value == null)
                throw new ArgumentException(Res.GetString(Res.NullDirectoryAttributeCollection));

            List.Insert(index, value);
        }

        public void Remove(DirectoryAttributeModification value)
        {
            List.Remove(value);
        }

        protected override void OnValidate(Object value)
        {
            if (value == null)
                throw new ArgumentException(Res.GetString(Res.NullDirectoryAttributeCollection));

            if (!(value is DirectoryAttributeModification))
                throw new ArgumentException(Res.GetString(Res.InvalidValueType, "DirectoryAttributeModification"), "value");
        }
    }
}

