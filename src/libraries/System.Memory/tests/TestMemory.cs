// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    // NOTE: This TestMemory<T> must have the same layout as Memory<T> and ReadOnlyMemory<T>,
    // since the test code uses Unsafe.As to cast between them.
    // This exists to get access to the private fields of Memory<T> for testing purposes.
    public readonly struct TestMemory<T>
    {
        public readonly object _object;
        public readonly int _index;
        public readonly int _length;
    }
}
