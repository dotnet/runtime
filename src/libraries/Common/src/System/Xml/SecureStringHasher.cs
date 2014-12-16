// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Xml
{
    // SecureStringHasher is a hash code provider for strings. The hash codes calculation starts with a seed (hasCodeRandomizer) which is usually
    // different for each instance of SecureStringHasher. Since the hash code depend on the seed, the chance of hashtable DoS attack in case when 
    // someone passes in lots of strings that hash to the same hash code is greatly reduced.
    // The SecureStringHasher implements IEqualityComparer for strings and therefore can be used in generic IDictionary.
    internal class SecureStringHasher : IEqualityComparer<String>
    {
        private int _hashCodeRandomizer;

        public SecureStringHasher()
        {
            _hashCodeRandomizer = Environment.TickCount;
        }

#if false // This is here only for debugging of hashing issues
        public SecureStringHasher( int hashCodeRandomizer ) {
            this.hashCodeRandomizer = hashCodeRandomizer;
        }
#endif

        public bool Equals(String x, String y)
        {
            return String.Equals(x, y, StringComparison.Ordinal);
        }

        public int GetHashCode(String key)
        {
            int hashCode = _hashCodeRandomizer;
            // use key.Length to eliminate the rangecheck
            for (int i = 0; i < key.Length; i++)
            {
                hashCode += (hashCode << 7) ^ key[i];
            }
            // mix it a bit more
            hashCode -= hashCode >> 17;
            hashCode -= hashCode >> 11;
            hashCode -= hashCode >> 5;
            return hashCode;
        }
    }
}