// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:      fwprintf.h
**
** Purpose:     Containts common testing functions for fwprintf
**
**
**==========================================================================*/

#ifndef __fwprintf_H__
#define __fwprintf_H__

void DoStrTest(WCHAR *formatstr, char* param, char *checkstr)
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

void DoWStrTest(WCHAR *formatstr, WCHAR* param, char *checkstr)
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


void DoPointerTest(WCHAR *formatstr, void* param, char* paramstr, 
                   char *checkstr1, char *checkstr2)
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



void DoCountTest(WCHAR *formatstr, int param, char *checkstr)
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

void DoShortCountTest(WCHAR *formatstr, int param, char *checkstr)
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


void DoCharTest(WCHAR *formatstr, char param, char *checkstr)
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

void DoWCharTest(WCHAR *formatstr, WCHAR param, char *checkstr)
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

void DoNumTest(WCHAR *formatstr, int value, char *checkstr)
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

void DoI64Test(WCHAR *formatstr, INT64 value, char *valuestr, char *checkstr1,
               char *checkstr2)
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

void DoDoubleTest(WCHAR *formatstr, double value, char *checkstr1,
                  char *checkstr2)
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


void DoArgumentPrecTest(WCHAR *formatstr, int precision, void *param, 
                        char *paramstr, char *checkstr1, char *checkstr2)
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

void DoArgumentPrecDoubleTest(WCHAR *formatstr, int precision, double param, 
                              char *checkstr1, char *checkstr2)
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

#endif
