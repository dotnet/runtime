// RangeListMap.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <assert.h>
#include <Windows.h>
using namespace std;
class IJitManager;
class Module;
class HeapList;

typedef Module* PTR_Module;
typedef HeapList* PTR_HeapList;
typedef uintptr_t TADDR;
#define TARGET_64BIT






template<typename T>
void VolatileStore(T* ptr, T val)
{
    *ptr = val;
}

template<typename T>
T VolatileLoad(T* ptr)
{
    return *ptr;
}

template<typename T>
T VolatileLoadWithoutBarrier(T* ptr)
{
    return *ptr;
}

class Range
{
public:
    // [begin,end] (This is an inclusive range)
    void* begin;
    void* end;
};

class RangeListMap;

class RangeSection
{
    friend class RangeListMap;
public:
    enum RangeSectionFlags
    {
        RANGE_SECTION_NONE          = 0x0,
        RANGE_SECTION_COLLECTIBLE   = 0x1,
        RANGE_SECTION_CODEHEAP      = 0x2,
    };

#ifdef FEATURE_READYTORUN
    RangeSection(Range range, IJitManager* pJit, RangeSectionFlags flags, PTR_Module pR2RModule) :
        _range(range),
        _flags(flags),
        _pjit(pJit),
        _pR2RModule(pR2RModule),
        _pHeapList(NULL)
    {
        assert(!(flags & RANGE_SECTION_COLLECTIBLE));
        assert(pR2RModule != NULL);
    }
#endif

    RangeSection(Range range, IJitManager* pJit, RangeSectionFlags flags, PTR_HeapList pHeapList) :
        _range(range),
        _flags(flags),
        _pjit(pJit),
        _pR2RModule(NULL),
        _pHeapList(pHeapList)
    {}

    const Range _range;
    const RangeSectionFlags _flags;
    IJitManager *const _pjit;
    const PTR_Module _pR2RModule;
    const PTR_HeapList _pHeapList;

    RangeSection* pRangeListNextForDelete = nullptr; // Used for adding to the cleanup list
};

enum class RangeListLockState
{
    None,
    NeedsLock,
    ReaderLocked,
    WriteLocked,
};

// For 64bit, we work with 8KB chunks of memory holding pointers to the next level. This provides 10 bits of address resolution per level.
// For *reasons* the X64 hardware is limited to 57bits of addressable address space, and the minimum granularity that makes sense for range lists is 64KB (or every 2^16 bits)
// Similarly the Arm64 specification requires addresses to use at most 52 bits. Thus we use the maximum addressable range of X64 to provide the real max range
// So the first level is bits [56:47] -> L4
// Then                       [46:37] -> L3
//                            [36:27] -> L2
//                            [26:17] -> L1
// This leaves 17 bits of the address to be handled by the RangeSection linked list
//
// For 32bit VA processes, use 1KB chunks holding pointers to the next level. This provides 8 bites of address resolution per level.    [31:24] and [23:16].

// The memory safety model for segment maps is that the pointers held within the individual segments can never change other than to go from NULL to a meaningful pointer, 
// except for the final level, which is only permitted to change when CleanupRangeLists is in use.

class RangeListMap
{
    RangeSection* EndOfCleanupListMarker() { return (RangeSection*)1; }

    class RangeSectionFragment;
    class RangeSectionFragmentPointer
    {
    private:
        uintptr_t _ptr;

        uintptr_t FragmentToPtr(RangeSectionFragment* fragment)
        {
            uintptr_t ptr = (uintptr_t)fragment;
            if (ptr == 0)
                return ptr;

            if (fragment->isCollectibleRangeListFragment)
            {
                ptr += 1;
            }

            return ptr;
        }

        RangeSectionFragmentPointer() { _ptr = 0; }
    public:

        RangeSectionFragmentPointer(RangeSectionFragmentPointer &) = delete;
        RangeSectionFragmentPointer(RangeSectionFragmentPointer &&) = delete;
        RangeSectionFragmentPointer& operator=(const RangeSectionFragmentPointer&) = delete;

        bool PointerIsCollectible()
        {
            return ((_ptr & 1) == 1);
        }

        bool IsNull()
        {
            return _ptr == 0;
        }

        RangeSectionFragment* VolatileLoadWithoutBarrier(RangeListLockState *pLockState)
        {
            uintptr_t ptr = ::VolatileLoadWithoutBarrier(&_ptr);
            if ((ptr & 1) == 1)
            {
                if ((*pLockState == RangeListLockState::None) || (*pLockState == RangeListLockState::NeedsLock))
                {
                    *pLockState = RangeListLockState::NeedsLock;
                    return NULL;
                }
                return (RangeSectionFragment*)(ptr - 1);
            }
            else
            {
                return (RangeSectionFragment*)(ptr);
            }
        }

        void VolatileStore(RangeSectionFragment* fragment)
        {
            ::VolatileStore(&_ptr, FragmentToPtr(fragment));
        }

        bool AtomicReplace(RangeSectionFragment* newFragment, RangeSectionFragment* oldFragment)
        {
            uintptr_t oldPtr = FragmentToPtr(oldFragment);
            uintptr_t newPtr = FragmentToPtr(newFragment);

            return oldPtr == (uintptr_t)InterlockedCompareExchangePointer((volatile PVOID*)&_ptr, (PVOID)newPtr, (PVOID)oldPtr);
        }
    };

    // Unlike a RangeSection, a RangeSectionFragment cannot span multiple elements of the last level of the RangeListMap
    // Always allocated via calloc
    class RangeSectionFragment
    {
    public:
        RangeSectionFragmentPointer pRangeListFragmentNext;
        Range _range;
        RangeSection* pRangeList;
        bool InRange(void* address) { return address >= _range.begin && address <= _range.end && pRangeList->pRangeListNextForDelete == NULL; }
        bool isPrimaryRangeListFragment; // RangeSectionFragment are allocated in arrays, but we only need to free the first allocated one. It will be marked with this flag.
        bool isCollectibleRangeListFragment; // RangeSectionFragments
    };

#ifdef TARGET_64BIT
    static const uintptr_t entriesPerMapLevel = 1024;
#else
    static const uintptr_t entriesPerMapLevel = 256;
#endif

    typedef RangeSectionFragmentPointer RangeSectionList;
    typedef RangeSectionList RangeSectionL1[entriesPerMapLevel];
    typedef RangeSectionL1* RangeSectionL2[entriesPerMapLevel];
    typedef RangeSectionL2* RangeSectionL3[entriesPerMapLevel];
    typedef RangeSectionL3* RangeSectionL4[entriesPerMapLevel];

#ifdef TARGET_64BIT
    typedef RangeSectionL4 RangeSectionTopLevel;
    static const uintptr_t mapLevels = 4;
    static const uintptr_t maxSetBit = 56; // This is 0 indexed
    static const uintptr_t bitsPerLevel = 10;
#else
    typedef RangeSectionL2 RangeSectionTopLevel;
    static const uintptr_t mapLevels = 2;
    static const uintptr_t maxSetBit = 31; // This is 0 indexed
    static const uintptr_t bitsPerLevel = 8;
#endif

    RangeSectionTopLevel *_topLevel = nullptr;

    RangeSection* pCleanupList;

    const uintptr_t bitsAtLastLevel = maxSetBit - (bitsPerLevel * mapLevels) + 1;
    const uintptr_t bytesAtLastLevel = (((uintptr_t)1) << (bitsAtLastLevel - 1));

    void* AllocateLevel() { return calloc(entriesPerMapLevel, sizeof(void*)); }

    uintptr_t EffectiveBitsForLevel(void* address, uintptr_t level)
    {
        uintptr_t addressAsInt = (uintptr_t)address;
        uintptr_t addressBitsUsedInMap = addressAsInt >> (maxSetBit - (mapLevels * bitsPerLevel));
        uintptr_t addressBitsShifted = addressBitsUsedInMap >> ((level - 1) * bitsPerLevel);
        uintptr_t addressBitsUsedInLevel = (entriesPerMapLevel - 1) & addressBitsShifted;
        return addressBitsUsedInLevel;
    }

    template<class T>
    auto EnsureLevel(void *address, T* outerLevel, uintptr_t level) -> decltype(&((**outerLevel)[0]))
    {
        uintptr_t index = EffectiveBitsForLevel(address, level);
        auto levelToGetPointerIn = VolatileLoadWithoutBarrier(outerLevel);

        if (levelToGetPointerIn == NULL)
        {
            auto levelNew = static_cast<decltype(&(*outerLevel)[0])>(AllocateLevel());
            if (levelNew == NULL)
                return NULL;
            auto levelPreviouslyStored = (decltype(&(*outerLevel)[0]))InterlockedCompareExchangePointer((volatile PVOID*)outerLevel, (PVOID)levelNew, NULL);
            if (levelPreviouslyStored != nullptr)
            {
                // Handle race where another thread grew the table
                levelToGetPointerIn = levelPreviouslyStored;
                free(levelNew);
            }
            else
            {
                levelToGetPointerIn = levelNew;
            }
            assert(levelToGetPointerIn != nullptr);
        }

        return &((*levelToGetPointerIn)[index]);
    }

    // Returns pointer to address in last level map that actually points at RangeSection space.
    RangeSectionFragmentPointer* EnsureMapsForAddress(void* address)
    {
        uintptr_t level = mapLevels;
#ifdef TARGET_64BIT
        auto _rangeListL3 = EnsureLevel(address, &_topLevel, level);
        if (_rangeListL3 == NULL)
            return NULL; // Failure case
        auto _rangeListL2 = EnsureLevel(address, _rangeListL3, --level);
        if (_rangeListL2 == NULL)
            return NULL; // Failure case
#else
        auto _rangeListL2 = &topLevel;
#endif
        auto _rangeListL1 = EnsureLevel(address, _rangeListL2, --level);
        if (_rangeListL1 == NULL)
            return NULL; // Failure case

        auto result = EnsureLevel(address, _rangeListL1, --level);
        if (result == NULL)
            return NULL; // Failure case

        return result;
    }

    RangeSectionFragment* GetRangeListForAddress(void* address, RangeListLockState *pLockState)
    {
#ifdef TARGET_64BIT
        auto _rangeListL4 = VolatileLoad(&_topLevel);
        auto _rangeListL3 = (*_rangeListL4)[EffectiveBitsForLevel(address, 4)];
        if (_rangeListL3 == NULL)
            return NULL;
        auto _rangeListL2 = (*_rangeListL3)[EffectiveBitsForLevel(address, 3)];
        if (_rangeListL2 == NULL)
            return NULL;
        auto _rangeListL1 = (*_rangeListL2)[EffectiveBitsForLevel(address, 2)];
#else
        auto _rangeListL1 = VolatileLoad(&_topLevel[EffectiveBitsForLevel(address, 2)]); // Use a VolatileLoad on the top level operation to ensure that the entire map is synchronized to a state that includes all data needed to examine currently active function pointers.
#endif
        if (_rangeListL1 == NULL)
            return NULL;

        return ((*_rangeListL1)[EffectiveBitsForLevel(address, 1)]).VolatileLoadWithoutBarrier(pLockState);
    }

    uintptr_t RangeListFragmentCount(RangeSection *pRangeList)
    {
        uintptr_t rangeSize = reinterpret_cast<uintptr_t>(pRangeList->_range.end) - reinterpret_cast<uintptr_t>(pRangeList->_range.begin);
        rangeSize /= bytesAtLastLevel;
        return rangeSize + 1;
    }

    void* IncrementAddressByMaxSizeOfFragment(void* input)
    {
        uintptr_t inputAsInt = reinterpret_cast<uintptr_t>(input);
        return reinterpret_cast<void*>(inputAsInt + bytesAtLastLevel);
    }

    bool AttachRangeListToMap(RangeSection* pRangeList, RangeListLockState *pLockState)
    {
        assert(*pLockState == RangeListLockState::ReaderLocked); // Must be locked so that the cannot fail case, can't fail. NOTE: This only needs the reader lock, as the attach process can happen in parallel to reads.

        uintptr_t rangeListFragmentCount = RangeListFragmentCount(pRangeList);
        RangeSectionFragment* fragments = (RangeSectionFragment*)calloc(rangeListFragmentCount, sizeof(RangeSectionFragment));

        if (fragments == NULL)
        {
            return false;
        }

        RangeSectionFragmentPointer** entriesInMapToUpdate = (RangeSectionFragmentPointer**)calloc(rangeListFragmentCount, sizeof(RangeSectionFragmentPointer*));
        if (entriesInMapToUpdate == NULL)
        {
            free(fragments);
            return false;
        }

        fragments[0].isPrimaryRangeListFragment = true;

        void* addressToPrepForUpdate = pRangeList->_range.begin;
        for (uintptr_t iFragment = 0; iFragment < rangeListFragmentCount; iFragment++)
        {
            fragments[iFragment].pRangeList = pRangeList;
            fragments[iFragment]._range = pRangeList->_range;
            fragments[iFragment].isCollectibleRangeListFragment = !!(pRangeList->_flags & RangeSection::RANGE_SECTION_COLLECTIBLE);
            RangeSectionFragmentPointer* entryInMapToUpdate = EnsureMapsForAddress(addressToPrepForUpdate);
            if (entryInMapToUpdate == NULL)
            {
                free(fragments);
                free(entriesInMapToUpdate);
                return false;
            }

            entriesInMapToUpdate[iFragment] = entryInMapToUpdate;
            addressToPrepForUpdate = IncrementAddressByMaxSizeOfFragment(addressToPrepForUpdate);
        }

        // At this point all the needed memory is allocated, and it is no longer possible to fail.
        for (uintptr_t iFragment = 0; iFragment < rangeListFragmentCount; iFragment++)
        {
            do
            {
                RangeSectionFragment* initialFragmentInMap = entriesInMapToUpdate[iFragment]->VolatileLoadWithoutBarrier(pLockState);
                fragments[iFragment].pRangeListFragmentNext.VolatileStore(initialFragmentInMap);
                if (entriesInMapToUpdate[iFragment]->AtomicReplace(&(fragments[iFragment]), initialFragmentInMap))
                    break;
            } while (true);
        }

        // entriesInMapToUpdate was just a temporary allocation
        free(entriesInMapToUpdate);

        return true;
    }

public:
    RangeListMap() : _topLevel{0}, pCleanupList(EndOfCleanupListMarker())
    {
    }

    bool Init()
    {
        return true;
    }

#ifdef FEATURE_READYTORUN
    RangeSection *AllocateRange(Range range, IJitManager* pJit, RangeSection::RangeSectionFlags flags, PTR_Module pR2RModule, RangeListLockState* pLockState)
    {
        RangeListLockState lockState = RangeListLockState::ReaderLocked;
        RangeSection *pSection = new(nothrow)RangeSection(range, pJit, flags, pR2RModule);
        if (pSection == NULL)
            return NULL;

        if (!AttachRangeListToMap(pSection, pLockState))
        {
            delete pSection;
            return NULL;
        }
        return pSection;
    }
#endif

    RangeSection *AllocateRange(Range range, IJitManager* pJit, RangeSection::RangeSectionFlags flags, PTR_HeapList pHeapList, RangeListLockState* pLockState)
    {
        RangeListLockState lockState = RangeListLockState::ReaderLocked;
        RangeSection *pSection = new(nothrow)RangeSection(range, pJit, flags, pHeapList);
        if (pSection == NULL)
            return NULL;

        if (!AttachRangeListToMap(pSection, pLockState))
        {
            delete pSection;
            return NULL;
        }
        return pSection;
    }

    RangeSection* LookupRangeList(void* address, RangeListLockState *pLockState)
    {
        RangeSectionFragment* fragment = GetRangeListForAddress(address, pLockState);
        if (fragment == NULL)
            return NULL;

        while ((fragment != NULL) && !fragment->InRange(address))
        {
            fragment = fragment->pRangeListFragmentNext.VolatileLoadWithoutBarrier(pLockState);
        }

        if (fragment != NULL)
        {
            if (fragment->pRangeList->pRangeListNextForDelete != NULL)
                return NULL;
            return fragment->pRangeList;
        }

        return NULL;
    }

    void RemoveRangeList(RangeSection* pRangeList)
    {
        assert(pRangeList->pRangeListNextForDelete == nullptr);
        assert(pRangeList->_flags & RangeSection::RANGE_SECTION_COLLECTIBLE);
#ifdef FEATURE_READYTORUN
        assert(pRangeList->pR2RModule == NULL);
#endif

        // Removal is implemented by placing onto the cleanup linked list. This is then processed later during cleanup
        RangeSection* pLatestRemovedRangeList;
        do
        {
            pLatestRemovedRangeList = VolatileLoad(&pCleanupList);
            VolatileStore(&pRangeList->pRangeListNextForDelete, pLatestRemovedRangeList);
        } while (InterlockedCompareExchangePointer((volatile PVOID *)&pCleanupList, pRangeList, pLatestRemovedRangeList) != pLatestRemovedRangeList);
    }

    void CleanupRangeLists(RangeListLockState *pLockState)
    {
        assert(*pLockState == RangeListLockState::WriteLocked);

        while (this->pCleanupList != EndOfCleanupListMarker())
        {
            RangeSection* pRangeListToCleanup = this->pCleanupList;
            RangeSectionFragment* pRangeListFragmentToFree = nullptr;
            this->pCleanupList = pRangeListToCleanup->pRangeListNextForDelete;

            uintptr_t rangeListFragmentCount = RangeListFragmentCount(pRangeListToCleanup);

            void* addressToPrepForCleanup = pRangeListToCleanup->_range.begin;

            // Remove fragments from each of the fragment linked lists
            for (uintptr_t iFragment = 0; iFragment < rangeListFragmentCount; iFragment++)
            {
                RangeSectionFragmentPointer* entryInMapToUpdate = EnsureMapsForAddress(addressToPrepForCleanup);
                assert(entryInMapToUpdate != NULL);

                while ((entryInMapToUpdate->VolatileLoadWithoutBarrier(pLockState))->pRangeList != pRangeListToCleanup)
                {
                    entryInMapToUpdate = &(entryInMapToUpdate->VolatileLoadWithoutBarrier(pLockState))->pRangeListFragmentNext;
                }

                RangeSectionFragment* fragment = entryInMapToUpdate->VolatileLoadWithoutBarrier(pLockState);

                // The fragment associated with the start of the range has the address that was allocated earlier
                if (iFragment == 0)
                {
                    pRangeListFragmentToFree = fragment;
                    assert(pRangeListFragmentToFree->isPrimaryRangeListFragment);
                }

                entryInMapToUpdate->VolatileStore(fragment->pRangeListFragmentNext.VolatileLoadWithoutBarrier(pLockState));
                addressToPrepForCleanup = IncrementAddressByMaxSizeOfFragment(addressToPrepForCleanup);
            }

            // Free the array of fragments
            delete pRangeListToCleanup;
            free(pRangeListFragmentToFree);
        }
    }
};

int main()
{
    RangeListMap map;
    Range rFirst;
    rFirst.begin = (void*)0x1111000;
    rFirst.end = (void*)0x1111050;
    Range rSecond;
    rSecond.begin = (void*)0x1111051;
    rSecond.end = (void*)0x1192050;

    RangeListLockState lockState = RangeListLockState::ReaderLocked;
    RangeSection *rSectionFirst = map.AllocateRange(rFirst, NULL, RangeSection::RANGE_SECTION_COLLECTIBLE, (PTR_HeapList)NULL, &lockState);
    RangeSection *rSectionSecond = map.AllocateRange(rSecond, NULL, RangeSection::RANGE_SECTION_NONE, (PTR_HeapList)NULL, &lockState);

    RangeSection *result;

    lockState = RangeListLockState::None;
    result = map.LookupRangeList((void*)0x1111000, &lockState);
    assert(lockState == RangeListLockState::NeedsLock);
    assert(result == NULL);
    lockState = RangeListLockState::ReaderLocked;
    result = map.LookupRangeList((void*)0x1111000, &lockState);
    assert(result == rSectionFirst);
    assert(lockState == RangeListLockState::ReaderLocked);

    lockState = RangeListLockState::None;
    result = map.LookupRangeList((void*)0x1111050, &lockState);
    assert(lockState == RangeListLockState::NeedsLock);
    assert(result == NULL);
    lockState = RangeListLockState::ReaderLocked;
    result = map.LookupRangeList((void*)0x1111050, &lockState);
    assert(result == rSectionFirst);
    assert(lockState == RangeListLockState::ReaderLocked);
    lockState = RangeListLockState::None;

    result = map.LookupRangeList((void*)0x1111051, &lockState);
    assert(lockState == RangeListLockState::None);
    assert(result == rSectionSecond);
    result = map.LookupRangeList((void*)0x1151050, &lockState);
    assert(lockState == RangeListLockState::None);
    assert(result == rSectionSecond);
    result = map.LookupRangeList((void*)0x1192050, &lockState);
    assert(lockState == RangeListLockState::None);
    assert(result == rSectionSecond);

    map.RemoveRangeList(rSectionFirst);

    lockState = RangeListLockState::None;
    result = map.LookupRangeList((void*)0x1111000, &lockState);
    assert(lockState == RangeListLockState::NeedsLock);
    assert(result == NULL);
    lockState = RangeListLockState::ReaderLocked;
    result = map.LookupRangeList((void*)0x1111000, &lockState);
    assert(result == NULL);
    assert(lockState == RangeListLockState::ReaderLocked);

    lockState = RangeListLockState::None;
    result = map.LookupRangeList((void*)0x1111050, &lockState);
    assert(lockState == RangeListLockState::NeedsLock);
    assert(result == NULL);
    lockState = RangeListLockState::ReaderLocked;
    result = map.LookupRangeList((void*)0x1111050, &lockState);
    assert(result == NULL);
    assert(lockState == RangeListLockState::ReaderLocked);
    lockState = RangeListLockState::None;

    result = map.LookupRangeList((void*)0x1111051, &lockState);
    assert(result == rSectionSecond);
    assert(lockState == RangeListLockState::None);
    result = map.LookupRangeList((void*)0x1151050, &lockState);
    assert(result == rSectionSecond);
    assert(lockState == RangeListLockState::None);
    result = map.LookupRangeList((void*)0x1192050, &lockState);
    assert(result == rSectionSecond);
    assert(lockState == RangeListLockState::None);

    assert(lockState == RangeListLockState::None);
    lockState = RangeListLockState::WriteLocked;
    map.CleanupRangeLists(&lockState);
    lockState = RangeListLockState::None;

    result = map.LookupRangeList((void*)0x1111000, &lockState);
    assert(result == NULL);
    assert(lockState == RangeListLockState::None);
    result = map.LookupRangeList((void*)0x1111050, &lockState);
    assert(result == NULL);
    assert(lockState == RangeListLockState::None);
    result = map.LookupRangeList((void*)0x1111051, &lockState);
    assert(result == rSectionSecond);
    assert(lockState == RangeListLockState::None);
    result = map.LookupRangeList((void*)0x1151050, &lockState);
    assert(result == rSectionSecond);
    assert(lockState == RangeListLockState::None);
    result = map.LookupRangeList((void*)0x1192050, &lockState);
    assert(result == rSectionSecond);
    assert(lockState == RangeListLockState::None);

    std::cout << "Done\n";
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
