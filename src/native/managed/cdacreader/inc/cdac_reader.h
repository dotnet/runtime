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
//   read_thread_context: a callback that reads the context of a thread in the target process
//   get_platform: a callback that reads the platform of the target process
//   read_context: a context pointer that will be passed to callbacks
//   handle: returned opaque the handle to the reader. This should be passed to other functions in this API.
int cdac_reader_init(
    uint64_t descriptor,
    int(*read_from_target)(uint64_t, uint8_t*, uint32_t, void*),
    int(*read_thread_context)(uint32_t, uint32_t, uint32_t, uint8_t*, void*),
    int(*get_platform)(uint32_t*, void*),
    void* read_context,
    /*out*/ intptr_t* handle);

// Free the cDAC reader
//   handle: handle to the reader
int cdac_reader_free(intptr_t handle);

// Get the SOS interface from the cDAC reader
//   handle: handle to the reader
//   legacyImpl: optional legacy implementation of the interface tha will be used as a fallback
//   obj: returned SOS interface that can be QI'd to ISOSDacInterface*
int cdac_reader_create_sos_interface(intptr_t handle, IUnknown* legacyImpl, IUnknown** obj);

#ifdef __cplusplus
}
#endif

#endif // CDAC_READER_H
