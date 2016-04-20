// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// These test cases test named mutexes, including positive
// and negative cases, cross - thread and cross - process, mutual
// exclusion, abandon detection, etc.

#include <palsuite.h>

#ifndef _countof
#define _countof(a) (sizeof(a) / sizeof(a[0]))
#endif // !_countof

const char *const SessionPrefix = "Local\\";
const char *const GlobalPrefix = "Global\\";

const char *const NamePrefix = "paltest_namedmutex_test1_";
const char *const TempNamePrefix = "paltest_namedmutex_test1_temp_";
const char *const InvalidNamePrefix0 = "paltest\\namedmutex_";
const char *const InvalidNamePrefix1 = "paltest/namedmutex_";
const char *const ParentEventNamePrefix0 = "paltest_namedmutex_test1_pe0_";
const char *const ParentEventNamePrefix1 = "paltest_namedmutex_test1_pe1_";
const char *const ChildEventNamePrefix0 = "paltest_namedmutex_test1_ce0_";
const char *const ChildEventNamePrefix1 = "paltest_namedmutex_test1_ce1_";

const char *const GlobalShmFilePathPrefix = "/tmp/.dotnet/shm/global/";

#define MaxPathSize (200)
const DWORD PollLoopSleepMilliseconds = 100;
const DWORD FailTimeoutMilliseconds = 30000;

bool isParent;
char processPath[4096], processCommandLinePath[4096];
DWORD parentPid = static_cast<DWORD>(-1);

extern char *(*test_strcpy)(char *dest, const char *src);
extern int (*test_strcmp)(const char *s1, const char *s2);
extern size_t (*test_strlen)(const char *s);
extern int (*test_sprintf)(char *str, const char *format, ...);
extern int (*test_sscanf)(const char *str, const char *format, ...);
extern int(*test_close)(int fd);
extern int (*test_unlink)(const char *pathname);
extern unsigned int test_getpid();
extern int test_kill(unsigned int pid);

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Test helpers

extern bool TestFileExists(const char *path);
extern bool WriteHeaderInfo(const char *path, char sharedMemoryType, char version, int *fdRef);

#define TestAssert(expression) \
    do \
    { \
        if (!(expression)) \
        { \
            if (!isParent) \
            { \
                Trace("Child process: "); \
            } \
            Trace("'paltest_namedmutex_test1' failed at line %u. Expression: " #expression "\n", __LINE__); \
            fflush(stdout); \
            return false; \
        } \
    } while(false)

char *BuildName(char *buffer, const char *prefix0 = nullptr, const char *prefix1 = nullptr)
{
    size_t nameLength = 0;
    const char *prefixes[] = {prefix0, prefix1};
    for (int i = 0; i < 2; ++i)
    {
        const char *prefix = prefixes[i];
        if (prefix == nullptr)
        {
            break;
        }
        test_strcpy(&buffer[nameLength], prefix);
        nameLength += test_strlen(prefix);
    }
    test_sprintf(&buffer[nameLength], "%u", parentPid);
    return buffer;
}

char *BuildGlobalShmFilePath(char *buffer, const char *namePrefix)
{
    size_t pathLength = 0;
    test_strcpy(&buffer[pathLength], GlobalShmFilePathPrefix);
    pathLength += test_strlen(GlobalShmFilePathPrefix);
    test_strcpy(&buffer[pathLength], namePrefix);
    pathLength += test_strlen(namePrefix);
    test_sprintf(&buffer[pathLength], "%u", parentPid);
    return buffer;
}

class AutoCloseMutexHandle
{
private:
    HANDLE m_handle;

public:
    AutoCloseMutexHandle(HANDLE handle = nullptr) : m_handle(handle)
    {
    }

    ~AutoCloseMutexHandle()
    {
        Close();
    }

public:
    HANDLE GetHandle() const
    {
        return m_handle;
    }

    bool Release()
    {
        return !!ReleaseMutex(m_handle);
    }

    void Close()
    {
        if (m_handle != nullptr)
        {
            CloseHandle(m_handle);
            m_handle = nullptr;
        }
    }

    void Abandon()
    {
        // Don't close the handle
        m_handle = nullptr;
    }

    AutoCloseMutexHandle &operator =(HANDLE handle)
    {
        Close();
        m_handle = handle;
        return *this;
    }

    operator HANDLE() const
    {
        return m_handle;
    }

private:
    AutoCloseMutexHandle(const AutoCloseMutexHandle &other);
    AutoCloseMutexHandle(AutoCloseMutexHandle &&other);
    AutoCloseMutexHandle &operator =(const AutoCloseMutexHandle &other);
};

void TestCreateMutex(AutoCloseMutexHandle &m, const char *name, bool initiallyOwned = false)
{
    m.Close();
    m = CreateMutexA(nullptr, initiallyOwned, name);
}

HANDLE TestOpenMutex(const char *name)
{
    return OpenMutexA(SYNCHRONIZE, false, name);
}

bool StartProcess(const char *funcName)
{
    size_t processCommandLinePathLength = 0;
    processCommandLinePath[processCommandLinePathLength++] = '\"';
    test_strcpy(&processCommandLinePath[processCommandLinePathLength], processPath);
    processCommandLinePathLength += test_strlen(processPath);
    processCommandLinePath[processCommandLinePathLength++] = '\"';
    processCommandLinePath[processCommandLinePathLength++] = ' ';
    processCommandLinePathLength += test_sprintf(&processCommandLinePath[processCommandLinePathLength], "%u", parentPid);
    processCommandLinePath[processCommandLinePathLength++] = ' ';
    test_strcpy(&processCommandLinePath[processCommandLinePathLength], funcName);
    processCommandLinePathLength += test_strlen(funcName);

    STARTUPINFO si;
    memset(&si, 0, sizeof(si));
    si.cb = sizeof(si);
    PROCESS_INFORMATION pi;
    memset(&pi, 0, sizeof(pi));
    return !!CreateProcessA(nullptr, processCommandLinePath, nullptr, nullptr, false, 0, nullptr, nullptr, &si, &pi);
}

bool StartThread(LPTHREAD_START_ROUTINE func)
{
    DWORD threadId;
    HANDLE handle = CreateThread(nullptr, 0, func, nullptr, 0, &threadId);
    if (handle != nullptr)
    {
        CloseHandle(handle);
        return true;
    }
    return false;
}

bool WaitForMutexToBeCreated(AutoCloseMutexHandle &m, const char *eventNamePrefix)
{
    char eventName[MaxPathSize];
    BuildName(eventName, GlobalPrefix, eventNamePrefix);
    DWORD startTime = GetTickCount();
    while (true)
    {
        m = TestOpenMutex(eventName);
        if (m != nullptr)
        {
            return true;
        }
        if (GetTickCount() - startTime >= FailTimeoutMilliseconds)
        {
            return false;
        }
        Sleep(PollLoopSleepMilliseconds);
    }
}

// The following functions are used for parent/child tests, where the child runs in a separate thread or process. The tests are
// organized such that one the parent or child is ever running code, and they yield control and wait for the other. Since the
// named mutex is the only type of cross-process sync object available, they are used as events to synchronize. The parent and
// child have a pair of event mutexes each, which they own initially. To release the other waiting thread/process, the
// thread/process releases one of its mutexes, which the other thread/process would be waiting on. To wait, the thread/process
// waits on one of the other thread/process' mutexes. All the while, they ping-pong between the two mutexes. YieldToChild() and
// YieldToParent() below control the releasing, waiting, and ping-ponging, to help create a deterministic path through the
// parent and child tests while both are running concurrently.

bool InitialWaitForParent(AutoCloseMutexHandle parentEvents[2])
{
    TestAssert(WaitForSingleObject(parentEvents[0], FailTimeoutMilliseconds) == WAIT_OBJECT_0);
    TestAssert(parentEvents[0].Release());
    return true;
}

bool YieldToChild(AutoCloseMutexHandle parentEvents[2], AutoCloseMutexHandle childEvents[2], int &ei)
{
    TestAssert(parentEvents[ei].Release());
    TestAssert(WaitForSingleObject(childEvents[ei], FailTimeoutMilliseconds) == WAIT_OBJECT_0);
    TestAssert(childEvents[ei].Release());
    TestAssert(WaitForSingleObject(parentEvents[ei], 0) == WAIT_OBJECT_0);
    ei = 1 - ei;
    return true;
}

bool YieldToParent(AutoCloseMutexHandle parentEvents[2], AutoCloseMutexHandle childEvents[2], int &ei)
{
    TestAssert(childEvents[ei].Release());
    ei = 1 - ei;
    TestAssert(WaitForSingleObject(parentEvents[ei], FailTimeoutMilliseconds) == WAIT_OBJECT_0);
    TestAssert(parentEvents[ei].Release());
    TestAssert(WaitForSingleObject(childEvents[1 - ei], 0) == WAIT_OBJECT_0);
    return true;
}

bool FinalWaitForChild(AutoCloseMutexHandle childEvents[2], int ei)
{
    TestAssert(WaitForSingleObject(childEvents[ei], FailTimeoutMilliseconds) == WAIT_OBJECT_0);
    TestAssert(childEvents[ei].Release());
    return true;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Tests

bool NameTests()
{
    AutoCloseMutexHandle m;
    char name[MaxPathSize];

    // Empty name
    TestCreateMutex(m, "");
    TestAssert(m != nullptr);

    // Normal name
    TestCreateMutex(m, BuildName(name, NamePrefix));
    TestAssert(m != nullptr);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(name, NamePrefix))) != nullptr);
    TestCreateMutex(m, BuildName(name, SessionPrefix, NamePrefix));
    TestAssert(m != nullptr);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(name, SessionPrefix, NamePrefix))) != nullptr);
    TestCreateMutex(m, BuildName(name, GlobalPrefix, NamePrefix));
    TestAssert(m != nullptr);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(name, GlobalPrefix, NamePrefix))) != nullptr);

    // Name too long. The maximum allowed length depends on the file system, so we're not checking for that.
    {
        char name[257];
        memset(name, 'a', _countof(name) - 1);
        name[_countof(name) - 1] = '\0';
        TestCreateMutex(m, name);
        TestAssert(m == nullptr);
        TestAssert(GetLastError() == ERROR_FILENAME_EXCED_RANGE);
        TestAssert(AutoCloseMutexHandle(TestOpenMutex(name)) == nullptr);
        TestAssert(GetLastError() == ERROR_FILENAME_EXCED_RANGE);
    }

    // Invalid characters in name
    TestCreateMutex(m, BuildName(name, InvalidNamePrefix0));
    TestAssert(m == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(name, InvalidNamePrefix0))) == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestCreateMutex(m, BuildName(name, InvalidNamePrefix1));
    TestAssert(m == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(name, InvalidNamePrefix1))) == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestCreateMutex(m, BuildName(name, SessionPrefix, InvalidNamePrefix0));
    TestAssert(m == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(name, SessionPrefix, InvalidNamePrefix0))) == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestCreateMutex(m, BuildName(name, GlobalPrefix, InvalidNamePrefix1));
    TestAssert(m == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(name, GlobalPrefix, InvalidNamePrefix1))) == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);

    // Creating a second reference to the same named mutex yields an error indicating that it was opened, not created
    {
        TestCreateMutex(m, BuildName(name, NamePrefix));
        TestAssert(m != nullptr);
        AutoCloseMutexHandle m2;
        TestCreateMutex(m2, BuildName(name, NamePrefix));
        TestAssert(m2 != nullptr);
        TestAssert(GetLastError() == ERROR_ALREADY_EXISTS);
    }

    return true;
}

bool HeaderMismatchTests()
{
    AutoCloseMutexHandle m, m2;
    char name[MaxPathSize];
    int fd;

    // Create and hold onto a mutex during this test to create the shared memory directory
    TestCreateMutex(m2, BuildName(name, GlobalPrefix, TempNamePrefix));

    // Unknown shared memory type
    TestAssert(WriteHeaderInfo(BuildGlobalShmFilePath(name, NamePrefix), -1, 0, &fd));
    TestCreateMutex(m, BuildName(name, GlobalPrefix, NamePrefix));
    TestAssert(m == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_HANDLE);
    TestAssert(test_close(fd) == 0);
    TestAssert(test_unlink(BuildGlobalShmFilePath(name, NamePrefix)) == 0);

    // Mismatched version
    TestAssert(WriteHeaderInfo(BuildGlobalShmFilePath(name, NamePrefix), 0, -1, &fd));
    TestCreateMutex(m, BuildName(name, GlobalPrefix, NamePrefix));
    TestAssert(m == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_HANDLE);
    TestAssert(test_close(fd) == 0);
    TestAssert(test_unlink(BuildGlobalShmFilePath(name, NamePrefix)) == 0);

    return true;
}

bool MutualExclusionTests_Parent()
{
    AutoCloseMutexHandle m, parentEvents[2], childEvents[2];
    char name[MaxPathSize];
    for (int i = 0; i < 2; ++i)
    {
        TestCreateMutex(
            parentEvents[i],
            BuildName(name, GlobalPrefix, i == 0 ? ParentEventNamePrefix0 : ParentEventNamePrefix1),
            true);
        TestAssert(parentEvents[i] != nullptr);
        TestAssert(GetLastError() != ERROR_ALREADY_EXISTS);
    }
    TestAssert(WaitForMutexToBeCreated(childEvents[0], ChildEventNamePrefix0));
    TestAssert(WaitForMutexToBeCreated(childEvents[1], ChildEventNamePrefix1));
    int ei = 0;

    TestCreateMutex(m, BuildName(name, GlobalPrefix, NamePrefix));
    TestAssert(m != nullptr);

    // Recursive locking with various timeouts
    TestAssert(WaitForSingleObject(m, 0) == WAIT_OBJECT_0);
    TestAssert(WaitForSingleObject(m, FailTimeoutMilliseconds) == WAIT_OBJECT_0);
    TestAssert(WaitForSingleObject(m, static_cast<DWORD>(-1)) == WAIT_OBJECT_0);
    TestAssert(m.Release());
    TestAssert(m.Release());
    TestAssert(m.Release());
    TestAssert(!m.Release()); // try to release the lock while nobody owns it, and verify recursive lock counting
    TestAssert(GetLastError() == ERROR_NOT_OWNER);

    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child takes the lock

    TestAssert(WaitForSingleObject(m, 0) == WAIT_TIMEOUT); // try to lock the mutex without waiting
    TestAssert(WaitForSingleObject(m, 500) == WAIT_TIMEOUT); // try to lock the mutex with a timeout
    TestAssert(!m.Release()); // try to release the lock while another thread owns it
    TestAssert(GetLastError() == ERROR_NOT_OWNER);

    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child releases the lock

    TestAssert(WaitForSingleObject(m, static_cast<DWORD>(-1)) == WAIT_OBJECT_0); // lock the mutex with no timeout and release
    TestAssert(m.Release());

    TestAssert(parentEvents[0].Release());
    TestAssert(parentEvents[1].Release());
    FinalWaitForChild(childEvents, ei);
    return true;
}

DWORD MutualExclusionTests_Child(void *arg = nullptr)
{
    AutoCloseMutexHandle childEvents[2];
    {
        AutoCloseMutexHandle m, parentEvents[2];
        char name[MaxPathSize];
        for (int i = 0; i < 2; ++i)
        {
            TestCreateMutex(
                childEvents[i],
                BuildName(name, GlobalPrefix, i == 0 ? ChildEventNamePrefix0 : ChildEventNamePrefix1),
                true);
            TestAssert(childEvents[i] != nullptr);
            TestAssert(GetLastError() != ERROR_ALREADY_EXISTS);
        }
        TestAssert(WaitForMutexToBeCreated(parentEvents[0], ParentEventNamePrefix0));
        TestAssert(WaitForMutexToBeCreated(parentEvents[1], ParentEventNamePrefix1));
        int ei = 0;

        InitialWaitForParent(parentEvents);
        TestCreateMutex(m, BuildName(name, GlobalPrefix, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(WaitForSingleObject(m, 0) == WAIT_OBJECT_0); // lock the mutex
        YieldToParent(parentEvents, childEvents, ei); // parent attempts to lock/release, and fails
        TestAssert(m.Release()); // release the lock
    }

    TestAssert(childEvents[0].Release());
    TestAssert(childEvents[1].Release());
    return 0;
}

bool MutualExclusionTests()
{
    {
        AutoCloseMutexHandle m;
        char name[MaxPathSize];

        // Releasing a lock that is not owned by any thread fails
        TestCreateMutex(m, BuildName(name, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(!m.Release());
        TestAssert(GetLastError() == ERROR_NOT_OWNER);

        // Acquire a lock during upon creation, and release
        TestCreateMutex(m, BuildName(name, NamePrefix), true);
        TestAssert(m != nullptr);
        TestAssert(m.Release());

        // Multi-waits including a named mutex are not supported
        AutoCloseMutexHandle m2;
        TestCreateMutex(m2, nullptr);
        TestAssert(m2 != nullptr);
        HANDLE waitHandles[] = {m2.GetHandle(), m.GetHandle()};
        TestAssert(
            WaitForMultipleObjects(
                _countof(waitHandles),
                waitHandles,
                false /* waitAll */,
                FailTimeoutMilliseconds) ==
            WAIT_FAILED);
        TestAssert(GetLastError() == ERROR_NOT_SUPPORTED);
        TestAssert(
            WaitForMultipleObjects(
                _countof(waitHandles),
                waitHandles,
                true /* waitAll */,
                FailTimeoutMilliseconds) ==
            WAIT_FAILED);
        TestAssert(GetLastError() == ERROR_NOT_SUPPORTED);
    }

    // When another thread or process owns the lock, this process should not be able to acquire a lock, and the converse
    TestAssert(StartThread(MutualExclusionTests_Child));
    TestAssert(MutualExclusionTests_Parent());
    TestAssert(StartProcess("MutualExclusionTests_Child"));
    TestAssert(MutualExclusionTests_Parent());

    return true;
}

bool LifetimeTests_Parent()
{
    AutoCloseMutexHandle m, parentEvents[2], childEvents[2];
    char name[MaxPathSize];
    for (int i = 0; i < 2; ++i)
    {
        TestCreateMutex(
            parentEvents[i],
            BuildName(name, GlobalPrefix, i == 0 ? ParentEventNamePrefix0 : ParentEventNamePrefix1),
            true);
        TestAssert(parentEvents[i] != nullptr);
        TestAssert(GetLastError() != ERROR_ALREADY_EXISTS);
    }
    TestAssert(WaitForMutexToBeCreated(childEvents[0], ChildEventNamePrefix0));
    TestAssert(WaitForMutexToBeCreated(childEvents[1], ChildEventNamePrefix1));
    int ei = 0;

    TestCreateMutex(m, BuildName(name, GlobalPrefix, NamePrefix)); // create first reference to mutex
    TestAssert(m != nullptr);
    TestAssert(TestFileExists(BuildGlobalShmFilePath(name, NamePrefix)));
    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child creates second reference to mutex using CreateMutex
    m.Close(); // close first reference
    TestAssert(TestFileExists(BuildGlobalShmFilePath(name, NamePrefix)));
    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child closes second reference
    TestAssert(!TestFileExists(BuildGlobalShmFilePath(name, NamePrefix)));

    TestCreateMutex(m, BuildName(name, GlobalPrefix, NamePrefix)); // create first reference to mutex
    TestAssert(m != nullptr);
    TestAssert(TestFileExists(BuildGlobalShmFilePath(name, NamePrefix)));
    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child creates second reference to mutex using OpenMutex
    m.Close(); // close first reference
    TestAssert(TestFileExists(BuildGlobalShmFilePath(name, NamePrefix)));
    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child closes second reference
    TestAssert(!TestFileExists(BuildGlobalShmFilePath(name, NamePrefix)));

    TestAssert(parentEvents[0].Release());
    TestAssert(parentEvents[1].Release());
    FinalWaitForChild(childEvents, ei);
    return true;
}

DWORD LifetimeTests_Child(void *arg = nullptr)
{
    AutoCloseMutexHandle childEvents[2];
    {
        AutoCloseMutexHandle m, parentEvents[2];
        char name[MaxPathSize];
        for (int i = 0; i < 2; ++i)
        {
            TestCreateMutex(
                childEvents[i],
                BuildName(name, GlobalPrefix, i == 0 ? ChildEventNamePrefix0 : ChildEventNamePrefix1),
                true);
            TestAssert(childEvents[i] != nullptr);
            TestAssert(GetLastError() != ERROR_ALREADY_EXISTS);
        }
        TestAssert(WaitForMutexToBeCreated(parentEvents[0], ParentEventNamePrefix0));
        TestAssert(WaitForMutexToBeCreated(parentEvents[1], ParentEventNamePrefix1));
        int ei = 0;

        InitialWaitForParent(parentEvents); // parent creates first reference to mutex
        TestCreateMutex(m, BuildName(name, GlobalPrefix, NamePrefix)); // create second reference to mutex using CreateMutex
        TestAssert(m != nullptr);
        TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent closes first reference
        m.Close(); // close second reference

        TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent verifies, and creates first reference to mutex again
        m = TestOpenMutex(BuildName(name, GlobalPrefix, NamePrefix)); // create second reference to mutex using OpenMutex
        TestAssert(m != nullptr);
        TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent closes first reference
        m.Close(); // close second reference

        TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent verifies
    }

    TestAssert(childEvents[0].Release());
    TestAssert(childEvents[1].Release());
    return 0;
}

bool LifetimeTests()
{
    {
        AutoCloseMutexHandle m;
        char name[MaxPathSize];

        // Shm file should be created and deleted
        TestCreateMutex(m, BuildName(name, GlobalPrefix, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(TestFileExists(BuildGlobalShmFilePath(name, NamePrefix)));
        m.Close();
        TestAssert(!TestFileExists(BuildGlobalShmFilePath(name, NamePrefix)));
    }

    // Shm file should not be deleted until last reference is released
    TestAssert(StartThread(LifetimeTests_Child));
    TestAssert(LifetimeTests_Parent());
    TestAssert(StartProcess("LifetimeTests_Child"));
    TestAssert(LifetimeTests_Parent());

    return true;
}

bool AbandonTests_Parent()
{
    AutoCloseMutexHandle m, parentEvents[2], childEvents[2];
    char name[MaxPathSize];
    for (int i = 0; i < 2; ++i)
    {
        TestCreateMutex(
            parentEvents[i],
            BuildName(name, GlobalPrefix, i == 0 ? ParentEventNamePrefix0 : ParentEventNamePrefix1),
            true);
        TestAssert(parentEvents[i] != nullptr);
        TestAssert(GetLastError() != ERROR_ALREADY_EXISTS);
    }
    TestAssert(WaitForMutexToBeCreated(childEvents[0], ChildEventNamePrefix0));
    TestAssert(WaitForMutexToBeCreated(childEvents[1], ChildEventNamePrefix1));
    int ei = 0;

    TestCreateMutex(m, BuildName(name, GlobalPrefix, NamePrefix));
    TestAssert(m != nullptr);
    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child locks mutex
    TestAssert(parentEvents[0].Release());
    TestAssert(parentEvents[1].Release()); // child sleeps for short duration and abandons the mutex
    TestAssert(WaitForSingleObject(m, FailTimeoutMilliseconds) == WAIT_ABANDONED_0); // attempt to lock and see abandoned mutex
    TestAssert(m.Release());
    TestAssert(WaitForSingleObject(m, FailTimeoutMilliseconds) == WAIT_OBJECT_0); // lock again to see it's not abandoned anymore
    TestAssert(m.Release());

    FinalWaitForChild(childEvents, ei);
    return true;
}

DWORD AbandonTests_Child_GracefulExit(void *arg = nullptr)
{
    AutoCloseMutexHandle childEvents[2];
    {
        AutoCloseMutexHandle m, parentEvents[2];
        char name[MaxPathSize];
        for (int i = 0; i < 2; ++i)
        {
            TestCreateMutex(
                childEvents[i],
                BuildName(name, GlobalPrefix, i == 0 ? ChildEventNamePrefix0 : ChildEventNamePrefix1),
                true);
            TestAssert(childEvents[i] != nullptr);
            TestAssert(GetLastError() != ERROR_ALREADY_EXISTS);
        }
        TestAssert(WaitForMutexToBeCreated(parentEvents[0], ParentEventNamePrefix0));
        TestAssert(WaitForMutexToBeCreated(parentEvents[1], ParentEventNamePrefix1));
        int ei = 0;

        InitialWaitForParent(parentEvents); // parent waits for child to lock mutex
        TestCreateMutex(m, BuildName(name, GlobalPrefix, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(WaitForSingleObject(m, 0) == WAIT_OBJECT_0);
        TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent waits on mutex
        Sleep(500); // wait for parent to wait on mutex
        m.Abandon(); // don't close the mutex
    }

    TestAssert(childEvents[0].Release());
    TestAssert(childEvents[1].Release());
    return 0; // abandon the mutex gracefully
}

DWORD AbandonTests_Child_AbruptExit(void *arg = nullptr)
{
    DWORD currentPid = test_getpid();
    TestAssert(currentPid != parentPid); // this test needs to run in a separate process

    {
        AutoCloseMutexHandle childEvents[2];
        {
            AutoCloseMutexHandle m, parentEvents[2];
            char name[MaxPathSize];
            for (int i = 0; i < 2; ++i)
            {
                TestCreateMutex(
                    childEvents[i],
                    BuildName(name, GlobalPrefix, i == 0 ? ChildEventNamePrefix0 : ChildEventNamePrefix1),
                    true);
                TestAssert(childEvents[i] != nullptr);
                TestAssert(GetLastError() != ERROR_ALREADY_EXISTS);
            }
            TestAssert(WaitForMutexToBeCreated(parentEvents[0], ParentEventNamePrefix0));
            TestAssert(WaitForMutexToBeCreated(parentEvents[1], ParentEventNamePrefix1));
            int ei = 0;

            InitialWaitForParent(parentEvents); // parent waits for child to lock mutex
            TestCreateMutex(m, BuildName(name, GlobalPrefix, NamePrefix));
            TestAssert(m != nullptr);
            TestAssert(WaitForSingleObject(m, 0) == WAIT_OBJECT_0);
            TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent waits on mutex
            Sleep(500); // wait for parent to wait on mutex
            m.Abandon(); // don't close the mutex
        }

        TestAssert(childEvents[0].Release());
        TestAssert(childEvents[1].Release());
    }

    TestAssert(test_kill(currentPid) == 0); // abandon the mutex abruptly
    return 0;
}

bool AbandonTests()
{
    // Abandon by graceful exit unblocks a waiter
    TestAssert(StartThread(AbandonTests_Child_GracefulExit));
    TestAssert(AbandonTests_Parent());
    TestAssert(StartProcess("AbandonTests_Child_GracefulExit"));
    TestAssert(AbandonTests_Parent());

    // Abandon by abrupt exit unblocks a waiter
    TestAssert(StartProcess("AbandonTests_Child_AbruptExit"));
    TestAssert(AbandonTests_Parent());

    return true;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Test harness

bool RunTests()
{
    bool (*(testList[]))() =
    {
        NameTests,
        HeaderMismatchTests,
        MutualExclusionTests,
        LifetimeTests,
        AbandonTests
    };

    bool allPassed = true;
    for (int i = 0; i < _countof(testList); ++i)
    {
        if (!testList[i]())
        {
            allPassed = false;
        }
    }
    return allPassed;
}

int __cdecl main(int argc, char **argv)
{
    if (argc != 1 && argc != 3)
    {
        return FAIL;
    }

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    test_strcpy(processPath, argv[0]);
    if (argc == 1)
    {
        isParent = true;
        parentPid = test_getpid();

        int result = RunTests() ? PASS : FAIL;
        ExitProcess(result);
        return result;
    }

    isParent = false;

    // Get parent process' ID from argument
    if (test_sscanf(argv[1], "%u", &parentPid) != 1)
    {
        ExitProcess(FAIL);
        return FAIL;
    }

    if (test_strcmp(argv[2], "MutualExclusionTests_Child") == 0)
    {
        MutualExclusionTests_Child();
    }
    else if (test_strcmp(argv[2], "LifetimeTests_Child") == 0)
    {
        LifetimeTests_Child();
    }
    else if (test_strcmp(argv[2], "AbandonTests_Child_GracefulExit") == 0)
    {
        AbandonTests_Child_GracefulExit();
    }
    else if (test_strcmp(argv[2], "AbandonTests_Child_AbruptExit") == 0)
    {
        AbandonTests_Child_AbruptExit();
    }
    ExitProcess(PASS);
    return PASS;
}
