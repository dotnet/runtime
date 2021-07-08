// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

struct ModuleInfo
{
private:
    uint64_t m_baseAddress;
    uint32_t m_timeStamp;
    uint32_t m_imageSize;
    GUID m_mvid;
    std::string m_moduleName;
    bool m_isManaged;

public:
    ModuleInfo(uint64_t baseAddress) :
        m_baseAddress(baseAddress)
    {
    }

    ModuleInfo(bool isManaged, uint64_t baseAddress, uint32_t timeStamp, uint32_t imageSize, GUID* mvid, const std::string& moduleName) :
        m_baseAddress(baseAddress),
        m_timeStamp(timeStamp),
        m_imageSize(imageSize),
        m_mvid(*mvid),
        m_moduleName(moduleName),
        m_isManaged(isManaged)
    {
    }

    // copy constructor
    ModuleInfo(const ModuleInfo& moduleInfo) :
        m_baseAddress(moduleInfo.m_baseAddress),
        m_timeStamp(moduleInfo.m_timeStamp),
        m_imageSize(moduleInfo.m_imageSize),
        m_mvid(moduleInfo.m_mvid),
        m_moduleName(moduleInfo.m_moduleName),
        m_isManaged(moduleInfo.m_isManaged)
    {
    }

    ~ModuleInfo()
    {
    }

    inline bool IsManaged() const { return m_isManaged; }
    inline uint64_t BaseAddress() const { return m_baseAddress; }
    inline uint32_t TimeStamp() const { return m_timeStamp; }
    inline uint32_t ImageSize() const { return m_imageSize; }
    inline const GUID* Mvid() const { return &m_mvid; }
    inline const std::string& ModuleName() const { return m_moduleName; }

    bool operator<(const ModuleInfo& rhs) const
    {
        return m_baseAddress < rhs.m_baseAddress;
    }

    void Trace() const
    {
        TRACE("%" PRIA PRIx64 " %s\n", m_baseAddress, m_moduleName.c_str());
    }
};
