// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    [Obsolete]
    public class ArrayBuffer : JSObject
    {
        /// <summary>
        /// Initializes a new instance of the JavaScript Core ArrayBuffer class.
        /// </summary>
        /// <param name="length">Length.</param>
        public ArrayBuffer(int length)
            : base(JavaScriptImports.CreateCSOwnedObject(nameof(ArrayBuffer), new object[] { length }))
        {
#if FEATURE_WASM_THREADS
            LegacyHostImplementation.ThrowIfLegacyWorkerThread();
#endif
            LegacyHostImplementation.RegisterCSOwnedObject(this);
        }

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
        public int ByteLength => (int)this.GetObjectProperty("byteLength");

        /// <summary>
        /// Gets a value indicating whether this ArrayBuffer is view.
        /// </summary>
        /// <value><see langword="true"/> if is view; otherwise, <see langword="false"/>.</value>
        public bool IsView => (bool)this.GetObjectProperty("isView");

        /// <summary>
        /// Slice the specified begin.
        /// </summary>
        /// <returns>The slice.</returns>
        /// <param name="begin">Begin.</param>
        public ArrayBuffer Slice(int begin) => (ArrayBuffer)this.Invoke("slice", begin);

        /// <summary>
        /// Slice the specified begin and end.
        /// </summary>
        /// <returns>The slice.</returns>
        /// <param name="begin">Begin.</param>
        /// <param name="end">End.</param>
        public ArrayBuffer Slice(int begin, int end) => (ArrayBuffer)this.Invoke("slice", begin, end);
    }
}
