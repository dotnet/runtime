// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    public sealed class Int16Array : TypedArray<Int16Array, short>
    {
        public Int16Array() { }

        public Int16Array(int length) : base(length) { }

        public Int16Array(ArrayBuffer buffer) : base(buffer) { }

        public Int16Array(ArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset) { }

        public Int16Array(ArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length) { }

        public Int16Array(SharedArrayBuffer buffer) : base(buffer) { }

        public Int16Array(SharedArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset) { }

        public Int16Array(SharedArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length) { }

        internal Int16Array(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }

        /// <summary>
        /// Defines an implicit conversion of Int16Array class to a short
        /// </summary>
        [CLSCompliant(false)]
        public static implicit operator Span<short>(Int16Array typedarray) => typedarray.ToArray();

        /// <summary>
        /// Defines an implicit conversion of short to a Int16Array class.
        /// </summary>
        public static implicit operator Int16Array(Span<short> span) => From(span);
    }
}
