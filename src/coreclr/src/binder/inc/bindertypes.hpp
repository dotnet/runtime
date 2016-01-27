// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// BinderTypes.hpp
//


//
// Declares a bunch of binder classes, types and macros
//
// ============================================================

#ifndef __BINDER_TYPES_HPP__
#define __BINDER_TYPES_HPP__

#include "clrtypes.h"
#include "sstring.h"

#include "fusionhelpers.hpp"

extern void DECLSPEC_NORETURN ThrowOutOfMemory();

#ifndef S_TRUE
#define S_TRUE S_OK
#endif

class PEImage;
class PEAssembly;

namespace BINDER_SPACE
{
    class AssemblyVersion;
    class AssemblyName;
    class Assembly;
    
    class GACEntry;
    class GACVersionIterator;
    class GAC;

    class ContextEntry;
    class ExecutionContext;
    class InspectionContext;

    class PropertyMap;
    class ApplicationContext;

    class BindResult;
    class FailureCache;
    class AssemblyBinder;

#if defined(BINDER_DEBUG_LOG)
    class DebugLog;
#endif

#if defined(FEATURE_VERSIONING_LOG)
    class BindingLog;
    class CDebugLog;
#endif // FEATURE_VERSIONING_LOG

    namespace Tests
    {
        HRESULT Run();
    };
};

#define IF_FAIL_GO(expr)                        \
    hr = (expr);                                \
    if (FAILED(hr))                             \
    {                                           \
        goto Exit;                              \
    }

#define IF_FALSE_GO(expr)                       \
   if (!(expr)) {                               \
       hr = E_FAIL;                             \
       goto Exit;                               \
   }

#define GO_WITH_HRESULT(hrValue)                \
   hr = hrValue;                                \
   goto Exit;

#define IF_WIN32_ERROR_GO(expr)                 \
   if (!(expr))                                 \
   {                                            \
       hr = HRESULT_FROM_GetLastError();        \
       goto Exit;                               \
   }

#define NEW_CONSTR(Object, Constr)              \
    (Object) = new (nothrow) Constr;

#define SAFE_NEW_CONSTR(Object, Constr)         \
    (Object) = new (nothrow) Constr;            \
    if ((Object) == NULL)                       \
    {                                           \
        hr = E_OUTOFMEMORY;                     \
        goto Exit;                              \
    }

#define SAFE_NEW(Object, Class)                 \
    SAFE_NEW_CONSTR(Object, Class());

#define SAFE_RELEASE(objectPtr)                 \
    if ((objectPtr) != NULL)                    \
    {                                           \
        (objectPtr)->Release();                 \
        (objectPtr) = NULL;                     \
    }

#define SAFE_DELETE(objectPtr)                  \
    if ((objectPtr) != NULL)                    \
    {                                           \
        delete (objectPtr);                     \
        (objectPtr) = NULL;                     \
    }

#define SAFE_DELETE_ARRAY(objectPtr)            \
    if ((objectPtr) != NULL)                    \
    {                                           \
        delete[] (objectPtr);                   \
        (objectPtr) = NULL;                     \
    }

#define LENGTH_OF(x)                            \
    (sizeof(x) / sizeof(x[0]))

#include "debuglog.hpp"

#endif
