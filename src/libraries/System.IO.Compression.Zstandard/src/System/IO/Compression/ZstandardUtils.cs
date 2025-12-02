// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.IO.Compression
{
    internal static class ZstandardUtils
    {
        // Zstandard compression level constants from native library
        internal static int Quality_Min => Interop.Zstd.ZSTD_minCLevel();
        internal static int Quality_Max => Interop.Zstd.ZSTD_maxCLevel();
        internal static int Quality_Default => Interop.Zstd.ZSTD_defaultCLevel();

        // Window size constraints based on Zstandard specification
        internal const int WindowLog_Min = 10;    // 1KB window
        internal const int WindowLog_Max = 31;    // 2GB window
        internal const int WindowLog_Default = 23; // 8MB window

        internal const int TargetBlockSize_Min = 1340;
        internal const int TargetBlockSize_Max = 131072;   // (2^17)

        // Buffer sizes for Zstandard operations
        internal const int DefaultInternalBufferSize = (1 << 16) - 16; // 65520 bytes, similar to Brotli

        /// <summary>Checks if a Zstandard operation result indicates an error.</summary>
        internal static bool IsError(nuint result) => Interop.Zstd.ZSTD_isError(result) != 0;

        /// <summary>Gets the error message for a Zstandard error code.</summary>
        internal static string GetErrorMessage(nuint errorCode)
        {
            IntPtr errorNamePtr = Interop.Zstd.ZSTD_getErrorName(errorCode);
            return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorNamePtr) ?? "Unknown error";
        }

        internal static void ThrowIfError(nuint result, string? message = null)
        {
            if (IsError(result))
            {
                Throw(result, message);
            }
        }

        [DoesNotReturn]
        internal static void Throw(nuint errorResult, string? message = null)
        {
            Debug.Assert(IsError(errorResult));
            throw CreateExceptionForError(errorResult, message);
        }

        internal static Exception CreateExceptionForError(nuint errorResult, string? message = null)
        {
            Debug.Assert(IsError(errorResult));
            return new Interop.Zstd.ZstdNativeException(SR.Format(message ?? SR.Zstd_NativeError, GetErrorMessage(errorResult)));
        }

        internal static int GetQualityFromCompressionLevel(CompressionLevel compressionLevel) =>
            compressionLevel switch
            {
                CompressionLevel.NoCompression => Quality_Min, // does not expose such option, choose lowest available
                CompressionLevel.Fastest => Quality_Min,
                CompressionLevel.Optimal => Quality_Default,
                CompressionLevel.SmallestSize => Quality_Max,
                _ => throw new ArgumentOutOfRangeException(nameof(compressionLevel), compressionLevel, SR.ArgumentOutOfRange_Enum)
            };
    }
}
