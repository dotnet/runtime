// Copyright (c) 2025 Vladimir Sadov
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// SatoriPage.h
//

#ifndef __SATORI_PAGE_H__
#define __SATORI_PAGE_H__

#include "common.h"
#include "../gc.h"
#include "SatoriUtil.h"

class SatoriHeap;
class SatoriRegion;

// The Page is a memory reservation unit. Also manages cards.
class SatoriPage
{
public:
    SatoriPage() = delete;
    ~SatoriPage() = delete;

    static SatoriPage* InitializeAt(size_t address, size_t pageSize, SatoriHeap* heap);
    SatoriRegion* MakeInitialRegion();

    void OnRegionInitialized(SatoriRegion* region);

    SatoriRegion* RegionForAddressChecked(size_t address);
    SatoriRegion* RegionForCardGroup(size_t group);

    SatoriRegion* NextInPage(SatoriRegion* region);

    size_t Start();
    size_t End();
    size_t RegionsStart();
    uint8_t* RegionMap();
    SatoriHeap* Heap();

    inline void SetCardForAddress(size_t address)
    {
        size_t offset = address - (size_t)this;
        size_t cardByteOffset = offset / Satori::BYTES_PER_CARD_BYTE;

        _ASSERTE(cardByteOffset >= m_cardTableStart);
        _ASSERTE(cardByteOffset < m_cardTableSize);

        if (!m_cardTable[cardByteOffset])
        {
            m_cardTable[cardByteOffset] = Satori::CardState::REMEMBERED;

            size_t cardGroup = offset / Satori::BYTES_PER_CARD_GROUP;
            if (!m_cardGroups[cardGroup * 2])
            {
                m_cardGroups[cardGroup * 2] = Satori::CardState::REMEMBERED;
                if (!m_cardState)
                {
                    m_cardState = Satori::CardState::REMEMBERED;
                }
            }
        }
    }

    // This is called to simulate a write concurrently with mutator.
    // For example when concurrent mark can't mark a child object so we need to keep
    // it logically gray, but can't keep it in the gray queue lest we never finish.
    // So we make the reference dirty as if it was written, but there was no actual write.
    inline void DirtyCardForAddressConcurrent(size_t address)
    {
        size_t offset = address - (size_t)this;
        size_t cardByteOffset = offset / Satori::BYTES_PER_CARD_BYTE;

        _ASSERTE(cardByteOffset >= m_cardTableStart);
        _ASSERTE(cardByteOffset < m_cardTableSize);

        // we dirty the card unconditionally, since it can be concurrently cleaned
        // however this does not get called after field writes (unlike in barriers or bulk helpers),
        // so the dirtying write can be unordered.
        m_cardTable[cardByteOffset] = Satori::CardState::DIRTY;

        // we do not clean groups concurrently, so these can be conditional and unordered
        // only the eventual final state matters
        size_t cardGroup = offset / Satori::BYTES_PER_CARD_GROUP;
        if (m_cardGroups[cardGroup * 2] != Satori::CardState::DIRTY)
        {
            m_cardGroups[cardGroup * 2] = Satori::CardState::DIRTY;
            if (m_cardState != Satori::CardState::DIRTY)
            {
                m_cardState = Satori::CardState::DIRTY;
            }
        }
    }

    void SetCardForAddressOnly(size_t address);
    void DirtyCardForAddress(size_t address);
    void SetCardsForRange(size_t start, size_t end);
    void DirtyCardsForRange(size_t start, size_t length);
    void DirtyCardsForRangeConcurrent(size_t start, size_t end);

    void WipeGroupsForRange(size_t start, size_t end);
    void ResetCardsForRange(size_t start, size_t end, bool isTenured);

    volatile int8_t& CardState();
    volatile uint8_t& ScanTicket();
    volatile int8_t& CardGroupState(size_t i);
    volatile uint8_t& CardGroupScanTicket(size_t i);

    size_t CardGroupCount();
    int8_t* CardsForGroup(size_t i);
    size_t LocationForCard(int8_t* cardPtr);

    template <typename F>
    void ForEachRegion(F lambda);

private:
    union
    {
        // 1bit  - 64  bytes
        // 1byte - 512 bytes   i.e cards add ~ 0.002 overhead
        // 8byte - 4k
        // 4K    - 2Mb (region granularity)
        // 2Mb   - 1Gb (region granule can store cards for 1Gb page)
        // We can start card table at the beginning of the page for simplicity
        // The first 4K cover the card itself, so that space will be unused and we can use it for other metadata.
        int8_t m_cardTable[1];

        // header (can be up to 2Kb for 1Gb page)
        struct
        {
            int8_t m_cardState;
            uint8_t m_scanTicket;
            size_t m_end;
            size_t m_initialCommit;
            size_t m_firstRegion;

            SatoriHeap* m_heap;

            // the following is useful when scanning/clearing cards
            // it can be computed from the page size, but we have space, so we will store.
            size_t m_cardTableSize;
            size_t m_cardTableStart;

            // -----  we can have a few more fields above as long as m_cardGroups starts at offset 128.
            //        that can be adjusted if needed

            // computed size, located after card groups
            // 1byte per region
            // 512 bytes per 1Gb
            uint8_t* m_regionMap;

            // computed size,
            // 2 byte per card group (4 per region granule)
            //   one signed byte for a state, which has negative values.
            //   and one unsigned for a ticket, which is a wrap-around counter.
            // 2048 bytes per 1Gb
            DECLSPEC_ALIGN(128)
            int8_t m_cardGroups[1];
        };
    };
};

#endif
