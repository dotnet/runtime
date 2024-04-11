// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CDAC_READER_H
#define CDAC_READER_H

#ifdef __cplusplus
extern "C"
{
#endif

int cdac_reader_init(uint64_t descriptor, int(*read_from_target)(uint64_t, uint8_t*, uint32_t, void*), void* read_context, intptr_t* handle);
int cdac_reader_free(intptr_t handle);
int cdac_reader_get_sos_interface(intptr_t handle, IUnknown** obj);

#ifdef __cplusplus
}
#endif

#endif // CDAC_READER_H
