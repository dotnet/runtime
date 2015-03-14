//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

class DebugClient : public IDebugClient
{
private:
    lldb::SBDebugger &m_debugger;
    lldb::SBCommandReturnObject &m_returnObject;

    void OutputString(ULONG mask, PCSTR str);
    lldb::SBProcess GetCurrentProcess();
    lldb::SBThread GetCurrentThread();
    
public:
    DebugClient(lldb::SBDebugger &debugger, lldb::SBCommandReturnObject &returnObject);
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
        PULONG Type);

    //----------------------------------------------------------------------------
    // IDebugDataSpaces
    //----------------------------------------------------------------------------

    HRESULT ReadVirtual(
        ULONG64 offset,
        PVOID buffer,
        ULONG bufferSize,
        PULONG bytesRead);

    //----------------------------------------------------------------------------
    // IDebugSymbols
    //----------------------------------------------------------------------------

    HRESULT GetSymbolOptions(
        PULONG options);

    HRESULT GetNumberModules(
        PULONG loaded,
        PULONG unloaded);

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

    //----------------------------------------------------------------------------
    // IDebugSystemObjects
    //----------------------------------------------------------------------------

    HRESULT GetCurrentThreadId(
        PULONG id);

    HRESULT SetCurrentThreadId(
        ULONG id);

    HRESULT GetCurrentThreadSystemId(
        PULONG sysId);

    //----------------------------------------------------------------------------
    // IDebugClient
    //----------------------------------------------------------------------------

    HRESULT GetThreadContextById(
        ULONG32 threadID,
        ULONG32 contextFlags,
        ULONG32 contextSize,
        PBYTE context);
};
