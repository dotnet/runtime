// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

Module Name:

    cgroup.cpp

Abstract:
    Read memory and cpu limits for the current process
--*/

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(MISC);
#include "pal/palinternal.h"
#include <sys/resource.h>
#include "pal/virtual.h"
#include "pal/cgroup.h"
#include <algorithm>

#define PROC_MOUNTINFO_FILENAME "/proc/self/mountinfo"
#define PROC_CGROUP_FILENAME "/proc/self/cgroup"
#define PROC_STATM_FILENAME "/proc/self/statm"
#define MEM_LIMIT_FILENAME "/memory.limit_in_bytes"
#define MEM_USAGE_FILENAME "/memory.usage_in_bytes"
#define CFS_QUOTA_FILENAME "/cpu.cfs_quota_us"
#define CFS_PERIOD_FILENAME "/cpu.cfs_period_us"
class CGroup
{
    static char *s_memory_cgroup_path;
    static char *s_cpu_cgroup_path;
public:
    static void Initialize()
    {
        s_memory_cgroup_path = FindCgroupPath(&IsMemorySubsystem);
        s_cpu_cgroup_path = FindCgroupPath(&IsCpuSubsystem);
    }

    static void Cleanup()
    {
        PAL_free(s_memory_cgroup_path);
        PAL_free(s_cpu_cgroup_path);
    }

    static bool GetPhysicalMemoryLimit(uint64_t *val)
    {
        char *mem_limit_filename = nullptr;
        bool result = false;

        if (s_memory_cgroup_path == nullptr)
            return result;

        size_t len = strlen(s_memory_cgroup_path);
        len += strlen(MEM_LIMIT_FILENAME);
        mem_limit_filename = (char*)PAL_malloc(len+1);
        if (mem_limit_filename == nullptr)
            return result;

        strcpy_s(mem_limit_filename, len+1, s_memory_cgroup_path);
        strcat_s(mem_limit_filename, len+1, MEM_LIMIT_FILENAME);
        result = ReadMemoryValueFromFile(mem_limit_filename, val);
        PAL_free(mem_limit_filename);
        return result;
    }

    static bool GetPhysicalMemoryUsage(size_t *val)
    {
        char *mem_usage_filename = nullptr;
        bool result = false;
        uint64_t temp;

        if (s_memory_cgroup_path == nullptr)
            return result;

        size_t len = strlen(s_memory_cgroup_path);
        len += strlen(MEM_USAGE_FILENAME);
        mem_usage_filename = (char*)malloc(len+1);
        if (mem_usage_filename == nullptr)
            return result;

        strcpy(mem_usage_filename, s_memory_cgroup_path);
        strcat(mem_usage_filename, MEM_USAGE_FILENAME);
        result = ReadMemoryValueFromFile(mem_usage_filename, &temp);
        if (result)
        {
            if (temp > std::numeric_limits<size_t>::max())
            {
                *val = std::numeric_limits<size_t>::max();
            }
            else
            {
                *val = (size_t)temp;
            }
        }
        free(mem_usage_filename);
        return result;
    }

    static bool GetCpuLimit(UINT *val)
    {
        long long quota;
        long long period;
        double cpu_count;

        quota = ReadCpuCGroupValue(CFS_QUOTA_FILENAME);
        if (quota <= 0)
            return false;

        period = ReadCpuCGroupValue(CFS_PERIOD_FILENAME);
        if (period <= 0)
            return false;

        // Cannot have less than 1 CPU
        if (quota <= period)
        {
            *val = 1;
            return true;
        }

        // Calculate cpu count based on quota and round it up
        cpu_count = (double) quota / period  + 0.999999999;
        *val = (cpu_count < UINT_MAX) ? (UINT)cpu_count : UINT_MAX;

        return true;
    }

private:
    static bool IsMemorySubsystem(const char *strTok){
        return strcmp("memory", strTok) == 0;
    }

    static bool IsCpuSubsystem(const char *strTok){
        return strcmp("cpu", strTok) == 0;
    }

    static char* FindCgroupPath(bool (*is_subsystem)(const char *)){
        char *cgroup_path = nullptr;
        char *hierarchy_mount = nullptr;
        char *hierarchy_root = nullptr;
        char *cgroup_path_relative_to_mount = nullptr;
        size_t len;

        FindHierarchyMount(is_subsystem, &hierarchy_mount, &hierarchy_root);
        if (hierarchy_mount == nullptr || hierarchy_root == nullptr)
            goto done;

        cgroup_path_relative_to_mount = FindCGroupPathForSubsystem(is_subsystem);
        if (cgroup_path_relative_to_mount == nullptr)
            goto done;

        len = strlen(hierarchy_mount);
        len += strlen(cgroup_path_relative_to_mount);
        cgroup_path = (char*)PAL_malloc(len+1);
        if (cgroup_path == nullptr)
           goto done;

        strcpy_s(cgroup_path, len+1, hierarchy_mount);
        // For a host cgroup, we need to append the relative path.
        // In a docker container, the root and relative path are the same and we don't need to append.
        if (strcmp(hierarchy_root, cgroup_path_relative_to_mount) != 0)
            strcat_s(cgroup_path, len+1, cgroup_path_relative_to_mount);

    done:
        PAL_free(hierarchy_mount);
        PAL_free(hierarchy_root);
        PAL_free(cgroup_path_relative_to_mount);
        return cgroup_path;
    }

    static void FindHierarchyMount(bool (*is_subsystem)(const char *), char** pmountpath, char** pmountroot)
    {
        char *line = nullptr;
        size_t lineLen = 0, maxLineLen = 0;
        char *filesystemType = nullptr;
        char *options = nullptr;
        char *mountpath = nullptr;
        char *mountroot = nullptr;

        FILE *mountinfofile = fopen(PROC_MOUNTINFO_FILENAME, "r");
        if (mountinfofile == nullptr)
            goto done;

        while (getline(&line, &lineLen, mountinfofile) != -1)
        {
            if (filesystemType == nullptr || lineLen > maxLineLen)
            {
                PAL_free(filesystemType);
                PAL_free(options);
                filesystemType = (char*)PAL_malloc(lineLen+1);
                if (filesystemType == nullptr)
                    goto done;
                options = (char*)PAL_malloc(lineLen+1);
                if (options == nullptr)
                    goto done;
                maxLineLen = lineLen;
            }
            char* separatorChar = strstr(line, " - ");;

            // See man page of proc to get format for /proc/self/mountinfo file
            int sscanfRet = sscanf_s(separatorChar,
                                     " - %s %*s %s",
                                     filesystemType, lineLen+1,
                                     options, lineLen+1);
            if (sscanfRet != 2)
            {
                _ASSERTE(!"Failed to parse mount info file contents with sscanf_s.");
                goto done;
            }

            if (strncmp(filesystemType, "cgroup", 6) == 0)
            {
                char* context = nullptr;
                char* strTok = strtok_s(options, ",", &context);
                while (strTok != nullptr)
                {
                    if (is_subsystem(strTok))
                    {
                        mountpath = (char*)PAL_malloc(lineLen+1);
                        if (mountpath == nullptr)
                            goto done;
                        mountroot = (char*)PAL_malloc(lineLen+1);
                        if (mountroot == nullptr)
                            goto done;

                        sscanfRet = sscanf_s(line,
                                             "%*s %*s %*s %s %s ",
                                             mountroot, lineLen+1,
                                             mountpath, lineLen+1);
                        if (sscanfRet != 2)
                            _ASSERTE(!"Failed to parse mount info file contents with sscanf_s.");

                        // assign the output arguments and clear the locals so we don't free them.
                        *pmountpath = mountpath;
                        *pmountroot = mountroot;
                        mountpath = mountroot = nullptr;
                        goto done;
                    }
                    strTok = strtok_s(nullptr, ",", &context);
                }
            }
        }
    done:
        PAL_free(mountpath);
        PAL_free(mountroot);
        PAL_free(filesystemType);
        PAL_free(options);
        free(line);
        if (mountinfofile)
            fclose(mountinfofile);
    }

    static char* FindCGroupPathForSubsystem(bool (*is_subsystem)(const char *))
    {
        char *line = nullptr;
        size_t lineLen = 0;
        size_t maxLineLen = 0;
        char *subsystem_list = nullptr;
        char *cgroup_path = nullptr;
        bool result = false;

        FILE *cgroupfile = fopen(PROC_CGROUP_FILENAME, "r");
        if (cgroupfile == nullptr)
            goto done;

        while (!result && getline(&line, &lineLen, cgroupfile) != -1)
        {
            if (subsystem_list == nullptr || lineLen > maxLineLen)
            {
                PAL_free(subsystem_list);
                PAL_free(cgroup_path);
                subsystem_list = (char*)PAL_malloc(lineLen+1);
                if (subsystem_list == nullptr)
                    goto done;
                cgroup_path = (char*)PAL_malloc(lineLen+1);
                if (cgroup_path == nullptr)
                    goto done;
                maxLineLen = lineLen;
            }

            // See man page of proc to get format for /proc/self/cgroup file
            int sscanfRet = sscanf_s(line,
                                     "%*[^:]:%[^:]:%s",
                                     subsystem_list, lineLen+1,
                                     cgroup_path, lineLen+1);
            if (sscanfRet != 2)
            {
                _ASSERTE(!"Failed to parse cgroup info file contents with sscanf_s.");
                goto done;
            }

            char* context = nullptr;
            char* strTok = strtok_s(subsystem_list, ",", &context);
            while (strTok != nullptr)
            {
                if (is_subsystem(strTok))
                {
                    result = true;
                    break;
                }
                strTok = strtok_s(nullptr, ",", &context);
            }
        }
    done:
        PAL_free(subsystem_list);
        if (!result)
        {
            PAL_free(cgroup_path);
            cgroup_path = nullptr;
        }
        free(line);
        if (cgroupfile)
            fclose(cgroupfile);
        return cgroup_path;
    }

    static bool ReadMemoryValueFromFile(const char* filename, uint64_t* val)
    {
        return ::ReadMemoryValueFromFile(filename, val);
    }

    static long long ReadCpuCGroupValue(const char* subsystemFilename){
        char *filename = nullptr;
        bool result = false;
        long long val;
        size_t len;

        if (s_cpu_cgroup_path == nullptr)
            return -1;

        len = strlen(s_cpu_cgroup_path);
        len += strlen(subsystemFilename);
        filename = (char*)PAL_malloc(len + 1);
        if (filename == nullptr)
            return -1;

        strcpy_s(filename, len+1, s_cpu_cgroup_path);
        strcat_s(filename, len+1, subsystemFilename);
        result = ReadLongLongValueFromFile(filename, &val);
        PAL_free(filename);
        if (!result)
            return -1;

        return val;
    }

    static bool ReadLongLongValueFromFile(const char* filename, long long* val)
    {
        bool result = false;
        char *line = nullptr;
        size_t lineLen = 0;

        if (val == nullptr)
            return false;;

        FILE* file = fopen(filename, "r");
        if (file == nullptr)
            goto done;

        if (getline(&line, &lineLen, file) == -1)
            goto done;

        errno = 0;
        *val = atoll(line);
        if (errno != 0)
            goto done;

        result = true;
    done:
        if (file)
            fclose(file);
        free(line);
        return result;
    }
};

char *CGroup::s_memory_cgroup_path = nullptr;
char *CGroup::s_cpu_cgroup_path = nullptr;

void InitializeCGroup()
{
    CGroup::Initialize();
}

void CleanupCGroup()
{
    CGroup::Cleanup();
}

size_t
PALAPI
PAL_GetRestrictedPhysicalMemoryLimit()
{
    uint64_t physical_memory_limit_64 = 0;
    size_t physical_memory_limit = 0;

    if (!CGroup::GetPhysicalMemoryLimit(&physical_memory_limit_64))
         return 0;

    // If there's no memory limit specified on the container this
    // actually returns 0x7FFFFFFFFFFFF000 (2^63-1 rounded down to
    // 4k which is a common page size). So we know we are not
    // running in a memory restricted environment.
    if (physical_memory_limit_64 > 0x7FFFFFFF00000000)
    {
        return 0;
    }

    if (physical_memory_limit_64 > std::numeric_limits<size_t>::max())
    {
        // It is observed in practice when the memory is unrestricted, Linux control
        // group returns a physical limit that is bigger than the address space
        physical_memory_limit = std::numeric_limits<size_t>::max();
    }
    else
    {
        physical_memory_limit = (size_t)physical_memory_limit_64;
    }

    struct rlimit curr_rlimit;
    size_t rlimit_soft_limit = (size_t)RLIM_INFINITY;
    if (getrlimit(RLIMIT_AS, &curr_rlimit) == 0)
    {
        rlimit_soft_limit = curr_rlimit.rlim_cur;
    }
    physical_memory_limit = std::min(physical_memory_limit, rlimit_soft_limit);

    // Ensure that limit is not greater than real memory size
    long pages = sysconf(_SC_PHYS_PAGES);
    if (pages != -1)
    {
        long pageSize = sysconf(_SC_PAGE_SIZE);
        if (pageSize != -1)
        {
            physical_memory_limit = std::min(physical_memory_limit,
                                            (size_t)(pages * pageSize));
        }
    }

    if(physical_memory_limit == SIZE_T_MAX)
        physical_memory_limit = 0;
    return physical_memory_limit;
}

BOOL
PALAPI
PAL_GetPhysicalMemoryUsed(size_t* val)
{
    BOOL result = false;
    size_t linelen;
    char* line = nullptr;

    if (val == nullptr)
        return FALSE;

    // Linux uses cgroup usage to trigger oom kills.
    if (CGroup::GetPhysicalMemoryUsage(val))
        return TRUE;

    // process resident set size.
    FILE* file = fopen(PROC_STATM_FILENAME, "r");
    if (file != nullptr && getline(&line, &linelen, file) != -1)
    {
        char* context = nullptr;
        char* strTok = strtok_s(line, " ", &context);
        strTok = strtok_s(nullptr, " ", &context);

        errno = 0;
        *val = strtoull(strTok, nullptr, 0);
        if(errno == 0)
        {
            *val = *val * GetVirtualPageSize();
            result = true;
        }
    }

    if (file)
        fclose(file);
    free(line);
    return result;
}

BOOL
PALAPI
PAL_GetCpuLimit(UINT* val)
{
    if (val == nullptr)
        return FALSE;

    return CGroup::GetCpuLimit(val);
}
