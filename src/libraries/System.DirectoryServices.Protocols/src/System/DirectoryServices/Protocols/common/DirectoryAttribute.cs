// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
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
                        throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, SR.DirectoryAttributeConversion));
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
                        throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, SR.DirectoryAttributeConversion));
                }

                return results;
            }
            else
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.ValidDirectoryAttributeType), "valuesType");
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
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.ValidValueType), "value");
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
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.ValidValueType), "value");

            return List.Add(value);
        }

        public void AddRange(object[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            if (!(values is string[]) && !(values is byte[][]) && !(values is Uri[]))
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.ValidValuesType), "values");

            for (int i = 0; i < values.Length; i++)
                if (values[i] == null)
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NullValueArray), "values");

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
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.ValidValueType), "value");
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
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NullDirectoryAttributeCollection));

                List[index] = value;
            }
        }

        public int Add(DirectoryAttribute attribute)
        {
            if (attribute == null)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NullDirectoryAttributeCollection));

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
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NullDirectoryAttributeCollection));
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
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NullDirectoryAttributeCollection));

            List.Insert(index, value);
        }

        public void Remove(DirectoryAttribute value)
        {
            List.Remove(value);
        }

        protected override void OnValidate(Object value)
        {
            if (value == null)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NullDirectoryAttributeCollection));

            if (!(value is DirectoryAttribute))
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.InvalidValueType, "DirectoryAttribute"), "value");
        }
    }

    public class DirectoryAttributeModificationCollection : CollectionBase
    {
        public DirectoryAttributeModificationCollection()
        {
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
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NullDirectoryAttributeCollection));

                List[index] = value;
            }
        }

        public int Add(DirectoryAttributeModification attribute)
        {
            if (attribute == null)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NullDirectoryAttributeCollection));

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
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NullDirectoryAttributeCollection));
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
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NullDirectoryAttributeCollection));

            List.Insert(index, value);
        }

        public void Remove(DirectoryAttributeModification value)
        {
            List.Remove(value);
        }

        protected override void OnValidate(Object value)
        {
            if (value == null)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.NullDirectoryAttributeCollection));

            if (!(value is DirectoryAttributeModification))
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.InvalidValueType, "DirectoryAttributeModification"), "value");
        }
    }
}

