// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdarg.h>
#include <stdlib.h>
#include <stdint.h>

#include "dllexport.h"
#include "jitinterface.h"

DLL_EXPORT int JitCompileMethod(
    CorInfoExceptionClass **ppException,
    ICorJitCompiler * pJit,
    void * thisHandle,
    void ** callbacks,
    CORINFO_METHOD_INFO* methodInfo,
    unsigned flags,
    uint8_t** entryAddress,
    uint32_t* nativeSizeOfCode)
{
    *ppException = nullptr;

    GUID versionId;
    pJit->getVersionIdentifier(&versionId);
    if (memcmp(&versionId, &JITEEVersionIdentifier, sizeof(GUID)) != 0)
    {
        // JIT and the compiler disagree on how the interface looks like.
        // Either get a matching version of the JIT from the CoreCLR repo or update the interface
        // on the CoreRT side. Under no circumstances should you comment this line out.
        return 1;
    }

    try
    {
        JitInterfaceWrapper jitInterfaceWrapper(thisHandle, callbacks);
        return pJit->compileMethod(&jitInterfaceWrapper, methodInfo, flags, entryAddress, nativeSizeOfCode);
    }
    catch (CorInfoExceptionClass *pException)
    {
        *ppException = pException;
    }

    return 1;
}

DLL_EXPORT unsigned GetMaxIntrinsicSIMDVectorLength(
    ICorJitCompiler * pJit,
    CORJIT_FLAGS * flags)
{
    return pJit->getMaxIntrinsicSIMDVectorLength(*flags);
}
