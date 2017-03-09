// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

Module Name:

    cgroup.cpp

Abstract:
    Read memory limits for the current process
--*/
#include <cstdint>
#include <cstddef>
#include <cassert>
#include <unistd.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <sys/resource.h>
#include <errno.h>

#define SIZE_T_MAX (~(size_t)0)
#define PROC_MOUNTINFO_FILENAME "/proc/self/mountinfo"
#define PROC_CGROUP_FILENAME "/proc/self/cgroup"
#define PROC_STATM_FILENAME "/proc/self/statm"
#define MEM_LIMIT_FILENAME "/memory.limit_in_bytes"

class CGroup
{
    char* m_memory_cgroup_path;
public:
    CGroup()
    {
        m_memory_cgroup_path = nullptr;
        char* memoryHierarchyMount = nullptr;
        char *cgroup_path_relative_to_mount = nullptr;
        size_t len;
        memoryHierarchyMount = FindMemoryHierarchyMount();
        if (memoryHierarchyMount == nullptr)
            goto done;

        cgroup_path_relative_to_mount = FindCGroupPathForMemorySubsystem();
        if (cgroup_path_relative_to_mount == nullptr)
            goto done;

        len = strlen(memoryHierarchyMount);
        len += strlen(cgroup_path_relative_to_mount);
        m_memory_cgroup_path = (char*)malloc(len+1);
        if (m_memory_cgroup_path == nullptr)
           goto done;
        
        strcpy(m_memory_cgroup_path, memoryHierarchyMount);
        strcat(m_memory_cgroup_path, cgroup_path_relative_to_mount);

    done:
        free(memoryHierarchyMount);
        free(cgroup_path_relative_to_mount);
    }

    ~CGroup()
    {
        free(m_memory_cgroup_path);
    }

    bool GetPhysicalMemoryLimit(size_t *val)
    {
        char *mem_limit_filename = nullptr;
        bool result = false;

        if (m_memory_cgroup_path == nullptr)
            return result;

        size_t len = strlen(m_memory_cgroup_path);
        len += strlen(MEM_LIMIT_FILENAME);
        mem_limit_filename = (char*)malloc(len+1);
        if (mem_limit_filename == nullptr)
            return result;

        strcpy(mem_limit_filename, m_memory_cgroup_path);
        strcat(mem_limit_filename, MEM_LIMIT_FILENAME);
        result = ReadMemoryValueFromFile(mem_limit_filename, val);
        free(mem_limit_filename);
        return result;
    }
    
private:
    char* FindMemoryHierarchyMount()
    {
        char *line = nullptr;
        size_t lineLen = 0, maxLineLen = 0;
        char *filesystemType = nullptr;
        char *options = nullptr;
        char* mountpath = nullptr;

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

            char* separatorChar = strchr(line, '-');

            // See man page of proc to get format for /proc/self/mountinfo file
            int sscanfRet = sscanf(separatorChar, 
                                   "- %s %*s %s",
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
                    if (strncmp("memory", strTok, 6) == 0)
                    {
                        mountpath = (char*)malloc(lineLen+1);
                        if (mountpath == nullptr)
                            goto done;
    
                        sscanfRet = sscanf(line,
                                           "%*s %*s %*s %*s %s ",
                                           mountpath);
                        if (sscanfRet != 1)
                        {
                            free(mountpath);
                            mountpath = nullptr;
                            assert(!"Failed to parse mount info file contents with sscanf.");
                        }
                        goto done;
                    }
                    strTok = strtok_r(nullptr, ",", &context);
                }
            }
        }
    done:
        free(filesystemType);
        free(options);
        free(line);
        if (mountinfofile)
            fclose(mountinfofile);
        return mountpath;
    }
    
    char* FindCGroupPathForMemorySubsystem()
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
                if (strncmp("memory", strTok, 6) == 0)
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
    
    bool ReadMemoryValueFromFile(const char* filename, size_t* val)
    {
        bool result = false;
        char *line = nullptr;
        size_t lineLen = 0;
        char* endptr = nullptr;
        size_t num = 0, l, multiplier;
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
};
   
size_t GetRestrictedPhysicalMemoryLimit()
{
    CGroup cgroup;
    size_t physical_memory_limit;
 
    if (!cgroup.GetPhysicalMemoryLimit(&physical_memory_limit))
         physical_memory_limit = SIZE_T_MAX;

    struct rlimit curr_rlimit;
    size_t rlimit_soft_limit = RLIM_INFINITY;
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

    return physical_memory_limit;
}

bool GetWorkingSetSize(size_t* val)
{
    bool result = false;
    size_t linelen;
    char* line = nullptr;

    if (val == nullptr)
        return false;

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
