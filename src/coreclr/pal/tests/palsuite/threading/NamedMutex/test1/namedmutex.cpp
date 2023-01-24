// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// These test cases test named mutexes, including positive
// and negative cases, cross - thread and cross - process, mutual
// exclusion, abandon detection, etc.

#include <palsuite.h>

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
const char *const ChildRunningEventNamePrefix = "paltest_namedmutex_test1_cr_";

const char *const GlobalShmFilePathPrefix = "/tmp/.dotnet/shm/global/";

#define MaxPathSize (200)
const DWORD PollLoopSleepMilliseconds = 100;
const DWORD FailTimeoutMilliseconds = 30000;
DWORD g_expectedTimeoutMilliseconds = 500;

bool g_isParent = true;
bool g_isStress = false;
char g_processPath[4096], g_processCommandLinePath[4096];
DWORD g_parentPid = static_cast<DWORD>(-1);

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
            if (!g_isParent) \
            { \
                Trace("'paltest_namedmutex_test1' child process failed at line %u. Expression: " #expression "\n", __LINE__); \
            } \
            else \
            { \
                Trace("'paltest_namedmutex_test1' failed at line %u. Expression: " #expression "\n", __LINE__); \
            } \
            fflush(stdout); \
            return false; \
        } \
    } while(false)

char *BuildName(const char *testName, char *buffer, const char *prefix0, const char *prefix1 = nullptr)
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

    if (g_isStress)
    {
        // Append the test name so that tests can run in parallel
        nameLength += test_sprintf(&buffer[nameLength], "%s", testName);
        buffer[nameLength++] = '_';
    }

    nameLength += test_sprintf(&buffer[nameLength], "%u", g_parentPid);
    return buffer;
}

char *BuildGlobalShmFilePath(const char *testName, char *buffer, const char *namePrefix)
{
    size_t pathLength = 0;
    test_strcpy(&buffer[pathLength], GlobalShmFilePathPrefix);
    pathLength += test_strlen(GlobalShmFilePathPrefix);
    test_strcpy(&buffer[pathLength], namePrefix);
    pathLength += test_strlen(namePrefix);

    if (g_isStress)
    {
        // Append the test name so that tests can run in parallel
        pathLength += test_sprintf(&buffer[pathLength], "%s", testName);
        buffer[pathLength++] = '_';
    }

    pathLength += test_sprintf(&buffer[pathLength], "%u", g_parentPid);
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
    LPWSTR nameW = convert(name);
    m = CreateMutex(nullptr, initiallyOwned, nameW);
    free(nameW);
}

HANDLE TestOpenMutex(const char *name)
{
    return OpenMutexA(SYNCHRONIZE, false, name);
}

bool StartProcess(const char *funcName)
{
    // Command line format: <processPath> <parentPid> <testFunctionName> [stress]

    size_t processCommandLinePathLength = 0;
    g_processCommandLinePath[processCommandLinePathLength++] = '\"';
    test_strcpy(&g_processCommandLinePath[processCommandLinePathLength], g_processPath);
    processCommandLinePathLength += test_strlen(g_processPath);
    g_processCommandLinePath[processCommandLinePathLength++] = '\"';
    g_processCommandLinePath[processCommandLinePathLength++] = ' ';
    processCommandLinePathLength += test_sprintf(&g_processCommandLinePath[processCommandLinePathLength], "%s ", "threading/NamedMutex/test1/paltest_namedmutex_test1");

    processCommandLinePathLength += test_sprintf(&g_processCommandLinePath[processCommandLinePathLength], "%u", g_parentPid);
    g_processCommandLinePath[processCommandLinePathLength++] = ' ';
    test_strcpy(&g_processCommandLinePath[processCommandLinePathLength], funcName);
    processCommandLinePathLength += test_strlen(funcName);

    if (g_isStress)
    {
        test_strcpy(&g_processCommandLinePath[processCommandLinePathLength], " stress");
        processCommandLinePathLength += STRING_LENGTH("stress");
    }

    STARTUPINFO si;
    memset(&si, 0, sizeof(si));
    si.cb = sizeof(si);
    PROCESS_INFORMATION pi;
    memset(&pi, 0, sizeof(pi));
    LPWSTR nameW = convert(g_processCommandLinePath);
    if (!CreateProcessW(nullptr, nameW, nullptr, nullptr, false, 0, nullptr, nullptr, &si, &pi))
    {
        free(nameW);
        return false;
    }

    free(nameW);
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    return true;
}

bool StartThread(LPTHREAD_START_ROUTINE func, void *arg = nullptr, HANDLE *threadHandleRef = nullptr)
{
    DWORD threadId;
    HANDLE handle = CreateThread(nullptr, 0, func, arg, 0, &threadId);
    if (handle != nullptr)
    {
        if (threadHandleRef == nullptr)
        {
            CloseHandle(handle);
        }
        else
        {
            *threadHandleRef = handle;
        }
        return true;
    }
    return false;
}

bool WaitForMutexToBeCreated(const char *testName, AutoCloseMutexHandle &m, const char *eventNamePrefix)
{
    char eventName[MaxPathSize];
    BuildName(testName, eventName, GlobalPrefix, eventNamePrefix);
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

bool AcquireChildRunningEvent(const char *testName, AutoCloseMutexHandle &childRunningEvent)
{
    char name[MaxPathSize];
    TestCreateMutex(childRunningEvent, BuildName(testName, name, GlobalPrefix, ChildRunningEventNamePrefix));
    TestAssert(WaitForSingleObject(childRunningEvent, FailTimeoutMilliseconds) == WAIT_OBJECT_0);
    return true;
}

bool InitializeParent(const char *testName, AutoCloseMutexHandle parentEvents[2], AutoCloseMutexHandle childEvents[2])
{
    // Create parent events
    char name[MaxPathSize];
    for (int i = 0; i < 2; ++i)
    {
        TestCreateMutex(
            parentEvents[i],
            BuildName(testName, name, GlobalPrefix, i == 0 ? ParentEventNamePrefix0 : ParentEventNamePrefix1),
            true);
        TestAssert(parentEvents[i] != nullptr);
        TestAssert(GetLastError() != ERROR_ALREADY_EXISTS);
    }

    // Wait for the child to create and acquire locks on its events so that the parent can wait on them
    TestAssert(WaitForMutexToBeCreated(testName, childEvents[0], ChildEventNamePrefix0));
    TestAssert(WaitForMutexToBeCreated(testName, childEvents[1], ChildEventNamePrefix1));
    return true;
}

bool UninitializeParent(const char *testName, AutoCloseMutexHandle parentEvents[2], bool releaseParentEvents = true)
{
    if (releaseParentEvents)
    {
        TestAssert(parentEvents[0].Release());
        TestAssert(parentEvents[1].Release());
    }

    // Wait for the child to finish its test. Child tests will release and close 'childEvents' before releasing
    // 'childRunningEvent', so after this wait, the parent process can freely start another child that will deterministically
    // recreate the 'childEvents', which the next parent test will wait on, upon its initialization.
    AutoCloseMutexHandle childRunningEvent;
    TestAssert(AcquireChildRunningEvent(testName, childRunningEvent));
    TestAssert(childRunningEvent.Release());
    return true;
}

bool InitializeChild(
    const char *testName,
    AutoCloseMutexHandle &childRunningEvent,
    AutoCloseMutexHandle parentEvents[2],
    AutoCloseMutexHandle childEvents[2])
{
    TestAssert(AcquireChildRunningEvent(testName, childRunningEvent));

    // Create child events
    char name[MaxPathSize];
    for (int i = 0; i < 2; ++i)
    {
        TestCreateMutex(
            childEvents[i],
            BuildName(testName, name, GlobalPrefix, i == 0 ? ChildEventNamePrefix0 : ChildEventNamePrefix1),
            true);
        TestAssert(childEvents[i] != nullptr);
        TestAssert(GetLastError() != ERROR_ALREADY_EXISTS);
    }

    // Wait for the parent to create and acquire locks on its events so that the child can wait on them
    TestAssert(WaitForMutexToBeCreated(testName, parentEvents[0], ParentEventNamePrefix0));
    TestAssert(WaitForMutexToBeCreated(testName, parentEvents[1], ParentEventNamePrefix1));

    // Parent/child tests start with the parent, so after initialization, wait for the parent to tell the child test to start
    TestAssert(WaitForSingleObject(parentEvents[0], FailTimeoutMilliseconds) == WAIT_OBJECT_0);
    TestAssert(parentEvents[0].Release());
    return true;
}

bool UninitializeChild(
    AutoCloseMutexHandle &childRunningEvent,
    AutoCloseMutexHandle parentEvents[2],
    AutoCloseMutexHandle childEvents[2])
{
    // Release and close 'parentEvents' and 'childEvents' before releasing 'childRunningEvent' to avoid races, see
    // UninitializeParent() for more info
    TestAssert(childEvents[0].Release());
    TestAssert(childEvents[1].Release());
    childEvents[0].Close();
    childEvents[1].Close();
    parentEvents[0].Close();
    parentEvents[1].Close();
    TestAssert(childRunningEvent.Release());
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

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Tests

bool NameTests()
{
    const char *testName = "NameTests";

    AutoCloseMutexHandle m;
    char name[MaxPathSize];

    // Empty name
    TestCreateMutex(m, "");
    TestAssert(m != nullptr);

    // Normal name
    TestCreateMutex(m, BuildName(testName, name, NamePrefix));
    TestAssert(m != nullptr);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(testName, name, NamePrefix))) != nullptr);
    TestCreateMutex(m, BuildName(testName, name, SessionPrefix, NamePrefix));
    TestAssert(m != nullptr);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(testName, name, SessionPrefix, NamePrefix))) != nullptr);
    TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
    TestAssert(m != nullptr);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(testName, name, GlobalPrefix, NamePrefix))) != nullptr);

    // Name too long. The maximum allowed length depends on the file system, so we're not checking for that.
    {
        char name[257];
        memset(name, 'a', STRING_LENGTH(name));
        name[STRING_LENGTH(name)] = '\0';
        TestCreateMutex(m, name);
        TestAssert(m == nullptr);
        TestAssert(GetLastError() == ERROR_FILENAME_EXCED_RANGE);
        TestAssert(AutoCloseMutexHandle(TestOpenMutex(name)) == nullptr);
        TestAssert(GetLastError() == ERROR_FILENAME_EXCED_RANGE);
    }

    // Invalid characters in name
    TestCreateMutex(m, BuildName(testName, name, InvalidNamePrefix0));
    TestAssert(m == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(testName, name, InvalidNamePrefix0))) == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestCreateMutex(m, BuildName(testName, name, InvalidNamePrefix1));
    TestAssert(m == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(testName, name, InvalidNamePrefix1))) == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestCreateMutex(m, BuildName(testName, name, SessionPrefix, InvalidNamePrefix0));
    TestAssert(m == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(testName, name, SessionPrefix, InvalidNamePrefix0))) == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, InvalidNamePrefix1));
    TestAssert(m == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(testName, name, GlobalPrefix, InvalidNamePrefix1))) == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_NAME);

    // Creating a second reference to the same named mutex yields an error indicating that it was opened, not created
    {
        TestCreateMutex(m, BuildName(testName, name, NamePrefix));
        TestAssert(m != nullptr);
        AutoCloseMutexHandle m2;
        TestCreateMutex(m2, BuildName(testName, name, NamePrefix));
        TestAssert(m2 != nullptr);
        TestAssert(GetLastError() == ERROR_ALREADY_EXISTS);
    }

    return true;
}

bool HeaderMismatchTests()
{
    const char *testName = "HeaderMismatchTests";

    AutoCloseMutexHandle m, m2;
    char name[MaxPathSize];
    int fd;

    // Create and hold onto a mutex during this test to create the shared memory directory
    TestCreateMutex(m2, BuildName(testName, name, GlobalPrefix, TempNamePrefix));
    TestAssert(m2 != nullptr);

    // Unknown shared memory type
    TestAssert(WriteHeaderInfo(BuildGlobalShmFilePath(testName, name, NamePrefix), -1, 1, &fd));
    TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
    TestAssert(m == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_HANDLE);
    TestAssert(test_close(fd) == 0);
    TestAssert(test_unlink(BuildGlobalShmFilePath(testName, name, NamePrefix)) == 0);

    // Mismatched version
    TestAssert(WriteHeaderInfo(BuildGlobalShmFilePath(testName, name, NamePrefix), 0, -1, &fd));
    TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
    TestAssert(m == nullptr);
    TestAssert(GetLastError() == ERROR_INVALID_HANDLE);
    TestAssert(test_close(fd) == 0);
    TestAssert(test_unlink(BuildGlobalShmFilePath(testName, name, NamePrefix)) == 0);

    return true;
}

bool MutualExclusionTests_Parent()
{
    const char *testName = "MutualExclusionTests";

    AutoCloseMutexHandle parentEvents[2], childEvents[2];
    TestAssert(InitializeParent(testName, parentEvents, childEvents));
    int ei = 0;
    char name[MaxPathSize];
    AutoCloseMutexHandle m;

    TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
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
    TestAssert(WaitForSingleObject(m, g_expectedTimeoutMilliseconds) == WAIT_TIMEOUT); // try to lock the mutex with a timeout
    TestAssert(!m.Release()); // try to release the lock while another thread owns it
    TestAssert(GetLastError() == ERROR_NOT_OWNER);

    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child releases the lock

    TestAssert(WaitForSingleObject(m, static_cast<DWORD>(-1)) == WAIT_OBJECT_0); // lock the mutex with no timeout and release
    TestAssert(m.Release());

    TestAssert(UninitializeParent(testName, parentEvents));
    return true;
}

DWORD PALAPI MutualExclusionTests_Child(void *arg = nullptr)
{
    const char *testName = "MutualExclusionTests";

    AutoCloseMutexHandle childRunningEvent, parentEvents[2], childEvents[2];
    TestAssert(InitializeChild(testName, childRunningEvent, parentEvents, childEvents));
    int ei = 0;

    {
        char name[MaxPathSize];
        AutoCloseMutexHandle m;

        TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(WaitForSingleObject(m, 0) == WAIT_OBJECT_0); // lock the mutex
        YieldToParent(parentEvents, childEvents, ei); // parent attempts to lock/release, and fails
        TestAssert(m.Release()); // release the lock
    }

    TestAssert(UninitializeChild(childRunningEvent, parentEvents, childEvents));
    return 0;
}

bool MutualExclusionTests()
{
    const char *testName = "MutualExclusionTests";

    {
        AutoCloseMutexHandle m;
        char name[MaxPathSize];

        // Releasing a lock that is not owned by any thread fails
        TestCreateMutex(m, BuildName(testName, name, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(!m.Release());
        TestAssert(GetLastError() == ERROR_NOT_OWNER);

        // Acquire a lock during upon creation, and release
        TestCreateMutex(m, BuildName(testName, name, NamePrefix), true);
        TestAssert(m != nullptr);
        TestAssert(m.Release());

        // Multi-waits including a named mutex are not supported
        AutoCloseMutexHandle m2;
        TestCreateMutex(m2, nullptr);
        TestAssert(m2 != nullptr);
        HANDLE waitHandles[] = {m2.GetHandle(), m.GetHandle()};
        TestAssert(
            WaitForMultipleObjects(
                ARRAY_SIZE(waitHandles),
                waitHandles,
                false /* waitAll */,
                FailTimeoutMilliseconds) ==
            WAIT_FAILED);
        TestAssert(GetLastError() == ERROR_NOT_SUPPORTED);
        TestAssert(
            WaitForMultipleObjects(
                ARRAY_SIZE(waitHandles),
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
    const char *testName = "LifetimeTests";

    AutoCloseMutexHandle parentEvents[2], childEvents[2];
    TestAssert(InitializeParent(testName, parentEvents, childEvents));
    int ei = 0;
    char name[MaxPathSize];
    AutoCloseMutexHandle m;

    TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix)); // create first reference to mutex
    TestAssert(m != nullptr);
    TestAssert(TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)));
    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child creates second reference to mutex using CreateMutex
    m.Close(); // close first reference
    TestAssert(TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)));
    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child closes second reference
    TestAssert(!TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)));

    TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix)); // create first reference to mutex
    TestAssert(m != nullptr);
    TestAssert(TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)));
    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child creates second reference to mutex using OpenMutex
    m.Close(); // close first reference
    TestAssert(TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)));
    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child closes second reference
    TestAssert(!TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)));

    TestAssert(UninitializeParent(testName, parentEvents));
    return true;
}

DWORD PALAPI LifetimeTests_Child(void *arg = nullptr)
{
    const char *testName = "LifetimeTests";

    AutoCloseMutexHandle childRunningEvent, parentEvents[2], childEvents[2];
    TestAssert(InitializeChild(testName, childRunningEvent, parentEvents, childEvents));
    int ei = 0;

    {
        char name[MaxPathSize];
        AutoCloseMutexHandle m;

        // ... parent creates first reference to mutex
        TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix)); // create second reference to mutex using CreateMutex
        TestAssert(m != nullptr);
        TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent closes first reference
        m.Close(); // close second reference

        TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent verifies, and creates first reference to mutex again
        m = TestOpenMutex(BuildName(testName, name, GlobalPrefix, NamePrefix)); // create second reference to mutex using OpenMutex
        TestAssert(m != nullptr);
        TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent closes first reference
        m.Close(); // close second reference

        TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent verifies
    }

    TestAssert(UninitializeChild(childRunningEvent, parentEvents, childEvents));
    return 0;
}

bool LifetimeTests()
{
    const char *testName = "LifetimeTests";

    {
        AutoCloseMutexHandle m;
        char name[MaxPathSize];

        // Shm file should be created and deleted
        TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)));
        m.Close();
        TestAssert(!TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)));
    }

    // Shm file should not be deleted until last reference is released
    TestAssert(StartThread(LifetimeTests_Child));
    TestAssert(LifetimeTests_Parent());
    TestAssert(StartProcess("LifetimeTests_Child"));
    TestAssert(LifetimeTests_Parent());

    return true;
}

DWORD PALAPI AbandonTests_Child_TryLock(void *arg = nullptr);

bool AbandonTests_Parent()
{
    const char *testName = "AbandonTests";

    char name[MaxPathSize];
    AutoCloseMutexHandle m;
    {
        AutoCloseMutexHandle parentEvents[2], childEvents[2];
        TestAssert(InitializeParent(testName, parentEvents, childEvents));
        int ei = 0;

        TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child locks mutex
        TestAssert(parentEvents[0].Release());
        TestAssert(parentEvents[1].Release()); // child sleeps for short duration and abandons the mutex
        TestAssert(WaitForSingleObject(m, FailTimeoutMilliseconds) == WAIT_ABANDONED_0); // attempt to lock and see abandoned mutex

        TestAssert(UninitializeParent(testName, parentEvents, false /* releaseParentEvents */)); // parent events are released above
    }

    // Verify that the mutex lock is owned by this thread, by starting a new thread and trying to lock it
    TestAssert(StartThread(AbandonTests_Child_TryLock));
    {
        AutoCloseMutexHandle parentEvents[2], childEvents[2];
        TestAssert(InitializeParent(testName, parentEvents, childEvents));
        int ei = 0;

        TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child tries to lock mutex

        TestAssert(UninitializeParent(testName, parentEvents));
    }

    // Verify that the mutex lock is owned by this thread, by starting a new process and trying to lock it
    TestAssert(StartProcess("AbandonTests_Child_TryLock"));
    AutoCloseMutexHandle parentEvents[2], childEvents[2];
    TestAssert(InitializeParent(testName, parentEvents, childEvents));
    int ei = 0;

    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child tries to lock mutex

    // Continue verification
    TestAssert(m.Release());
    TestAssert(WaitForSingleObject(m, FailTimeoutMilliseconds) == WAIT_OBJECT_0); // lock again to see it's not abandoned anymore
    TestAssert(m.Release());

    TestAssert(UninitializeParent(testName, parentEvents));

    // Since the child abandons the mutex, and a child process may not release the file lock on the shared memory file before
    // indicating completion to the parent, make sure to delete the shared memory file by repeatedly opening/closing the mutex
    // until the parent process becomes the last process to reference the mutex and closing it deletes the file.
    DWORD startTime = GetTickCount();
    while (true)
    {
        m.Close();
        if (!TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)))
        {
            break;
        }

        TestAssert(GetTickCount() - startTime < FailTimeoutMilliseconds);
        m = TestOpenMutex(BuildName(testName, name, GlobalPrefix, NamePrefix));
    }

    return true;
}

DWORD PALAPI AbandonTests_Child_GracefulExit_Close(void *arg = nullptr)
{
    const char *testName = "AbandonTests";

    AutoCloseMutexHandle childRunningEvent, parentEvents[2], childEvents[2];
    TestAssert(InitializeChild(testName, childRunningEvent, parentEvents, childEvents));
    int ei = 0;

    {
        char name[MaxPathSize];
        AutoCloseMutexHandle m;

        // ... parent waits for child to lock mutex
        TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(WaitForSingleObject(m, 0) == WAIT_OBJECT_0);
        TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent waits on mutex
        Sleep(g_expectedTimeoutMilliseconds); // wait for parent to wait on mutex
        m.Close(); // close mutex without releasing lock
    }

    TestAssert(UninitializeChild(childRunningEvent, parentEvents, childEvents));
    return 0;
}

DWORD AbandonTests_Child_GracefulExit_NoClose(void *arg = nullptr)
{
    const char *testName = "AbandonTests";

    // This test needs to run in a separate process because it does not close the mutex handle. Running it in a separate thread
    // causes the mutex object to retain a reference until the process terminates.
    TestAssert(test_getpid() != g_parentPid);

    AutoCloseMutexHandle childRunningEvent, parentEvents[2], childEvents[2];
    TestAssert(InitializeChild(testName, childRunningEvent, parentEvents, childEvents));
    int ei = 0;

    {
        char name[MaxPathSize];
        AutoCloseMutexHandle m;

        // ... parent waits for child to lock mutex
        TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(WaitForSingleObject(m, 0) == WAIT_OBJECT_0);
        TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent waits on mutex
        Sleep(g_expectedTimeoutMilliseconds); // wait for parent to wait on mutex
        m.Abandon(); // don't close the mutex
    }

    TestAssert(UninitializeChild(childRunningEvent, parentEvents, childEvents));
    return 0;
}

DWORD AbandonTests_Child_AbruptExit(void *arg = nullptr)
{
    const char *testName = "AbandonTests";

    DWORD currentPid = test_getpid();
    TestAssert(currentPid != g_parentPid); // this test needs to run in a separate process

    {
        AutoCloseMutexHandle childRunningEvent, parentEvents[2], childEvents[2];
        TestAssert(InitializeChild(testName, childRunningEvent, parentEvents, childEvents));
        int ei = 0;

        {
            char name[MaxPathSize];
            AutoCloseMutexHandle m;

            // ... parent waits for child to lock mutex
            TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
            TestAssert(m != nullptr);
            TestAssert(WaitForSingleObject(m, 0) == WAIT_OBJECT_0);
            TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent waits on mutex
            Sleep(g_expectedTimeoutMilliseconds); // wait for parent to wait on mutex
            m.Abandon(); // don't close the mutex
        }

        TestAssert(UninitializeChild(childRunningEvent, parentEvents, childEvents));
    }

    TestAssert(test_kill(currentPid) == 0); // abandon the mutex abruptly
    return 0;
}

// This child process acquires the mutex lock, creates another child process (to ensure that file locks are not inherited), and
// abandons the mutex abruptly. The second child process detects the abandonment and abandons the mutex again for the parent to
// detect. Issue: https://github.com/dotnet/runtime/issues/11636
DWORD AbandonTests_Child_FileLocksNotInherited_Parent_AbruptExit(void *arg = nullptr)
{
    const char *testName = "AbandonTests";

    DWORD currentPid = test_getpid();
    TestAssert(currentPid != g_parentPid); // this test needs to run in a separate process

    {
        char name[MaxPathSize];
        AutoCloseMutexHandle m;

        // ... root parent waits for child to lock mutex
        TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(WaitForSingleObject(m, 0) == WAIT_OBJECT_0);

        // Start a child process while holding the lock on the mutex, to ensure that file locks are not inherited by the
        // immediate child, such that the immediate child would be able to detect the mutex being abandoned below. This process
        // does not communicate with the root parent, it only communicates to the immediate child by abandoning the mutex. The
        // immediate child communicates with the root parent to complete the test.
        TestAssert(StartProcess("AbandonTests_Child_FileLocksNotInherited_Child_AbruptExit")); // immediate child waits on mutex

        Sleep(g_expectedTimeoutMilliseconds); // wait for immediate child to wait on mutex
        m.Abandon(); // don't close the mutex
    }

    TestAssert(test_kill(currentPid) == 0); // abandon the mutex abruptly
    return 0;
}

DWORD AbandonTests_Child_FileLocksNotInherited_Child_AbruptExit(void *arg = nullptr)
{
    const char *testName = "AbandonTests";

    DWORD currentPid = test_getpid();
    TestAssert(currentPid != g_parentPid); // this test needs to run in a separate process

    AutoCloseMutexHandle childRunningEvent, parentEvents[2], childEvents[2];
    TestAssert(InitializeChild(testName, childRunningEvent, parentEvents, childEvents));
    int ei = 0;

    {
        char name[MaxPathSize];
        AutoCloseMutexHandle m;

        // ... immediate parent expects child to wait on mutex
        TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(WaitForSingleObject(m, FailTimeoutMilliseconds) == WAIT_ABANDONED_0); // attempt to lock and see abandoned mutex
        TestAssert(YieldToParent(parentEvents, childEvents, ei)); // root parent waits on mutex
        Sleep(g_expectedTimeoutMilliseconds); // wait for root parent to wait on mutex
        m.Close(); // close mutex without releasing lock (root parent expects the mutex to be abandoned)
    }

    TestAssert(UninitializeChild(childRunningEvent, parentEvents, childEvents));
    return 0;
}

DWORD PALAPI AbandonTests_Child_TryLock(void *arg)
{
    const char *testName = "AbandonTests";

    AutoCloseMutexHandle childRunningEvent, parentEvents[2], childEvents[2];
    TestAssert(InitializeChild(testName, childRunningEvent, parentEvents, childEvents));
    int ei = 0;

    {
        char name[MaxPathSize];
        AutoCloseMutexHandle m;

        // ... parent waits for child to lock mutex
        TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
        TestAssert(m != nullptr);
        TestAssert(WaitForSingleObject(m, 0) == WAIT_TIMEOUT); // try to lock the mutex while the parent holds the lock
        TestAssert(WaitForSingleObject(m, g_expectedTimeoutMilliseconds) == WAIT_TIMEOUT);
    }

    TestAssert(UninitializeChild(childRunningEvent, parentEvents, childEvents));
    return 0;
}

bool AbandonTests()
{
    // Abandon by graceful exit where the lock owner closes the mutex before releasing it, unblocks a waiter
    TestAssert(StartThread(AbandonTests_Child_GracefulExit_Close));
    TestAssert(AbandonTests_Parent());
    TestAssert(StartProcess("AbandonTests_Child_GracefulExit_Close"));
    TestAssert(AbandonTests_Parent());

    // Abandon by graceful exit without closing the mutex unblocks a waiter
    TestAssert(StartProcess("AbandonTests_Child_GracefulExit_NoClose"));
    TestAssert(AbandonTests_Parent());

    // Abandon by abrupt exit unblocks a waiter
    TestAssert(StartProcess("AbandonTests_Child_AbruptExit"));
    TestAssert(AbandonTests_Parent());

    TestAssert(StartProcess("AbandonTests_Child_FileLocksNotInherited_Parent_AbruptExit"));
    TestAssert(AbandonTests_Parent());

    return true;
}

bool LockAndCloseWithoutThreadExitTests_Parent_CloseOnSameThread()
{
    const char *testName = "LockAndCloseWithoutThreadExitTests";

    AutoCloseMutexHandle parentEvents[2], childEvents[2];
    TestAssert(InitializeParent(testName, parentEvents, childEvents));
    int ei = 0;
    char name[MaxPathSize];
    AutoCloseMutexHandle m;

    TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
    TestAssert(m != nullptr);

    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child locks mutex and closes second reference to mutex on lock-owner thread
    TestAssert(WaitForSingleObject(m, 0) == WAIT_TIMEOUT); // attempt to lock and fail

    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child closes last reference to mutex on lock-owner thread
    TestAssert(WaitForSingleObject(m, 0) == WAIT_ABANDONED_0); // attempt to lock and see abandoned mutex
    TestAssert(m.Release());

    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child exits
    TestAssert(TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)));
    m.Close();
    TestAssert(!TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)));

    TestAssert(UninitializeParent(testName, parentEvents));
    return true;
}

DWORD PALAPI LockAndCloseWithoutThreadExitTests_Child_CloseOnSameThread(void *arg = nullptr)
{
    const char *testName = "LockAndCloseWithoutThreadExitTests";

    TestAssert(test_getpid() != g_parentPid); // this test needs to run in a separate process

    AutoCloseMutexHandle childRunningEvent, parentEvents[2], childEvents[2];
    TestAssert(InitializeChild(testName, childRunningEvent, parentEvents, childEvents));
    int ei = 0;
    char name[MaxPathSize];

    // ... parent waits for child to lock and close second reference to mutex
    AutoCloseMutexHandle m(TestOpenMutex(BuildName(testName, name, GlobalPrefix, NamePrefix)));
    TestAssert(m != nullptr);
    TestAssert(WaitForSingleObject(m, 0) == WAIT_OBJECT_0);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(testName, name, GlobalPrefix, NamePrefix))) != nullptr);
    TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent waits for child to close last reference to mutex

    m.Close(); // close mutex on lock-owner thread without releasing lock
    TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent verifies while this thread is still active

    TestAssert(UninitializeChild(childRunningEvent, parentEvents, childEvents));
    return 0;
}

DWORD PALAPI LockAndCloseWithoutThreadExitTests_ChildThread_CloseMutex(void *arg);

bool LockAndCloseWithoutThreadExitTests_Parent_CloseOnDifferentThread()
{
    const char *testName = "LockAndCloseWithoutThreadExitTests";

    AutoCloseMutexHandle parentEvents[2], childEvents[2];
    TestAssert(InitializeParent(testName, parentEvents, childEvents));
    int ei = 0;
    char name[MaxPathSize];
    AutoCloseMutexHandle m;

    TestCreateMutex(m, BuildName(testName, name, GlobalPrefix, NamePrefix));
    TestAssert(m != nullptr);

    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child locks mutex and closes second reference to mutex on lock-owner thread
    TestAssert(WaitForSingleObject(m, 0) == WAIT_TIMEOUT); // attempt to lock and fail

    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child closes last reference to mutex on non-lock-owner thread
    TestAssert(WaitForSingleObject(m, 0) == WAIT_TIMEOUT); // attempt to lock and fail
    m.Close();
    m = TestOpenMutex(BuildName(testName, name, GlobalPrefix, NamePrefix));
    TestAssert(m != nullptr); // child has implicit reference to mutex

    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child closes new reference to mutex on lock-owner thread
    TestAssert(WaitForSingleObject(m, 0) == WAIT_ABANDONED_0); // attempt to lock and see abandoned mutex
    TestAssert(m.Release());

    TestAssert(YieldToChild(parentEvents, childEvents, ei)); // child exits
    TestAssert(TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)));
    m.Close();
    TestAssert(!TestFileExists(BuildGlobalShmFilePath(testName, name, NamePrefix)));

    TestAssert(UninitializeParent(testName, parentEvents));
    return true;
}

DWORD PALAPI LockAndCloseWithoutThreadExitTests_Child_CloseOnDifferentThread(void *arg = nullptr)
{
    const char *testName = "LockAndCloseWithoutThreadExitTests";

    TestAssert(test_getpid() != g_parentPid); // this test needs to run in a separate process

    AutoCloseMutexHandle childRunningEvent, parentEvents[2], childEvents[2];
    TestAssert(InitializeChild(testName, childRunningEvent, parentEvents, childEvents));
    int ei = 0;
    char name[MaxPathSize];

    // ... parent waits for child to lock and close second reference to mutex
    AutoCloseMutexHandle m(TestOpenMutex(BuildName(testName, name, GlobalPrefix, NamePrefix)));
    TestAssert(m != nullptr);
    TestAssert(WaitForSingleObject(m, 0) == WAIT_OBJECT_0);
    TestAssert(AutoCloseMutexHandle(TestOpenMutex(BuildName(testName, name, GlobalPrefix, NamePrefix))) != nullptr);
    TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent waits for child to close last reference to mutex

    // Close the mutex on a thread that is not the lock-owner thread, without releasing the lock
    HANDLE closeMutexThread = nullptr;
    TestAssert(StartThread(LockAndCloseWithoutThreadExitTests_ChildThread_CloseMutex, (HANDLE)m, &closeMutexThread));
    TestAssert(closeMutexThread != nullptr);
    TestAssert(WaitForSingleObject(closeMutexThread, FailTimeoutMilliseconds) == WAIT_OBJECT_0);
    TestAssert(CloseHandle(closeMutexThread));
    m.Abandon(); // mutex is already closed, don't close it again
    TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent verifies while this lock-owner thread is still active

    m = TestOpenMutex(BuildName(testName, name, GlobalPrefix, NamePrefix));
    TestAssert(m != nullptr);
    m.Close(); // close mutex on lock-owner thread without releasing lock
    TestAssert(YieldToParent(parentEvents, childEvents, ei)); // parent verifies while this thread is still active

    TestAssert(UninitializeChild(childRunningEvent, parentEvents, childEvents));
    return 0;
}

DWORD PALAPI LockAndCloseWithoutThreadExitTests_ChildThread_CloseMutex(void *arg)
{
    TestAssert(arg != nullptr);
    AutoCloseMutexHandle((HANDLE)arg).Close();
    return 0;
}

bool LockAndCloseWithoutThreadExitTests()
{
    TestAssert(StartProcess("LockAndCloseWithoutThreadExitTests_Child_CloseOnSameThread"));
    TestAssert(LockAndCloseWithoutThreadExitTests_Parent_CloseOnSameThread());

    TestAssert(StartProcess("LockAndCloseWithoutThreadExitTests_Child_CloseOnDifferentThread"));
    TestAssert(LockAndCloseWithoutThreadExitTests_Parent_CloseOnDifferentThread());

    return true;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Test harness

bool (*const TestList[])() =
{
    NameTests,
    HeaderMismatchTests,
    MutualExclusionTests,
    LifetimeTests,
    AbandonTests,
    LockAndCloseWithoutThreadExitTests
};

bool RunTests()
{
    bool allPassed = true;
    for (SIZE_T i = 0; i < ARRAY_SIZE(TestList); ++i)
    {
        if (!TestList[i]())
        {
            allPassed = false;
        }
    }
    return allPassed;
}

DWORD g_stressDurationMilliseconds = 0;
LONG g_stressTestCounts[ARRAY_SIZE(TestList)] = {0};
LONG g_stressResult = true;

DWORD PALAPI StressTest(void *arg)
{
    // Run the specified test continuously for the stress duration
    SIZE_T testIndex = reinterpret_cast<SIZE_T>(arg);
    DWORD startTime = GetTickCount();
    do
    {
        ++g_stressTestCounts[testIndex];
        if (!TestList[testIndex]())
        {
            InterlockedExchange(&g_stressResult, false);
            break;
        }
    } while (
        InterlockedCompareExchange(&g_stressResult, false, false) == true &&
        GetTickCount() - startTime < g_stressDurationMilliseconds);
    return 0;
}

bool StressTests(DWORD durationMinutes)
{
    g_isStress = true;
    g_expectedTimeoutMilliseconds = 1;
    g_stressDurationMilliseconds = durationMinutes * (60 * 1000);

    // Start a thread for each test
    HANDLE threadHandles[ARRAY_SIZE(TestList)];
    for (SIZE_T i = 0; i < ARRAY_SIZE(threadHandles); ++i)
    {
        TestAssert(StartThread(StressTest, reinterpret_cast<void *>(i), &threadHandles[i]));
    }

    while (true)
    {
        DWORD waitResult =
            WaitForMultipleObjects(ARRAY_SIZE(threadHandles), threadHandles, true /* bWaitAll */, 10 * 1000 /* dwMilliseconds */);
        TestAssert(waitResult == WAIT_OBJECT_0 || waitResult == WAIT_TIMEOUT);
        if (waitResult == WAIT_OBJECT_0)
        {
            break;
        }

        Trace("'paltest_namedmutex_test1' stress test counts: ");
        for (SIZE_T i = 0; i < ARRAY_SIZE(g_stressTestCounts); ++i)
        {
            if (i != 0)
            {
                Trace(", ");
            }
            Trace("%u", g_stressTestCounts[i]);
        }
        Trace("\n");
        fflush(stdout);
    }

    for (SIZE_T i = 0; i < ARRAY_SIZE(threadHandles); ++i)
    {
        CloseHandle(threadHandles[i]);
    }
    return static_cast<bool>(g_stressResult);
}

PALTEST(threading_NamedMutex_test1_paltest_namedmutex_test1, "threading/NamedMutex/test1/paltest_namedmutex_test1")
{
    if (argc < 1 || argc > 4)
    {
        return FAIL;
    }

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    test_strcpy(g_processPath, argv[0]);

    if (argc == 1)
    {
        // Unit test arguments: <processPath>

        g_parentPid = test_getpid();
        int result = RunTests() ? PASS : FAIL;
        ExitProcess(result);
        return result;
    }

    if (test_strcmp(argv[1], "stress") == 0)
    {
        // Stress test arguments: <processPath> stress [durationMinutes]

        DWORD durationMinutes = 1;
        if (argc >= 3 && test_sscanf(argv[2], "%u", &durationMinutes) != 1)
        {
            ExitProcess(FAIL);
            return FAIL;
        }

        g_parentPid = test_getpid();
        int result = StressTests(durationMinutes) ? PASS : FAIL;
        ExitProcess(result);
        return result;
    }

    // Child test process arguments: <processPath> <parentPid> <testFunctionName> [stress]

    g_isParent = false;

    // Get parent process' ID from argument
    if (test_sscanf(argv[1], "%u", &g_parentPid) != 1)
    {
        ExitProcess(FAIL);
        return FAIL;
    }

    if (argc >= 4 && test_strcmp(argv[3], "stress") == 0)
    {
        g_isStress = true;
    }

    if (test_strcmp(argv[2], "MutualExclusionTests_Child") == 0)
    {
        MutualExclusionTests_Child();
    }
    else if (test_strcmp(argv[2], "LifetimeTests_Child") == 0)
    {
        LifetimeTests_Child();
    }
    else if (test_strcmp(argv[2], "AbandonTests_Child_GracefulExit_Close") == 0)
    {
        AbandonTests_Child_GracefulExit_Close();
    }
    else if (test_strcmp(argv[2], "AbandonTests_Child_GracefulExit_NoClose") == 0)
    {
        AbandonTests_Child_GracefulExit_NoClose();
    }
    else if (test_strcmp(argv[2], "AbandonTests_Child_AbruptExit") == 0)
    {
        AbandonTests_Child_AbruptExit();
    }
    else if (test_strcmp(argv[2], "AbandonTests_Child_FileLocksNotInherited_Parent_AbruptExit") == 0)
    {
        AbandonTests_Child_FileLocksNotInherited_Parent_AbruptExit();
    }
    else if (test_strcmp(argv[2], "AbandonTests_Child_FileLocksNotInherited_Child_AbruptExit") == 0)
    {
        AbandonTests_Child_FileLocksNotInherited_Child_AbruptExit();
    }
    else if (test_strcmp(argv[2], "AbandonTests_Child_TryLock") == 0)
    {
        AbandonTests_Child_TryLock();
    }
    else if (test_strcmp(argv[2], "LockAndCloseWithoutThreadExitTests_Child_CloseOnSameThread") == 0)
    {
        LockAndCloseWithoutThreadExitTests_Child_CloseOnSameThread();
    }
    else if (test_strcmp(argv[2], "LockAndCloseWithoutThreadExitTests_Child_CloseOnDifferentThread") == 0)
    {
        LockAndCloseWithoutThreadExitTests_Child_CloseOnDifferentThread();
    }
    ExitProcess(PASS);
    return PASS;
}
