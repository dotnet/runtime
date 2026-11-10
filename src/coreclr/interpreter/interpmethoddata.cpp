// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpreter.h"
#include "interpmethoddata.h"

uint32_t InterpMethodDataBuilder::AlignUp(uint32_t value, uint32_t alignment)
{
    assert(alignment != 0);
    assert((alignment & (alignment - 1)) == 0);
    return (value + alignment - 1) & ~(alignment - 1);
}

InterpMethodDataBuilder::InterpMethodDataBuilder()
{
    // Initialize section alignments
    m_sections[(int)InterpMethodDataSection::Header].alignment = sizeof(void*);
    m_sections[(int)InterpMethodDataSection::Bytecode].alignment = sizeof(int32_t);
    m_sections[(int)InterpMethodDataSection::InterpMethod].alignment = sizeof(void*);
    m_sections[(int)InterpMethodDataSection::DataItems].alignment = sizeof(void*);
    m_sections[(int)InterpMethodDataSection::AsyncSuspendData].alignment = sizeof(void*);
    m_sections[(int)InterpMethodDataSection::IntervalMaps].alignment = sizeof(uint32_t);

    // Header is always sizeof(InterpMethod*) for the InterpByteCodeStart
    m_sections[(int)InterpMethodDataSection::Header].size = sizeof(InterpMethod*);
}

InterpMethodDataBuilder::~InterpMethodDataBuilder()
{
}

InterpSectionRef InterpMethodDataBuilder::AllocateInSection(InterpMethodDataSection section, uint32_t size, uint32_t alignment)
{
    assert(!m_finalized);
    InterpSectionData& sectionData = m_sections[(int)section];

    if (alignment == 0)
    {
        alignment = sectionData.alignment;
    }

    // Align the current size
    uint32_t alignedOffset = AlignUp(sectionData.size, alignment);
    sectionData.size = alignedOffset + size;

    return InterpSectionRef(section, alignedOffset);
}

void InterpMethodDataBuilder::SetBytecodeSize(uint32_t sizeInBytes)
{
    assert(!m_finalized);
    m_sections[(int)InterpMethodDataSection::Bytecode].size = sizeInBytes;
}

uint32_t InterpMethodDataBuilder::GetTotalSize()
{
    uint32_t offset = 0;

    // Sections are laid out in order
    for (int i = 0; i < (int)InterpMethodDataSection::Count; i++)
    {
        InterpSectionData& section = m_sections[i];
        if (section.size > 0)
        {
            offset = AlignUp(offset, section.alignment);
            section.finalOffset = offset;
            offset += section.size;
        }
        else
        {
            section.finalOffset = offset; // Empty section points to next
        }
    }

    return offset;
}

uint32_t InterpMethodDataBuilder::GetSectionOffset(InterpMethodDataSection section) const
{
    return m_sections[(int)section].finalOffset;
}

uint32_t InterpMethodDataBuilder::GetSectionSize(InterpMethodDataSection section) const
{
    return m_sections[(int)section].size;
}

void* InterpMethodDataBuilder::GetFinalPointer(InterpSectionRef ref) const
{
    assert(m_finalized);
    return m_finalBaseAddress + m_sections[(int)ref.section].finalOffset + ref.offset;
}

void InterpMethodDataBuilder::Finalize(void* baseAddressRW, void* baseAddressRX)
{
    assert(!m_finalized);
    m_finalBaseAddress = (uint8_t*)baseAddressRX;

    m_finalized = true;
}

InterpByteCodeStart* InterpMethodDataBuilder::GetByteCodeStart() const
{
    assert(m_finalized);
    return (InterpByteCodeStart*)m_finalBaseAddress;
}

void* InterpMethodDataBuilder::GetWritablePointer(void* baseAddressRW, InterpSectionRef ref) const
{
    return (uint8_t*)baseAddressRW + m_sections[(int)ref.section].finalOffset + ref.offset;
}

InterpSectionRef InterpMethodDataBuilder::AllocateInterpMethod()
{
    return AllocateInSection(InterpMethodDataSection::InterpMethod, sizeof(InterpMethod));
}

InterpSectionRef InterpMethodDataBuilder::AllocateDataItems(int32_t count)
{
    return AllocateInSection(InterpMethodDataSection::DataItems, count * sizeof(void*));
}

InterpSectionRef InterpMethodDataBuilder::AllocateAsyncSuspendData()
{
    return AllocateInSection(InterpMethodDataSection::AsyncSuspendData, sizeof(InterpAsyncSuspendData));
}

InterpSectionRef InterpMethodDataBuilder::AllocateIntervalMap(int32_t count)
{
    return AllocateInSection(InterpMethodDataSection::IntervalMaps, count * sizeof(InterpIntervalMapEntry));
}
