// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <string.h>
#include <stdint.h>

// Include System.IO.Compression.Native headers
#include "../zlib/pal_zlib.h"
#include "../brotli/include/brotli/decode.h"
#include "../brotli/include/brotli/encode.h"
#include "../brotli/include/brotli/port.h"
#include "../brotli/include/brotli/types.h"

#ifndef lengthof
#define lengthof(rg)    (sizeof(rg)/sizeof(rg[0]))
#endif

typedef struct
{
    const char* name;
    const void* method;
} Entry;

static Entry s_compressionNative[] =
{
    {"BrotliDecoderCreateInstance", BrotliDecoderCreateInstance},
    {"BrotliDecoderDecompress", BrotliDecoderDecompress},
    {"BrotliDecoderDecompressStream", BrotliDecoderDecompressStream},
    {"BrotliDecoderDestroyInstance", BrotliDecoderDestroyInstance},
    {"BrotliDecoderIsFinished", BrotliDecoderIsFinished},
    {"BrotliEncoderCompress", BrotliEncoderCompress},
    {"BrotliEncoderCompressStream", BrotliEncoderCompressStream},
    {"BrotliEncoderCreateInstance", BrotliEncoderCreateInstance},
    {"BrotliEncoderDestroyInstance", BrotliEncoderDestroyInstance},
    {"BrotliEncoderHasMoreOutput", BrotliEncoderHasMoreOutput},
    {"BrotliEncoderSetParameter", BrotliEncoderSetParameter},
    {"CompressionNative_Crc32", CompressionNative_Crc32},
    {"CompressionNative_Deflate", CompressionNative_Deflate},
    {"CompressionNative_DeflateEnd", CompressionNative_DeflateEnd},
    {"CompressionNative_DeflateInit2_", CompressionNative_DeflateInit2_},
    {"CompressionNative_Inflate", CompressionNative_Inflate},
    {"CompressionNative_InflateEnd", CompressionNative_InflateEnd},
    {"CompressionNative_InflateInit2_", CompressionNative_InflateInit2_},
};

extern const void* CompressionResolveDllImport(const char* name)
{
    for (int i = 0; i < lengthof(s_compressionNative); i++)
    {
        if (strcmp(name, s_compressionNative[i].name) == 0)
        {
            return s_compressionNative[i].method;
        }
    }

    return NULL;
}
