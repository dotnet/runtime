// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// StaticContract.h
// ---------------------------------------------------------------------------

#ifndef __STATIC_CONTRACT_H_
#define __STATIC_CONTRACT_H_

#define STATIC_CONTRACT_THROWS
#define STATIC_CONTRACT_NOTHROW
#define STATIC_CONTRACT_CAN_TAKE_LOCK
#define STATIC_CONTRACT_CANNOT_TAKE_LOCK
#define STATIC_CONTRACT_FAULT
#define STATIC_CONTRACT_FORBID_FAULT
#define STATIC_CONTRACT_GC_TRIGGERS
#define STATIC_CONTRACT_GC_NOTRIGGER

#define STATIC_CONTRACT_SUPPORTS_DAC
#define STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY

#define STATIC_CONTRACT_MODE_COOPERATIVE
#define STATIC_CONTRACT_MODE_PREEMPTIVE
#define STATIC_CONTRACT_MODE_ANY
#define STATIC_CONTRACT_LEAF
#define STATIC_CONTRACT_LIMITED_METHOD
#define STATIC_CONTRACT_WRAPPER

#define STATIC_CONTRACT_ENTRY_POINT

#ifdef _DEBUG
#define STATIC_CONTRACT_DEBUG_ONLY                                  \
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
#else
#define STATIC_CONTRACT_DEBUG_ONLY
#endif

#define STATIC_CONTRACT_VIOLATION(mask)

#define CANNOT_HAVE_CONTRACT

#endif // __STATIC_CONTRACT_H_
