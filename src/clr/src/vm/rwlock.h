// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// 
//+-------------------------------------------------------------------
//
//  File:       RWLock.h
//
//  Contents:   Reader writer lock implementation that supports the
//              following features
//                  1. Cheap enough to be used in large numbers
//                     such as per object synchronization.
//                  2. Supports timeout. This is a valuable feature
//                     to detect deadlocks
//                  3. Supports caching of events. The allows
//                     the events to be moved from least contentious
//                     regions to the most contentious regions.
//                     In other words, the number of events needed by
//                     Reader-Writer lockls is bounded by the number
//                     of threads in the process.
//                  4. Supports nested locks by readers and writers
//                  5. Supports spin counts for avoiding context switches
//                     on  multi processor machines.
//                  6. Supports functionality for upgrading to a writer
//                     lock with a return argument that indicates
//                     intermediate writes. Downgrading from a writer
//                     lock restores the state of the lock.
//                  7. Supports functionality to Release Lock for calling
//                     app code. RestoreLock restores the lock state and
//                     indicates intermediate writes.
//                  8. Recovers from most common failures such as creation of
//                     events. In other words, the lock mainitains consistent
//                     internal state and remains usable
//
//  Classes:    CRWLock,
//              CStaticRWLock
// 
//--------------------------------------------------------------------

#ifdef FEATURE_RWLOCK
#ifndef _RWLOCK_H_
#define _RWLOCK_H_
#include "common.h"
#include "threads.h"

// If you do define this, make sure you define this in managed as well.
//#define RWLOCK_STATISTICS     0

extern DWORD gdwDefaultTimeout;
extern DWORD gdwDefaultSpinCount;


//+-------------------------------------------------------------------
//
//  Struct:     LockCookie
//
//  Synopsis:   Lock cookies returned to the client
//
//+-------------------------------------------------------------------
typedef struct {
    DWORD dwFlags;
    DWORD dwWriterSeqNum;
    WORD wReaderLevel;
    WORD wWriterLevel;
    DWORD dwThreadID;
} LockCookie;

#endif // _RWLOCK_H_

#endif // FEATURE_RWLOCK

