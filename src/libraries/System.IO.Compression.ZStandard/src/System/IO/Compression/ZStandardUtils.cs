// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal static class ZStandardUtils
    {
        // ZStandard compression level constants from native library
        internal static int Quality_Min => Interop.Zstd.ZSTD_minCLevel();
        internal static int Quality_Max => Interop.Zstd.ZSTD_maxCLevel();
        internal static int Quality_Default => Interop.Zstd.ZSTD_defaultCLevel();

        // Window size constraints based on ZStandard specification
        internal const int WindowBits_Min = 10;    // 1KB window
        internal const int WindowBits_Max = 31;    // 2GB window
        internal const int WindowBits_Default = 23; // 8MB window

        // Buffer sizes for ZStandard operations
        internal const int DefaultInternalBufferSize = (1 << 16) - 16; // 65520 bytes, similar to Brotli

        /// <summary>Checks if a ZStandard operation result indicates an error.</summary>
        internal static bool IsError(nuint result) => Interop.Zstd.ZSTD_isError(result) != 0;

        /// <summary>Gets the error message for a ZStandard error code.</summary>
        internal static string GetErrorMessage(nuint errorCode)
        {
            IntPtr errorNamePtr = Interop.Zstd.ZSTD_getErrorName(errorCode);
            return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorNamePtr) ?? "Unknown error";
        }
    }
}
