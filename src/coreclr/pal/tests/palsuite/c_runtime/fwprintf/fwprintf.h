// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      fwprintf.h
**
** Purpose:     Contains common testing functions for fwprintf
**
**
**==========================================================================*/

#ifndef __fwprintf_H__
#define __fwprintf_H__

inline void DoStrTest_fwprintf(const WCHAR *formatstr, char* param, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }
    if ((fwprintf(fp, formatstr, param)) < 0)
    {
        Fail("ERROR: fwprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }

    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert string \"%\" into \"%S\"\n"
            "Expected \"%s\" got \"%s\".\n",
            param, formatstr, checkstr, buf);
    }
    fclose(fp);
}
#define DoStrTest DoStrTest_fwprintf

inline void DoWStrTest_fwprintf(const WCHAR *formatstr, WCHAR* param, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }
    if ((fwprintf(fp, formatstr, param)) < 0)
    {
        Fail("ERROR: fwprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }

    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%S\"\n"
            "Expected \"%s\" got \"%s\".\n",
            convertC(param), formatstr, checkstr, buf);
    }
    fclose(fp);
}
#define DoWStrTest DoWStrTest_fwprintf

inline void DoPointerTest_fwprintf(const WCHAR *formatstr, void* param, char* paramstr,
                   const char *checkstr1, const char *checkstr2)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((fwprintf(fp, formatstr, param)) < 0)
    {
        Fail("ERROR: fwprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }

    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0 )
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\" or \"%s\" got \"%s\".\n",
            paramstr, formatstr, checkstr1, checkstr2, buf);
    }

    if ((fclose( fp )) != 0)
    {
        Fail("ERROR: fclose failed to close \"testfile.txt\"\n");
    }
}
#define DoPointerTest DoPointerTest_fwprintf


inline void DoCountTest_fwprintf(const WCHAR *formatstr, int param, const char *checkstr)
{
    FILE *fp;
    char buf[512] = { 0 };
    int n = -1;

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((fwprintf(fp, formatstr, &n)) < 0)
    {
        Fail("ERROR: fwprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, sizeof(buf), fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %X\n",
            param, n);
    }

    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n", checkstr, buf);
    }

    if ((fclose( fp )) != 0)
    {
        Fail("ERROR: fclose failed to close \"testfile.txt\"\n");
    }
}
#define DoCountTest DoCountTest_fwprintf

inline void DoShortCountTest_fwprintf(const WCHAR *formatstr, int param, const char *checkstr)
{
    FILE *fp;
    char buf[512] = { 0 };
    short int n = -1;

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((fwprintf(fp, formatstr, &n)) < 0)
    {
        Fail("ERROR: fwprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %X\n",
            param, n);
    }

    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n", checkstr, buf);
    }

    if ((fclose( fp )) != 0)
    {
        Fail("ERROR: fclose failed to close \"testfile.txt\"\n");
    }
}
#define DoShortCountTest DoShortCountTest_fwprintf

inline void DoCharTest_fwprintf(const WCHAR *formatstr, char param, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }
    if ((fwprintf(fp, formatstr, param)) < 0)
    {
        Fail("ERROR: fwprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }

    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert char \'%c\' (%d) into \"%S\"\n"
            "Expected \"%s\" got \"%s\".\n",
            param, param, formatstr, checkstr, buf);
    }
    fclose(fp);
}
#define DoCharTest DoCharTest_fwprintf

inline void DoWCharTest_fwprintf(const WCHAR *formatstr, WCHAR param, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }
    if ((fwprintf(fp, formatstr, param)) < 0)
    {
        Fail("ERROR: fwprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }

    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide char \'%c\' (%d) into \"%S\"\n"
            "Expected \"%s\" got \"%s\".\n",
            (char)param, param, formatstr, checkstr, buf);
    }
    fclose(fp);
}
#define DoWCharTest DoWCharTest_fwprintf

inline void DoNumTest_fwprintf(const WCHAR *formatstr, int value, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }
    if ((fwprintf(fp, formatstr, value)) < 0)
    {
        Fail("ERROR: fwprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }

    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert %#x into \"%S\"\n"
            "Expected \"%s\" got \"%s\".\n",
            value, formatstr, checkstr, buf);
    }
    fclose(fp);
}
#define DoNumTest DoNumTest_fwprintf

inline void DoI64Test_fwprintf(const WCHAR *formatstr, INT64 value, char *valuestr, const char *checkstr1,
               const char *checkstr2)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }
    if ((fwprintf(fp, formatstr, value)) < 0)
    {
        Fail("ERROR: fwprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }

    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%S\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n",
            valuestr, formatstr, checkstr1, checkstr2, buf);
    }
    fclose(fp);
}
#define DoI64Test DoI64Test_fwprintf

inline void DoDoubleTest_fwprintf(const WCHAR *formatstr, double value, const char *checkstr1,
                  const char *checkstr2)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((fwprintf(fp, formatstr, value)) < 0)
    {
        Fail("ERROR: fwprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }

    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%S\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n",
            value, formatstr, checkstr1, checkstr2, buf);
    }
    fclose(fp);
}
#define DoDoubleTest DoDoubleTest_fwprintf

inline void DoArgumentPrecTest_fwprintf(const WCHAR *formatstr, int precision, void *param,
                        char *paramstr, const char *checkstr1, const char *checkstr2)
{
    FILE *fp;
    char buf[256];

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((fwprintf(fp, formatstr, precision, param)) < 0)
    {
        Fail("ERROR: fwprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }

    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", paramstr, formatstr,
            precision, checkstr1, checkstr2, buf);
    }


    if ((fclose( fp )) != 0)
    {
        Fail("ERROR: fclose failed to close \"testfile.txt\"\n");
    }

}
#define DoArgumentPrecTest DoArgumentPrecTest_fwprintf

inline void DoArgumentPrecDoubleTest_fwprintf(const WCHAR *formatstr, int precision, double param,
                              const char *checkstr1, const char *checkstr2)
{
    FILE *fp;
    char buf[256];

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((fwprintf(fp, formatstr, precision, param)) < 0)
    {
        Fail("ERROR: fwprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }

    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", param, formatstr,
            precision, checkstr1, checkstr2, buf);
    }

    if ((fclose( fp )) != 0)
    {
        Fail("ERROR: fclose failed to close \"testfile.txt\"\n");
    }

}
#define DoArgumentPrecDoubleTest DoArgumentPrecDoubleTest_fwprintf
#endif
