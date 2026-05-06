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
    void* m_module;
    uint64_t m_localBaseAddress;

    // no public copy constructor
    ModuleInfo(const ModuleInfo&) = delete;
    void operator=(const ModuleInfo&) = delete;

    void LoadModule();

public:
    ModuleInfo() :
        m_baseAddress(0),
        m_module(nullptr),
        m_localBaseAddress(0)
    {
    }

    ModuleInfo(uint64_t baseAddress) :
        m_baseAddress(baseAddress),
        m_module(nullptr),
        m_localBaseAddress(0)
    {
    }

    ModuleInfo(bool isManaged, uint64_t baseAddress, uint32_t timeStamp, uint32_t imageSize, GUID* mvid, const std::string& moduleName) :
        m_baseAddress(baseAddress),
        m_timeStamp(timeStamp),
        m_imageSize(imageSize),
        m_mvid(*mvid),
        m_moduleName(moduleName),
        m_isManaged(isManaged),
        m_module(nullptr),
        m_localBaseAddress(0)
    {
    }

    ~ModuleInfo()
    {
        if (m_module != nullptr)
        {
            dlclose(m_module);
            m_module = nullptr;
        }
    }

    inline bool IsManaged() const { return m_isManaged; }
    inline uint64_t BaseAddress() const { return m_baseAddress; }
    inline uint32_t TimeStamp() const { return m_timeStamp; }
    inline uint32_t ImageSize() const { return m_imageSize; }
    inline const GUID* Mvid() const { return &m_mvid; }
    inline const std::string& ModuleName() const { return m_moduleName; }

    const char* GetSymbolName(uint64_t address);
};
