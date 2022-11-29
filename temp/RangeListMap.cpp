// RangeListMap.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <assert.h>
#include <Windows.h>

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
    void* begin;
    void* end;
};

class RangeSection
{
public:
    RangeSection(Range range) :
        _range(range)
    {}

    Range _range;

    RangeSection* pRangeListNextForDelete = nullptr; // Used for adding to the cleanup list
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
// except for the final level, which is only permitted to change when CleanupWhileNoThreadMayLookupRangeLists is in use.



class RangeListMap
{
    // Unlike a RangeSection, a RangeSectionFragment cannot span multiple elements of the last level of the RangeListMap
    // Always allocated via calloc
    class RangeSectionFragment
    {
    public:
        RangeSectionFragment* pRangeListFragmentNext;
        Range _range;
        RangeSection* pRangeList;
        bool InRange(void* address) { return address >= _range.begin && address <= _range.end && pRangeList->pRangeListNextForDelete == NULL; }
        bool isPrimaryRangeListFragment; // RangeSectionFragment are allocated in arrays, but we only need to free the first allocated one. It will be marked with this flag.
    };

#ifdef TARGET_64BIT
    static const uintptr_t entriesPerMapLevel = 1024;
#else
    static const uintptr_t entriesPerMapLevel = 256;
#endif

    typedef RangeSectionFragment* RangeSectionList;
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

    int _lock = 0; // 0 indicates unlocked. -1 indicates in the process of cleanup, Positive numbers indicate read locks
    RangeSection* pCleanupList = nullptr;

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
    RangeSectionFragment** EnsureMapsForAddress(void* address)
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

    RangeSectionFragment* GetRangeListForAddress(void* address)
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

        return (*_rangeListL1)[EffectiveBitsForLevel(address, 1)];
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

public:
    RangeListMap() : _topLevel{0}
    {
    }

    bool Init()
    {
        return true;
    }

    bool AttachRangeListToMap(RangeSection* pRangeList)
    {
        uintptr_t rangeListFragmentCount = RangeListFragmentCount(pRangeList);
        RangeSectionFragment* fragments = (RangeSectionFragment*)calloc(rangeListFragmentCount, sizeof(RangeSectionFragment));

        if (fragments == NULL)
        {
            return false;
        }

        RangeSectionFragment*** entriesInMapToUpdate = (RangeSectionFragment***)calloc(rangeListFragmentCount, sizeof(RangeSectionFragment**));
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
            RangeSectionFragment** entryInMapToUpdate = EnsureMapsForAddress(addressToPrepForUpdate);
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
            RangeSectionFragment* initialFragmentInMap = VolatileLoad(entriesInMapToUpdate[iFragment]);
            do
            {
                VolatileStore(&fragments[iFragment].pRangeListFragmentNext, initialFragmentInMap);
                RangeSectionFragment* currentFragmentInMap = (RangeSectionFragment*)InterlockedCompareExchangePointer((volatile PVOID*)entriesInMapToUpdate[iFragment], &(fragments[iFragment]), initialFragmentInMap);
                if (currentFragmentInMap == initialFragmentInMap)
                {
                    break;
                }
                initialFragmentInMap = currentFragmentInMap;
            } while (true);
        }

        // entriesInMapToUpdate was just a temporary allocation
        free(entriesInMapToUpdate);

        return true;
    }

private:
    RangeSection* LookupRangeListByAddressForKnownValidAddressWhileCleanupCannotHappenOrUnderLock(void* address)
    {
        RangeSectionFragment* fragment = GetRangeListForAddress(address);
        if (fragment == NULL)
            return NULL;

        while (fragment != NULL && !fragment->InRange(address))
        {
            fragment = VolatileLoadWithoutBarrier(&fragment->pRangeListFragmentNext);
        }

        if (fragment != NULL)
        {
            return fragment->pRangeList;
        }

        return NULL;
    }

public:
    bool TryLookupRangeListByAddressForKnownValidAddress(void* address, RangeSection** pRangeList)
    {
        *pRangeList = NULL;

        bool locked = false;
        int lockVal;

        do
        {
            lockVal = VolatileLoad(&_lock);

            // Cleanup in process. Do not succeed in producing result
            if (lockVal < 0)
                return false;

            // Take reader lock
        } while (InterlockedCompareExchange((volatile unsigned*)&_lock, (unsigned)lockVal + 1, (unsigned)lockVal) != lockVal);

        *pRangeList = LookupRangeListByAddressForKnownValidAddressWhileCleanupCannotHappenOrUnderLock(address);

        // Release lock
        InterlockedDecrement((volatile unsigned*)&_lock);
    }

    // Due to the thread safety semantics of removal, the address passed in here MUST be the address of a function on the stack, and therefore not eligible to be cleaned up due to some race.
    RangeSection* LookupRangeListCannotCallInParallelWithCleanup(void* address)
    {
        // Locked readers may be reading, but no cleanup can be happening
        assert(_lock != -1);
        return LookupRangeListByAddressForKnownValidAddressWhileCleanupCannotHappenOrUnderLock(address);
    }

    void RemoveRangeListCannotCallInParallelWithCleanup(RangeSection* pRangeList)
    {
        assert(pRangeList->pRangeListNextForDelete = nullptr);
        assert(pRangeList == LookupRangeListCannotCallInParallelWithCleanup(pRangeList->_range.begin));

        // Removal is implemented by placing onto the cleanup linked list. This is then processed later during cleanup
        RangeSection* pLatestRemovedRangeList;
        do
        {
            pLatestRemovedRangeList = VolatileLoad(&pCleanupList);
            VolatileStore(&pRangeList->pRangeListNextForDelete, pLatestRemovedRangeList);
        } while (InterlockedCompareExchangePointer((volatile PVOID *)&pCleanupList, pRangeList, pLatestRemovedRangeList) == pLatestRemovedRangeList);
    }

    void CleanupWhileNoThreadMayLookupRangeLists()
    {
        // Take cleanup lock
        if (InterlockedCompareExchange((volatile unsigned*)&_lock, (unsigned)(-1), 0) != 0)
        {
            // If a locked read is in progress. That's OK. We'll clean up some in a future call to cleanup.
            return;
        }

        while (this->pCleanupList != nullptr)
        {
            RangeSection* pRangeListToCleanup = this->pCleanupList;
            RangeSectionFragment* pRangeListFragmentToFree = nullptr;
            this->pCleanupList = pRangeListToCleanup->pRangeListNextForDelete;

            uintptr_t rangeListFragmentCount = RangeListFragmentCount(pRangeListToCleanup);

            void* addressToPrepForCleanup = pRangeListToCleanup->_range.begin;

            // Remove fragments from each of the fragment linked lists
            for (uintptr_t iFragment = 0; iFragment < rangeListFragmentCount; iFragment++)
            {
                RangeSectionFragment** entryInMapToUpdate = EnsureMapsForAddress(addressToPrepForCleanup);
                assert(entryInMapToUpdate != NULL);

                while ((*entryInMapToUpdate)->pRangeList != pRangeListToCleanup)
                {
                    entryInMapToUpdate = &(*entryInMapToUpdate)->pRangeListFragmentNext;
                }

                // The fragment associated with the start of the range has the address that was allocated earlier
                if (iFragment == 0)
                {
                    pRangeListFragmentToFree = *entryInMapToUpdate;
                    assert(pRangeListFragmentToFree->isPrimaryRangeListFragment);
                }

                *entryInMapToUpdate = (*entryInMapToUpdate)->pRangeListFragmentNext;

                addressToPrepForCleanup = IncrementAddressByMaxSizeOfFragment(addressToPrepForCleanup);
            }

            // Free the array of fragments
            free(pRangeListFragmentToFree);
        }

        // Release lock
        VolatileStore(&_lock, 0);
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
    RangeSection rSectionFirst(rFirst);
    RangeSection rSectionSecond(rSecond);


    map.AttachRangeListToMap(&rSectionFirst);
    map.AttachRangeListToMap(&rSectionSecond);

    RangeSection *result;

    result = map.LookupRangeListCannotCallInParallelWithCleanup((void*)0x1111000);
    assert(result == &rSectionFirst);
    result = map.LookupRangeListCannotCallInParallelWithCleanup((void*)0x1111050);
    assert(result == &rSectionFirst);
    result = map.LookupRangeListCannotCallInParallelWithCleanup((void*)0x1111051);
    assert(result == &rSectionSecond);
    result = map.LookupRangeListCannotCallInParallelWithCleanup((void*)0x1151050);
    assert(result == &rSectionSecond);
    result = map.LookupRangeListCannotCallInParallelWithCleanup((void*)0x1192050);
    assert(result == &rSectionSecond);

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
