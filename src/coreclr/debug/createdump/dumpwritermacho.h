// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

struct ThreadCommand
{
    thread_command command;
    uint32_t gpflavor;
    uint32_t gpcount;
#if defined(__x86_64__)
    x86_thread_state64_t gpregisters;
#elif defined(__aarch64__)
    arm_thread_state64_t gpregisters;
#endif
    uint32_t fpflavor;
    uint32_t fpcount;
#if defined(__x86_64__)
    x86_float_state64_t fpregisters;
#elif defined(__aarch64__)
    arm_neon_state64_t fpregisters;
#endif
};

class DumpWriter
{
private:
    int m_fd;
    CrashInfo& m_crashInfo;

    std::vector<segment_command_64> m_segmentLoadCommands;
    std::vector<ThreadCommand> m_threadLoadCommands;
    BYTE m_tempBuffer[0x4000];

public:
    DumpWriter(CrashInfo& crashInfo);
    virtual ~DumpWriter();
    bool WriteDump(std::string& dumpFileName);
    void WriteCrashReport(std::string& dumpFileName);
    static bool WriteData(int fd, const void* buffer, size_t length);

private:
    bool OpenDump(const char* dumpFileName);
    void WriteCrashReport(JsonWriter& writer);
    void WriteStackFrame(JsonWriter& writer, const StackFrame& frame);
    void BuildSegmentLoadCommands();
    void BuildThreadLoadCommands();
    bool WriteHeader(uint64_t* pFileOffset);
    bool WriteSegments();
    bool WriteData(const void* buffer, size_t length) { return WriteData(m_fd, buffer, length); }
};
