// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Resources
{
    //internal sealed class FastResourceComparer : IComparer, IEqualityComparer, IComparer<String>, IEqualityComparer<String>
    internal sealed class FastResourceComparer :IComparer<string>, IEqualityComparer<string>
    {
        internal static readonly FastResourceComparer Default = new FastResourceComparer();


        public int GetHashCode(string key)
        {
            return FastResourceComparer.HashFunction(key);
        }

        // This hash function MUST be publicly documented with the resource
        // file format, AND we may NEVER change this hash function's return 
        // value (without changing the file format).
        internal static int HashFunction(string key)
        {
            // Never change this hash function.  We must standardize it so that 
            // others can read & write our .resources files.  Additionally, we
            // have a copy of it in InternalResGen as well.
            uint hash = 5381;
            for (int i = 0; i < key.Length; i++)
                hash = unchecked((hash << 5) + hash) ^ key[i];
            return (int)hash;
        }

        public int Compare(string a, string b)
        {
            return string.CompareOrdinal(a, b);
        }

        public bool Equals(string a, string b)
        {
            return string.Equals(a, b);
        }

     
    }
}

