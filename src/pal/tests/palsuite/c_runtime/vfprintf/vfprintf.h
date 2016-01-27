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

int DoVfprintf(FILE *fp, char *format, ...)
{
    int retVal;
    va_list arglist;

    va_start(arglist, format);
    retVal = vfprintf(fp, format, arglist);
    va_end(arglist);

    return (retVal);
}

void DoStrTest(char *formatstr, char* param, char *checkstr)
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

void DoWStrTest(char *formatstr, WCHAR* param, char *checkstr)
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


void DoPointerTest(char *formatstr, void* param, char* paramstr, 
                   char *checkstr1)
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



void DoCountTest(char *formatstr, int param, char *checkstr)
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

void DoShortCountTest(char *formatstr, int param, char *checkstr)
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


void DoCharTest(char *formatstr, char param, char *checkstr)
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

void DoWCharTest(char *formatstr, WCHAR param, char *checkstr)
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

void DoNumTest(char *formatstr, int value, char *checkstr)
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

void DoI64Test(char *formatstr, INT64 value, char *valuestr, char *checkstr1)
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

void DoDoubleTest(char *formatstr, double value, char *checkstr1,
                  char *checkstr2)
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


void DoArgumentPrecTest(char *formatstr, int precision, void *param, 
                        char *paramstr, char *checkstr1, char *checkstr2)
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

void DoArgumentPrecDoubleTest(char *formatstr, int precision, double param, 
                              char *checkstr1, char *checkstr2)
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

