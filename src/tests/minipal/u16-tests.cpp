// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern "C"
{
#include "test.h"
}

#include <errno.h>
#include <limits.h>
#include <math.h>
#include <stdint.h>
#include <minipal/utils.h>

#ifdef _WIN32
typedef wchar_t WCHAR;
#else
typedef char16_t WCHAR;
#endif

#include <dn-u16.h>

#define W(str) ((const WCHAR*)u##str)

// A value the parsers never set, to see whether they touch errno
#define ERRNO_SENTINEL 12345

static RESULT test_u16_strlen(void)
{
    if (u16_strlen(W("foo ")) != 4)
        return FAILED("u16_strlen(\"foo \") != 4");

    if (u16_strlen(W("")) != 0)
        return FAILED("u16_strlen(\"\") != 0");

    return OK;
}

static RESULT test_u16_strcmp(void)
{
    if (u16_strcmp(W("foo"), W("foo")) != 0)
        return FAILED("u16_strcmp(\"foo\", \"foo\") != 0");

    if (u16_strcmp(W("foo"), W("foox")) >= 0)
        return FAILED("u16_strcmp(\"foo\", \"foox\") >= 0");

    if (u16_strcmp(W("foox"), W("foo")) <= 0)
        return FAILED("u16_strcmp(\"foox\", \"foo\") <= 0");

    if (u16_strcmp(W("foo"), W("fOo")) <= 0)
        return FAILED("u16_strcmp(\"foo\", \"fOo\") <= 0");

    if (u16_strcmp(W("fOo"), W("foo")) >= 0)
        return FAILED("u16_strcmp(\"fOo\", \"foo\") >= 0");

    return OK;
}

static RESULT test_u16_strncmp(void)
{
    if (u16_strncmp(W("foo"), W("foox"), 4) >= 0)
        return FAILED("u16_strncmp(\"foo\", \"foox\", 4) >= 0");

    if (u16_strncmp(W("foox"), W("foo"), 4) <= 0)
        return FAILED("u16_strncmp(\"foox\", \"foo\", 4) <= 0");

    if (u16_strncmp(W("foo"), W("foox"), 3) != 0)
        return FAILED("u16_strncmp(\"foo\", \"foox\", 3) != 0");

    if (u16_strncmp(W("foo"), W("fOo"), 3) <= 0)
        return FAILED("u16_strncmp(\"foo\", \"fOo\", 3) <= 0");

    if (u16_strncmp(W("fOo"), W("foo"), 3) >= 0)
        return FAILED("u16_strncmp(\"fOo\", \"foo\", 3) >= 0");

    if (u16_strncmp(W("abc"), W("xyz"), 0) != 0)
        return FAILED("u16_strncmp(\"abc\", \"xyz\", 0) != 0");

    return OK;
}

static RESULT test_u16_stricmp(void)
{
    if (u16_stricmp(W("foo"), W("fOo")) != 0)
        return FAILED("u16_stricmp(\"foo\", \"fOo\") != 0");

    if (u16_stricmp(W("fOo"), W("foo_bar")) >= 0)
        return FAILED("u16_stricmp(\"fOo\", \"foo_bar\") >= 0");

    if (u16_stricmp(W("foo_bar"), W("foobar")) >= 0)
        return FAILED("u16_stricmp(\"foo_bar\", \"foobar\") >= 0");

    if (u16_stricmp(W("foobar"), W("foo")) <= 0)
        return FAILED("u16_stricmp(\"foobar\", \"foo\") <= 0");

    if (u16_stricmp(W("foo_bar"), W("fOo")) <= 0)
        return FAILED("u16_stricmp(\"foo_bar\", \"fOo\") <= 0");

    if (u16_stricmp(W(""), W("")) != 0)
        return FAILED("u16_stricmp(\"\", \"\") != 0");

    return OK;
}

static RESULT test_u16_strnicmp(void)
{
    if (u16_strnicmp(W("foo"), W("foox"), 4) >= 0)
        return FAILED("u16_strnicmp(\"foo\", \"foox\", 4) >= 0");

    if (u16_strnicmp(W("foox"), W("foo"), 4) <= 0)
        return FAILED("u16_strnicmp(\"foox\", \"foo\", 4) <= 0");

    if (u16_strnicmp(W("foo"), W("foox"), 3) != 0)
        return FAILED("u16_strnicmp(\"foo\", \"foox\", 3) != 0");

    if (u16_strnicmp(W("foo"), W("fOo"), 3) != 0)
        return FAILED("u16_strnicmp(\"foo\", \"fOo\", 3) != 0");

    // ']' sorts between the upper and lower case letters, so these show that both sides are lowered
    if (u16_strnicmp(W("ABCDE"), W("ABCD]"), 5) <= 0)
        return FAILED("u16_strnicmp(\"ABCDE\", \"ABCD]\", 5) <= 0");

    if (u16_strnicmp(W("abcde"), W("abcd]"), 5) <= 0)
        return FAILED("u16_strnicmp(\"abcde\", \"abcd]\", 5) <= 0");

    if (u16_strnicmp(W("api-ms-win"), W("API-xx"), 4) != 0)
        return FAILED("u16_strnicmp(\"api-ms-win\", \"API-xx\", 4) != 0");

    if (u16_strnicmp(W("ab"), W("AB"), 10) != 0)
        return FAILED("u16_strnicmp(\"ab\", \"AB\", 10) != 0");

    return OK;
}

static RESULT test_u16_strchr(void)
{
    const WCHAR* str = W("foo bar ");

    if (u16_strchr(str, (WCHAR)' ') != str + 3)
        return FAILED("u16_strchr didn't find the first ' '");

    if (u16_strchr(str, (WCHAR)'$') != NULL)
        return FAILED("u16_strchr found a missing character");

    if (u16_strchr(str, (WCHAR)'\0') != str + 8)
        return FAILED("u16_strchr didn't find the null terminator");

    return OK;
}

static RESULT test_u16_strrchr(void)
{
    const WCHAR* str = W("foo bar baz");

    if (u16_strrchr(str, (WCHAR)' ') != str + 7)
        return FAILED("u16_strrchr didn't find the last ' '");

    if (u16_strrchr(str, (WCHAR)'$') != NULL)
        return FAILED("u16_strrchr found a missing character");

    return OK;
}

static RESULT test_u16_strstr(void)
{
    const WCHAR* str = W("foo bar baz bar");

    if (u16_strstr(str, W("bar")) != str + 4)
        return FAILED("u16_strstr didn't find the first \"bar\"");

    if (u16_strstr(str, W("Bar")) != NULL)
        return FAILED("u16_strstr ignored case");

    if (u16_strstr(str, W("")) != str)
        return FAILED("u16_strstr didn't return the string for an empty key");

    if (u16_strstr(str, W("arggggh!")) != NULL)
        return FAILED("u16_strstr found a missing string");

    return OK;
}

static RESULT test_u16_strcpy_s(void)
{
    WCHAR dest[80];

    if (u16_strcpy_s(dest, ARRAY_SIZE(dest), W("foo")) != dest)
        return FAILED("u16_strcpy_s didn't return the destination");

    if (u16_strcmp(dest, W("foo")) != 0)
        return FAILED("u16_strcpy_s didn't copy \"foo\"");

    return OK;
}

static RESULT test_u16_strcat_s(void)
{
    WCHAR dest[80];
    dest[0] = 0;

    if (u16_strcat_s(dest, ARRAY_SIZE(dest), W("foo ")) != dest ||
        u16_strcat_s(dest, ARRAY_SIZE(dest), W("bar ")) != dest ||
        u16_strcat_s(dest, ARRAY_SIZE(dest), W("baz")) != dest)
    {
        return FAILED("u16_strcat_s didn't return the destination");
    }

    if (u16_strcmp(dest, W("foo bar baz")) != 0)
        return FAILED("u16_strcat_s didn't give \"foo bar baz\"");

    return OK;
}

static RESULT test_u16_strncpy_s(void)
{
    WCHAR dest[80];
    for (size_t i = 0; i < ARRAY_SIZE(dest); i++)
        dest[i] = (WCHAR)'x';

    if (u16_strncpy_s(dest, ARRAY_SIZE(dest), W("foobar"), 3) != dest)
        return FAILED("u16_strncpy_s didn't return the destination");

    if (u16_strcmp(dest, W("foo")) != 0)
        return FAILED("u16_strncpy_s didn't copy and terminate \"foo\"");

    if (u16_strncpy_s(dest, ARRAY_SIZE(dest), W("foobar"), 40) != dest)
        return FAILED("u16_strncpy_s didn't return the destination");

    if (u16_strcmp(dest, W("foobar")) != 0)
        return FAILED("u16_strncpy_s didn't copy \"foobar\"");

    return OK;
}

typedef struct
{
    const WCHAR* str;
    int base;
    uint32_t value;
    size_t stop;
    int error;
} StrToULCase;

static const StrToULCase strtoul_cases[] =
{
    { W("12345"), 4, 27, 3, 0 },
    { W("12345"), 10, 12345, 5, 0 },
    { W("Z"), 10, 0, 0, 0 },
    { W(""), 10, 0, 0, 0 },
    { W("4294967295"), 10, UINT32_MAX, 10, 0 },
    { W("4294967296"), 10, UINT32_MAX, 10, ERANGE },
    // Beyond 64 bits as well
    { W("99999999999999999999"), 10, UINT32_MAX, 20, ERANGE },
    { W("-1"), 10, UINT32_MAX, 2, 0 },
    { W("-4"), 10, 0xFFFFFFFC, 2, 0 },
#if ULONG_MAX == UINT32_MAX
    { W("-4294967296"), 10, UINT32_MAX, 11, ERANGE },
#else
    // Negative values are truncated to 32 bits where unsigned long is 64-bit, like the PAL wcstoul did
    { W("-4294967296"), 10, 0, 11, 0 },
    { W("-4294967297"), 10, UINT32_MAX, 11, 0 },
#endif
    { W("0x12"), 16, 0x12, 4, 0 },
    { W("0x12"), 10, 0, 1, 0 },
    { W("012"), 8, 10, 3, 0 },
    { W("012"), 10, 12, 3, 0 },
    { W(" \t42 "), 10, 42, 4, 0 },
    { W("+7"), 10, 7, 2, 0 },
    { W("12\x00E9" "3"), 10, 12, 2, 0 },
};

static RESULT test_u16_strtoul(void)
{
    for (size_t i = 0; i < ARRAY_SIZE(strtoul_cases); i++)
    {
        const StrToULCase* c = &strtoul_cases[i];
        WCHAR* end;
        errno = ERRNO_SENTINEL;
        uint32_t value = u16_strtoul(c->str, &end, c->base);
        int error = errno == ERRNO_SENTINEL ? 0 : errno;

        if (value != c->value)
            return FAILED("u16_strtoul case %zu returned %u instead of %u", i, value, c->value);

        if (end != c->str + c->stop)
            return FAILED("u16_strtoul case %zu stopped at %td instead of %zu", i, end - c->str, c->stop);

        if (error != c->error)
            return FAILED("u16_strtoul case %zu set errno to %d instead of %d", i, error, c->error);
    }

    if (u16_strtoul(W("123"), NULL, 10) != 123)
        return FAILED("u16_strtoul didn't accept a null end pointer");

    return OK;
}

typedef struct
{
    const WCHAR* str;
    int base;
    uint64_t value;
    size_t stop;
    int error;
} StrToUI64Case;

static const StrToUI64Case strtoui64_cases[] =
{
    { W("12345"), 10, 12345, 5, 0 },
    { W("Z"), 10, 0, 0, 0 },
    { W("4294967296"), 10, 4294967296ull, 10, 0 },
    { W("18446744073709551615"), 10, UINT64_MAX, 20, 0 },
    { W("18446744073709551616"), 10, UINT64_MAX, 20, ERANGE },
    { W("-1"), 10, UINT64_MAX, 2, 0 },
    { W("ffffffffff"), 16, 0xffffffffffull, 10, 0 },
    { W("  123abc"), 10, 123, 5, 0 },
};

static RESULT test_u16_strtoui64(void)
{
    for (size_t i = 0; i < ARRAY_SIZE(strtoui64_cases); i++)
    {
        const StrToUI64Case* c = &strtoui64_cases[i];
        WCHAR* end;
        errno = ERRNO_SENTINEL;
        uint64_t value = u16_strtoui64(c->str, &end, c->base);
        int error = errno == ERRNO_SENTINEL ? 0 : errno;

        if (value != c->value)
            return FAILED("u16_strtoui64 case %zu returned %llu instead of %llu", i, (unsigned long long)value, (unsigned long long)c->value);

        if (end != c->str + c->stop)
            return FAILED("u16_strtoui64 case %zu stopped at %td instead of %zu", i, end - c->str, c->stop);

        if (error != c->error)
            return FAILED("u16_strtoui64 case %zu set errno to %d instead of %d", i, error, c->error);
    }

    return OK;
}

typedef struct
{
    const WCHAR* str;
    double value;
    size_t stop;
} StrToDCase;

static const StrToDCase strtod_cases[] =
{
    { W("1234"), 1234, 4 },
    { W("-1234"), -1234, 5 },
    { W("1234.44"), 1234.44, 7 },
    { W("1234e-5"), 1234e-5, 7 },
    { W("1234e+5"), 1234e+5, 7 },
    { W("1234E5"), 1234E5, 6 },
    { W("1234.657e-8"), 1234.657e-8, 11 },
    { W("1e-800"), 0, 6 },
    { W("-1e-800"), 0, 7 },
    { W("   1234567e-8 foo"), 1234567e-8, 13 },
    { W(" foo 32 bar"), 0, 0 },
    { W("  -1b  "), -1, 4 },
    { W(""), 0, 0 },
    { W("12\x00E9"), 12, 2 },
    { W("1.5e"), 1.5, 3 },
    { W("1.5e+"), 1.5, 3 },
    { W("inf"), HUGE_VAL, 3 },
    { W("0x10"), 16, 4 },
};

static RESULT test_u16_strtod(void)
{
    for (size_t i = 0; i < ARRAY_SIZE(strtod_cases); i++)
    {
        const StrToDCase* c = &strtod_cases[i];
        WCHAR* end;
        double value = u16_strtod(c->str, &end);

        if (value != c->value)
            return FAILED("u16_strtod case %zu returned %g instead of %g", i, value, c->value);

        if (end != c->str + c->stop)
            return FAILED("u16_strtod case %zu stopped at %td instead of %zu", i, end - c->str, c->stop);
    }

    if (u16_strtod(W("1E+10000"), NULL) != HUGE_VAL)
        return FAILED("u16_strtod(\"1E+10000\") isn't infinity");

    if (u16_strtod(W("-1E+10000"), NULL) != -HUGE_VAL)
        return FAILED("u16_strtod(\"-1E+10000\") isn't negative infinity");

    return OK;
}

static Test minipal_u16_tests[] =
{
    {"test_u16_strlen", test_u16_strlen},
    {"test_u16_strcmp", test_u16_strcmp},
    {"test_u16_strncmp", test_u16_strncmp},
    {"test_u16_stricmp", test_u16_stricmp},
    {"test_u16_strnicmp", test_u16_strnicmp},
    {"test_u16_strchr", test_u16_strchr},
    {"test_u16_strrchr", test_u16_strrchr},
    {"test_u16_strstr", test_u16_strstr},
    {"test_u16_strcpy_s", test_u16_strcpy_s},
    {"test_u16_strcat_s", test_u16_strcat_s},
    {"test_u16_strncpy_s", test_u16_strncpy_s},
    {"test_u16_strtoul", test_u16_strtoul},
    {"test_u16_strtoui64", test_u16_strtoui64},
    {"test_u16_strtod", test_u16_strtod},
    {NULL, NULL}
};

extern "C" DEFINE_TEST_GROUP_INIT(minipal_u16_tests_init, minipal_u16_tests)
