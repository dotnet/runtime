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

static size_t g_sccsLen;
static StronglyConnectedComponent* g_sccs;
static size_t g_ccrsLen;
static ComponentCrossReference* g_ccrs;

MarkCrossReferencesFtn g_bpFinishCallback;

static void BPFinish()
{
    // Dummy java GC
    int ms = rand() % 200;
    printf("native sleeping for %d ms\n", ms);
    std::this_thread::sleep_for(std::chrono::milliseconds(ms));

    // A real callback would also receive the set of unreachable
    // bridge objects so the gchandles for them are freed
    MarkCrossReferencesArgs crossRefs;
    crossRefs.Components = g_sccs;
    crossRefs.ComponentCount = g_sccsLen;
    crossRefs.CrossReferences = g_ccrs;
    crossRefs.CrossReferenceCount = g_ccrsLen;

    g_bpFinishCallback(&crossRefs);
}

static void MarkCrossReferences(MarkCrossReferencesArgs* crossRefs)
{
    g_sccsLen = crossRefs->ComponentCount;
    g_sccs = crossRefs->Components;
    g_ccrsLen = crossRefs->CrossReferenceCount;
    g_ccrs = crossRefs->CrossReferences;

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

