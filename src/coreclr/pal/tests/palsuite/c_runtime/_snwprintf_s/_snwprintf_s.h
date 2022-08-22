// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  swprintf_s.h
**
** Purpose: Contains common testing functions for swprintf_s
**
**
**==========================================================================*/

#ifndef ___SNWPRINTF_H__
#define ___SNWPRINTF_H__

inline void DoWStrTest_snwprintf_s(const WCHAR *formatstr, WCHAR *param, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    _snwprintf_s(buf, 256, _TRUNCATE, formatstr, param);

    if (memcmp(buf, checkstr, wcslen(checkstr) * 2 + 2) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\".\n"
             "Expected \"%s\", got \"%s\".\n", convertC(param),
             convertC(formatstr), convertC(checkstr), convertC(buf));
    }
}
#define DoWStrTest DoWStrTest_snwprintf_s

inline void DoStrTest_snwprintf_s(const WCHAR *formatstr, char *param, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    _snwprintf_s(buf, 256, _TRUNCATE, formatstr, param);

    if (memcmp(buf, checkstr, wcslen(checkstr) * 2 + 2) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\".\n"
             "Expected \"%s\", got \"%s\".\n",
             param, convertC(formatstr), convertC(checkstr), convertC(buf));
    }
}
#define DoStrTest DoStrTest_snwprintf_s

inline void DoPointerTest_snwprintf_s(const WCHAR *formatstr, void* param, const WCHAR *checkstr1)
{
    WCHAR buf[256] = { 0 };

    _snwprintf_s(buf, 256, _TRUNCATE, formatstr, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert pointer to %#p into \"%s\"\n"
             "Expected \"%s\", got \"%s\".\n", param, convertC(formatstr),
             convertC(checkstr1), convertC(buf));
    }
}
#define DoPointerTest DoPointerTest_snwprintf_s

inline void DoCountTest_snwprintf_s(const WCHAR *formatstr, int param, const WCHAR *checkstr)
{
    WCHAR buf[512] = { 0 };
    int n = -1;

    swprintf_s(buf, 512, formatstr, &n);

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %d\n",
             param, n);
    }

    if (memcmp(buf, checkstr, wcslen(checkstr)*2 + 2) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n",
             convertC(checkstr), convertC(buf));
    }
}
#define DoCountTest DoCountTest_snwprintf_s

inline void DoShortCountTest_snwprintf_s(const WCHAR *formatstr, int param, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };
    short int n = -1;

    _snwprintf_s(buf, 256, _TRUNCATE, formatstr, &n);

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %d\n",
             param, n);
    }

    if (memcmp(buf, checkstr, wcslen(checkstr)*2 + 2) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n",
             convertC(checkstr), convertC(buf));
    }
}
#define DoShortCountTest DoShortCountTest_snwprintf_s

inline void DoCharTest_snwprintf_s(const WCHAR *formatstr, char param, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    _snwprintf_s(buf, 256, _TRUNCATE, formatstr, param);
    if (memcmp(buf, checkstr, wcslen(checkstr)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert char \'%c\' (%d) into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n", param, param,
             convertC(formatstr), convertC(checkstr), convertC(buf));
    }
}
#define DoCharTest DoCharTest_snwprintf_s

inline void DoWCharTest_snwprintf_s(const WCHAR *formatstr, WCHAR param, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    _snwprintf_s(buf, 256, _TRUNCATE, formatstr, param);
    if (memcmp(buf, checkstr, wcslen(checkstr)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert wide char \'%c\' (%d) into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n", (char) param, param,
             convertC(formatstr),  convertC(checkstr), convertC(buf));
    }
}
#define DoWCharTest DoWCharTest_snwprintf_s

inline void DoNumTest_snwprintf_s(const WCHAR *formatstr, int value, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    _snwprintf_s(buf, 256, _TRUNCATE, formatstr, value);
    if (memcmp(buf, checkstr, wcslen(checkstr)* 2 + 2) != 0)
    {
        Fail("ERROR: failed to insert %#x into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n", value, convertC(formatstr),
             convertC(checkstr), convertC(buf));
    }
}
#define DoNumTest DoNumTest_snwprintf_s

inline void DoI64Test_snwprintf_s(const WCHAR *formatstr, INT64 param, char *paramdesc,
               const WCHAR *checkstr1)
{
    WCHAR buf[256] = { 0 };

    _snwprintf_s(buf, 256, _TRUNCATE, formatstr, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
             "Expected \"%s\", got \"%s\".\n", paramdesc,
            convertC(formatstr), convertC(checkstr1), convertC(buf));
    }
}
#define DoI64Test DoI64Test_snwprintf_s

inline void DoDoubleTest_snwprintf_s(const WCHAR *formatstr, double value, const WCHAR *checkstr1,
                  const WCHAR *checkstr2)
{
    WCHAR buf[256] = { 0 };

    _snwprintf_s(buf, 256, _TRUNCATE, formatstr, value);
    if (memcmp(buf, checkstr1, wcslen(checkstr1)*2 + 2) != 0 &&
        memcmp(buf, checkstr2, wcslen(checkstr2)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\"\n"
             "Expected \"%s\" or \"%s\", got \"%s\".\n",
             value, convertC(formatstr), convertC(checkstr1),
             convertC(checkstr2), convertC(buf));
    }
}
#define DoDoubleTest DoDoubleTest_snwprintf_s

inline void DoArgumentPrecTest_snwprintf_s(const WCHAR *formatstr, int precision, void *param,
                        char *paramstr, const WCHAR *checkstr1, const WCHAR *checkstr2)
{
    WCHAR buf[256];

    _snwprintf_s(buf, 256, _TRUNCATE, formatstr, precision, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1) + 2) != 0 &&
        memcmp(buf, checkstr2, wcslen(checkstr2) + 2) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\" with precision %d\n"
             "Expected \"%s\" or \"%s\", got \"%s\".\n",
             paramstr, convertC(formatstr), precision,
            convertC(checkstr1), convertC(checkstr2) ,convertC(buf));
    }
}
#define DoArgumentPrecTest DoArgumentPrecTest_snwprintf_s

inline void DoArgumentPrecDoubleTest_snwprintf_s(const WCHAR *formatstr, int precision, double param,
                              const WCHAR *checkstr)
{
    WCHAR buf[256];

    _snwprintf_s(buf, 256, _TRUNCATE, formatstr, precision, param);
    if (memcmp(buf, checkstr, wcslen(checkstr) + 2) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\" with precision %d\n"
             "Expected \"%s\", got \"%s\".\n", param, convertC(formatstr),
             precision, convertC(checkstr), convertC(buf));
    }
}
#define DoArgumentPrecDoubleTest DoArgumentPrecDoubleTest_snwprintf_s

#endif

