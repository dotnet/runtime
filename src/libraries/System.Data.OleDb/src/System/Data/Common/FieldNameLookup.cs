// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;

namespace System.Data.ProviderBase
{
    internal sealed class FieldNameLookup
    { // V1.2.3300

        // hashtable stores the index into the _fieldNames, match via case-sensitive
        private Hashtable? _fieldNameLookup;

        // original names for linear searches when exact matches fail
        private readonly string[] _fieldNames;

        // if _defaultLocaleID is -1 then _compareInfo is initialized with InvariantCulture CompareInfo
        // otherwise it is specified by the server? for the correct compare info
        private CompareInfo? _compareInfo;
        private readonly int _defaultLocaleID;

        public FieldNameLookup(IDataRecord reader, int defaultLocaleID)
        { // V1.2.3300

            int length = reader.FieldCount;
            string[] fieldNames = new string[length];
            for (int i = 0; i < length; ++i)
            {
                fieldNames[i] = reader.GetName(i);
                Debug.Assert(null != fieldNames[i]);
            }
            _fieldNames = fieldNames;
            _defaultLocaleID = defaultLocaleID;
        }

        public int GetOrdinal(string fieldName)
        { // V1.2.3300
            if (null == fieldName)
            {
                throw ADP.ArgumentNull("fieldName");
            }
            int index = IndexOf(fieldName);
            if (-1 == index)
            {
                throw ADP.IndexOutOfRange(fieldName);
            }
            return index;
        }

        public int IndexOfName(string fieldName)
        { // V1.2.3300
            if (null == _fieldNameLookup)
            {
                GenerateLookup();
            }
            // via case sensitive search, first match with lowest ordinal matches
            object? value = _fieldNameLookup![fieldName];
            return ((null != value) ? (int)value : -1);
        }

        public int IndexOf(string fieldName)
        { // V1.2.3300
            if (null == _fieldNameLookup)
            {
                GenerateLookup();
            }
            int index;
            object? value = _fieldNameLookup![fieldName];
            if (null != value)
            {
                // via case sensitive search, first match with lowest ordinal matches
                index = (int)value;
            }
            else
            {
                // via case insensitive search, first match with lowest ordinal matches
                index = LinearIndexOf(fieldName, CompareOptions.IgnoreCase);
                if (-1 == index)
                {
                    // do the slow search now (kana, width insensitive comparison)
                    index = LinearIndexOf(fieldName, ADP.compareOptions);
                }
            }
            return index;
        }

        private int LinearIndexOf(string fieldName, CompareOptions compareOptions)
        {
            CompareInfo? compareInfo = _compareInfo;
            if (null == compareInfo)
            {
                if (-1 != _defaultLocaleID)
                {
                    compareInfo = CompareInfo.GetCompareInfo(_defaultLocaleID);
                }
                if (null == compareInfo)
                {
                    compareInfo = CultureInfo.InvariantCulture.CompareInfo;
                }
                _compareInfo = compareInfo;
            }
            int length = _fieldNames.Length;
            for (int i = 0; i < length; ++i)
            {
                if (0 == compareInfo.Compare(fieldName, _fieldNames[i], compareOptions))
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
            for (int i = length - 1; 0 <= i; --i)
            {
                string fieldName = _fieldNames[i];
                hash[fieldName] = i;
            }
            _fieldNameLookup = hash;
        }
    }
}
