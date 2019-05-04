// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public partial struct HashCode
    {
        private int _dummyPrimitive;
        public void Add<T>(T value) { }
        public void Add<T>(T value, System.Collections.Generic.IEqualityComparer<T> comparer) { }
        public static int Combine<T1>(T1 value1) { throw null; }
        public static int Combine<T1, T2>(T1 value1, T2 value2) { throw null; }
        public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3) { throw null; }
        public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4) { throw null; }
        public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) { throw null; }
        public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) { throw null; }
        public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) { throw null; }
        public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public int ToHashCode() { throw null; }
    }
}
