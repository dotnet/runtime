// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include <stdlib.h>
#include "pal_zlib.h"

#ifdef  _WIN32
    #define c_static_assert(e) static_assert((e),"")
    #include "../../Windows/System.IO.Compression.Native/zlib/zlib.h"
#else
    #include "pal_utilities.h"
    #include <zlib.h>
#endif

c_static_assert(PAL_Z_NOFLUSH == Z_NO_FLUSH);
c_static_assert(PAL_Z_FINISH == Z_FINISH);

c_static_assert(PAL_Z_OK == Z_OK);
c_static_assert(PAL_Z_STREAMEND == Z_STREAM_END);
c_static_assert(PAL_Z_STREAMERROR == Z_STREAM_ERROR);
c_static_assert(PAL_Z_DATAERROR == Z_DATA_ERROR);
c_static_assert(PAL_Z_MEMERROR == Z_MEM_ERROR);
c_static_assert(PAL_Z_BUFERROR == Z_BUF_ERROR);
c_static_assert(PAL_Z_VERSIONERROR == Z_VERSION_ERROR);

c_static_assert(PAL_Z_NOCOMPRESSION == Z_NO_COMPRESSION);
c_static_assert(PAL_Z_BESTSPEED == Z_BEST_SPEED);
c_static_assert(PAL_Z_DEFAULTCOMPRESSION == Z_DEFAULT_COMPRESSION);

c_static_assert(PAL_Z_DEFAULTSTRATEGY == Z_DEFAULT_STRATEGY);

c_static_assert(PAL_Z_DEFLATED == Z_DEFLATED);

/*
Initializes the PAL_ZStream by creating and setting its underlying z_stream.
*/
static int32_t Init(PAL_ZStream* stream)
{
    z_stream* zStream = (z_stream*)malloc(sizeof(z_stream));
    stream->internalState = zStream;

    if (zStream != NULL)
    {
        zStream->zalloc = Z_NULL;
        zStream->zfree = Z_NULL;
        zStream->opaque = Z_NULL;
        return PAL_Z_OK;
    }
    else
    {
        return PAL_Z_MEMERROR;
    }
}

/*
Frees any memory on the PAL_ZStream that was created by Init.
*/
static void End(PAL_ZStream* stream)
{
    z_stream* zStream = (z_stream*)(stream->internalState);
    assert(zStream != NULL);
    if (zStream != NULL)
    {
        free(zStream);
        stream->internalState = NULL;
    }
}

/*
Transfers the output values from the underlying z_stream to the PAL_ZStream.
*/
static void TransferStateToPalZStream(z_stream* from, PAL_ZStream* to)
{
    to->nextIn = from->next_in;
    to->availIn = from->avail_in;

    to->nextOut = from->next_out;
    to->availOut = from->avail_out;

    to->msg = from->msg;
}

/*
Transfers the input values from the PAL_ZStream to the underlying z_stream object.
*/
static void TransferStateFromPalZStream(PAL_ZStream* from, z_stream* to)
{
    to->next_in = from->nextIn;
    to->avail_in = from->availIn;

    to->next_out = from->nextOut;
    to->avail_out = from->availOut;
}

/*
Gets the current z_stream object for the specified PAL_ZStream.

This ensures any inputs are transferred from the PAL_ZStream to the underlying z_stream,
since the current values are always needed.
*/
static z_stream* GetCurrentZStream(PAL_ZStream* stream)
{
    z_stream* zStream = (z_stream*)(stream->internalState);
    assert(zStream != NULL);

    TransferStateFromPalZStream(stream, zStream);
    return zStream;
}

int32_t CompressionNative_DeflateInit2_(
    PAL_ZStream* stream, int32_t level, int32_t method, int32_t windowBits, int32_t memLevel, int32_t strategy)
{
    assert(stream != NULL);

    int32_t result = Init(stream);
    if (result == PAL_Z_OK)
    {
        z_stream* zStream = GetCurrentZStream(stream);
        result = deflateInit2(zStream, level, method, windowBits, memLevel, strategy);
        TransferStateToPalZStream(zStream, stream);
    }

    return result;
}

int32_t CompressionNative_Deflate(PAL_ZStream* stream, int32_t flush)
{
    assert(stream != NULL);

    z_stream* zStream = GetCurrentZStream(stream);
    int32_t result = deflate(zStream, flush);
    TransferStateToPalZStream(zStream, stream);

    return result;
}

int32_t CompressionNative_DeflateReset(PAL_ZStream* stream)
{
    assert(stream != NULL);

    z_stream* zStream = GetCurrentZStream(stream);
    int32_t result = deflateReset(zStream);
    TransferStateToPalZStream(zStream, stream);

    return result;
}

int32_t CompressionNative_DeflateEnd(PAL_ZStream* stream)
{
    assert(stream != NULL);

    z_stream* zStream = GetCurrentZStream(stream);
    int32_t result = deflateEnd(zStream);
    End(stream);

    return result;
}

int32_t CompressionNative_InflateInit2_(PAL_ZStream* stream, int32_t windowBits)
{
    assert(stream != NULL);

    int32_t result = Init(stream);
    if (result == PAL_Z_OK)
    {
        z_stream* zStream = GetCurrentZStream(stream);
        result = inflateInit2(zStream, windowBits);
        TransferStateToPalZStream(zStream, stream);
    }

    return result;
}

int32_t CompressionNative_Inflate(PAL_ZStream* stream, int32_t flush)
{
    assert(stream != NULL);

    z_stream* zStream = GetCurrentZStream(stream);
    int32_t result = inflate(zStream, flush);
    TransferStateToPalZStream(zStream, stream);

    return result;
}

int32_t CompressionNative_InflateReset(PAL_ZStream* stream)
{
    assert(stream != NULL);

    z_stream* zStream = GetCurrentZStream(stream);
    int32_t result = inflateReset(zStream);
    TransferStateToPalZStream(zStream, stream);

    return result;
}

int32_t CompressionNative_InflateEnd(PAL_ZStream* stream)
{
    assert(stream != NULL);

    z_stream* zStream = GetCurrentZStream(stream);
    int32_t result = inflateEnd(zStream);
    End(stream);

    return result;
}

uint32_t CompressionNative_Crc32(uint32_t crc, uint8_t* buffer, int32_t len)
{
    assert(buffer != NULL);

    unsigned long result = crc32(crc, buffer, len);
    assert(result <= UINT32_MAX);
    return (uint32_t)result;
}
