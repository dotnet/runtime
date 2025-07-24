// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_JAVAMARSHAL

#include "common.h"

#include "gcenv.h"
#include "gc.h"
#include "gcbridge.h"

struct DynPtrArray
{
    int size;
    int capacity;
    char* data;
};

static void DynPtrArrayInit(DynPtrArray* da)
{
    da->size = 0;
    da->capacity = 0;
    da->data = NULL;
}

static int DynPtrArraySize(DynPtrArray* da)
{
    return da->size;
}

static void DynPtrArrayEmpty(DynPtrArray* da)
{
    da->size = 0;
}

static void DynPtrArrayUninit(DynPtrArray* da)
{
    if (da->capacity < 0)
    {
        DynPtrArrayInit(da);
        return;
    }

    if (da->capacity == 0)
        return;

    free(da->data);
    da->data = NULL;
}

static char* DynPtrArrayEnsureCapacityInternal(DynPtrArray* da, int capacity)
{
    if (da->capacity <= 0)
        da->capacity = 2;
    while (capacity > da->capacity)
        da->capacity *= 2;

    return (char*)new (nothrow) void*[da->capacity];
}

static void DynPtrArrayEnsureCapacity(DynPtrArray* da, int capacity)
{
    int oldCapacity = da->capacity;
    char* newData;

    assert(capacity > 0);

    if (capacity <= oldCapacity)
        return;

    newData = DynPtrArrayEnsureCapacityInternal(da, capacity);
    assert(newData);
    memcpy(newData, da->data, sizeof(void*) * da->size);
    if (oldCapacity > 0)
        free(da->data);
    da->data = newData;
}

static void DynPtrArrayAdd(DynPtrArray* da, void* ptr)
{
    void* p;

    DynPtrArrayEnsureCapacity(da, da->size + 1);

    p = da->data + da->size * sizeof(void*);
    da->size++;

    *(void**)p = ptr;
}

static void* DynPtrArrayGet(DynPtrArray* da, int x)
{
    return ((void**)da->data)[x];
}

static void DynPtrArraySet(DynPtrArray* da, int x, void* ptr)
{
    ((void**)da->data)[x] = ptr;
}

#define DynPtrArrayPush DynPtrArrayAdd

static void* DynPtrArrayPop(DynPtrArray* da)
{
    int size = da->size;
    void *p;
    assert(size > 0);
    assert(da->capacity > 1);
    p = DynPtrArrayGet(da, size - 1);
    da->size--;
    return p;
}

static void DynPtrArraySetAll(DynPtrArray* dst, DynPtrArray* src)
{
    const int copySize = src->size;
    if (copySize > 0)
    {
        DynPtrArrayEnsureCapacity(dst, copySize);

        memcpy(dst->data, src->data, copySize * sizeof(void*));
    }
    dst->size = src->size;
}

// This bridge implementation is based on the tarjan algorithm for strongly
// connected components. It has two elements:
//
//   - Standard tarjan SCC algorithm to convert graph to SCC forest
//
//   - "Colors": We reduce the SCC forest to bridged-SCCs-only by using a
//     "color" algorithm devised by Kumpera. Consider the set of bridged SCCs
//     which is reachable from a given object. We call each such unique set a
//     "color". We compute the set of colors and which colors contain links to
//     which colors. The color graph then becomes the reduced SCC graph.

// #define DUMP_GRAPH 1

// Used in BridgelessColorIsHeavy:
// The idea here is as long as the reference fanin and fanout on a node are both 2 or greater, then
// removing that node will result in a net increase in edge count. So the question is which node
// removals are counterproductive (i.e., how many edges saved balances out one node added).
// The number of edges saved by preserving a node is (fanin*fanout - fanin - fanout).
//
// With all that in mind:
//
// - HEAVY_REFS_MIN is the number that *both* fanin and fanout must meet to preserve the node.
// - HEAVY_COMBINED_REFS_MIN is the number (fanin*fanout) must meet to preserve the node.
//
// Note HEAVY_COMBINED_REFS_MIN must be <= 2*INCOMING_COLORS_MAX, or we won't know the true fanin.

#define HEAVY_REFS_MIN 2
#define HEAVY_COMBINED_REFS_MIN 60

// Used in ColorData:
// The higher INCOMING_COLORS_BITS is the higher HEAVY_COMBINED_REFS_MIN can be (see above).
// However, API_INDEX_BITS + INCOMING_COLORS_BITS must be equal to 31, and if API_INDEX_BITS is too
// low then terrible things will happen if too many colors are generated. (The number of colors we
// will ever attempt to generate is currently naturally limited by the JNI GREF limit.)

#define API_INDEX_BITS        26
#define INCOMING_COLORS_BITS  5

#define BRIDGE_MARKED_BIT 2

#define API_INDEX_MAX         ((1<<API_INDEX_BITS)-1)
#define INCOMING_COLORS_MAX   ((1<<INCOMING_COLORS_BITS)-1)

// ScanData state
enum
{
    INITIAL,
    SCANNED,
    FINISHED_ON_STACK,
    FINISHED_OFF_STACK
};

struct ColorData
{
    // Colors (ColorDatas) linked to by objects with this color
    DynPtrArray otherColors;
    // Bridge objects (Objects) held by objects with this color
    DynPtrArray bridges;
    // Index of this color's SCC in the array passed to the client (or -1 for none)
    signed apiIndex         : API_INDEX_BITS;
    // Count of colors that list this color in their otherColors
    unsigned incomingColors : INCOMING_COLORS_BITS;
    unsigned visited : 1;
};

// Represents one managed object. Equivalent of new/old bridge "HashEntry"
struct ScanData
{
    // FIXME this can be eliminated; if we have a ScanData we generally looked it up from its Object
    Object* obj;
    // We use the header in Object to store a pointer to the ScanData. Cache the original here to restore later:
    size_t headerWord;

    // Only for bridge objects, stores the context attached to the crossref gc handle
    uintptr_t context;

    ColorData* color;
    // If this object isn't the scc root, we still need to store the xref colors on it, after computing
    // low index, so we can add them to the scc that this object is part of.
    DynPtrArray xrefs;

    // Tarjan algorithm index (order visited)
    int index;
    // Tarjan index of lowest-index object known reachable from here
    signed lowIndex : 27;

    // See "ScanData state" enum above
    unsigned state : 2;
    unsigned isBridge : 1;
};

// If true, disable an optimization where sometimes SCC nodes don't contain any bridge objects, in order to reduce total xrefs.
static bool DisableNonBridgeScc;

// Should color be made visible to client even though it has no bridges?
// True if we predict the number of reduced edges to be enough to justify the extra node.

static bool BridgelessColorIsHeavy(ColorData* data)
{
    if (DisableNonBridgeScc)
        return false;
    int fanin = data->incomingColors;
    int fanout = DynPtrArraySize(&data->otherColors);
    return fanin > HEAVY_REFS_MIN && fanout > HEAVY_REFS_MIN
        && fanin*fanout >= HEAVY_COMBINED_REFS_MIN;
}

// Should color be made visible to client?
static bool ColorVisibleToClient(ColorData* data)
{
    return DynPtrArraySize(&data->bridges) || BridgelessColorIsHeavy(data);
}

// Stacks of ScanData objects used for tarjan algorithm.
// The Tarjan algorithm is normally defined recursively; here g_scanStack simulates the call stack of a recursive algorithm,
// and g_loopStack is the stack structure used by the algorithm itself.
static DynPtrArray g_scanStack, g_loopStack;

// Objects from crossref handles registered with RegisterBridgeObject
static DynPtrArray g_registeredBridges;
static DynPtrArray g_registeredBridgesContexts;

// As we traverse the graph, which ColorData objects are accessible from our current position?
static DynPtrArray g_colorMergeArray;
// Running hash of the contents of the g_colorMergeArray.
static unsigned int g_colorMergeArrayHash;

static void ColorMergeArrayEmpty()
{
    DynPtrArrayEmpty(&g_colorMergeArray);
    g_colorMergeArrayHash = 0;
}

static int g_objectIndex;
static int g_numColorsWithBridges;
static int g_numSccs;
static int g_xrefCount;

static size_t g_tarjanTime;

#define BUCKET_SIZE 8184

//ScanData buckets
#define NUM_SCAN_ENTRIES ((BUCKET_SIZE - sizeof(void*) * 2) / sizeof (ScanData))

struct ObjectBucket
{
    ObjectBucket* next;
    ScanData* nextData;
    ScanData data[NUM_SCAN_ENTRIES];
};

static ObjectBucket* g_rootObjectBucket;
static ObjectBucket* g_curObjectBucket;
static int g_objectDataCount;

// Arenas to allocate ScanData from
static ObjectBucket* NewObjectBucket()
{
    ObjectBucket* res = new (nothrow) ObjectBucket;
    assert(res);
    res->next = NULL;
    res->nextData = &res->data[0];
    return res;
}

static void ObjectAllocInit()
{
    if (!g_rootObjectBucket)
        g_rootObjectBucket = g_curObjectBucket = NewObjectBucket();
}

static ScanData* AllocObjectData()
{
    ScanData* res;
retry:

    // nextData points to the first free entry
    res = g_curObjectBucket->nextData;
    if (res >= &g_curObjectBucket->data[NUM_SCAN_ENTRIES])
    {
        if (g_curObjectBucket->next)
        {
            g_curObjectBucket = g_curObjectBucket->next;
        }
        else
        {
            ObjectBucket* b = NewObjectBucket();
            g_curObjectBucket->next = b;
            g_curObjectBucket = b;
        }
        goto retry;
    }
    g_curObjectBucket->nextData = res + 1;
    g_objectDataCount++;
    new (res) ScanData(); // zero memory
    return res;
}

static void EmptyObjectBuckets()
{
    ObjectBucket* cur = g_rootObjectBucket;

    g_objectDataCount = 0;

    while (cur)
    {
        cur->nextData = &cur->data[0];
        cur = cur->next;
    }

    g_curObjectBucket = g_rootObjectBucket;
}

//ColorData buckets
#define NUM_COLOR_ENTRIES ((BUCKET_SIZE - sizeof(void*) * 2) / sizeof (ColorData))

// Arenas for ColorDatas, same as ObjectBucket except items-per-bucket differs
struct ColorBucket
{
    ColorBucket* next;
    ColorData* nextData;
    ColorData data[NUM_COLOR_ENTRIES];
};

static ColorBucket* g_rootColorBucket;
static ColorBucket* g_curColorBucket;
static int g_colorDataCount;

static ColorBucket* NewColorBucket()
{
    ColorBucket* res = new (nothrow) ColorBucket;
    assert(res);
    res->next = NULL;
    res->nextData = &res->data[0];
    return res;
}

static void ColorAllocInit()
{
    if (!g_rootColorBucket)
        g_rootColorBucket = g_curColorBucket = NewColorBucket();
}

static ColorData* AllocColorData()
{
    ColorData* res;
retry:

    // nextData points to the first free entry
    res = g_curColorBucket->nextData;
    if (res >= &g_curColorBucket->data[NUM_COLOR_ENTRIES])
    {
        if (g_curColorBucket->next)
        {
            g_curColorBucket = g_curColorBucket->next;
        }
        else
        {
            ColorBucket* b = NewColorBucket();
            g_curColorBucket->next = b;
            g_curColorBucket = b;
        }
        goto retry;
    }
    g_curColorBucket->nextData = res + 1;
    g_colorDataCount++;
    new (res) ColorData(); // zero memory
    return res;
}

static void EmptyColorBuckets()
{
    ColorBucket* cur;

    g_colorDataCount = 0;

    for (cur = g_rootColorBucket; cur; cur = cur->next)
    {
        ColorData* cd;
        for (cd = &cur->data[0]; cd < cur->nextData; cd++)
        {
            DynPtrArrayUninit(&cd->otherColors);
            DynPtrArrayUninit(&cd->bridges);
        }

        cur->nextData = &cur->data[0];
    }

    g_curColorBucket = g_rootColorBucket;
}

static ScanData* CreateData(Object* obj)
{
    size_t* o = (size_t*)obj;
    ScanData* res = AllocObjectData();
    res->obj = obj;
    res->index = res->lowIndex = -1;
    res->state = INITIAL;
    res->headerWord = o[-1];

    o[0] |= BRIDGE_MARKED_BIT;
    o[-1] = (size_t)res;
    return res;
}

static ScanData* FindData(Object* obj)
{
    ScanData* a = NULL;
    size_t* o = (size_t*)obj;
    if (o[0] & BRIDGE_MARKED_BIT)
        a = (ScanData*)o[-1];
    return a;
}

static void ResetObjectsHeader()
{
    ObjectBucket* cur;

    for (cur = g_rootObjectBucket; cur; cur = cur->next)
    {
        ScanData* sd;
        for (sd = &cur->data[0]; sd < cur->nextData; sd++)
        {
            size_t *o = (size_t*)sd->obj;
            o[0] &= ~BRIDGE_MARKED_BIT;
            o[-1] = sd->headerWord;
        }
    }
}

struct HashEntry
{
    ColorData* color;
    unsigned int hash;
};

// The merge cache maps an ordered list of ColorDatas [the g_colorMergeArray] to a single ColorData.
// About cache bucket tuning: We tried 2/32, 2/128, 4/32, 4/128, 6/128 and 8/128.
// The performance cost between 4/128 and 8/128 is so small since cache movement happens completely in the same cacheline,
// making the extra space pretty much free.
// The cost between 32 and 128 itens is minimal too, it's mostly a fixed, setup cost.
// Memory wise, 4/32 takes 512 and 8/128 takes 8k, so it's quite reasonable.

#define ELEMENTS_PER_BUCKET 8
#define COLOR_CACHE_SIZE 128
static HashEntry g_mergeCache[COLOR_CACHE_SIZE][ELEMENTS_PER_BUCKET];
static unsigned int g_hashPerturb;

// If true, disable an optimization where sometimes SCC nodes are merged without a perfect check
static bool g_sccPreciseMerge;

static unsigned int MixHash(uintptr_t source)
{
    unsigned int hash = (unsigned int)source;

    // The full hash determines whether two colors can be merged-- sometimes exclusively.
    // This value changes every GC, so XORing it in before performing the hash will make the
    // chance that two different colors will produce the same hash on successive GCs very low.
    hash = hash ^ g_hashPerturb;

    // Actual hash
    hash = (((hash * 215497) >> 16) ^ ((hash * 1823231) + hash));

    // Mix in highest bits on 64-bit systems only
    if (sizeof (source) > 4)
        hash = hash ^ ((unsigned int)((source >> 31) >> 1));

    return hash;
}

static void ResetCache()
{
    memset(g_mergeCache, 0, sizeof(g_mergeCache));

    // When using the precise merge debug option, we do not want the inconsistency caused by g_hashPerturb.
    if (!g_sccPreciseMerge)
        g_hashPerturb++;
}


static bool DynPtrArrayContains(DynPtrArray* da, void* x)
{
    int i;
    for (i = 0; i < DynPtrArraySize(da); i++)
    {
        if (DynPtrArrayGet(da, i) == x)
            return true;
    }
    return false;
}

static bool MatchColorsEstimate(DynPtrArray* a, DynPtrArray* b)
{
    return DynPtrArraySize(a) == DynPtrArraySize(b);
}

static bool MatchColors(DynPtrArray* a, DynPtrArray* b)
{
    int i;
    if (DynPtrArraySize(a) != DynPtrArraySize(b))
        return false;

    for (i = 0; i < DynPtrArraySize(a); i++)
    {
        if (!DynPtrArrayContains(b, DynPtrArrayGet(a, i)))
            return false;
    }
    return true;
}

// If g_sccPreciseMerge, "semihits" refers to FindInCache calls aborted because the merge array was too large.
// Otherwise "semihits" refers to cache hits where the match was only estimated.
static int g_cacheHits, g_cacheSemiHits, g_cacheMisses;

// The cache contains only non-bridged colors.
static ColorData* FindInCache(int* insertIndex)
{
    HashEntry* bucket;
    int i, size, index;

    size = DynPtrArraySize(&g_colorMergeArray);

    // Color equality checking is very expensive with a lot of elements, so when there are many
    // elements we switch to a cheap comparison method which allows false positives. When false
    // positives occur the worst that can happen is two items will be inappropriately merged
    // and memory will be retained longer than it should be. We assume that will correct itself
    // on the next GC (the g_hashPerturb mechanism increases the probability of this).
    //
    // Because this has *some* potential to create problems, if the user set the debug option
    // 'enable-tarjan-precise-merge' we bail out early (and never merge SCCs with >3 colors).

    bool colorMergeArrayLarge = size > 3;
    if (g_sccPreciseMerge && colorMergeArrayLarge)
    {
        g_cacheSemiHits++;
        return NULL;
    }

    unsigned int hash = g_colorMergeArrayHash;
    if (!hash) // 0 is used to indicate an empty bucket entry
        hash = 1;

    index = hash & (COLOR_CACHE_SIZE - 1);
    bucket = g_mergeCache[index];
    for (i = 0; i < ELEMENTS_PER_BUCKET; i++)
    {
        if (bucket[i].hash != hash)
            continue;

        if (colorMergeArrayLarge)
        {
            if (MatchColorsEstimate(&bucket[i].color->otherColors, &g_colorMergeArray))
            {
                g_cacheSemiHits++;
                return bucket[i].color;
            }
        }
        else
        {
            if (MatchColors(&bucket[i].color->otherColors, &g_colorMergeArray))
            {
                g_cacheHits++;
                return bucket[i].color;
            }
        }
    }

    //move elements to the back
    for (i = ELEMENTS_PER_BUCKET - 1; i > 0; i--)
        bucket[i] = bucket[i - 1];
    g_cacheMisses++;
    *insertIndex = index;
    bucket[0].hash = hash;
    return NULL;
}

// Populate otherColors for a give color (otherColors represent the xrefs for this color)
static void AddOtherColors(ColorData* color, DynPtrArray* otherColors, bool checkVisited)
{
    for (int i = 0; i < DynPtrArraySize(otherColors); i++)
    {
        ColorData* pointsTo = (ColorData*)DynPtrArrayGet(otherColors, i);
        if (checkVisited)
        {
            if (pointsTo->visited)
                continue;
            pointsTo->visited = true;
        }
        DynPtrArrayAdd(&color->otherColors, pointsTo);
        // Inform targets
        if (pointsTo->incomingColors < INCOMING_COLORS_MAX)
            pointsTo->incomingColors++;
    }
}

// A color is needed for an SCC. If the SCC has bridges, the color MUST be newly allocated.
// If the SCC lacks bridges, the allocator MAY use the cache to merge it with an existing one.
static ColorData* NewColor(bool hasBridges)
{
    int cacheSlot = -1;
    ColorData* cd;
    // Try to find an equal one and return it
    if (!hasBridges)
    {
        cd = FindInCache(&cacheSlot);
        if (cd)
            return cd;
    }

    cd = AllocColorData();
    cd->apiIndex = -1;

    AddOtherColors(cd, &g_colorMergeArray, false);

    // if cacheSlot >= 0, it means we prepared a given slot to receive the new color
    if (cacheSlot >= 0)
        g_mergeCache[cacheSlot][0].color = cd;

    return cd;
}


// Called during DFS; visits one child. If it is a candidate to be scanned, pushes it to the stacks.
static bool PushObject(Object* obj, void* unused)
{
    ScanData* data;

#if DUMP_GRAPH
    printf ("\t= pushing %p mt(%p) -> ", obj, obj->GetGCSafeMethodTable());
#endif

    data = FindData(obj);

    if (data && data->state != INITIAL)
    {
#if DUMP_GRAPH
        printf ("already marked\n");
#endif
        return true;
    }

    // We only care about dead objects
    if (!data && g_theGCHeap->IsPromoted2(obj, false))
    {
#if DUMP_GRAPH
        printf ("alive\n");
#endif
        return true;
    }

#if DUMP_GRAPH
    printf ("pushed!\n");
#endif

    if (!data)
        data = CreateData(obj);
    assert(data->state == INITIAL);
    assert(data->index == -1);
    DynPtrArrayPush(&g_scanStack, data);

    return true;
}

// DFS () function's queue-children-of-object operation.
static void PushAll(ScanData* data)
{
    Object* obj = data->obj;

#if DUMP_GRAPH
    printf ("+scanning %p mt(%p) index %d color %p\n", obj, obj->GetGCSafeMethodTable(), data->index, data->color);
#endif

    g_theGCHeap->DiagWalkObject(obj, PushObject, NULL);
}

static bool ComputeLowIndex(Object* obj, void* context)
{
    ScanData* data = (ScanData*)context;
    ScanData* other;
    ColorData* cd;

    other = FindData(obj);

    if (!other)
        return true;
#if DUMP_GRAPH
    printf("\tcompute low %p ->%p mt(%p) %p (%d / %d, color %p)\n", data->obj, obj, obj->GetGCSafeMethodTable(), other, other ? other->index : -2, other->lowIndex, other->color);
#endif

    assert(other->state != INITIAL);

    if ((other->state == SCANNED || other->state == FINISHED_ON_STACK) && data->lowIndex > other->lowIndex)
        data->lowIndex = other->lowIndex;

    // Compute the low color
    if (other->color == NULL)
        return true;

    cd = other->color;

    // The scc for the referenced object was already created, meaning this is an xref.
    // Add it to the color merge array so we can handle it later when creating the scc
    // for the current object (data)
    if (!cd->visited)
    {
        g_colorMergeArrayHash += MixHash((uintptr_t) other->color);
#if DUMP_GRAPH
        printf("\t\tadd color %p to g_colorMergeArray\n", other->color);
#endif
        DynPtrArrayAdd(&g_colorMergeArray, other->color);
        cd->visited = true;
    }

    return true;
}

static void ComputeLow(ScanData* data)
{
    Object* obj = data->obj;

    g_theGCHeap->DiagWalkObject(obj, ComputeLowIndex, data);
}

// A non-bridged object needs a single color describing the current merge array.
static ColorData* ReduceColor()
{
    ColorData *color = NULL;
    int size = DynPtrArraySize(&g_colorMergeArray);

    // Merge array is empty-- this SCC points to no bridged colors.
    // This SCC can be ignored completely.
    if (size == 0)
        color = NULL;

    // Merge array has one item-- this SCC points to a single bridged color.
    // This SCC can be forwarded to the pointed-to color.
    else if (size == 1)
    {
        // This SCC gets to talk to the color allocator.
        color = (ColorData *)DynPtrArrayGet(&g_colorMergeArray, 0);
    }
    else
    {
        color = NewColor(false);
    }

    return color;
}

static void CreateScc(ScanData* data)
{
    int i;
    bool found = false;
    bool foundBridge = false;
    ColorData* colorData = NULL;
    bool canReduceColor = true;

    for (i = DynPtrArraySize(&g_loopStack) - 1; i >= 0; i--)
    {
        ScanData* other = (ScanData*)DynPtrArrayGet(&g_loopStack, i);
        foundBridge |= other->isBridge;
        if (DynPtrArraySize(&other->xrefs) > 0 || foundBridge)
        {
            // This scc will have more xrefs than the ones from the g_colorMergeArray,
            // we will need to create a new color to store this information.
            canReduceColor = false;
        }
        if (foundBridge || other == data)
            break;
    }

    if (foundBridge)
    {
        colorData = NewColor(true);
        g_numColorsWithBridges++;
    }
    else if (canReduceColor)
    {
        colorData = ReduceColor();
    }
    else
    {
        colorData = NewColor(false);
    }
#if DUMP_GRAPH
    printf("|SCC %p rooted in %p mt(%p) has bridge %d\n", colorData, data->obj, data->obj->GetGCSafeMethodTable(), data->obj, foundBridge);
    printf("\tloop stack: ");
    for (i = 0; i < DynPtrArraySize(&g_loopStack); i++)
    {
        ScanData* other = (ScanData*)DynPtrArrayGet(&g_loopStack, i);
        printf("(%d/%d)", other->index, other->lowIndex);
    }
    printf("\n");
#endif

    while (DynPtrArraySize(&g_loopStack) > 0)
    {
        ScanData* other = (ScanData*)DynPtrArrayPop(&g_loopStack);

#if DUMP_GRAPH
        printf("\tmember %p mt(%p) index %d low-index %d color %p state %d\n", other->obj, other->obj->GetGCSafeMethodTable(), other->index, other->lowIndex, other->color, other->state);
#endif

        other->color = colorData;
        switch (other->state)
        {
            case FINISHED_ON_STACK:
                other->state = FINISHED_OFF_STACK;
                break;
            case FINISHED_OFF_STACK:
                break;
            default:
                // Invalid state when building SCC
                assert(0);
        }

        if (other->isBridge)
        {
            assert(colorData);
            DynPtrArrayAdd(&colorData->bridges, other->obj);
        }

        if (DynPtrArraySize(&other->xrefs) > 0)
        {
            assert(colorData != NULL);
            assert(canReduceColor == false);
            // We need to eliminate duplicates early otherwise the heaviness property
            // can change in GatherXRefs and it breaks down the loop that reports the
            // xrefs to the client.
            //
            // We reuse the visited flag to mark the objects that are already part of
            // the colorData array. The array was created above with the NewColor call
            // and xrefs were populated from g_colorMergeArray, which is already
            // deduplicated and every entry is marked as visited.
            AddOtherColors(colorData, &other->xrefs, true);
        }
        DynPtrArrayUninit(&other->xrefs);

        if (other == data)
        {
            found = true;
            break;
        }
    }
    assert(found);

    // Clear the visited flag on nodes that were added with AddOtherColors in the loop above
    if (!canReduceColor)
    {
        for (i = DynPtrArraySize(&g_colorMergeArray); i < DynPtrArraySize(&colorData->otherColors); i++)
        {
            ColorData *cd = (ColorData *)DynPtrArrayGet(&colorData->otherColors, i);
            assert(cd->visited);
            cd->visited = false;
        }
    }

#if DUMP_GRAPH
    if (colorData)
    {
        printf("\tpoints-to-colors: ");
        for (i = 0; i < DynPtrArraySize(&colorData->otherColors); i++)
            printf("%p ", DynPtrArrayGet(&colorData->otherColors, i));
        printf("\n");
    }
#endif
}

static void DFS()
{
    assert(DynPtrArraySize(&g_scanStack) == 1);
    assert(DynPtrArraySize(&g_loopStack) == 0);

    ColorMergeArrayEmpty();

    while (DynPtrArraySize(&g_scanStack) > 0)
    {
        ScanData* data = (ScanData*)DynPtrArrayPop(&g_scanStack);

        // Ignore finished objects on stack, they happen due to loops. For example:
        // A -> C
        // A -> B
        // B -> C
        // C -> A
        //
        // We start scanning from A and push C before B. So, after the first iteration, the scan stack will have: A C B.
        // We then visit B, which will find C in its initial state and push again.
        // Finally after finish with C and B, the stack will be left with "A C" and at this point C should be ignored.
        //
        // The above explains FINISHED_ON_STACK, to explain FINISHED_OFF_STACK, consider if the root was D, which pointed
        // to A and C. A is processed first, leaving C on stack after that in the mentioned state.

        if (data->state == FINISHED_ON_STACK || data->state == FINISHED_OFF_STACK)
            continue;

        if (data->state == INITIAL)
        {
            assert(data->index == -1);
            assert(data->lowIndex == -1);

            data->state = SCANNED;
            data->lowIndex = data->index = g_objectIndex++;
            DynPtrArrayPush(&g_scanStack, data);
            DynPtrArrayPush(&g_loopStack, data);

            // push all refs
            PushAll(data);
        }
        else
        {
            assert(data->state == SCANNED);
            data->state = FINISHED_ON_STACK;

#if DUMP_GRAPH
            printf("-finishing %p mt(%p) index %d low-index %d color %p\n", data->obj, data->obj->GetGCSafeMethodTable(), data->index, data->lowIndex, data->color);
#endif

            // Compute low index
            ComputeLow(data);

#if DUMP_GRAPH
            printf("-finished %p mt(%p) index %d low-index %d color %p\n", data->obj, data->obj->GetGCSafeMethodTable(), data->index, data->lowIndex, data->color);
#endif
            //SCC root
            if (data->index == data->lowIndex)
            {
                CreateScc(data);
            }
            else
            {
                // We need to clear colo_merge_array from all xrefs. We flush them to the current color
                // and will add them to the scc when we reach the root of the scc.
                assert(DynPtrArraySize(&data->xrefs) == 0);
                DynPtrArraySetAll(&data->xrefs, &g_colorMergeArray);
            }
            // We populated g_colorMergeArray while scanning the object with each neighbor color. Clear it now
            for (int i = 0; i < DynPtrArraySize(&g_colorMergeArray); i++)
            {
                ColorData* cd = (ColorData*)DynPtrArrayGet(&g_colorMergeArray, i);
                assert(cd->visited);
                cd->visited = false;
            }
            ColorMergeArrayEmpty();
        }
    }
}

static void ResetData()
{
    DynPtrArrayEmpty(&g_registeredBridges);
}

#ifdef DUMP_GRAPH
static void DumpColorTable(const char* why, bool doIndex)
{
    ColorBucket* cur;
    int i = 0, j;
    printf("colors%s:\n", why);

    for (cur = g_rootColorBucket; cur; cur = cur->next, i++)
    {
        ColorData* cd;
        for (cd = &cur->data[0]; cd < cur->nextData; cd++)
        {
            if (doIndex)
                printf("\t%d(%d):", i, cd->apiIndex);
            else
                printf("\t%d: ", i);

            for (j = 0; j < DynPtrArraySize(&cd->otherColors); j++)
                printf ("%p ", DynPtrArrayGet(&cd->otherColors, j));

            if (DynPtrArraySize(&cd->bridges))
            {
                printf(" bridges: ");
                for (j = 0; j < DynPtrArraySize(&cd->bridges); j++)
                {
                    Object* obj = (Object*)DynPtrArrayGet(&cd->bridges, j);
                    ScanData* data = FindData(obj);
                    if (!data)
                        printf("%p ", obj);
                    else
                        printf("%p(%d) ", obj, data->index);
                }
            }
            printf("\n");
        }
    }
}
#endif

static void GatherXRefs(ColorData* color)
{
    int i;
    for (i = 0; i < DynPtrArraySize(&color->otherColors); i++)
    {
        ColorData* src = (ColorData*)DynPtrArrayGet(&color->otherColors, i);
        if (src->visited)
            continue;
        src->visited = true;
        if (ColorVisibleToClient(src))
            DynPtrArrayAdd(&g_colorMergeArray, src);
        else
            GatherXRefs(src);
    }
}

static void ResetXRefs(ColorData* color)
{
    int i;
    for (i = 0; i < DynPtrArraySize(&color->otherColors); i++)
    {
        ColorData* src = (ColorData*)DynPtrArrayGet(&color->otherColors, i);
        if (!src->visited)
            continue;
        src->visited = false;
        if (!ColorVisibleToClient(src))
            ResetXRefs(src);
    }
}

static uint64_t g_startTime;
static uint64_t g_afterTarjanTime;

static void BridgeFinish()
{
#if DUMP_GRAPH
    int bridgeCount = DynPtrArraySize(&g_registeredBridges);
    int objectCount = g_objectDataCount;
    int colorCount = g_colorDataCount;
    int colorsWithBridgesCount = g_numColorsWithBridges;

    uint64_t curtime = GetHighPrecisionTimeStamp();

    printf("GC_TAR_BRIDGE bridges %d objects %d colors %d colors-bridged %d colors-visible %d xref %d cache-hit %d cache-%s %d cache-miss %d tarjan %dms scc-setup %dms\n",
        bridgeCount, objectCount,
        colorCount, colorsWithBridgesCount, g_numSccs, g_xrefCount,
        g_cacheHits, (g_sccPreciseMerge ? "abstain" : "semihit"), g_cacheSemiHits, g_cacheMisses,
        (int)(g_afterTarjanTime - g_startTime) / 1000,
        (int)(curtime - g_afterTarjanTime) / 1000);

    g_cacheHits = g_cacheSemiHits = g_cacheMisses = 0;
#endif
}

void BridgeResetData()
{
    DynPtrArrayEmpty(&g_registeredBridges);
    DynPtrArrayEmpty(&g_registeredBridgesContexts);
    DynPtrArrayEmpty(&g_scanStack);
    DynPtrArrayEmpty(&g_loopStack);
    EmptyObjectBuckets();
    EmptyColorBuckets();
    ResetCache();
    g_objectIndex = 0;
    g_numColorsWithBridges = 0;
}

void RegisterBridgeObject(Object* object, uintptr_t context)
{
    DynPtrArrayAdd(&g_registeredBridges, object);
    DynPtrArrayAdd(&g_registeredBridgesContexts, (void*)context);
}

uint8_t** GetRegisteredBridges(size_t* pNumBridges)
{
    *pNumBridges = (size_t)g_registeredBridges.size;
    return (uint8_t**)g_registeredBridges.data;
}

static bool TarjanSccAlgorithm()
{
    int i;
    int bridgeCount = DynPtrArraySize(&g_registeredBridges);

    if (!bridgeCount)
        return false;

#if defined (DUMP_GRAPH)
    printf ("-----------------\n");
#endif

    g_startTime = GetHighPrecisionTimeStamp();

    ObjectAllocInit();
    ColorAllocInit();

    for (i = 0; i < bridgeCount; i++)
    {
        Object* obj = (Object*)DynPtrArrayGet(&g_registeredBridges, i);
        uintptr_t context = (uintptr_t)DynPtrArrayGet(&g_registeredBridgesContexts, i);

        ScanData *sd = CreateData(obj);
        sd->isBridge = true;
        sd->context = context;
    }

    for (i = 0; i < bridgeCount; i++)
    {
        ScanData* sd = FindData((Object*)DynPtrArrayGet(&g_registeredBridges, i));
        if (sd->state == INITIAL)
        {
            DynPtrArrayPush(&g_scanStack, sd);
            DFS();
        }
        else
        {
            assert(sd->state == FINISHED_OFF_STACK);
        }
    }

#if defined (DUMP_GRAPH)
    printf("----summary----\n");
    printf("bridges:\n");
    for (i = 0; i < bridgeCount; i++)
    {
        ScanData* sd = FindData((Object*)DynPtrArrayGet(&g_registeredBridges, i));
        printf("\t%p mt(%p) index %d color %p\n", sd->obj, sd->obj->GetGCSafeMethodTable(), sd->index, sd->color);
    }

    DumpColorTable(" after tarjan", false);
#endif

    g_afterTarjanTime = GetHighPrecisionTimeStamp();

    return true;
}

static MarkCrossReferencesArgs* BuildSccCallbackData()
{
    ColorBucket *cur;
    int j;

#if defined (DUMP_GRAPH)
    printf("***** API *****\n");
    printf("number of SCCs %d\n", g_numColorsWithBridges);
#endif

    // Count the number of SCCs visible to the client
    g_numSccs = 0;
    for (cur = g_rootColorBucket; cur; cur = cur->next)
    {
        ColorData* cd;
        for (cd = &cur->data[0]; cd < cur->nextData; cd++)
        {
            if (ColorVisibleToClient(cd))
                g_numSccs++;
        }
    }

    // This is a straightforward translation from colors to the bridge callback format.
    StronglyConnectedComponent* apiSccs = new (nothrow) StronglyConnectedComponent[g_numSccs];
    assert(apiSccs);
    int apiIndex = 0;
    g_xrefCount = 0;

    // Convert visible SCCs, along with their bridged object list, to StronglyConnectedComponent in the client's SCC list
    for (cur = g_rootColorBucket; cur; cur = cur->next)
    {
        ColorData* cd;
        for (cd = &cur->data[0]; cd < cur->nextData; cd++)
        {
            int bridges = DynPtrArraySize(&cd->bridges);
            if (!(bridges || BridgelessColorIsHeavy(cd)))
                continue;

            apiSccs[apiIndex].Count = bridges;
            uintptr_t *contexts = new (nothrow) uintptr_t[bridges];
            assert(contexts);

            for (j = 0; j < bridges; ++j)
                contexts[j] = FindData((Object*)DynPtrArrayGet(&cd->bridges, j))->context;

            apiSccs[apiIndex].Contexts = contexts;
            cd->apiIndex = apiIndex;

            assert(apiIndex < API_INDEX_MAX);
            apiIndex++;
        }
    }

    // Eliminate non-visible SCCs from the SCC list and redistribute xrefs
    for (cur = g_rootColorBucket; cur; cur = cur->next)
    {
        ColorData* cd;
        for (cd = &cur->data[0]; cd < cur->nextData; cd++)
        {
            if (!ColorVisibleToClient(cd))
                continue;

            ColorMergeArrayEmpty();
            GatherXRefs(cd);
            ResetXRefs(cd);
            DynPtrArraySetAll(&cd->otherColors, &g_colorMergeArray);
            assert(ColorVisibleToClient(cd));
            g_xrefCount += DynPtrArraySize(&cd->otherColors);
        }
    }

#if defined (DUMP_GRAPH)
    printf("TOTAL XREFS %d\n", g_xrefCount);
    DumpColorTable(" after xref pass", true);
#endif

    // Write out xrefs array
    ComponentCrossReference* apiXRefs = new (nothrow) ComponentCrossReference[g_xrefCount];
    assert(apiXRefs);
    int xrefIndex = 0;
    for (cur = g_rootColorBucket; cur; cur = cur->next)
    {
        ColorData* src;
        for (src = &cur->data[0]; src < cur->nextData; ++src)
        {
            if (!ColorVisibleToClient(src))
                continue;

            for (j = 0; j < DynPtrArraySize(&src->otherColors); j++)
            {
                ColorData *dest = (ColorData *)DynPtrArrayGet(&src->otherColors, j);
                // Supposedly we already eliminated all xrefs to non-visible objects
                assert (ColorVisibleToClient (dest));

                apiXRefs[xrefIndex].SourceGroupIndex = src->apiIndex;
                apiXRefs[xrefIndex].DestinationGroupIndex = dest->apiIndex;

                xrefIndex++;
            }
        }
    }

    assert(g_xrefCount == xrefIndex);

#if defined (DUMP_GRAPH)
    printf("---xrefs:\n");
    for (int i = 0; i < g_xrefCount; i++)
        printf("\t%ld -> %ld\n", apiXRefs[i].SourceGroupIndex, apiXRefs[i].DestinationGroupIndex);
#endif

    return new (nothrow) MarkCrossReferencesArgs(g_numSccs, apiSccs, g_xrefCount, apiXRefs);
}

MarkCrossReferencesArgs* ProcessBridgeObjects()
{
    if (!TarjanSccAlgorithm())
        return NULL;

    MarkCrossReferencesArgs* args = BuildSccCallbackData();

    ResetObjectsHeader();

    BridgeFinish();

    return args;
}

#endif // FEATURE_JAVAMARSHAL
