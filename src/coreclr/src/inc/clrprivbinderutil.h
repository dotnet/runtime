// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// Contains helper types for assembly binding host infrastructure.

#ifndef __CLRPRIVBINDERUTIL_H__
#define __CLRPRIVBINDERUTIL_H__

#include "clrprivbinding.h"

//=====================================================================================================================
// Forward declarations
typedef DPTR(ICLRPrivAssembly) PTR_ICLRPrivAssembly;
typedef DPTR(ICLRPrivBinder) PTR_ICLRPrivBinder;

//=====================================================================================================================
#define VALIDATE_CONDITION(condition, fail_op)  \
    do {                                        \
        _ASSERTE((condition));                  \
        if (!(condition))                       \
            fail_op;                            \
    } while (false)

#define VALIDATE_ARG_RET(condition) VALIDATE_CONDITION(condition, return E_INVALIDARG)

#endif // __CLRPRIVBINDERUTIL_H__
