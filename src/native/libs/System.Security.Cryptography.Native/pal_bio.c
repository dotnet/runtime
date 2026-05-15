// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_bio.h"

#include <assert.h>

BIO* CryptoNative_CreateMemoryBio(void)
{
    ERR_clear_error();
    return BIO_new(BIO_s_mem());
}

BIO* CryptoNative_BioNewFile(const char* filename, const char* mode)
{
    ERR_clear_error();
    return BIO_new_file(filename, mode);
}

int32_t CryptoNative_BioDestroy(BIO* a)
{
    return BIO_free(a);
}

int32_t CryptoNative_BioGets(BIO* b, char* buf, int32_t size)
{
    ERR_clear_error();
    return BIO_gets(b, buf, size);
}

int32_t CryptoNative_BioRead(BIO* b, void* buf, int32_t len)
{
    ERR_clear_error();
    return BIO_read(b, buf, len);
}

int32_t CryptoNative_BioWrite(BIO* b, const void* buf, int32_t len)
{
    ERR_clear_error();
    return BIO_write(b, buf, len);
}

int32_t CryptoNative_GetMemoryBioSize(BIO* bio)
{
    // No impact on error queue.
    long ret = BIO_get_mem_data(bio, NULL);

    // BIO_get_mem_data returns the memory size, which will always be
    // an int32.
    assert(ret <= INT32_MAX);
    return (int32_t)ret;
}

int32_t CryptoNative_BioCtrlPending(BIO* bio)
{
    // No impact on the error queue.
    size_t result = BIO_ctrl_pending(bio);
    assert(result <= INT32_MAX);
    return (int32_t)result;
}

#include <pthread.h>
#include <stdlib.h>
#include <string.h>

typedef struct
{
    /* Carry-over buffer: holds bytes from a prior read window that SSL did
       not consume before the window was cleared. */
    uint8_t*       readCarry;
    int32_t        readCarryCapacity;
    int32_t        readCarryLen;
    int32_t        readCarryPos;

    const uint8_t* readPtr;
    int32_t        readLen;
    int32_t        readPos;

    uint8_t*       writePtr;
    int32_t        writeCapacity;
    int32_t        writePos;

    uint8_t*       spillBuf;
    int32_t        spillCapacity;
    int32_t        spillLen;

    int32_t        readError;   /* set to 1 if a carry allocation failed and bytes were lost */
} ManagedSpanBioCtx;

#define MANAGED_SPAN_SPILL_INITIAL 4096

static BIO_METHOD* g_managedSpanBioMethod = NULL;
static pthread_once_t g_managedSpanBioOnce = PTHREAD_ONCE_INIT;

static int ManagedSpanBioRead(BIO* bio, char* buf, int len)
{
    BIO_clear_retry_flags(bio);

    if (bio == NULL || buf == NULL || len <= 0)
    {
        return 0;
    }

    ManagedSpanBioCtx* ctx = (ManagedSpanBioCtx*)BIO_get_data(bio);
    if (ctx == NULL)
    {
        return -1;
    }

    int32_t carryAvail = ctx->readCarryLen - ctx->readCarryPos;
    if (carryAvail > 0)
    {
        int32_t toCopy = len < carryAvail ? len : carryAvail;
        memcpy(buf, ctx->readCarry + ctx->readCarryPos, (size_t)toCopy);
        ctx->readCarryPos += toCopy;
        if (ctx->readCarryPos == ctx->readCarryLen)
        {
            ctx->readCarryPos = 0;
            ctx->readCarryLen = 0;
        }
        return toCopy;
    }

    if (ctx->readError)
    {
        /* A prior BioClearReadWindow could not allocate carry space and dropped
           unread bytes. Surface this as a hard read failure so it does not look
           like a transient EAGAIN to the SSL engine. */
        return -1;
    }

    int32_t available = ctx->readLen - ctx->readPos;
    if (available <= 0 || ctx->readPtr == NULL)
    {
        BIO_set_retry_read(bio);
        return -1;
    }

    int32_t toCopy = len < available ? len : available;
    memcpy(buf, ctx->readPtr + ctx->readPos, (size_t)toCopy);
    ctx->readPos += toCopy;
    return toCopy;
}

static int ManagedSpanBioGrowCarry(ManagedSpanBioCtx* ctx, int32_t needed)
{
    if (ctx->readCarryCapacity >= needed)
    {
        return 1;
    }

    int32_t newCap = ctx->readCarryCapacity > 0 ? ctx->readCarryCapacity : 4096;
    while (newCap < needed)
    {
        if (newCap > INT32_MAX / 2)
        {
            newCap = needed;
            break;
        }
        newCap *= 2;
    }

    uint8_t* newBuf = (uint8_t*)realloc(ctx->readCarry, (size_t)newCap);
    if (newBuf == NULL)
    {
        return 0;
    }

    ctx->readCarry = newBuf;
    ctx->readCarryCapacity = newCap;
    return 1;
}

static int ManagedSpanBioGrowSpill(ManagedSpanBioCtx* ctx, int32_t needed)
{
    if (ctx->spillCapacity >= needed)
    {
        return 1;
    }

    int32_t newCap = ctx->spillCapacity > 0 ? ctx->spillCapacity : MANAGED_SPAN_SPILL_INITIAL;
    while (newCap < needed)
    {
        if (newCap > INT32_MAX / 2)
        {
            newCap = needed;
            break;
        }
        newCap *= 2;
    }

    uint8_t* newBuf = (uint8_t*)realloc(ctx->spillBuf, (size_t)newCap);
    if (newBuf == NULL)
    {
        return 0;
    }

    ctx->spillBuf = newBuf;
    ctx->spillCapacity = newCap;
    return 1;
}

static int ManagedSpanBioWrite(BIO* bio, const char* buf, int len)
{
    BIO_clear_retry_flags(bio);

    if (bio == NULL || buf == NULL || len < 0)
    {
        return 0;
    }

    if (len == 0)
    {
        return 0;
    }

    ManagedSpanBioCtx* ctx = (ManagedSpanBioCtx*)BIO_get_data(bio);
    if (ctx == NULL)
    {
        return -1;
    }

    int32_t remaining = len;
    const uint8_t* src = (const uint8_t*)buf;

    if (ctx->writePtr != NULL)
    {
        int32_t windowAvail = ctx->writeCapacity - ctx->writePos;
        if (windowAvail > 0)
        {
            int32_t toCopy = remaining < windowAvail ? remaining : windowAvail;
            memcpy(ctx->writePtr + ctx->writePos, src, (size_t)toCopy);
            ctx->writePos += toCopy;
            src += toCopy;
            remaining -= toCopy;
        }
    }

    if (remaining > 0)
    {
        int32_t needed = ctx->spillLen + remaining;
        if (!ManagedSpanBioGrowSpill(ctx, needed))
        {
            return -1;
        }
        memcpy(ctx->spillBuf + ctx->spillLen, src, (size_t)remaining);
        ctx->spillLen += remaining;
    }

    return len;
}

static long ManagedSpanBioCtrl(BIO* bio, int cmd, long num, void* ptr)
{
    (void)num;
    (void)ptr;

    ManagedSpanBioCtx* ctx = (ManagedSpanBioCtx*)BIO_get_data(bio);

    switch (cmd)
    {
        case BIO_CTRL_FLUSH:
            return 1;

        case BIO_CTRL_PENDING:
            if (ctx == NULL)
            {
                return 0;
            }
            return (long)((ctx->readCarryLen - ctx->readCarryPos) + (ctx->readLen - ctx->readPos));

        case BIO_CTRL_WPENDING:
            if (ctx == NULL)
            {
                return 0;
            }
            return (long)(ctx->writePos + ctx->spillLen);

        case BIO_CTRL_RESET:
            if (ctx != NULL)
            {
                ctx->readCarryLen = 0;
                ctx->readCarryPos = 0;
                ctx->readPtr = NULL;
                ctx->readLen = 0;
                ctx->readPos = 0;
                ctx->writePtr = NULL;
                ctx->writeCapacity = 0;
                ctx->writePos = 0;
                ctx->spillLen = 0;
                ctx->readError = 0;
                BIO_clear_retry_flags(bio);
            }
            return 1;

        case BIO_CTRL_EOF:
            return 0;

        default:
            return 0;
    }
}

static int ManagedSpanBioCreate(BIO* bio)
{
    ManagedSpanBioCtx* ctx = (ManagedSpanBioCtx*)calloc(1, sizeof(ManagedSpanBioCtx));
    if (ctx == NULL)
    {
        return 0;
    }

    BIO_set_data(bio, ctx);
    BIO_set_init(bio, 1);
    return 1;
}

static int ManagedSpanBioDestroy(BIO* bio)
{
    if (bio == NULL)
    {
        return 0;
    }

    ManagedSpanBioCtx* ctx = (ManagedSpanBioCtx*)BIO_get_data(bio);
    if (ctx != NULL)
    {
        free(ctx->readCarry);
        free(ctx->spillBuf);
        free(ctx);
        BIO_set_data(bio, NULL);
    }
    BIO_set_init(bio, 0);
    return 1;
}

static void ManagedSpanBioMethodInit(void)
{
    int index = BIO_get_new_index();
    if (index == -1)
    {
        return;
    }

    BIO_METHOD* method = BIO_meth_new(index | BIO_TYPE_SOURCE_SINK, "dotnet-managed-span");
    if (method == NULL)
    {
        return;
    }

    if (!BIO_meth_set_write(method, ManagedSpanBioWrite) ||
        !BIO_meth_set_read(method, ManagedSpanBioRead) ||
        !BIO_meth_set_ctrl(method, ManagedSpanBioCtrl) ||
        !BIO_meth_set_create(method, ManagedSpanBioCreate) ||
        !BIO_meth_set_destroy(method, ManagedSpanBioDestroy))
    {
        BIO_meth_free(method);
        return;
    }

    g_managedSpanBioMethod = method;
}

static BIO_METHOD* GetManagedSpanBioMethod(void)
{
    pthread_once(&g_managedSpanBioOnce, ManagedSpanBioMethodInit);
    return g_managedSpanBioMethod;
}

BIO* CryptoNative_BioNewManagedSpan(void)
{
    ERR_clear_error();

    BIO_METHOD* method = GetManagedSpanBioMethod();
    if (method == NULL)
    {
        return NULL;
    }

    return BIO_new(method);
}

void CryptoNative_BioSetReadWindow(BIO* bio, const void* ptr, int32_t len)
{
    if (bio == NULL)
    {
        return;
    }

    ManagedSpanBioCtx* ctx = (ManagedSpanBioCtx*)BIO_get_data(bio);
    if (ctx == NULL)
    {
        return;
    }

    ctx->readPtr = (const uint8_t*)ptr;
    ctx->readLen = ptr != NULL ? len : 0;
    ctx->readPos = 0;
}

void CryptoNative_BioClearReadWindow(BIO* bio)
{
    if (bio == NULL)
    {
        return;
    }

    ManagedSpanBioCtx* ctx = (ManagedSpanBioCtx*)BIO_get_data(bio);
    if (ctx == NULL)
    {
        return;
    }

    int32_t unread = ctx->readLen - ctx->readPos;
    if (unread > 0 && ctx->readPtr != NULL)
    {
        /* Move existing carry tail down to position 0 first. */
        int32_t carryTail = ctx->readCarryLen - ctx->readCarryPos;
        if (carryTail > 0 && ctx->readCarryPos > 0)
        {
            memmove(ctx->readCarry, ctx->readCarry + ctx->readCarryPos, (size_t)carryTail);
        }
        ctx->readCarryLen = carryTail;
        ctx->readCarryPos = 0;

        int32_t needed = ctx->readCarryLen + unread;
        if (ManagedSpanBioGrowCarry(ctx, needed))
        {
            memcpy(ctx->readCarry + ctx->readCarryLen, ctx->readPtr + ctx->readPos, (size_t)unread);
            ctx->readCarryLen += unread;
        }
        else
        {
            /* Carry allocation failed; bytes are lost. Mark the BIO as
               permanently broken so the next BIO_read surfaces the failure
               rather than masking it as a protocol error. */
            ctx->readError = 1;
        }
    }

    ctx->readPtr = NULL;
    ctx->readLen = 0;
    ctx->readPos = 0;
}

void CryptoNative_BioSetWriteWindow(BIO* bio, void* ptr, int32_t capacity)
{
    if (bio == NULL)
    {
        return;
    }

    ManagedSpanBioCtx* ctx = (ManagedSpanBioCtx*)BIO_get_data(bio);
    if (ctx == NULL)
    {
        return;
    }

    ctx->writePtr = (uint8_t*)ptr;
    ctx->writeCapacity = ptr != NULL ? capacity : 0;
    ctx->writePos = 0;
}

void CryptoNative_BioGetWriteResult(BIO* bio, int32_t* writtenToWindow, int32_t* spillLen)
{
    if (writtenToWindow != NULL)
    {
        *writtenToWindow = 0;
    }
    if (spillLen != NULL)
    {
        *spillLen = 0;
    }

    if (bio == NULL)
    {
        return;
    }

    ManagedSpanBioCtx* ctx = (ManagedSpanBioCtx*)BIO_get_data(bio);
    if (ctx == NULL)
    {
        return;
    }

    if (writtenToWindow != NULL)
    {
        *writtenToWindow = ctx->writePos;
    }
    if (spillLen != NULL)
    {
        *spillLen = ctx->spillLen;
    }
}

int32_t CryptoNative_BioDrainSpill(BIO* bio, void* dst, int32_t dstLen)
{
    if (bio == NULL || dst == NULL || dstLen <= 0)
    {
        return 0;
    }

    ManagedSpanBioCtx* ctx = (ManagedSpanBioCtx*)BIO_get_data(bio);
    if (ctx == NULL || ctx->spillLen == 0)
    {
        return 0;
    }

    int32_t toCopy = dstLen < ctx->spillLen ? dstLen : ctx->spillLen;
    memcpy(dst, ctx->spillBuf, (size_t)toCopy);

    int32_t remaining = ctx->spillLen - toCopy;
    if (remaining > 0)
    {
        memmove(ctx->spillBuf, ctx->spillBuf + toCopy, (size_t)remaining);
    }
    ctx->spillLen = remaining;
    return toCopy;
}

