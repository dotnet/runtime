// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.Interop
{
    /// <summary>
    /// Exposes the hashing utilities from Roslyn
    /// </summary>
    public class HashCode
    {
        public static int Combine<T1, T2>(T1 t1, T2 t2)
        {
            int hash1 = t1 != null ? t1.GetHashCode() : 0;
            int hash2 = t2 != null ? t2.GetHashCode() : 0;
            int combinedHash = Hash.Combine(hash1, hash2);
            return combinedHash;
        }

        public static int Combine<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
        {
            int hash1 = t1 != null ? t1.GetHashCode() : 0;
            int hash2 = t2 != null ? t2.GetHashCode() : 0;
            int hash3 = t3 != null ? t3.GetHashCode() : 0;
            int combinedHash = Hash.Combine(hash1, hash2);
            combinedHash = Hash.Combine(combinedHash, hash3);
            return combinedHash;
        }

        public static int Combine<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4)
        {
            int hash1 = t1 != null ? t1.GetHashCode() : 0;
            int hash2 = t2 != null ? t2.GetHashCode() : 0;
            int hash3 = t3 != null ? t3.GetHashCode() : 0;
            int hash4 = t4 != null ? t4.GetHashCode() : 0;
            int combinedHash = Hash.Combine(hash1, hash2);
            combinedHash = Hash.Combine(combinedHash, hash3);
            combinedHash = Hash.Combine(combinedHash, hash4);
            return combinedHash;
        }

        public static int Combine<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            int hash1 = t1 != null ? t1.GetHashCode() : 0;
            int hash2 = t2 != null ? t2.GetHashCode() : 0;
            int hash3 = t3 != null ? t3.GetHashCode() : 0;
            int hash4 = t4 != null ? t4.GetHashCode() : 0;
            int hash5 = t5 != null ? t5.GetHashCode() : 0;
            int combinedHash = Hash.Combine(hash1, hash2);
            combinedHash = Hash.Combine(combinedHash, hash3);
            combinedHash = Hash.Combine(combinedHash, hash4);
            combinedHash = Hash.Combine(combinedHash, hash5);
            return combinedHash;
        }

        public static int Combine<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        {
            int hash1 = t1 != null ? t1.GetHashCode() : 0;
            int hash2 = t2 != null ? t2.GetHashCode() : 0;
            int hash3 = t3 != null ? t3.GetHashCode() : 0;
            int hash4 = t4 != null ? t4.GetHashCode() : 0;
            int hash5 = t5 != null ? t5.GetHashCode() : 0;
            int hash6 = t6 != null ? t6.GetHashCode() : 0;
            int combinedHash = Hash.Combine(hash1, hash2);
            combinedHash = Hash.Combine(combinedHash, hash3);
            combinedHash = Hash.Combine(combinedHash, hash4);
            combinedHash = Hash.Combine(combinedHash, hash5);
            combinedHash = Hash.Combine(combinedHash, hash6);
            return combinedHash;
        }

        public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
        {
            int hash1 = t1 != null ? t1.GetHashCode() : 0;
            int hash2 = t2 != null ? t2.GetHashCode() : 0;
            int hash3 = t3 != null ? t3.GetHashCode() : 0;
            int hash4 = t4 != null ? t4.GetHashCode() : 0;
            int hash5 = t5 != null ? t5.GetHashCode() : 0;
            int hash6 = t6 != null ? t6.GetHashCode() : 0;
            int hash7 = t7 != null ? t7.GetHashCode() : 0;
            int combinedHash = Hash.Combine(hash1, hash2);
            combinedHash = Hash.Combine(combinedHash, hash3);
            combinedHash = Hash.Combine(combinedHash, hash4);
            combinedHash = Hash.Combine(combinedHash, hash5);
            combinedHash = Hash.Combine(combinedHash, hash6);
            combinedHash = Hash.Combine(combinedHash, hash7);
            return combinedHash;
        }

        public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
        {
            int hash1 = t1 != null ? t1.GetHashCode() : 0;
            int hash2 = t2 != null ? t2.GetHashCode() : 0;
            int hash3 = t3 != null ? t3.GetHashCode() : 0;
            int hash4 = t4 != null ? t4.GetHashCode() : 0;
            int hash5 = t5 != null ? t5.GetHashCode() : 0;
            int hash6 = t6 != null ? t6.GetHashCode() : 0;
            int hash7 = t7 != null ? t7.GetHashCode() : 0;
            int hash8 = t8 != null ? t8.GetHashCode() : 0;
            int combinedHash = Hash.Combine(hash1, hash2);
            combinedHash = Hash.Combine(combinedHash, hash3);
            combinedHash = Hash.Combine(combinedHash, hash4);
            combinedHash = Hash.Combine(combinedHash, hash5);
            combinedHash = Hash.Combine(combinedHash, hash6);
            combinedHash = Hash.Combine(combinedHash, hash7);
            combinedHash = Hash.Combine(combinedHash, hash8);
            return combinedHash;
        }

        public static int SequentialValuesHash<T>(IEnumerable<T> values)
        {
            int hash = 0;
            foreach (var value in values)
            {
                hash = Hash.Combine(hash, value.GetHashCode());
            }
            return hash;
        }
    }
}
