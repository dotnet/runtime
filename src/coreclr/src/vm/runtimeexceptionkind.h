// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// RuntimeExceptionKind.h
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

#endif  // __runtimeexceptionkind_h__
