// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    public sealed class Int32Array : TypedArray<Int32Array, int>
    {
        public Int32Array() { }

        public Int32Array(int length) : base(length) { }

        public Int32Array(ArrayBuffer buffer) : base(buffer) { }

        public Int32Array(ArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset) { }

        public Int32Array(ArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length) { }

        public Int32Array(SharedArrayBuffer buffer) : base(buffer) { }

        public Int32Array(SharedArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset) { }

        public Int32Array(SharedArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length) { }

        internal Int32Array(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }

        /// <summary>
        /// Defines an implicit conversion of Int32Array class to a int
        /// </summary>
        public static implicit operator Span<int>(Int32Array typedarray) => typedarray.ToArray();

        /// <summary>
        /// Defines an implicit conversion of int to a Int32Array class.
        /// </summary>
        public static implicit operator Int32Array(Span<int> span) => From(span);
    }
}
