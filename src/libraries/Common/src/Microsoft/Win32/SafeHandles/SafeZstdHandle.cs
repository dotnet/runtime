// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeZstdCompressHandle : SafeHandle
    {
        internal SafeZstdCDictHandle? _dictionary;
        public SafeZstdCompressHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Zstd.ZSTD_freeCCtx(handle);

            // release the addref we took in SetDictionary
            _dictionary?.DangerousRelease();
            return true;
        }

        public void SetDictionary(SafeZstdCDictHandle dictionary)
        {
            Debug.Assert(dictionary != null);

            bool added = false;
            dictionary.DangerousAddRef(ref added);
            _dictionary = dictionary;

            nuint result = Interop.Zstd.ZSTD_CCtx_refCDict(this, dictionary);
            if (Interop.Zstd.ZSTD_isError(result) != 0)
            {
                throw new Interop.Zstd.ZstdNativeException(SR.ZstandardEncoder_DictionaryAttachFailed);
            }
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeZstdDecompressHandle : SafeHandle
    {
        internal SafeZstdDDictHandle? _dictionary;
        public SafeZstdDecompressHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Zstd.ZSTD_freeDCtx(handle);

            // release the addref we took in SetDictionary
            _dictionary?.DangerousRelease();
            return true;
        }

        public void SetDictionary(SafeZstdDDictHandle dictionary)
        {
            Debug.Assert(dictionary != null);

            bool added = false;
            dictionary.DangerousAddRef(ref added);
            _dictionary = dictionary;

            nuint result = Interop.Zstd.ZSTD_DCtx_refDDict(this, dictionary);
            if (Interop.Zstd.ZSTD_isError(result) != 0)
            {
                throw new Interop.Zstd.ZstdNativeException(SR.ZstandardDecoder_DictionaryAttachFailed);
            }
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeZstdCDictHandle : SafeHandle
    {
        public SafeZstdCDictHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Zstd.ZSTD_freeCDict(handle);
            return true;
        }

        public int Quality { get; set; }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeZstdDDictHandle : SafeHandle
    {
        public SafeZstdDDictHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Zstd.ZSTD_freeDDict(handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
