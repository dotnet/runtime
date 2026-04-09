// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace SharedTypes
{
    public struct IntFields
    {
        public int a;
        public int b;
        public int c;
    }

    public unsafe struct PointerFields
    {
        public int* i;
        public bool* b;
        public char* c;
    }

    public struct BlittableIntWrapper
    {
        public int i;
    }
}
