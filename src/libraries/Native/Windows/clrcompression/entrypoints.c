// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

// Include clrcompression headers
#include "../../AnyOS/zlib/pal_zlib.h"
#include "../../AnyOS/brotli/include/brotli/decode.h"
#include "../../AnyOS/brotli/include/brotli/encode.h"
#include "../../AnyOS/brotli/include/brotli/port.h"
#include "../../AnyOS/brotli/include/brotli/types.h"

#define FCFuncStart(name) EXTERN_C const void* name[]; const void* name[] = {
#define FCFuncEnd() (void*)0x01 /* FCFuncFlag_EndOfArray */ };

#define QCFuncElement(name,impl) \
    (void*)0x8 /* FCFuncFlag_QCall */, (void*)(impl), (void*)name,

FCFuncStart(gEmbedded_Brotli)
    QCFuncElement("BrotliDecoderCreateInstance", BrotliDecoderCreateInstance)
    QCFuncElement("BrotliDecoderDecompressStream", BrotliDecoderDecompressStream)
    QCFuncElement("BrotliDecoderDecompress", BrotliDecoderDecompress)
    QCFuncElement("BrotliDecoderDestroyInstance", BrotliDecoderDestroyInstance)
    QCFuncElement("BrotliDecoderIsFinished", BrotliDecoderIsFinished)
    QCFuncElement("BrotliEncoderCreateInstance", BrotliEncoderCreateInstance)
    QCFuncElement("BrotliEncoderSetParameter", BrotliEncoderSetParameter)
    QCFuncElement("BrotliEncoderCompressStream", BrotliEncoderCompressStream)
    QCFuncElement("BrotliEncoderHasMoreOutput", BrotliEncoderHasMoreOutput)
    QCFuncElement("BrotliEncoderDestroyInstance", BrotliEncoderDestroyInstance)
    QCFuncElement("BrotliEncoderCompress", BrotliEncoderCompress)
FCFuncEnd()

FCFuncStart(gEmbedded_zlib)
    QCFuncElement("DeflateInit2_", CompressionNative_DeflateInit2_)
    QCFuncElement("Deflate", CompressionNative_Deflate)
    QCFuncElement("DeflateEnd", CompressionNative_DeflateEnd)
    QCFuncElement("InflateInit2_", CompressionNative_InflateInit2_)
    QCFuncElement("Inflate", CompressionNative_Inflate)
    QCFuncElement("InflateEnd", CompressionNative_InflateEnd)
    QCFuncElement("crc32", CompressionNative_Crc32)
FCFuncEnd()
