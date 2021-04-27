// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    public sealed class Float64Array : TypedArray<Float64Array, double>
    {
        public Float64Array() { }

        public Float64Array(int length) : base(length) { }

        public Float64Array(ArrayBuffer buffer) : base(buffer) { }

        public Float64Array(ArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset) { }

        public Float64Array(ArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length) { }

        public Float64Array(SharedArrayBuffer buffer) : base(buffer) { }

        public Float64Array(SharedArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset) { }

        public Float64Array(SharedArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length) { }

        internal Float64Array(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }

        /// <summary>
        /// Defines an implicit conversion of Float64Array class to a double
        /// </summary>
        public static implicit operator Span<double>(Float64Array typedarray) => typedarray.ToArray();

        /// <summary>
        /// Defines an implicit conversion of double to a Float64Array class.
        /// </summary>
        public static implicit operator Float64Array(Span<double> span) => From(span);
    }
}
