// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

struct StackFrame
{
private:
    uint64_t m_moduleAddress;
    uint64_t m_returnAddress;
    uint64_t m_stackPointer;
    uint32_t m_nativeOffset;
    uint32_t m_token;
    uint32_t m_ilOffset;
    bool m_isManaged;

public:
    // Create native stack frame
    StackFrame(uint64_t moduleAddress, uint64_t returnAddress, uint64_t stackPointer, uint32_t nativeOffset) :
        m_moduleAddress(moduleAddress),
        m_returnAddress(returnAddress),
        m_stackPointer(stackPointer),
        m_nativeOffset(nativeOffset),
        m_token(0),
        m_ilOffset(0),
        m_isManaged(false)
    {
    }

    // Create managed stack frame
    StackFrame(uint64_t moduleAddress, uint64_t returnAddress, uint64_t stackPointer, uint32_t nativeOffset, uint64_t token, uint32_t ilOffset) :
        m_moduleAddress(moduleAddress),
        m_returnAddress(returnAddress),
        m_stackPointer(stackPointer),
        m_nativeOffset(nativeOffset),
        m_token(token),
        m_ilOffset(ilOffset),
        m_isManaged(true)
    {
    }

    inline uint64_t ModuleAddress() const { return m_moduleAddress; }
    inline uint64_t ReturnAddress() const { return m_returnAddress; }
    inline uint64_t StackPointer() const { return m_stackPointer; }
    inline uint32_t NativeOffset() const { return m_nativeOffset; }
    inline uint32_t Token() const { return m_token; }
    inline uint32_t ILOffset() const { return m_ilOffset; }
    inline bool IsManaged() const { return m_isManaged; }

    bool operator<(const StackFrame& rhs) const
    {
        return m_stackPointer < rhs.m_stackPointer;
    }
};
