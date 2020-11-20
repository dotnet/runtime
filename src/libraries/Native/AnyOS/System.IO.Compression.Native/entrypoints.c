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
#define lengthof(rg)    (int)(sizeof(rg)/sizeof(rg[0]))
#endif

typedef struct
{
    const char* name;
    const void* method;
} Entry;

static Entry s_compressionNative[] =
{
    {"BrotliDecoderCreateInstance", (void*)BrotliDecoderCreateInstance},
    {"BrotliDecoderDecompress", (void*)BrotliDecoderDecompress},
    {"BrotliDecoderDecompressStream", (void*)BrotliDecoderDecompressStream},
    {"BrotliDecoderDestroyInstance", (void*)BrotliDecoderDestroyInstance},
    {"BrotliDecoderIsFinished", (void*)BrotliDecoderIsFinished},
    {"BrotliEncoderCompress", (void*)BrotliEncoderCompress},
    {"BrotliEncoderCompressStream", (void*)BrotliEncoderCompressStream},
    {"BrotliEncoderCreateInstance", (void*)BrotliEncoderCreateInstance},
    {"BrotliEncoderDestroyInstance", (void*)BrotliEncoderDestroyInstance},
    {"BrotliEncoderHasMoreOutput", (void*)BrotliEncoderHasMoreOutput},
    {"BrotliEncoderSetParameter", (void*)BrotliEncoderSetParameter},
    {"CompressionNative_Crc32", (void*)CompressionNative_Crc32},
    {"CompressionNative_Deflate", (void*)CompressionNative_Deflate},
    {"CompressionNative_DeflateEnd", (void*)CompressionNative_DeflateEnd},
    {"CompressionNative_DeflateInit2_", (void*)CompressionNative_DeflateInit2_},
    {"CompressionNative_Inflate", (void*)CompressionNative_Inflate},
    {"CompressionNative_InflateEnd", (void*)CompressionNative_InflateEnd},
    {"CompressionNative_InflateInit2_", (void*)CompressionNative_InflateInit2_},
};

EXTERN_C const void* CompressionResolveDllImport(const char* name);

EXTERN_C const void* CompressionResolveDllImport(const char* name)
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
