// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

#ifndef PT_ARM_EXIDX
#define PT_ARM_EXIDX   0x70000001      /* See llvm ELF.h */
#endif

extern CrashInfo* g_crashInfo;

int g_readProcessMemoryErrno = 0;

bool GetProcessInfo(pid_t pid, pid_t* ppid, pid_t* tgid, std::string* name);

bool
CrashInfo::Initialize()
{
    CLRConfigNoCache disablePagemapUse = CLRConfigNoCache::Get("DbgDisablePagemapUse", /*noprefix*/ false, &getenv);
    DWORD val = 0;
    if (disablePagemapUse.IsSet() && disablePagemapUse.TryAsInteger(10, val) && val == 0)
    {
        TRACE("DbgDisablePagemapUse detected - pagemap file checking is enabled\n");
        char pagemapPath[128];
        int chars = snprintf(pagemapPath, sizeof(pagemapPath), "/proc/%u/pagemap", Pid());
        if (chars <= 0 || (size_t)chars >= sizeof(pagemapPath))
        {
            printf_error("snprintf failed building /proc/<pid>/pagemap name\n");
            return false;
        }
        m_fdPagemap = open(pagemapPath, O_RDONLY);
        if (m_fdPagemap == -1)
        {
            TRACE("open(%s) FAILED %d (%s), will fallback to dumping all memory regions without checking if they are committed\n", pagemapPath, errno, strerror(errno));
        }
    }
    else
    {
        m_fdPagemap = -1;
    }

    return true;
}

void
CrashInfo::CleanupAndResumeProcess()
{
    if (m_fdPagemap != -1)
    {
        close(m_fdPagemap);
        m_fdPagemap = -1;
    }
}

bool
CrashInfo::CopyDumpWriterRegions(
    DynamicArray<ModuleRegion>& moduleMappings,
    DynamicArray<MemoryRegion>& dumpRegions) const
{
    for (const ModuleRegion& mapping : m_moduleMappings)
    {
        ModuleRegion moduleMapping(static_cast<const MemoryRegion&>(mapping));
        if (!moduleMapping.SetFileName(mapping.FileName()) || !moduleMappings.Add(Move(moduleMapping)))
        {
            return false;
        }
    }

    for (const MemoryRegion& region : m_memoryRegions)
    {
        if (!dumpRegions.Add(region))
        {
            return false;
        }
    }
    return true;
}

//
// Get the module mappings for the core dump NT_FILE notes
//
bool
CrashInfo::EnumerateMemoryRegions()
{
}

//
// All the shared (native) module info to the core dump
//
bool
CrashInfo::GetDSOInfo()
{
    Phdr* phdrAddr = reinterpret_cast<Phdr*>(m_processInfo.AuxvValue(AT_PHDR));
    int phnum = m_processInfo.AuxvValue(AT_PHNUM);
    assert(m_processInfo.AuxvValue(AT_PHENT) == sizeof(Phdr));
    assert(phnum != PN_XNUM);
    return EnumerateElfInfo(phdrAddr, phnum);
}

//
// Add all the necessary ELF headers to the core dump
//
void
CrashInfo::VisitModule(uint64_t baseAddress, std::string& moduleName)
{
    if (baseAddress == 0 || baseAddress == m_processInfo.AuxvValue(AT_SYSINFO_EHDR)) {
        return;
    }
    // For reasons unknown the main app singlefile module name is empty in the DSO. This replaces
    // it with the one found in the /proc/<pid>/maps.
    if (moduleName.empty())
    {
        ModuleRegion search(0, baseAddress, baseAddress + PAGE_SIZE);
        const ModuleRegion* region = SearchModuleRegions(search);
        if (region != nullptr)
        {
            moduleName = region->FileName();
            TRACE("VisitModule using module name from mappings '%s'\n", moduleName.c_str());
        }
    }
    AddModuleInfo(false, baseAddress, nullptr, moduleName);
    if (m_coreclrPath.empty())
    {
        size_t last = moduleName.rfind(DIRECTORY_SEPARATOR_STR_A MAKEDLLNAME_A("coreclr"));
        if (last != std::string::npos)
        {
            m_coreclrPath = moduleName.substr(0, last + 1);
            m_runtimeBaseAddress = baseAddress;

            // Now populate the elfreader with the runtime module info and
            // lookup the DAC table symbol to ensure that all the memory
            // necessary is in the core dump.
            if (PopulateForSymbolLookup(baseAddress))
            {
                uint64_t symbolOffset;
                if (!TryLookupSymbol(DACCESS_TABLE_SYMBOL, &symbolOffset))
                {
                    TRACE("TryLookupSymbol(" DACCESS_TABLE_SYMBOL ") FAILED\n");
                }
            }
        }
        else if (m_appModel == AppModelType::SingleFile)
        {
            if (PopulateForSymbolLookup(baseAddress))
            {
                uint64_t symbolOffset;
                if (TryLookupSymbol("DotNetRuntimeInfo", &symbolOffset))
                {
                    m_coreclrPath = GetDirectory(moduleName);
                    m_runtimeBaseAddress = baseAddress;

                    // explicit initialization for old gcc support; instead of just runtimeInfo { }
                    RuntimeInfo runtimeInfo { .Signature = { }, .Version = 0, .RuntimeModuleIndex = { }, .DacModuleIndex = { }, .DbiModuleIndex = { }, .RuntimeVersion = { } };
                    if (ReadMemory(baseAddress + symbolOffset, &runtimeInfo, sizeof(RuntimeInfo)))
                    {
                        if (strcmp(runtimeInfo.Signature, RUNTIME_INFO_SIGNATURE) == 0)
                        {
                            TRACE("Found valid single-file runtime info\n");
                        }
                    }
                }
            }
        }
        else if (m_appModel == AppModelType::NativeAOT)
        {
            if (PopulateForSymbolLookup(baseAddress))
            {
                uint64_t symbolOffset;
                if (TryLookupSymbol("DotNetRuntimeContractDescriptor", &symbolOffset))
                {
                    m_coreclrPath = GetDirectory(moduleName);
                    m_runtimeBaseAddress = baseAddress;
                    TRACE("Found valid NativeAOT runtime module\n");
                }
            }
        }
    }
    EnumerateProgramHeaders(baseAddress);
}

// Helper for PAL_GetUnwindInfoSize. Reads memory directly without adding it to the memory region list.
BOOL
ReadMemoryAdapter(PVOID address, PVOID buffer, SIZE_T size)
{
    size_t read = 0;
    return g_crashInfo->ReadProcessMemory(CONVERT_FROM_SIGN_EXTENDED(address), buffer, size, &read);
}

//
// Called for each program header adding the build id note, unwind frame
// region and module addresses to the crash info.
//
void
CrashInfo::VisitProgramHeader(uint64_t loadbias, uint64_t baseAddress, Phdr* phdr)
{
    switch (phdr->p_type)
    {
    case PT_DYNAMIC:
    case PT_NOTE:
#if defined(TARGET_ARM)
    case PT_ARM_EXIDX:
#endif
        if (phdr->p_vaddr != 0 && phdr->p_memsz != 0)
        {
            InsertMemoryRegion(loadbias + phdr->p_vaddr, phdr->p_memsz);
        }
        break;

    case PT_GNU_EH_FRAME:
        if (phdr->p_vaddr != 0 && phdr->p_memsz != 0)
        {
            uint64_t ehFrameHdrStart = loadbias + phdr->p_vaddr;
            uint64_t ehFrameHdrSize = phdr->p_memsz;
            TRACE("VisitProgramHeader: ehFrameHdrStart %" PRIA PRIx64 " ehFrameHdrSize %08" PRIx64 "\n", ehFrameHdrStart, ehFrameHdrSize);
            InsertMemoryRegion(ehFrameHdrStart, ehFrameHdrSize);

            if (m_appModel != AppModelType::NativeAOT)
            {
                ULONG64 ehFrameStart;
                ULONG64 ehFrameSize;
                if (PAL_GetUnwindInfoSize(baseAddress, ehFrameHdrStart, ReadMemoryAdapter, &ehFrameStart, &ehFrameSize))
                {
                    TRACE("VisitProgramHeader: ehFrameStart %" PRIA PRIx64 " ehFrameSize %08" PRIx64 "\n", ehFrameStart, ehFrameSize);
                    if (ehFrameStart != 0 && ehFrameSize != 0)
                    {
                        InsertMemoryRegion(ehFrameStart, ehFrameSize);
                    }
                }
                else
                {
                    TRACE("VisitProgramHeader: PAL_GetUnwindInfoSize FAILED\n");
                }
            }
        }
        break;

    case PT_LOAD:
        AddModuleAddressRange(loadbias + phdr->p_vaddr, loadbias + phdr->p_vaddr + phdr->p_memsz, baseAddress);
        break;
    }
}

//
// Get the memory region flags for a start address
//
uint32_t
CrashInfo::GetMemoryRegionFlags(uint64_t start)
{
    assert(start == CONVERT_FROM_SIGN_EXTENDED(start));

    ModuleRegion search(0, start, start + PAGE_SIZE, 0);
    const ModuleRegion* moduleRegion = SearchModuleRegions(search);
    if (moduleRegion != nullptr) {
        return moduleRegion->Flags();
    }
    const MemoryRegion* region = SearchMemoryRegions(m_otherMappings, search);
    if (region != nullptr) {
        return region->Flags();
    }
    TRACE_VERBOSE("GetMemoryRegionFlags: %" PRIA PRIx64 " FAILED\n", start);
    return PF_R | PF_W | PF_X;
}

//
// Read raw memory
//
bool
CrashInfo::ReadProcessMemory(uint64_t address, void* buffer, size_t size, size_t* read)
{
    return m_processInfo.ReadProcessMemory(address, buffer, size, read);
}

//
// Get the process or thread status
//
bool
GetStatus(pid_t pid, pid_t* ppid, pid_t* tgid, std::string* name)
{
    char statusPath[128];
    int chars = snprintf(statusPath, sizeof(statusPath), "/proc/%d/status", pid);
    if (chars <= 0 || (size_t)chars >= sizeof(statusPath))
    {
        printf_error("snprintf failed building /proc/<pid>/status\n");
        return false;
    }

    FILE *statusFile = fopen(statusPath, "rb");
    if (statusFile == nullptr)
    {
        printf_error("GetStatus fopen(%s) FAILED %s (%d)\n", statusPath, strerror(errno), errno);
        return false;
    }

    *ppid = -1;

    char *line = nullptr;
    size_t lineLen = 0;
    ssize_t read;
    while ((read = getline(&line, &lineLen, statusFile)) != -1)
    {
        if (strncmp("PPid:\t", line, 6) == 0)
        {
            *ppid = atoll(line + 6);
        }
        else if (strncmp("Tgid:\t", line, 6) == 0)
        {
            *tgid = atoll(line + 6);
        }
        else if (strncmp("Name:\t", line, 6) == 0)
        {
            if (name != nullptr)
            {
                char* n = strchr(line + 6, '\n');
                if (n != nullptr)
                {
                    *n = '\0';
                }
                *name = line + 6;
            }
        }
    }

    free(line);
    fclose(statusFile);
    return true;
}

bool
GetProcessInfo(pid_t pid, pid_t* ppid, pid_t* tgid, std::string* name)
{
    if(!GetStatus(pid, ppid, tgid, name))
    {
        return false;
    }

    // Try reading the executable name from the /proc/<pid>/exe link. Prefer this name to the
    // one reported by status if it is available because the status name is often truncated
    char exePath[128];
    int chars = snprintf(exePath, sizeof(exePath), "/proc/%d/exe", pid);
    if (chars > 0 && (size_t)chars < sizeof(exePath))
    {
        struct stat sb;
        if (lstat(exePath, &sb) != -1)
        {
            ssize_t bufSize = sb.st_size == 0 ? 4096 : sb.st_size + 1;
            char *buf = static_cast<char*>(malloc(bufSize));
            if (buf != nullptr)
            {
                ssize_t nbytes = readlink(exePath, buf, bufSize - 1);
                if (nbytes != -1)
                {
                    buf[nbytes] = '\0';
                    char* executableName = strrchr(buf, '/');
                    *name = (executableName != nullptr) ? (executableName + 1) : buf;
                }
                free(buf);
            }
        }
    }

    return true;
}

void
ModuleInfo::LoadModule()
{
    if (m_module == nullptr)
    {
        m_module = dlopen(m_moduleName.c_str(), RTLD_LAZY);
        if (m_module != nullptr)
        {
            m_localBaseAddress = ((struct link_map*)m_module)->l_addr;
        }
        else
        {
            TRACE("LoadModule: dlopen(%s) FAILED %s\n", m_moduleName.c_str(), dlerror());
        }
    }
}
