// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

// Include System.IO.Compression.Native headers
#include "../zlib/pal_zlib.h"
#include "../brotli/include/brotli/decode.h"
#include "../brotli/include/brotli/encode.h"
#include "../brotli/include/brotli/port.h"
#include "../brotli/include/brotli/types.h"

#define FCFuncStart(name) EXTERN_C const void* name[]; const void* name[] = {
#define FCFuncEnd() (void*)0x01 /* FCFuncFlag_EndOfArray */ };

#define QCFuncElement(name,impl) \
    (void*)0x8 /* FCFuncFlag_QCall */, (void*)(impl), (void*)name,

FCFuncStart(gEmbedded_Brotli)
    QCFuncElement("BrotliDecoderCreateInstance", BrotliDecoderCreateInstance)
    QCFuncElement("BrotliDecoderDecompress", BrotliDecoderDecompress)
    QCFuncElement("BrotliDecoderDecompressStream", BrotliDecoderDecompressStream)
    QCFuncElement("BrotliDecoderDestroyInstance", BrotliDecoderDestroyInstance)
    QCFuncElement("BrotliDecoderIsFinished", BrotliDecoderIsFinished)
    QCFuncElement("BrotliEncoderCompress", BrotliEncoderCompress)
    QCFuncElement("BrotliEncoderCompressStream", BrotliEncoderCompressStream)
    QCFuncElement("BrotliEncoderCreateInstance", BrotliEncoderCreateInstance)
    QCFuncElement("BrotliEncoderDestroyInstance", BrotliEncoderDestroyInstance)
    QCFuncElement("BrotliEncoderHasMoreOutput", BrotliEncoderHasMoreOutput)
    QCFuncElement("BrotliEncoderSetParameter", BrotliEncoderSetParameter)
FCFuncEnd()

FCFuncStart(gEmbedded_zlib)
    QCFuncElement("crc32", CompressionNative_Crc32)
    QCFuncElement("Deflate", CompressionNative_Deflate)
    QCFuncElement("DeflateEnd", CompressionNative_DeflateEnd)
    QCFuncElement("DeflateInit2_", CompressionNative_DeflateInit2_)
    QCFuncElement("Inflate", CompressionNative_Inflate)
    QCFuncElement("InflateEnd", CompressionNative_InflateEnd)
    QCFuncElement("InflateInit2_", CompressionNative_InflateInit2_)
FCFuncEnd()

