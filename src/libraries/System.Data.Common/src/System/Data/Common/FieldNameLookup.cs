// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;

namespace System.Data.ProviderBase
{
    internal sealed class FieldNameLookup
    {
        // hashtable stores the index into the _fieldNames, match via case-sensitive
        private Hashtable? _fieldNameLookup;

        // original names for linear searches when exact matches fail
        private readonly string[] _fieldNames;

        // if _defaultLocaleID is -1 then _compareInfo is initialized with InvariantCulture CompareInfo
        // otherwise it is specified by the server? for the correct compare info
        private CompareInfo? _compareInfo;
        private readonly int _defaultLocaleID;

        public FieldNameLookup(IDataRecord reader, int defaultLocaleID)
        {
            int length = reader.FieldCount;
            string[] fieldNames = new string[length];
            for (int i = 0; i < length; ++i)
            {
                fieldNames[i] = reader.GetName(i);
                Debug.Assert(fieldNames[i] != null);
            }
            _fieldNames = fieldNames;
            _defaultLocaleID = defaultLocaleID;
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

        public int IndexOf(string fieldName)
        {
            if (_fieldNameLookup == null)
            {
                GenerateLookup();
            }
            int index;
            object? value = _fieldNameLookup![fieldName];
            if (value != null)
            {
                // via case sensitive search, first match with lowest ordinal matches
                index = (int)value;
            }
            else
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

        private int LinearIndexOf(string fieldName, CompareOptions compareOptions)
        {
            CompareInfo? compareInfo = _compareInfo;
            if (compareInfo == null)
            {
                if (_defaultLocaleID != -1)
                {
                    compareInfo = CompareInfo.GetCompareInfo(_defaultLocaleID);
                }
                if (compareInfo == null)
                {
                    compareInfo = CultureInfo.InvariantCulture.CompareInfo;
                }
                _compareInfo = compareInfo;
            }
            int length = _fieldNames.Length;
            for (int i = 0; i < length; ++i)
            {
                if (compareInfo.Compare(fieldName, _fieldNames[i], compareOptions) == 0)
                {
                    _fieldNameLookup![fieldName] = i; // add an exact match for the future
                    return i;
                }
            }
            return -1;
        }

        // RTM common code for generating Hashtable from array of column names
        private void GenerateLookup()
        {
            int length = _fieldNames.Length;
            Hashtable hash = new Hashtable(length);

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
