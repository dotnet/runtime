// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*NOTE:
The creation of the test file within each function is because the FILE 
structure is not defined within pal.h. Therefore, unable to have
function with this as a return type.
*/

#ifndef __FPRINTF_H__
#define __FPRINTF_H__

inline void DoStrTest_fprintf(const char *formatstr, char* param, const char *checkstr)
{
    FILE *fp;    
    char buf[256] = { 0 };
    
    if ((fp = fopen("testfile.txt", "w+")) == NULL )
        Fail("ERROR: fopen failed to create testfile\n");
    if ((fprintf(fp, formatstr, param)) < 0)
        Fail("ERROR: fprintf failed\n");
    if ((fseek(fp, 0, SEEK_SET)) != 0)
        Fail("ERROR: fseek failed\n");
    if ((fgets(buf, 100, fp)) == NULL)
        Fail("ERROR: fseek failed\n");

    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert string \"%s\" into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", 
            param, formatstr, checkstr, buf);
    }    
    fclose(fp);
}
#define DoStrTest DoStrTest_fprintf

inline void DoWStrTest_fprintf(const char *formatstr, WCHAR* param, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
        Fail("ERROR: fopen failed to create testfile\n");
    if ((fprintf(fp, formatstr, param)) < 0)
        Fail("ERROR: fprintf failed\n");
    if ((fseek(fp, 0, SEEK_SET)) != 0)
        Fail("ERROR: fseek failed\n");
    if ((fgets(buf, 100, fp)) == NULL)
        Fail("ERROR: fseek failed\n");

    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", 
            convertC(param), formatstr, checkstr, buf);
    }    
    fclose(fp);
}
#define DoWStrTest DoWStrTest_fprintf

inline void DoCharTest_fprintf(const char *formatstr, char param, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
        Fail("ERROR: fopen failed to create testfile\n");
    if ((fprintf(fp, formatstr, param)) < 0)
        Fail("ERROR: fprintf failed\n");
    if ((fseek(fp, 0, SEEK_SET)) != 0)
        Fail("ERROR: fseek failed\n");
    if ((fgets(buf, 100, fp)) == NULL)
        Fail("ERROR: fseek failed\n");

    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", 
            param, param, formatstr, checkstr, buf);
    }    
    fclose(fp);
}
#define DoCharTest DoCharTest_fprintf

inline void DoWCharTest_fprintf(const char *formatstr, WCHAR param, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
        Fail("ERROR: fopen failed to create testfile\n");
    if ((fprintf(fp, formatstr, param)) < 0)
        Fail("ERROR: fprintf failed\n");
    if ((fseek(fp, 0, SEEK_SET)) != 0)
        Fail("ERROR: fseek failed\n");
    if ((fgets(buf, 100, fp)) == NULL)
        Fail("ERROR: fseek failed\n");
    
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", 
            (char)param, param, formatstr, checkstr, buf);
    }    
    fclose(fp);
}
#define DoWCharTest DoWCharTest_fprintf

inline void DoNumTest_fprintf(const char *formatstr, int value, const char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
        Fail("ERROR: fopen failed to create testfile\n");
    if ((fprintf(fp, formatstr, value)) < 0)
        Fail("ERROR: fprintf failed\n");
    if ((fseek(fp, 0, SEEK_SET)) != 0)
        Fail("ERROR: fseek failed\n");
    if ((fgets(buf, 100, fp)) == NULL)
        Fail("ERROR: fseek failed\n");

    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert %#x into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", 
            value, formatstr, checkstr, buf);
    }    
    fclose(fp);
}
#define DoNumTest DoNumTest_fprintf

inline void DoI64Test_fprintf(const char *formatstr, INT64 value, char *valuestr, const char *checkstr1, const char *checkstr2)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
        Fail("ERROR: fopen failed to create testfile\n");
    if ((fprintf(fp, formatstr, value)) < 0)
        Fail("ERROR: fprintf failed\n");
    if ((fseek(fp, 0, SEEK_SET)) != 0)
        Fail("ERROR: fseek failed\n");
    if ((fgets(buf, 100, fp)) == NULL)
        Fail("ERROR: fseek failed\n");

    if (memcmp(buf, checkstr1, strlen(buf) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", 
            valuestr, formatstr, checkstr1, checkstr2, buf);
    }    
    fclose(fp);
}
#define DoI64Test DoI64Test_fprintf

inline void DoDoubleTest_fprintf(const char *formatstr, double value, const char *checkstr1, const char *checkstr2)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
        Fail("ERROR: fopen failed to create testfile\n");
    if ((fprintf(fp, formatstr, value)) < 0)
        Fail("ERROR: fprintf failed\n");
    if ((fseek(fp, 0, SEEK_SET)) != 0)
        Fail("ERROR: fseek failed\n");
    if ((fgets(buf, 100, fp)) == NULL)
        Fail("ERROR: fseek failed\n");

    if (memcmp(buf, checkstr1, strlen(buf) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", 
            value, formatstr, checkstr1, checkstr2, buf);
    }    
    fclose(fp);
}
#define DoDoubleTest DoDoubleTest_fprintf
#endif
