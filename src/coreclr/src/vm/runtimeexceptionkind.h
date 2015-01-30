//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// RuntimeExceptionKind.h
//

// 


#ifndef __runtimeexceptionkind_h__
#define __runtimeexceptionkind_h__

//==========================================================================
// Identifies commonly-used exception classes for COMPlusThrowable().
//==========================================================================
enum RuntimeExceptionKind {
#define DEFINE_EXCEPTION(ns, reKind, bHRformessage, ...) k##reKind,
#define DEFINE_EXCEPTION_HR_WINRT_ONLY(ns, reKind, ...)
#define DEFINE_EXCEPTION_IN_OTHER_FX_ASSEMBLY(ns, reKind, assemblySimpleName, publicKeyToken, bHRformessage, ...) DEFINE_EXCEPTION(ns, reKind, bHRformessage, __VA_ARGS__)
#include "rexcep.h"
kLastException
};


// I would have preferred to define a unique HRESULT in our own facility, but we
// weren't supposed to create new HRESULTs so close to ship.  And now it's set
// in stone.
#define E_PROCESS_SHUTDOWN_REENTRY    HRESULT_FROM_WIN32(ERROR_PROCESS_ABORTED)


#endif  // __runtimeexceptionkind_h__
