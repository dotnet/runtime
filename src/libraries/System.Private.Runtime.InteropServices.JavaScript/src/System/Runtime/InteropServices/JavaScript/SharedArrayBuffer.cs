// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    public class SharedArrayBuffer : CoreObject
    {
        /// <summary>
        /// Initializes a new instance of the JavaScript Core SharedArrayBuffer class.
        /// </summary>
        /// <param name="length">The size, in bytes, of the array buffer to create.</param>
        public SharedArrayBuffer(int length) : base(Interop.Runtime.New<SharedArrayBuffer>(length))
        { }

        internal SharedArrayBuffer(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }

        /// <summary>
        /// The size, in bytes, of the array. This is established when the array is constructed and cannot be changed.
        /// </summary>
        /// <value>The size, in bytes, of the array.</value>
        public int ByteLength => (int)GetObjectProperty("byteLength");

        /// <summary>
        /// Returns a new JavaScript Core SharedArrayBuffer whose contents are a copy of this SharedArrayBuffer's bytes from begin,
        /// inclusive, up to end, exclusive. If either begin or end is negative, it refers to an index from the end
        /// of the array, as opposed to from the beginning.
        /// </summary>
        /// <returns>a new JavaScript Core SharedArrayBuffer</returns>
        /// <param name="begin">Beginning index of copy.</param>
        /// <param name="end">Ending index, exclusive.</param>
        public SharedArrayBuffer Slice(int begin, int end) => (SharedArrayBuffer)Invoke("slice", begin, end);
    }
}
