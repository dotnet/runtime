// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ---------------------------------------------------------------------------
// NativeFormatWriter
//
// Utilities to write native data to images, that can be read by the NativeFormat.Reader class
// ---------------------------------------------------------------------------

#include "common.h"

#include "nativeformatwriter.h"

#include <clr_std/algorithm>

namespace NativeFormat
{
    //
    // Same encoding as what's used by CTL
    //
    void NativeWriter::WriteUnsigned(unsigned d)
    {
        if (d < 128)
        {
            WriteByte((byte)(d*2 + 0));
        }
        else if (d < 128*128)
        {
            WriteByte((byte)(d*4 + 1));
            WriteByte((byte)(d >> 6));
        }
        else if (d < 128*128*128)
        {
            WriteByte((byte)(d*8 + 3));
            WriteByte((byte)(d >> 5));
            WriteByte((byte)(d >> 13));
        }
        else if (d < 128*128*128*128)
        {
            WriteByte((byte)(d*16 + 7));
            WriteByte((byte)(d >> 4));
            WriteByte((byte)(d >> 12));
            WriteByte((byte)(d >> 20));
        }
        else
        {
            WriteByte((byte)15);
            WriteUInt32(d);
        }
    }

    unsigned NativeWriter::GetUnsignedEncodingSize(unsigned d)
    {
        if (d < 128) return 1;
        if (d < 128*128) return 2;
        if (d < 128*128*128) return 3;
        if (d < 128*128*128*128) return 4;
        return 5;
    }

    void NativeWriter::WriteSigned(int i)
    {
        unsigned d = (unsigned)i;
        if (d + 64 < 128)
        {
            WriteByte((byte)(d*2 + 0));
        }
        else if (d + 64*128 < 128*128)
        {
            WriteByte((byte)(d*4 + 1));
            WriteByte((byte)(d >> 6));
        }
        else if (d + 64*128*128 < 128*128*128)
        {
            WriteByte((byte)(d*8 + 3));
            WriteByte((byte)(d >> 5));
            WriteByte((byte)(d >> 13));
        }
        else if (d + 64*128*128*128 < 128*128*128*128)
        {
            WriteByte((byte)(d*16 + 7));
            WriteByte((byte)(d >> 4));
            WriteByte((byte)(d >> 12));
            WriteByte((byte)(d >> 20));
        }
        else
        {
            WriteByte((byte)15);
            WriteUInt32(d);
        }
    }

    void NativeWriter::WriteRelativeOffset(Vertex * pVal)
    {
        WriteSigned(GetExpectedOffset(pVal) - GetCurrentOffset());
    }

    int NativeWriter::GetExpectedOffset(Vertex * pVal)
    {
        assert(pVal->m_offset != Vertex::NotPlaced);

        if (pVal->m_iteration == -1)
        {
            // If the offsets are not determined yet, use the maximum possible encoding
            return 0x7FFFFFFF;
        }

        int offset = pVal->m_offset;

        // If the offset was not update in this iteration yet, adjust it by delta we have accumulated in this iteration so far.
        // This adjustment allows the offsets to converge faster.
        if (pVal->m_iteration < m_iteration)
            offset += m_offsetAdjustment;

        return offset;
    }

    vector<byte>& NativeWriter::Save()
    {
        assert(m_phase == Initial);

        for (auto pSection : m_Sections) for (auto pVertex : *pSection)
        {
            pVertex->m_offset = GetCurrentOffset();
            pVertex->m_iteration = m_iteration;
            pVertex->Save(this);
        }

        // Aggresive phase that only allows offsets to shrink.
        m_phase = Shrinking;
        for (;;)
        {
            m_iteration++;
            m_Buffer.clear();

            m_offsetAdjustment = 0;

            for (auto pSection : m_Sections) for (auto pVertex : *pSection)
            {
                int currentOffset = GetCurrentOffset();

                // Only allow the offsets to shrink.
                m_offsetAdjustment = min(m_offsetAdjustment, currentOffset - pVertex->m_offset);

                pVertex->m_offset += m_offsetAdjustment;

                if (pVertex->m_offset < currentOffset)
                {
                    // It is possible for the encoding of relative offsets to grow during some iterations.
                    // Ignore this growth because of it should disappear during next iteration.
                    RollbackTo(pVertex->m_offset);
                }
                assert(pVertex->m_offset == GetCurrentOffset());

                pVertex->m_iteration = m_iteration;

                pVertex->Save(this);
            }

            // We are not able to shrink anymore. We cannot just return here. It is possible that we have rolledback
            // above because of we shrinked too much.
            if (m_offsetAdjustment == 0)
                break;

            // Limit number of shrinking interations. This limit is meant to be hit in corner cases only.
            if (m_iteration > 10)
                break;
        }

        // Conservative phase that only allows the offsets to grow. It is guaranteed to converge.
        m_phase = Growing;
        for (;;)
        {
            m_iteration++;
            m_Buffer.clear();

            m_offsetAdjustment = 0;
            m_paddingSize = 0;

            for (auto pSection : m_Sections) for (auto pVertex : *pSection)
            {
                int currentOffset = GetCurrentOffset();

                // Only allow the offsets to grow.
                m_offsetAdjustment = max(m_offsetAdjustment, currentOffset - pVertex->m_offset);

                pVertex->m_offset += m_offsetAdjustment;

                if (pVertex->m_offset > currentOffset)
                {
                    // Padding
                    int padding = pVertex->m_offset - currentOffset;
                    m_paddingSize += padding;
                    WritePad(padding);
                }
                assert(pVertex->m_offset == GetCurrentOffset());

                pVertex->m_iteration = m_iteration;

                pVertex->Save(this);
            }

            if (m_offsetAdjustment == 0)
                return m_Buffer;
        }

        m_phase = Done;
    }

    Vertex * NativeSection::Place(Vertex * pVertex)
    {
        assert(pVertex->m_offset == Vertex::NotPlaced);
        pVertex->m_offset = Vertex::Placed;
        push_back(pVertex);

        return pVertex;
    }

    Vertex * VertexArray::ExpandBlock(size_t index, int depth, bool place, bool * pIsLeaf)
    {
        if (depth == 1)
        {
            Vertex * pFirst = (index < m_Entries.size()) ? m_Entries[index] : nullptr;
            Vertex * pSecond = ((index + 1) < m_Entries.size()) ? m_Entries[index + 1] : nullptr;

            if (pFirst == nullptr && pSecond == nullptr)
                return nullptr;

            if (pFirst == nullptr || pSecond == nullptr)
            {
                VertexLeaf * pLeaf = new VertexLeaf();
                if (place)
                    m_pSection->Place(pLeaf);

                pLeaf->m_pVertex = (pFirst == nullptr) ? pSecond : pFirst;
                pLeaf->m_leafIndex = ((pFirst == nullptr) ? (index + 1) : index) & (_blockSize - 1);

                *pIsLeaf = true;
                return pLeaf;
            }

            VertexTree * pTree = new VertexTree();
            if (place)
                m_pSection->Place(pTree);

            pTree->m_pFirst = pFirst;
            pTree->m_pSecond = pSecond;

            m_pSection->Place(pSecond);

            return pTree;
        }
        else
        {
            VertexTree * pTree = new VertexTree();
            if (place)
                m_pSection->Place(pTree);

            bool fFirstIsLeaf = false, fSecondIsLeaf = false;
            Vertex * pFirst = ExpandBlock(index, depth - 1, false, &fFirstIsLeaf);
            Vertex * pSecond = ExpandBlock(index + (size_t{ 1 } << (depth - 1)), depth - 1, true, &fSecondIsLeaf);

            Vertex * pPop;

            if ((pFirst == nullptr && pSecond == nullptr))
            {
                if (place)
                {
                    pPop = m_pSection->Pop();
                    assert(pPop == pTree);
                }

                delete pTree;
                return nullptr;
            }

            if ((pFirst == nullptr) && fSecondIsLeaf)
            {
                pPop = m_pSection->Pop();
                assert(pPop == pSecond);

                if (place)
                {
                    pPop = m_pSection->Pop();
                    assert(pPop == pTree);
                }

                delete pTree;

                if (place)
                    m_pSection->Place(pSecond);

                *pIsLeaf = true;
                return pSecond;
            }

            if ((pSecond == nullptr) && fFirstIsLeaf)
            {
                if (place)
                {
                    pPop = m_pSection->Pop();
                    assert(pPop == pTree);
                }

                delete pTree;

                if (place)
                    m_pSection->Place(pFirst);

                *pIsLeaf = true;
                return pFirst;
            }

            pTree->m_pFirst = pFirst;
            pTree->m_pSecond = pSecond;

            return pTree;
        }
    }

    void VertexArray::ExpandLayout()
    {
        VertexLeaf * pNullBlock = nullptr;
        for (size_t i = 0; i < m_Entries.size(); i += _blockSize)
        {
            bool fIsLeaf;
            Vertex * pBlock = ExpandBlock(i, 4, true, &fIsLeaf);

            if (pBlock == nullptr)
            {
                if (pNullBlock == nullptr)
                {
                    pNullBlock = new VertexLeaf();
                    pNullBlock->m_leafIndex = _blockSize;
                    pNullBlock->m_pVertex = nullptr;
                    m_pSection->Place(pNullBlock);
                }
                pBlock = pNullBlock;
            }

            m_Blocks.push_back(pBlock);
        }

        // Start with maximum size entries
        m_entryIndexSize = 2;
    }

    void VertexArray::VertexLeaf::Save(NativeWriter * pWriter)
    {
        pWriter->WriteUnsigned(m_leafIndex << 2);

        if (m_pVertex != nullptr)
            m_pVertex->Save(pWriter);
    }

    void VertexArray::VertexTree::Save(NativeWriter * pWriter)
    {
        unsigned value = (m_pFirst != nullptr) ? 1 : 0;

        if (m_pSecond != nullptr)
        {
            value |= 2;

            int delta = pWriter->GetExpectedOffset(m_pSecond) - pWriter->GetCurrentOffset();
            assert(delta >= 0);
            value |= (delta << 2);
        }

        pWriter->WriteUnsigned(value);

        if (m_pFirst != nullptr)
            m_pFirst->Save(pWriter);
    }

    void VertexArray::Save(NativeWriter * pWriter)
    {
        // Lowest two bits are entry index size, the rest is number of elements
        pWriter->WriteUnsigned((m_Entries.size() << 2) | m_entryIndexSize);

        int blocksOffset = pWriter->GetCurrentOffset();
        int maxOffset = 0;

        for (auto pBlock : m_Blocks)
        {
            int offset = pWriter->GetExpectedOffset(pBlock) - blocksOffset;
            assert(offset >= 0);

            maxOffset = max(offset, maxOffset);

            if (m_entryIndexSize == 0)
            {
                pWriter->WriteByte((byte)offset);
            }
            else
            if (m_entryIndexSize == 1)
            {
                pWriter->WriteUInt16((UInt16)offset);
            }
            else
            {
                pWriter->WriteUInt32((UInt32)offset);
            }
        }

        int newEntryIndexSize = 0;
        if (maxOffset > 0xFF)
        {
            newEntryIndexSize++;
            if (maxOffset > 0xFFFF)
                newEntryIndexSize++;
        }

        if (pWriter->IsGrowing())
        {
            if (newEntryIndexSize > m_entryIndexSize)
            {
                // Ensure that the table will be redone with new entry index size
                pWriter->UpdateOffsetAdjustment(1);

                m_entryIndexSize = newEntryIndexSize;
            }
        }
        else
        {
            if (newEntryIndexSize < m_entryIndexSize)
            {
                // Ensure that the table will be redone with new entry index size
                pWriter->UpdateOffsetAdjustment(-1);

                m_entryIndexSize = newEntryIndexSize;
            }
        }
    }

    //
    // VertexHashtable
    //

    // Returns 1 + log2(x) rounded up, 0 iff x == 0
    static unsigned HighestBit(unsigned x)
    {
        unsigned ret = 0;
        while (x != 0)
        {
            x >>= 1;
            ret++;
        }
        return ret;
    }

    // Helper method to back patch entry index in the bucket table
    static void PatchEntryIndex(NativeWriter * pWriter, int patchOffset, int entryIndexSize, int entryIndex)
    {
        if (entryIndexSize == 0)
        {
            pWriter->PatchByteAt(patchOffset, (byte)entryIndex);
        }
        else
            if (entryIndexSize == 1)
            {
                pWriter->PatchByteAt(patchOffset, (byte)entryIndex);
                pWriter->PatchByteAt(patchOffset + 1, (byte)(entryIndex >> 8));
            }
            else
            {
                pWriter->PatchByteAt(patchOffset, (byte)entryIndex);
                pWriter->PatchByteAt(patchOffset + 1, (byte)(entryIndex >> 8));
                pWriter->PatchByteAt(patchOffset + 2, (byte)(entryIndex >> 16));
                pWriter->PatchByteAt(patchOffset + 3, (byte)(entryIndex >> 24));
            }
    }

    void VertexHashtable::Save(NativeWriter * pWriter)
    {
        // Compute the layout of the table if we have not done it yet
        if (m_nBuckets == 0)
            ComputeLayout();

        int nEntries = (int)m_Entries.size();
        int startOffset = pWriter->GetCurrentOffset();
        int bucketMask = (m_nBuckets - 1);

        // Lowest two bits are entry index size, the rest is log2 number of buckets
        int numberOfBucketsShift = HighestBit(m_nBuckets) - 1;
        pWriter->WriteByte(static_cast<uint8_t>((numberOfBucketsShift << 2) | m_entryIndexSize));

        int bucketsOffset = pWriter->GetCurrentOffset();

        pWriter->WritePad((m_nBuckets + 1) << m_entryIndexSize);

        // For faster lookup at runtime, we store the first entry index even though it is redundant (the value can be
        // inferred from number of buckets)
        PatchEntryIndex(pWriter, bucketsOffset, m_entryIndexSize, pWriter->GetCurrentOffset() - bucketsOffset);

        int iEntry = 0;

        for (int iBucket = 0; iBucket < m_nBuckets; iBucket++)
        {
            while (iEntry < nEntries)
            {
                Entry &e = m_Entries[iEntry];

                if (((e.hashcode >> 8) & bucketMask) != (unsigned)iBucket)
                    break;

                int currentOffset = pWriter->GetCurrentOffset();
                pWriter->UpdateOffsetAdjustment(currentOffset - e.offset);
                e.offset = currentOffset;

                pWriter->WriteByte((byte)e.hashcode);
                pWriter->WriteRelativeOffset(e.pVertex);

                iEntry++;
            }

            int patchOffset = bucketsOffset + ((iBucket + 1) << m_entryIndexSize);

            PatchEntryIndex(pWriter, patchOffset, m_entryIndexSize, pWriter->GetCurrentOffset() - bucketsOffset);
        }
        assert(iEntry == nEntries);

        int maxIndexEntry = (pWriter->GetCurrentOffset() - bucketsOffset);
        int newEntryIndexSize = 0;
        if (maxIndexEntry > 0xFF)
        {
            newEntryIndexSize++;
            if (maxIndexEntry > 0xFFFF)
                newEntryIndexSize++;
        }

        if (pWriter->IsGrowing())
        {
            if (newEntryIndexSize > m_entryIndexSize)
            {
                // Ensure that the table will be redone with new entry index size
                pWriter->UpdateOffsetAdjustment(1);

                m_entryIndexSize = newEntryIndexSize;
            }
        }
        else
        {
            if (newEntryIndexSize < m_entryIndexSize)
            {
                // Ensure that the table will be redone with new entry index size
                pWriter->UpdateOffsetAdjustment(-1);

                m_entryIndexSize = newEntryIndexSize;
            }
        }
    }

    void VertexHashtable::ComputeLayout()
    {
        unsigned bucketsEstimate = (unsigned)(m_Entries.size() / m_nFillFactor);

        // Round number of buckets up to the power of two
        m_nBuckets = 1 << HighestBit(bucketsEstimate);

        // Lowest byte of the hashcode is used for lookup within the bucket. Keep it sorted too so that
        // we can use the ordering to terminate the lookup prematurely.
        unsigned mask = ((m_nBuckets - 1) << 8) | 0xFF;

        // sort it by hashcode
        std::sort(m_Entries.begin(), m_Entries.end(),
            [=](Entry const& a, Entry const& b)
            {
                return (a.hashcode & mask) < (b.hashcode & mask);
            }
        );

        // Start with maximum size entries
        m_entryIndexSize = 2;
    }
}
