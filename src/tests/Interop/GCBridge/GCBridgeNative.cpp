// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <platformdefines.h>

#include <stdio.h>
#include <stdlib.h>
#include <thread>

struct StronglyConnectedComponent
{
    size_t Count;
    void** ContextMemory;
};

struct ComponentCrossReference
{
    size_t SourceGroupIndex;
    size_t DestinationGroupIndex;
};

struct MarkCrossReferencesArgs
{
    size_t ComponentCount;
    StronglyConnectedComponent* Components;
    size_t CrossReferenceCount;
    ComponentCrossReference* CrossReferences;
};

typedef void (*MarkCrossReferencesFtn)(MarkCrossReferencesArgs*);

static MarkCrossReferencesArgs *markCrossRefsArgs;

MarkCrossReferencesFtn g_bpFinishCallback;

static void BPFinish()
{
    // Dummy java GC
    int ms = rand() % 200;
    printf("native sleeping for %d ms\n", ms);
    std::this_thread::sleep_for(std::chrono::milliseconds(ms));

    // A real callback would also receive the set of unreachable
    // bridge objects so the gchandles for them are freed
    g_bpFinishCallback(markCrossRefsArgs);
}

static void MarkCrossReferences(MarkCrossReferencesArgs* crossRefs)
{
    markCrossRefsArgs = crossRefs;

    std::thread thr(BPFinish);
    thr.detach();
}

extern "C"
DLL_EXPORT MarkCrossReferencesFtn GetMarkCrossReferencesFtn()
{
    return MarkCrossReferences;
}

extern "C"
DLL_EXPORT void SetBridgeProcessingFinishCallback(MarkCrossReferencesFtn callback)
{
    g_bpFinishCallback = callback;
}

