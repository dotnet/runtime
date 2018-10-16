// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public static class StructUtilsTestData
    {
        public static TheoryData<object, int> RecordsAndLength() => new TheoryData<object, int>
        {
            { null, 0 },
            { new Struct_Empty(), 0 },
            { new Struct_T<float>(), 4 },
            { new Struct_T<double>(), 8 },
            { new Struct_T<short>(), 2 },
            { new Struct_T<int>(), 4 },
            { new Struct_T<byte>(), 1 },
            { new Struct_T<long>(), 8 },
            { new Struct_T<DateTime>(), 8 },
            { new Struct_T<bool>(), 2 },
            { new Struct_T<decimal>(), 16 },
            { new Struct_T<char>(), 2 },
            { new Struct_T<string>(), 4 },
            { new Struct_ArrayT<byte>(elementCount: 10), 4 },
            { new Struct_ArrayT<int>(elementCount: 10), 4 },
            { new Struct_FixedArrayT10<byte>(), 10 },
            { new Struct_FixedArrayT10<int>(), 40 },
            { new Struct_FixedArrayT10x20<byte>(), 200 },
            { new Struct_FixedArrayT10x20<int>(), 800 },
            { new Struct_FixedString10(), 10 },
            { new Struct_PrivateInt(), 0 },
            { new Struct_MultipleWithAlignment(), 22 },
        };

        public struct Struct_Empty { }
        public struct Struct_T<T> { public T x; }
        public struct Struct_ArrayT<T> { public Struct_ArrayT(int elementCount) { x = new T[elementCount]; } public T[] x; }
        public struct Struct_FixedArrayT10<T> { [VBFixedArray(9)] public T[] x; }
        public struct Struct_FixedArrayT10x20<T> { [VBFixedArray(9, 19)] public T[] x; }
        public struct Struct_FixedString10 { [VBFixedString(10)] public string x; }
#pragma warning disable 0169
        public struct Struct_PrivateInt { private int x; }
#pragma warning restore 0169
        public struct Struct_MultipleWithAlignment { public byte b; public char c; [VBFixedString(3)] public string s; public decimal d; }
    }
}
