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
};

EXTERN_C const void* CompressionResolveDllImport(const char* name);

EXTERN_C const void* CompressionResolveDllImport(const char* name)
{
    return minipal_resolve_dllimport(s_compressionNative, ARRAY_SIZE(s_compressionNative), name);
}
