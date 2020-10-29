// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      vfprintf.h
**
** Purpose:     Contains common testing functions for vfprintf
**
**
**==========================================================================*/

#ifndef __vfprintf_H__
#define __vfprintf_H__

inline int DoVfprintf(FILE *fp, const char *format, ...)
{
    int retVal;
    va_list arglist;

    va_start(arglist, format);
    retVal = vfprintf(fp, format, arglist);
    va_end(arglist);

    return (retVal);
}

inline void DoStrTest_vfprintf(const char *formatstr, char* param, const char *checkstr)
{
    FILE *fp;    
    char buf[256] = { 0 };
    
    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }
    if ((DoVfprintf(fp, formatstr, param)) < 0)
    {
        Fail("ERROR: vfprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
    }

    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert string \"%s\" into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", 
            param, formatstr, checkstr, buf);
    }    
    fclose(fp);
}
#define DoStrTest DoStrTest_vfprintf

inline void DoWStrTest_vfprintf(const char *formatstr, WCHAR* param, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }
    if ((DoVfprintf(fp, formatstr, param)) < 0)
    {
        Fail("ERROR: vfprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
    }

    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%S\" into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", 
            param, formatstr, checkstr, buf);
    }    
    fclose(fp);
}
#define DoWStrTest DoWStrTest_vfprintf

inline void DoPointerTest_vfprintf(const char *formatstr, void* param, char* paramstr, 
                   const char *checkstr1)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((DoVfprintf(fp, formatstr, param)) < 0)
    {
        Fail("ERROR: vfprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
    }
   
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", 
            paramstr, formatstr, checkstr1, buf);
    }    
    
    if ((fclose( fp )) != 0)
    {
        Fail("ERROR: fclose failed to close \"testfile.txt\"\n");
    }
}
#define DoPointerTest DoPointerTest_vfprintf

inline void DoCountTest_vfprintf(const char *formatstr, int param, const char *checkstr)
{
    FILE *fp;
    char buf[512] = { 0 };
    int n = -1;
    
    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((DoVfprintf(fp, formatstr, &n)) < 0)
    {
        Fail("ERROR: vfprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, sizeof(buf), fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
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
#define DoCountTest DoCountTest_vfprintf

inline void DoShortCountTest_vfprintf(const char *formatstr, int param, const char *checkstr)
{
    FILE *fp;
    char buf[512] = { 0 };
    short int n = -1;
    
    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((DoVfprintf(fp, formatstr, &n)) < 0)
    {
        Fail("ERROR: vfprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
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
#define DoShortCountTest DoShortCountTest_vfprintf

inline void DoCharTest_vfprintf(const char *formatstr, char param, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }
    if ((DoVfprintf(fp, formatstr, param)) < 0)
    {
        Fail("ERROR: vfprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
    }

    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", 
            param, param, formatstr, checkstr, buf);
    }    
    fclose(fp);
}
#define DoCharTest DoCharTest_vfprintf

inline void DoWCharTest_vfprintf(const char *formatstr, WCHAR param, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }
    if ((DoVfprintf(fp, formatstr, param)) < 0)
    {
        Fail("ERROR: vfprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
    }
    
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", 
            (char)param, param, formatstr, checkstr, buf);
    }    
    fclose(fp);
}
#define DoWCharTest DoWCharTest_vfprintf

inline void DoNumTest_vfprintf(const char *formatstr, int value, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }
    if ((DoVfprintf(fp, formatstr, value)) < 0)
    {
        Fail("ERROR: vfprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
    }

    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert %#x into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", 
            value, formatstr, checkstr, buf);
    }    
    fclose(fp);
}
#define DoNumTest DoNumTest_vfprintf

inline void DoI64Test_vfprintf(const char *formatstr, INT64 value, char *valuestr, const char *checkstr1)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }
    if ((DoVfprintf(fp, formatstr, value)) < 0)
    {
        Fail("ERROR: vfprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
    }

    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\", got \"%s\".\n", 
            valuestr, formatstr, checkstr1, buf);
    }    
    fclose(fp);
}
#define DoI64Test DoI64Test_vfprintf

inline void DoDoubleTest_vfprintf(const char *formatstr, double value, const char *checkstr1,
                  const char *checkstr2)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((DoVfprintf(fp, formatstr, value)) < 0)
    {
        Fail("ERROR: vfprintf failed\n");
    }
    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
    }

    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", 
            value, formatstr, checkstr1, checkstr2, buf);
    }    
    fclose(fp);
}
#define DoDoubleTest DoDoubleTest_vfprintf

inline void DoArgumentPrecTest_vfprintf(const char *formatstr, int precision, void *param, 
                        char *paramstr, const char *checkstr1, const char *checkstr2)
{
    FILE *fp;
    char buf[256];

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((DoVfprintf(fp, formatstr, precision, param)) < 0)
    {
        Fail("ERROR: vfprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
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
#define DoArgumentPrecTest DoArgumentPrecTest_vfprintf

inline void DoArgumentPrecDoubleTest_vfprintf(const char *formatstr, int precision, double param, 
                              const char *checkstr1, const char *checkstr2)
{
    FILE *fp;
    char buf[256];

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((DoVfprintf(fp, formatstr, precision, param)) < 0)
    {
        Fail("ERROR: vfprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
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
#define DoArgumentPrecDoubleTest DoArgumentPrecDoubleTest_vfprintf

#endif

