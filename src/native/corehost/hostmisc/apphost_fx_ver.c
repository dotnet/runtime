// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "apphost_fx_ver.h"

#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <assert.h>
#include <ctype.h>

void fx_ver_init(fx_ver_t* ver)
{
    ver->major = -1;
    ver->minor = -1;
    ver->patch = -1;
    ver->pre[0] = '\0';
    ver->build[0] = '\0';
}

void fx_ver_set(fx_ver_t* ver, int major, int minor, int patch)
{
    ver->major = major;
    ver->minor = minor;
    ver->patch = patch;
    ver->pre[0] = '\0';
    ver->build[0] = '\0';
}

bool fx_ver_is_empty(const fx_ver_t* ver)
{
    return ver->major == -1;
}

char* fx_ver_as_str(const fx_ver_t* ver, char* out_str, size_t out_str_len)
{
    int written;
    if (ver->pre[0] != '\0' && ver->build[0] != '\0')
    {
        written = snprintf(out_str, out_str_len, "%d.%d.%d%s%s",
            ver->major, ver->minor, ver->patch, ver->pre, ver->build);
    }
    else if (ver->pre[0] != '\0')
    {
        written = snprintf(out_str, out_str_len, "%d.%d.%d%s",
            ver->major, ver->minor, ver->patch, ver->pre);
    }
    else
    {
        written = snprintf(out_str, out_str_len, "%d.%d.%d",
            ver->major, ver->minor, ver->patch);
    }

    (void)written;
    return out_str;
}

static size_t index_of_non_numeric(const char* str, size_t start)
{
    for (size_t i = start; str[i] != '\0'; i++)
    {
        if (str[i] < '0' || str[i] > '9')
            return i;
    }
    return (size_t)-1; // npos equivalent
}

static bool try_stou(const char* str, size_t len, unsigned* num)
{
    if (len == 0)
        return false;

    for (size_t i = 0; i < len; i++)
    {
        if (str[i] < '0' || str[i] > '9')
            return false;
    }

    // Convert substring to unsigned
    char buf[32];
    if (len >= sizeof(buf))
        return false;
    memcpy(buf, str, len);
    buf[len] = '\0';

    *num = (unsigned)strtoul(buf, NULL, 10);
    return true;
}

static bool valid_identifier_char_set(const char* id, size_t len)
{
    for (size_t i = 0; i < len; i++)
    {
        char c = id[i];
        if (c >= 'A')
        {
            if ((c > 'Z' && c < 'a') || c > 'z')
                return false;
        }
        else
        {
            if ((c < '0' && c != '-') || c > '9')
                return false;
        }
    }
    return true;
}

static bool valid_identifier(const char* id, size_t id_len, bool build_meta)
{
    if (id_len == 0)
        return false;

    if (!valid_identifier_char_set(id, id_len))
        return false;

    if (!build_meta && id[0] == '0' && id_len > 1)
    {
        // Check if all-numeric (no leading zeros allowed)
        bool all_numeric = true;
        for (size_t i = 1; i < id_len; i++)
        {
            if (id[i] < '0' || id[i] > '9')
            {
                all_numeric = false;
                break;
            }
        }
        if (all_numeric)
            return false;
    }
    return true;
}

static bool valid_identifiers(const char* ids)
{
    if (ids[0] == '\0')
        return true;

    bool prerelease = ids[0] == '-';
    bool build_meta = ids[0] == '+';

    if (!(prerelease || build_meta))
        return false;

    size_t len = strlen(ids);
    size_t id_start = 1;
    for (size_t i = 1; i <= len; i++)
    {
        if (i == len || ids[i] == '.')
        {
            if (!valid_identifier(ids + id_start, i - id_start, build_meta))
                return false;
            id_start = i + 1;
        }
    }

    return true;
}

static bool parse_internal(const char* ver_str, fx_ver_t* out_ver, bool parse_only_production)
{
    size_t ver_len = strlen(ver_str);
    if (ver_len == 0)
        return false;

    // Find first dot (major.minor separator)
    const char* maj_dot = strchr(ver_str, '.');
    if (maj_dot == NULL)
        return false;

    size_t maj_len = (size_t)(maj_dot - ver_str);
    unsigned major_val = 0;
    if (!try_stou(ver_str, maj_len, &major_val))
        return false;
    if (maj_len > 1 && ver_str[0] == '0')
        return false; // no leading zeros

    // Find second dot (minor.patch separator)
    const char* min_start = maj_dot + 1;
    const char* min_dot = strchr(min_start, '.');
    if (min_dot == NULL)
        return false;

    size_t min_len = (size_t)(min_dot - min_start);
    unsigned minor_val = 0;
    if (!try_stou(min_start, min_len, &minor_val))
        return false;
    if (min_len > 1 && min_start[0] == '0')
        return false; // no leading zeros

    // Parse patch (and potentially prerelease/build)
    const char* pat_start = min_dot + 1;
    size_t pat_non_numeric = index_of_non_numeric(pat_start, 0);

    unsigned patch_val = 0;
    if (pat_non_numeric == (size_t)-1)
    {
        // Entire remainder is patch
        size_t pat_len = strlen(pat_start);
        if (!try_stou(pat_start, pat_len, &patch_val))
            return false;
        if (pat_len > 1 && pat_start[0] == '0')
            return false;

        fx_ver_set(out_ver, (int)major_val, (int)minor_val, (int)patch_val);
        return true;
    }

    if (parse_only_production)
        return false;

    if (!try_stou(pat_start, pat_non_numeric, &patch_val))
        return false;
    if (pat_non_numeric > 1 && pat_start[0] == '0')
        return false;

    // Parse prerelease and build
    const char* pre_start = pat_start + pat_non_numeric;
    const char* build_start = strchr(pre_start, '+');

    char pre_buf[256];
    pre_buf[0] = '\0';
    if (build_start != NULL)
    {
        size_t pre_len = (size_t)(build_start - pre_start);
        if (pre_len >= sizeof(pre_buf))
            return false;
        memcpy(pre_buf, pre_start, pre_len);
        pre_buf[pre_len] = '\0';
    }
    else
    {
        size_t pre_len = strlen(pre_start);
        if (pre_len >= sizeof(pre_buf))
            return false;
        memcpy(pre_buf, pre_start, pre_len + 1);
    }

    if (!valid_identifiers(pre_buf))
        return false;

    char build_buf[256];
    build_buf[0] = '\0';
    if (build_start != NULL)
    {
        size_t build_len = strlen(build_start);
        if (build_len >= sizeof(build_buf))
            return false;
        memcpy(build_buf, build_start, build_len + 1);

        if (!valid_identifiers(build_buf))
            return false;
    }

    fx_ver_set(out_ver, (int)major_val, (int)minor_val, (int)patch_val);
    memcpy(out_ver->pre, pre_buf, strlen(pre_buf) + 1);
    memcpy(out_ver->build, build_buf, strlen(build_buf) + 1);
    return true;
}

bool fx_ver_parse(const char* ver_str, fx_ver_t* out_ver, bool parse_only_production)
{
    fx_ver_init(out_ver);
    return parse_internal(ver_str, out_ver, parse_only_production);
}

// Get a dot-delimited identifier starting at position idStart.
// Returns length of the identifier.
static size_t get_id_len(const char* ids, size_t id_start)
{
    size_t i = id_start;
    while (ids[i] != '\0' && ids[i] != '.')
        i++;
    return i - id_start;
}

int fx_ver_compare(const fx_ver_t* a, const fx_ver_t* b)
{
    if (a->major != b->major)
        return (a->major > b->major) ? 1 : -1;

    if (a->minor != b->minor)
        return (a->minor > b->minor) ? 1 : -1;

    if (a->patch != b->patch)
        return (a->patch > b->patch) ? 1 : -1;

    bool a_empty = (a->pre[0] == '\0');
    bool b_empty = (b->pre[0] == '\0');

    if (a_empty || b_empty)
    {
        // Release > prerelease, both release are equal
        return a_empty ? (b_empty ? 0 : 1) : -1;
    }

    // Both are non-empty prerelease
    assert(a->pre[0] == '-');
    assert(b->pre[0] == '-');

    // Compare prerelease identifiers
    size_t id_start = 1;
    for (size_t i = id_start; ; ++i)
    {
        if (a->pre[i] != b->pre[i])
        {
            if (a->pre[i] == '\0' && b->pre[i] == '.')
                return -1;

            if (b->pre[i] == '\0' && a->pre[i] == '.')
                return 1;

            // Compare individual identifiers
            size_t ida_len = get_id_len(a->pre, id_start);
            size_t idb_len = get_id_len(b->pre, id_start);

            unsigned idanum = 0;
            bool ida_is_num = try_stou(a->pre + id_start, ida_len, &idanum);
            unsigned idbnum = 0;
            bool idb_is_num = try_stou(b->pre + id_start, idb_len, &idbnum);

            if (ida_is_num && idb_is_num)
                return (idanum > idbnum) ? 1 : -1;
            else if (ida_is_num || idb_is_num)
                return idb_is_num ? 1 : -1;

            // String comparison
            size_t min_len = ida_len < idb_len ? ida_len : idb_len;
            int cmp = strncmp(a->pre + id_start, b->pre + id_start, min_len);
            if (cmp != 0)
                return cmp;
            if (ida_len != idb_len)
                return ida_len > idb_len ? 1 : -1;
            return 0;
        }
        else
        {
            if (a->pre[i] == '\0')
                break;
            if (a->pre[i] == '.')
                id_start = i + 1;
        }
    }

    return 0;
}
