// RangeListMap.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <assert.h>
#include <Windows.h>

#define TARGET_64BIT


class Range
{
public:
    void* begin;
    void* end;
};

class RangeList
{
public:
    RangeList(Range range) :
        _range(range)
    {}

    Range _range;

    RangeList* pRangeListNextForDelete = nullptr; // Used for adding to the cleanup list
};

// Unlike a RangeList, a RangeListMini cannot span multiple elements of the last level of the SegmentMap
// Always allocated via calloc
class RangeListMini
{
public:
    RangeListMini* pRangeListMiniNext;
    Range _range;
    RangeList* pRangeList;
    bool InRange(void* address) { return address >= _range.begin && address <= _range.end && pRangeList->pRangeListNextForDelete == NULL; }
    bool isPrimaryRangeListMini; // RangeListMini are allocated in arrays, but we only need to free the first allocated one. It will be marked with this flag.
};


// For 64bit, we work with 8KB chunks of memory holding pointers to the next level. This provides 10 bits of address resolution per level.
// For *reasons* the X64 hardware is limited to 57bits of addressable address space, and the minimum granularity that makes sense for range lists is 64KB (or every 2^16 bits)
// Similarly the Arm64 specification requires addresses to use at most 52 bits. Thus we use the maximum addressable range of X64 to provide the real max range
// So the first level is bits [56:47] -> L4
// Then                       [46:37] -> L3
//                            [36:27] -> L2
//                            [26:17] -> L1
// This leaves 17 bits of the address to be handled by the RangeList linked list
//
// For 32bit VA processes, use 1KB chunks holding pointers to the next level. This provides 8 bites of address resolution per level.    [31:24] and [23:16].

// The memory safety model for segment maps is that the pointers held within the individual segments can never change other than to go from NULL to a meaningful pointer, 
// except for the final level, which is only permitted to change when CleanupWhileNoThreadMayLookupRangeLists is in use.



class SingleElementSegmentMap
{

};


template<typename T>
void VolatileStore(T* ptr, T val)
{
    *ptr = val;
}

template<typename T>
T VolatileRead(T* ptr)
{
    return *ptr;
}

template<typename T>
T VolatileLoadWithoutBarrier(T* ptr)
{
    return *ptr;
}

class RangeListMap
{
#ifdef TARGET_64BIT
    static const uintptr_t entriesPerMapLevel = 1024;
    static const uintptr_t mapLevels = 4;
    static const uintptr_t maxSetBit = 56; // This is 0 indexed
    static const uintptr_t bitsPerLevel = 10;
    RangeListMini***** _rangeListL4;
    void** GetTopLevelAddress() { return reinterpret_cast<void**>(&_rangeListL4); }
#else
    static const uintptr_t entriesPerMapLevel = 256;
    static const uintptr_t mapLevels = 2;
    static const uintptr_t maxSetBit = 31; // This is 0 indexed
    static const uintptr_t bitsPerLevel = 8;

    RangeListMini** _rangeListL2;
    void** GetTopLevelAddress() { return reinterpret_cast<void**>(&_rangeListL2); }
#endif
    int _lock = 0; // 0 indicates unlocked. -1 indicates in the process of cleanup, Positive numbers indicate read locks
    RangeList* pCleanupList = nullptr;

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

    template<typename T>
    T EnsureLevel(void *address, T* outerLevel, uintptr_t level)
    {
        uintptr_t index = EffectiveBitsForLevel(address, level);
        T rangeListResult = outerLevel[index];
        if (rangeListResult == NULL)
        {
            T rangeListNew = static_cast<T>(AllocateLevel());
            T rangeListOld = (T)InterlockedCompareExchangePointer((volatile PVOID*)&outerLevel[index], (PVOID)rangeListNew, NULL);

            if (rangeListOld != NULL)
            {
                rangeListResult = rangeListOld;
                free(rangeListNew);
            }
            else
            {
                rangeListResult = rangeListNew;
            }
        }

        return rangeListResult;
    }

    // Returns pointer to address in last level map that actually points at RangeList space.
    RangeListMini** EnsureMapsForAddress(void* address)
    {
#ifdef TARGET_64BIT
        RangeListMini**** _rangeListL3 = EnsureLevel(address, _rangeListL4, 4);
        if (_rangeListL3 == NULL)
            return NULL; // Failure case
        RangeListMini*** _rangeListL2 = EnsureLevel(address, _rangeListL3, 3);
        if (_rangeListL2 == NULL)
            return NULL; // Failure case
#endif
        RangeListMini** _rangeListL1 = EnsureLevel(address, _rangeListL2, 2);
        if (_rangeListL1 == NULL)
            return NULL; // Failure case
        return &_rangeListL1[EffectiveBitsForLevel(address, 1)];
    }

    RangeListMini* GetRangeListForAddress(void* address)
    {
#ifdef TARGET_64BIT
        RangeListMini**** _rangeListL3 = VolatileRead(&_rangeListL4[EffectiveBitsForLevel(address, 4)]); // Use a VolatileRead on the top level operation to ensure that the entire map is synchronized to a state that includes all data needed to examine currently active function pointers.
        if (_rangeListL3 == NULL)
            return NULL;
        RangeListMini*** _rangeListL2 = _rangeListL3[EffectiveBitsForLevel(address, 3)];
        if (_rangeListL2 == NULL)
            return NULL;
        RangeListMini** _rangeListL1 = _rangeListL2[EffectiveBitsForLevel(address, 2)];
#else
        RangeListMini** _rangeListL1 = VolatileRead(&_rangeListL2[EffectiveBitsForLevel(address, 2)]);
#endif
        if (_rangeListL2 == NULL)
            return NULL;

        return _rangeListL1[EffectiveBitsForLevel(address, 1)];
    }

    uintptr_t RangeListMiniCount(RangeList *pRangeList)
    {
        uintptr_t rangeSize = reinterpret_cast<uintptr_t>(pRangeList->_range.end) - reinterpret_cast<uintptr_t>(pRangeList->_range.begin);
        rangeSize /= bytesAtLastLevel;
        return rangeSize + 1;
    }

    void* IncrementAddressByMaxSizeOfMini(void* input)
    {
        uintptr_t inputAsInt = reinterpret_cast<uintptr_t>(input);
        return reinterpret_cast<void*>(inputAsInt + bytesAtLastLevel);
    }

public:
    RangeListMap()
    {
    }

    bool Init()
    {
        *GetTopLevelAddress() = AllocateLevel();
        if (*GetTopLevelAddress() == NULL)
            return false;

        return true;
    }

    bool AttachRangeListToMap(RangeList* pRangeList)
    {
        uintptr_t rangeListMiniCount = RangeListMiniCount(pRangeList);
        RangeListMini* minis = (RangeListMini*)calloc(rangeListMiniCount, sizeof(RangeListMini));

        if (minis == NULL)
        {
            return false;
        }

        RangeListMini*** entriesInMapToUpdate = (RangeListMini***)calloc(rangeListMiniCount, sizeof(RangeListMini**));
        if (entriesInMapToUpdate == NULL)
        {
            free(minis);
            return false;
        }

        minis[0].isPrimaryRangeListMini = true;

        void* addressToPrepForUpdate = pRangeList->_range.begin;
        for (uintptr_t iMini = 0; iMini < rangeListMiniCount; iMini++)
        {
            minis[iMini].pRangeList = pRangeList;
            minis[iMini]._range = pRangeList->_range;
            RangeListMini** entryInMapToUpdate = EnsureMapsForAddress(addressToPrepForUpdate);
            if (entryInMapToUpdate == NULL)
            {
                free(minis);
                free(entriesInMapToUpdate);
                return false;
            }

            entriesInMapToUpdate[iMini] = entryInMapToUpdate;
            addressToPrepForUpdate = IncrementAddressByMaxSizeOfMini(addressToPrepForUpdate);
        }

        // At this point all the needed memory is allocated, and it is no longer possible to fail.
        for (uintptr_t iMini = 0; iMini < rangeListMiniCount; iMini++)
        {
            RangeListMini* initialMiniInMap = VolatileRead(entriesInMapToUpdate[iMini]);
            do
            {
                VolatileStore(&minis[iMini].pRangeListMiniNext, initialMiniInMap);
                RangeListMini* currentMiniInMap = (RangeListMini*)InterlockedCompareExchangePointer((volatile PVOID*)entriesInMapToUpdate[iMini], &(minis[iMini]), initialMiniInMap);
                if (currentMiniInMap == initialMiniInMap)
                {
                    break;
                }
                initialMiniInMap = currentMiniInMap;
            } while (true);
        }

        // entriesInMapToUpdate was just a temporary allocation
        free(entriesInMapToUpdate);

        return true;
    }

private:
    RangeList* LookupRangeListByAddressForKnownValidAddressWhileCleanupCannotHappenOrUnderLock(void* address)
    {
        RangeListMini* mini = GetRangeListForAddress(address);
        if (mini == NULL)
            return NULL;

        while (mini != NULL && !mini->InRange(address))
        {
            mini = VolatileLoadWithoutBarrier(&mini->pRangeListMiniNext);
        }

        if (mini != NULL)
        {
            return mini->pRangeList;
        }

        return NULL;
    }

public:
    bool TryLookupRangeListByAddressForKnownValidAddress(void* address, RangeList** pRangeList)
    {
        *pRangeList = NULL;

        bool locked = false;
        int lockVal;

        do
        {
            lockVal = VolatileRead(&_lock);

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
    RangeList* LookupRangeListCannotCallInParallelWithCleanup(void* address)
    {
        // Locked readers may be reading, but no cleanup can be happening
        assert(_lock != -1);
        return LookupRangeListByAddressForKnownValidAddressWhileCleanupCannotHappenOrUnderLock(address);
    }

    void RemoveRangeListCannotCallInParallelWithCleanup(RangeList* pRangeList)
    {
        assert(pRangeList->pRangeListNextForDelete = nullptr);
        assert(pRangeList == LookupRangeListCannotCallInParallelWithCleanup(pRangeList->_range.begin));

        // Removal is implemented by placing onto the cleanup linked list. This is then processed later during cleanup
        RangeList* pLatestRemovedRangeList;
        do
        {
            pLatestRemovedRangeList = VolatileRead(&pCleanupList);
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

        RangeListMini *minisToFree = nullptr;

        while (this->pCleanupList != nullptr)
        {
            RangeList* pRangeListToCleanup = this->pCleanupList;
            RangeListMini* pRangeListMiniToFree = nullptr;
            this->pCleanupList = pRangeListToCleanup->pRangeListNextForDelete;

            uintptr_t rangeListMiniCount = RangeListMiniCount(pRangeListToCleanup);

            void* addressToPrepForCleanup = pRangeListToCleanup->_range.begin;

            for (uintptr_t iMini = 0; iMini < rangeListMiniCount; iMini++)
            {
                RangeListMini** entryInMapToUpdate = EnsureMapsForAddress(addressToPrepForCleanup);
                assert(entryInMapToUpdate != NULL);

                while ((*entryInMapToUpdate)->pRangeList != pRangeListToCleanup)
                {
                    entryInMapToUpdate = &(*entryInMapToUpdate)->pRangeListMiniNext;
                }

                if (iMini == 0)
                {
                    pRangeListMiniToFree = *entryInMapToUpdate;
                    assert(pRangeListMiniToFree->isPrimaryRangeListMini);
                }

                *entryInMapToUpdate = (*entryInMapToUpdate)->pRangeListMiniNext;

                addressToPrepForCleanup = IncrementAddressByMaxSizeOfMini(addressToPrepForCleanup);
            }

            free(pRangeListMiniToFree);
        }

        // Release lock
        VolatileStore(&_lock, 0);
    }
};

int main()
{
    std::cout << "Hello World!\n";
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
