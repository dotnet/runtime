// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

enum FatalErrorHandlerResult : int32_t
{
    RunDefaultHandler = 0,
    SkipDefaultHandler = 1,
};

#if defined(_MSC_VER) && defined(_M_IX86)
#define DOTNET_CALLCONV __stdcall
#else
#define DOTNET_CALLCONV
#endif

struct FatalErrorInfo
{
    size_t size;    // size of the FatalErrorInfo instance
    void*  address; // code location correlated with the failure (i.e. location where FailFast was called)

    // exception/signal information, if available
    void* info;     // Cast to PEXCEPTION_RECORD on Windows or siginfo_t* on non-Windows.
    void* context;  // Cast to PCONTEXT on Windows or ucontext_t* on non-Windows.

    // An entry point for logging additional information about the crash.
    // As runtime finds information suitable for logging, it will invoke pfnLogAction and pass the information in logString.
    // The callback may be called multiple times.
    // Combined, the logString will contain the same parts as in the console output of the default crash handler.
    // The errorLog string will have UTF-8 encoding.
    void (DOTNET_CALLCONV *pfnGetFatalErrorLog)(
           FatalErrorInfo* errorData, 
           void (DOTNET_CALLCONV *pfnLogAction)(char8_t* logString, void *userContext), 
           void* userContext);

    // More information can be exposed for querying in the future by adding
    // entry points with similar pattern as in pfnGetFatalErrorLog
};
