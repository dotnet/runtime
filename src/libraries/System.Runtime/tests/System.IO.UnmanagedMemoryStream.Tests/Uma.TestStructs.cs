// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Tests
{
    public class Uma_TestStructs
    {
        public const int UmaTestStruct_UnalignedSize = 2 * sizeof(int) + 2 * sizeof(bool) + sizeof(char);
        public const int UmaTestStruct_AlignedSize = 16; // potentially architecture dependent.
        public struct UmaTestStruct
        {
            public int int1;
            public bool bool1;
            public int int2;
            public char char1;
            public bool bool2;
        }

        public struct UmaTestStruct_ContainsReferenceType
        {
            public object referenceType;
        }

        public struct UmaTestStruct_Generic<T>
        {
            public T ofT;
        }
    }
}
