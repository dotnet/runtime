//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// LightWeightMap.h -
// Notes:
// improvements:
//   1. we could pack the size down a bit with various encoding tricks (don't use 4 bytes for the numItems etc)
//   2. Add() could find the right place to insert via binary search... though the list is normally very small
//   3. Buffer encoding could easily be more compact
//----------------------------------------------------------
#ifndef _LightWeightMap
#define _LightWeightMap

#include "errorhandling.h"

//#define DEBUG_LWM

// Common base class that implements the raw buffer functionality.
class LightWeightMapBuffer
{
public:
    LightWeightMapBuffer()
    {
        InitialClear();
    }

    LightWeightMapBuffer(const LightWeightMapBuffer& lwm)
    {
        InitialClear();
        bufferLength = lwm.bufferLength;

        if ((lwm.buffer != nullptr) && (lwm.bufferLength > 0))
        {
            buffer = new unsigned char[lwm.bufferLength];
            memcpy(buffer, lwm.buffer, lwm.bufferLength);
        }
    }

    ~LightWeightMapBuffer()
    {
        delete[] buffer;
    }

    unsigned int AddBuffer(const unsigned char* buff, unsigned int len)
    {
        return AddBuffer(buff, len, false);
    }

    unsigned int AddBuffer(const unsigned char* buff, unsigned int len, bool forceUnique)
    {
        if (len == 0)
            return -1;
        if (buff == nullptr)
            return -1;
        int index = Contains(buff, len); // See if there is already a copy of this data in our buffer
        if ((index != -1) && (!forceUnique))
            return index;
        if (locked)
        {
            LogError("Added item that extended the buffer after it was locked by a call to GetBuffer()");
            __debugbreak();
        }

        unsigned int   newbuffsize = bufferLength + sizeof(unsigned int) + len;
        unsigned char* newbuffer   = new unsigned char[newbuffsize];
        unsigned int   newOffset   = bufferLength;
        if (bufferLength > 0)
            memcpy(newbuffer, buffer, bufferLength);
        memcpy(newbuffer + bufferLength + sizeof(unsigned int), buff, len);
        *((unsigned int*)(newbuffer + bufferLength)) = len;
        bufferLength += sizeof(unsigned int) + len;
        if (buffer != nullptr)
            delete[] buffer;
        buffer = newbuffer;
        return newOffset + sizeof(unsigned int);
    }

    unsigned char* GetBuffer(unsigned int offset)
    {
        if (offset == (unsigned int)-1)
            return nullptr;
        AssertCodeMsg(offset < bufferLength, EXCEPTIONCODE_LWM, "Hit offset bigger than bufferLength %u >= %u", offset,
                      bufferLength);
        locked = true;
        // LogDebug("Address given %p", &buffer[offset]);
        return &buffer[offset];
    }

    int Contains(const unsigned char* buff, unsigned int len)
    {
#ifdef DEBUG_LWM
        LogDebug("New call to Contains %d {", len);
        for (int i = 0; i < len; i++)
            LogDebug("0x%02x ", buff[len]);
        LogDebug("}");
#endif
        if (len == 0)
            return -1;
        if (bufferLength == 0)
            return -1;
        unsigned int offset = 0;
        while ((offset + sizeof(unsigned int) + len) <= bufferLength)
        {
            unsigned int buffChunkLen = *(unsigned int*)(&buffer[offset]);
#ifdef DEBUG_LWM
            LogDebug("Investigating len %d @ %d", buffChunkLen, offset);
#endif
            if (buffChunkLen == len)
            {
#ifdef DEBUG_LWM
                LogDebug("peering into {");
                for (int i = 0; i < len; i++)
                    LogDebug("0x%02x ", buff[len]);
                LogDebug("}");
#endif
                if (memcmp(&buffer[offset + sizeof(unsigned int)], buff, len) == 0)
                {
#ifdef DEBUG_LWM
                    LogDebug("Found!");
#endif
                    return offset + sizeof(unsigned int);
                }
            }
            offset += sizeof(unsigned int) + buffChunkLen;
        }
#ifdef DEBUG_LWM
        LogDebug("NOT Found!");
#endif
        return -1;
    }

    void Unlock() // did you really mean to use this?
    {
        locked = false;
    }

protected:
    void InitialClear()
    {
        buffer       = nullptr;
        bufferLength = 0;
        locked       = false;
    }

    unsigned char* buffer; // TODO-Cleanup: this should really be a linked list; we reallocate it with every call to
                           // AddBuffer().
    unsigned int bufferLength;
    bool         locked;
};

template <typename _Key, typename _Item>
class LightWeightMap : public LightWeightMapBuffer
{
public:
    LightWeightMap()
    {
        InitialClear();
    }

    LightWeightMap(const LightWeightMap& lwm)
    {
        InitialClear();
        numItems     = lwm.numItems;
        strideSize   = lwm.strideSize;
        bufferLength = lwm.bufferLength;
        locked       = false;

        pKeys  = nullptr;
        pItems = nullptr;

        if (lwm.pKeys != nullptr)
        {
            pKeys = new _Key[numItems];
            memcpy(pKeys, lwm.pKeys, numItems * sizeof(_Key));
        }
        if (lwm.pItems != nullptr)
        {
            pItems = new _Item[numItems];
            memcpy(pItems, lwm.pItems, numItems * sizeof(_Item));
        }
        if ((lwm.buffer != nullptr) && (lwm.bufferLength > 0))
        {
            buffer = new unsigned char[lwm.bufferLength];
            memcpy(buffer, lwm.buffer, lwm.bufferLength);
        }
    }

    ~LightWeightMap()
    {
        if (pKeys != nullptr)
            delete[] pKeys;
        if (pItems != nullptr)
            delete[] pItems;
    }

    void ReadFromArray(const unsigned char* rawData, unsigned int size)
    {
        unsigned int         sizeOfKey  = sizeof(_Key);
        unsigned int         sizeOfItem = sizeof(_Item);
        const unsigned char* ptr        = rawData;

        // The tag is optional, to roll forward previous formats which don't have
        // the tag, but which also have the same format.
        if (0 == memcmp(ptr, "LWM1", 4))
        {
            ptr += 4;
        }

        memcpy(&numItems, ptr, sizeof(unsigned int));
        ptr += sizeof(unsigned int);
        strideSize = numItems;

        if (numItems > 0)
        {
            // Read the buffersize
            memcpy(&bufferLength, ptr, sizeof(unsigned int));
            ptr += sizeof(unsigned int);

            AssertCodeMsg(pKeys == nullptr, EXCEPTIONCODE_LWM, "Found existing pKeys");
            pKeys = new _Key[numItems];
            // Set the Keys
            memcpy(pKeys, ptr, sizeOfKey * numItems);
            ptr += sizeOfKey * numItems;

            AssertCodeMsg(pItems == nullptr, EXCEPTIONCODE_LWM, "Found existing pItems");
            pItems = new _Item[numItems];
            // Set the Items
            memcpy(pItems, ptr, sizeOfItem * numItems);
            ptr += sizeOfItem * numItems;

            AssertCodeMsg(buffer == nullptr, EXCEPTIONCODE_LWM, "Found existing buffer");
            buffer = new unsigned char[bufferLength];
            // Read the buffer
            memcpy(buffer, ptr, bufferLength * sizeof(unsigned char));
            ptr += bufferLength * sizeof(unsigned char);
        }

        // If we have RTTI, we can make this assert report the correct type. No RTTI, though, when
        // built with .NET Core, especially when built against the PAL.
        AssertCodeMsg((ptr - rawData) == size, EXCEPTIONCODE_LWM, "%s - Ended with unexpected sizes %Ix != %x",
                      "Unknown type" /*typeid(_Item).name()*/, ptr - rawData, size);
    }

    unsigned int CalculateArraySize()
    {
        int size = 4 /* tag */ + sizeof(unsigned int) /* numItems */;
        if (numItems > 0)
        {
            size += sizeof(unsigned int);                 // size of bufferLength
            size += sizeof(_Key) * numItems;              // size of keyset
            size += sizeof(_Item) * numItems;             // size of itemset
            size += sizeof(unsigned char) * bufferLength; // bulk size of raw buffer
        }
        return size;
    }

    unsigned int DumpToArray(unsigned char* bytes)
    {
        unsigned char* ptr  = bytes;
        unsigned int   size = CalculateArraySize();

        // Write the tag
        memcpy(ptr, "LWM1", 4);
        ptr += 4;

        // Write the header
        memcpy(ptr, &numItems, sizeof(unsigned int));
        ptr += sizeof(unsigned int);

        if (numItems > 0)
        {
            unsigned int sizeOfKey  = sizeof(_Key);
            unsigned int sizeOfItem = sizeof(_Item);

            // Write the buffersize
            memcpy(ptr, &bufferLength, sizeof(unsigned int));
            ptr += sizeof(unsigned int);

            // Write the Keys
            memcpy(ptr, pKeys, sizeOfKey * numItems);
            ptr += sizeOfKey * numItems;

            // Write the Items
            memcpy(ptr, pItems, sizeOfItem * numItems);
            ptr += sizeOfItem * numItems;

            // Write the buffer
            memcpy(ptr, buffer, bufferLength * sizeof(unsigned char));
            ptr += bufferLength * sizeof(unsigned char);
        }

        // If we have RTTI, we can make this assert report the correct type. No RTTI, though, when
        // built with .NET Core, especially when built against the PAL.
        AssertCodeMsg((ptr - bytes) == size, EXCEPTIONCODE_LWM, "%s - Ended with unexpected sizes %p != %x",
                      "Unknown type" /*typeid(_Item).name()*/, (void*)(ptr - bytes), size);
        return size;
    }

    // It's worth noting that the actual order of insertion here doesnt meet what you might expect.  It's using memcmp, so
    // since we are on a little endian machine we'd use the lowest 8 bits as the first part of the key.  This is
    // a side effect of using the same code for large structs and DWORDS etc...
    bool Add(_Key key, _Item item)
    {
        // Make sure we have space left, expand if needed
        if (numItems == strideSize)
        {
            _Key*  tKeys  = pKeys;
            _Item* tItems = pItems;
            pKeys         = new _Key[(strideSize * 2) + 4];
            memcpy(pKeys, tKeys, strideSize * sizeof(_Key));
            pItems = new _Item[(strideSize * 2) + 4];
            memcpy(pItems, tItems, strideSize * sizeof(_Item));
            strideSize = (strideSize * 2) + 4;
            delete[] tKeys;
            delete[] tItems;
        }
        unsigned int insert = 0;
        // Find the right place to insert O(n) version
        /*      for(;insert < numItems; insert++)
                {
                    int res = memcmp(&pKeys[insert], &key, sizeof(_Key));
                    if(res == 0)
                        return false;
                    if(res>0)
                        break;
                }
        */
        // O(log n) version
        int first = 0;
        int mid   = 0;
        int last  = numItems - 1;
        while (first <= last)
        {
            mid     = (first + last) / 2; // compute mid point.
            int res = memcmp(&pKeys[mid], &key, sizeof(_Key));

            if (res < 0)
                first = mid + 1; // repeat search in top half.
            else if (res > 0)
                last = mid - 1; // repeat search in bottom half.
            else
                return false; // found it. return position /////
        }
        insert = first;
        if (insert != (unsigned int)first)
        {
            LogDebug("index = %u f %u mid = %u l %u***************************", insert, first, mid, last);
            __debugbreak();
        }

        if (numItems > 0)
        {
            for (unsigned int i = numItems; i > insert; i--)
            {
                pKeys[i]  = pKeys[i - 1];
                pItems[i] = pItems[i - 1];
            }
        }

        pKeys[insert]  = key;
        pItems[insert] = item;
        numItems++;
        return true;
    }

    void Update(int index, _Item item)
    {
        pItems[index] = item;
    }

    int GetIndex(_Key key)
    {
        AssertCodeMsg(this != nullptr, EXCEPTIONCODE_MC, "There is no such LWM (in GetIndex)");
        if (numItems == 0)
            return -1;

        // O(log n) version
        int first = 0;
        int mid   = 0;
        int last  = numItems - 1;
        while (first <= last)
        {
            mid     = (first + last) / 2; // compute mid point.
            int res = memcmp(&pKeys[mid], &key, sizeof(_Key));

            if (res < 0)
                first = mid + 1; // repeat search in top half.
            else if (res > 0)
                last = mid - 1; // repeat search in bottom half.
            else
                return mid; // found it. return position /////
        }
        return -1; // Didn't find key
    }

    _Item GetItem(int index)
    {
        AssertCodeMsg(index != -1, EXCEPTIONCODE_LWM, "Didn't find Key");
        return pItems[index]; // found it. return position /////
    }

    _Key GetKey(int index)
    {
        AssertCodeMsg(index != -1, EXCEPTIONCODE_LWM, "Didn't find Key (in GetKey)");
        return pKeys[index];
    }

    _Item Get(_Key key)
    {
        int index = GetIndex(key);
        AssertCodeMsg(index != -1, EXCEPTIONCODE_MC, "Didn't find Item (in Get)");
        return GetItem(index);
    }

    _Item* GetRawItems()
    {
        return pItems;
    }

    _Key* GetRawKeys()
    {
        return pKeys;
    }

    unsigned int GetCount()
    {
        return numItems;
    }

private:
    void InitialClear()
    {
        numItems   = 0;
        strideSize = 0;
        pKeys      = nullptr;
        pItems     = nullptr;
    }

    unsigned int numItems;   // Number of active items in the pKeys and pItems arrays.
    unsigned int strideSize; // Allocated count of items in the pKeys and pItems arrays.
    _Key*        pKeys;
    _Item*       pItems;
};

// Second implementation of LightWeightMap where the Key type is an unsigned int in the range [0 .. numItems - 1] (where
// numItems is the number of items stored in the map). Keys are not stored, since the index into the pItems array is
// the key. Appending to the end of the map is O(1), since we don't have to search for it, and we don't have to move
// anything down.

template <typename _Item>
class DenseLightWeightMap : public LightWeightMapBuffer
{
public:
    DenseLightWeightMap()
    {
        InitialClear();
    }

    DenseLightWeightMap(const DenseLightWeightMap& lwm)
    {
        InitialClear();
        numItems     = lwm.numItems;
        strideSize   = lwm.strideSize;
        bufferLength = lwm.bufferLength;

        if (lwm.pItems != nullptr)
        {
            pItems = new _Item[numItems];
            memcpy(pItems, lwm.pItems, numItems * sizeof(_Item));
        }
        if ((lwm.buffer != nullptr) && (lwm.bufferLength > 0))
        {
            buffer = new unsigned char[lwm.bufferLength];
            memcpy(buffer, lwm.buffer, lwm.bufferLength);
        }
    }

    ~DenseLightWeightMap()
    {
        if (pItems != nullptr)
            delete[] pItems;
    }

    void ReadFromArray(const unsigned char* rawData, unsigned int size)
    {
        unsigned int         sizeOfItem = sizeof(_Item);
        const unsigned char* ptr        = rawData;

        // Check tag; if this is a v1 LWM, convert it to a DenseLightWeightMap in memory
        if (0 != memcmp(ptr, "DWM1", 4))
        {
            ReadFromArrayAndConvertLWM1(rawData, size);
            return;
        }
        ptr += 4;

        memcpy(&numItems, ptr, sizeof(unsigned int));
        ptr += sizeof(unsigned int);
        strideSize = numItems;

        if (numItems > 0)
        {
            // Read the buffersize
            memcpy(&bufferLength, ptr, sizeof(unsigned int));
            ptr += sizeof(unsigned int);

            AssertCodeMsg(pItems == nullptr, EXCEPTIONCODE_LWM, "Found existing pItems");
            pItems = new _Item[numItems];
            // Set the Items
            memcpy(pItems, ptr, sizeOfItem * numItems);
            ptr += sizeOfItem * numItems;

            AssertCodeMsg(buffer == nullptr, EXCEPTIONCODE_LWM, "Found existing buffer");
            buffer = new unsigned char[bufferLength];
            // Read the buffer
            memcpy(buffer, ptr, bufferLength * sizeof(unsigned char));
            ptr += bufferLength * sizeof(unsigned char);
        }

        AssertCodeMsg((ptr - rawData) == size, EXCEPTIONCODE_LWM, "Ended with unexpected sizes %Ix != %x",
                      ptr - rawData, size);
    }

private:
    void ReadFromArrayAndConvertLWM1(const unsigned char* rawData, unsigned int size)
    {
        unsigned int         sizeOfKey  = sizeof(DWORD);
        unsigned int         sizeOfItem = sizeof(_Item);
        const unsigned char* ptr        = rawData;

        memcpy(&numItems, ptr, sizeof(unsigned int));
        ptr += sizeof(unsigned int);
        strideSize = numItems;

        if (numItems > 0)
        {
            // Read the buffersize
            memcpy(&bufferLength, ptr, sizeof(unsigned int));
            ptr += sizeof(unsigned int);

            DWORD* tKeys = new DWORD[numItems];
            // Set the Keys
            memcpy(tKeys, ptr, sizeOfKey * numItems);
            ptr += sizeOfKey * numItems;

            _Item* tItems = new _Item[numItems];
            // Set the Items
            memcpy(tItems, ptr, sizeOfItem * numItems);
            ptr += sizeOfItem * numItems;

            AssertCodeMsg(buffer == nullptr, EXCEPTIONCODE_LWM, "Found existing buffer");
            buffer = new unsigned char[bufferLength];
            // Read the buffer
            memcpy(buffer, ptr, bufferLength * sizeof(unsigned char));
            ptr += bufferLength * sizeof(unsigned char);

            // Convert to new format
            AssertCodeMsg(pItems == nullptr, EXCEPTIONCODE_LWM, "Found existing pItems");
            bool* tKeySeen = new bool[numItems]; // Used for assert, below: keys must be unique.
            for (unsigned int i = 0; i < numItems; i++)
            {
                tKeySeen[i] = false;
            }
            pItems = new _Item[numItems];
            for (unsigned int index = 0; index < numItems; index++)
            {
                unsigned int key = tKeys[index];
                AssertCodeMsg(key < numItems, EXCEPTIONCODE_LWM, "Illegal key %d, numItems == %d", key, numItems);
                AssertCodeMsg(!tKeySeen[key], EXCEPTIONCODE_LWM, "Duplicate key %d", key);
                tKeySeen[key] = true;
                pItems[key]   = tItems[index];
            }

            // Note that if we get here, we've seen every key [0 .. numItems - 1].
            delete[] tKeySeen;
            delete[] tKeys;
            delete[] tItems;
        }

        AssertCodeMsg((ptr - rawData) == size, EXCEPTIONCODE_LWM, "Ended with unexpected sizes %Ix != %x",
                      ptr - rawData, size);
    }

public:
    unsigned int CalculateArraySize()
    {
        int size = 4 /* tag */ + sizeof(unsigned int) /* numItems */;
        if (numItems > 0)
        {
            size += sizeof(unsigned int);                 // size of bufferLength
            size += sizeof(_Item) * numItems;             // size of itemset
            size += sizeof(unsigned char) * bufferLength; // bulk size of raw buffer
        }
        return size;
    }

    unsigned int DumpToArray(unsigned char* bytes)
    {
        unsigned char* ptr  = bytes;
        unsigned int   size = CalculateArraySize();

        // Write the tag
        memcpy(ptr, "DWM1", 4);
        ptr += 4;

        // Write the header
        memcpy(ptr, &numItems, sizeof(unsigned int));
        ptr += sizeof(unsigned int);

        if (numItems > 0)
        {
            unsigned int sizeOfItem = sizeof(_Item);

            // Write the buffersize
            memcpy(ptr, &bufferLength, sizeof(unsigned int));
            ptr += sizeof(unsigned int);

            // Write the Items
            memcpy(ptr, pItems, sizeOfItem * numItems);
            ptr += sizeOfItem * numItems;

            // Write the buffer
            memcpy(ptr, buffer, bufferLength * sizeof(unsigned char));
            ptr += bufferLength * sizeof(unsigned char);
        }

        AssertCodeMsg((ptr - bytes) == size, EXCEPTIONCODE_LWM, "Ended with unexpected sizes %Ix != %x", ptr - bytes,
                      size);
        return size;
    }

    bool Append(_Item item)
    {
        // Make sure we have space left, expand if needed
        if (numItems == strideSize)
        {
            // NOTE: if this is the first allocation, we'll just allocate 4 items. ok?
            _Item* tItems = pItems;
            pItems        = new _Item[(strideSize * 2) + 4];
            memcpy(pItems, tItems, strideSize * sizeof(_Item));
            strideSize = (strideSize * 2) + 4;
            delete[] tItems;
        }

        pItems[numItems] = item;
        numItems++;
        return true;
    }

    int GetIndex(unsigned int key)
    {
        if (key >= numItems)
            return -1;

        return (int)key;
    }

    _Item GetItem(int index)
    {
        AssertCodeMsg(index != -1, EXCEPTIONCODE_LWM, "Didn't find Key");
        return pItems[index]; // found it. return position /////
    }

    _Item Get(unsigned int key)
    {
        int index = GetIndex(key);
        return GetItem(index);
    }

    _Item* GetRawItems()
    {
        return pItems;
    }

    unsigned int GetCount()
    {
        return numItems;
    }

private:
    void InitialClear()
    {
        numItems   = 0;
        strideSize = 0;
        pItems     = nullptr;
    }

    static int CompareKeys(unsigned int key1, unsigned int key2)
    {
        if (key1 < key2)
            return -1;
        else if (key1 > key2)
            return 1;
        else
            return 0; // equal
    }

    unsigned int numItems;   // Number of active items in the pKeys and pItems arrays.
    unsigned int strideSize; // Allocated count of items in the pKeys and pItems arrays.
    _Item*       pItems;
};

#define dumpLWM(ptr, mapName)                                                                                          \
    if (ptr->mapName != nullptr)                                                                                       \
    {                                                                                                                  \
        printf("%s - %u\n", #mapName, ptr->mapName->GetCount());                                                       \
        for (unsigned int i = 0; i < ptr->mapName->GetCount(); i++)                                                    \
        {                                                                                                              \
            printf("%u-", i);                                                                                          \
            ptr->dmp##mapName(ptr->mapName->GetRawKeys()[i], ptr->mapName->GetRawItems()[i]);                          \
            printf("\n");                                                                                              \
        }                                                                                                              \
    }

#define dumpLWMDense(ptr, mapName)                                                                                     \
    if (ptr->mapName != nullptr)                                                                                       \
    {                                                                                                                  \
        printf("%s - %u\n", #mapName, ptr->mapName->GetCount());                                                       \
        for (unsigned int i = 0; i < ptr->mapName->GetCount(); i++)                                                    \
        {                                                                                                              \
            printf("%u-", i);                                                                                          \
            ptr->dmp##mapName(i, ptr->mapName->GetRawItems()[i]);                                                      \
            printf("\n");                                                                                              \
        }                                                                                                              \
    }

#endif // _LightWeightMap
