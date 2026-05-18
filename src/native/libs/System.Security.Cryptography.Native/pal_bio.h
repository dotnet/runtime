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
Sets the read window on a managed-span BIO. Subsequent BIO_read calls
consume from ptr[0..len). Passing ptr=NULL/len=0 clears the window.
*/
PALEXPORT void CryptoNative_BioSetReadWindow(BIO* bio, const void* ptr, int32_t len);

/*
Clears the read window on a managed-span BIO.
*/
PALEXPORT void CryptoNative_BioClearReadWindow(BIO* bio, int32_t* leftoverLength);

/*
Sets the write window on a managed-span BIO. BIO_write fills this window
first, then spills the remainder to a heap buffer. Passing ptr=NULL/cap=0
clears the window so all writes go to the spill buffer.
*/
PALEXPORT void CryptoNative_BioSetWriteWindow(BIO* bio, void* ptr, int32_t capacity);

/*
Returns the number of bytes written into the window and into the spill
buffer respectively since the last reset/window-set.
*/
PALEXPORT void CryptoNative_BioGetWriteResult(BIO* bio, int32_t* writtenToWindow, int32_t* spillLen);

/*
Drains up to dstLen bytes from the start of the spill buffer into dst,
shifting the rest down. Returns the number of bytes drained.
*/
PALEXPORT int32_t CryptoNative_BioDrainSpill(BIO* bio, void* dst, int32_t dstLen);

