// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace System.Data.Common
{
    internal class DbConnectionStringBuilderDescriptor : PropertyDescriptor
    {
        internal DbConnectionStringBuilderDescriptor(string propertyName, Type componentType, Type propertyType, bool isReadOnly, Attribute[] attributes) : base(propertyName, attributes)
        {
            ComponentType = componentType;
            PropertyType = propertyType;
            IsReadOnly = isReadOnly;
        }

        internal bool RefreshOnChange { get; set; }
        public override Type ComponentType { get; }
        public override bool IsReadOnly { get; }
        public override Type PropertyType { get; }

        public override bool CanResetValue(object component)
        {
            DbConnectionStringBuilder builder = (component as DbConnectionStringBuilder);
            return ((null != builder) && builder.ShouldSerialize(DisplayName));
        }

        public override object GetValue(object component)
        {
            DbConnectionStringBuilder builder = (component as DbConnectionStringBuilder);
            if (null != builder)
            {
                object value;
                if (builder.TryGetValue(DisplayName, out value))
                {
                    return value;
                }
            }
            return null;
        }

        public override void ResetValue(object component)
        {
            DbConnectionStringBuilder builder = (component as DbConnectionStringBuilder);
            if (null != builder)
            {
                builder.Remove(DisplayName);

                if (RefreshOnChange)
                {
                    builder.ClearPropertyDescriptors();
                }
            }
        }

        public override void SetValue(object component, object value)
        {
            DbConnectionStringBuilder builder = (component as DbConnectionStringBuilder);
            if (null != builder)
            {
                // via the editor, empty string does a defacto Reset
                if ((typeof(string) == PropertyType) && string.Empty.Equals(value))
                {
                    value = null;
                }
                builder[DisplayName] = value;

                if (RefreshOnChange)
                {
                    builder.ClearPropertyDescriptors();
                }
            }
        }

        public override bool ShouldSerializeValue(object component)
        {
            DbConnectionStringBuilder builder = (component as DbConnectionStringBuilder);
            return ((null != builder) && builder.ShouldSerialize(DisplayName));
        }
    }

    [Serializable]
    internal sealed class ReadOnlyCollection<T> : ICollection, ICollection<T>
    {
        private T[] _items;

        internal ReadOnlyCollection(T[] items)
        {
            _items = items;
#if DEBUG
            for (int i = 0; i < items.Length; ++i)
            {
                Debug.Assert(null != items[i], "null item");
            }
#endif
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _items.Length);
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _items.Length);
        }


        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator<T>(_items);
        }

        public IEnumerator GetEnumerator()
        {
            return new Enumerator<T>(_items);
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return _items; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        void ICollection<T>.Add(T value)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Contains(T value)
        {
            return Array.IndexOf(_items, value) >= 0;
        }

        bool ICollection<T>.Remove(T value)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return _items.Length; }
        }

        [Serializable]
        internal struct Enumerator<K> : IEnumerator<K>, IEnumerator
        {
            // based on List<T>.Enumerator
            private K[] _items;
            private int _index;

            internal Enumerator(K[] items)
            {
                _items = items;
                _index = -1;
            }

            public void Dispose() { }

            public bool MoveNext() => (++_index < _items.Length);

            public K Current => _items[_index];

            object IEnumerator.Current => _items[_index];

            void IEnumerator.Reset()
            {
                _index = -1;
            }
        }
    }

    internal static class DbConnectionStringBuilderUtil
    {
        internal static bool ConvertToBoolean(object value)
        {
            Debug.Assert(null != value, "ConvertToBoolean(null)");
            string svalue = (value as string);
            if (null != svalue)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(svalue, "true") || StringComparer.OrdinalIgnoreCase.Equals(svalue, "yes"))
                {
                    return true;
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(svalue, "false") || StringComparer.OrdinalIgnoreCase.Equals(svalue, "no"))
                {
                    return false;
                }
                else
                {
                    string tmp = svalue.Trim();  // Remove leading & trailing white space.
                    if (StringComparer.OrdinalIgnoreCase.Equals(tmp, "true") || StringComparer.OrdinalIgnoreCase.Equals(tmp, "yes"))
                    {
                        return true;
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(tmp, "false") || StringComparer.OrdinalIgnoreCase.Equals(tmp, "no"))
                    {
                        return false;
                    }
                }
                return bool.Parse(svalue);
            }

            try
            {
                return ((IConvertible)value).ToBoolean(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(bool), e);
            }
        }

        internal static bool ConvertToIntegratedSecurity(object value)
        {
            Debug.Assert(null != value, "ConvertToIntegratedSecurity(null)");
            string svalue = (value as string);
            if (null != svalue)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(svalue, "sspi") || StringComparer.OrdinalIgnoreCase.Equals(svalue, "true") || StringComparer.OrdinalIgnoreCase.Equals(svalue, "yes"))
                {
                    return true;
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(svalue, "false") || StringComparer.OrdinalIgnoreCase.Equals(svalue, "no"))
                {
                    return false;
                }
                else
                {
                    string tmp = svalue.Trim();  // Remove leading & trailing white space.
                    if (StringComparer.OrdinalIgnoreCase.Equals(tmp, "sspi") || StringComparer.OrdinalIgnoreCase.Equals(tmp, "true") || StringComparer.OrdinalIgnoreCase.Equals(tmp, "yes"))
                    {
                        return true;
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(tmp, "false") || StringComparer.OrdinalIgnoreCase.Equals(tmp, "no"))
                    {
                        return false;
                    }
                }
                return bool.Parse(svalue);
            }

            try
            {
                return ((IConvertible)value).ToBoolean(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(bool), e);
            }
        }

        internal static int ConvertToInt32(object value)
        {
            try
            {
                return ((IConvertible)value).ToInt32(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(int), e);
            }
        }

        internal static string ConvertToString(object value)
        {
            try
            {
                return ((IConvertible)value).ToString(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(string), e);
            }
        }
    }

    internal static class DbConnectionStringDefaults
    {
        internal const int ConnectTimeout = 15;
    }

    internal static class DbConnectionStringKeywords
    {
        internal const string Driver = "Driver";
        internal const string Password = "Password";
    }

    internal static class DbConnectionStringSynonyms
    {
        internal const string Pwd = "pwd";
    }
}
