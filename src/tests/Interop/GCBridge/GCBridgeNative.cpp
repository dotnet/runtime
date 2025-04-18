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

typedef void (*MarkCrossReferencesFtn)(size_t, StronglyConnectedComponent*, size_t, ComponentCrossReference*);

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
    g_bpFinishCallback(g_sccsLen, g_sccs, g_ccrsLen, g_ccrs);
}

static void MarkCrossReferences(
    size_t sccsLen,
    StronglyConnectedComponent* sccs,
    size_t ccrsLen,
    ComponentCrossReference* ccrs)
{

    g_sccsLen = sccsLen;
    g_sccs = sccs;
    g_ccrsLen = ccrsLen;
    g_ccrs = ccrs;

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

