// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <cstdarg>
#include <cstdlib>
#include "sosplugin.h"
#include <string.h>
#include <string>

ULONG g_currentThreadIndex = -1;
ULONG g_currentThreadSystemId = -1;
char *g_coreclrDirectory;

DebugClient::DebugClient(lldb::SBDebugger &debugger, lldb::SBCommandReturnObject &returnObject, lldb::SBProcess *process, lldb::SBThread *thread) : 
    m_debugger(debugger),
    m_returnObject(returnObject),
    m_currentProcess(process),
    m_currentThread(thread)
{
    returnObject.SetStatus(lldb::eReturnStatusSuccessFinishResult);
}

DebugClient::~DebugClient()
{
}

//----------------------------------------------------------------------------
// IDebugControl2
//----------------------------------------------------------------------------

// Checks for a user interrupt, such a Ctrl-C
// or stop button.
// This method is reentrant.
HRESULT 
DebugClient::GetInterrupt()
{
    return E_FAIL;
}

// Sends output through clients
// output callbacks if the mask is allowed
// by the current output control mask and
// according to the output distribution
// settings.
HRESULT 
DebugClient::Output(
    ULONG mask,
    PCSTR format,
    ...)
{
    va_list args;
    va_start (args, format);
    HRESULT result = OutputVaList(mask, format, args);
    va_end (args);
    return result;
}

HRESULT 
DebugClient::OutputVaList(
    ULONG mask,
    PCSTR format,
    va_list args)
{
    HRESULT result = S_OK;
    char str[1024];

    va_list args_copy;
    va_copy (args_copy, args);

    // Try and format our string into a fixed buffer first and see if it fits
    size_t length = ::vsnprintf(str, sizeof(str), format, args);
    if (length < sizeof(str))
    {
        OutputString(mask, str);
    }
    else
    {
        // Our stack buffer wasn't big enough to contain the entire formatted
        // string, so lets let vasprintf create the string for us!
        char *str_ptr = nullptr;
        length = ::vasprintf(&str_ptr, format, args_copy);
        if (str_ptr)
        {
            OutputString(mask, str_ptr);
            ::free (str_ptr);
        }
        else
        {
            result = E_FAIL;
        }
    }

    va_end (args_copy);

    return result;
}

// The following methods allow direct control
// over the distribution of the given output
// for situations where something other than
// the default is desired.  These methods require
// extra work in the engine so they should
// only be used when necessary.
HRESULT 
DebugClient::ControlledOutput(
    ULONG outputControl,
    ULONG mask,
    PCSTR format,
    ...)
{
    va_list args;
    va_start (args, format);
    HRESULT result = ControlledOutputVaList(outputControl, mask, format, args);
    va_end (args);
    return result;
}

HRESULT 
DebugClient::ControlledOutputVaList(
    ULONG outputControl,
    ULONG mask,
    PCSTR format,
    va_list args)
{
    return OutputVaList(mask, format, args);
}

// Returns information about the debuggee such
// as user vs. kernel, dump vs. live, etc.
HRESULT 
DebugClient::GetDebuggeeType(
    PULONG debugClass,
    PULONG qualifier)
{
    *debugClass = DEBUG_CLASS_USER_WINDOWS; 
    *qualifier = 0;
    return S_OK;
}

// Returns the page size for the currently executing
// processor context.  The page size may vary between
// processor types.
HRESULT 
DebugClient::GetPageSize(
    PULONG size)
{
    *size = 4096;
    return S_OK;
}

HRESULT 
DebugClient::GetExecutingProcessorType(
    PULONG type)
{
    *type = IMAGE_FILE_MACHINE_AMD64;
    return S_OK;
}

HRESULT 
DebugClient::Execute(
    ULONG outputControl,
    PCSTR command,
    ULONG flags)
{
    lldb::SBCommandInterpreter interpreter = m_debugger.GetCommandInterpreter();

    lldb::SBCommandReturnObject result;
    lldb::ReturnStatus status = interpreter.HandleCommand(command, result);

    return status <= lldb::eReturnStatusSuccessContinuingResult ? S_OK : E_FAIL;
}

// PAL raise exception function and exception record pointer variable name
// See coreclr\src\pal\src\exception\seh-unwind.cpp for the details. This
// function depends on RtlpRaisException not being inlined or optimized.
#define FUNCTION_NAME "RtlpRaiseException"
#define VARIABLE_NAME "ExceptionRecord"

HRESULT 
DebugClient::GetLastEventInformation(
    PULONG type,
    PULONG processId,
    PULONG threadId,
    PVOID extraInformation,
    ULONG extraInformationSize,
    PULONG extraInformationUsed,
    PSTR description,
    ULONG descriptionSize,
    PULONG descriptionUsed)
{
    if (extraInformationSize < sizeof(DEBUG_LAST_EVENT_INFO_EXCEPTION) || 
        type == NULL || processId == NULL || threadId == NULL || extraInformationUsed == NULL) 
    {
        return E_FAIL;
    }

    *type = DEBUG_EVENT_EXCEPTION;
    *processId = 0;
    *threadId = 0;
    *extraInformationUsed = sizeof(DEBUG_LAST_EVENT_INFO_EXCEPTION);

    DEBUG_LAST_EVENT_INFO_EXCEPTION *pdle = (DEBUG_LAST_EVENT_INFO_EXCEPTION *)extraInformation;
    pdle->FirstChance = 1; 

    lldb::SBProcess process = GetCurrentProcess();
    if (!process.IsValid())
    {
        return E_FAIL;
    }
    lldb::SBThread thread = GetCurrentThread();
    if (!thread.IsValid())
    {
        return E_FAIL;
    }

    *processId = process.GetProcessID();
    *threadId = thread.GetThreadID();

    // Enumerate each stack frame at the special "throw"
    // breakpoint and find the raise exception function 
    // with the exception record parameter.
    int numFrames = thread.GetNumFrames();
    for (int i = 0; i < numFrames; i++)
    {
        lldb::SBFrame frame = thread.GetFrameAtIndex(i);
        if (!frame.IsValid())
        {
            break;
        }

        const char *functionName = frame.GetFunctionName();
        if (functionName == NULL || strncmp(functionName, FUNCTION_NAME, sizeof(FUNCTION_NAME) - 1) != 0)
        {
            continue;
        }

        lldb::SBValue exValue = frame.FindVariable(VARIABLE_NAME);
        if (!exValue.IsValid())
        {
            break;
        }

        lldb::SBError error;
        ULONG64 pExceptionRecord = exValue.GetValueAsUnsigned(error);
        if (error.Fail())
        {
            break;
        }

        process.ReadMemory(pExceptionRecord, &pdle->ExceptionRecord, sizeof(pdle->ExceptionRecord), error);
        if (error.Fail())
        {
            break;
        }

        return S_OK;
    }

    return E_FAIL;
}

// Internal output string function
void
DebugClient::OutputString(
    ULONG mask,
    PCSTR str)
{
    if (mask == DEBUG_OUTPUT_ERROR)
    {
        m_returnObject.SetStatus(lldb::eReturnStatusFailed);
    }
    // Can not use AppendMessage or AppendWarning because they add a newline. SetError
    // can not be used for DEBUG_OUTPUT_ERROR mask because it caches the error strings
    // seperately from the normal output so error/normal texts are not intermixed 
    // correctly.
    m_returnObject.Printf("%s", str);
}

//----------------------------------------------------------------------------
// IDebugDataSpaces
//----------------------------------------------------------------------------

HRESULT 
DebugClient::ReadVirtual(
    ULONG64 offset,
    PVOID buffer,
    ULONG bufferSize,
    PULONG bytesRead)
{
    lldb::SBError error;
    size_t read = 0;

    lldb::SBProcess process = GetCurrentProcess();
    if (!process.IsValid())
    {
        goto exit;
    }

    read = process.ReadMemory(offset, buffer, bufferSize, error);

exit:
    if (bytesRead)
    {
        *bytesRead = read;
    }
    return error.Success() ? S_OK : E_FAIL;
}

HRESULT 
DebugClient::WriteVirtual(
    ULONG64 offset,
    PVOID buffer,
    ULONG bufferSize,
    PULONG bytesWritten)
{
    lldb::SBError error;
    size_t written = 0;

    lldb::SBProcess process = GetCurrentProcess();
    if (!process.IsValid())
    {
        goto exit;
    }

    written = process.WriteMemory(offset, buffer, bufferSize, error);

exit:
    if (bytesWritten)
    {
        *bytesWritten = written;
    }
    return error.Success() ? S_OK : E_FAIL;
}

//----------------------------------------------------------------------------
// IDebugSymbols
//----------------------------------------------------------------------------

HRESULT 
DebugClient::GetSymbolOptions(
    PULONG options)
{
    *options = 0;
    return S_OK;
}

HRESULT 
DebugClient::GetNameByOffset(
    ULONG64 offset,
    PSTR nameBuffer,
    ULONG nameBufferSize,
    PULONG nameSize,
    PULONG64 displacement)
{
    HRESULT hr = E_FAIL;
    ULONG64 disp = 0;

    lldb::SBTarget target;
    lldb::SBAddress address;
    lldb::SBModule module;
    lldb::SBFileSpec file;
    lldb::SBSymbol symbol;
    std::string str;

    target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        goto exit;
    }

    address = target.ResolveLoadAddress(offset);
    if (!address.IsValid())
    {
        goto exit;
    }

    module = address.GetModule();
    if (!module.IsValid())
    {
        goto exit;
    }

    file = module.GetFileSpec();
    if (file.IsValid())
    {
        str.append(file.GetFilename());
    }

    symbol = address.GetSymbol();
    if (symbol.IsValid())
    {
        lldb::SBAddress startAddress = symbol.GetStartAddress();
        disp = address.GetOffset() - startAddress.GetOffset();

        const char *name = symbol.GetName();
        if (name)
        {
            if (file.IsValid())
            {
                str.append("!");
            }
            str.append(name);
        }
    }

    str.append(1, '\0');
    hr = S_OK;

exit:
    if (nameSize)
    {
        *nameSize = str.length();
    }
    if (nameBuffer)
    {
        str.copy(nameBuffer, nameBufferSize);
    }
    if (displacement)
    {
        *displacement = disp;
    }
    return hr;
}

HRESULT 
DebugClient::GetNumberModules(
    PULONG loaded,
    PULONG unloaded)
{
    ULONG numModules = 0;
    HRESULT hr = S_OK;

    lldb::SBTarget target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        hr = E_FAIL;
        goto exit;
    }

    numModules = target.GetNumModules();

exit:
    if (loaded)
    {
        *loaded = numModules;
    }
    if (unloaded)
    {
        *unloaded = 0;
    }
    return hr;
}

HRESULT DebugClient::GetModuleByIndex(
    ULONG index,
    PULONG64 base)
{
    ULONG64 moduleBase = UINT64_MAX;

    lldb::SBTarget target;
    lldb::SBModule module;
    
    target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        goto exit;
    }

    module = target.GetModuleAtIndex(index);
    if (!module.IsValid())
    {
        goto exit;
    }

    moduleBase = GetModuleBase(target, module);

exit:
    if (base)
    {
        *base = moduleBase;
    }
    return moduleBase == UINT64_MAX ? E_FAIL : S_OK;
}

HRESULT 
DebugClient::GetModuleByModuleName(
    PCSTR name,
    ULONG startIndex,
    PULONG index,
    PULONG64 base)
{
    ULONG64 moduleBase = UINT64_MAX;
    ULONG moduleIndex = UINT32_MAX;

    lldb::SBTarget target;
    lldb::SBModule module;
    lldb::SBFileSpec fileSpec;
    fileSpec.SetFilename(name);

    target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        goto exit;
    }

    module = target.FindModule(fileSpec);
    if (!module.IsValid())
    {
        goto exit;
    }

    moduleBase = GetModuleBase(target, module);

    if (index)
    {
        int numModules = target.GetNumModules();
        for (int mi = startIndex; mi < numModules; mi++)
        {
            lldb::SBModule mod = target.GetModuleAtIndex(mi);
            if (module == mod)
            {
                moduleIndex = mi;
                break;
            }
        }
    }

exit:
    if (index)
    {
        *index = moduleIndex;
    }
    if (base)
    {
        *base = moduleBase;
    }
    return moduleBase == UINT64_MAX ? E_FAIL : S_OK;
}

HRESULT 
DebugClient::GetModuleByOffset(
    ULONG64 offset,
    ULONG startIndex,
    PULONG index,
    PULONG64 base)
{
    ULONG64 moduleBase = UINT64_MAX;
    ULONG moduleIndex = UINT32_MAX;

    lldb::SBTarget target;
    int numModules;

    target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        goto exit;
    }

    numModules = target.GetNumModules();
    for (int mi = startIndex; mi < numModules; mi++)
    {
        lldb::SBModule module = target.GetModuleAtIndex(mi);

        int numSections = module.GetNumSections();
        for (int si = 0; si < numSections; si++)
        {
            lldb::SBSection section = module.GetSectionAtIndex(si);
            if (section.IsValid())
            {
                lldb::addr_t baseAddress = section.GetLoadAddress(target);
                if (baseAddress != LLDB_INVALID_ADDRESS)
                {
                    if (offset > baseAddress)
                    {
                        if ((offset - baseAddress) < section.GetByteSize())
                        {
                            moduleIndex = mi;
                            moduleBase = baseAddress - section.GetFileOffset();
                            goto exit;
                        }
                    }
                }
            }
        }
    }

exit:
    if (index)
    {
        *index = moduleIndex;
    }
    if (base)
    {
        *base = moduleBase;
    }
    return moduleBase == UINT64_MAX ? E_FAIL : S_OK;
}

HRESULT 
DebugClient::GetModuleNames(
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
    PULONG loadedImageNameSize)
{
    HRESULT hr = S_OK;
    lldb::SBTarget target;
    lldb::SBFileSpec fileSpec;

    target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        hr = E_FAIL;
        goto exit;
    }

    if (index != DEBUG_ANY_ID)
    {
        lldb::SBModule module = target.GetModuleAtIndex(index);
        if (module.IsValid())
        {
            fileSpec = module.GetFileSpec();
        }
    }
    else
    {
        int numModules = target.GetNumModules();
        for (int mi = 0; mi < numModules; mi++)
        {
            lldb::SBModule module = target.GetModuleAtIndex(mi);
            if (module.IsValid())
            {
                ULONG64 moduleBase = GetModuleBase(target, module);
                if (base == moduleBase)
                {
                    fileSpec = module.GetFileSpec();
                    break;
                }
            }
        }
    }

    if (!fileSpec.IsValid())
    {
        hr = E_FAIL;
        goto exit;
    }

exit:
    if (imageNameBuffer)
    {
        int size = fileSpec.GetPath(imageNameBuffer, imageNameBufferSize);
        if (imageNameSize)
        {
            *imageNameSize = size;
        }
    }
    if (moduleNameBuffer)
    {
        const char *fileName = fileSpec.GetFilename();
        if (fileName == NULL)
        {
            fileName = "";
        }
        stpncpy(moduleNameBuffer, fileName, moduleNameBufferSize);
        if (moduleNameSize)
        {
            *moduleNameSize = strlen(fileName);
        }
    }
    if (loadedImageNameBuffer)
    {
        int size = fileSpec.GetPath(loadedImageNameBuffer, loadedImageNameBufferSize);
        if (loadedImageNameSize)
        {
            *loadedImageNameSize = size;
        }
    }
    return hr;
}

PCSTR
DebugClient::GetModuleDirectory(
    PCSTR name)
{
    lldb::SBTarget target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        return NULL;
    }

    lldb::SBFileSpec fileSpec;
    fileSpec.SetFilename(name);

    lldb::SBModule module = target.FindModule(fileSpec);
    if (!module.IsValid())
    {
        return NULL;
    }

    return module.GetFileSpec().GetDirectory();
}

// Internal function
ULONG64
DebugClient::GetModuleBase(
    /* const */ lldb::SBTarget& target,
    /* const */ lldb::SBModule& module)
{
    // Find the first section with an valid base address
    int numSections = module.GetNumSections();
    for (int si = 0; si < numSections; si++)
    {
        lldb::SBSection section = module.GetSectionAtIndex(si);
        if (section.IsValid())
        {
            lldb::addr_t baseAddress = section.GetLoadAddress(target);
            if (baseAddress != LLDB_INVALID_ADDRESS)
            {
                return baseAddress - section.GetFileOffset();
            }
        }
    }

    return UINT64_MAX;
}

//----------------------------------------------------------------------------
// IDebugSystemObjects
//----------------------------------------------------------------------------

HRESULT 
DebugClient::GetCurrentProcessId(
    PULONG id)
{
    if (id == NULL)  
    {
        return E_INVALIDARG;
    }

    lldb::SBProcess process = GetCurrentProcess();
    if (!process.IsValid())
    {
        *id = 0;
        return E_FAIL;
    }

    *id = process.GetProcessID();
    return S_OK;
}

HRESULT 
DebugClient::GetCurrentThreadId(
    PULONG id)
{
    if (id == NULL)  
    {
        return E_INVALIDARG;
    }

    lldb::SBThread thread = GetCurrentThread();
    if (!thread.IsValid())
    {
        *id = 0;
        return E_FAIL;
    }

    // This is allow the a valid current TID to be returned to 
    // workaround a bug in lldb on core dumps.
    if (g_currentThreadIndex != -1)
    {
        *id = g_currentThreadIndex;
        return S_OK;
    }

    *id = thread.GetIndexID();
    return S_OK;
}

HRESULT 
DebugClient::SetCurrentThreadId(
    ULONG id)
{
    lldb::SBProcess process = GetCurrentProcess();
    if (!process.IsValid())
    {
        return E_FAIL;
    }

    if (!process.SetSelectedThreadByIndexID(id))
    {
        return E_FAIL;
    }

    return S_OK;
}

HRESULT 
DebugClient::GetCurrentThreadSystemId(
    PULONG sysId)
{
    if (sysId == NULL)  
    {
        return E_INVALIDARG;
    }

    lldb::SBThread thread = GetCurrentThread();
    if (!thread.IsValid())
    {
        *sysId = 0;
        return E_FAIL;
    }

    // This is allow the a valid current TID to be returned to 
    // workaround a bug in lldb on core dumps.
    if (g_currentThreadSystemId != -1)
    {
        *sysId = g_currentThreadSystemId;
        return S_OK;
    }

    *sysId = thread.GetThreadID();
    return S_OK;
}

HRESULT 
DebugClient::GetThreadIdBySystemId(
    ULONG sysId,
    PULONG threadId)
{
    HRESULT hr = E_FAIL;
    ULONG id = 0;

    lldb::SBProcess process;
    lldb::SBThread thread;

    if (threadId == NULL)  
    {
        return E_INVALIDARG;
    }

    process = GetCurrentProcess();
    if (!process.IsValid())
    {
        goto exit;
    }

    // If we have a "fake" thread OS (system) id and a fake thread index,
    // we need to return fake thread index.
    if (g_currentThreadSystemId == sysId && g_currentThreadIndex != -1)
    {
        id = g_currentThreadIndex;
    }
    else
    {
        thread = process.GetThreadByID(sysId);
        if (!thread.IsValid())
        {
            goto exit;
        }

        id = thread.GetIndexID();
    }
    hr = S_OK;

exit:
    *threadId = id;
    return hr;
}

HRESULT 
DebugClient::GetThreadContextById(
    /* in */ ULONG32 threadID,
    /* in */ ULONG32 contextFlags,
    /* in */ ULONG32 contextSize,
    /* out */ PBYTE context)
{
    lldb::SBProcess process;
    lldb::SBThread thread;
    lldb::SBFrame frame;
    DT_CONTEXT *dtcontext;
    HRESULT hr = E_FAIL;

    if (context == NULL || contextSize < sizeof(DT_CONTEXT))
    {
        goto exit;
    }
    memset(context, 0, contextSize);

    process = GetCurrentProcess();
    if (!process.IsValid())
    {
        goto exit;
    }

    // If we have a "fake" thread OS (system) id and a fake thread index,
    // use the fake thread index to get the context.
    if (g_currentThreadSystemId == threadID && g_currentThreadIndex != -1)
    {
        thread = process.GetThreadByIndexID(g_currentThreadIndex);
    }
    else
    {
        thread = process.GetThreadByID(threadID);
    }
    
    if (!thread.IsValid())
    {
        goto exit;
    }

    frame = thread.GetFrameAtIndex(0);
    if (!frame.IsValid())
    {
        goto exit;
    }

    dtcontext = (DT_CONTEXT*)context;
    dtcontext->ContextFlags = contextFlags;

    GetContextFromFrame(frame, dtcontext);
    hr = S_OK;

exit:
    return hr;
}

// Internal function
void
DebugClient::GetContextFromFrame(
    /* const */ lldb::SBFrame& frame,
    DT_CONTEXT *dtcontext)
{
#ifdef DBG_TARGET_AMD64
    dtcontext->Rip = frame.GetPC();
    dtcontext->Rsp = frame.GetSP();
    dtcontext->Rbp = frame.GetFP();
    dtcontext->EFlags = GetRegister(frame, "rflags");

    dtcontext->Rax = GetRegister(frame, "rax");
    dtcontext->Rbx = GetRegister(frame, "rbx");
    dtcontext->Rcx = GetRegister(frame, "rcx");
    dtcontext->Rdx = GetRegister(frame, "rdx");
    dtcontext->Rsi = GetRegister(frame, "rsi");
    dtcontext->Rdi = GetRegister(frame, "rdi");
    dtcontext->R8 = GetRegister(frame, "r8");
    dtcontext->R9 = GetRegister(frame, "r9");
    dtcontext->R10 = GetRegister(frame, "r10");
    dtcontext->R11 = GetRegister(frame, "r11");
    dtcontext->R12 = GetRegister(frame, "r12");
    dtcontext->R13 = GetRegister(frame, "r13");
    dtcontext->R14 = GetRegister(frame, "r14");
    dtcontext->R15 = GetRegister(frame, "r15");

    dtcontext->SegCs = GetRegister(frame, "cs");
    dtcontext->SegSs = GetRegister(frame, "ss");
    dtcontext->SegDs = GetRegister(frame, "ds");
    dtcontext->SegEs = GetRegister(frame, "es");
    dtcontext->SegFs = GetRegister(frame, "fs");
    dtcontext->SegGs = GetRegister(frame, "gs");
#elif DBG_TARGET_ARM
    dtcontext->Pc = frame.GetPC();
    dtcontext->Sp = frame.GetSP();
    dtcontext->Lr = GetRegister(frame, "lr");
    dtcontext->Cpsr = GetRegister(frame, "cpsr");

    dtcontext->R0 = GetRegister(frame, "r0");
    dtcontext->R1 = GetRegister(frame, "r1");
    dtcontext->R2 = GetRegister(frame, "r2");
    dtcontext->R3 = GetRegister(frame, "r3");
    dtcontext->R4 = GetRegister(frame, "r4");
    dtcontext->R5 = GetRegister(frame, "r5");
    dtcontext->R6 = GetRegister(frame, "r6");
    dtcontext->R7 = GetRegister(frame, "r7");
    dtcontext->R8 = GetRegister(frame, "r8");
    dtcontext->R9 = GetRegister(frame, "r9");
    dtcontext->R10 = GetRegister(frame, "r10");
    dtcontext->R11 = GetRegister(frame, "r11");
    dtcontext->R12 = GetRegister(frame, "r12");
#endif
}

// Internal function
DWORD_PTR 
DebugClient::GetRegister(
    /* const */ lldb::SBFrame& frame,
    const char *name)
{
    lldb::SBValue regValue = frame.FindRegister(name);

    lldb::SBError error;
    DWORD_PTR result = regValue.GetValueAsUnsigned(error);

    return result;
}

//----------------------------------------------------------------------------
// IDebugRegisters
//----------------------------------------------------------------------------

HRESULT
DebugClient::GetValueByName(
    PCSTR name,
    PDWORD_PTR debugValue)
{
    lldb::SBFrame frame = GetCurrentFrame();
    if (!frame.IsValid())
    {
        *debugValue = 0;
        return E_FAIL;
    }

    lldb::SBValue value = frame.FindRegister(name);
    if (!value.IsValid())
    {
        *debugValue = 0;
        return E_FAIL;
    }

    *debugValue = value.GetValueAsUnsigned();
    return S_OK;
}

HRESULT 
DebugClient::GetInstructionOffset(
    PULONG64 offset)
{
    lldb::SBFrame frame = GetCurrentFrame();
    if (!frame.IsValid())
    {
        *offset = 0;
        return E_FAIL;
    }

    *offset = frame.GetPC();
    return S_OK;
}

HRESULT 
DebugClient::GetStackOffset(
    PULONG64 offset)
{
    lldb::SBFrame frame = GetCurrentFrame();
    if (!frame.IsValid())
    {
        *offset = 0;
        return E_FAIL;
    }

    *offset = frame.GetSP();
    return S_OK;
}

HRESULT 
DebugClient::GetFrameOffset(
    PULONG64 offset)
{
    lldb::SBFrame frame = GetCurrentFrame();
    if (!frame.IsValid())
    {
        *offset = 0;
        return E_FAIL;
    }

    *offset = frame.GetFP();
    return S_OK;
}

//----------------------------------------------------------------------------
// IDebugClient
//----------------------------------------------------------------------------

PCSTR
DebugClient::GetCoreClrDirectory()
{
    return g_coreclrDirectory;
}

DWORD_PTR
DebugClient::GetExpression(
    PCSTR exp)
{
    if (exp == nullptr)
    {
        return 0;
    }

    lldb::SBFrame frame = GetCurrentFrame();
    if (!frame.IsValid())
    {
        return 0;
    }

    DWORD_PTR result = 0;
    lldb::SBError error;
    std::string str;

    // To be compatible with windbg/dbgeng, we need to emulate the default
    // hex radix (because sos prints addresses and other hex values without
    // the 0x) by first prepending 0x and if that fails use the actual
    // undecorated expression.
    str.append("0x");
    str.append(exp);

    result = GetExpression(frame, error, str.c_str());
    if (error.Fail())
    {
        result = GetExpression(frame, error, exp);
    }

    return result;
}

// Internal function
DWORD_PTR 
DebugClient::GetExpression(
    /* const */ lldb::SBFrame& frame,
    lldb::SBError& error,
    PCSTR exp)
{
    DWORD_PTR result = 0;

    lldb::SBValue value = frame.EvaluateExpression(exp, lldb::eNoDynamicValues);
    if (value.IsValid())
    {
        result = value.GetValueAsUnsigned(error);
    }

    return result;
}

HRESULT 
DebugClient::VirtualUnwind(
    DWORD threadID,
    ULONG32 contextSize,
    PBYTE context)
{
    lldb::SBProcess process;
    lldb::SBThread thread;

    if (context == NULL || contextSize < sizeof(DT_CONTEXT))
    {
        return E_FAIL;
    }

    process = GetCurrentProcess();
    if (!process.IsValid())
    {
        return E_FAIL;
    }

    thread = process.GetThreadByID(threadID);
    if (!thread.IsValid())
    {
        return E_FAIL;
    }

    DT_CONTEXT *dtcontext = (DT_CONTEXT*)context;
    lldb::SBFrame frameFound;

#ifdef DBG_TARGET_AMD64
    DWORD64 spToFind = dtcontext->Rsp;
#elif DBG_TARGET_ARM
    DWORD spToFind = dtcontext->Sp;
#endif
    
    int numFrames = thread.GetNumFrames();
    for (int i = 0; i < numFrames; i++)
    {
        lldb::SBFrame frame = thread.GetFrameAtIndex(i);
        if (!frame.IsValid())
        {
            break;
        }

        lldb::addr_t sp = frame.GetSP();

        if (sp == spToFind && (i + 1) < numFrames)
        {
            // Get next frame after finding the match
            frameFound = thread.GetFrameAtIndex(i + 1);
            break;
        }
    }

    if (!frameFound.IsValid())
    {
        return E_FAIL;
    }

    GetContextFromFrame(frameFound, dtcontext);

    return S_OK;
}

bool 
ExceptionBreakpointCallback(
    void *baton, 
    lldb::SBProcess &process,
    lldb::SBThread &thread, 
    lldb::SBBreakpointLocation &location)
{
    lldb::SBDebugger debugger = process.GetTarget().GetDebugger();

    // Send the normal and error output to stdout/stderr since we
    // don't have a return object from the command interpreter.
    lldb::SBCommandReturnObject result;
    result.SetImmediateOutputFile(stdout);
    result.SetImmediateErrorFile(stderr);

    // Save the process and thread to be used by the current process/thread 
    // helper functions.
    DebugClient* client = new DebugClient(debugger, result, &process, &thread);
    return ((PFN_EXCEPTION_CALLBACK)baton)(client) == S_OK;
}

lldb::SBBreakpoint g_exceptionbp;

HRESULT 
DebugClient::SetExceptionCallback(
    PFN_EXCEPTION_CALLBACK callback)
{
    if (!g_exceptionbp.IsValid())
    {
        lldb::SBTarget target = m_debugger.GetSelectedTarget();
        if (!target.IsValid())
        {
            return E_FAIL;
        }
        lldb::SBBreakpoint exceptionbp = target.BreakpointCreateForException(lldb::LanguageType::eLanguageTypeC_plus_plus, false, true);
        if (!exceptionbp.IsValid())
        {
            return E_FAIL;
        }
#ifdef FLAGS_ANONYMOUS_ENUM
        exceptionbp.AddName("DoNotDeleteOrDisable");
#endif
        exceptionbp.SetCallback(ExceptionBreakpointCallback, (void *)callback);
        g_exceptionbp = exceptionbp;
    }
    return S_OK;
}

HRESULT 
DebugClient::ClearExceptionCallback()
{
    if (g_exceptionbp.IsValid())
    {
        lldb::SBTarget target = m_debugger.GetSelectedTarget();
        if (!target.IsValid())
        {
            return E_FAIL;
        }
        target.BreakpointDelete(g_exceptionbp.GetID());
        g_exceptionbp = lldb::SBBreakpoint();
    }
    return S_OK;
}

//----------------------------------------------------------------------------
// Helper functions
//----------------------------------------------------------------------------

lldb::SBProcess
DebugClient::GetCurrentProcess()
{
    lldb::SBProcess process;

    if (m_currentProcess == nullptr)
    {
        lldb::SBTarget target = m_debugger.GetSelectedTarget();
        if (target.IsValid())
        {
            process = target.GetProcess();
        }
    }
    else
    {
        process = *m_currentProcess;
    }

    return process;
}

lldb::SBThread 
DebugClient::GetCurrentThread()
{
    lldb::SBThread thread;

    if (m_currentThread == nullptr)
    {
        lldb::SBProcess process = GetCurrentProcess();
        if (process.IsValid())
        {
            thread = process.GetSelectedThread();
        }
    }
    else
    {
        thread = *m_currentThread;
    }

    return thread;
}

lldb::SBFrame 
DebugClient::GetCurrentFrame()
{
    lldb::SBFrame frame;

    lldb::SBThread thread = GetCurrentThread();
    if (thread.IsValid())
    {
        frame = thread.GetSelectedFrame();
    }

    return frame;
}
