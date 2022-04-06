// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    public class ArrayBuffer : JSObject
    {
        /// <summary>
        /// Initializes a new instance of the JavaScript Core ArrayBuffer class.
        /// </summary>
        public ArrayBuffer() : base(nameof(ArrayBuffer))
        { }

        /// <summary>
        /// Initializes a new instance of the JavaScript Core ArrayBuffer class.
        /// </summary>
        /// <param name="length">Length.</param>
        public ArrayBuffer(int length) : base(nameof(ArrayBuffer), length)
        { }

        /// <summary>
        /// Initializes a new instance of the JavaScript Core ArrayBuffer class.
        /// </summary>
        /// <param name="jsHandle">Js handle.</param>
        internal ArrayBuffer(IntPtr jsHandle) : base(jsHandle)
        { }

        /// <summary>
        /// The length of an ArrayBuffer in bytes.
        /// </summary>
        /// <value>The length of the underlying ArrayBuffer in bytes.</value>
        public int ByteLength => (int)GetObjectProperty("byteLength");

        /// <summary>
        /// Gets a value indicating whether this ArrayBuffer is view.
        /// </summary>
        /// <value><c>true</c> if is view; otherwise, <c>false</c>.</value>
        public bool IsView => (bool)GetObjectProperty("isView");

        /// <summary>
        /// Slice the specified begin.
        /// </summary>
        /// <returns>The slice.</returns>
        /// <param name="begin">Begin.</param>
        public ArrayBuffer Slice(int begin) => (ArrayBuffer)Invoke("slice", begin);

        /// <summary>
        /// Slice the specified begin and end.
        /// </summary>
        /// <returns>The slice.</returns>
        /// <param name="begin">Begin.</param>
        /// <param name="end">End.</param>
        public ArrayBuffer Slice(int begin, int end) => (ArrayBuffer)Invoke("slice", begin, end);
    }
}
