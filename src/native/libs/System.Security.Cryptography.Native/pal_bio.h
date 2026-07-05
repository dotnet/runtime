// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Creates a new memory-backed BIO instance.
*/
PALEXPORT BIO* CryptoNative_CreateMemoryBio(void);

/*
Direct shim to BIO_new_file.
*/
PALEXPORT BIO* CryptoNative_BioNewFile(const char* filename, const char* mode);

/*
Cleans up and deletes a BIO instance.

Implemented by:
1) Calling BIO_free

No-op if a is null.
The given BIO pointer is invalid after this call.
*/
PALEXPORT int32_t CryptoNative_BioDestroy(BIO* a);

/*
Direct shim to BIO_gets.
*/
PALEXPORT int32_t CryptoNative_BioGets(BIO* b, char* buf, int32_t size);

/*
Direct shim to BIO_read.
*/
PALEXPORT int32_t CryptoNative_BioRead(BIO* b, void* buf, int32_t len);

/*
Direct shim to BIO_write.
*/
PALEXPORT int32_t CryptoNative_BioWrite(BIO* b, const void* buf, int32_t len);

/*
Gets the size of data available in the BIO.

Shims the BIO_get_mem_data method.
*/
PALEXPORT int32_t CryptoNative_GetMemoryBioSize(BIO* bio);

/*
Shims the BIO_ctrl_pending method.

Returns the number of pending characters in the BIOs read and write buffers.
*/
PALEXPORT int32_t CryptoNative_BioCtrlPending(BIO* bio);

/*
Creates a new BIO using the managed-span BIO_METHOD that operates on
caller-supplied buffer windows (with a heap spill on write overflow).
*/
PALEXPORT BIO* CryptoNative_BioNewManagedSpan(void);

/*
Internal helpers used by pal_ssl.c to drive the managed-span BIO during
single-shot SSL handshake/encrypt/decrypt operations. Not exported.
*/
void CryptoNative_BioSetReadWindow(BIO* bio, const void* ptr, int32_t len);
void CryptoNative_BioClearReadWindow(BIO* bio, int32_t* leftoverLength);
void CryptoNative_BioSetWriteWindow(BIO* bio, void* ptr, int32_t capacity);

/*
Reports the current state of the managed-span output BIO after an SSL
operation.

writtenToWindow is the number of bytes BIO_write deposited directly into
the current caller-supplied window since the last CryptoNative_BioSetWriteWindow
call. CryptoNative_BioSetWriteWindow resets this counter to zero each time
a new window is installed.

spillLen is the total number of bytes currently held in the per-BIO heap
spill buffer. The spill is *not* reset by CryptoNative_BioSetWriteWindow;
it accumulates across SSL operations (so out-of-band output such as alerts
or TLS 1.3 KeyUpdate frames emitted during SSL_read is preserved for the
caller) and is only drained by CryptoNative_BioDrainSpill.
*/
PALEXPORT void CryptoNative_BioGetWriteResult(BIO* bio, int32_t* writtenToWindow, int32_t* spillLen);

/*
Drains up to dstLen bytes from the start of the spill buffer into dst,
shifting the rest down. Returns the number of bytes drained.
*/
PALEXPORT int32_t CryptoNative_BioDrainSpill(BIO* bio, void* dst, int32_t dstLen);

/*
Creates a new BIO that first replays the provided prefix bytes to any
BIO_read caller, then delegates BIO_read/BIO_write to recv/send on the
supplied socket file descriptor.

Used by the OpenSSL deferred-server flow: the managed TlsSession first
peeks the ClientHello off the socket to run a ServerOptionsSelectionCallback
(so SNI is available), then installs an SSL* whose input BIO replays the
already-consumed ClientHello bytes before touching the wire again. The
prefix bytes are copied into the BIO; the fd is borrowed (the BIO does
not take ownership of it or close it in Destroy).
*/
PALEXPORT BIO* CryptoNative_BioNewSocketReplay(intptr_t fd, const void* prefix, int32_t prefixLen);

/*
Reads directly from a socket-replay BIO's bound fd into its internal peek
buffer until a full TLS record (5-byte header + fragment) is present.
Used by the deferred-server fast path to peek the ClientHello without a
managed pre-fetch buffer + copy round-trip: the same BIO becomes the SSL's
read BIO once SetServerContext/SetServerOptions resumes the handshake.

*outPtr / *outLen point into the BIO's internal buffer and are valid
until the BIO is destroyed or SocketReplayBioRead begins consuming it.

Returns:
   1  = full frame present.
   0  = need more data (fd would block); caller polls SelectRead and retries.
  -1  = error (invalid args, EOF, oversized record, or recv failure).
*/
PALEXPORT int32_t CryptoNative_BioReadTlsFrame(BIO* bio, uint8_t** outPtr, int32_t* outLen);

