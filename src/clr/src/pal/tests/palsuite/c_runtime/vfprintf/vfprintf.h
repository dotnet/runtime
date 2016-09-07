// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

int DoVfprintf(FILE *fp, const char *format, ...)
{
    int retVal;
    va_list arglist;

    va_start(arglist, format);
    retVal = vfprintf(fp, format, arglist);
    va_end(arglist);

    return (retVal);
}

void DoStrTest(const char *formatstr, char* param, const char *checkstr)
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

void DoWStrTest(const char *formatstr, WCHAR* param, const char *checkstr)
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


void DoPointerTest(const char *formatstr, void* param, char* paramstr, 
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



void DoCountTest(const char *formatstr, int param, const char *checkstr)
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

void DoShortCountTest(const char *formatstr, int param, const char *checkstr)
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


void DoCharTest(const char *formatstr, char param, const char *checkstr)
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

void DoWCharTest(const char *formatstr, WCHAR param, const char *checkstr)
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

void DoNumTest(const char *formatstr, int value, const char *checkstr)
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

void DoI64Test(const char *formatstr, INT64 value, char *valuestr, const char *checkstr1)
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

void DoDoubleTest(const char *formatstr, double value, const char *checkstr1,
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


void DoArgumentPrecTest(const char *formatstr, int precision, void *param, 
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

void DoArgumentPrecDoubleTest(const char *formatstr, int precision, double param, 
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

#endif

