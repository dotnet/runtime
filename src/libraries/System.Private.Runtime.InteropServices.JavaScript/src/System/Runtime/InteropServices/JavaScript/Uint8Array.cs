// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    public sealed class Uint8Array : JSObject
    {
        /// <summary>
        /// Initializes a new instance of the JavaScript Core Uint8Array class.
        /// </summary>
        public Uint8Array()
        { }

        public Uint8Array(int length) : base(nameof(Uint8Array), length)
        { }

        public Uint8Array(ArrayBuffer buffer) : base(nameof(Uint8Array), buffer)
        { }

        public Uint8Array(ArrayBuffer buffer, int byteOffset) : base(nameof(Uint8Array), buffer, byteOffset)
        { }

        public Uint8Array(ArrayBuffer buffer, int byteOffset, int length) : base(nameof(Uint8Array), buffer, byteOffset, length)
        { }

        internal Uint8Array(IntPtr jsHandle) : base(jsHandle)
        { }

        public int Length
        {
            get => Convert.ToInt32(GetObjectProperty("length"));
            set => SetObjectProperty("length", value, false);
        }

        /// <summary>
        /// Defines an implicit conversion of JavaScript Core Uint8Array class to a Span&lt;byte&gt;
        /// </summary>
        public static implicit operator Span<byte>(Uint8Array typedarray) => typedarray.ToArray();

        public static implicit operator Uint8Array(Span<byte> span) => From(span);

        public byte[] ToArray()
        {
            AssertNotDisposed();

            Interop.Runtime.TypedArrayToArrayRef(JSHandle, out int exception, out object res);

            if (exception != 0)
                throw new JSException((string)res);
            return (byte[])res;
        }

        public static unsafe Uint8Array From(ReadOnlySpan<byte> span)
        {
            // source has to be instantiated.
            if (span == null)
            {
                throw new System.ArgumentException(SR.Format(SR.ArgumentCannotBeNull, nameof(span)));
            }

            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(span);
            fixed (byte* ptr = bytes)
            {
                Interop.Runtime.TypedArrayFromRef((int)ptr, 0, span.Length, sizeof(byte), (int)TypedArrayTypeCode.Uint8Array, out int exception, out object res);
                if (exception != 0)
                    throw new JSException((string)res);
                var r = (Uint8Array)res;
                r.ReleaseInFlight();
                return r;
            }

        }

        public enum TypedArrayTypeCode
        {
            Uint8Array = 6,
        }
    }
}
