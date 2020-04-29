// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
internal static partial class Interop
{
    internal static partial class JavaScript
    {

        public sealed class Uint8Array : TypedArray<Uint8Array, byte>
        {

            public Uint8Array()
            { }

            public Uint8Array(int length) : base(length)
            { }


            public Uint8Array(ArrayBuffer buffer) : base(buffer)
            { }

            public Uint8Array(ArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset)
            { }

            public Uint8Array(ArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length)
            { }

            public Uint8Array(SharedArrayBuffer buffer) : base(buffer)
            { }

            public Uint8Array(SharedArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset)
            { }

            public Uint8Array(SharedArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length)
            { }

            internal Uint8Array(IntPtr js_handle) : base(js_handle)
            { }

            public static implicit operator Span<byte>(Uint8Array typedarray) => typedarray.ToArray();

            public static implicit operator Uint8Array(Span<byte> span) => From(span);
        }
    }
}
