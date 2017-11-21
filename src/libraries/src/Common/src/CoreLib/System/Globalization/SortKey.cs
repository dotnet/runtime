// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////
//
//
//  Purpose:  This class implements a set of methods for retrieving
//            sort key information.
//
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace System.Globalization
{
    public partial class SortKey
    {
        //--------------------------------------------------------------------//
        //                        Internal Information                        //
        //--------------------------------------------------------------------//

        internal string _localeName;       // locale identifier

        internal CompareOptions _options;  // options
        internal string _string;         // original string
        internal byte[] _keyData;        // sortkey data

        //
        // The following constructor is designed to be called from CompareInfo to get the 
        // the sort key of specific string for synthetic culture
        //
        internal SortKey(String localeName, String str, CompareOptions options, byte[] keyData)
        {
            _keyData = keyData;
            _localeName = localeName;
            _options = options;
            _string = str;
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  GetOriginalString
        //
        //  Returns the original string used to create the current instance
        //  of SortKey.
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String OriginalString
        {
            get
            {
                return (_string);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  GetKeyData
        //
        //  Returns a byte array representing the current instance of the
        //  sort key.
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual byte[] KeyData
        {
            get
            {
                return (byte[])(_keyData.Clone());
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  Compare
        //
        //  Compares the two sort keys.  Returns 0 if the two sort keys are
        //  equal, a number less than 0 if sortkey1 is less than sortkey2,
        //  and a number greater than 0 if sortkey1 is greater than sortkey2.
        //
        ////////////////////////////////////////////////////////////////////////
        public static int Compare(SortKey sortkey1, SortKey sortkey2)
        {
            if (sortkey1 == null || sortkey2 == null)
            {
                throw new ArgumentNullException((sortkey1 == null ? nameof(sortkey1) : nameof(sortkey2)));
            }

            byte[] key1Data = sortkey1._keyData;
            byte[] key2Data = sortkey2._keyData;

            Debug.Assert(key1Data != null, "key1Data != null");
            Debug.Assert(key2Data != null, "key2Data != null");

            if (key1Data.Length == 0)
            {
                if (key2Data.Length == 0)
                {
                    return (0);
                }
                return (-1);
            }
            if (key2Data.Length == 0)
            {
                return (1);
            }

            int compLen = (key1Data.Length < key2Data.Length) ? key1Data.Length : key2Data.Length;

            for (int i = 0; i < compLen; i++)
            {
                if (key1Data[i] > key2Data[i])
                {
                    return (1);
                }
                if (key1Data[i] < key2Data[i])
                {
                    return (-1);
                }
            }

            return 0;
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  Equals
        //
        //  Implements Object.Equals().  Returns a boolean indicating whether
        //  or not object refers to the same SortKey as the current instance.
        //
        ////////////////////////////////////////////////////////////////////////
        public override bool Equals(Object value)
        {
            SortKey that = value as SortKey;

            if (that != null)
            {
                return Compare(this, that) == 0;
            }

            return (false);
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  GetHashCode
        //
        //  Implements Object.GetHashCode().  Returns the hash code for the
        //  SortKey.  The hash code is guaranteed to be the same for
        //  SortKey A and B where A.Equals(B) is true.
        //
        ////////////////////////////////////////////////////////////////////////
        public override int GetHashCode()
        {
            return (CompareInfo.GetCompareInfo(_localeName).GetHashCodeOfString(_string, _options));
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  ToString
        //
        //  Implements Object.ToString().  Returns a string describing the
        //  SortKey.
        //
        ////////////////////////////////////////////////////////////////////////
        public override String ToString()
        {
            return ("SortKey - " + _localeName + ", " + _options + ", " + _string);
        }
    }
}
