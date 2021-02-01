// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    public sealed class Float32Array : TypedArray<Float32Array, float>
    {
        public Float32Array() { }

        public Float32Array(int length) : base(length) { }

        public Float32Array(ArrayBuffer buffer) : base(buffer) { }

        public Float32Array(ArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset) { }

        public Float32Array(ArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length) { }

        public Float32Array(SharedArrayBuffer buffer) : base(buffer) { }

        public Float32Array(SharedArrayBuffer buffer, int byteOffset) : base(buffer, byteOffset) { }

        public Float32Array(SharedArrayBuffer buffer, int byteOffset, int length) : base(buffer, byteOffset, length) { }

        internal Float32Array(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }

        /// <summary>
        /// Defines an implicit conversion of Float32Array class to a float
        /// </summary>
        public static implicit operator Span<float>(Float32Array typedarray) => typedarray.ToArray();

        /// <summary>
        /// Defines an implicit conversion of float to a Float32Array class.
        /// </summary>
        public static implicit operator Float32Array(Span<float> span) => From(span);
    }
}
