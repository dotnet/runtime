// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif

#include "test.h"

#include <errno.h>
#include <linux/magic.h>
#include <stdint.h>
#include <sys/vfs.h>
#include <minipal/cgroup.h>
#include <minipal/utils.h>

typedef struct
{
    const char* root;
    const char* encodedMount;
    const char* mount;
    const char* membership;
    const char* relativePath;
} CgroupPathCase;

static const CgroupPathCase* s_current;
static int s_version;
static uint64_t s_childLimit;
static uint64_t s_parentLimit;
static size_t s_cpuReads;
static size_t s_limitReads;
static size_t s_usageReads;

FILE* __real_fopen(const char* filename, const char* mode);
FILE* __wrap_fopen(const char* filename, const char* mode);
int __real_statfs(const char* filename, struct statfs* stats);
int __wrap_statfs(const char* filename, struct statfs* stats);

#ifdef __GLIBC__
FILE* __real_fopen64(const char* filename, const char* mode);
FILE* __wrap_fopen64(const char* filename, const char* mode);
int __real_statfs64(const char* filename, struct statfs64* stats);
int __wrap_statfs64(const char* filename, struct statfs64* stats);
#endif

static FILE* open_text(const char* text)
{
    FILE* file = tmpfile();
    if (file != NULL)
    {
        if (fputs(text, file) == EOF)
        {
            fclose(file);
            return NULL;
        }
        rewind(file);
    }
    return file;
}

static void normalize_path(char* path)
{
    char* destination = path;
    for (const char* source = path; *source != '\0'; source++)
    {
        if (*source != '/' || destination == path || destination[-1] != '/')
            *destination++ = *source;
    }
    if (destination > path + 1 && destination[-1] == '/')
        destination--;
    *destination = '\0';
}

static bool is_resource(const char* path, const char* directory, const char* filename)
{
    size_t directoryLength = strlen(directory);
    return strncmp(path, directory, directoryLength) == 0 && strcmp(path + directoryLength, filename) == 0;
}

static FILE* open_fixture(const char* filename)
{
    char* text = NULL;
    if (strcmp(filename, "/proc/self/mountinfo") == 0)
    {
        const char* tail = s_version == 2 ? "cgroup2 cgroup rw" : "cgroup cgroup rw,cpu,memory";
        if (asprintf(&text, "32 29 0:28 %s %s rw - %s\n", s_current->root, s_current->encodedMount, tail) < 0)
            return NULL;
    }
    else if (strcmp(filename, "/proc/self/cgroup") == 0)
    {
        const char* prefix = s_version == 2 ? "0::" : "5:cpu,memory:";
        if (asprintf(&text, "%s%s\n", prefix, s_current->membership) < 0)
            return NULL;
    }

    if (text != NULL)
    {
        FILE* file = open_text(text);
        free(text);
        return file;
    }

    char* path = strdup(filename);
    char* child = NULL;
    if (path == NULL || asprintf(&child, "%s%s", s_current->mount, s_current->relativePath) < 0)
    {
        free(path);
        return NULL;
    }
    normalize_path(path);
    normalize_path(child);

    FILE* file = NULL;
    if (is_resource(path, child, "/cpu.max"))
    {
        s_cpuReads++;
        file = open_text("75000 100000\n");
    }
    else if (is_resource(path, child, "/cpu.cfs_quota_us"))
    {
        s_cpuReads++;
        file = open_text("75000\n");
    }
    else if (is_resource(path, child, "/cpu.cfs_period_us"))
    {
        file = open_text("100000\n");
    }
    else if (is_resource(path, child, "/memory.max") || is_resource(path, child, "/memory.limit_in_bytes"))
    {
        s_limitReads++;
        if (asprintf(&text, "%llu\n", (unsigned long long)s_childLimit) >= 0)
            file = open_text(text);
    }
    else if (is_resource(path, child, "/memory.use_hierarchy"))
    {
        file = open_text("0\n");
    }
    else if (is_resource(path, child, "/memory.current") || is_resource(path, child, "/memory.usage_in_bytes"))
    {
        s_usageReads++;
        file = open_text("67108864\n");
    }
    else if (is_resource(path, child, "/memory.stat"))
    {
        file = open_text(s_version == 2 ? "inactive_file 8388608\n" : "total_inactive_file 8388608\n");
    }
    else
    {
        size_t mountLength = strlen(s_current->mount);
        while (strlen(child) > mountLength)
        {
            *strrchr(child, '/') = '\0';
            if (is_resource(path, child, "/memory.max"))
            {
                if (asprintf(&text, "%llu\n", (unsigned long long)s_parentLimit) >= 0)
                    file = open_text(text);
                break;
            }
        }
    }

    free(text);
    free(child);
    free(path);
    if (file == NULL)
        errno = ENOENT;
    return file;
}

// The native driver is a separate process; fixtures never change the host's cgroups.
FILE* __wrap_fopen(const char* filename, const char* mode)
{
    return s_current == NULL ? __real_fopen(filename, mode) : open_fixture(filename);
}

int __wrap_statfs(const char* filename, struct statfs* stats)
{
    if (s_current == NULL)
        return __real_statfs(filename, stats);
    if (strcmp(filename, "/sys/fs/cgroup") != 0)
    {
        errno = ENOENT;
        return -1;
    }
    memset(stats, 0, sizeof(*stats));
    stats->f_type = s_version == 2 ? CGROUP2_SUPER_MAGIC : TMPFS_MAGIC;
    return 0;
}

#ifdef __GLIBC__
FILE* __wrap_fopen64(const char* filename, const char* mode)
{
    return s_current == NULL ? __real_fopen64(filename, mode) : open_fixture(filename);
}

int __wrap_statfs64(const char* filename, struct statfs64* stats)
{
    if (s_current == NULL)
        return __real_statfs64(filename, stats);
    if (strcmp(filename, "/sys/fs/cgroup") != 0)
    {
        errno = ENOENT;
        return -1;
    }
    memset(stats, 0, sizeof(*stats));
    stats->f_type = s_version == 2 ? CGROUP2_SUPER_MAGIC : TMPFS_MAGIC;
    return 0;
}
#endif // __GLIBC__

static RESULT check_path(const CgroupPathCase* path, int version, bool parentTighter)
{
    minipal_cgroup_cleanup();
    s_current = path;
    s_version = version;
    s_childLimit = parentTighter ? 1073741824 : 268435456;
    s_parentLimit = parentTighter ? 268435456 : 1073741824;
    s_cpuReads = s_limitReads = s_usageReads = 0;

    uint32_t cpu = 99;
    uint64_t limit = 99;
    uint64_t usage = 99;
    bool cpuFound = minipal_cgroup_get_cpu_limit(&cpu);
    bool limitFound = minipal_cgroup_get_memory_limit(&limit);
    bool usageFound = minipal_cgroup_get_memory_usage(&usage);
    uint64_t expectedLimit = version == 2 && s_parentLimit < s_childLimit ? s_parentLimit : s_childLimit;
    minipal_cgroup_cleanup();
    s_current = NULL;

    if (!cpuFound || cpu != 1 || s_cpuReads == 0)
        return FAILED("cgroup v%d membership '%s': CPU limit found=%d, value=%u, child reads=%zu", version, path->membership, cpuFound, cpu, s_cpuReads);
    if (!limitFound || limit != expectedLimit || s_limitReads == 0)
        return FAILED("cgroup v%d membership '%s': memory limit found=%d, value=%llu, child reads=%zu", version, path->membership, limitFound, (unsigned long long)limit, s_limitReads);
    if (!usageFound || usage != 58720256 || s_usageReads == 0)
        return FAILED("cgroup v%d membership '%s': memory usage found=%d, value=%llu, child reads=%zu", version, path->membership, usageFound, (unsigned long long)usage, s_usageReads);

    return OK;
}

static RESULT check_paths(const CgroupPathCase* cases, size_t count)
{
    for (size_t i = 0; i < count; i++)
    {
        for (int version = 1; version <= 2; version++)
        {
            RESULT result = check_path(&cases[i], version, false);
            if (result != OK)
                return result;
        }
    }
    return OK;
}

static RESULT test_cgroup_paths_unescaped(void)
{
    const CgroupPathCase cases[] =
    {
        {"/", "/fixture/cgroup", "/fixture/cgroup", "/plain/child", "/plain/child"},
        {"/parent", "/fixture/cgroup", "/fixture/cgroup", "/parent/child", "/child"},
        {"/parent", "/fixture/cgroup", "/fixture/cgroup", "/", "/"},
        {"/", "/fixture/cgroup", "/fixture/cgroup", "/system.slice/example\\x2dservice.scope/child", "/system.slice/example\\x2dservice.scope/child"},
        {"/", "/fixture/cgroup", "/fixture/cgroup", "/literal\\040group/child", "/literal\\040group/child"}
    };
    return check_paths(cases, ARRAY_SIZE(cases));
}

static RESULT test_cgroup_membership_whitespace(void)
{
    const CgroupPathCase cases[] =
    {
        {"/", "/fixture/cgroup", "/fixture/cgroup", "/worker group/child", "/worker group/child"},
        {"/", "/fixture/cgroup", "/fixture/cgroup", "/worker\tgroup/child", "/worker\tgroup/child"},
        {"/", "/fixture/cgroup", "/fixture/cgroup", "/worker ", "/worker "},
        {"/", "/fixture/cgroup", "/fixture/cgroup", "/worker\rgroup/child", "/worker\rgroup/child"}
    };
    return check_paths(cases, ARRAY_SIZE(cases));
}

static RESULT test_cgroup_mount_escapes(void)
{
    const CgroupPathCase cases[] =
    {
        {"/", "/fixture/cgroup\\040mount", "/fixture/cgroup mount", "/plain/child", "/plain/child"},
        {"/", "/fixture/cgroup\\011mount", "/fixture/cgroup\tmount", "/plain/child", "/plain/child"},
        {"/", "/fixture/cgroup\\012mount", "/fixture/cgroup\nmount", "/plain/child", "/plain/child"},
        {"/", "/fixture/cgroup\\134path", "/fixture/cgroup\\path", "/plain/child", "/plain/child"},
        {"/", "/fixture/cgroup\rmount", "/fixture/cgroup\rmount", "/plain/child", "/plain/child"}
    };
    return check_paths(cases, ARRAY_SIZE(cases));
}

static RESULT test_cgroup_root_escapes(void)
{
    const CgroupPathCase cases[] =
    {
        {"/parent\\040group", "/fixture/cgroup", "/fixture/cgroup", "/parent group/child", "/child"},
        {"/parent\\011group", "/fixture/cgroup", "/fixture/cgroup", "/parent\tgroup/child", "/child"},
        {"/parent\\134x2dscope", "/fixture/cgroup", "/fixture/cgroup", "/parent\\x2dscope/child", "/child"},
        {"/parent\\040group", "/fixture/cgroup\\040mount", "/fixture/cgroup mount", "/parent group/child", "/child"},
        {"/parent\\040group", "/fixture/cgroup", "/fixture/cgroup", "/", "/"}
    };
    return check_paths(cases, ARRAY_SIZE(cases));
}

static RESULT test_cgroup_literal_sequences(void)
{
    const CgroupPathCase cases[] =
    {
        {"/", "/fixture/cgroup\\134040mount", "/fixture/cgroup\\040mount", "/plain/child", "/plain/child"},
        {"/parent\\134040group", "/fixture/cgroup", "/fixture/cgroup", "/parent\\040group/child", "/child"},
        {"/", "/fixture/cgroup\\777mount", "/fixture/cgroup\\777mount", "/plain/child", "/plain/child"},
        {"/", "/fixture/cgroup\\123mount", "/fixture/cgroup\\123mount", "/plain/child", "/plain/child"},
        {"/", "/fixture/cgroup\\", "/fixture/cgroup\\", "/plain/child", "/plain/child"},
        {"/", "/fixture/cgroup\\0", "/fixture/cgroup\\0", "/plain/child", "/plain/child"},
        {"/", "/fixture/cgroup\\04", "/fixture/cgroup\\04", "/plain/child", "/plain/child"}
    };
    return check_paths(cases, ARRAY_SIZE(cases));
}

static RESULT test_cgroup_parent_limits(void)
{
    const CgroupPathCase cases[] =
    {
        {"/", "/fixture/cgroup", "/fixture/cgroup", "/plain/child", "/plain/child"},
        {"/", "/fixture/cgroup", "/fixture/cgroup", "/worker group/child", "/worker group/child"}
    };
    for (size_t i = 0; i < ARRAY_SIZE(cases); i++)
    {
        RESULT result = check_path(&cases[i], 2, true);
        if (result != OK)
            return result;
    }
    return OK;
}

static Test minipal_cgroup_tests[] =
{
    {"test_cgroup_paths_unescaped", test_cgroup_paths_unescaped},
    {"test_cgroup_membership_whitespace", test_cgroup_membership_whitespace},
    {"test_cgroup_mount_escapes", test_cgroup_mount_escapes},
    {"test_cgroup_root_escapes", test_cgroup_root_escapes},
    {"test_cgroup_literal_sequences", test_cgroup_literal_sequences},
    {"test_cgroup_parent_limits", test_cgroup_parent_limits},
    {NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(minipal_cgroup_tests_init, minipal_cgroup_tests)
