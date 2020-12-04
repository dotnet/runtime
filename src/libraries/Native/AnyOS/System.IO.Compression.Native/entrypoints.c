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
    OverrideEntry(BrotliDecoderCreateInstance)
    OverrideEntry(BrotliDecoderDecompress)
    OverrideEntry(BrotliDecoderDecompressStream)
    OverrideEntry(BrotliDecoderDestroyInstance)
    OverrideEntry(BrotliDecoderIsFinished)
    OverrideEntry(BrotliEncoderCompress)
    OverrideEntry(BrotliEncoderCompressStream)
    OverrideEntry(BrotliEncoderCreateInstance)
    OverrideEntry(BrotliEncoderDestroyInstance)
    OverrideEntry(BrotliEncoderHasMoreOutput)
    OverrideEntry(BrotliEncoderSetParameter)
    OverrideEntry(CompressionNative_Crc32)
    OverrideEntry(CompressionNative_Deflate)
    OverrideEntry(CompressionNative_DeflateEnd)
    OverrideEntry(CompressionNative_DeflateInit2_)
    OverrideEntry(CompressionNative_Inflate)
    OverrideEntry(CompressionNative_InflateEnd)
    OverrideEntry(CompressionNative_InflateInit2_)
};

EXTERN_C const void* CompressionResolveDllImport(const char* name);

EXTERN_C const void* CompressionResolveDllImport(const char* name)
{
    return ResolveDllImport(s_compressionNative, lengthof(s_compressionNative), name);
}
