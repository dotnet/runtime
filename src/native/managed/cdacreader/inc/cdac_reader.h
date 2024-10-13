// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CDAC_READER_H
#define CDAC_READER_H

#ifdef __cplusplus
extern "C"
{
#endif

// Initialize the cDAC reader
//   descriptor: the address of the descriptor in the target process
//   read_from_target: a callback that reads memory from the target process
//   read_context: a context pointer that will be passed to read_from_target
//   handle: returned opaque the handle to the reader. This should be passed to other functions in this API.
int cdac_reader_init(uint64_t descriptor, int(*read_from_target)(uint64_t, uint8_t*, uint32_t, void*), void* read_context, /*out*/ intptr_t* handle);

// Free the cDAC reader
//   handle: handle to the reader
int cdac_reader_free(intptr_t handle);

// Get the SOS interface from the cDAC reader
//   handle: handle to the reader
//   obj: returned SOS interface that can be QI'd to ISOSDacInterface*
int cdac_reader_get_sos_interface(intptr_t handle, IUnknown** obj);

#ifdef __cplusplus
}
#endif

#endif // CDAC_READER_H
