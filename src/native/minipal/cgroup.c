// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(__linux__) && !defined(_GNU_SOURCE)
// glibc declares asprintf only when _GNU_SOURCE is defined before <stdio.h>.
#define _GNU_SOURCE
#endif

#include "cgroup.h"

#if defined(__linux__)

#include "utils.h"
#include "mountinfo.h"

#include <assert.h>
#include <errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/vfs.h>

#define CGROUP2_SUPER_MAGIC 0x63677270

#define PROC_MOUNTINFO_FILENAME "/proc/self/mountinfo"
#define PROC_CGROUP_FILENAME "/proc/self/cgroup"
#define CGROUP1_MEMORY_LIMIT_FILENAME "/memory.limit_in_bytes"
#define CGROUP2_MEMORY_LIMIT_FILENAME "/memory.max"
#define CGROUP_MEMORY_STAT_FILENAME "/memory.stat"
#define CGROUP1_MEMORY_USAGE_FILENAME "/memory.usage_in_bytes"
#define CGROUP2_MEMORY_USAGE_FILENAME "/memory.current"
#define CGROUP1_MEMORY_USE_HIERARCHY_FILENAME "/memory.use_hierarchy"
#define CGROUP1_MEMORY_STAT_HIERARCHICAL_MEMORY_LIMIT_FIELD "hierarchical_memory_limit "
#define CGROUP1_MEMORY_STAT_INACTIVE_FIELD "total_inactive_file "
#define CGROUP2_MEMORY_STAT_INACTIVE_FIELD "inactive_file "
#define CGROUP1_CFS_QUOTA_FILENAME "/cpu.cfs_quota_us"
#define CGROUP1_CFS_PERIOD_FILENAME "/cpu.cfs_period_us"
#define CGROUP2_CPU_MAX_FILENAME "/cpu.max"

// The cgroup version, or 0 if cgroups are not found or not enabled
static int s_cgroup_version;

static char* s_memory_cgroup_path;
static char* s_memory_cgroup_hierarchy_mount;
static char* s_cpu_cgroup_path;

static bool s_initialized;

static bool read_memory_value_from_file(const char* filename, uint64_t* val)
{
    bool result = false;
    char* line = NULL;
    size_t lineLen = 0;
    char* endptr = NULL;
    uint64_t num = 0, multiplier;

    FILE* file = fopen(filename, "r");
    if (file == NULL)
        goto done;

    if (getline(&line, &lineLen, file) == -1)
        goto done;

    errno = 0;
    num = strtoull(line, &endptr, 0);
    if (line == endptr || errno != 0)
        goto done;

    multiplier = 1;
    switch (*endptr)
    {
    case 'g':
    case 'G': multiplier = 1024;
              FALLTHROUGH;
    case 'm':
    case 'M': multiplier = multiplier * 1024;
              FALLTHROUGH;
    case 'k':
    case 'K': multiplier = multiplier * 1024;
    }

    *val = num * multiplier;
    result = *val / multiplier == num;

done:
    if (file)
        fclose(file);
    free(line);
    return result;
}

static bool read_long_long_value_from_file(const char* filename, long long* val)
{
    bool result = false;
    char* line = NULL;
    size_t lineLen = 0;
    char* endptr = NULL;

    FILE* file = fopen(filename, "r");
    if (file == NULL)
        goto done;

    if (getline(&line, &lineLen, file) == -1)
        goto done;

    errno = 0;
    *val = strtoll(line, &endptr, 10);
    if (line == endptr || errno != 0)
        goto done;

    result = true;

done:
    if (file)
        fclose(file);
    free(line);
    return result;
}

static int find_cgroup_version(void)
{
    // It is possible to have both cgroup v1 and v2 enabled on a system.
    // Most non-bleeding-edge Linux distributions fall in this group. We
    // look at the file system type of /sys/fs/cgroup to determine which
    // one is the default. For more details, see:
    // https://systemd.io/CGROUP_DELEGATION/#three-different-tree-setups-
    // We dont care about the difference between the "legacy" and "hybrid"
    // modes because both of those involve cgroup v1 controllers managing
    // resources.

    struct statfs stats;
    if (statfs("/sys/fs/cgroup", &stats) != 0)
        return 0;

    // Assume that if /sys/fs/cgroup exists and the file system type is not cgroup2fs,
    // it is cgroup v1. Typically the file system type is tmpfs, but other values have
    // been seen in the wild.
    return stats.f_type == CGROUP2_SUPER_MAGIC ? 2 : 1;
}

static bool is_cgroup1_memory_subsystem(const char* strTok)
{
    return strcmp("memory", strTok) == 0;
}

static bool is_cgroup1_cpu_subsystem(const char* strTok)
{
    return strcmp("cpu", strTok) == 0;
}

static void find_hierarchy_mount(bool (*is_subsystem)(const char*), char** pmountpath, char** pmountroot)
{
    char* line = NULL;
    size_t lineLen = 0, maxLineLen = 0;
    char* filesystemType = NULL;
    char* options = NULL;
    char* mountpath = NULL;
    char* mountroot = NULL;

    FILE* mountinfofile = fopen(PROC_MOUNTINFO_FILENAME, "r");
    if (mountinfofile == NULL)
        goto done;

    while (getline(&line, &lineLen, mountinfofile) != -1)
    {
        if (filesystemType == NULL || lineLen > maxLineLen)
        {
            free(filesystemType);
            filesystemType = NULL;
            free(options);
            options = NULL;
            filesystemType = (char*)malloc(lineLen + 1);
            if (filesystemType == NULL)
                goto done;
            options = (char*)malloc(lineLen + 1);
            if (options == NULL)
                goto done;
            maxLineLen = lineLen;
        }

        char* separatorChar = strstr(line, " - ");

        // See man page of proc to get format for /proc/self/mountinfo file
        int sscanfRet = sscanf(separatorChar, " - %s %*s %s", filesystemType, options);
        if (sscanfRet != 2)
        {
            assert(!"Failed to parse mount info file contents with sscanf.");
            goto done;
        }

        if (strncmp(filesystemType, "cgroup", 6) == 0)
        {
            bool isSubsystemMatch = is_subsystem == NULL;
            if (!isSubsystemMatch)
            {
                char* context = NULL;
                char* strTok = strtok_r(options, ",", &context);
                while (!isSubsystemMatch && strTok != NULL)
                {
                    isSubsystemMatch = is_subsystem(strTok);
                    strTok = strtok_r(NULL, ",", &context);
                }
            }
            if (isSubsystemMatch)
            {
                mountpath = (char*)malloc(lineLen + 1);
                if (mountpath == NULL)
                    goto done;
                mountroot = (char*)malloc(lineLen + 1);
                if (mountroot == NULL)
                    goto done;

                if (!minipal_get_mountinfo_paths(line, mountroot, mountpath, lineLen + 1))
                {
                    assert(!"Failed to parse mount info file paths.");
                    goto done;
                }

                // assign the output arguments and clear the locals so we don't free them.
                *pmountpath = mountpath;
                *pmountroot = mountroot;
                mountpath = mountroot = NULL;
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

static char* find_cgroup_path_for_subsystem(bool (*is_subsystem)(const char*))
{
    char* line = NULL;
    size_t lineLen = 0;
    size_t maxLineLen = 0;
    char* subsystem_list = NULL;
    char* cgroup_path = NULL;
    bool result = false;

    FILE* cgroupfile = fopen(PROC_CGROUP_FILENAME, "r");
    if (cgroupfile == NULL)
        goto done;

    while (!result && getline(&line, &lineLen, cgroupfile) != -1)
    {
        if (subsystem_list == NULL || lineLen > maxLineLen)
        {
            free(subsystem_list);
            subsystem_list = NULL;
            free(cgroup_path);
            cgroup_path = NULL;
            subsystem_list = (char*)malloc(lineLen + 1);
            if (subsystem_list == NULL)
                goto done;
            cgroup_path = (char*)malloc(lineLen + 1);
            if (cgroup_path == NULL)
                goto done;
            maxLineLen = lineLen;
        }

        if (s_cgroup_version == 1)
        {
            // See man page of proc to get format for /proc/self/cgroup file
            int sscanfRet = sscanf(line, "%*[^:]:%[^:]:%[^\n]", subsystem_list, cgroup_path);
            if (sscanfRet != 2)
            {
                assert(!"Failed to parse cgroup info file contents with sscanf.");
                goto done;
            }

            char* context = NULL;
            char* strTok = strtok_r(subsystem_list, ",", &context);
            while (strTok != NULL)
            {
                if (is_subsystem(strTok))
                {
                    result = true;
                    break;
                }
                strTok = strtok_r(NULL, ",", &context);
            }
        }
        else if (s_cgroup_version == 2)
        {
            // See https://www.kernel.org/doc/Documentation/cgroup-v2.txt
            // Look for a "0::/some/path"
            if (sscanf(line, "0::%[^\n]", cgroup_path) == 1)
            {
                result = true;
            }
        }
        else
        {
            assert(!"Unknown cgroup version in mountinfo.");
            goto done;
        }
    }

done:
    free(subsystem_list);
    if (!result)
    {
        free(cgroup_path);
        cgroup_path = NULL;
    }
    free(line);
    if (cgroupfile)
        fclose(cgroupfile);
    return cgroup_path;
}

static void find_cgroup_path(bool (*is_subsystem)(const char*), char** pcgroup_path, char** pcgroup_hierarchy_mount)
{
    char* cgroup_path = NULL;
    char* hierarchy_mount = NULL;
    char* hierarchy_root = NULL;
    char* cgroup_path_relative_to_mount = NULL;
    size_t common_path_prefix_len;

    find_hierarchy_mount(is_subsystem, &hierarchy_mount, &hierarchy_root);
    if (hierarchy_mount == NULL || hierarchy_root == NULL)
        goto done;

    cgroup_path_relative_to_mount = find_cgroup_path_for_subsystem(is_subsystem);
    if (cgroup_path_relative_to_mount == NULL)
        goto done;

    cgroup_path = (char*)malloc(strlen(hierarchy_mount) + strlen(cgroup_path_relative_to_mount) + 1);
    if (cgroup_path == NULL)
        goto done;

    strcpy(cgroup_path, hierarchy_mount);
    // For a host cgroup, we need to append the relative path.
    // The root and cgroup path can share a common prefix of the path that should not be appended.
    // Example 1 (docker):
    // hierarchy_mount:               /sys/fs/cgroup/cpu
    // hierarchy_root:                /docker/87ee2de57e51bc75175a4d2e81b71d162811b179d549d6601ed70b58cad83578
    // cgroup_path_relative_to_mount: /docker/87ee2de57e51bc75175a4d2e81b71d162811b179d549d6601ed70b58cad83578/my_named_cgroup
    // append do the cgroup_path:     /my_named_cgroup
    // final cgroup_path:             /sys/fs/cgroup/cpu/my_named_cgroup
    //
    // Example 2 (out of docker)
    // hierarchy_mount:               /sys/fs/cgroup/cpu
    // hierarchy_root:                /
    // cgroup_path_relative_to_mount: /my_named_cgroup
    // append do the cgroup_path:     /my_named_cgroup
    // final cgroup_path:             /sys/fs/cgroup/cpu/my_named_cgroup
    common_path_prefix_len = strlen(hierarchy_root);
    if ((common_path_prefix_len == 1) || strncmp(hierarchy_root, cgroup_path_relative_to_mount, common_path_prefix_len) != 0)
    {
        common_path_prefix_len = 0;
    }

    assert((cgroup_path_relative_to_mount[common_path_prefix_len] == '/') || (cgroup_path_relative_to_mount[common_path_prefix_len] == '\0'));

    strcat(cgroup_path, cgroup_path_relative_to_mount + common_path_prefix_len);

done:
    free(hierarchy_root);
    free(cgroup_path_relative_to_mount);
    *pcgroup_path = cgroup_path;
    if (pcgroup_hierarchy_mount != NULL)
    {
        *pcgroup_hierarchy_mount = hierarchy_mount;
    }
    else
    {
        free(hierarchy_mount);
    }
}

static bool get_memory_stat_field(const char* fieldName, uint64_t* val)
{
    if (s_memory_cgroup_path == NULL)
        return false;

    char* stat_filename = NULL;
    if (asprintf(&stat_filename, "%s%s", s_memory_cgroup_path, CGROUP_MEMORY_STAT_FILENAME) < 0)
        return false;

    FILE* stat_file = fopen(stat_filename, "r");
    free(stat_filename);
    if (stat_file == NULL)
        return false;

    char* line = NULL;
    size_t lineLen = 0;
    bool foundFieldValue = false;
    char* endptr;

    size_t fieldNameLength = strlen(fieldName);

    while (getline(&line, &lineLen, stat_file) != -1)
    {
        if (strncmp(line, fieldName, fieldNameLength) == 0)
        {
            errno = 0;
            const char* startptr = line + fieldNameLength;
            uint64_t fieldValue = strtoull(startptr, &endptr, 10);
            if (endptr != startptr && errno == 0)
            {
                foundFieldValue = true;
                *val = fieldValue;
            }

            break;
        }
    }

    fclose(stat_file);
    free(line);

    return foundFieldValue;
}

static bool get_cgroup1_memory_limit(uint64_t* val)
{
    if (s_memory_cgroup_path == NULL)
        return false;

    char* mem_use_hierarchy_filename = NULL;
    if (asprintf(&mem_use_hierarchy_filename, "%s%s", s_memory_cgroup_path, CGROUP1_MEMORY_USE_HIERARCHY_FILENAME) < 0)
        return false;

    uint64_t useHierarchy = 0;
    read_memory_value_from_file(mem_use_hierarchy_filename, &useHierarchy);
    free(mem_use_hierarchy_filename);

    if (useHierarchy)
    {
        return get_memory_stat_field(CGROUP1_MEMORY_STAT_HIERARCHICAL_MEMORY_LIMIT_FIELD, val);
    }

    char* mem_limit_filename = NULL;
    if (asprintf(&mem_limit_filename, "%s%s", s_memory_cgroup_path, CGROUP1_MEMORY_LIMIT_FILENAME) < 0)
        return false;

    bool result = read_memory_value_from_file(mem_limit_filename, val);
    free(mem_limit_filename);
    return result;
}

static bool get_cgroup2_memory_limit(uint64_t* val)
{
    if (s_memory_cgroup_path == NULL)
        return false;

    // Process the whole CGroup hierarchy to find a level with the most limiting limit
    size_t memory_cgroup_hierarchy_mount_length = strlen(s_memory_cgroup_hierarchy_mount);
    uint64_t min_limit = UINT64_MAX;
    uint64_t limit;
    bool found_any_limit = false;

    char* mem_limit_filename = NULL;
    if (asprintf(&mem_limit_filename, "%s%s", s_memory_cgroup_path, CGROUP2_MEMORY_LIMIT_FILENAME) < 0)
        return false;

    size_t cgroupPathLength = strlen(s_memory_cgroup_path);

    // Iterate over the directory hierarchy representing the cgroup hierarchy until reaching the
    // mount directory. The mount directory can also contain the memory.max in case it
    // is not the global root cgroup.
    while (true)
    {
        if (read_memory_value_from_file(mem_limit_filename, &limit))
        {
            found_any_limit = true;
            if (limit < min_limit)
            {
                min_limit = limit;
            }
        }

        if (cgroupPathLength == memory_cgroup_hierarchy_mount_length)
        {
            break;
        }

        // Get the parent cgroup memory limit file path
        char* parent_directory_end = mem_limit_filename + cgroupPathLength - 1;
        while (*parent_directory_end != '/')
        {
            parent_directory_end--;
        }

        cgroupPathLength = parent_directory_end - mem_limit_filename;

        strcpy(parent_directory_end, CGROUP2_MEMORY_LIMIT_FILENAME);
    }

    free(mem_limit_filename);

    if (found_any_limit)
    {
        *val = min_limit;
    }
    return found_any_limit;
}

static bool get_memory_usage(uint64_t* val, const char* filename, const char* inactiveFileFieldName)
{
    if (s_memory_cgroup_path == NULL)
        return false;

    // Use the same way to calculate memory load as popular container tools (Docker, Kubernetes, Containerd etc.)
    // For cgroup v1: value of 'memory.usage_in_bytes' minus 'total_inactive_file' value of 'memory.stat'
    // For cgroup v2: value of 'memory.current' minus 'inactive_file' value of 'memory.stat'

    char* mem_usage_filename = NULL;
    if (asprintf(&mem_usage_filename, "%s%s", s_memory_cgroup_path, filename) < 0)
        return false;

    uint64_t usage = 0;
    bool result = read_memory_value_from_file(mem_usage_filename, &usage);
    free(mem_usage_filename);

    if (!result)
        return false;

    uint64_t inactiveFileValue = 0;
    if (!get_memory_stat_field(inactiveFileFieldName, &inactiveFileValue))
        return false;

    *val = usage - inactiveFileValue;
    return true;
}

static void compute_cpu_limit(long long period, long long quota, uint32_t* val)
{
    // Cannot have less than 1 CPU
    if (quota <= period)
    {
        *val = 1;
        return;
    }

    // Calculate cpu count based on quota and round it up
    double cpu_count = (double)quota / period + 0.999999999;
    *val = (cpu_count < UINT32_MAX) ? (uint32_t)cpu_count : UINT32_MAX;
}

static long long read_cpu_cgroup_value(const char* subsystemFilename)
{
    char* filename = NULL;
    long long val;

    if (s_cpu_cgroup_path == NULL)
        return -1;

    if (asprintf(&filename, "%s%s", s_cpu_cgroup_path, subsystemFilename) < 0)
        return -1;

    bool result = read_long_long_value_from_file(filename, &val);
    free(filename);
    if (!result)
        return -1;

    return val;
}

static bool get_cgroup1_cpu_limit(uint32_t* val)
{
    long long quota = read_cpu_cgroup_value(CGROUP1_CFS_QUOTA_FILENAME);
    if (quota <= 0)
        return false;

    long long period = read_cpu_cgroup_value(CGROUP1_CFS_PERIOD_FILENAME);
    if (period <= 0)
        return false;

    compute_cpu_limit(period, quota, val);
    return true;
}

static bool get_cgroup2_cpu_limit(uint32_t* val)
{
    char* filename = NULL;
    FILE* file = NULL;
    char* endptr = NULL;
    char* max_quota_string = NULL;
    char* period_string = NULL;
    char* context = NULL;
    char* line = NULL;
    size_t lineLen = 0;

    long long quota = 0;
    long long period = 0;

    bool result = false;

    if (s_cpu_cgroup_path == NULL)
        return false;

    if (asprintf(&filename, "%s%s", s_cpu_cgroup_path, CGROUP2_CPU_MAX_FILENAME) < 0)
        return false;

    file = fopen(filename, "r");
    if (file == NULL)
        goto done;

    if (getline(&line, &lineLen, file) == -1)
        goto done;

    // The expected format is:
    //     $MAX $PERIOD
    // Where "$MAX" may be the string literal "max"

    max_quota_string = strtok_r(line, " ", &context);
    if (max_quota_string == NULL)
    {
        assert(!"Unable to parse " CGROUP2_CPU_MAX_FILENAME " file contents.");
        goto done;
    }
    period_string = strtok_r(NULL, " ", &context);
    if (period_string == NULL)
    {
        assert(!"Unable to parse " CGROUP2_CPU_MAX_FILENAME " file contents.");
        goto done;
    }

    // "max" means no cpu limit
    if (strcmp("max", max_quota_string) == 0)
        goto done;

    errno = 0;
    quota = strtoll(max_quota_string, &endptr, 10);
    if (max_quota_string == endptr || errno != 0)
        goto done;

    period = strtoll(period_string, &endptr, 10);
    if (period_string == endptr || errno != 0)
        goto done;

    compute_cpu_limit(period, quota, val);
    result = true;

done:
    if (file)
        fclose(file);
    free(filename);
    free(line);

    return result;
}

void minipal_cgroup_initialize(void)
{
    if (s_initialized)
        return;

    s_initialized = true;
    s_cgroup_version = find_cgroup_version();
    if (s_cgroup_version != 0)
    {
        find_cgroup_path(s_cgroup_version == 1 ? &is_cgroup1_memory_subsystem : NULL, &s_memory_cgroup_path, &s_memory_cgroup_hierarchy_mount);
        find_cgroup_path(s_cgroup_version == 1 ? &is_cgroup1_cpu_subsystem : NULL, &s_cpu_cgroup_path, NULL);
    }
}

void minipal_cgroup_cleanup(void)
{
    free(s_memory_cgroup_path);
    s_memory_cgroup_path = NULL;
    free(s_memory_cgroup_hierarchy_mount);
    s_memory_cgroup_hierarchy_mount = NULL;
    free(s_cpu_cgroup_path);
    s_cpu_cgroup_path = NULL;
    s_initialized = false;
}

bool minipal_cgroup_get_cpu_limit(uint32_t* val)
{
    minipal_cgroup_initialize();

    if (s_cgroup_version == 1)
        return get_cgroup1_cpu_limit(val);
    if (s_cgroup_version == 2)
        return get_cgroup2_cpu_limit(val);
    return false;
}

bool minipal_cgroup_get_memory_limit(uint64_t* val)
{
    minipal_cgroup_initialize();

    if (s_cgroup_version == 1)
        return get_cgroup1_memory_limit(val);
    if (s_cgroup_version == 2)
        return get_cgroup2_memory_limit(val);
    return false;
}

bool minipal_cgroup_get_memory_usage(uint64_t* val)
{
    minipal_cgroup_initialize();

    if (s_cgroup_version == 1)
        return get_memory_usage(val, CGROUP1_MEMORY_USAGE_FILENAME, CGROUP1_MEMORY_STAT_INACTIVE_FIELD);
    if (s_cgroup_version == 2)
        return get_memory_usage(val, CGROUP2_MEMORY_USAGE_FILENAME, CGROUP2_MEMORY_STAT_INACTIVE_FIELD);
    return false;
}

#else // __linux__

void minipal_cgroup_initialize(void)
{
}

void minipal_cgroup_cleanup(void)
{
}

bool minipal_cgroup_get_cpu_limit(uint32_t* val)
{
    (void)val;
    return false;
}

bool minipal_cgroup_get_memory_limit(uint64_t* val)
{
    (void)val;
    return false;
}

bool minipal_cgroup_get_memory_usage(uint64_t* val)
{
    (void)val;
    return false;
}

#endif // __linux__
