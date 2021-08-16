// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

namespace BINDER_SPACE
{
    class AssemblyVersion;
    class AssemblyName;
    class Assembly;

    class ContextEntry;
    class ExecutionContext;

    class ApplicationContext;

    class BindResult;
};

typedef enum __AssemblyContentType
{
    AssemblyContentType_Default	        = 0,
    AssemblyContentType_WindowsRuntime  = 0x1,
    AssemblyContentType_Invalid	        = 0xffffffff,
} AssemblyContentType;

typedef enum __ASM_DISPLAY_FLAGS
{
    ASM_DISPLAYF_VERSION                = 0x1,
    ASM_DISPLAYF_CULTURE                = 0x2,
    ASM_DISPLAYF_PUBLIC_KEY_TOKEN       = 0x4,
    ASM_DISPLAYF_PUBLIC_KEY             = 0x8,
    ASM_DISPLAYF_CUSTOM                 = 0x10,
    ASM_DISPLAYF_PROCESSORARCHITECTURE  = 0x20,
    ASM_DISPLAYF_LANGUAGEID             = 0x40,
    ASM_DISPLAYF_RETARGET               = 0x80,
    ASM_DISPLAYF_CONFIG_MASK            = 0x100,
    ASM_DISPLAYF_MVID                   = 0x200,
    ASM_DISPLAYF_CONTENT_TYPE           = 0x400,
    ASM_DISPLAYF_FULL                   = ASM_DISPLAYF_VERSION
                                            | ASM_DISPLAYF_CULTURE
                                            | ASM_DISPLAYF_PUBLIC_KEY_TOKEN
                                            | ASM_DISPLAYF_RETARGET
                                            | ASM_DISPLAYF_PROCESSORARCHITECTURE
                                            | ASM_DISPLAYF_CONTENT_TYPE,
} ASM_DISPLAY_FLAGS;

typedef enum __PEKIND
{
    peNone      = 0x00000000,
    peMSIL      = 0x00000001,
    peI386      = 0x00000002,
    peIA64      = 0x00000003,
    peAMD64     = 0x00000004,
    peARM       = 0x00000005,
    peARM64     = 0x00000006,
    peInvalid   = 0xffffffff,
} PEKIND;

struct AssemblyNameData
{
    LPCSTR Name;
    LPCSTR Culture;

    const BYTE* PublicKeyOrToken;
    DWORD PublicKeyOrTokenLength;

    DWORD MajorVersion;
    DWORD MinorVersion;
    DWORD BuildNumber;
    DWORD RevisionNumber;

    PEKIND ProcessorArchitecture;
    AssemblyContentType ContentType;

    DWORD IdentityFlags;
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

#endif
