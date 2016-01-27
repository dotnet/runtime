// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

class DebugClient : public IDebugClient
{
private:
    lldb::SBDebugger &m_debugger;
    lldb::SBCommandReturnObject &m_returnObject;

    lldb::SBProcess *m_currentProcess;
    lldb::SBThread *m_currentThread;

    void OutputString(ULONG mask, PCSTR str);
    ULONG64 GetModuleBase(lldb::SBTarget& target, lldb::SBModule& module);
    DWORD_PTR GetExpression(lldb::SBFrame& frame, lldb::SBError& error, PCSTR exp);
    void GetContextFromFrame(lldb::SBFrame& frame, DT_CONTEXT *dtcontext);
    DWORD_PTR GetRegister(lldb::SBFrame& frame, const char *name);

    lldb::SBProcess GetCurrentProcess();
    lldb::SBThread GetCurrentThread();
    lldb::SBFrame GetCurrentFrame();

public:
    DebugClient(lldb::SBDebugger &debugger, lldb::SBCommandReturnObject &returnObject, lldb::SBProcess *process = nullptr, lldb::SBThread *thread = nullptr);
    ~DebugClient();

    //----------------------------------------------------------------------------
    // IDebugControl2
    //----------------------------------------------------------------------------

    HRESULT GetInterrupt();

    HRESULT Output(
        ULONG mask,
        PCSTR format,
        ...);

    HRESULT OutputVaList(
        ULONG mask,
        PCSTR format,
        va_list args);

    HRESULT ControlledOutput(
        ULONG outputControl,
        ULONG mask,
        PCSTR format,
        ...);

    HRESULT ControlledOutputVaList(
        ULONG outputControl,
        ULONG mask,
        PCSTR format,
        va_list args);

    HRESULT GetDebuggeeType(
        PULONG debugClass,
        PULONG qualifier);

    HRESULT GetPageSize(
        PULONG size);

    HRESULT GetExecutingProcessorType(
        PULONG type);

    HRESULT Execute(
        ULONG outputControl,
        PCSTR command,
        ULONG flags);

    HRESULT GetLastEventInformation(
        PULONG type,
        PULONG processId,
        PULONG threadId,
        PVOID extraInformation,
        ULONG extraInformationSize,
        PULONG extraInformationUsed,
        PSTR description,
        ULONG descriptionSize,
        PULONG descriptionUsed);

    //----------------------------------------------------------------------------
    // IDebugDataSpaces
    //----------------------------------------------------------------------------

    HRESULT ReadVirtual(
        ULONG64 offset,
        PVOID buffer,
        ULONG bufferSize,
        PULONG bytesRead);

    HRESULT WriteVirtual(
        ULONG64 offset,
        PVOID buffer,
        ULONG bufferSize,
        PULONG bytesWritten);

    //----------------------------------------------------------------------------
    // IDebugSymbols
    //----------------------------------------------------------------------------

    HRESULT GetSymbolOptions(
        PULONG options);

    HRESULT GetNameByOffset(
        ULONG64 offset,
        PSTR nameBuffer,
        ULONG nameBufferSize,
        PULONG nameSize,
        PULONG64 displacement);

    HRESULT GetNumberModules(
        PULONG loaded,
        PULONG unloaded);

    HRESULT GetModuleByIndex(
        ULONG index,
        PULONG64 base);

    HRESULT GetModuleByModuleName(
        PCSTR name,
        ULONG startIndex,
        PULONG index,
        PULONG64 base);

    HRESULT GetModuleByOffset(
        ULONG64 offset,
        ULONG startIndex,
        PULONG index,
        PULONG64 base);

    HRESULT GetModuleNames(
        ULONG index,
        ULONG64 base,
        PSTR imageNameBuffer,
        ULONG imageNameBufferSize,
        PULONG imageNameSize,
        PSTR moduleNameBuffer,
        ULONG moduleNameBufferSize,
        PULONG moduleNameSize,
        PSTR loadedImageNameBuffer,
        ULONG loadedImageNameBufferSize,
        PULONG loadedImageNameSize);

    PCSTR GetModuleDirectory(
        PCSTR name);

    //----------------------------------------------------------------------------
    // IDebugSystemObjects
    //----------------------------------------------------------------------------

    HRESULT GetCurrentProcessId(
        PULONG id);

    HRESULT GetCurrentThreadId(
        PULONG id);

    HRESULT SetCurrentThreadId(
        ULONG id);

    HRESULT GetCurrentThreadSystemId(
        PULONG sysId);

    HRESULT GetThreadIdBySystemId(
        ULONG sysId,
        PULONG threadId);

    HRESULT GetThreadContextById(
        ULONG32 threadID,
        ULONG32 contextFlags,
        ULONG32 contextSize,
        PBYTE context);

    //----------------------------------------------------------------------------
    // IDebugRegisters
    //----------------------------------------------------------------------------

    HRESULT GetValueByName(
        PCSTR name,
        PDWORD_PTR debugValue);

    HRESULT GetInstructionOffset(
        PULONG64 offset);

    HRESULT GetStackOffset(
        PULONG64 offset);

    HRESULT GetFrameOffset(
        PULONG64 offset);

    //----------------------------------------------------------------------------
    // IDebugClient
    //----------------------------------------------------------------------------

    PCSTR GetCoreClrDirectory();

    DWORD_PTR GetExpression(
        PCSTR exp);

    HRESULT VirtualUnwind(
        DWORD threadID,
        ULONG32 contextSize,
        PBYTE context);

    HRESULT SetExceptionCallback(
        PFN_EXCEPTION_CALLBACK callback);

    HRESULT ClearExceptionCallback();
};
