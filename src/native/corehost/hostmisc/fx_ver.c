// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "fx_ver_c.h"

#include <stdlib.h>
#include <assert.h>
#include <ctype.h>
#include <minipal/utils.h>

static pal_char_t* pal_strdup_c(const pal_char_t* s)
{
    if (s == NULL)
        return NULL;
    size_t len = pal_strlen(s);
    pal_char_t* dup = (pal_char_t*)malloc((len + 1) * sizeof(pal_char_t));
    if (dup != NULL)
        memcpy(dup, s, (len + 1) * sizeof(pal_char_t));
    return dup;
}

void c_fx_ver_init(c_fx_ver_t* ver)
{
    ver->major = -1;
    ver->minor = -1;
    ver->patch = -1;
    ver->pre = NULL;
    ver->build = NULL;
}

void c_fx_ver_cleanup(c_fx_ver_t* ver)
{
    free(ver->pre);
    ver->pre = NULL;
    free(ver->build);
    ver->build = NULL;
}

void c_fx_ver_set(c_fx_ver_t* ver, int major, int minor, int patch)
{
    ver->major = major;
    ver->minor = minor;
    ver->patch = patch;
    free(ver->pre);
    ver->pre = NULL;
    free(ver->build);
    ver->build = NULL;
}

bool c_fx_ver_is_empty(const c_fx_ver_t* ver)
{
    return ver->major == -1;
}

pal_char_t* c_fx_ver_as_str(const c_fx_ver_t* ver, pal_char_t* out_str, size_t out_str_len)
{
    bool has_pre = ver->pre != NULL && ver->pre[0] != _X('\0');
    bool has_build = ver->build != NULL && ver->build[0] != _X('\0');

    if (has_pre && has_build)
    {
        pal_str_printf(out_str, out_str_len, _X("%d.%d.%d") _X("%s") _X("%s"),
            ver->major, ver->minor, ver->patch, ver->pre, ver->build);
    }
    else if (has_pre)
    {
        pal_str_printf(out_str, out_str_len, _X("%d.%d.%d") _X("%s"),
            ver->major, ver->minor, ver->patch, ver->pre);
    }
    else if (has_build)
    {
        pal_str_printf(out_str, out_str_len, _X("%d.%d.%d") _X("%s"),
            ver->major, ver->minor, ver->patch, ver->build);
    }
    else
    {
        pal_str_printf(out_str, out_str_len, _X("%d.%d.%d"),
            ver->major, ver->minor, ver->patch);
    }

    return out_str;
}

static size_t index_of_non_numeric(const pal_char_t* str, size_t start)
{
    for (size_t i = start; str[i] != _X('\0'); i++)
    {
        if (str[i] < _X('0') || str[i] > _X('9'))
            return i;
    }
    return (size_t)-1; // npos equivalent
}

static bool try_stou(const pal_char_t* str, size_t len, unsigned* num)
{
    if (len == 0)
        return false;

    for (size_t i = 0; i < len; i++)
    {
        if (str[i] < _X('0') || str[i] > _X('9'))
            return false;
    }

    // Convert substring to unsigned
    pal_char_t buf[32];
    if (len >= ARRAY_SIZE(buf))
        return false;
    memcpy(buf, str, len * sizeof(pal_char_t));
    buf[len] = _X('\0');

    *num = (unsigned)pal_strtoul(buf, NULL, 10);
    return true;
}

static bool valid_identifier_char_set(const pal_char_t* id, size_t len)
{
    for (size_t i = 0; i < len; i++)
    {
        pal_char_t c = id[i];
        if (c >= _X('A'))
        {
            if ((c > _X('Z') && c < _X('a')) || c > _X('z'))
                return false;
        }
        else
        {
            if ((c < _X('0') && c != _X('-')) || c > _X('9'))
                return false;
        }
    }
    return true;
}

static bool valid_identifier(const pal_char_t* id, size_t id_len, bool build_meta)
{
    if (id_len == 0)
        return false;

    if (!valid_identifier_char_set(id, id_len))
        return false;

    if (!build_meta && id[0] == _X('0') && id_len > 1)
    {
        // Check if all-numeric (no leading zeros allowed)
        bool all_numeric = true;
        for (size_t i = 1; i < id_len; i++)
        {
            if (id[i] < _X('0') || id[i] > _X('9'))
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

static bool valid_identifiers(const pal_char_t* ids)
{
    if (ids[0] == _X('\0'))
        return true;

    bool prerelease = ids[0] == _X('-');
    bool build_meta = ids[0] == _X('+');

    if (!(prerelease || build_meta))
        return false;

    size_t len = pal_strlen(ids);
    size_t id_start = 1;
    for (size_t i = 1; i <= len; i++)
    {
        if (i == len || ids[i] == _X('.'))
        {
            if (!valid_identifier(ids + id_start, i - id_start, build_meta))
                return false;
            id_start = i + 1;
        }
    }

    return true;
}

static bool parse_internal(const pal_char_t* ver_str, c_fx_ver_t* out_ver, bool parse_only_production)
{
    size_t ver_len = pal_strlen(ver_str);
    if (ver_len == 0)
        return false;

    // Find first dot (major.minor separator)
    const pal_char_t* maj_dot = pal_strchr(ver_str, _X('.'));
    if (maj_dot == NULL)
        return false;

    size_t maj_len = (size_t)(maj_dot - ver_str);
    unsigned major_val = 0;
    if (!try_stou(ver_str, maj_len, &major_val))
        return false;
    if (maj_len > 1 && ver_str[0] == _X('0'))
        return false; // no leading zeros

    // Find second dot (minor.patch separator)
    const pal_char_t* min_start = maj_dot + 1;
    const pal_char_t* min_dot = pal_strchr(min_start, _X('.'));
    if (min_dot == NULL)
        return false;

    size_t min_len = (size_t)(min_dot - min_start);
    unsigned minor_val = 0;
    if (!try_stou(min_start, min_len, &minor_val))
        return false;
    if (min_len > 1 && min_start[0] == _X('0'))
        return false; // no leading zeros

    // Parse patch (and potentially prerelease/build)
    const pal_char_t* pat_start = min_dot + 1;
    size_t pat_non_numeric = index_of_non_numeric(pat_start, 0);

    unsigned patch_val = 0;
    if (pat_non_numeric == (size_t)-1)
    {
        // Entire remainder is patch
        size_t pat_len = pal_strlen(pat_start);
        if (!try_stou(pat_start, pat_len, &patch_val))
            return false;
        if (pat_len > 1 && pat_start[0] == _X('0'))
            return false;

        c_fx_ver_set(out_ver, (int)major_val, (int)minor_val, (int)patch_val);
        return true;
    }

    if (parse_only_production)
        return false;

    if (!try_stou(pat_start, pat_non_numeric, &patch_val))
        return false;
    if (pat_non_numeric > 1 && pat_start[0] == _X('0'))
        return false;

    // Parse prerelease and build
    const pal_char_t* pre_start = pat_start + pat_non_numeric;
    const pal_char_t* build_start = pal_strchr(pre_start, _X('+'));

    pal_char_t* pre_buf = NULL;
    if (build_start != NULL)
    {
        size_t pre_len = (size_t)(build_start - pre_start);
        pre_buf = (pal_char_t*)malloc((pre_len + 1) * sizeof(pal_char_t));
        if (pre_buf == NULL)
            return false;
        memcpy(pre_buf, pre_start, pre_len * sizeof(pal_char_t));
        pre_buf[pre_len] = _X('\0');
    }
    else
    {
        size_t pre_len = pal_strlen(pre_start);
        pre_buf = (pal_char_t*)malloc((pre_len + 1) * sizeof(pal_char_t));
        if (pre_buf == NULL)
            return false;
        memcpy(pre_buf, pre_start, (pre_len + 1) * sizeof(pal_char_t));
    }

    if (!valid_identifiers(pre_buf))
    {
        free(pre_buf);
        return false;
    }

    pal_char_t* build_buf = NULL;
    if (build_start != NULL)
    {
        size_t build_len = pal_strlen(build_start);
        build_buf = (pal_char_t*)malloc((build_len + 1) * sizeof(pal_char_t));
        if (build_buf == NULL)
        {
            free(pre_buf);
            return false;
        }
        memcpy(build_buf, build_start, (build_len + 1) * sizeof(pal_char_t));

        if (!valid_identifiers(build_buf))
        {
            free(pre_buf);
            free(build_buf);
            return false;
        }
    }

    c_fx_ver_set(out_ver, (int)major_val, (int)minor_val, (int)patch_val);
    out_ver->pre = pre_buf;
    out_ver->build = build_buf;
    return true;
}

bool c_fx_ver_parse(const pal_char_t* ver_str, c_fx_ver_t* out_ver, bool parse_only_production)
{
    c_fx_ver_init(out_ver);
    return parse_internal(ver_str, out_ver, parse_only_production);
}

// Get a dot-delimited identifier starting at position idStart.
// Returns length of the identifier.
static size_t get_id_len(const pal_char_t* ids, size_t id_start)
{
    size_t i = id_start;
    while (ids[i] != _X('\0') && ids[i] != _X('.'))
        i++;
    return i - id_start;
}

int c_fx_ver_compare(const c_fx_ver_t* a, const c_fx_ver_t* b)
{
    if (a->major != b->major)
        return (a->major > b->major) ? 1 : -1;

    if (a->minor != b->minor)
        return (a->minor > b->minor) ? 1 : -1;

    if (a->patch != b->patch)
        return (a->patch > b->patch) ? 1 : -1;

    bool a_empty = (a->pre == NULL || a->pre[0] == _X('\0'));
    bool b_empty = (b->pre == NULL || b->pre[0] == _X('\0'));

    if (a_empty || b_empty)
    {
        // Release > prerelease, both release are equal
        return a_empty ? (b_empty ? 0 : 1) : -1;
    }

    // Both are non-empty prerelease
    assert(a->pre[0] == _X('-'));
    assert(b->pre[0] == _X('-'));

    // Compare prerelease identifiers
    size_t id_start = 1;
    for (size_t i = id_start; ; ++i)
    {
        if (a->pre[i] != b->pre[i])
        {
            if (a->pre[i] == _X('\0') && b->pre[i] == _X('.'))
                return -1;

            if (b->pre[i] == _X('\0') && a->pre[i] == _X('.'))
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
            int cmp = pal_strncmp(a->pre + id_start, b->pre + id_start, min_len);
            if (cmp != 0)
                return cmp;
            if (ida_len != idb_len)
                return ida_len > idb_len ? 1 : -1;
            return 0;
        }
        else
        {
            if (a->pre[i] == _X('\0'))
                break;
            if (a->pre[i] == _X('.'))
                id_start = i + 1;
        }
    }

    return 0;
}
