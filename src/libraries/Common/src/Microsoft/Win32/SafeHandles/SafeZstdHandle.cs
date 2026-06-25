// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO.Compression;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeZstdCompressHandle : SafeHandle
    {
        internal SafeZstdCDictHandle? _dictionary;
        internal MemoryHandle? _prefixHandle;
        public SafeZstdCompressHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Zstd.ZSTD_freeCCtx(handle);

            // release the addref we took in SetDictionary
            _dictionary?.DangerousRelease();

            if (_prefixHandle != null)
            {
                _prefixHandle.Value.Dispose();
                _prefixHandle = null;
            }
            return true;
        }

        public void SetDictionary(SafeZstdCDictHandle dictionary)
        {
            Debug.Assert(_dictionary == null);
            Debug.Assert(dictionary != null);

            bool added = false;
            try
            {
                dictionary.DangerousAddRef(ref added);
                ZstandardUtils.ThrowIfError(Interop.Zstd.ZSTD_CCtx_refCDict(this, dictionary));

                _dictionary = dictionary;
            }
            catch when (added)
            {
                dictionary.DangerousRelease();
                throw;
            }
        }

        public unsafe nuint SetPrefix(ReadOnlyMemory<byte> prefix)
        {
            MemoryHandle handle = prefix.Pin();

            nuint result = Interop.Zstd.ZSTD_CCtx_refPrefix(this, (byte*)handle.Pointer, (nuint)prefix.Length);

            if (Interop.Zstd.ZSTD_isError(result) != 0)
            {
                handle.Dispose();
            }
            else
            {
                _prefixHandle?.Dispose();
                _prefixHandle = handle;
            }

            return result;
        }

        public unsafe void Reset()
        {
            ZstandardUtils.ThrowIfError(Interop.Zstd.ZSTD_CCtx_reset(this, Interop.Zstd.ZstdResetDirective.ZSTD_reset_session_only));

            // prefix is not sticky and is cleared by reset
            if (_prefixHandle != null)
            {
                _prefixHandle.Value.Dispose();
                _prefixHandle = null;
            }
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeZstdDecompressHandle : SafeHandle
    {
        internal SafeZstdDDictHandle? _dictionary;
        internal MemoryHandle? _prefixHandle;
        public SafeZstdDecompressHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Zstd.ZSTD_freeDCtx(handle);

            // release the addref we took in SetDictionary
            _dictionary?.DangerousRelease();

            if (_prefixHandle != null)
            {
                _prefixHandle.Value.Dispose();
                _prefixHandle = null;
            }
            return true;
        }

        public void SetDictionary(SafeZstdDDictHandle dictionary)
        {
            Debug.Assert(_dictionary == null);
            Debug.Assert(dictionary != null);

            bool added = false;
            try
            {
                dictionary.DangerousAddRef(ref added);
                ZstandardUtils.ThrowIfError(Interop.Zstd.ZSTD_DCtx_refDDict(this, dictionary));

                _dictionary = dictionary;
            }
            catch when (added)
            {
                dictionary.DangerousRelease();
                throw;
            }
        }

        public unsafe nuint SetPrefix(ReadOnlyMemory<byte> prefix)
        {
            MemoryHandle handle = prefix.Pin();

            nuint result = Interop.Zstd.ZSTD_DCtx_refPrefix(this, (byte*)handle.Pointer, (nuint)prefix.Length);

            if (Interop.Zstd.ZSTD_isError(result) != 0)
            {
                handle.Dispose();
            }
            else
            {
                _prefixHandle?.Dispose();
                _prefixHandle = handle;
            }

            return result;
        }

        public unsafe void Reset()
        {
            ZstandardUtils.ThrowIfError(Interop.Zstd.ZSTD_DCtx_reset(this, Interop.Zstd.ZstdResetDirective.ZSTD_reset_session_only));

            // prefix is not sticky and is cleared by reset
            if (_prefixHandle != null)
            {
                _prefixHandle.Value.Dispose();
                _prefixHandle = null;
            }
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeZstdCDictHandle : SafeHandle
    {
        public SafeZstdCDictHandle() : base(IntPtr.Zero, true) { }

        internal PinnedGCHandle<byte[]> _pinnedData;

        protected override bool ReleaseHandle()
        {
            Interop.Zstd.ZSTD_freeCDict(handle);
            _pinnedData.Dispose();
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeZstdDDictHandle : SafeHandle
    {
        public SafeZstdDDictHandle() : base(IntPtr.Zero, true) { }

        internal PinnedGCHandle<byte[]> _pinnedData;

        protected override bool ReleaseHandle()
        {
            Interop.Zstd.ZSTD_freeDDict(handle);
            _pinnedData.Dispose();
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
