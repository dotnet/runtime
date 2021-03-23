// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include "verbdumpmap.h"
#include "verbildump.h"
#include "spmiutil.h"

// Dump the CSV format header for all the columns we're going to dump.
void DumpMapHeader()
{
    printf("index,");
    // printf("process name,");
    printf("method name,");
    printf("full signature\n");
}

void DumpMap(int index, MethodContext* mc)
{
    CORINFO_METHOD_INFO cmi;
    unsigned int        flags = 0;

    mc->repCompileMethod(&cmi, &flags);

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
    printf(" %s(", methodName);
    DumpSigToConsoleBare(mc, &cmi.args);
    printf(")\"\n");
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
