// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ---------------------------------------------------------------------------
// NativeFormatWriter
//
// Utilities to write native data to images, that can be read by the NativeFormat.Reader class
// ---------------------------------------------------------------------------

#pragma once

#include <assert.h>
#include <stdint.h>

// To reduce differences between C# and C++ versions
#define byte uint8_t

#define UInt16 uint16_t
#define UInt32 uint32_t
#define UInt64 uint64_t

#include <clr_std/vector>

namespace NativeFormat
{
    using namespace std;

    class NativeSection;
    class NativeWriter;

    class Vertex
    {
        friend class NativeWriter;
        friend class NativeSection;

        int m_offset;
        int m_iteration; // Iteration that the offset is valid for

        static const int NotPlaced = -1;
        static const int Placed = -2;

    public:
        Vertex()
            : m_offset(Vertex::NotPlaced), m_iteration(-1)
        {
        }

        virtual ~Vertex() {}

        virtual void Save(NativeWriter * pWriter) = 0;

        int GetOffset()
        {
            assert(m_offset >= 0);
            return m_offset;
        }
    };

    class NativeSection : vector<Vertex *>
    {
        friend class NativeWriter;

    public:
        Vertex * Place(Vertex * pVertex);

        Vertex * Pop()
        {
            Vertex * pVertex = *(end() - 1);
            erase(end() - 1);

            assert(pVertex->m_offset == Vertex::Placed);
            pVertex->m_offset = Vertex::NotPlaced;

            return pVertex;
        }
    };

    class NativeWriter
    {
        vector<NativeSection *> m_Sections;

        enum SavePhase
        {
            Initial,
            Shrinking,
            Growing,
            Done
        };

        vector<byte> m_Buffer;
        int m_iteration;
        SavePhase m_phase; // Current save phase
        int m_offsetAdjustment; // Cumulative offset adjustment compared to previous iteration
        int m_paddingSize; // How much padding was used

    public:
        NativeWriter()
        {
            m_iteration = 0;
            m_phase = Initial;
        }

        NativeSection * NewSection()
        {
            NativeSection * pSection = new NativeSection();
            m_Sections.push_back(pSection);
            return pSection;
        }

        void WriteByte(byte b)
        {
            m_Buffer.push_back(b);
        }

        void WriteUInt16(UInt16 value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value>>8));
        }

        void WriteUInt32(UInt32 value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value>>8));
            WriteByte((byte)(value>>16));
            WriteByte((byte)(value>>24));
        }

        void WritePad(unsigned size)
        {
            while (size > 0)
            {
                WriteByte(0);
                size--;
            }
        }

        bool IsGrowing()
        {
            return m_phase == Growing;
        }

        void UpdateOffsetAdjustment(int offsetDelta)
        {
            switch (m_phase)
            {
            case Shrinking:
                m_offsetAdjustment = min(m_offsetAdjustment, offsetDelta);
                break;
            case Growing:
                m_offsetAdjustment = max(m_offsetAdjustment, offsetDelta);
                break;
            default:
                break;
            }
        }

        void RollbackTo(int offset)
        {
            m_Buffer.erase(m_Buffer.begin() + offset, m_Buffer.end());
        }

        void RollbackTo(int offset, int offsetAdjustment)
        {
            m_offsetAdjustment = offsetAdjustment;
            RollbackTo(offset);
        }

        void PatchByteAt(int offset, byte value)
        {
            m_Buffer[offset] = value;
        }

        //
        // Same encoding as what's used by CTL
        //
        void WriteUnsigned(unsigned d);
        static unsigned GetUnsignedEncodingSize(unsigned d);

        template <typename T>
        void WriteUnsigned(T d)
        {
            WriteUnsigned((unsigned)d);
        }

        void WriteSigned(int i);

        void WriteRelativeOffset(Vertex * pVal);

        int GetExpectedOffset(Vertex * pVal);

        int GetCurrentOffset(Vertex * pVal)
        {
            if (pVal->m_iteration != m_iteration)
                return -1;
            return pVal->m_offset;
        }

        void SetCurrentOffset(Vertex * pVal)
        {
            pVal->m_iteration = m_iteration;
            pVal->m_offset = GetCurrentOffset();
        }

        int GetCurrentOffset()
        {
            return (int)m_Buffer.size();
        }

        int GetNumberOfIterations()
        {
            return m_iteration;
        }

        int GetPaddingSize()
        {
            return m_paddingSize;
        }

        vector<byte>& Save();
    };


    //
    // Data structure building blocks
    //

    class UnsignedConstant : public Vertex
    {
        unsigned m_value;

    public:
        UnsignedConstant(unsigned value)
            : m_value(value)
        {
        }

        virtual void Save(NativeWriter * pWriter)
        {
            pWriter->WriteUnsigned(m_value);
        }
    };

    //
    // Sparse array. Good for random access based on index
    //
    class VertexArray : public Vertex
    {
        vector<Vertex *> m_Entries;

        NativeSection * m_pSection;
        vector<Vertex *> m_Blocks;

        static const int _blockSize = 16;

        // Current size of index entry
        int m_entryIndexSize; // 0 - uint8, 1 - uint16, 2 - uint32

        class VertexLeaf : public Vertex
        {
        public:
            Vertex * m_pVertex;
            size_t m_leafIndex;

            virtual void Save(NativeWriter * pWriter);
        };

        class VertexTree : public Vertex
        {
        public:
            Vertex * m_pFirst;
            Vertex * m_pSecond;

            virtual void Save(NativeWriter * pWriter);
        };

        Vertex * ExpandBlock(size_t index, int depth, bool place, bool * pLeaf);

    public:
        VertexArray(NativeSection * pSection)
            : m_pSection(pSection)
        {
        }

        void Set(int index, Vertex * pElement)
        {
            while ((size_t)index >= m_Entries.size())
                m_Entries.push_back(nullptr);

            m_Entries[index] = pElement;
        }

        void ExpandLayout();

        virtual void Save(NativeWriter * pWriter);
    };

    //
    // Hashtable. Good for random access based on hashcode + key
    //
    class VertexHashtable : public Vertex
    {
        struct Entry
        {
            Entry()
                : offset(-1), hashcode(0), pVertex(NULL)
            {
            }

            Entry(unsigned hashcode, Vertex * pVertex)
                : offset(0), hashcode(hashcode), pVertex(pVertex)
            {
            }

            int offset;

            unsigned hashcode;
            Vertex * pVertex;
        };

        vector<Entry> m_Entries;

        // How many entries to target per bucket. Higher fill factor means smaller size, but worse runtime perf.
        int m_nFillFactor;

        // Number of buckets choosen for the table. Must be power of two. 0 means that the table is still open for mutation.
        int m_nBuckets;

        // Current size of index entry
        int m_entryIndexSize; // 0 - uint8, 1 - uint16, 2 - uint32

        void ComputeLayout();

    public:
        static const int DefaultFillFactor = 13;

        VertexHashtable(int fillFactor = DefaultFillFactor)
        {
            m_nBuckets = 0;

            m_nFillFactor = fillFactor;
        }

        void Append(unsigned hashcode, Vertex * pElement)
        {
            // The table needs to be open for mutation
            assert(m_nBuckets == 0);

            m_Entries.push_back(Entry(hashcode, pElement));
        }

        virtual void Save(NativeWriter * pWriter);
    };
};
