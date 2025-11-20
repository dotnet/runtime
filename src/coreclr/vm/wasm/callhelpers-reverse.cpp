// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <callhelpers.hpp>

// Define reverse thunks here

// Entry point for interpreted method execution from unmanaged code
class MethodDesc;

// WASM-TODO: The method lookup would ideally be fully qualified assembly and then methodDef token.
// The current approach has limitations with overloaded methods.
extern "C" void LookupMethodByName(const char* fullQualifiedTypeName, const char* methodName, MethodDesc** ppMD);
extern "C" void ExecuteInterpretedMethodFromUnmanaged(MethodDesc* pMD, int8_t* args, size_t argSize, int8_t* ret);

static MethodDesc* MD_System_Private_CoreLib_System_Threading_ThreadPool_BackgroundJobHandler_Void_RetVoid = nullptr;
static void Call_System_Private_CoreLib_System_Threading_ThreadPool_BackgroundJobHandler()
{
    // Lazy lookup of MethodDesc for the function export scenario.
    if (!MD_System_Private_CoreLib_System_Threading_ThreadPool_BackgroundJobHandler_Void_RetVoid)
    {
        LookupMethodByName("System.Threading.ThreadPool, System.Private.CoreLib", "BackgroundJobHandler", &MD_System_Private_CoreLib_System_Threading_ThreadPool_BackgroundJobHandler_Void_RetVoid);
    }
    ExecuteInterpretedMethodFromUnmanaged(MD_System_Private_CoreLib_System_Threading_ThreadPool_BackgroundJobHandler_Void_RetVoid, nullptr, 0, nullptr);
}

extern "C" void SystemJS_ExecuteBackgroundJobCallback()
{
    Call_System_Private_CoreLib_System_Threading_ThreadPool_BackgroundJobHandler();
}

static MethodDesc* MD_System_Private_CoreLib_System_Threading_TimerQueue_TimerHandler_Void_RetVoid = nullptr;
static void Call_System_Private_CoreLib_System_Threading_TimerQueue_TimerHandler()
{
    // Lazy lookup of MethodDesc for the function export scenario.
    if (!MD_System_Private_CoreLib_System_Threading_TimerQueue_TimerHandler_Void_RetVoid)
    {
        LookupMethodByName("System.Threading.TimerQueue, System.Private.CoreLib", "TimerHandler", &MD_System_Private_CoreLib_System_Threading_TimerQueue_TimerHandler_Void_RetVoid);
    }
    ExecuteInterpretedMethodFromUnmanaged(MD_System_Private_CoreLib_System_Threading_TimerQueue_TimerHandler_Void_RetVoid, nullptr, 0, nullptr);
}

extern "C" void SystemJS_ExecuteTimerCallback()
{
    Call_System_Private_CoreLib_System_Threading_TimerQueue_TimerHandler();
}

extern const ReverseThunkMapEntry g_ReverseThunks[] =
{
    { 100678287, { &MD_System_Private_CoreLib_System_Threading_ThreadPool_BackgroundJobHandler_Void_RetVoid, (void*)&Call_System_Private_CoreLib_System_Threading_ThreadPool_BackgroundJobHandler } },
    { 100678363, { &MD_System_Private_CoreLib_System_Threading_TimerQueue_TimerHandler_Void_RetVoid, (void*)&Call_System_Private_CoreLib_System_Threading_TimerQueue_TimerHandler } },
};

const size_t g_ReverseThunksCount = sizeof(g_ReverseThunks) / sizeof(g_ReverseThunks[0]);
