// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    [Obsolete]
    public sealed class Uint8Array : JSObject
    {
        public Uint8Array(int length)
            : base(JavaScriptImports.CreateCSOwnedObject(nameof(Uint8Array), new object[] { length }))
        {
            JSHostImplementation.RegisterCSOwnedObject(this);
        }

        public Uint8Array(ArrayBuffer buffer)
            : base(JavaScriptImports.CreateCSOwnedObject(nameof(Uint8Array), new object[] { buffer }))
        {
            JSHostImplementation.RegisterCSOwnedObject(this);
        }

        internal Uint8Array(IntPtr jsHandle) : base(jsHandle)
        { }

        public int Length
        {
            get => Convert.ToInt32(this.GetObjectProperty("length"));
            set => this.SetObjectProperty("length", value, false);
        }

        /// <summary>
        /// Defines an implicit conversion of JavaScript Core Uint8Array class to a Span&lt;byte&gt;
        /// </summary>
        public static implicit operator Span<byte>(Uint8Array typedarray) => typedarray.ToArray();

        public static implicit operator Uint8Array(Span<byte> span) => From(span);

        [MethodImpl(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public byte[] ToArray()
        {
            this.AssertNotDisposed();

            Interop.Runtime.TypedArrayToArrayRef(JSHandle, out int exception, out object res);

            if (exception != 0)
                throw new JSException((string)res);
            return (byte[])res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
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
