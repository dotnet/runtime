// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;

namespace System.Data.Odbc
{
    public sealed partial class OdbcParameterCollection : DbParameterCollection
    {
        private List<OdbcParameter>? _items;

        public override int Count
        {
            get
            {
                return ((null != _items) ? _items.Count : 0);
            }
        }

        private List<OdbcParameter> InnerList
        {
            get
            {
                List<OdbcParameter>? items = _items;

                if (null == items)
                {
                    items = new List<OdbcParameter>();
                    _items = items;
                }
                return items;
            }
        }

        public override bool IsFixedSize
        {
            get
            {
                return ((System.Collections.IList)InnerList).IsFixedSize;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return ((System.Collections.IList)InnerList).IsReadOnly;
            }
        }

        public override bool IsSynchronized
        {
            get
            {
                return ((System.Collections.ICollection)InnerList).IsSynchronized;
            }
        }

        public override object SyncRoot
        {
            get
            {
                return ((System.Collections.ICollection)InnerList).SyncRoot;
            }
        }

        public override int Add(object value)
        {
            OnChange();
            ValidateType(value);
            Validate(-1, value);
            InnerList.Add((OdbcParameter)value);
            return Count - 1;
        }

        public override void AddRange(System.Array values)
        {
            OnChange();
            if (null == values)
            {
                throw ADP.ArgumentNull(nameof(values));
            }
            foreach (object value in values)
            {
                ValidateType(value);
            }
            foreach (OdbcParameter value in values)
            {
                Validate(-1, value);
                InnerList.Add((OdbcParameter)value);
            }
        }

        private int CheckName(string parameterName)
        {
            int index = IndexOf(parameterName);
            if (index < 0)
            {
                throw ADP.ParametersSourceIndex(parameterName, this, s_itemType);
            }
            return index;
        }

        public override void Clear()
        {
            OnChange();
            List<OdbcParameter> items = InnerList;

            if (null != items)
            {
                foreach (OdbcParameter item in items)
                {
                    item.ResetParent();
                }
                items.Clear();
            }
        }

        public override bool Contains(object value)
        {
            return (-1 != IndexOf(value));
        }

        public override void CopyTo(Array array, int index)
        {
            ((System.Collections.ICollection)InnerList).CopyTo(array, index);
        }

        public override System.Collections.IEnumerator GetEnumerator()
        {
            return ((System.Collections.ICollection)InnerList).GetEnumerator();
        }

        protected override DbParameter GetParameter(int index)
        {
            RangeCheck(index);
            return InnerList[index];
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            int index = IndexOf(parameterName);
            if (index < 0)
            {
                throw ADP.ParametersSourceIndex(parameterName, this, s_itemType);
            }
            return InnerList[index];
        }

        private static int IndexOf(System.Collections.IEnumerable items, string parameterName)
        {
            if (null != items)
            {
                int i = 0;

                foreach (OdbcParameter parameter in items)
                {
                    if (parameterName == parameter.ParameterName)
                    {
                        return i;
                    }
                    ++i;
                }
                i = 0;

                foreach (OdbcParameter parameter in items)
                {
                    if (0 == ADP.DstCompare(parameterName, parameter.ParameterName))
                    {
                        return i;
                    }
                    ++i;
                }
            }
            return -1;
        }

        public override int IndexOf(string parameterName)
        {
            return IndexOf(InnerList, parameterName);
        }

        public override int IndexOf(object value)
        {
            if (null != value)
            {
                ValidateType(value);

                List<OdbcParameter> items = InnerList;

                if (null != items)
                {
                    int count = items.Count;

                    for (int i = 0; i < count; i++)
                    {
                        if (value == items[i])
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        public override void Insert(int index, object value)
        {
            OnChange();
            ValidateType(value);
            Validate(-1, (OdbcParameter)value);
            InnerList.Insert(index, (OdbcParameter)value);
        }

        private void RangeCheck(int index)
        {
            if ((index < 0) || (Count <= index))
            {
                throw ADP.ParametersMappingIndex(index, this);
            }
        }

        public override void Remove(object value)
        {
            OnChange();
            ValidateType(value);
            int index = IndexOf(value);
            if (-1 != index)
            {
                RemoveIndex(index);
            }
            else if (this != ((OdbcParameter)value).CompareExchangeParent(null, this))
            {
                throw ADP.CollectionRemoveInvalidObject(s_itemType, this);
            }
        }

        public override void RemoveAt(int index)
        {
            OnChange();
            RangeCheck(index);
            RemoveIndex(index);
        }

        public override void RemoveAt(string parameterName)
        {
            OnChange();
            int index = CheckName(parameterName);
            RemoveIndex(index);
        }

        private void RemoveIndex(int index)
        {
            List<OdbcParameter> items = InnerList;
            Debug.Assert((null != items) && (0 <= index) && (index < Count), "RemoveIndex, invalid");
            OdbcParameter item = items[index];
            items.RemoveAt(index);
            item.ResetParent();
        }

        private void Replace(int index, object newValue)
        {
            List<OdbcParameter> items = InnerList;
            Debug.Assert((null != items) && (0 <= index) && (index < Count), "Replace Index invalid");
            ValidateType(newValue);
            Validate(index, newValue);
            OdbcParameter item = items[index];
            items[index] = (OdbcParameter)newValue;
            item.ResetParent();
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            OnChange();
            RangeCheck(index);
            Replace(index, value);
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            OnChange();
            int index = IndexOf(parameterName);
            if (index < 0)
            {
                throw ADP.ParametersSourceIndex(parameterName, this, s_itemType);
            }
            Replace(index, value);
        }

        private void Validate(int index, object value)
        {
            if (null == value)
            {
                throw ADP.ParameterNull(nameof(value), this, s_itemType);
            }

            object? parent = ((OdbcParameter)value).CompareExchangeParent(this, null);
            if (null != parent)
            {
                if (this != parent)
                {
                    throw ADP.ParametersIsNotParent(s_itemType, this);
                }
                if (index != IndexOf(value))
                {
                    throw ADP.ParametersIsParent(s_itemType, this);
                }
            }

            string name = ((OdbcParameter)value).ParameterName;
            if (0 == name.Length)
            {
                index = 1;
                do
                {
                    name = ADP.Parameter + index.ToString(CultureInfo.CurrentCulture);
                    index++;
                } while (-1 != IndexOf(name));
                ((OdbcParameter)value).ParameterName = name;
            }
        }

        private void ValidateType(object value)
        {
            if (null == value)
            {
                throw ADP.ParameterNull(nameof(value), this, s_itemType);
            }
            else if (!s_itemType.IsInstanceOfType(value))
            {
                throw ADP.InvalidParameterType(this, s_itemType, value);
            }
        }
    };
}
