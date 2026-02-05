// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.IO.Compression
{
    internal static class ZstandardUtils
    {
        // Zstandard compression level constants from native library
        internal static readonly int Quality_Min = Interop.Zstd.ZSTD_minCLevel();
        internal static readonly int Quality_Max = Interop.Zstd.ZSTD_maxCLevel();
        internal static readonly int Quality_Default = Interop.Zstd.ZSTD_defaultCLevel();

        // Window size constraints based on Zstandard specification
        internal const int WindowLog_Min = 10;    // 1KB window
        internal static int WindowLog_Max => Environment.Is64BitProcess ? 31 : 30;    // 1GB or 2GB window, depending on platform
        internal const int WindowLog_Default = 23; // 8MB window

        internal const int TargetBlockSize_Min = 1340;
        internal const int TargetBlockSize_Max = 131072;   // (2^17)

        // Buffer sizes for Zstandard operations
        internal const int DefaultInternalBufferSize = (1 << 16) - 16; // 65520 bytes, similar to Brotli

        /// <summary>Checks if a Zstandard operation result indicates an error.</summary>
        internal static bool IsError(nuint result) => Interop.Zstd.ZSTD_isError(result) != 0;
        internal static bool IsError(nuint result, out Interop.Zstd.ZSTD_error error)
        {
            if (IsError(result))
            {
                error = (Interop.Zstd.ZSTD_error)result;
                return true;
            }

            error = Interop.Zstd.ZSTD_error.no_error;
            return false;
        }

        /// <summary>Gets the error message for a Zstandard error code.</summary>
        internal static string GetErrorMessage(Interop.Zstd.ZSTD_error error)
        {
            IntPtr errorNamePtr = Interop.Zstd.ZSTD_getErrorName((nuint)error);
            return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorNamePtr) ?? $"Unknown error {error}";
        }

        internal static void ThrowIfError(nuint result)
        {
            if (IsError(result, out var error))
            {
                Throw(error);
            }
        }

        [DoesNotReturn]
        internal static void Throw(Interop.Zstd.ZSTD_error error)
        {
            Debug.Assert(IsError((nuint)error));
            throw CreateExceptionForError(error);
        }

        internal static Exception CreateExceptionForError(Interop.Zstd.ZSTD_error error)
        {
            Debug.Assert(IsError((nuint)error));

            switch (error)
            {
                case Interop.Zstd.ZSTD_error.frameParameter_windowTooLarge:
                    return new IOException(SR.ZstandardDecoder_WindowTooLarge);

                case Interop.Zstd.ZSTD_error.dictionary_wrong:
                    return new InvalidDataException(SR.ZstandardDecoder_DictionaryWrong);

                case Interop.Zstd.ZSTD_error.memory_allocation:
                    return new OutOfMemoryException();

                case Interop.Zstd.ZSTD_error.stage_wrong:
                    return new InvalidOperationException(SR.ZstandardEncoderDecoder_InvalidState);

                default:
                    return new IOException(SR.Format(SR.Zstd_InternalError, GetErrorMessage(error)));
            }
        }

        internal static int GetQualityFromCompressionLevel(CompressionLevel compressionLevel) =>
            compressionLevel switch
            {
                // zstd supports negative quality levels, all negative levels map to the
                // same behavior (essentially no compression). Quality 0 means "default" = 3.
                // 1 is therefore the fastest compression level with some compression.
                CompressionLevel.NoCompression => Quality_Min,
                CompressionLevel.Fastest => 1,
                CompressionLevel.Optimal => Quality_Default,
                CompressionLevel.SmallestSize => Quality_Max,
                _ => throw new ArgumentOutOfRangeException(nameof(compressionLevel), compressionLevel, SR.ArgumentOutOfRange_Enum)
            };
    }
}
