// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

Module Name:

    cgroup.cpp

Abstract:
    Read memory and cpu limits for the current process
--*/
#ifdef __FreeBSD__
#define _WITH_GETLINE
#endif

#include <cstdint>
#include <cstddef>
#include <cassert>
#include <unistd.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <sys/resource.h>
#include <errno.h>
#include <limits>

#include "cgroup.h"

#ifndef SIZE_T_MAX
#define SIZE_T_MAX (~(size_t)0)
#endif

#define PROC_MOUNTINFO_FILENAME "/proc/self/mountinfo"
#define PROC_CGROUP_FILENAME "/proc/self/cgroup"
#define PROC_STATM_FILENAME "/proc/self/statm"
#define MEM_LIMIT_FILENAME "/memory.limit_in_bytes"
#define MEM_USAGE_FILENAME "/memory.usage_in_bytes"
#define CFS_QUOTA_FILENAME "/cpu.cfs_quota_us"
#define CFS_PERIOD_FILENAME "/cpu.cfs_period_us"

class CGroup
{
    static char* s_memory_cgroup_path;
    static char* s_cpu_cgroup_path;
public:
    static void Initialize()
    {
        s_memory_cgroup_path = FindCgroupPath(&IsMemorySubsystem);
        s_cpu_cgroup_path = FindCgroupPath(&IsCpuSubsystem);
    }

    static void Cleanup()
    {
        free(s_memory_cgroup_path);
        free(s_cpu_cgroup_path);
    }

    static bool GetPhysicalMemoryLimit(uint64_t *val)
    {
        char *mem_limit_filename = nullptr;
        bool result = false;

        if (s_memory_cgroup_path == nullptr)
            return result;

        size_t len = strlen(s_memory_cgroup_path);
        len += strlen(MEM_LIMIT_FILENAME);
        mem_limit_filename = (char*)malloc(len+1);
        if (mem_limit_filename == nullptr)
            return result;

        strcpy(mem_limit_filename, s_memory_cgroup_path);
        strcat(mem_limit_filename, MEM_LIMIT_FILENAME);
        result = ReadMemoryValueFromFile(mem_limit_filename, val);
        free(mem_limit_filename);
        return result;
    }

    static bool GetPhysicalMemoryUsage(size_t *val)
    {
        char *mem_usage_filename = nullptr;
        bool result = false;
        uint64_t temp = 0;

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

    static bool GetCpuLimit(uint32_t *val)
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
        *val = (cpu_count < UINT32_MAX) ? (uint32_t)cpu_count : UINT32_MAX;

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

        FindHierarchyMount(is_subsystem, &hierarchy_mount, &hierarchy_root);
        if (hierarchy_mount == nullptr || hierarchy_root == nullptr)
            goto done;

        cgroup_path_relative_to_mount = FindCGroupPathForSubsystem(is_subsystem);
        if (cgroup_path_relative_to_mount == nullptr)
            goto done;

        cgroup_path = (char*)malloc(strlen(hierarchy_mount) + strlen(cgroup_path_relative_to_mount) + 1);
        if (cgroup_path == nullptr)
           goto done;

        strcpy(cgroup_path, hierarchy_mount);
        // For a host cgroup, we need to append the relative path.
        // In a docker container, the root and relative path are the same and we don't need to append.
        if (strcmp(hierarchy_root, cgroup_path_relative_to_mount) != 0)
            strcat(cgroup_path, cgroup_path_relative_to_mount);

    done:
        free(hierarchy_mount);
        free(hierarchy_root);
        free(cgroup_path_relative_to_mount);
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
                free(filesystemType);
                free(options);
                filesystemType = (char*)malloc(lineLen+1);
                if (filesystemType == nullptr)
                    goto done;
                options = (char*)malloc(lineLen+1);
                if (options == nullptr)
                    goto done;
                maxLineLen = lineLen;
            }

            char* separatorChar = strstr(line, " - ");

            // See man page of proc to get format for /proc/self/mountinfo file
            int sscanfRet = sscanf(separatorChar,
                                   " - %s %*s %s",
                                   filesystemType,
                                   options);
            if (sscanfRet != 2)
            {
                assert(!"Failed to parse mount info file contents with sscanf.");
                goto done;
            }

            if (strncmp(filesystemType, "cgroup", 6) == 0)
            {
                char* context = nullptr;
                char* strTok = strtok_r(options, ",", &context);
                while (strTok != nullptr)
                {
                    if (is_subsystem(strTok))
                    {
                        mountpath = (char*)malloc(lineLen+1);
                        if (mountpath == nullptr)
                            goto done;
                        mountroot = (char*)malloc(lineLen+1);
                        if (mountroot == nullptr)
                            goto done;

                        sscanfRet = sscanf(line,
                                           "%*s %*s %*s %s %s ",
                                           mountroot,
                                           mountpath);
                        if (sscanfRet != 2)
                            assert(!"Failed to parse mount info file contents with sscanf.");

                        // assign the output arguments and clear the locals so we don't free them.
                        *pmountpath = mountpath;
                        *pmountroot = mountroot;
                        mountpath = mountroot = nullptr;
                        goto done;
                    }
                    strTok = strtok_r(nullptr, ",", &context);
                }
            }
        }
    done:
        free(mountpath);
        free(mountroot);
        free(filesystemType);
        free(options);
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
                free(subsystem_list);
                free(cgroup_path);
                subsystem_list = (char*)malloc(lineLen+1);
                if (subsystem_list == nullptr)
                    goto done;
                cgroup_path = (char*)malloc(lineLen+1);
                if (cgroup_path == nullptr)
                    goto done;
                maxLineLen = lineLen;
            }

            // See man page of proc to get format for /proc/self/cgroup file
            int sscanfRet = sscanf(line,
                                   "%*[^:]:%[^:]:%s",
                                   subsystem_list,
                                   cgroup_path);
            if (sscanfRet != 2)
            {
                assert(!"Failed to parse cgroup info file contents with sscanf.");
                goto done;
            }

            char* context = nullptr;
            char* strTok = strtok_r(subsystem_list, ",", &context);
            while (strTok != nullptr)
            {
                if (is_subsystem(strTok))
                {
                    result = true;
                    break;
                }
                strTok = strtok_r(nullptr, ",", &context);
            }
        }
    done:
        free(subsystem_list);
        if (!result)
        {
            free(cgroup_path);
            cgroup_path = nullptr;
        }
        free(line);
        if (cgroupfile)
            fclose(cgroupfile);
        return cgroup_path;
    }

    static bool ReadMemoryValueFromFile(const char* filename, uint64_t* val)
    {
        bool result = false;
        char *line = nullptr;
        size_t lineLen = 0;
        char* endptr = nullptr;
        uint64_t num = 0, l, multiplier;
        FILE* file = nullptr;

        if (val == nullptr)
            goto done;

        file = fopen(filename, "r");
        if (file == nullptr)
            goto done;

        if (getline(&line, &lineLen, file) == -1)
            goto done;

        errno = 0;
        num = strtoull(line, &endptr, 0);
        if (errno != 0)
            goto done;

        multiplier = 1;
        switch(*endptr)
        {
            case 'g':
            case 'G': multiplier = 1024;
            case 'm':
            case 'M': multiplier = multiplier*1024;
            case 'k':
            case 'K': multiplier = multiplier*1024;
        }

        *val = num * multiplier;
        result = true;
        if (*val/multiplier != num)
            result = false;
    done:
        if (file)
            fclose(file);
        free(line);
        return result;
    }

    static long long ReadCpuCGroupValue(const char* subsystemFilename){
        char *filename = nullptr;
        bool result = false;
        long long val;

        if (s_cpu_cgroup_path == nullptr)
            return -1;

        filename = (char*)malloc(strlen(s_cpu_cgroup_path) + strlen(subsystemFilename) + 1);
        if (filename == nullptr)
            return -1;

        strcpy(filename, s_cpu_cgroup_path);
        strcat(filename, subsystemFilename);
        result = ReadLongLongValueFromFile(filename, &val);
        free(filename);
        if (!result)
             return -1;

        return val;
    }

    static bool ReadLongLongValueFromFile(const char* filename, long long* val)
    {
        bool result = false;
        char *line = nullptr;
        size_t lineLen = 0;

        FILE* file = nullptr;

        if (val == nullptr)
            goto done;

        file = fopen(filename, "r");
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

size_t GetRestrictedPhysicalMemoryLimit()
{
    uint64_t physical_memory_limit = 0;

    if (!CGroup::GetPhysicalMemoryLimit(&physical_memory_limit))
         return 0;

    // If there's no memory limit specified on the container this
    // actually returns 0x7FFFFFFFFFFFF000 (2^63-1 rounded down to
    // 4k which is a common page size). So we know we are not
    // running in a memory restricted environment.
    if (physical_memory_limit > 0x7FFFFFFF00000000)
    {
        return 0;
    }

    struct rlimit curr_rlimit;
    size_t rlimit_soft_limit = (size_t)RLIM_INFINITY;
    if (getrlimit(RLIMIT_AS, &curr_rlimit) == 0)
    {
        rlimit_soft_limit = curr_rlimit.rlim_cur;
    }
    physical_memory_limit = (physical_memory_limit < rlimit_soft_limit) ?
                            physical_memory_limit : rlimit_soft_limit;

    // Ensure that limit is not greater than real memory size
    long pages = sysconf(_SC_PHYS_PAGES);
    if (pages != -1)
    {
        long pageSize = sysconf(_SC_PAGE_SIZE);
        if (pageSize != -1)
        {
            physical_memory_limit = (physical_memory_limit < (size_t)pages * pageSize)?
                                    physical_memory_limit : (size_t)pages * pageSize;
        }
    }

    if (physical_memory_limit > std::numeric_limits<size_t>::max())
    {
        // It is observed in practice when the memory is unrestricted, Linux control
        // group returns a physical limit that is bigger than the address space
        return std::numeric_limits<size_t>::max();
    }
    else
    {
        return (size_t)physical_memory_limit;
    }
}

bool GetPhysicalMemoryUsed(size_t* val)
{
    bool result = false;
    size_t linelen;
    char* line = nullptr;

    if (val == nullptr)
        return false;

    // Linux uses cgroup usage to trigger oom kills.
    if (CGroup::GetPhysicalMemoryUsage(val))
        return true;

    // process resident set size.
    FILE* file = fopen(PROC_STATM_FILENAME, "r");
    if (file != nullptr && getline(&line, &linelen, file) != -1)
    {
        char* context = nullptr;
        char* strTok = strtok_r(line, " ", &context);
        strTok = strtok_r(nullptr, " ", &context);

        errno = 0;
        *val = strtoull(strTok, nullptr, 0);
        if (errno == 0)
        {
            long pageSize = sysconf(_SC_PAGE_SIZE);
            if (pageSize != -1)
            {
                *val = *val * pageSize;
                result = true;
            }
        }
    }

    if (file)
        fclose(file);
    free(line);
    return result;
}

bool GetCpuLimit(uint32_t* val)
{
    if (val == nullptr)
        return false;

    return CGroup::GetCpuLimit(val);
}
