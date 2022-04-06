// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Unmanaged GC memory helpers
//

void GCSafeCopyMemoryWithWriteBarrier(void * dest, const void *src, size_t len);

EXTERN_C void REDHAWK_CALLCONV RhpBulkWriteBarrier(void* pMemStart, uint32_t cbMemSize);
