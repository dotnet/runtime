//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include <cstdarg>
#include <cstdlib>
#include "sosplugin.h"
#include <string.h>
#include <dbgtargetcontext.h>
#include <string>

DebugClient::DebugClient(lldb::SBDebugger &debugger, lldb::SBCommandReturnObject &returnObject) :
    m_debugger(debugger),
    m_returnObject(returnObject)
{
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

// Internal output string function
void
DebugClient::OutputString(
    ULONG mask,
    PCSTR str)
{
    if (mask & DEBUG_OUTPUT_ERROR)
    {
        m_returnObject.SetError(str);
    }
    else 
    {
        // Can not use AppendMessage or AppendWarning because they add a newline.
        m_returnObject.Printf("%s", str);
    }
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
    if (bytesWritten)
    {
        *bytesWritten = 0;
    }
    return E_NOTIMPL;
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
    lldb::SBTarget target,
    lldb::SBModule module)
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
DebugClient::GetCurrentThreadId(
    PULONG id)
{
    lldb::SBThread thread = GetCurrentThread();
    if (!thread.IsValid())
    {
        *id = 0;
        return E_FAIL;
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
    lldb::SBThread thread = GetCurrentThread();
    if (!thread.IsValid())
    {
        *sysId = 0;
        return E_FAIL;
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

    process = GetCurrentProcess();
    if (!process.IsValid())
    {
        goto exit;
    }

    thread = process.GetThreadByID(sysId);
    if (!thread.IsValid())
    {
        goto exit;
    }

    id = thread.GetIndexID();
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

    if (contextSize < sizeof(DT_CONTEXT))
    {
        goto exit;
    }
    memset(context, 0, contextSize);

    process = GetCurrentProcess();
    if (!process.IsValid())
    {
        goto exit;
    }

    thread = process.GetThreadByID(threadID);
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

    hr = S_OK;

exit:
    return hr;
}

// Internal function
DWORD_PTR 
DebugClient::GetRegister(lldb::SBFrame frame, const char *name)
{
    lldb::SBValue regValue = frame.FindRegister(name);

    lldb::SBError error;
    DWORD_PTR result = regValue.GetValueAsUnsigned(error);

#ifdef _DEBUG
    if (!regValue.IsValid() || error.Fail())
    {
        Output(DEBUG_OUTPUT_ERROR, "Invalid register name '%s'\n", name);
    }
#endif

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
    lldb::SBFrame frame,
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

//----------------------------------------------------------------------------
// Helper functions
//----------------------------------------------------------------------------

lldb::SBProcess
DebugClient::GetCurrentProcess()
{
    lldb::SBProcess process;

    lldb::SBTarget target = m_debugger.GetSelectedTarget();
    if (target.IsValid())
    {
        process = target.GetProcess();
    }

    return process;
}

lldb::SBThread 
DebugClient::GetCurrentThread()
{
    lldb::SBThread thread;

    lldb::SBProcess process = GetCurrentProcess();
    if (process.IsValid())
    {
        thread = process.GetSelectedThread();
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
