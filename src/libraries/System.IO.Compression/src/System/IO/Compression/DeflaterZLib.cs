// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Security;

using ZErrorCode = System.IO.Compression.ZLibNative.ErrorCode;
using ZFlushCode = System.IO.Compression.ZLibNative.FlushCode;

namespace System.IO.Compression
{
    internal class DeflaterZLib : IDeflater
    {
        private ZLibNative.ZLibStreamHandle _zlibStream;
        private GCHandle _inputBufferHandle;
        private bool _isDisposed;

        // Note, DeflateStream or the deflater do not try to be thread safe.
        // The lock is just used to make writing to unmanaged structures atomic to make sure
        // that they do not get inconsistent fields that may lead to an unmanaged memory violation.
        // To prevent *managed* buffer corruption or other weird behaviour users need to synchronise
        // on the stream explicitly.
        private readonly object _syncLock = new object();

        #region exposed members

        internal DeflaterZLib()

            : this(CompressionLevel.Optimal)
        {
        }

        internal DeflaterZLib(CompressionLevel compressionLevel)
        {
            ZLibNative.CompressionLevel zlibCompressionLevel;
            int windowBits;
            int memLevel;
            ZLibNative.CompressionStrategy strategy;

            switch (compressionLevel)
            {
                // Note that ZLib currently exactly correspond to the optimal values.
                // However, we have determined the optimal values by intependent measurements across
                // a range of all possible ZLib parameters and over a set of different data.
                // We stress that by using explicitly the values obtained by the measurements rather than
                // ZLib defaults even if they happened to be the same.
                // For ZLib 1.2.3 we have (copied from ZLibNative.cs):
                // ZLibNative.CompressionLevel.DefaultCompression = 6;
                // ZLibNative.Deflate_DefaultWindowBits = -15;
                // ZLibNative.Deflate_DefaultMemLevel = 8;


                case CompressionLevel.Optimal:
                    zlibCompressionLevel = (ZLibNative.CompressionLevel)6;
                    windowBits = -15;
                    memLevel = 8;
                    strategy = ZLibNative.CompressionStrategy.DefaultStrategy;
                    break;

                case CompressionLevel.Fastest:
                    zlibCompressionLevel = (ZLibNative.CompressionLevel)1;
                    windowBits = -15;
                    memLevel = 8;
                    strategy = ZLibNative.CompressionStrategy.DefaultStrategy;
                    break;

                case CompressionLevel.NoCompression:
                    zlibCompressionLevel = (ZLibNative.CompressionLevel)0;
                    windowBits = -15;
                    memLevel = 7;
                    strategy = ZLibNative.CompressionStrategy.DefaultStrategy;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("compressionLevel");
            }

            _isDisposed = false;
            DeflateInit(zlibCompressionLevel, windowBits, memLevel, strategy);
        }

        ~DeflaterZLib()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [SecuritySafeCritical]
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    _zlibStream.Dispose();

                if (_inputBufferHandle.IsAllocated)
                    DeallocateInputBufferHandle();
                _isDisposed = true;
            }
        }

        public bool NeedsInput()
        {
            return 0 == _zlibStream.AvailIn;
        }

        void IDeflater.SetInput(byte[] inputBuffer, int startIndex, int count)
        {
            Debug.Assert(NeedsInput(), "We have something left in previous input!");
            Debug.Assert(null != inputBuffer);
            Debug.Assert(startIndex >= 0 && count >= 0 && count + startIndex <= inputBuffer.Length);
            Debug.Assert(!_inputBufferHandle.IsAllocated);

            if (0 == count)
                return;

            lock (_syncLock)
            {
                _inputBufferHandle = GCHandle.Alloc(inputBuffer, GCHandleType.Pinned);

                _zlibStream.NextIn = _inputBufferHandle.AddrOfPinnedObject() + startIndex;
                _zlibStream.AvailIn = (uint)count;
            }
        }

        int IDeflater.GetDeflateOutput(byte[] outputBuffer)
        {
            Contract.Ensures(Contract.Result<int>() >= 0 && Contract.Result<int>() <= outputBuffer.Length);

            Debug.Assert(null != outputBuffer, "Can't pass in a null output buffer!");
            Debug.Assert(!NeedsInput(), "GetDeflateOutput should only be called after providing input");
            Debug.Assert(_inputBufferHandle.IsAllocated);

            try
            {
                int bytesRead;
                ReadDeflateOutput(outputBuffer, ZFlushCode.NoFlush, out bytesRead);
                return bytesRead;
            }
            finally
            {
                // Before returning, make sure to release input buffer if necesary:
                if (0 == _zlibStream.AvailIn && _inputBufferHandle.IsAllocated)
                    DeallocateInputBufferHandle();
            }
        }

        private unsafe ZErrorCode ReadDeflateOutput(byte[] outputBuffer, ZFlushCode flushCode, out int bytesRead)
        {
            lock (_syncLock)
            {
                fixed (byte* bufPtr = outputBuffer)
                {
                    _zlibStream.NextOut = (IntPtr)bufPtr;
                    _zlibStream.AvailOut = (uint)outputBuffer.Length;

                    ZErrorCode errC = Deflate(flushCode);
                    bytesRead = outputBuffer.Length - (int)_zlibStream.AvailOut;

                    return errC;
                }
            }
        }

        bool IDeflater.Finish(byte[] outputBuffer, out int bytesRead)
        {
            Debug.Assert(null != outputBuffer, "Can't pass in a null output buffer!");
            Debug.Assert(NeedsInput(), "We have something left in previous input!");
            Debug.Assert(!_inputBufferHandle.IsAllocated);

            // Note: we require that NeedsInput() == true, i.e. that 0 == _zlibStream.AvailIn.
            // If there is still input left we should never be getting here; instead we
            // should be calling GetDeflateOutput.

            ZErrorCode errC = ReadDeflateOutput(outputBuffer, ZFlushCode.Finish, out bytesRead);
            return errC == ZErrorCode.StreamEnd;
        }

        #endregion  // exposed functions


        #region helpers & native call wrappers

        private void DeallocateInputBufferHandle()
        {
            Debug.Assert(_inputBufferHandle.IsAllocated);

            lock (_syncLock)
            {
                _zlibStream.AvailIn = 0;
                _zlibStream.NextIn = ZLibNative.ZNullPtr;
                _inputBufferHandle.Free();
            }
        }

        [SecuritySafeCritical]
        private void DeflateInit(ZLibNative.CompressionLevel compressionLevel, int windowBits, int memLevel,
                                 ZLibNative.CompressionStrategy strategy)
        {
            ZErrorCode errC;
            try
            {
                errC = ZLibNative.CreateZLibStreamForDeflate(out _zlibStream, compressionLevel,
                                                             windowBits, memLevel, strategy);
            }
            catch (Exception cause)
            {
                throw new ZLibException(SR.ZLibErrorDLLLoadError, cause);
            }

            switch (errC)
            {
                case ZErrorCode.Ok:
                    return;

                case ZErrorCode.MemError:
                    throw new ZLibException(SR.ZLibErrorNotEnoughMemory, "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());

                case ZErrorCode.VersionError:
                    throw new ZLibException(SR.ZLibErrorVersionMismatch, "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());

                case ZErrorCode.StreamError:
                    throw new ZLibException(SR.ZLibErrorIncorrectInitParameters, "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());

                default:
                    throw new ZLibException(SR.ZLibErrorUnexpected, "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());
            }
        }


        [SecuritySafeCritical]
        private ZErrorCode Deflate(ZFlushCode flushCode)
        {
            ZErrorCode errC;
            try
            {
                errC = _zlibStream.Deflate(flushCode);
            }
            catch (Exception cause)
            {
                throw new ZLibException(SR.ZLibErrorDLLLoadError, cause);
            }

            switch (errC)
            {
                case ZErrorCode.Ok:
                case ZErrorCode.StreamEnd:
                    return errC;

                case ZErrorCode.BufError:
                    return errC;  // This is a recoverable error

                case ZErrorCode.StreamError:
                    throw new ZLibException(SR.ZLibErrorInconsistentStream, "deflate", (int)errC, _zlibStream.GetErrorMessage());

                default:
                    throw new ZLibException(SR.ZLibErrorUnexpected, "deflate", (int)errC, _zlibStream.GetErrorMessage());
            }
        }
        #endregion  // helpers & native call wrappers

    }  // internal class DeflaterZLib
}  // namespace System.IO.Compression
