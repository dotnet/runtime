// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(__linux__) && !defined(_GNU_SOURCE)
#define _GNU_SOURCE
#endif

#include "test.h"

#if defined(__linux__)

#include <errno.h>
#include <sys/vfs.h>
#include <minipal/utils.h>

static FILE* fixture_fopen(const char* filename, const char* mode);
static int fixture_statfs(const char* filename, struct statfs* stats);

// Compile the real reader with test-local entry points and procfs substitutions, so the paths it resolves can be checked.
#define minipal_cgroup_initialize fixture_cgroup_initialize
#define minipal_cgroup_cleanup fixture_cgroup_cleanup
#define minipal_cgroup_get_cpu_limit fixture_cgroup_get_cpu_limit
#define minipal_cgroup_get_memory_limit fixture_cgroup_get_memory_limit
#define minipal_cgroup_get_memory_usage fixture_cgroup_get_memory_usage
#define fopen(filename, mode) fixture_fopen(filename, mode)
#define statfs(filename, stats) fixture_statfs(filename, stats)
#include <minipal/cgroup.c>
#undef statfs
#undef fopen
#undef minipal_cgroup_get_memory_usage
#undef minipal_cgroup_get_memory_limit
#undef minipal_cgroup_get_cpu_limit
#undef minipal_cgroup_cleanup
#undef minipal_cgroup_initialize

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

static FILE* fixture_fopen(const char* filename, const char* mode)
{
    (void)mode;
    char* text = NULL;
    int length = -1;
    if (strcmp(filename, PROC_MOUNTINFO_FILENAME) == 0)
    {
        const char* tail = s_version == 2 ? "cgroup2 cgroup rw" : "cgroup cgroup rw,cpu,memory";
        length = asprintf(&text, "32 29 0:28 %s %s rw - %s\n", s_current->root, s_current->encodedMount, tail);
    }
    else if (strcmp(filename, PROC_CGROUP_FILENAME) == 0)
    {
        length = asprintf(&text, "%s%s\n", s_version == 2 ? "0::" : "5:cpu,memory:", s_current->membership);
    }

    FILE* file = NULL;
    if (length >= 0)
    {
        file = open_text(text);
        free(text);
    }

    if (file == NULL)
        errno = ENOENT;
    return file;
}

static int fixture_statfs(const char* filename, struct statfs* stats)
{
    (void)filename;
    memset(stats, 0, sizeof(*stats));
    stats->f_type = s_version == 2 ? CGROUP2_SUPER_MAGIC : 0;
    return 0;
}

static bool is_expected_path(const char* path)
{
    size_t mountLength = strlen(s_current->mount);
    return path != NULL && strncmp(path, s_current->mount, mountLength) == 0 && strcmp(path + mountLength, s_current->relativePath) == 0;
}

static RESULT check_paths(const CgroupPathCase* cases, size_t count)
{
    for (size_t i = 0; i < count; i++)
    {
        for (s_version = 1; s_version <= 2; s_version++)
        {
            s_current = &cases[i];
            fixture_cgroup_initialize();
            RESULT result = OK;
            if (!is_expected_path(s_cpu_cgroup_path) || !is_expected_path(s_memory_cgroup_path) ||
                s_memory_cgroup_hierarchy_mount == NULL || strcmp(s_memory_cgroup_hierarchy_mount, s_current->mount) != 0)
            {
                result = FAILED("cgroup v%d membership '%s': CPU path '%s', memory path '%s', memory mount '%s'", s_version, s_current->membership,
                    s_cpu_cgroup_path ? s_cpu_cgroup_path : "(null)",
                    s_memory_cgroup_path ? s_memory_cgroup_path : "(null)",
                    s_memory_cgroup_hierarchy_mount ? s_memory_cgroup_hierarchy_mount : "(null)");
            }
            fixture_cgroup_cleanup();
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
        {"/", "/fixture/cgroup\\04", "/fixture/cgroup\\04", "/plain/child", "/plain/child"}
    };
    return check_paths(cases, ARRAY_SIZE(cases));
}

static Test minipal_cgroup_tests[] =
{
    {"test_cgroup_paths_unescaped", test_cgroup_paths_unescaped},
    {"test_cgroup_membership_whitespace", test_cgroup_membership_whitespace},
    {"test_cgroup_mount_escapes", test_cgroup_mount_escapes},
    {"test_cgroup_root_escapes", test_cgroup_root_escapes},
    {"test_cgroup_literal_sequences", test_cgroup_literal_sequences},
    {NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(minipal_cgroup_tests_init, minipal_cgroup_tests)

#endif // __linux__
