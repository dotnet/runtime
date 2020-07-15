// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Runtime.Loader.Tests
{
    public class TestClassNotSupported_FixedAddressValueType
    {
        public struct S<T>
        {
            public T Value;
        }

        [FixedAddressValueType]
        public static S<int> FixedInt;
    }
}
