// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    [CLSCompliant(false)]
    public sealed class Uint32Array : TypedArray<Uint32Array, uint>
    {
        public Uint32Array() { }

        public Uint32Array(int length) : base(length) { }

        public Uint32Array(ArrayBuffer buffer) : base(buffer) { }

        public Uint32Array(ArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset) { }

        public Uint32Array(ArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length) { }

        public Uint32Array(SharedArrayBuffer buffer) : base(buffer) { }

        public Uint32Array(SharedArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset) { }

        public Uint32Array(SharedArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length) { }

        internal Uint32Array(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }

        /// <summary>
        /// Defines an implicit conversion of Uint32Array class to a uint
        /// </summary>
        public static implicit operator Span<uint>(Uint32Array typedarray) => typedarray.ToArray();

        /// <summary>
        /// Defines an implicit conversion of uint to a Uint32Array class.
        /// </summary>
        public static implicit operator Uint32Array(Span<uint> span) => From(span);
    }
}
