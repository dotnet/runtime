// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

PALEXPORT int32_t SystemNative_IoUringShimSetup(uint32_t entries, void* params, int32_t* ringFd);

PALEXPORT int32_t SystemNative_IoUringShimEnter(int32_t ringFd, uint32_t toSubmit, uint32_t minComplete, uint32_t flags, int32_t* result);

PALEXPORT int32_t SystemNative_IoUringShimEnterExt(int32_t ringFd, uint32_t toSubmit, uint32_t minComplete, uint32_t flags, void* arg, int32_t* result);

PALEXPORT int32_t SystemNative_IoUringShimRegister(int32_t ringFd, uint32_t opcode, void* arg, uint32_t nrArgs, int32_t* result);

PALEXPORT int32_t SystemNative_IoUringShimMmap(int32_t ringFd, uint64_t size, uint64_t offset, void** mappedPtr);

PALEXPORT int32_t SystemNative_IoUringShimMunmap(void* addr, uint64_t size);

PALEXPORT int32_t SystemNative_IoUringShimCreateEventFd(int32_t* eventFd);

PALEXPORT int32_t SystemNative_IoUringShimWriteEventFd(int32_t eventFd);

PALEXPORT int32_t SystemNative_IoUringShimReadEventFd(int32_t eventFd, uint64_t* value);

PALEXPORT int32_t SystemNative_IoUringShimCloseFd(int32_t fd);
