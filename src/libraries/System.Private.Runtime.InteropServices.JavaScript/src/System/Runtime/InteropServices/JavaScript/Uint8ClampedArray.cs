// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    public sealed class Uint8ClampedArray : TypedArray<Uint8ClampedArray, byte>
    {
        /// <summary>
        /// Initializes a new instance of the JavaScript Core Uint8ClampedArray class.
        /// </summary>
        public Uint8ClampedArray()
        { }

        public Uint8ClampedArray(int length) : base(length)
        { }

        public Uint8ClampedArray(ArrayBuffer buffer) : base(buffer)
        { }

        public Uint8ClampedArray(ArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset)
        { }

        public Uint8ClampedArray(ArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length)
        { }

        public Uint8ClampedArray(SharedArrayBuffer buffer) : base(buffer)
        { }

        public Uint8ClampedArray(SharedArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset)
        { }

        public Uint8ClampedArray(SharedArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length)
        { }

        internal Uint8ClampedArray(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }

        /// <summary>
        /// Defines an implicit conversion of JavaScript Core Uint8ClampedArray class to a Span&lt;byte&gt;
        /// </summary>
        public static implicit operator Span<byte>(Uint8ClampedArray typedarray) => typedarray.ToArray();

        public static implicit operator Uint8ClampedArray(Span<byte> span) => From(span);
    }
}
