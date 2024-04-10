// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CDAC_READER_H
#define CDAC_READER_H

#ifdef __cplusplus
extern "C"
{
#endif

int cdac_reader_init(intptr_t descriptor, intptr_t* handle);
int cdac_reader_free(intptr_t handle);
int cdac_reader_get_sos_interface(intptr_t handle, IUnknown** obj);

#ifdef __cplusplus
}
#endif

#endif // CDAC_READER_H
