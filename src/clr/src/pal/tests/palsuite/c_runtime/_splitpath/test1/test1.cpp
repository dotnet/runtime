// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Passes _splitpath() a series of sample paths and checks that it
**          parses them as expected.
**
**
**==========================================================================*/


#include <palsuite.h>

struct testCase
{
    const char path[_MAX_PATH];    /* The path to parse. */
    const char drive[_MAX_DRIVE];  /* The expected values... */
    const char dir[_MAX_DIR];
    const char fname[_MAX_FNAME];
    const char ext[_MAX_EXT];
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
    char drive[_MAX_DRIVE];
    char dir[_MAX_DIR];
    char fname[_MAX_FNAME];
    char ext[_MAX_EXT];

    int i=0;

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    for (i = 0; i < sizeof(testCases)/sizeof(struct testCase); i++)
    {
        _splitpath(testCases[i].path, drive, dir, fname, ext);


        /*on platforms that don't support drive letters, the drive
          returned should always be "" */
        if (strcmp(drive, testCases[i].drive) != 0)
        {
            Fail("_splitpath read the path \"%s\" and thought the drive was "
                   "\"%s\" instead of \"%s\"\n"
                   , testCases[i].path, drive, testCases[i].drive);
        }

        if (strcmp(dir, testCases[i].dir) != 0)
        {
            Fail("_splitpath read the path \"%s\" and thought the directory "
                   "was \"%s\" instead of \"%s\"\n"
                   , testCases[i].path, dir, testCases[i].dir);
        }

        if (strcmp(fname, testCases[i].fname) != 0)
        {
            Fail("_splitpath read the path \"%s\" and thought the filename "
                   "was \"%s\" instead of \"%s\"\n"
                   , testCases[i].path, fname, testCases[i].fname);
        }

        if (strcmp(ext, testCases[i].ext) != 0)
        {
            Fail("_splitpath read the path \"%s\" and thought the file "
                   "extension was \"%s\" instead of \"%s\"\n"
                   , testCases[i].path, ext, testCases[i].ext);
        }
    }
    PAL_Terminate();
    return PASS;
}






