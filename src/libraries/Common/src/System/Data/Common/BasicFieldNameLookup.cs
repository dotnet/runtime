// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;

namespace System.Data.ProviderBase
{
    internal class BasicFieldNameLookup
    {
        // Dictionary stores the index into the _fieldNames, match via case-sensitive
        private Dictionary<string, int>? _fieldNameLookup;

        // original names for linear searches when exact matches fail
        private readonly string[] _fieldNames;

        // By default _compareInfo is set to InvariantCulture CompareInfo
        private CompareInfo? _compareInfo;

        public BasicFieldNameLookup(string[] fieldNames)
        {
            if (fieldNames == null)
            {
                throw ADP.ArgumentNull(nameof(fieldNames));
            }
            _fieldNames = fieldNames;
        }

        public BasicFieldNameLookup(System.Collections.ObjectModel.ReadOnlyCollection<string> columnNames)
        {
            int length = columnNames.Count;
            string[] fieldNames = new string[length];
            for (int i = 0; i < length; ++i)
            {
                fieldNames[i] = columnNames[i];
            }
            _fieldNames = fieldNames;
            GenerateLookup();
        }

        public BasicFieldNameLookup(IDataReader reader)
        {
            int length = reader.FieldCount;
            string[] fieldNames = new string[length];
            for (int i = 0; i < length; ++i)
            {
                fieldNames[i] = reader.GetName(i);
            }
            _fieldNames = fieldNames;
        }

        public int GetOrdinal(string fieldName)
        {
            if (fieldName == null)
            {
                throw ADP.ArgumentNull(nameof(fieldName));
            }
            int index = IndexOf(fieldName);
            if (index == -1)
            {
                throw ADP.IndexOutOfRange(fieldName);
            }
            return index;
        }

        public int IndexOfName(string fieldName)
        {
            if (_fieldNameLookup == null)
            {
                GenerateLookup();
            }

            int value;
            // via case sensitive search, first match with lowest ordinal matches
            return _fieldNameLookup!.TryGetValue(fieldName, out value) ? value : -1;
        }

        public int IndexOf(string fieldName)
        {
            if (_fieldNameLookup == null)
            {
                GenerateLookup();
            }
            int index;
            // via case sensitive search, first match with lowest ordinal matches
            if (!_fieldNameLookup!.TryGetValue(fieldName, out index))
            {
                // via case insensitive search, first match with lowest ordinal matches
                index = LinearIndexOf(fieldName, CompareOptions.IgnoreCase);
                if (index == -1)
                {
                    // do the slow search now (kana, width insensitive comparison)
                    index = LinearIndexOf(fieldName, ADP.DefaultCompareOptions);
                }
            }

            return index;
        }

        protected virtual CompareInfo GetCompareInfo()
        {
            return CultureInfo.InvariantCulture.CompareInfo;
        }

        private int LinearIndexOf(string fieldName, CompareOptions compareOptions)
        {
            if (_compareInfo == null)
            {
                _compareInfo = GetCompareInfo();
            }

            int length = _fieldNames.Length;
            for (int i = 0; i < length; ++i)
            {
                if (_compareInfo.Compare(fieldName, _fieldNames[i], compareOptions) == 0)
                {
                    _fieldNameLookup![fieldName] = i; // add an exact match for the future
                    return i;
                }
            }
            return -1;
        }

        // RTM common code for generating Dictionary from array of column names
        private void GenerateLookup()
        {
            int length = _fieldNames.Length;
            Dictionary<string, int> hash = new Dictionary<string, int>(length);

            // via case sensitive search, first match with lowest ordinal matches
            for (int i = length - 1; i >= 0; --i)
            {
                string fieldName = _fieldNames[i];
                hash[fieldName] = i;
            }
            _fieldNameLookup = hash;
        }
    }
}
