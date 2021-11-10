// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common
{
    // The string storage does not use BitArrays in DataStorage
    internal sealed class StringStorage : DataStorage
    {
        private string?[] _values = default!; // Late-initialized

        public StringStorage(DataColumn column)
        : base(column, typeof(string), string.Empty, StorageType.String)
        {
        }

        public override object Aggregate(int[] recordNos, AggregateType kind)
        {
            int i;
            switch (kind)
            {
                case AggregateType.Min:
                    int min = -1;
                    for (i = 0; i < recordNos.Length; i++)
                    {
                        if (IsNull(recordNos[i]))
                            continue;
                        min = recordNos[i];
                        break;
                    }
                    if (min >= 0)
                    {
                        for (i = i + 1; i < recordNos.Length; i++)
                        {
                            if (IsNull(recordNos[i]))
                                continue;
                            if (Compare(min, recordNos[i]) > 0)
                            {
                                min = recordNos[i];
                            }
                        }
                        return Get(min);
                    }
                    return _nullValue;

                case AggregateType.Max:
                    int max = -1;
                    for (i = 0; i < recordNos.Length; i++)
                    {
                        if (IsNull(recordNos[i]))
                            continue;
                        max = recordNos[i];
                        break;
                    }
                    if (max >= 0)
                    {
                        for (i = i + 1; i < recordNos.Length; i++)
                        {
                            if (Compare(max, recordNos[i]) < 0)
                            {
                                max = recordNos[i];
                            }
                        }
                        return Get(max);
                    }
                    return _nullValue;

                case AggregateType.Count:
                    int count = 0;
                    for (i = 0; i < recordNos.Length; i++)
                    {
                        object? value = _values[recordNos[i]];
                        if (value != null)
                            count++;
                    }
                    return count;
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            string? valueNo1 = _values[recordNo1];
            string? valueNo2 = _values[recordNo2];

            if (valueNo1 == (object?)valueNo2)
                return 0;

            if (valueNo1 == null)
                return -1;
            if (valueNo2 == null)
                return 1;

            return _table.Compare(valueNo1, valueNo2);
        }

        public override int CompareValueTo(int recordNo, object? value)
        {
            Debug.Assert(recordNo != -1, "Invalid (-1) parameter: 'recordNo'");
            Debug.Assert(null != value, "null value");
            string? valueNo1 = _values[recordNo];

            if (null == valueNo1)
            {
                if (_nullValue == value)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if (_nullValue == value)
            {
                return 1;
            }
            return _table.Compare(valueNo1, (string)value);
        }

        public override object ConvertValue(object? value)
        {
            if (_nullValue != value)
            {
                if (null != value)
                {
                    value = value.ToString()!;
                }
                else
                {
                    value = _nullValue;
                }
            }
            return value;
        }

        public override void Copy(int recordNo1, int recordNo2)
        {
            _values[recordNo2] = _values[recordNo1];
        }

        public override object Get(int recordNo)
        {
            string? value = _values[recordNo];

            if (null != value)
            {
                return value;
            }
            return _nullValue;
        }

        public override int GetStringLength(int record)
        {
            string? value = _values[record];
            return ((null != value) ? value.Length : 0);
        }

        public override bool IsNull(int record)
        {
            return (null == _values[record]);
        }

        public override void Set(int record, object value)
        {
            Debug.Assert(null != value, "null value");
            if (_nullValue == value)
            {
                _values[record] = null;
            }
            else
            {
                _values[record] = value.ToString();
            }
        }

        public override void SetCapacity(int capacity)
        {
            string[] newValues = new string[capacity];
            if (_values != null)
            {
                Array.Copy(_values, newValues, Math.Min(capacity, _values.Length));
            }
            _values = newValues;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override object ConvertXmlToObject(string s)
        {
            return s;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override string ConvertObjectToXml(object value)
        {
            return (string)value;
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new string[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            string?[] typedStore = (string?[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, IsNull(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (string?[])store;
            //           SetNullStorage(nullbits);
        }
    }
}
