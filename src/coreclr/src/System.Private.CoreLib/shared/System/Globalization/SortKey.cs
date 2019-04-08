// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Globalization
{
    /// <summary>
    /// This class implements a set of methods for retrieving
    /// </summary>
    public partial class SortKey
    {
        private readonly string _localeName;
        private readonly CompareOptions _options;
        private readonly string _string;
        private readonly byte[] _keyData;

        /// <summary>
        /// The following constructor is designed to be called from CompareInfo to get the 
        /// the sort key of specific string for synthetic culture
        /// </summary>
        internal SortKey(string localeName, string str, CompareOptions options, byte[] keyData)
        {
            _keyData = keyData;
            _localeName = localeName;
            _options = options;
            _string = str;
        }

        /// <summary>
        /// Returns the original string used to create the current instance
        /// of SortKey.
        /// </summary>
        public virtual string OriginalString => _string;

        /// <summary>
        /// Returns a byte array representing the current instance of the
        /// sort key.
        /// </summary>
        public virtual byte[] KeyData => (byte[])_keyData.Clone();

        /// <summary>
        /// Compares the two sort keys.  Returns 0 if the two sort keys are
        /// equal, a number less than 0 if sortkey1 is less than sortkey2,
        /// and a number greater than 0 if sortkey1 is greater than sortkey2.
        /// </summary>
        public static int Compare(SortKey sortkey1, SortKey sortkey2)
        {
            if (sortkey1 == null)
            {
                throw new ArgumentNullException(nameof(sortkey1));
            }
            if (sortkey2 == null)
            {
                throw new ArgumentNullException(nameof(sortkey2));
            }

            byte[] key1Data = sortkey1._keyData;
            byte[] key2Data = sortkey2._keyData;

            Debug.Assert(key1Data != null, "key1Data != null");
            Debug.Assert(key2Data != null, "key2Data != null");

            if (key1Data.Length == 0)
            {
                if (key2Data.Length == 0)
                {
                    return 0;
                }

                return -1;
            }
            if (key2Data.Length == 0)
            {
                return 1;
            }

            int compLen = (key1Data.Length < key2Data.Length) ? key1Data.Length : key2Data.Length;
            for (int i = 0; i < compLen; i++)
            {
                if (key1Data[i] > key2Data[i])
                {
                    return 1;
                }
                if (key1Data[i] < key2Data[i])
                {
                    return -1;
                }
            }

            return 0;
        }

        public override bool Equals(object? value)
        {
            return value is SortKey otherSortKey && Compare(this, otherSortKey) == 0;
        }

        public override int GetHashCode()
        {
            return CompareInfo.GetCompareInfo(_localeName).GetHashCodeOfString(_string, _options);
        }

        public override string ToString()
        {
            return "SortKey - " + _localeName + ", " + _options + ", " + _string;
        }
    }
}
