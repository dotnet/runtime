// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "spmiutil.h"
#include "icorjitcompiler.h"
#include "icorjitinfo.h"
#include "jithost.h"
#include "superpmi-shim-collector.h"

#define fatMC                               // this is nice to have on so ildump works...

void interceptor_ICJC::setTargetOS(CORINFO_OS os)
{
    currentOs = os;
    original_ICorJitCompiler->setTargetOS(os);
}

void interceptor_ICJC::finalizeAndCommitCollection(MethodContext* mc, CorJitResult result, uint8_t* nativeEntry, uint32_t nativeSizeOfCode)
{
    mc->cr->recCompileMethod(&nativeEntry, &nativeSizeOfCode, result);

    if (result == CORJIT_OK)
    {
        mc->cr->recAllocMemCapture();
        mc->cr->recAllocGCInfoCapture();
    }

    mc->saveToFile(hFile);
}

template<typename TPrinter>
static void printInFull(TPrinter print)
{
    size_t requiredSize;
    char buffer[256];
    print(buffer, sizeof(buffer), &requiredSize);

    if (requiredSize > sizeof(buffer))
    {
        std::vector<char> vec(requiredSize);
        print(vec.data(), requiredSize, nullptr);
    }
}

CorJitResult interceptor_ICJC::compileMethod(ICorJitInfo*                comp,     /* IN */
                                             struct CORINFO_METHOD_INFO* info,     /* IN */
                                             unsigned /* code:CorJitFlag */ flags, /* IN */
                                             uint8_t** nativeEntry,                /* OUT */
                                             uint32_t* nativeSizeOfCode            /* OUT */
                                             )
{
    // See if we are filtering the collection (currently by simple substring match)
    //
    if (g_collectionFilter != nullptr)
    {
        bool collect = false;
        const char* className = nullptr;
        const char* methodName = comp->getMethodNameFromMetadata(info->ftn, &className, nullptr, nullptr);
        collect |= (methodName != nullptr) && strstr(methodName, g_collectionFilter) != nullptr;
        collect |= (className != nullptr) && strstr(className, g_collectionFilter) != nullptr;
        if (!collect)
        {
            return original_ICorJitCompiler->compileMethod(comp, info, flags, nativeEntry, nativeSizeOfCode);
        }
    }

    auto* mc = new MethodContext();
    interceptor_ICJI our_ICorJitInfo(this, comp, mc);

    mc->cr->recProcessName(GetCommandLineA());

    mc->recCompileMethod(info, flags, currentOs);

    // force some extra data into our tables..
    // data probably not needed with RyuJIT, but needed in 4.5 and 4.5.1 to help with catching cached values
    our_ICorJitInfo.getBuiltinClass(CLASSID_SYSTEM_OBJECT);
    our_ICorJitInfo.getBuiltinClass(CLASSID_TYPED_BYREF);
    our_ICorJitInfo.getBuiltinClass(CLASSID_TYPE_HANDLE);
    our_ICorJitInfo.getBuiltinClass(CLASSID_FIELD_HANDLE);
    our_ICorJitInfo.getBuiltinClass(CLASSID_METHOD_HANDLE);
    our_ICorJitInfo.getBuiltinClass(CLASSID_STRING);
    our_ICorJitInfo.getBuiltinClass(CLASSID_RUNTIME_TYPE);

#ifdef fatMC
    // to build up a fat mc
    CORINFO_CLASS_HANDLE ourClass = our_ICorJitInfo.getMethodClass(info->ftn);
    our_ICorJitInfo.getClassAttribs(ourClass);
    our_ICorJitInfo.isValueClass(ourClass);
    our_ICorJitInfo.asCorInfoType(ourClass);

    printInFull([&](char* buffer, size_t bufferSize, size_t* requiredBufferSize) { our_ICorJitInfo.printClassName(ourClass, buffer, bufferSize, requiredBufferSize); });
    printInFull([&](char* buffer, size_t bufferSize, size_t* requiredBufferSize) { our_ICorJitInfo.printMethodName(info->ftn, buffer, bufferSize, requiredBufferSize); });
#endif

    // Record data from the global context, if any
    if (g_globalContext != nullptr)
    {
        mc->recGlobalContext(*g_globalContext);
    }

    struct CompileParams
    {
        ICorJitCompiler* origComp;
        interceptor_ICJI* ourICJI;
        struct CORINFO_METHOD_INFO* methodInfo;
        unsigned flags;
        uint8_t** nativeEntry;
        uint32_t* nativeSizeOfCode;
        CorJitResult result;
    } compileParams;

    compileParams.origComp = original_ICorJitCompiler;
    compileParams.ourICJI = &our_ICorJitInfo;
    compileParams.methodInfo = info;
    compileParams.flags = flags;
    compileParams.nativeEntry = nativeEntry;
    compileParams.nativeSizeOfCode = nativeSizeOfCode;
    compileParams.result = CORJIT_INTERNALERROR;

    *nativeEntry = nullptr;
    *nativeSizeOfCode = 0;

    auto doCompile = [mc, our_ICorJitInfo, this, &compileParams]()
    {
        PAL_TRY(CompileParams*, pParam, &compileParams)
        {
            pParam->result = pParam->origComp->compileMethod(
                pParam->ourICJI,
                pParam->methodInfo,
                pParam->flags,
                pParam->nativeEntry,
                pParam->nativeSizeOfCode);
        }
        PAL_FINALLY
        {
            if (!our_ICorJitInfo.SavedCollectionEarly())
            {
                finalizeAndCommitCollection(mc, compileParams.result, *compileParams.nativeEntry, *compileParams.nativeSizeOfCode);
            }

            delete mc;
        }
        PAL_ENDTRY;
    };

    doCompile();

    return compileParams.result;
}

void interceptor_ICJC::ProcessShutdownWork(ICorStaticInfo* info)
{
    original_ICorJitCompiler->ProcessShutdownWork(info);
}

void interceptor_ICJC::getVersionIdentifier(GUID* versionIdentifier /* OUT */)
{
    original_ICorJitCompiler->getVersionIdentifier(versionIdentifier);
}
