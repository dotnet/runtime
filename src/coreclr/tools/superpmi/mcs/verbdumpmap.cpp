// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include "verbdumpmap.h"
#include "verbildump.h"
#include "spmiutil.h"
#include "spmidumphelper.h"

// Dump the CSV format header for all the columns we're going to dump.
void DumpMapHeader()
{
    printf("index,");
    // printf("process name,");
    printf("method name,");
    printf("full signature,");
    printf("jit flags,");
    printf("os\n");
}

void DumpMap(int index, MethodContext* mc)
{
    CORINFO_METHOD_INFO cmi;
    unsigned int        flags = 0;
    CORINFO_OS          os;

    mc->repCompileMethod(&cmi, &flags, &os);

    const char* moduleName = nullptr;
    const char* methodName = mc->repGetMethodName(cmi.ftn, &moduleName);
    const char* className  = mc->repGetClassName(mc->repGetMethodClass(cmi.ftn));

    printf("%d,", index);
    // printf("\"%s\",", mc->cr->repProcessName());
    printf("%s:%s,", className, methodName);

    // Also, dump the full method signature
    printf("\"");
    DumpAttributeToConsoleBare(mc->repGetMethodAttribs(cmi.ftn));
    DumpPrimToConsoleBare(mc, cmi.args.retType, CastHandle(cmi.args.retTypeClass));
    printf(" %s", methodName);

    // Show class and method generic params, if there are any
    CORINFO_SIG_INFO sig;
    mc->repGetMethodSig(cmi.ftn, &sig, nullptr);

    const unsigned classInst = sig.sigInst.classInstCount;
    if (classInst > 0)
    {
        for (unsigned i = 0; i < classInst; i++)
        {
            CORINFO_CLASS_HANDLE ci = sig.sigInst.classInst[i];
            className = mc->repGetClassName(ci);

            printf("%s%s%s%s",
                i == 0 ? "[" : "",
                i > 0 ? ", " : "",
                className,
                i == classInst - 1 ? "]" : "");
        }
    }

    const unsigned methodInst = sig.sigInst.methInstCount;
    if (methodInst > 0)
    {
        for (unsigned i = 0; i < methodInst; i++)
        {
            CORINFO_CLASS_HANDLE ci = sig.sigInst.methInst[i];
            className = mc->repGetClassName(ci);

            printf("%s%s%s%s",
                i == 0 ? "[" : "",
                i > 0 ? ", " : "",
                className,
                i == methodInst - 1 ? "]" : "");
        }
    }

    printf("(");
    DumpSigToConsoleBare(mc, &cmi.args);
    printf(")\"");

    // Dump the jit flags
    CORJIT_FLAGS corJitFlags;
    mc->repGetJitFlags(&corJitFlags, sizeof(corJitFlags));
    unsigned long long rawFlags = corJitFlags.GetFlagsRaw();

    // Add in the "fake" pgo flags
    bool hasEdgeProfile = false;
    bool hasClassProfile = false;
    bool hasMethodProfile = false;
    bool hasLikelyClass = false;
    ICorJitInfo::PgoSource pgoSource = ICorJitInfo::PgoSource::Unknown;
    if (mc->hasPgoData(hasEdgeProfile, hasClassProfile, hasMethodProfile, hasLikelyClass, pgoSource))
    {
        rawFlags |= 1ULL << (EXTRA_JIT_FLAGS::HAS_PGO);

        if (hasEdgeProfile)
        {
            rawFlags |= 1ULL << (EXTRA_JIT_FLAGS::HAS_EDGE_PROFILE);
        }

        if (hasClassProfile)
        {
            rawFlags |= 1ULL << (EXTRA_JIT_FLAGS::HAS_CLASS_PROFILE);
        }

        if (hasMethodProfile)
        {
            rawFlags |= 1ULL << (EXTRA_JIT_FLAGS::HAS_METHOD_PROFILE);
        }

        if (hasLikelyClass)
        {
            rawFlags |= 1ULL << (EXTRA_JIT_FLAGS::HAS_LIKELY_CLASS);
        }

        if (pgoSource == ICorJitInfo::PgoSource::Static)
        {
            rawFlags |= 1ULL << (EXTRA_JIT_FLAGS::HAS_STATIC_PROFILE);
        }
        
        if (pgoSource == ICorJitInfo::PgoSource::Dynamic)
        {
            rawFlags |= 1ULL << (EXTRA_JIT_FLAGS::HAS_DYNAMIC_PROFILE);
        }
    }

    printf(", %s, %d\n", SpmiDumpHelper::DumpJitFlags(rawFlags).c_str(), (int)os);
}

int verbDumpMap::DoWork(const char* nameOfInput)
{
    MethodContextIterator mci;
    if (!mci.Initialize(nameOfInput))
        return -1;

    DumpMapHeader();

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();
        DumpMap(mci.MethodContextNumber(), mc);
    }

    if (!mci.Destroy())
        return -1;

    return 0;
}
