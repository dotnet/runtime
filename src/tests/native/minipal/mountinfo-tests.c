// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "test.h"

#include <minipal/mountinfo.h>
#include <minipal/utils.h>

typedef struct
{
    const char* root;
    const char* mountPoint;
    const char* expectedRoot;
    const char* expectedMountPoint;
} MountInfoCase;

static RESULT check_mountinfo_paths(const MountInfoCase* cases, size_t count)
{
    for (size_t i = 0; i < count; i++)
    {
        char line[1024];
        struct
        {
            char root[512];
            char rootCanary;
            char mountPoint[512];
            char mountPointCanary;
        } buffers;
        memset(&buffers, 'X', sizeof(buffers));
        snprintf(line, sizeof(line), "36 35 0:0 %s %s rw - cgroup2 cgroup rw", cases[i].root, cases[i].mountPoint);

        if (!minipal_get_mountinfo_paths(line, buffers.root, buffers.mountPoint, sizeof(buffers.root)))
            return FAILED("Mountinfo case %zu was not parsed", i);

        if (buffers.rootCanary != 'X' || buffers.mountPointCanary != 'X')
            return FAILED("Mountinfo case %zu overwrote a buffer", i);

        if (strcmp(buffers.root, cases[i].expectedRoot) != 0 || strcmp(buffers.mountPoint, cases[i].expectedMountPoint) != 0)
            return FAILED("Mountinfo case %zu returned root '%s', mount point '%s'", i, buffers.root, buffers.mountPoint);
    }

    return OK;
}

static RESULT test_mountinfo_paths_unescaped(void)
{
    const MountInfoCase cases[] =
    {
        {"/", "/sys/fs/cgroup", "/", "/sys/fs/cgroup"},
        {"/root", "/mnt", "/root", "/mnt"},
        {"/root\\", "/mnt\\0", "/root\\", "/mnt\\0"},
        {"/root\\04", "/mnt\\043", "/root\\04", "/mnt\\043"},
        {"/root\\000", "/mnt\\377", "/root\\000", "/mnt\\377"},
        {"/root\\054", "/mnt\\075", "/root\\054", "/mnt\\075"},
        {"/r\xc3\xa9sum\xc3\xa9", "/mnt\xf0\x9f\x98\x80", "/r\xc3\xa9sum\xc3\xa9", "/mnt\xf0\x9f\x98\x80"}
    };

    return check_mountinfo_paths(cases, ARRAY_SIZE(cases));
}

static RESULT test_mountinfo_paths_escapes(void)
{
    const MountInfoCase cases[] =
    {
        {"/root\\040path", "/mnt\\040path", "/root path", "/mnt path"},
        {"/root\\011path", "/mnt\\011path", "/root\tpath", "/mnt\tpath"},
        {"/root\\012path", "/mnt\\012path", "/root\npath", "/mnt\npath"},
        {"/root\\134path", "/mnt\\134path", "/root\\path", "/mnt\\path"},
        {"/root\\040\\011\\012\\134path", "/mnt\\134\\040", "/root \t\n\\path", "/mnt\\ "},
        {"/docker\\134x2d1234.scope", "/sys/fs/cgroup\\040v2", "/docker\\x2d1234.scope", "/sys/fs/cgroup v2"},
        {"/root\\041\\040path", "/mnt\\x\\011path", "/root\\041 path", "/mnt\\x\tpath"}
    };

    return check_mountinfo_paths(cases, ARRAY_SIZE(cases));
}

static RESULT test_mountinfo_paths_decode_once(void)
{
    const MountInfoCase cases[] =
    {
        {"/root\\134040", "/mnt\\134043", "/root\\040", "/mnt\\043"},
        {"/root\\134011\\134012\\134134", "/mnt\\134\\134", "/root\\011\\012\\134", "/mnt\\\\"},
        {"/root\\\\040", "/mnt\\134\\040", "/root\\ ", "/mnt\\ "}
    };

    return check_mountinfo_paths(cases, ARRAY_SIZE(cases));
}

static RESULT test_mountinfo_paths_carriage_returns(void)
{
    const MountInfoCase cases[] =
    {
        {"/root\rpath", "/mnt\rpath", "/root\rpath", "/mnt\rpath"},
        {"/root\r", "/mnt\r", "/root\r", "/mnt\r"},
        {"/root\r\\040path", "/mnt\\040\r", "/root\r path", "/mnt \r"}
    };

    return check_mountinfo_paths(cases, ARRAY_SIZE(cases));
}

static RESULT test_mountinfo_paths_invalid(void)
{
    const char* lines[] =
    {
        "",
        "36 35 0:0",
        "36 35 0:0 / /mnt",
        "36 35 0:0  /mnt rw - cgroup2 cgroup rw",
        "36 35 0:0 - /mnt rw - cgroup2 cgroup rw",
        "36 35 0:0 / - rw - cgroup2 cgroup rw"
    };
    char root[128];
    char mountPoint[128];

    for (size_t i = 0; i < ARRAY_SIZE(lines); i++)
    {
        if (minipal_get_mountinfo_paths(lines[i], root, mountPoint, sizeof(root)))
            return FAILED("Invalid mountinfo case %zu was parsed", i);
    }

    for (size_t capacity = 0; capacity < sizeof("/mount"); capacity++)
    {
        if (minipal_get_mountinfo_paths("36 35 0:0 /root /mount rw - cgroup2 cgroup rw", root, mountPoint, capacity))
            return FAILED("Mountinfo paths fit into capacity %zu", capacity);
    }

    return OK;
}

static RESULT test_mountinfo_paths_long(void)
{
    char line[10000];
    char root[5000];
    char mountPoint[5000];
    memset(root, 'r', sizeof(root));
    root[0] = '/';
    root[4096] = '\0';
    snprintf(line, sizeof(line), "36 35 0:0 %s\\040x /mnt\\134040 rw - cgroup2 cgroup rw", root);

    if (!minipal_get_mountinfo_paths(line, root, mountPoint, sizeof(root)))
        return FAILED("Long mountinfo paths were not parsed");

    for (size_t i = 1; i < 4096; i++)
    {
        if (root[i] != 'r')
            return FAILED("Long mountinfo root changed at %zu", i);
    }

    if (root[0] != '/' || strcmp(root + 4096, " x") != 0 || strcmp(mountPoint, "/mnt\\040") != 0)
        return FAILED("Long mountinfo paths were not decoded");

    return OK;
}

static Test minipal_mountinfo_tests[] =
{
    {"test_mountinfo_paths_unescaped", test_mountinfo_paths_unescaped},
    {"test_mountinfo_paths_escapes", test_mountinfo_paths_escapes},
    {"test_mountinfo_paths_decode_once", test_mountinfo_paths_decode_once},
    {"test_mountinfo_paths_carriage_returns", test_mountinfo_paths_carriage_returns},
    {"test_mountinfo_paths_invalid", test_mountinfo_paths_invalid},
    {"test_mountinfo_paths_long", test_mountinfo_paths_long},
    {NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(minipal_mountinfo_tests_init, minipal_mountinfo_tests)
