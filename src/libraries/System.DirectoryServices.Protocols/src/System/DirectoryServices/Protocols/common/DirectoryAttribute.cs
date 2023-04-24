// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace System.DirectoryServices.Protocols
{
    public class DirectoryAttribute : CollectionBase
    {
        private string _attributeName = "";
        internal bool _isSearchResult;
        // Does not request Unicode byte order mark prefix be emitted, but turn on error detection.
        private static readonly UTF8Encoding s_utf8EncoderWithErrorDetection = new UTF8Encoding(false, true);
        // No Error detection.
        private static readonly UTF8Encoding s_encoder = new UTF8Encoding();

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
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(value);

            Name = name;
            Add(value);
        }

        public DirectoryAttribute(string name, params object[] values) : this()
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(values);

            Name = name;

            for (int i = 0; i < values.Length; i++)
            {
                Add(values[i]);
            }
        }

        public string Name
        {
            get => _attributeName;
            set => _attributeName = value ?? throw new ArgumentNullException(nameof(value));
        }

        public object[] GetValues(Type valuesType)
        {
            // Return the binary value.
            if (valuesType == typeof(byte[]))
            {
                int count = List.Count;
                byte[][] results = new byte[count][];

                for (int i = 0; i < count; i++)
                {
                    if (List[i] is string)
                    {
                        results[i] = s_encoder.GetBytes((string)List[i]);
                    }
                    else if (List[i] is byte[])
                    {
                        results[i] = (byte[])List[i];
                    }
                    else
                    {
                        throw new NotSupportedException(SR.DirectoryAttributeConversion);
                    }
                }

                return results;
            }
            // Return the string value.
            else if (valuesType == typeof(string))
            {
                int count = List.Count;
                string[] results = new string[count];

                for (int i = 0; i < count; i++)
                {
                    if (List[i] is string)
                    {
                        results[i] = (string)List[i];
                    }
                    else if (List[i] is byte[])
                    {
                        results[i] = s_encoder.GetString((byte[])List[i]);
                    }
                    else
                    {
                        throw new NotSupportedException(SR.DirectoryAttributeConversion);
                    }
                }

                return results;
            }

            throw new ArgumentException(SR.ValidDirectoryAttributeType, nameof(valuesType));
        }

        public object this[int index]
        {
            get
            {
                if (!_isSearchResult)
                {
                    return List[index];
                }

                if (List[index] is byte[] temp)
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

                // This hould not happen.
                return List[index];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                else if (!(value is string) && !(value is byte[]) && !(value is Uri))
                {
                    throw new ArgumentException(SR.ValidValueType, nameof(value));
                }

                List[index] = value;
            }
        }

        public int Add(byte[] value) => Add((object)value);

        public int Add(string value) => Add((object)value);

        public int Add(Uri value) => Add((object)value);

        internal int Add(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!(value is string) && !(value is byte[]) && !(value is Uri))
            {
                throw new ArgumentException(SR.ValidValueType, nameof(value));
            }

            return List.Add(value);
        }

        public void AddRange(object[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (!(values is string[]) && !(values is byte[][]) && !(values is Uri[]))
            {
                throw new ArgumentException(SR.ValidValuesType, nameof(values));
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                {
                    throw new ArgumentException(SR.NullValueArray, nameof(values));
                }
            }

            InnerList.AddRange(values);
        }

        public bool Contains(object value) => List.Contains(value);

        public void CopyTo(object[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(object value) => List.IndexOf(value);

        public void Insert(int index, byte[] value) => Insert(index, (object)value);

        public void Insert(int index, string value) => Insert(index, (object)value);

        public void Insert(int index, Uri value) => Insert(index, (object)value);

        private void Insert(int index, object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            List.Insert(index, value);
        }

        public void Remove(object value) => List.Remove(value);

        protected override void OnValidate(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!(value is string) && !(value is byte[]) && !(value is Uri))
            {
                throw new ArgumentException(SR.ValidValueType, nameof(value));
            }
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
            get => _attributeOperation;
            set
            {
                if (value < DirectoryAttributeOperation.Add || value > DirectoryAttributeOperation.Replace)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(DirectoryAttributeOperation));
                }

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
                {
                    throw new ArgumentNullException(nameof(attributeName));
                }

                object objectName = attributeName.ToLowerInvariant();
                return (DirectoryAttribute)InnerHashtable[objectName];
            }
        }

        public ICollection AttributeNames => Dictionary.Keys;

        public ICollection Values => Dictionary.Values;

        internal void Add(string name, DirectoryAttribute value)
        {
            Dictionary.Add(name.ToLowerInvariant(), value);
        }

        public bool Contains(string attributeName)
        {
            ArgumentNullException.ThrowIfNull(attributeName);

            object objectName = attributeName.ToLowerInvariant();
            return Dictionary.Contains(objectName);
        }

        public void CopyTo(DirectoryAttribute[] array, int index) => Dictionary.Values.CopyTo(array, index);
    }

    public class DirectoryAttributeCollection : CollectionBase
    {
        public DirectoryAttributeCollection()
        {
        }

        public DirectoryAttribute this[int index]
        {
            get => (DirectoryAttribute)List[index];
            set => List[index] = value ?? throw new ArgumentException(SR.NullDirectoryAttributeCollection);
        }

        public int Add(DirectoryAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentException(SR.NullDirectoryAttributeCollection);
            }

            return List.Add(attribute);
        }

        public void AddRange(DirectoryAttribute[] attributes)
        {
            ArgumentNullException.ThrowIfNull(attributes);

            foreach (DirectoryAttribute attribute in attributes)
            {
                if (attribute == null)
                {
                    throw new ArgumentException(SR.NullDirectoryAttributeCollection);
                }
            }

            InnerList.AddRange(attributes);
        }

        public void AddRange(DirectoryAttributeCollection attributeCollection)
        {
            ArgumentNullException.ThrowIfNull(attributeCollection);

            int currentCount = attributeCollection.Count;
            for (int i = 0; i < currentCount; i = ((i) + (1)))
            {
                Add(attributeCollection[i]);
            }
        }

        public bool Contains(DirectoryAttribute value) => List.Contains(value);

        public void CopyTo(DirectoryAttribute[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(DirectoryAttribute value) => List.IndexOf(value);

        public void Insert(int index, DirectoryAttribute value)
        {
            if (value == null)
            {
                throw new ArgumentException(SR.NullDirectoryAttributeCollection);
            }

            List.Insert(index, value);
        }

        public void Remove(DirectoryAttribute value) => List.Remove(value);

        protected override void OnValidate(object value)
        {
            if (value == null)
            {
                throw new ArgumentException(SR.NullDirectoryAttributeCollection);
            }
            if (!(value is DirectoryAttribute))
            {
                throw new ArgumentException(SR.Format(SR.InvalidValueType, nameof(DirectoryAttribute)), nameof(value));
            }
        }
    }

    public class DirectoryAttributeModificationCollection : CollectionBase
    {
        public DirectoryAttributeModificationCollection()
        {
        }

        public DirectoryAttributeModification this[int index]
        {
            get => (DirectoryAttributeModification)List[index];
            set => List[index] = value ?? throw new ArgumentException(SR.NullDirectoryAttributeCollection);
        }

        public int Add(DirectoryAttributeModification attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentException(SR.NullDirectoryAttributeCollection);
            }

            return List.Add(attribute);
        }

        public void AddRange(DirectoryAttributeModification[] attributes)
        {
            ArgumentNullException.ThrowIfNull(attributes);

            foreach (DirectoryAttributeModification attribute in attributes)
            {
                if (attribute == null)
                {
                    throw new ArgumentException(SR.NullDirectoryAttributeCollection);
                }
            }

            InnerList.AddRange(attributes);
        }

        public void AddRange(DirectoryAttributeModificationCollection attributeCollection)
        {
            ArgumentNullException.ThrowIfNull(attributeCollection);

            int currentCount = attributeCollection.Count;
            for (int i = 0; i < currentCount; i = ((i) + (1)))
            {
                Add(attributeCollection[i]);
            }
        }

        public bool Contains(DirectoryAttributeModification value) => List.Contains(value);

        public void CopyTo(DirectoryAttributeModification[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(DirectoryAttributeModification value) => List.IndexOf(value);

        public void Insert(int index, DirectoryAttributeModification value)
        {
            if (value == null)
            {
                throw new ArgumentException(SR.NullDirectoryAttributeCollection);
            }

            List.Insert(index, value);
        }

        public void Remove(DirectoryAttributeModification value) => List.Remove(value);

        protected override void OnValidate(object value)
        {
            if (value == null)
            {
                throw new ArgumentException(SR.NullDirectoryAttributeCollection);
            }
            if (!(value is DirectoryAttributeModification))
            {
                throw new ArgumentException(SR.Format(SR.InvalidValueType, nameof(DirectoryAttributeModification)), nameof(value));
            }
        }
    }
}
