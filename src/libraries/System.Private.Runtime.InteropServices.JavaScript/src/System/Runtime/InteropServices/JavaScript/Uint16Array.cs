// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    [CLSCompliant(false)]
    public sealed class Uint16Array : TypedArray<Uint16Array, ushort>
    {
        public Uint16Array() { }

        public Uint16Array(int length) : base(length) { }

        public Uint16Array(ArrayBuffer buffer) : base(buffer) { }

        public Uint16Array(ArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset) { }

        public Uint16Array(ArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length) { }

        public Uint16Array(SharedArrayBuffer buffer) : base(buffer) { }

        public Uint16Array(SharedArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset) { }

        public Uint16Array(SharedArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length) { }

        internal Uint16Array(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }

        /// <summary>
        /// Defines an implicit conversion of Uint16Array class to a ushort
        /// </summary>
        public static implicit operator Span<ushort>(Uint16Array typedarray) => typedarray.ToArray();

        /// <summary>
        /// Defines an implicit conversion of ushort to a Uint16Array class.
        /// </summary>
        public static implicit operator Uint16Array(Span<ushort> span) => From(span);
    }
}
