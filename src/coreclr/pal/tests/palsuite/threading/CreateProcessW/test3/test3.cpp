// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: CreateProcessW/test3/test3.cpp
**
** Purpose: Verify that CreateProcessW skips directories when searching
** $PATH for an executable, matching Windows CreateProcessW behavior.
**
** Two scenarios are tested:
**   1. Negative: only a directory named as the executable exists in PATH;
**      CreateProcessW must fail with ERROR_FILE_NOT_FOUND (not
**      ERROR_ACCESS_DENIED, which would indicate the directory was
**      treated as a match and PATH search stopped prematurely).
**   2. Positive: a directory named as the executable appears before
**      the real executable in PATH; CreateProcessW must skip the
**      directory, find the real executable, and launch it successfully.
**
**============================================================*/

#include <palsuite.h>
#include <unistd.h>

#define CHILD_EXE_NAME  "paltest_createprocessw_test3_child"
#define CHILD_TEST_NAME "threading/CreateProcessW/test3/paltest_createprocessw_test3_child"

static void CleanupTestDirs(const char *dir1, const char *blockerDir,
                             const char *dir2, const char *symlinkPath)
{
    if (symlinkPath != NULL && symlinkPath[0] != '\0')
        unlink(symlinkPath);
    if (blockerDir != NULL && blockerDir[0] != '\0')
        rmdir(blockerDir);
    if (dir1 != NULL && dir1[0] != '\0')
        rmdir(dir1);
    if (dir2 != NULL && dir2[0] != '\0')
        rmdir(dir2);
}

PALTEST(threading_CreateProcessW_test3_paltest_createprocessw_test3, "threading/CreateProcessW/test3/paltest_createprocessw_test3")
{
    STARTUPINFOW si;
    PROCESS_INFORMATION pi;
    DWORD dwExitCode;
    char tmpDir1[MAX_PATH];
    char tmpDir2[MAX_PATH];
    char blockerDir[MAX_PATH];
    char symlinkPath[MAX_PATH];
    char savedPath[32768];
    char newPath[MAX_PATH * 2 + 2];
    WCHAR cmdLineW[MAX_PATH + 256];
    char cmdLineA[MAX_PATH + 256];
    DWORD savedPathLen;
    BOOL createResult;

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Build unique temp directory names using the process ID */
    DWORD pid = GetCurrentProcessId();
    snprintf(tmpDir1, sizeof(tmpDir1), "/tmp/paltest_cpw_t3_%u_d1", pid);
    snprintf(tmpDir2, sizeof(tmpDir2), "/tmp/paltest_cpw_t3_%u_d2", pid);
    blockerDir[0] = '\0';
    symlinkPath[0] = '\0';

    if (!CreateDirectoryA(tmpDir1, NULL))
    {
        Fail("CreateDirectoryA(%s) failed with error %u\n", tmpDir1, GetLastError());
    }

    if (!CreateDirectoryA(tmpDir2, NULL))
    {
        rmdir(tmpDir1);
        Fail("CreateDirectoryA(%s) failed with error %u\n", tmpDir2, GetLastError());
    }

    /* Create a directory named CHILD_EXE_NAME inside tmpDir1 (the blocker) */
    snprintf(blockerDir, sizeof(blockerDir), "%s/" CHILD_EXE_NAME, tmpDir1);
    if (!CreateDirectoryA(blockerDir, NULL))
    {
        CleanupTestDirs(tmpDir1, NULL, tmpDir2, NULL);
        Fail("CreateDirectoryA(%s) failed with error %u\n", blockerDir, GetLastError());
    }

    /* Create a symlink named CHILD_EXE_NAME inside tmpDir2 pointing to paltests */
    snprintf(symlinkPath, sizeof(symlinkPath), "%s/" CHILD_EXE_NAME, tmpDir2);
    if (symlink(argv[0], symlinkPath) != 0)
    {
        CleanupTestDirs(tmpDir1, blockerDir, tmpDir2, NULL);
        Fail("symlink(%s -> %s) failed with errno %d\n", symlinkPath, argv[0], errno);
    }

    /* Save the current PATH */
    savedPathLen = GetEnvironmentVariableA("PATH", savedPath, sizeof(savedPath));
    if (savedPathLen == 0 && GetLastError() != ERROR_ENVVAR_NOT_FOUND)
    {
        CleanupTestDirs(tmpDir1, blockerDir, tmpDir2, symlinkPath);
        Fail("GetEnvironmentVariableA(PATH) failed with error %u\n", GetLastError());
    }
    if (savedPathLen >= sizeof(savedPath))
    {
        CleanupTestDirs(tmpDir1, blockerDir, tmpDir2, symlinkPath);
        Fail("PATH environment variable is too long to save\n");
    }

    /* Build the command line (wide): CHILD_EXE_NAME CHILD_TEST_NAME */
    snprintf(cmdLineA, sizeof(cmdLineA), CHILD_EXE_NAME " " CHILD_TEST_NAME);
    MultiByteToWideChar(CP_ACP, 0, cmdLineA, -1, cmdLineW, (int)(sizeof(cmdLineW) / sizeof(WCHAR)));

    /*
     * Negative test: set PATH to contain only tmpDir1 (which has a directory
     * named CHILD_EXE_NAME but no real executable).  CreateProcessW must fail
     * with ERROR_FILE_NOT_FOUND, not ERROR_ACCESS_DENIED.
     */
    if (!SetEnvironmentVariableA("PATH", tmpDir1))
    {
        CleanupTestDirs(tmpDir1, blockerDir, tmpDir2, symlinkPath);
        Fail("SetEnvironmentVariableA failed with error %u\n", GetLastError());
    }

    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));

    createResult = CreateProcessW(NULL, cmdLineW, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);
    if (createResult)
    {
        /* Unexpected success: a directory should not be treated as an executable */
        WaitForSingleObject(pi.hProcess, 30000);
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        SetEnvironmentVariableA("PATH", savedPath);
        CleanupTestDirs(tmpDir1, blockerDir, tmpDir2, symlinkPath);
        Fail("CreateProcessW unexpectedly succeeded when PATH contains only a "
             "directory named " CHILD_EXE_NAME "\n");
    }
    else if (GetLastError() != ERROR_FILE_NOT_FOUND)
    {
        DWORD err = GetLastError();
        SetEnvironmentVariableA("PATH", savedPath);
        CleanupTestDirs(tmpDir1, blockerDir, tmpDir2, symlinkPath);
        Fail("CreateProcessW failed with error %u; expected ERROR_FILE_NOT_FOUND (%u). "
             "A directory in PATH should be skipped, not treated as a match.\n",
             err, ERROR_FILE_NOT_FOUND);
    }

    /*
     * Positive test: set PATH to tmpDir1:tmpDir2.  tmpDir1 contains a directory
     * named CHILD_EXE_NAME; tmpDir2 contains a symlink to the real paltests
     * binary with the same name.  CreateProcessW must skip the directory,
     * find the symlink, and launch the child successfully.
     */
    snprintf(newPath, sizeof(newPath), "%s:%s", tmpDir1, tmpDir2);
    if (!SetEnvironmentVariableA("PATH", newPath))
    {
        SetEnvironmentVariableA("PATH", savedPath);
        CleanupTestDirs(tmpDir1, blockerDir, tmpDir2, symlinkPath);
        Fail("SetEnvironmentVariableA failed with error %u\n", GetLastError());
    }

    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));

    createResult = CreateProcessW(NULL, cmdLineW, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);

    /* Restore PATH before any potential Fail() calls */
    SetEnvironmentVariableA("PATH", savedPath);

    if (!createResult)
    {
        CleanupTestDirs(tmpDir1, blockerDir, tmpDir2, symlinkPath);
        Fail("CreateProcessW failed with error %u when a real executable follows "
             "a directory of the same name in PATH\n", GetLastError());
    }

    WaitForSingleObject(pi.hProcess, 30000);

    if (!GetExitCodeProcess(pi.hProcess, &dwExitCode))
    {
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        CleanupTestDirs(tmpDir1, blockerDir, tmpDir2, symlinkPath);
        Fail("GetExitCodeProcess failed with error %u\n", GetLastError());
    }

    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    CleanupTestDirs(tmpDir1, blockerDir, tmpDir2, symlinkPath);

    if (dwExitCode != PASS)
    {
        Fail("Child process exited with code %u; expected PASS (%u)\n", dwExitCode, PASS);
    }

    PAL_Terminate();
    return PASS;
}
