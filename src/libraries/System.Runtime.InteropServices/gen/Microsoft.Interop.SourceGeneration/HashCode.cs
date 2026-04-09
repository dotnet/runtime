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
            return Hash.Combine(t1 != null ? t1.GetHashCode() : 0, t2 != null ? t2.GetHashCode() : 0);
        }

        public static int Combine<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
        {
            int combinedHash = t1 != null ? t1.GetHashCode() : 0;
            combinedHash = Hash.Combine(combinedHash, t2 != null ? t2.GetHashCode() : 0);
            return Hash.Combine(combinedHash, t3 != null ? t3.GetHashCode() : 0);
        }

        public static int Combine<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4)
        {
            int combinedHash = t1 != null ? t1.GetHashCode() : 0;
            combinedHash = Hash.Combine(combinedHash, t2 != null ? t2.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t3 != null ? t3.GetHashCode() : 0);
            return Hash.Combine(combinedHash, t4 != null ? t4.GetHashCode() : 0);
        }

        public static int Combine<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            int combinedHash = t1 != null ? t1.GetHashCode() : 0;
            combinedHash = Hash.Combine(combinedHash, t2 != null ? t2.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t3 != null ? t3.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t4 != null ? t4.GetHashCode() : 0);
            return Hash.Combine(combinedHash, t5 != null ? t5.GetHashCode() : 0);
        }

        public static int Combine<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        {
            int combinedHash = t1 != null ? t1.GetHashCode() : 0;
            combinedHash = Hash.Combine(combinedHash, t2 != null ? t2.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t3 != null ? t3.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t4 != null ? t4.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t5 != null ? t5.GetHashCode() : 0);
            return Hash.Combine(combinedHash, t6 != null ? t6.GetHashCode() : 0);
        }

        public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
        {
            int combinedHash = t1 != null ? t1.GetHashCode() : 0;
            combinedHash = Hash.Combine(combinedHash, t2 != null ? t2.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t3 != null ? t3.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t4 != null ? t4.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t5 != null ? t5.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t6 != null ? t6.GetHashCode() : 0);
            return Hash.Combine(combinedHash, t7 != null ? t7.GetHashCode() : 0);
        }

        public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
        {
            int combinedHash = t1 != null ? t1.GetHashCode() : 0;
            combinedHash = Hash.Combine(combinedHash, t2 != null ? t2.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t3 != null ? t3.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t4 != null ? t4.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t5 != null ? t5.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t6 != null ? t6.GetHashCode() : 0);
            combinedHash = Hash.Combine(combinedHash, t7 != null ? t7.GetHashCode() : 0);
            return Hash.Combine(combinedHash, t8 != null ? t8.GetHashCode() : 0);
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
