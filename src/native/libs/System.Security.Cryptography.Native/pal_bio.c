// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_bio.h"

#include <assert.h>
#include <errno.h>
#include <pthread.h>
#include <stdlib.h>
#include <string.h>
#include <sys/socket.h>
#include <sys/types.h>
#include <unistd.h>

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

/*
 * Managed-span BIO
 * ----------------
 *
 * OpenSSL drives TLS by reading ciphertext from an "input BIO" and writing
 * ciphertext to an "output BIO" that the application owns. The stock
 * BIO_s_mem() implementation works, but it always copies the caller's data
 * into an internal heap buffer and then OpenSSL copies again on the way out,
 * resulting in two memcpys per TLS record in each direction.
 *
 * The "managed-span" BIO_METHOD defined below avoids one of those copies in
 * each direction by letting the SSL operation read from / write to a
 * caller-supplied buffer "window" directly. It is paired with the single-shot
 * SSL_{Handshake,Encrypt,Decrypt} entry points in pal_ssl.c which install
 * the windows around an SSL operation and tear them down again afterwards.
 *
 * Lifetimes
 * ~~~~~~~~~
 * The windows are only valid for the duration of a single SSL_* call and
 * are installed via the (non-exported) helpers
 *   CryptoNative_BioSetReadWindow / CryptoNative_BioClearReadWindow
 *   CryptoNative_BioSetWriteWindow / CryptoNative_BioGetWriteResult
 * (see pal_ssl.c). The caller pins/fixes the underlying managed buffers for
 * exactly that scope, then unpins them on return. The BIO context itself
 * outlives the SSL handle and persists across calls; only the {read,write}Ptr
 * fields refer to caller memory while a call is in progress.
 *
 * Read side
 * ~~~~~~~~~
 * BioSetReadWindow records (ptr, len) of the caller's ciphertext span.
 * BIO_read inside SSL copies from there directly (one memcpy, into OpenSSL's
 * record decode buffer). After the SSL call returns, BioClearReadWindow
 * reports how many bytes are still unread (= len - readPos) so the caller
 * knows what to keep in its own buffer for the next round; the window pointer
 * is then cleared. If SSL did not advance the BIO at all (e.g. a renegotiate
 * state-machine quirk that returns SSL_ERROR_NONE without consuming bytes)
 * the unread tail simply stays in the caller's buffer and is re-supplied on
 * the next call - the BIO holds no carry buffer of its own.
 *
 * Write side
 * ~~~~~~~~~~
 * BioSetWriteWindow records (ptr, capacity) of the caller's outgoing-token
 * buffer. BIO_write fills the window first (one memcpy, from OpenSSL's record
 * encode buffer into the caller's span). If OpenSSL produces more output than
 * fits in the window (because our upper-bound estimate was too small, or
 * because alerts / KeyUpdate frames are emitted out-of-band during an
 * SSL_read) the overflow goes into the per-BIO heap "spill" buffer. After the
 * SSL call returns, BioGetWriteResult reports both counts; if any spill is
 * present the caller drains it with BioDrainSpill (one extra memcpy, but only
 * on the rare overflow path).
 *
 * Spill buffer reuse
 * ~~~~~~~~~~~~~~~~~~
 * The spill buffer is owned by the BIO context (allocated lazily, grown by
 * doubling, freed in BioDestroy). It is also the catch-all for output that
 * OpenSSL writes outside an explicit window - notably TLS 1.3 KeyUpdate /
 * post-handshake auth messages emitted while SSL_read is in progress. The
 * managed wrapper drains the spill at the start of the next outgoing SSL
 * operation so those bytes are not lost.
 */

typedef struct
{
    const uint8_t* readPtr;
    int32_t        readLen;
    int32_t        readPos;

    uint8_t*       writePtr;
    int32_t        writeCapacity;
    int32_t        writePos;

    uint8_t*       spillBuf;
    int32_t        spillCapacity;
    int32_t        spillLen;
} ManagedSpanBioCtx;

static ManagedSpanBioCtx* GetManagedSpanBioCtx(BIO* bio)
{
    return (ManagedSpanBioCtx*)BIO_get_data(bio);
}

#define MANAGED_SPAN_SPILL_INITIAL 4096

static BIO_METHOD* g_managedSpanBioMethod = NULL;
static pthread_once_t g_managedSpanBioOnce = PTHREAD_ONCE_INIT;

static int ManagedSpanBioRead(BIO* bio, char* buf, int len)
{
    if (bio == NULL || buf == NULL || len <= 0)
    {
        return 0;
    }

    BIO_clear_retry_flags(bio);

    ManagedSpanBioCtx* ctx = GetManagedSpanBioCtx(bio);
    if (ctx == NULL)
    {
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
    if (bio == NULL || buf == NULL || len < 0)
    {
        return 0;
    }

    BIO_clear_retry_flags(bio);

    if (len == 0)
    {
        return 0;
    }

    ManagedSpanBioCtx* ctx = GetManagedSpanBioCtx(bio);
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
        // Guard against int32 overflow before computing the new spill size.
        // remaining and spillLen are both non-negative int32; bail out if the
        // sum would not fit so ManagedSpanBioGrowSpill cannot be tricked into
        // sizing the buffer based on a wrapped value.
        if (remaining > INT32_MAX - ctx->spillLen)
        {
            return -1;
        }
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
    (void)bio;
    (void)num;
    (void)ptr;

    // OpenSSL only invokes a small set of ctrl commands against this BIO when it is plugged
    // into the SSL state machine via SSL_set_bio. Empirically (verified across the full
    // System.Net.Security test suite) only BIO_CTRL_FLUSH needs a real response. Returning 0
    // from the default case correctly answers BIO_CTRL_PUSH/POP (no-op), the kTLS probes
    // BIO_CTRL_GET_KTLS_SEND/RECV (not supported), and any other future query. We
    // deliberately do not implement BIO_CTRL_RESET: the SSL flows we exercise never issue
    // it, and a real reset would have to either preserve or drop the spill buffer of bytes
    // that have not yet been drained - neither of which is a safe default. Returning 0 for
    // an unhandled command surfaces that explicitly to OpenSSL rather than pretending
    // success.
    if (cmd == BIO_CTRL_FLUSH)
    {
        return 1;
    }
    return 0;
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

    ManagedSpanBioCtx* ctx = GetManagedSpanBioCtx(bio);
    if (ctx != NULL)
    {
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

    ManagedSpanBioCtx* ctx = GetManagedSpanBioCtx(bio);
    if (ctx == NULL)
    {
        return;
    }

    ctx->readPtr = (const uint8_t*)ptr;
    ctx->readLen = ptr != NULL ? len : 0;
    ctx->readPos = 0;
}

void CryptoNative_BioClearReadWindow(BIO* bio, int32_t* leftoverLength)
{
    if (bio == NULL)
    {
        return;
    }

    ManagedSpanBioCtx* ctx = GetManagedSpanBioCtx(bio);
    if (ctx == NULL)
    {
        return;
    }

    if (leftoverLength != NULL)
    {
        *leftoverLength = ctx->readLen - ctx->readPos;
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

    ManagedSpanBioCtx* ctx = GetManagedSpanBioCtx(bio);
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

    ManagedSpanBioCtx* ctx = GetManagedSpanBioCtx(bio);
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

    ManagedSpanBioCtx* ctx = GetManagedSpanBioCtx(bio);
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

/*
 * Socket BIO with a replayable prefix
 * -----------------------------------
 *
 * The deferred-server flow on OpenSSL sockets works like this: the managed
 * TlsSession first uses its buffered (non-fd) path to peek the ClientHello
 * off the socket so SNI is available to a ServerOptionsSelectionCallback.
 * Once the caller resolves options via SetServerOptions, the session installs
 * a real SSL* on the same socket. The ClientHello bytes are already sitting
 * in the managed scratch and must not be re-read from the wire.
 *
 * This BIO holds a heap-owned copy of those prefix bytes. BIO_read drains the
 * prefix first, then delegates to recv() on the stored fd. BIO_write always
 * goes straight to send() on the fd. EAGAIN/EWOULDBLOCK maps to
 * BIO_set_retry_{read,write} so the SSL state machine surfaces WANT_READ /
 * WANT_WRITE and the managed handshake loop can wait for socket readiness.
 *
 * The BIO does not own the fd: the socket lifetime is managed by
 * SafeSocketHandle. Destroy only frees the prefix buffer and the context.
 */

typedef struct
{
    uint8_t* prefix;
    int32_t  prefixLen;
    int32_t  prefixCap;
    int32_t  prefixPos;
    int      fd;
} SocketReplayBioCtx;

static SocketReplayBioCtx* GetSocketReplayBioCtx(BIO* bio)
{
    return (SocketReplayBioCtx*)BIO_get_data(bio);
}

static BIO_METHOD* g_socketReplayBioMethod = NULL;
static pthread_once_t g_socketReplayBioOnce = PTHREAD_ONCE_INIT;

static int SocketReplayBioRead(BIO* bio, char* buf, int len)
{
    if (bio == NULL || buf == NULL || len <= 0)
    {
        return 0;
    }

    BIO_clear_retry_flags(bio);

    SocketReplayBioCtx* ctx = GetSocketReplayBioCtx(bio);
    if (ctx == NULL)
    {
        return -1;
    }

    // Drain any remaining prefix bytes first.
    int32_t available = ctx->prefixLen - ctx->prefixPos;
    if (available > 0)
    {
        int32_t toCopy = len < available ? len : available;
        memcpy(buf, ctx->prefix + ctx->prefixPos, (size_t)toCopy);
        ctx->prefixPos += toCopy;

        // Free the prefix eagerly once fully drained; the BIO becomes a pure
        // socket BIO from this point on.
        if (ctx->prefixPos == ctx->prefixLen)
        {
            free(ctx->prefix);
            ctx->prefix = NULL;
            ctx->prefixLen = 0;
            ctx->prefixPos = 0;
        }
        return toCopy;
    }

    // Prefix exhausted; delegate to the socket.
    if (ctx->fd < 0)
    {
        return -1;
    }

    ssize_t n;
    do
    {
        n = recv(ctx->fd, buf, (size_t)len, 0);
    } while (n < 0 && errno == EINTR);

    if (n > 0)
    {
        return (int)n;
    }

    if (n == 0)
    {
        // Peer closed the connection cleanly. Return 0 without setting retry
        // flags so SSL_get_error reports SSL_ERROR_ZERO_RETURN / SYSCALL.
        return 0;
    }

    if (errno == EAGAIN || errno == EWOULDBLOCK)
    {
        BIO_set_retry_read(bio);
    }
    return -1;
}

static int SocketReplayBioWrite(BIO* bio, const char* buf, int len)
{
    if (bio == NULL || buf == NULL || len < 0)
    {
        return 0;
    }

    BIO_clear_retry_flags(bio);

    if (len == 0)
    {
        return 0;
    }

    SocketReplayBioCtx* ctx = GetSocketReplayBioCtx(bio);
    if (ctx == NULL || ctx->fd < 0)
    {
        return -1;
    }

    ssize_t n;
    do
    {
#ifdef MSG_NOSIGNAL
        n = send(ctx->fd, buf, (size_t)len, MSG_NOSIGNAL);
#else
        n = send(ctx->fd, buf, (size_t)len, 0);
#endif
    } while (n < 0 && errno == EINTR);

    if (n > 0)
    {
        return (int)n;
    }

    if (errno == EAGAIN || errno == EWOULDBLOCK)
    {
        BIO_set_retry_write(bio);
    }
    return -1;
}

static long SocketReplayBioCtrl(BIO* bio, int cmd, long num, void* ptr)
{
    (void)bio;
    (void)num;
    (void)ptr;

    if (cmd == BIO_CTRL_FLUSH)
    {
        return 1;
    }
    return 0;
}

static int SocketReplayBioCreate(BIO* bio)
{
    SocketReplayBioCtx* ctx = (SocketReplayBioCtx*)calloc(1, sizeof(SocketReplayBioCtx));
    if (ctx == NULL)
    {
        return 0;
    }

    ctx->fd = -1;

    BIO_set_data(bio, ctx);
    BIO_set_init(bio, 1);
    return 1;
}

static int SocketReplayBioDestroy(BIO* bio)
{
    if (bio == NULL)
    {
        return 0;
    }

    SocketReplayBioCtx* ctx = GetSocketReplayBioCtx(bio);
    if (ctx != NULL)
    {
        free(ctx->prefix);
        free(ctx);
        BIO_set_data(bio, NULL);
    }
    BIO_set_init(bio, 0);
    return 1;
}

static void SocketReplayBioMethodInit(void)
{
    int index = BIO_get_new_index();
    if (index == -1)
    {
        return;
    }

    BIO_METHOD* method = BIO_meth_new(index | BIO_TYPE_SOURCE_SINK, "dotnet-socket-replay");
    if (method == NULL)
    {
        return;
    }

    if (!BIO_meth_set_write(method, SocketReplayBioWrite) ||
        !BIO_meth_set_read(method, SocketReplayBioRead) ||
        !BIO_meth_set_ctrl(method, SocketReplayBioCtrl) ||
        !BIO_meth_set_create(method, SocketReplayBioCreate) ||
        !BIO_meth_set_destroy(method, SocketReplayBioDestroy))
    {
        BIO_meth_free(method);
        return;
    }

    g_socketReplayBioMethod = method;
}

static BIO_METHOD* GetSocketReplayBioMethod(void)
{
    pthread_once(&g_socketReplayBioOnce, SocketReplayBioMethodInit);
    return g_socketReplayBioMethod;
}

BIO* CryptoNative_BioNewSocketReplay(intptr_t fd, const void* prefix, int32_t prefixLen)
{
    ERR_clear_error();

    if (fd < 0 || prefixLen < 0)
    {
        return NULL;
    }

    BIO_METHOD* method = GetSocketReplayBioMethod();
    if (method == NULL)
    {
        return NULL;
    }

    BIO* bio = BIO_new(method);
    if (bio == NULL)
    {
        return NULL;
    }

    SocketReplayBioCtx* ctx = GetSocketReplayBioCtx(bio);
    assert(ctx != NULL);
    ctx->fd = (int)fd;

    if (prefixLen > 0)
    {
        uint8_t* copy = (uint8_t*)malloc((size_t)prefixLen);
        if (copy == NULL)
        {
            BIO_free(bio);
            return NULL;
        }
        memcpy(copy, prefix, (size_t)prefixLen);
        ctx->prefix = copy;
        ctx->prefixLen = prefixLen;
        ctx->prefixCap = prefixLen;
    }

    return bio;
}

// Reads directly from the BIO's bound fd into the BIO's internal peek buffer until
// a complete TLS record (5-byte header + fragment) is present, or the underlying
// fd would block. The buffered bytes are visible to managed via *outPtr/*outLen
// (span-wrap without a copy) and remain in the BIO for later SocketReplayBioRead
// draining once SSL_do_handshake starts.
//
// Returns:
//   1  = full frame present; *outPtr / *outLen point into the BIO's internal buffer.
//   0  = need more data (fd would block); caller polls SelectRead and retries.
//  -1  = error (invalid args, EOF, oversized record, or recv failure).
//
// Preconditions: BIO is a socket-replay BIO created via BioNewSocketReplay with
// no prefix (empty peek buffer). Calling this after SocketReplayBioRead has begun
// draining the buffer produces undefined framing.
int32_t CryptoNative_BioReadTlsFrame(BIO* bio, uint8_t** outPtr, int32_t* outLen)
{
    ERR_clear_error();

    if (bio == NULL || outPtr == NULL || outLen == NULL)
    {
        return -1;
    }

    SocketReplayBioCtx* ctx = GetSocketReplayBioCtx(bio);
    if (ctx == NULL || ctx->fd < 0)
    {
        return -1;
    }

    // Max TLS record length: 5-byte header + 2^14 payload + 2048 for encryption overhead.
    // ClientHello is unencrypted so the practical max is 5 + 2^14 = 16389, but we allow
    // the encrypted-record ceiling so this shim can be reused for other early-record peeks.
    const int32_t MaxTlsRecord = 5 + (1 << 14) + 2048;

    // Lazy-allocate the peek buffer once, sized to the max record we might see.
    if (ctx->prefix == NULL)
    {
        ctx->prefix = (uint8_t*)malloc((size_t)MaxTlsRecord);
        if (ctx->prefix == NULL)
        {
            return -1;
        }
        ctx->prefixCap = MaxTlsRecord;
        ctx->prefixLen = 0;
        ctx->prefixPos = 0;
    }
    else if (ctx->prefixCap < MaxTlsRecord)
    {
        // Caller supplied a smaller buffer via BioNewSocketReplay; refuse to reuse it
        // as a peek buffer since we can't grow past prefixCap without breaking the
        // pointer contract exposed to managed.
        return -1;
    }

    for (;;)
    {
        int32_t need;
        if (ctx->prefixLen < 5)
        {
            need = 5; // read at least the record header
        }
        else
        {
            // Decode the 2-byte fragment length from the record header (big-endian).
            int32_t fragmentLen = ((int32_t)ctx->prefix[3] << 8) | (int32_t)ctx->prefix[4];
            need = 5 + fragmentLen;
            if (need > ctx->prefixCap)
            {
                return -1; // record too large for our buffer
            }
            if (ctx->prefixLen >= need)
            {
                break; // complete frame
            }
        }

        int32_t want = need - ctx->prefixLen;
        ssize_t n;
        do
        {
            n = recv(ctx->fd, ctx->prefix + ctx->prefixLen, (size_t)want, 0);
        } while (n < 0 && errno == EINTR);

        if (n > 0)
        {
            ctx->prefixLen += (int32_t)n;
            continue;
        }

        if (n == 0)
        {
            return -1; // peer closed before ClientHello
        }

        if (errno == EAGAIN || errno == EWOULDBLOCK)
        {
            return 0; // caller polls and retries
        }

        return -1;
    }

    *outPtr = ctx->prefix;
    *outLen = ctx->prefixLen;
    return 1;
}
