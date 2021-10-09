// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    [CLSCompliant(false)]
    public sealed class Int8Array : TypedArray<Int8Array, sbyte>
    {
        public Int8Array()
        { }

        public Int8Array(int length) : base(length)
        { }

        public Int8Array(ArrayBuffer buffer) : base(buffer)
        { }

        public Int8Array(ArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset)
        { }

        public Int8Array(ArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length)
        { }

        public Int8Array(SharedArrayBuffer buffer) : base(buffer)
        { }

        public Int8Array(SharedArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset)
        { }

        public Int8Array(SharedArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length)
        { }

        internal Int8Array(IntPtr jsHandle) : base(jsHandle)
        { }

        /// <summary>
        /// Defines an implicit conversion of Int8Array class to a sbyte
        /// </summary>
        [CLSCompliant(false)]
        public static implicit operator Span<sbyte>(Int8Array typedarray) => typedarray.ToArray();

        /// <summary>
        /// Defines an implicit conversion of sbyte to a Int8Array class.
        /// </summary>
        [CLSCompliant(false)]
        public static implicit operator Int8Array(Span<sbyte> span) => From(span);
    }
}
