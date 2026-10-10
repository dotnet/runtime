// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

struct ThreadCommand
{
    thread_command command;
    uint32_t gpflavor;
    uint32_t gpcount;
    // LC_THREAD state data is a tightly packed array of uint32_t values. Typed
    // state structures can add architecture-dependent padding to the command.
#if defined(__x86_64__)
    uint32_t gpregisters[x86_THREAD_STATE64_COUNT];
#elif defined(__aarch64__)
    uint32_t gpregisters[ARM_THREAD_STATE64_COUNT];
#endif
    uint32_t fpflavor;
    uint32_t fpcount;
#if defined(__x86_64__)
    uint32_t fpregisters[x86_FLOAT_STATE64_COUNT];
#elif defined(__aarch64__)
    uint32_t fpregisters[ARM_NEON_STATE64_COUNT];
#endif
#if defined(__x86_64__)
    // 64-bit Mach-O load commands must have a size that is a multiple of 8.
    uint32_t padding;
#endif
};

// ThreadCommand is serialized directly as an LC_THREAD load command. Verify
// that each state immediately follows its flavor and count without padding.
static_assert(offsetof(ThreadCommand, gpregisters) == sizeof(thread_command) + (2 * sizeof(uint32_t)));
#if defined(__x86_64__)
static_assert(offsetof(ThreadCommand, fpflavor) == offsetof(ThreadCommand, gpregisters) + sizeof(x86_thread_state64_t));
static_assert(offsetof(ThreadCommand, fpregisters) == offsetof(ThreadCommand, fpflavor) + (2 * sizeof(uint32_t)));
static_assert(offsetof(ThreadCommand, padding) == offsetof(ThreadCommand, fpregisters) + sizeof(x86_float_state64_t));
static_assert(sizeof(ThreadCommand) == offsetof(ThreadCommand, padding) + sizeof(uint32_t));
#elif defined(__aarch64__)
static_assert(offsetof(ThreadCommand, fpflavor) == offsetof(ThreadCommand, gpregisters) + sizeof(arm_thread_state64_t));
static_assert(offsetof(ThreadCommand, fpregisters) == offsetof(ThreadCommand, fpflavor) + (2 * sizeof(uint32_t)));
static_assert(sizeof(ThreadCommand) == offsetof(ThreadCommand, fpregisters) + sizeof(arm_neon_state64_t));
#endif
static_assert((sizeof(ThreadCommand) & 7) == 0);

class DumpWriter
{
private:
    int m_fd;
    CrashInfo& m_crashInfo;

    std::vector<segment_command_64> m_segmentLoadCommands;
    std::vector<ThreadCommand> m_threadLoadCommands;
    note_command m_processMetadataNote{};
    std::string m_processMetadata;
    BYTE m_tempBuffer[0x4000];

    // no public copy constructor
    DumpWriter(const DumpWriter&) = delete;
    void operator=(const DumpWriter&) = delete;

public:
    DumpWriter(CrashInfo& crashInfo);
    virtual ~DumpWriter();
    bool OpenAndWriteDump(const char* dumpFileName);
    static bool WriteData(int fd, const void* buffer, size_t length);

private:
    bool OpenDump(const char* dumpFileName);
    bool WriteDump();
    bool WriteDiagInfo(size_t size);
    void BuildProcessMetadataNote();
    void BuildSegmentLoadCommands();
    void BuildThreadLoadCommands();
    bool WriteHeader(uint64_t* pFileOffset);
    bool WriteSegments();
    bool WriteData(const void* buffer, size_t length) { return WriteData(m_fd, buffer, length); }
};
