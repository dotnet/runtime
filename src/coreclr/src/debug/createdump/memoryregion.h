// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

struct MemoryRegion 
{
private:
    uint32_t m_permissions;
    uint64_t m_startAddress;
    uint64_t m_endAddress;
    uint64_t m_offset;

    // The name used for NT_FILE output
    char* m_fileName;

public:
    MemoryRegion(uint64_t start, uint64_t end) : 
        m_permissions(PF_R | PF_W | PF_X),
        m_startAddress(start),
        m_endAddress(end),
        m_offset(0),
        m_fileName(nullptr)
    {
        assert((start & ~PAGE_MASK) == 0);
        assert((end & ~PAGE_MASK) == 0);
    }

    MemoryRegion(uint32_t permissions, uint64_t start, uint64_t end, uint64_t offset, char* filename) : 
        m_permissions(permissions),
        m_startAddress(start),
        m_endAddress(end),
        m_offset(offset),
        m_fileName(filename)
    {
        assert((start & ~PAGE_MASK) == 0);
        assert((end & ~PAGE_MASK) == 0);
    }

    const uint32_t Permissions() const { return m_permissions; }
    const uint64_t StartAddress() const { return m_startAddress; }
    const uint64_t EndAddress() const { return m_endAddress; }
    const uint64_t Size() const { return m_endAddress - m_startAddress; }
    const uint64_t Offset() const { return m_offset; }
    const char* FileName() const { return m_fileName; }

    bool operator<(const MemoryRegion& rhs) const
    {
        return (m_startAddress < rhs.m_startAddress) && (m_endAddress <= rhs.m_startAddress);
    }

    bool Contains(const MemoryRegion& rhs) const
    {
        return (m_startAddress <= rhs.m_startAddress) && (m_endAddress >= rhs.m_endAddress);
    }

    void Cleanup()
    {
        if (m_fileName != nullptr)
        {
            free(m_fileName);
            m_fileName = nullptr;
        }
    }

    void Print() const
    {
        if (m_fileName != nullptr) {
            TRACE("%016lx - %016lx (%06ld) %016lx %x %s\n", m_startAddress, m_endAddress, (Size() >> PAGE_SHIFT), m_offset, m_permissions, m_fileName);
        }
        else {
            TRACE("%016lx - %016lx (%06ld) %x\n", m_startAddress, m_endAddress, (Size() >> PAGE_SHIFT), m_permissions);
        }
    }
};
