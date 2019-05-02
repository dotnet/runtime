// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "mockcoreclr.h"
#include <chrono>
#include <iostream>
#include <thread>
#include "trace.h"

#define MockLog(string)\
{\
    std::stringstream ss;\
    ss << "mock " << string << std::endl;\
    std::cout << ss.str();\
}
#define MockLogArg(arg)\
{\
    std::stringstream ss;\
    ss << "mock " << #arg << ":" << arg << std::endl;\
    std::cout << ss.str();\
}
#define MockLogEntry(dict, key, value)\
{\
    std::stringstream ss;\
    ss << "mock " << dict << "[" << key << "] = " << value << std::endl;\
    std::cout << ss.str();\
}

SHARED_API pal::hresult_t STDMETHODCALLTYPE coreclr_initialize(
    const char* exePath,
    const char* appDomainFriendlyName,
    int propertyCount,
    const char** propertyKeys,
    const char** propertyValues,
    coreclr_t::host_handle_t* hostHandle,
    unsigned int* domainId)
{
    MockLog("coreclr_initialize() called");
    MockLogArg(exePath);
    MockLogArg(appDomainFriendlyName);
    MockLogArg(propertyCount);
    MockLogArg(propertyKeys);
    MockLogArg(propertyValues);
    MockLogArg(hostHandle);
    MockLogArg(domainId);

    for (int i = 0; i < propertyCount; ++i)
    {
        MockLogEntry("property", propertyKeys[i], propertyValues[i]);
    }

    if (hostHandle != nullptr)
    {
        *hostHandle = reinterpret_cast<coreclr_t::host_handle_t>(0xdeadbeef);
    }

    return StatusCode::Success;
}


// Prototype of the coreclr_shutdown function from coreclr.dll
SHARED_API pal::hresult_t STDMETHODCALLTYPE coreclr_shutdown_2(
    coreclr_t::host_handle_t hostHandle,
    unsigned int domainId,
    int* latchedExitCode)
{
    MockLog("coreclr_shutdown_2() called");
    MockLogArg(hostHandle);
    MockLogArg(domainId);

    if (latchedExitCode != nullptr)
    {
        *latchedExitCode = 0;
    }

    return StatusCode::Success;
}

// Prototype of the coreclr_execute_assembly function from coreclr.dll
SHARED_API pal::hresult_t STDMETHODCALLTYPE coreclr_execute_assembly(
    coreclr_t::host_handle_t hostHandle,
    unsigned int domainId,
    int argc,
    const char** argv,
    const char* managedAssemblyPath,
    unsigned int* exitCode)
{
    MockLog("coreclr_execute_assembly() called");
    MockLogArg(hostHandle);
    MockLogArg(domainId);
    MockLogArg(argc);
    MockLogArg(argv);
    MockLogArg(managedAssemblyPath);

    for (int i = 0; i < argc; ++i)
    {
        MockLogEntry("argv", i, argv[i]);
    }

    pal::string_t path;
    if (pal::getenv(_X("TEST_BLOCK_MOCK_EXECUTE_ASSEMBLY"), &path))
    {
        while (pal::file_exists(path))
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(100));
        }
    }

    if (exitCode != nullptr)
    {
        *exitCode = 0;
    }

    return StatusCode::Success;
}

struct MockCoreClrDelegate
{
    MockCoreClrDelegate() :
    m_hostHandle(nullptr),
    m_domainId(0),
    initialized(false)
    {}

    MockCoreClrDelegate(coreclr_t::host_handle_t hostHandle,
                        unsigned int domainId,
                        const char* entryPointAssemblyName,
                        const char* entryPointTypeName,
                        const char* entryPointMethodName) :
    m_hostHandle(hostHandle),
    m_domainId(domainId),
    m_entryPointAssemblyName(entryPointAssemblyName),
    m_entryPointTypeName(entryPointTypeName),
    m_entryPointMethodName(entryPointMethodName),
    initialized(true)
    {}

    coreclr_t::host_handle_t m_hostHandle;
    unsigned int             m_domainId;
    std::string              m_entryPointAssemblyName;
    std::string              m_entryPointTypeName;
    std::string              m_entryPointMethodName;
    bool initialized;

    void Echo()
    {
        MockLog("Delegate called");

        if (!initialized)
        {
            MockLog("ERROR called unitialized delegate!!!");
            return;
        }

        MockLogArg(m_hostHandle);
        MockLogArg(m_domainId);
        MockLogArg(m_entryPointAssemblyName);
        MockLogArg(m_entryPointTypeName);
        MockLogArg(m_entryPointMethodName);
    }
};

typedef void (*CoreClrDelegate)();

const int MaxDelegates = 16;

static MockCoreClrDelegate DelegateState[MaxDelegates];

#define DelegateFunction(index)\
void Delegate_ ## index() { DelegateState[index].Echo(); }

DelegateFunction(0);
DelegateFunction(1);
DelegateFunction(2);
DelegateFunction(3);
DelegateFunction(4);
DelegateFunction(5);
DelegateFunction(6);
DelegateFunction(7);
DelegateFunction(8);
DelegateFunction(9);
DelegateFunction(10);
DelegateFunction(11);
DelegateFunction(12);
DelegateFunction(13);
DelegateFunction(14);
DelegateFunction(15);

#undef DelegateFunction

// Prototype of the coreclr_create_delegate function from coreclr.dll
SHARED_API pal::hresult_t STDMETHODCALLTYPE coreclr_create_delegate(
    coreclr_t::host_handle_t hostHandle,
    unsigned int domainId,
    const char* entryPointAssemblyName,
    const char* entryPointTypeName,
    const char* entryPointMethodName,
    void** delegate)
{
    MockLog("coreclr_create_delegate() called");
    MockLogArg(hostHandle);
    MockLogArg(domainId);
    MockLogArg(entryPointAssemblyName);
    MockLogArg(entryPointTypeName);
    MockLogArg(entryPointMethodName);
    MockLogArg(delegate);

    static int nextDelegate = 0;
    static CoreClrDelegate delegates[] =
    {
        Delegate_0,
        Delegate_1,
        Delegate_2,
        Delegate_3,
        Delegate_4,
        Delegate_5,
        Delegate_6,
        Delegate_7,
        Delegate_8,
        Delegate_9,
        Delegate_10,
        Delegate_11,
        Delegate_12,
        Delegate_13,
        Delegate_14,
        Delegate_15
    };

    int delegateIndex = (nextDelegate++);

    while (delegateIndex >= MaxDelegates)
    {
        delegateIndex -= MaxDelegates;
        MockLog("MaxDelegates exceeded recycling older ones");
    }

    MockCoreClrDelegate delegateState(hostHandle, domainId, entryPointAssemblyName, entryPointTypeName, entryPointMethodName);

    DelegateState[delegateIndex] = delegateState;

    *delegate = reinterpret_cast<void*>(delegates[delegateIndex]);

    return StatusCode::Success;
}
