// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/entrypoints.h>

// Include System.IO.Compression.Native headers
#include "pal_zlib.h"
#include <brotli/decode.h>
#include <brotli/encode.h>
#include <brotli/port.h>
#include <brotli/types.h>
#include <zstd.h>

static const Entry s_compressionNative[] =
{
    DllImportEntry(BrotliDecoderCreateInstance)
    DllImportEntry(BrotliDecoderDecompress)
    DllImportEntry(BrotliDecoderDecompressStream)
    DllImportEntry(BrotliDecoderDestroyInstance)
    DllImportEntry(BrotliDecoderIsFinished)
    DllImportEntry(BrotliEncoderCompress)
    DllImportEntry(BrotliEncoderCompressStream)
    DllImportEntry(BrotliEncoderCreateInstance)
    DllImportEntry(BrotliEncoderDestroyInstance)
    DllImportEntry(BrotliEncoderHasMoreOutput)
    DllImportEntry(BrotliEncoderMaxCompressedSize)
    DllImportEntry(BrotliEncoderSetParameter)
    DllImportEntry(CompressionNative_Crc32)
    DllImportEntry(CompressionNative_Deflate)
    DllImportEntry(CompressionNative_DeflateEnd)
    DllImportEntry(CompressionNative_DeflateInit2_)
    DllImportEntry(CompressionNative_Inflate)
    DllImportEntry(CompressionNative_InflateEnd)
    DllImportEntry(CompressionNative_InflateInit2_)
    DllImportEntry(CompressionNative_InflateReset2_)
    DllImportEntry(ZSTD_compress)
    DllImportEntry(ZSTD_compressBound)
    DllImportEntry(ZSTD_compressStream2)
    DllImportEntry(ZSTD_createCCtx)
    DllImportEntry(ZSTD_createDCtx)
    DllImportEntry(ZSTD_CCtx_setParameter)
    DllImportEntry(ZSTD_CStreamInSize)
    DllImportEntry(ZSTD_CStreamOutSize)
    DllImportEntry(ZSTD_decompress)
    DllImportEntry(ZSTD_decompressStream)
    DllImportEntry(ZSTD_defaultCLevel)
    DllImportEntry(ZSTD_DStreamInSize)
    DllImportEntry(ZSTD_DStreamOutSize)
    DllImportEntry(ZSTD_freeCCtx)
    DllImportEntry(ZSTD_freeDCtx)
    DllImportEntry(ZSTD_getErrorName)
    DllImportEntry(ZSTD_isError)
    DllImportEntry(ZSTD_maxCLevel)
    DllImportEntry(ZSTD_minCLevel)
};

EXTERN_C const void* CompressionResolveDllImport(const char* name);

EXTERN_C const void* CompressionResolveDllImport(const char* name)
{
    return minipal_resolve_dllimport(s_compressionNative, ARRAY_SIZE(s_compressionNative), name);
}
