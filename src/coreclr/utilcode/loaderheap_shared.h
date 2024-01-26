#ifndef LOADERHEAP_SHARED
#define LOADERHEAP_SHARED

void ReleaseReservedMemory(BYTE* value);
using ReservedMemoryHolder = SpecializedWrapper<BYTE, ReleaseReservedMemory>;

#ifdef RANDOMIZE_ALLOC
#include <time.h>
static class RandomForLoaderHeap
{
public:
    Random() { seed = (unsigned int)time(NULL); }
    unsigned int Next()
    {
        return ((seed = seed * 214013L + 2531011L) >> 16) & 0x7fff;
    }
private:
    unsigned int seed;
};

extern RandomForLoaderHeap s_randomForLoaderHeap;
#endif



//=====================================================================================
// In DEBUG builds only, we tag live blocks with the requested size and the type of
// allocation (AllocMem, AllocAlignedMem, AllocateOntoReservedMem). This is strictly
// to validate that those who call Backout* are passing in the right values.
//
// For simplicity, we'll use one LoaderHeapValidationTag structure for all types even
// though not all fields are applicable to all types.
//=====================================================================================
#ifdef _DEBUG
enum AllocationType
{
    kAllocMem = 1,
    kFreedMem = 4,
};

struct LoaderHeapValidationTag
{
    size_t         m_dwRequestedSize;      // What the caller requested (not what was actually allocated)
    AllocationType m_allocationType;       // Which api allocated this block.
    const char *   m_szFile;               // Who allocated me
    int            m_lineNum;              // Who allocated me

};
#endif //_DEBUG

//=====================================================================================
// These classes do detailed loaderheap sniffing to help in debugging heap crashes
//=====================================================================================
#ifdef _DEBUG

// This structure logs the results of an Alloc or Free call. They are stored in reverse time order
// with UnlockedLoaderHeap::m_pEventList pointing to the most recent event.
struct LoaderHeapEvent
{
    LoaderHeapEvent *m_pNext;
    AllocationType   m_allocationType;      //Which api was called
    const char      *m_szFile;              //Caller Id
    int              m_lineNum;             //Caller Id
    const char      *m_szAllocFile;         //(BackoutEvents): Who allocated the block?
    int              m_allocLineNum;        //(BackoutEvents): Who allocated the block?
    void            *m_pMem;                //Starting address of block
    size_t           m_dwRequestedSize;     //Requested size of block
    size_t           m_dwSize;              //Actual size of block (including validation tags, padding, everything)


    void Describe(SString *pSString);
    BOOL QuietValidate();
};


class LoaderHeapSniffer
{
    public:
        static DWORD InitDebugFlags();
        static VOID RecordEvent(UnlockedLoaderHeap *pHeap,
                                AllocationType allocationType,
                                _In_ const char *szFile,
                                int            lineNum,
                                _In_ const char *szAllocFile,
                                int            allocLineNum,
                                void          *pMem,
                                size_t         dwRequestedSize,
                                size_t         dwSize
                                );
        static VOID ClearEvents(UnlockedLoaderHeap *pHeap);
        static VOID CompactEvents(UnlockedLoaderHeap *pHeap);
        static VOID PrintEvents(UnlockedLoaderHeap *pHeap);
        static VOID PitchSniffer(SString *pSString);
        static LoaderHeapEvent *FindEvent(UnlockedLoaderHeap *pHeap, void *pAddr);
        static void ValidateFreeList(UnlockedLoaderHeap *pHeap);
        static void WeGotAFaultNowWhat(UnlockedLoaderHeap *pHeap);
};

LoaderHeapValidationTag *AllocMem_GetTag(LPVOID pBlock, size_t dwRequestedSize);

#endif // _DEBUG

#ifdef _DEBUG
#define LOADER_HEAP_BEGIN_TRAP_FAULT BOOL __faulted = FALSE; EX_TRY {
#define LOADER_HEAP_END_TRAP_FAULT   } EX_CATCH {__faulted = TRUE; } EX_END_CATCH(SwallowAllExceptions) if (__faulted) LoaderHeapSniffer::WeGotAFaultNowWhat(pHeap);
#else
#define LOADER_HEAP_BEGIN_TRAP_FAULT
#define LOADER_HEAP_END_TRAP_FAULT
#endif

//=====================================================================================
// This freelist implementation is a first cut and probably needs to be tuned.
// It should be tuned with the following assumptions:
//
//    - Freeing LoaderHeap memory is done primarily for OOM backout. LoaderHeaps
//      weren't designed to be general purpose heaps and shouldn't be used that way.
//
//    - And hence, when memory is freed, expect it to be freed in large clumps and in a
//      LIFO order. Since the LoaderHeap normally hands out memory with sequentially
//      increasing addresses, blocks will typically be freed with sequentially decreasing
//      addresses.
//
// The first cut of the freelist is a single-linked list of free blocks using first-fit.
// Assuming the above alloc-free pattern holds, the list will end up mostly sorted
// in increasing address order. When a block is freed, we'll attempt to coalesce it
// with the first block in the list. We could also choose to be more aggressive about
// sorting and coalescing but this should probably catch most cases in practice.
//=====================================================================================

// When a block is freed, we place this structure on the first bytes of the freed block (Allocations
// are bumped in size if necessary to make sure there's room.)
struct LoaderHeapFreeBlock
{
    public:
        LoaderHeapFreeBlock   *m_pNext;         // Pointer to next block on free list
        size_t                 m_dwSize;        // Total size of this block
        void                  *m_pBlockAddress; // Virtual address of the block

#ifndef DACCESS_COMPILE
        static void InsertFreeBlock(LoaderHeapFreeBlock **ppHead, void *pMem, size_t dwTotalSize, UnlockedLoaderHeap *pHeap);
        static void *AllocFromFreeList(LoaderHeapFreeBlock **ppHead, size_t dwSize, UnlockedLoaderHeap *pHeap);
    private:
        // Try to merge pFreeBlock with its immediate successor. Return TRUE if a merge happened. FALSE if no merge happened.
        static BOOL MergeBlock(LoaderHeapFreeBlock *pFreeBlock, UnlockedLoaderHeap *pHeap);
#endif // DACCESS_COMPILE
};

#endif // LOADERHEAP_SHARED