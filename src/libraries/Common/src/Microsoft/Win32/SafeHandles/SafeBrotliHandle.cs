// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeBrotliEncoderHandle : SafeHandle
    {
        public SafeBrotliEncoderHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Brotli.BrotliEncoderDestroyInstance(handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeBrotliDecoderHandle : SafeHandle
    {
        public SafeBrotliDecoderHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Brotli.BrotliDecoderDestroyInstance(handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeBrotliPreparedDictionaryHandle : SafeHandle
    {
        internal IntPtr _dictionaryBytes;
        internal int _dictionaryLength;

        public int DictionaryLength => _dictionaryLength;
        public unsafe byte* DictionaryBytes => (byte*)_dictionaryBytes.ToPointer();

        public SafeBrotliPreparedDictionaryHandle() : base(IntPtr.Zero, true) { }

        public void SetDictionaryBytes(IntPtr dictionaryBytes, int dictionaryLength)
        {
            _dictionaryBytes = dictionaryBytes;
            _dictionaryLength = dictionaryLength;
        }

        protected override bool ReleaseHandle()
        {
            Interop.Brotli.BrotliEncoderDestroyPreparedDictionary(handle);
            unsafe
            {
                NativeMemory.Free(_dictionaryBytes.ToPointer());
            }
            _dictionaryBytes = IntPtr.Zero;
            _dictionaryLength = 0;
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
