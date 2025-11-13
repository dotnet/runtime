// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/entrypoints.h>

// Include System.IO.Compression.Native headers
#include "pal_zlib.h"

#if !defined(TARGET_WASM)
#include <brotli/decode.h>
#include <brotli/encode.h>
#include <brotli/port.h>
#include <brotli/types.h>

#define ZSTD_STATIC_LINKING_ONLY
#include <zstd.h>
#include <zdict.h>
#endif // !TARGET_WASM

static const Entry s_compressionNative[] =
{
#if !defined(TARGET_WASM)
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
#endif // !TARGET_WASM
    DllImportEntry(CompressionNative_Crc32)
    DllImportEntry(CompressionNative_Deflate)
    DllImportEntry(CompressionNative_DeflateEnd)
    DllImportEntry(CompressionNative_DeflateInit2_)
    DllImportEntry(CompressionNative_Inflate)
    DllImportEntry(CompressionNative_InflateEnd)
    DllImportEntry(CompressionNative_InflateInit2_)
    DllImportEntry(CompressionNative_InflateReset2_)
#if !defined(TARGET_WASM)
    DllImportEntry(ZSTD_createCCtx)
    DllImportEntry(ZSTD_createDCtx)
    DllImportEntry(ZSTD_freeCCtx)
    DllImportEntry(ZSTD_freeDCtx)
    DllImportEntry(ZSTD_createCDict_byReference)
    DllImportEntry(ZSTD_freeCDict)
    DllImportEntry(ZSTD_createDDict_byReference)
    DllImportEntry(ZSTD_freeDDict)
    DllImportEntry(ZSTD_decompress)
    DllImportEntry(ZSTD_decompressDCtx)
    DllImportEntry(ZSTD_decompress_usingDDict)
    DllImportEntry(ZSTD_decompressBound)
    DllImportEntry(ZSTD_decompressStream)
    DllImportEntry(ZSTD_DCtx_reset)
    DllImportEntry(ZSTD_DCtx_refDDict)
    DllImportEntry(ZSTD_CCtx_refCDict)
    DllImportEntry(ZSTD_compressBound)
    DllImportEntry(ZSTD_compress2)
    DllImportEntry(ZSTD_compress_usingCDict)
    DllImportEntry(ZSTD_compressStream2)
    DllImportEntry(ZSTD_CCtx_setParameter)
    DllImportEntry(ZSTD_DCtx_setParameter)
    DllImportEntry(ZSTD_CCtx_reset)
    DllImportEntry(ZSTD_minCLevel)
    DllImportEntry(ZSTD_maxCLevel)
    DllImportEntry(ZSTD_defaultCLevel)
    DllImportEntry(ZSTD_isError)
    DllImportEntry(ZSTD_getErrorName)
    DllImportEntry(ZSTD_DCtx_refPrefix)
    DllImportEntry(ZSTD_CCtx_refPrefix)
    DllImportEntry(ZSTD_CCtx_setPledgedSrcSize)
    DllImportEntry(ZDICT_trainFromBuffer)
#endif // !TARGET_WASM
};

EXTERN_C const void* CompressionResolveDllImport(const char* name);

EXTERN_C const void* CompressionResolveDllImport(const char* name)
{
    return minipal_resolve_dllimport(s_compressionNative, ARRAY_SIZE(s_compressionNative), name);
}
