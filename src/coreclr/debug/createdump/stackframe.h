// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

struct StackFrame
{
private:
    uint64_t m_moduleAddress;
    uint64_t m_instructionPointer;
    uint64_t m_stackPointer;
    uint32_t m_nativeOffset;
    uint32_t m_token;
    uint32_t m_ilOffset;
    IXCLRDataMethodInstance* m_pMethod;
    bool m_isManaged;

public:
    // Create native stack frame
    StackFrame(uint64_t moduleAddress, uint64_t instructionPointer, uint64_t stackPointer, uint32_t nativeOffset) :
        m_moduleAddress(moduleAddress),
        m_instructionPointer(instructionPointer),
        m_stackPointer(stackPointer),
        m_nativeOffset(nativeOffset),
        m_token(0),
        m_ilOffset(0),
        m_pMethod(nullptr),
        m_isManaged(false)
    {
    }

    // Create managed stack frame
    StackFrame(uint64_t moduleAddress, uint64_t instructionPointer, uint64_t stackPointer, IXCLRDataMethodInstance* pMethod, uint32_t nativeOffset, uint64_t token, uint32_t ilOffset) :
        m_moduleAddress(moduleAddress),
        m_instructionPointer(instructionPointer),
        m_stackPointer(stackPointer),
        m_nativeOffset(nativeOffset),
        m_token(token),
        m_ilOffset(ilOffset),
        m_pMethod(pMethod),
        m_isManaged(true)
    {
    }

    // copy constructor
    StackFrame(const StackFrame& frame) :
        m_moduleAddress(frame.m_moduleAddress),
        m_instructionPointer(frame.m_instructionPointer),
        m_stackPointer(frame.m_stackPointer),
        m_nativeOffset(frame.m_nativeOffset),
        m_token(frame.m_token),
        m_ilOffset(frame.m_ilOffset),
        m_pMethod(frame.m_pMethod),
        m_isManaged(frame.m_isManaged)
    {
        if (m_pMethod != nullptr)
        {
            m_pMethod->AddRef();
        }
    }

    ~StackFrame()
    {
        if (m_pMethod != nullptr)
        {
            m_pMethod->Release();
            m_pMethod = nullptr;
        }
    }

    inline uint64_t ModuleAddress() const { return m_moduleAddress; }
    inline uint64_t InstructionPointer() const { return m_instructionPointer; }
    inline uint64_t StackPointer() const { return m_stackPointer; }
    inline uint32_t NativeOffset() const { return m_nativeOffset; }
    inline uint32_t Token() const { return m_token; }
    inline uint32_t ILOffset() const { return m_ilOffset; }
    inline bool IsManaged() const { return m_isManaged; }
    inline IXCLRDataMethodInstance* GetMethod() const { return m_pMethod; }

    bool operator<(const StackFrame& rhs) const
    {
        return m_stackPointer < rhs.m_stackPointer;
    }
};
