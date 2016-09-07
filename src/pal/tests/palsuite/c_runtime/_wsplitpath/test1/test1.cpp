// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Passes _wsplitpath() a series of sample paths and checks
**          that it parses them as expected.
**
**
**==========================================================================*/


#include <palsuite.h>

struct testCase
{
    char path[_MAX_PATH];    /* The path to parse. */
    char drive[_MAX_DRIVE];  /* The expected values... */
    char dir[_MAX_DIR];
    char fname[_MAX_FNAME];
    char ext[_MAX_EXT];
};

struct wTestCase
{
    WCHAR *path;    /* The path to parse. */
    WCHAR *drive;  /* The expected values... */
    WCHAR *dir;
    WCHAR *fname;
    WCHAR *ext;
};



int __cdecl main(int argc, char **argv)
{
    struct testCase testCases[] = 
    {
#if WIN32 
        {"c:\\foo\\bar\\foo.bar", "c:", "\\foo\\bar\\", "foo", ".bar"},
        {"c:/foo/bar/foo.bar", "c:", "/foo/bar/", "foo", ".bar"},
        {"c:/foo/bar/foo", "c:", "/foo/bar/", "foo", ""},
        {"c:/foo/bar/.bar", "c:", "/foo/bar/", "", ".bar"},
        {"c:/foo/bar/", "c:", "/foo/bar/", "", ""},
        {"/foo/bar/foo.bar", "", "/foo/bar/", "foo", ".bar"},
        {"c:foo.bar", "c:", "", "foo", ".bar"}
#else
        {"c:\\foo\\bar\\foo.bar", "","c:/foo/bar/", "foo", ".bar"},
        {"c:/foo/bar/foo.bar", "", "c:/foo/bar/", "foo", ".bar"},
        {"c:/foo/bar/foo", "", "c:/foo/bar/", "foo", ""},
        {"c:/foo/bar/.bar", "", "c:/foo/bar/", ".bar", ""},
        {"c:/foo/bar/", "", "c:/foo/bar/", "", ""},
        {"/foo/bar/foo.bar", "", "/foo/bar/", "foo", ".bar"},
        {"c:foo.bar", "", "", "c:foo", ".bar"}
#endif
    };  
  
    struct wTestCase wTestCases[sizeof(testCases)/sizeof(struct testCase)];
  
    wchar_t wDrive[_MAX_DRIVE];
    wchar_t wDir[_MAX_DIR];
    wchar_t wFname[_MAX_FNAME];
    wchar_t wExt[_MAX_EXT];

    char *drive;
    char *dir;
    char *fname;
    char *ext;

    int i;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /*create wide character versions of the test cases*/
    for(i = 0; i < sizeof(testCases)/sizeof(struct testCase); i ++) 
    {
        wTestCases[i].path = convert(testCases[i].path);
        wTestCases[i].drive = convert(testCases[i].drive);
        wTestCases[i].dir = convert(testCases[i].dir);
        wTestCases[i].fname = convert(testCases[i].fname);
        wTestCases[i].ext = convert(testCases[i].ext);
    }
          

    for (i = 0; i < sizeof(wTestCases)/sizeof(struct wTestCase); i++) 
    { 
        _wsplitpath(wTestCases[i].path, wDrive, wDir, wFname, wExt);

        /*Convert the results to regular ANSI strings.*/
        drive = convertC(wDrive);
        dir = convertC(wDir);
        fname = convertC(wFname);
        ext = convertC(wExt);

                
        /*on platforms that don't support drive letters, the drive
          returned should always be "" */
        if (wcscmp(wDrive, wTestCases[i].drive) != 0)
        {
            Fail("_wsplitpath read the path \"%s\" and thought the drive was "
                 "\"%s\" instead of \"%s\""
                 , testCases[i].path, drive, testCases[i].drive);
        }

        if (wcscmp(wDir, wTestCases[i].dir) != 0)
        {
            Fail("_wsplitpath read the path \"%s\" and thought the directory "
                 "was \"%s\" instead of \"%s\""
                 , testCases[i].path, dir, testCases[i].dir);
        }

        if (wcscmp(wFname, wTestCases[i].fname) != 0) 
        {
            Fail("_wsplitpath read the path \"%s\" and thought the filename "
                 "was \"%s\" instead of \"%s\""
                 , testCases[i].path, fname, testCases[i].fname);
        }

        if (wcscmp(wExt, wTestCases[i].ext) != 0)
        {
            Fail("_wsplitpath read the path \"%s\" and thought the file "
                 "extension was \"%s\" instead of \"%s\""
                 , testCases[i].path, ext, testCases[i].ext);
        }

        free(drive);
        free(dir);
        free(fname);
        free(ext);
    }

    for(i = 0; i < sizeof(testCases)/sizeof(struct testCase); i++) 
    {
        free(wTestCases[i].path);
        free(wTestCases[i].drive);
        free(wTestCases[i].dir);
        free(wTestCases[i].fname);
        free(wTestCases[i].ext);
    }

    PAL_Terminate();

    return PASS;
}
