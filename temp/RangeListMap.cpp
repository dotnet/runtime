// RangeListMap.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <Windows.h>

#define TARGET_64BIT


class Range
{
public:
    void* begin;
    void* end;
    bool InRange(void* address)
    {
        return address >= begin && address <= end;
    }
};

class RangeList
{
public:
    Range range;
    RangeList* pRangeListNextForDelete = nullptr; // Used for adding to the cleanup list
};

// Unlike a RangeList, a RangeListMini cannot span multiple elements of the last level of the SegmentMap
// Always allocated via calloc
class RangeListMini
{
public:
    RangeListMini* pRangeListMiniNext;
    RangeListMini* pRangeListMiniNextForFree; // Used for adding to the cleanup list
    Range range;
    RangeList* pRangeList;
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
void VolatileWrite(T* ptr, T val)
{
    *ptr = val;
}

template<typename T>
T VolatileRead(T* ptr)
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
    RangeListMini* pRangeListMinisReadyToDelete = nullptr;

    const uintptr_t bitsAtLastLevel = maxSetBit - (bitsPerLevel * mapLevels) + 1;
    const uintptr_t bytesAtLastLevel = (1 << (bitsAtLastLevel - 1));

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
    T EnsureLevel(T* outerLevel, uintptr_t level)
    {
        uintptr_t index = EffectiveBitsForLevel(address, level);
        T rangeListResult = outerLevel[index];
        if (rangeListResult == NULL)
        {
            T rangeListNew = static_cast<T>(AllocateLevel());
            T rangeListOld = InterlockedCompareExchangePointer((volatile PVOID*)&outerLevel[index], (PVOID)rangeListNew, NULL);

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
        uintptr_t index;
        void* newRangeList;
#ifdef TARGET_64BIT
        RangeListMini**** _rangeListL3 = EnsureLevel(_rangeListL4, 4);
        if (_rangeListL3 == NULL)
            return NULL; // Failure case
        RangeListMini*** _rangeListL2 = EnsureLevel(_rangeListL3, 3);
        if (_rangeListL2 == NULL)
            return NULL; // Failure case
#endif
        RangeListMini** _rangeListL1 = EnsureLevel(_rangeListL2, 2);
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
        uintptr_t rangeSize = reinterpret_cast<uintptr_t>(pRangeList->range.end) - reinterpret_cast<uintptr_t>(pRangeList->range.begin);
        rangeSize /= bytesAtLastLevel;
        rangeSize + 1;

        return rangeSize;
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

        RangeListMini*** entriesInMapToUpdate = (RangeList***)calloc(rangeListMiniCount, sizeof(RangeList**));
        if (entriesInMapToUpdate == NULL)
        {
            free(minis);
            return false;
        }

        minis[0].isPrimaryRangeListMini = true;

        void* addressToPrepForUpdate = pRangeList->range.begin;
        for (uintptr_t iMini = 0; iMini < rangeListMiniCount; iMini++)
        {
            minis[iMini].pRangeList = pRangeList;
            minis[iMini].range = pRangeList->range;
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
                VolatileWrite(&minis[iMini].pRangeListMiniNext, initialMiniInMap);
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

    RangeList* LookupRangeListByAddressForKnownValidAddressesOrUnderLock(void* address)
    {
        RangeListMini* mini = GetRangeListForAddress(address);
        if (mini == NULL)
            return NULL;

        while (mini != NULL && !mini->range.InRange(address))
        {
            mini = VolatileRead(&mini->pRangeListMiniNext);
        }

        if (mini != NULL)
        {
            return mini->pRangeList;
        }

        return NULL;
    }

    void RemoveRangeList(RangeList* pRangeList)
    {
        uintptr_t rangeListMiniCount = RangeListMiniCount(pRangeList);
        void* addressToPrepForDelete = pRangeList->range.begin;
        for (uintptr_t iMini = 0; iMini < rangeListMiniCount; iMini++)
        {
            // Implement as marking the rangelist minis as deleted
            //  Actual deletion from the RangeListMini list is done in CleanupWhileNoThreadMayLookupRangeLists

            addressToPrepForDelete = IncrementAddressByMaxSizeOfMini(addressToPrepForDelete);
        }
    }
    void CleanupWhileNoThreadMayLookupRangeLists()
    {
        // Set a lock
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
