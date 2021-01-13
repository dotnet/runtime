// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    public sealed class Uint8Array : TypedArray<Uint8Array, byte>
    {
        /// <summary>
        /// Initializes a new instance of the JavaScript Core Uint8Array class.
        /// </summary>
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

        internal Uint8Array(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }

        /// <summary>
        /// Defines an implicit conversion of JavaScript Core Uint8Array class to a Span&lt;byte&gt;
        /// </summary>
        public static implicit operator Span<byte>(Uint8Array typedarray) => typedarray.ToArray();

        public static implicit operator Uint8Array(Span<byte> span) => From(span);
    }
}
