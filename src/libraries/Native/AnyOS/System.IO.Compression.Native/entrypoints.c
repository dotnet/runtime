// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../AnyOS/entrypoints.h"

// Include System.IO.Compression.Native headers
#include "../zlib/pal_zlib.h"
#include "../brotli/include/brotli/decode.h"
#include "../brotli/include/brotli/encode.h"
#include "../brotli/include/brotli/port.h"
#include "../brotli/include/brotli/types.h"

#include "../../AnyOS/entrypoints.h"

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
    DllImportEntry(BrotliEncoderSetParameter)
    DllImportEntry(CompressionNative_Crc32)
    DllImportEntry(CompressionNative_Deflate)
    DllImportEntry(CompressionNative_DeflateEnd)
    DllImportEntry(CompressionNative_DeflateReset)
    DllImportEntry(CompressionNative_DeflateInit2_)
    DllImportEntry(CompressionNative_Inflate)
    DllImportEntry(CompressionNative_InflateEnd)
    DllImportEntry(CompressionNative_InflateReset)
    DllImportEntry(CompressionNative_InflateInit2_)
};

EXTERN_C const void* CompressionResolveDllImport(const char* name);

EXTERN_C const void* CompressionResolveDllImport(const char* name)
{
    return ResolveDllImport(s_compressionNative, lengthof(s_compressionNative), name);
}
