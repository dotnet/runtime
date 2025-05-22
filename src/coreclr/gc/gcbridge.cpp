// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

static void dyn_ptr_array_init (DynPtrArray* da)
{
    da->size = 0;
    da->capacity = 0;
    da->data = NULL;
}

static int dyn_ptr_array_size (DynPtrArray* da)
{
    return da->size;
}

static void dyn_ptr_array_empty (DynPtrArray* da)
{
    da->size = 0;
}

static void dyn_ptr_array_uninit (DynPtrArray* da)
{
    if (da->capacity < 0)
    {
        dyn_ptr_array_init (da);
        return;
    }

    if (da->capacity == 0)
        return;

    free(da->data);
    da->data = NULL;
}

static char* dyn_ptr_array_ensure_capacity_internal (DynPtrArray* da, int capacity)
{
    if (da->capacity <= 0)
        da->capacity = 2;
    while (capacity > da->capacity)
        da->capacity *= 2;

    return (char*)new (nothrow) void*[da->capacity];
}

static void dyn_ptr_array_ensure_capacity (DynPtrArray* da, int capacity)
{
    int old_capacity = da->capacity;
    char* new_data;

    assert(capacity > 0);

    if (capacity <= old_capacity)
        return;

    new_data = dyn_ptr_array_ensure_capacity_internal(da, capacity);
    assert(new_data);
    memcpy(new_data, da->data, sizeof(void*) * da->size);
    if (old_capacity > 0)
        free(da->data);
    da->data = new_data;
}

static void dyn_ptr_array_add (DynPtrArray* da, void* ptr)
{
    void* p;

    dyn_ptr_array_ensure_capacity(da, da->size + 1);

    p = da->data + da->size * sizeof(void*);
    da->size++;

    *(void**)p = ptr;
}

static void* dyn_ptr_array_get (DynPtrArray* da, int x)
{
    return ((void**)da->data)[x];
}

inline
static void dyn_ptr_array_set (DynPtrArray* da, int x, void* ptr)
{
    ((void**)da->data)[x] = ptr;
}

#define dyn_ptr_array_push dyn_ptr_array_add

static void*
dyn_ptr_array_pop (DynPtrArray* da)
{
    int size = da->size;
    void *p;
    assert(size > 0);
    assert(da->capacity > 1);
    p = dyn_ptr_array_get(da, size - 1);
    da->size--;
    return p;
}

inline
static void dyn_ptr_array_set_all (DynPtrArray* dst, DynPtrArray* src)
{
    const int copysize = src->size;
    if (copysize > 0)
    {
        dyn_ptr_array_ensure_capacity(dst, copysize);

        memcpy(dst->data, src->data, copysize * sizeof (void*));
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

// Used in bridgeless_color_is_heavy:
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
    DynPtrArray other_colors;
    // Bridge objects (Objects) held by objects with this color
    DynPtrArray bridges;
    // Index of this color's MonoGCBridgeSCC in the array passed to the client (or -1 for none)
    signed api_index         : API_INDEX_BITS;
    // Count of colors that list this color in their other_colors
    unsigned incoming_colors : INCOMING_COLORS_BITS;
    unsigned visited : 1;
};

// Represents one managed object. Equivalent of new/old bridge "HashEntry"
struct ScanData
{
    // FIXME this can be eliminated; if we have a ScanData we generally looked it up from its Object
    Object* obj;
    // We use the header in Object to store a pointer to the ScanData. Cache the original here to restore later:
    size_t header_word;

    // Only for bridge objects, stores the context attached to the crossref gc handle
    uintptr_t context;

    ColorData* color;
    // If this object isn't the scc root, we still need to store the xref colors on it, after computing
    // low index, so we can add them to the scc that this object is part of.
    DynPtrArray xrefs;

    // Tarjan algorithm index (order visited)
    int index;
    // Tarjan index of lowest-index object known reachable from here
    signed low_index : 27;

    // See "ScanData state" enum above
    unsigned state : 2;
    unsigned is_bridge : 1;
};

// If true, disable an optimization where sometimes SCC nodes don't contain any bridge objects, in order to reduce total xrefs.
static bool disable_non_bridge_scc;

// Should color be made visible to client even though it has no bridges?
// True if we predict the number of reduced edges to be enough to justify the extra node.

static bool bridgeless_color_is_heavy (ColorData* data)
{
    if (disable_non_bridge_scc)
        return false;
    int fanin = data->incoming_colors;
    int fanout = dyn_ptr_array_size(&data->other_colors);
    return fanin > HEAVY_REFS_MIN && fanout > HEAVY_REFS_MIN
        && fanin*fanout >= HEAVY_COMBINED_REFS_MIN;
}

// Should color be made visible to client?
static bool color_visible_to_client (ColorData* data)
{
    return dyn_ptr_array_size(&data->bridges) || bridgeless_color_is_heavy(data);
}

// Stacks of ScanData objects used for tarjan algorithm.
// The Tarjan algorithm is normally defined recursively; here scan_stack simulates the call stack of a recursive algorithm,
// and loop_stack is the stack structure used by the algorithm itself.
static DynPtrArray scan_stack, loop_stack;

// Objects from crossref handles registered with RegisterBridgeObject
static DynPtrArray registered_bridges;
static DynPtrArray registered_bridges_context;

// As we traverse the graph, which ColorData objects are accessible from our current position?
static DynPtrArray color_merge_array;
// Running hash of the contents of the color_merge_array.
static unsigned int color_merge_array_hash;

static void color_merge_array_empty ()
{
    dyn_ptr_array_empty(&color_merge_array);
    color_merge_array_hash = 0;
}

static int object_index;
static int num_colors_with_bridges;
static int num_sccs;
static int xref_count;

static size_t tarjan_time;

#define BUCKET_SIZE 8184

//ScanData buckets
#define NUM_SCAN_ENTRIES ((BUCKET_SIZE - sizeof(void*) * 2) / sizeof (ScanData))

struct ObjectBucket
{
    ObjectBucket* next;
    ScanData* next_data;
    ScanData data[NUM_SCAN_ENTRIES];
};

static ObjectBucket* root_object_bucket;
static ObjectBucket* cur_object_bucket;
static int object_data_count;

// Arenas to allocate ScanData from
static ObjectBucket* new_object_bucket ()
{
    ObjectBucket* res = new (nothrow) ObjectBucket;
    assert(res);
    res->next = NULL;
    res->next_data = &res->data[0];
    return res;
}

static void object_alloc_init ()
{
    if (!root_object_bucket)
        root_object_bucket = cur_object_bucket = new_object_bucket();
}

static ScanData* alloc_object_data ()
{
    ScanData* res;
retry:

    // next_data points to the first free entry
    res = cur_object_bucket->next_data;
    if (res >= &cur_object_bucket->data[NUM_SCAN_ENTRIES])
    {
        if (cur_object_bucket->next)
        {
            cur_object_bucket = cur_object_bucket->next;
        }
        else
        {
            ObjectBucket* b = new_object_bucket ();
            cur_object_bucket->next = b;
            cur_object_bucket = b;
        }
        goto retry;
    }
    cur_object_bucket->next_data = res + 1;
    object_data_count++;
    new (res) ScanData(); // zero memory
    return res;
}

static void empty_object_buckets ()
{
    ObjectBucket* cur = root_object_bucket;

    object_data_count = 0;

    while (cur)
    {
        cur->next_data = &cur->data[0];
        cur = cur->next;
    }

    cur_object_bucket = root_object_bucket;
}

//ColorData buckets
#define NUM_COLOR_ENTRIES ((BUCKET_SIZE - sizeof(void*) * 2) / sizeof (ColorData))

// Arenas for ColorDatas, same as ObjectBucket except items-per-bucket differs
struct ColorBucket
{
    ColorBucket* next;
    ColorData* next_data;
    ColorData data[NUM_COLOR_ENTRIES];
};

static ColorBucket* root_color_bucket;
static ColorBucket* cur_color_bucket;
static int color_data_count;


static ColorBucket* new_color_bucket ()
{
    ColorBucket* res = new (nothrow) ColorBucket;
    assert(res);
    res->next = NULL;
    res->next_data = &res->data[0];
    return res;
}

static void color_alloc_init ()
{
    if (!root_color_bucket)
        root_color_bucket = cur_color_bucket = new_color_bucket();
}

static ColorData* alloc_color_data ()
{
    ColorData* res;
retry:

    // next_data points to the first free entry
    res = cur_color_bucket->next_data;
    if (res >= &cur_color_bucket->data[NUM_COLOR_ENTRIES])
    {
        if (cur_color_bucket->next)
        {
            cur_color_bucket = cur_color_bucket->next;
        }
        else
        {
            ColorBucket* b = new_color_bucket();
            cur_color_bucket->next = b;
            cur_color_bucket = b;
        }
        goto retry;
    }
    cur_color_bucket->next_data = res + 1;
    color_data_count++;
    new (res) ColorData(); // zero memory
    return res;
}

static void empty_color_buckets ()
{
    ColorBucket* cur;
    ColorBucket* tmp;

    color_data_count = 0;

    for (cur = root_color_bucket; cur; cur = cur->next)
    {
        ColorData* cd;
        for (cd = &cur->data[0]; cd < cur->next_data; cd++)
        {
            dyn_ptr_array_uninit(&cd->other_colors);
            dyn_ptr_array_uninit(&cd->bridges);
        }

        cur->next_data = &cur->data[0];
    }

    cur_color_bucket = root_color_bucket;
}


static ScanData* create_data (Object* obj)
{
    size_t* o = (size_t*)obj;
    ScanData* res = alloc_object_data();
    res->obj = obj;
    res->index = res->low_index = -1;
    res->state = INITIAL;
    res->header_word = o[-1];

    o[0] |= BRIDGE_MARKED_BIT;
    o[-1] = (size_t)res;
    return res;
}

static ScanData* find_data (Object* obj)
{
    ScanData* a = NULL;
    size_t* o = (size_t*)obj;
    if (o[0] & BRIDGE_MARKED_BIT)
        a = (ScanData*)o[-1];
    return a;
}

static void reset_objects_header ()
{
    ObjectBucket* cur;

    for (cur = root_object_bucket; cur; cur = cur->next)
    {
        ScanData* sd;
        for (sd = &cur->data[0]; sd < cur->next_data; sd++)
        {
            size_t *o = (size_t*)sd->obj;
            o[0] &= ~BRIDGE_MARKED_BIT;
            o[-1] = sd->header_word;
        }
    }
}

#ifdef DUMP_GRAPH
static const char* safe_name_bridge (Object* obj)
{
    MethodTable* pMT = obj->GetGCSafeMethodTable();
    return GCToEEInterface::GetMethodTableDebugName(pMT);
}
#endif

struct HashEntry
{
    ColorData* color;
    unsigned int hash;
};

// The merge cache maps an ordered list of ColorDatas [the color_merge_array] to a single ColorData.
// About cache bucket tuning: We tried 2/32, 2/128, 4/32, 4/128, 6/128 and 8/128.
// The performance cost between 4/128 and 8/128 is so small since cache movement happens completely in the same cacheline,
// making the extra space pretty much free.
// The cost between 32 and 128 itens is minimal too, it's mostly a fixed, setup cost.
// Memory wise, 4/32 takes 512 and 8/128 takes 8k, so it's quite reasonable.

#define ELEMENTS_PER_BUCKET 8
#define COLOR_CACHE_SIZE 128
static HashEntry merge_cache[COLOR_CACHE_SIZE][ELEMENTS_PER_BUCKET];
static unsigned int hash_perturb;

// If true, disable an optimization where sometimes SCC nodes are merged without a perfect check
static bool scc_precise_merge;

static unsigned int mix_hash (uintptr_t source)
{
    unsigned int hash = (unsigned int)source;

    // The full hash determines whether two colors can be merged-- sometimes exclusively.
    // This value changes every GC, so XORing it in before performing the hash will make the
    // chance that two different colors will produce the same hash on successive GCs very low.
    hash = hash ^ hash_perturb;

    // Actual hash
    hash = (((hash * 215497) >> 16) ^ ((hash * 1823231) + hash));

    // Mix in highest bits on 64-bit systems only
    if (sizeof (source) > 4)
        hash = hash ^ ((unsigned int)((source >> 31) >> 1));

    return hash;
}

static void reset_cache ()
{
    memset(merge_cache, 0, sizeof (merge_cache));

    // When using the precise merge debug option, we do not want the inconsistency caused by hash_perturb.
    if (!scc_precise_merge)
        hash_perturb++;
}


static bool dyn_ptr_array_contains (DynPtrArray* da, void* x)
{
    int i;
    for (i = 0; i < dyn_ptr_array_size(da); i++)
    {
        if (dyn_ptr_array_get(da, i) == x)
            return true;
    }
    return false;
}

static bool match_colors_estimate (DynPtrArray* a, DynPtrArray* b)
{
    return dyn_ptr_array_size(a) == dyn_ptr_array_size(b);
}


static bool match_colors (DynPtrArray* a, DynPtrArray* b)
{
    int i;
    if (dyn_ptr_array_size(a) != dyn_ptr_array_size(b))
        return false;

    for (i = 0; i < dyn_ptr_array_size(a); i++)
    {
        if (!dyn_ptr_array_contains(b, dyn_ptr_array_get(a, i)))
            return false;
    }
    return true;
}

// If scc_precise_merge, "semihits" refers to find_in_cache calls aborted because the merge array was too large.
// Otherwise "semihits" refers to cache hits where the match was only estimated.
static int cache_hits, cache_semihits, cache_misses;

// The cache contains only non-bridged colors.
static ColorData* find_in_cache (int* insert_index)
{
    HashEntry* bucket;
    int i, size, index;

    size = dyn_ptr_array_size(&color_merge_array);

    // Color equality checking is very expensive with a lot of elements, so when there are many
    // elements we switch to a cheap comparison method which allows false positives. When false
    // positives occur the worst that can happen is two items will be inappropriately merged
    // and memory will be retained longer than it should be. We assume that will correct itself
    // on the next GC (the hash_perturb mechanism increases the probability of this).
    //
    // Because this has *some* potential to create problems, if the user set the debug option
    // 'enable-tarjan-precise-merge' we bail out early (and never merge SCCs with >3 colors).

    bool color_merge_array_large = size > 3;
    if (scc_precise_merge && color_merge_array_large)
    {
        cache_semihits++;
        return NULL;
    }

    unsigned int hash = color_merge_array_hash;
    if (!hash) // 0 is used to indicate an empty bucket entry
        hash = 1;

    index = hash & (COLOR_CACHE_SIZE - 1);
    bucket = merge_cache[index];
    for (i = 0; i < ELEMENTS_PER_BUCKET; i++)
    {
        if (bucket[i].hash != hash)
            continue;

        if (color_merge_array_large)
        {
            if (match_colors_estimate(&bucket[i].color->other_colors, &color_merge_array))
            {
                cache_semihits++;
                return bucket[i].color;
            }
        }
        else
        {
            if (match_colors(&bucket[i].color->other_colors, &color_merge_array))
            {
                cache_hits++;
                return bucket[i].color;
            }
        }
    }

    //move elements to the back
    for (i = ELEMENTS_PER_BUCKET - 1; i > 0; i--)
        bucket[i] = bucket[i - 1];
    cache_misses++;
    *insert_index = index;
    bucket[0].hash = hash;
    return NULL;
}

// Populate other_colors for a give color (other_colors represent the xrefs for this color)
static void add_other_colors (ColorData* color, DynPtrArray* other_colors, bool check_visited)
{
    for (int i = 0; i < dyn_ptr_array_size(other_colors); i++)
    {
        ColorData* points_to = (ColorData*)dyn_ptr_array_get(other_colors, i);
        if (check_visited)
        {
            if (points_to->visited)
                continue;
            points_to->visited = true;
        }
        dyn_ptr_array_add(&color->other_colors, points_to);
        // Inform targets
        points_to->incoming_colors = min(points_to->incoming_colors + 1, INCOMING_COLORS_MAX);
    }
}

// A color is needed for an SCC. If the SCC has bridges, the color MUST be newly allocated.
// If the SCC lacks bridges, the allocator MAY use the cache to merge it with an existing one.
static ColorData* new_color (bool has_bridges)
{
    int cacheSlot = -1;
    ColorData* cd;
    // Try to find an equal one and return it
    if (!has_bridges)
    {
        cd = find_in_cache(&cacheSlot);
        if (cd)
            return cd;
    }

    cd = alloc_color_data();
    cd->api_index = -1;

    add_other_colors(cd, &color_merge_array, false);

    // if cacheSlot >= 0, it means we prepared a given slot to receive the new color
    if (cacheSlot >= 0)
        merge_cache[cacheSlot][0].color = cd;

    return cd;
}


static void register_bridge_object (Object* obj, uintptr_t context)
{
    ScanData *sd = create_data(obj);
    sd->is_bridge = true;
    sd->context = context;
}

// Called during DFS; visits one child. If it is a candidate to be scanned, pushes it to the stacks.
static bool push_object (Object* obj, void* unused)
{
    ScanData* data;

#if DUMP_GRAPH
    printf ("\t= pushing %p %s -> ", obj, safe_name_bridge(obj));
#endif

    data = find_data(obj);

    if (data && data->state != INITIAL)
    {
#if DUMP_GRAPH
        printf ("already marked\n");
#endif
        return true;
    }

    // We only care about dead objects
    if (!data && g_theGCHeap->IsPromoted(obj, false))
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
        data = create_data(obj);
    assert(data->state == INITIAL);
    assert(data->index == -1);
    dyn_ptr_array_push(&scan_stack, data);

    return true;
}

// dfs () function's queue-children-of-object operation.
static void push_all (ScanData* data)
{
    Object* obj = data->obj;

#if DUMP_GRAPH
    printf ("+scanning %s (%p) index %d color %p\n", safe_name_bridge(data->obj), data->obj, data->index, data->color);
#endif

    g_theGCHeap->DiagWalkObject(obj, push_object, NULL);
}

static bool compute_low_index (Object* obj, void* context)
{
    ScanData* data = (ScanData*)context;
    ScanData* other;
    ColorData* cd;

    other = find_data(obj);

    if (!other)
        return true;
#if DUMP_GRAPH
    printf("\tcompute low %p ->%p (%s) %p (%d / %d, color %p)\n", data->obj, obj, safe_name_bridge(obj), other, other ? other->index : -2, other->low_index, other->color);
#endif

    assert(other->state != INITIAL);

    if ((other->state == SCANNED || other->state == FINISHED_ON_STACK) && data->low_index > other->low_index)
        data->low_index = other->low_index;

    // Compute the low color
    if (other->color == NULL)
        return true;

    cd = other->color;

    // The scc for the referenced object was already created, meaning this is an xref.
    // Add it to the color merge array so we can handle it later when creating the scc
    // for the current object (data)
    if (!cd->visited)
    {
        color_merge_array_hash += mix_hash((uintptr_t) other->color);
#if DUMP_GRAPH
        printf("\t\tadd color %p to color_merge_array\n", other->color);
#endif
        dyn_ptr_array_add(&color_merge_array, other->color);
        cd->visited = true;
    }

    return true;
}

static void compute_low (ScanData* data)
{
    Object* obj = data->obj;

    g_theGCHeap->DiagWalkObject(obj, compute_low_index, data);
}

// A non-bridged object needs a single color describing the current merge array.
static ColorData* reduce_color ()
{
    ColorData *color = NULL;
    int size = dyn_ptr_array_size(&color_merge_array);

    // Merge array is empty-- this SCC points to no bridged colors.
    // This SCC can be ignored completely.
    if (size == 0)
        color = NULL;

    // Merge array has one item-- this SCC points to a single bridged color.
    // This SCC can be forwarded to the pointed-to color.
    else if (size == 1)
    {
        // This SCC gets to talk to the color allocator.
        color = (ColorData *)dyn_ptr_array_get(&color_merge_array, 0);
    }
    else
    {
        color = new_color(false);
    }

    return color;
}

static void create_scc (ScanData* data)
{
    int i;
    bool found = false;
    bool found_bridge = false;
    ColorData* color_data = NULL;
    bool can_reduce_color = true;

    for (i = dyn_ptr_array_size (&loop_stack) - 1; i >= 0; i--)
    {
        ScanData* other = (ScanData*)dyn_ptr_array_get(&loop_stack, i);
        found_bridge |= other->is_bridge;
        if (dyn_ptr_array_size(&other->xrefs) > 0 || found_bridge)
        {
            // This scc will have more xrefs than the ones from the color_merge_array,
            // we will need to create a new color to store this information.
            can_reduce_color = false;
        }
        if (found_bridge || other == data)
            break;
    }

    if (found_bridge)
    {
        color_data = new_color(true);
        num_colors_with_bridges++;
    }
    else if (can_reduce_color)
    {
        color_data = reduce_color();
    }
    else
    {
        color_data = new_color(false);
    }
#if DUMP_GRAPH
    printf("|SCC %p rooted in %s (%p) has bridge %d\n", color_data, safe_name_bridge(data->obj), data->obj, found_bridge);
    printf("\tloop stack: ");
    for (i = 0; i < dyn_ptr_array_size(&loop_stack); i++)
    {
        ScanData* other = (ScanData*)dyn_ptr_array_get(&loop_stack, i);
        printf("(%d/%d)", other->index, other->low_index);
    }
    printf("\n");
#endif

    while (dyn_ptr_array_size(&loop_stack) > 0)
    {
        ScanData* other = (ScanData*)dyn_ptr_array_pop(&loop_stack);

#if DUMP_GRAPH
        printf("\tmember %s (%p) index %d low-index %d color %p state %d\n", safe_name_bridge (other->obj), other->obj, other->index, other->low_index, other->color, other->state);
#endif

        other->color = color_data;
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

        if (other->is_bridge)
        {
            assert(color_data);
            dyn_ptr_array_add(&color_data->bridges, other->obj);
        }

        if (dyn_ptr_array_size(&other->xrefs) > 0)
        {
            assert(color_data != NULL);
            assert(can_reduce_color == false);
            // We need to eliminate duplicates early otherwise the heaviness property
            // can change in gather_xrefs and it breaks down the loop that reports the
            // xrefs to the client.
            //
            // We reuse the visited flag to mark the objects that are already part of
            // the color_data array. The array was created above with the new_color call
            // and xrefs were populated from color_merge_array, which is already
            // deduplicated and every entry is marked as visited.
            add_other_colors(color_data, &other->xrefs, true);
        }
        dyn_ptr_array_uninit(&other->xrefs);

        if (other == data)
        {
            found = true;
            break;
        }
    }
    assert(found);

    // Clear the visited flag on nodes that were added with add_other_colors in the loop above
    if (!can_reduce_color)
    {
        for (i = dyn_ptr_array_size(&color_merge_array); i < dyn_ptr_array_size(&color_data->other_colors); i++)
        {
            ColorData *cd = (ColorData *)dyn_ptr_array_get(&color_data->other_colors, i);
            assert(cd->visited);
            cd->visited = false;
        }
    }

#if DUMP_GRAPH
    if (color_data)
    {
        printf("\tpoints-to-colors: ");
        for (i = 0; i < dyn_ptr_array_size(&color_data->other_colors); i++)
            printf("%p ", dyn_ptr_array_get(&color_data->other_colors, i));
        printf("\n");
    }
#endif
}

static void dfs ()
{
    assert(dyn_ptr_array_size(&scan_stack) == 1);
    assert(dyn_ptr_array_size(&loop_stack) == 0);

    color_merge_array_empty();

    while (dyn_ptr_array_size(&scan_stack) > 0)
    {
        ScanData* data = (ScanData*)dyn_ptr_array_pop(&scan_stack);

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
            assert(data->low_index == -1);

            data->state = SCANNED;
            data->low_index = data->index = object_index++;
            dyn_ptr_array_push(&scan_stack, data);
            dyn_ptr_array_push(&loop_stack, data);

            // push all refs
            push_all(data);
        }
        else
        {
            assert(data->state == SCANNED);
            data->state = FINISHED_ON_STACK;

#if DUMP_GRAPH
            printf("-finishing %s (%p) index %d low-index %d color %p\n", safe_name_bridge(data->obj), data->obj, data->index, data->low_index, data->color);
#endif

            // Compute low index
            compute_low(data);

#if DUMP_GRAPH
            printf("-finished %s (%p) index %d low-index %d color %p\n", safe_name_bridge(data->obj), data->obj, data->index, data->low_index, data->color);
#endif
            //SCC root
            if (data->index == data->low_index)
            {
                create_scc(data);
            }
            else
            {
                // We need to clear colo_merge_array from all xrefs. We flush them to the current color
                // and will add them to the scc when we reach the root of the scc.
                assert(dyn_ptr_array_size(&data->xrefs) == 0);
                dyn_ptr_array_set_all(&data->xrefs, &color_merge_array);
            }
            // We populated color_merge_array while scanning the object with each neighbor color. Clear it now
            for (int i = 0; i < dyn_ptr_array_size(&color_merge_array); i++)
            {
                ColorData* cd = (ColorData*)dyn_ptr_array_get(&color_merge_array, i);
                assert(cd->visited);
                cd->visited = false;
            }
            color_merge_array_empty();
        }
    }
}

static void reset_data ()
{
    dyn_ptr_array_empty(&registered_bridges);
}

#ifdef DUMP_GRAPH
static void dump_color_table (const char* why, bool do_index)
{
    ColorBucket* cur;
    int i = 0, j;
    printf("colors%s:\n", why);

    for (cur = root_color_bucket; cur; cur = cur->next, i++)
    {
        ColorData* cd;
        for (cd = &cur->data[0]; cd < cur->next_data; cd++)
        {
            if (do_index)
                printf("\t%d(%d):", i, cd->api_index);
            else
                printf("\t%d: ", i);

            for (j = 0; j < dyn_ptr_array_size(&cd->other_colors); j++)
                printf ("%p ", dyn_ptr_array_get(&cd->other_colors, j));

            if (dyn_ptr_array_size(&cd->bridges))
            {
                printf(" bridges: ");
                for (j = 0; j < dyn_ptr_array_size(&cd->bridges); j++)
                {
                    Object* obj = (Object*)dyn_ptr_array_get(&cd->bridges, j);
                    ScanData* data = find_data(obj);
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

static void gather_xrefs (ColorData* color)
{
    int i;
    for (i = 0; i < dyn_ptr_array_size(&color->other_colors); i++)
    {
        ColorData* src = (ColorData*)dyn_ptr_array_get(&color->other_colors, i);
        if (src->visited)
            continue;
        src->visited = true;
        if (color_visible_to_client(src))
            dyn_ptr_array_add(&color_merge_array, src);
        else
            gather_xrefs(src);
    }
}

static void reset_xrefs (ColorData* color)
{
    int i;
    for (i = 0; i < dyn_ptr_array_size(&color->other_colors); i++)
    {
        ColorData* src = (ColorData*)dyn_ptr_array_get(&color->other_colors, i);
        if (!src->visited)
            continue;
        src->visited = false;
        if (!color_visible_to_client(src))
            reset_xrefs(src);
    }
}

static uint64_t start_time;
static uint64_t after_tarjan_time;

static void bridge_finish ()
{
#if DUMP_GRAPH
    int bridge_count = dyn_ptr_array_size(&registered_bridges);
    int object_count = object_data_count;
    int color_count = color_data_count;
    int colors_with_bridges_count = num_colors_with_bridges;

    uint64_t curtime = GCToOSInterface::GetLowPrecisionTimeStamp();

    printf("GC_TAR_BRIDGE bridges %d objects %d colors %d colors-bridged %d colors-visible %d xref %d cache-hit %d cache-%s %d cache-miss %d tarjan %dms scc-setup %dms\n",
        bridge_count, object_count,
        color_count, colors_with_bridges_count, num_sccs, xref_count,
        cache_hits, (scc_precise_merge ? "abstain" : "semihit"), cache_semihits, cache_misses,
        (int)(after_tarjan_time - start_time),
        (int)(curtime - after_tarjan_time));

    cache_hits = cache_semihits = cache_misses = 0;
#endif
}

void BridgeResetData ()
{
    dyn_ptr_array_empty(&registered_bridges);
    dyn_ptr_array_empty(&registered_bridges_context);
    dyn_ptr_array_empty(&scan_stack);
    dyn_ptr_array_empty(&loop_stack);
    empty_object_buckets();
    empty_color_buckets();
    reset_cache();
    object_index = 0;
    num_colors_with_bridges = 0;
}

void RegisterBridgeObject (Object* object, uintptr_t context)
{
    dyn_ptr_array_add(&registered_bridges, object);
    dyn_ptr_array_add(&registered_bridges_context, (void*)context);
}

uint8_t** GetRegisteredBridges(size_t* pNumBridges)
{
    *pNumBridges = (size_t)registered_bridges.size;
    return (uint8_t**)registered_bridges.data;
}

static bool tarjan_scc_algorithm ()
{
    int i;
    int bridgeCount = dyn_ptr_array_size(&registered_bridges);

    if (!bridgeCount)
        return false;

#if defined (DUMP_GRAPH)
    printf ("-----------------\n");
#endif

    start_time = GCToOSInterface::GetLowPrecisionTimeStamp();

    object_alloc_init ();
    color_alloc_init ();

    for (i = 0; i < bridgeCount; i++)
    {
        Object* obj = (Object*)dyn_ptr_array_get(&registered_bridges, i);
        uintptr_t context = (uintptr_t)dyn_ptr_array_get(&registered_bridges_context, i);

        register_bridge_object(obj, context);
    }

    for (i = 0; i < bridgeCount; i++)
    {
        ScanData* sd = find_data((Object*)dyn_ptr_array_get(&registered_bridges, i));
        if (sd->state == INITIAL)
        {
            dyn_ptr_array_push(&scan_stack, sd);
            dfs();
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
        ScanData* sd = find_data((Object*)dyn_ptr_array_get(&registered_bridges, i));
        printf("\t%s (%p) index %d color %p\n", safe_name_bridge(sd->obj), sd->obj, sd->index, sd->color);
    }

    dump_color_table(" after tarjan", false);
#endif

    after_tarjan_time = GCToOSInterface::GetLowPrecisionTimeStamp();

    return true;
}

static void build_scc_callback_data (BridgeProcessorResult *bp_res)
{
    ColorBucket *cur;
    int j;

#if defined (DUMP_GRAPH)
    printf("***** API *****\n");
    printf("number of SCCs %d\n", num_colors_with_bridges);
#endif

    // Count the number of SCCs visible to the client
    num_sccs = 0;
    for (cur = root_color_bucket; cur; cur = cur->next)
    {
        ColorData* cd;
        for (cd = &cur->data[0]; cd < cur->next_data; cd++)
        {
            if (color_visible_to_client(cd))
                num_sccs++;
        }
    }

    // This is a straightforward translation from colors to the bridge callback format.
    StronglyConnectedComponent* api_sccs = new (nothrow) StronglyConnectedComponent[num_sccs];
    assert(api_sccs);
    int api_index = 0;
    xref_count = 0;

    // Convert visible SCCs, along with their bridged object list, to StronglyConnectedComponent in the client's SCC list
    for (cur = root_color_bucket; cur; cur = cur->next)
    {
        ColorData* cd;
        for (cd = &cur->data[0]; cd < cur->next_data; cd++)
        {
            int bridges = dyn_ptr_array_size(&cd->bridges);
            if (!(bridges || bridgeless_color_is_heavy(cd)))
                continue;

            api_sccs[api_index].Count = bridges;
            uintptr_t *contexts = new (nothrow) uintptr_t[bridges];
            assert(contexts);

            for (j = 0; j < bridges; ++j)
                contexts[j] = find_data((Object*)dyn_ptr_array_get(&cd->bridges, j))->context;

            api_sccs[api_index].Context = contexts;
            cd->api_index = api_index;

            assert(api_index < API_INDEX_MAX);
            api_index++;
        }
    }

    // Eliminate non-visible SCCs from the SCC list and redistribute xrefs
    for (cur = root_color_bucket; cur; cur = cur->next)
    {
        ColorData* cd;
        for (cd = &cur->data[0]; cd < cur->next_data; cd++)
        {
            if (!color_visible_to_client(cd))
                continue;

            color_merge_array_empty();
            gather_xrefs(cd);
            reset_xrefs(cd);
            dyn_ptr_array_set_all(&cd->other_colors, &color_merge_array);
            assert(color_visible_to_client (cd));
            xref_count += dyn_ptr_array_size(&cd->other_colors);
        }
    }

#if defined (DUMP_GRAPH)
    printf("TOTAL XREFS %d\n", xref_count);
    dump_color_table(" after xref pass", true);
#endif

    // Write out xrefs array
    ComponentCrossReference* api_xrefs = new (nothrow) ComponentCrossReference[xref_count];
    assert(api_xrefs);
    int xref_index = 0;
    for (cur = root_color_bucket; cur; cur = cur->next)
    {
        ColorData* src;
        for (src = &cur->data[0]; src < cur->next_data; ++src)
        {
            if (!color_visible_to_client(src))
                continue;

            for (j = 0; j < dyn_ptr_array_size (&src->other_colors); j++)
            {
                ColorData *dest = (ColorData *)dyn_ptr_array_get(&src->other_colors, j);
                // Supposedly we already eliminated all xrefs to non-visible objects
                assert (color_visible_to_client (dest));

                api_xrefs[xref_index].SourceGroupIndex = src->api_index;
                api_xrefs[xref_index].DestinationGroupIndex = dest->api_index;

                xref_index++;
            }
        }
    }

    assert(xref_count == xref_index);

#if defined (DUMP_GRAPH)
    printf("---xrefs:\n");
    for (int i = 0; i < xref_count; i++)
        printf("\t%ld -> %ld\n", api_xrefs[i].SourceGroupIndex, api_xrefs[i].DestinationGroupIndex);
#endif

    bp_res->sccsLen = num_sccs;
    bp_res->sccs = api_sccs;
    bp_res->ccrsLen = xref_count;
    bp_res->ccrs = api_xrefs;
}

BridgeProcessorResult ProcessBridgeObjects()
{
    int i;
    uint64_t curtime;

    BridgeProcessorResult bp_res = { 0 };

    if (!tarjan_scc_algorithm())
        return bp_res;

    build_scc_callback_data(&bp_res);

    reset_objects_header();

    bridge_finish();

    return bp_res;
}
