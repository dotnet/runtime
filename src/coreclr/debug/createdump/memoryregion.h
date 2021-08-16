// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !defined(PAGE_SIZE) && (defined(__arm__) || defined(__aarch64__))
#define PAGE_SIZE sysconf(_SC_PAGESIZE)
#endif

#undef PAGE_MASK 
#define PAGE_MASK (~(PAGE_SIZE-1))

enum MEMORY_REGION_FLAGS : uint32_t
{
    // PF_X        = 0x01,      // Execute
    // PF_W        = 0x02,      // Write
    // PF_R        = 0x04,      // Read
    MEMORY_REGION_FLAG_PERMISSIONS_MASK = 0x0f,
    MEMORY_REGION_FLAG_SHARED = 0x10,
    MEMORY_REGION_FLAG_PRIVATE = 0x20,
    MEMORY_REGION_FLAG_MEMORY_BACKED = 0x40
};

struct MemoryRegion
{
private:
    uint32_t m_flags;
    uint64_t m_startAddress;
    uint64_t m_endAddress;
    uint64_t m_offset;

    // The name used for NT_FILE output
    std::string m_fileName;

public:
    MemoryRegion(uint32_t flags, uint64_t start, uint64_t end, uint64_t offset, const std::string& filename) :
        m_flags(flags),
        m_startAddress(start),
        m_endAddress(end),
        m_offset(offset),
        m_fileName(filename)
    {
        assert((start & ~PAGE_MASK) == 0);
        assert((end & ~PAGE_MASK) == 0);
    }

    MemoryRegion(uint32_t flags, uint64_t start, uint64_t end, uint64_t offset) :
        m_flags(flags),
        m_startAddress(start),
        m_endAddress(end),
        m_offset(offset)
    {
    }

    MemoryRegion(uint32_t flags, uint64_t start, uint64_t end) :
        m_flags(flags),
        m_startAddress(start),
        m_endAddress(end)
    {
        assert((start & ~PAGE_MASK) == 0);
        assert((end & ~PAGE_MASK) == 0);
    }

    // copy with new file name constructor
    MemoryRegion(const MemoryRegion& region, const std::string& fileName) :
        m_flags(region.m_flags),
        m_startAddress(region.m_startAddress),
        m_endAddress(region.m_endAddress),
        m_offset(region.m_offset),
        m_fileName(fileName)
    {
    }

    // copy with new flags constructor. The file name is not copied.
    MemoryRegion(const MemoryRegion& region, uint32_t flags) :
        m_flags(flags),
        m_startAddress(region.m_startAddress),
        m_endAddress(region.m_endAddress),
        m_offset(region.m_offset)
    {
    }

    // copy constructor
    MemoryRegion(const MemoryRegion& region) :
        m_flags(region.m_flags),
        m_startAddress(region.m_startAddress),
        m_endAddress(region.m_endAddress),
        m_offset(region.m_offset),
        m_fileName(region.m_fileName)
    {
    }

    ~MemoryRegion()
    {
    }

    uint32_t Permissions() const { return m_flags & MEMORY_REGION_FLAG_PERMISSIONS_MASK; }
    inline uint32_t Flags() const { return m_flags; }
    bool IsBackedByMemory() const { return (m_flags & MEMORY_REGION_FLAG_MEMORY_BACKED) != 0; }
    inline uint64_t StartAddress() const { return m_startAddress; }
    inline uint64_t EndAddress() const { return m_endAddress; }
    inline uint64_t Size() const { return m_endAddress - m_startAddress; }
    inline uint64_t Offset() const { return m_offset; }
    inline const std::string& FileName() const { return m_fileName; }

    bool operator<(const MemoryRegion& rhs) const
    {
        return (m_startAddress < rhs.m_startAddress) && (m_endAddress <= rhs.m_startAddress);
    }

    // Returns true if "rhs" is wholly contained in this one
    bool Contains(const MemoryRegion& rhs) const
    {
        return (m_startAddress <= rhs.m_startAddress) && (m_endAddress >= rhs.m_endAddress);
    }

    void Trace() const
    {
        TRACE("%" PRIA PRIx64 " - %" PRIA PRIx64 " (%06" PRIx64 ") %" PRIA PRIx64 " %c%c%c%c%c%c %02x %s\n",
            m_startAddress,
            m_endAddress,
            Size() / PAGE_SIZE,
            m_offset,
            (m_flags & PF_R) ? 'r' : '-',
            (m_flags & PF_W) ? 'w' : '-',
            (m_flags & PF_X) ? 'x' : '-',
            (m_flags & MEMORY_REGION_FLAG_SHARED) ? 's' : '-',
            (m_flags & MEMORY_REGION_FLAG_PRIVATE) ? 'p' : '-',
            (m_flags & MEMORY_REGION_FLAG_MEMORY_BACKED) ? 'b' : '-',
            m_flags,
            m_fileName.c_str());
    }
};
