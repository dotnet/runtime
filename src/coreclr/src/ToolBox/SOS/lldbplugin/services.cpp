// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <cstdarg>
#include <cstdlib>
#include "sosplugin.h"
#include <string.h>
#include <string>

#define CONVERT_FROM_SIGN_EXTENDED(offset) ((ULONG_PTR)(offset))

ULONG g_currentThreadIndex = -1;
ULONG g_currentThreadSystemId = -1;
char *g_coreclrDirectory;

LLDBServices::LLDBServices(lldb::SBDebugger &debugger, lldb::SBCommandReturnObject &returnObject, lldb::SBProcess *process, lldb::SBThread *thread) : 
    m_ref(1),
    m_debugger(debugger),
    m_returnObject(returnObject),
    m_currentProcess(process),
    m_currentThread(thread)
{
    returnObject.SetStatus(lldb::eReturnStatusSuccessFinishResult);
}

LLDBServices::~LLDBServices()
{
}

//----------------------------------------------------------------------------
// IUnknown
//----------------------------------------------------------------------------

HRESULT
LLDBServices::QueryInterface(
    REFIID InterfaceId,
    PVOID* Interface
    )
{
    if (InterfaceId == __uuidof(IUnknown) ||
        InterfaceId == __uuidof(ILLDBServices))
    {
        *Interface = (ILLDBServices*)this;
        AddRef();
        return S_OK;
    }
    else
    {
        *Interface = NULL;
        return E_NOINTERFACE;
    }
}

ULONG
LLDBServices::AddRef()
{
    LONG ref = InterlockedIncrement(&m_ref);    
    return ref;
}

ULONG
LLDBServices::Release()
{
    LONG ref = InterlockedDecrement(&m_ref);
    if (ref == 0)
    {
        delete this;
    }
    return ref;
}

//----------------------------------------------------------------------------
// ILLDBServices
//----------------------------------------------------------------------------

PCSTR
LLDBServices::GetCoreClrDirectory()
{
    return g_coreclrDirectory;
}

DWORD_PTR
LLDBServices::GetExpression(
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
LLDBServices::GetExpression(
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

//
// lldb doesn't have a way or API to unwind an arbitrary context (IP, SP)
// and return the next frame so we have to stick with the native frames
// lldb has found and find the closest frame to the incoming context SP.
//
HRESULT 
LLDBServices::VirtualUnwind(
    DWORD threadID,
    ULONG32 contextSize,
    PBYTE context)
{
    lldb::SBProcess process;
    lldb::SBThread thread;

    if (context == NULL || contextSize < sizeof(DT_CONTEXT))
    {
        return E_INVALIDARG;
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
#elif DBG_TARGET_X86
    DWORD spToFind = dtcontext->Esp;
#elif DBG_TARGET_ARM
    DWORD spToFind = dtcontext->Sp;
#elif DBG_TARGET_ARM64
    DWORD64 spToFind = dtcontext->Sp;
#else
#error "spToFind undefined for this platform"
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

        if ((i + 1) < numFrames)
        {
            lldb::SBFrame frameNext = thread.GetFrameAtIndex(i + 1);
            if (frameNext.IsValid())
            {
                lldb::addr_t spNext = frameNext.GetSP();

                // An exact match of the current frame's SP would be nice
                // but sometimes the incoming context is between lldb frames
                if (spToFind >= sp && spToFind < spNext)
                {
                    frameFound = frameNext;
                    break;
                }
            }
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
    LLDBServices* client = new LLDBServices(debugger, result, &process, &thread);
    return ((PFN_EXCEPTION_CALLBACK)baton)(client) == S_OK;
}

lldb::SBBreakpoint g_exceptionbp;

HRESULT 
LLDBServices::SetExceptionCallback(
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
LLDBServices::ClearExceptionCallback()
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
// IDebugControl2
//----------------------------------------------------------------------------

// Checks for a user interrupt, such a Ctrl-C
// or stop button.
// This method is reentrant.
HRESULT 
LLDBServices::GetInterrupt()
{
    return E_FAIL;
}

// Sends output through clients
// output callbacks if the mask is allowed
// by the current output control mask and
// according to the output distribution
// settings.
HRESULT 
LLDBServices::Output(
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
LLDBServices::OutputVaList(
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
LLDBServices::ControlledOutput(
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
LLDBServices::ControlledOutputVaList(
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
LLDBServices::GetDebuggeeType(
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
LLDBServices::GetPageSize(
    PULONG size)
{
    *size = 4096;
    return S_OK;
}

HRESULT 
LLDBServices::GetExecutingProcessorType(
    PULONG type)
{
#ifdef DBG_TARGET_AMD64
    *type = IMAGE_FILE_MACHINE_AMD64;
#elif DBG_TARGET_ARM
    *type = IMAGE_FILE_MACHINE_ARMNT;
#elif DBG_TARGET_ARM64
    *type = IMAGE_FILE_MACHINE_ARM64;
#elif DBG_TARGET_X86
    *type = IMAGE_FILE_MACHINE_I386;
#else
#error "Unsupported target"
#endif
    return S_OK;
}

HRESULT 
LLDBServices::Execute(
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
LLDBServices::GetLastEventInformation(
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
        return E_INVALIDARG;
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

HRESULT 
LLDBServices::Disassemble(
    ULONG64 offset,
    ULONG flags,
    PSTR buffer,
    ULONG bufferSize,
    PULONG disassemblySize,
    PULONG64 endOffset)
{
    lldb::SBInstruction instruction;
    lldb::SBInstructionList list;
    lldb::SBTarget target;
    lldb::SBAddress address;
    lldb::SBError error;
    lldb::SBData data;
    std::string str;
    HRESULT hr = S_OK;
    ULONG size = 0;
    uint8_t byte;
    int cch;

    // lldb doesn't expect sign-extended address
    offset = CONVERT_FROM_SIGN_EXTENDED(offset);

    if (buffer == NULL)
    {
        hr = E_INVALIDARG;
        goto exit;
    }
    *buffer = 0;

    target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        hr = E_INVALIDARG;
        goto exit;
    }
    address = target.ResolveLoadAddress(offset);
    if (!address.IsValid())
    {
        hr = E_INVALIDARG;
        goto exit;
    }
    list = target.ReadInstructions(address, 1, "intel");
    if (!list.IsValid())
    {
        hr = E_FAIL;
        goto exit;
    }
    instruction = list.GetInstructionAtIndex(0);
    if (!instruction.IsValid())
    {
        hr = E_FAIL;
        goto exit;
    }
    cch = snprintf(buffer, bufferSize, "%016llx ", (unsigned long long)offset);
    buffer += cch;
    bufferSize -= cch;

    size = instruction.GetByteSize();
    data = instruction.GetData(target);
    for (int i = 0; i < size && bufferSize > 0; i++)
    {
        byte = data.GetUnsignedInt8(error, i);
        if (error.Fail())
        {
            hr = E_FAIL;
            goto exit;
        }
        cch = snprintf(buffer, bufferSize, "%02x", byte);
        buffer += cch;
        bufferSize -= cch;
    }
    // Pad the data bytes to 16 chars
    cch = size * 2;
    while (bufferSize > 0)
    {
        *buffer++ = ' ';
        bufferSize--;
        if (++cch >= 21)
            break;
    } 

    cch = snprintf(buffer, bufferSize, "%s", instruction.GetMnemonic(target));
    buffer += cch;
    bufferSize -= cch;

    // Pad the mnemonic to 8 chars
    while (bufferSize > 0)
    {
        *buffer++ = ' ';
        bufferSize--;
        if (++cch >= 8)
            break;
    } 
    snprintf(buffer, bufferSize, "%s\n", instruction.GetOperands(target));

exit:
    if (disassemblySize != NULL)
    {
        *disassemblySize = size;
    }
    if (endOffset != NULL)
    {
        *endOffset = offset + size;
    }
    return hr;
}

// Internal output string function
void
LLDBServices::OutputString(
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
// IDebugControl4
//----------------------------------------------------------------------------

HRESULT
LLDBServices::GetContextStackTrace(
    PVOID startContext,
    ULONG startContextSize,
    PDEBUG_STACK_FRAME frames,
    ULONG framesSize,
    PVOID frameContexts,
    ULONG frameContextsSize,
    ULONG frameContextsEntrySize,
    PULONG framesFilled)
{
    DT_CONTEXT *currentContext = (DT_CONTEXT*)frameContexts;
    PDEBUG_STACK_FRAME currentFrame = frames;
    lldb::SBThread thread;
    lldb::SBFrame frame;
    ULONG cFrames = 0;
    HRESULT hr = S_OK;

    // Doesn't support a starting context
    if (startContext != NULL || frames == NULL || frameContexts == NULL || frameContextsEntrySize != sizeof(DT_CONTEXT))
    {
        hr = E_INVALIDARG;
        goto exit;
    }

    thread = GetCurrentThread();
    if (!thread.IsValid())
    {
        hr = E_FAIL;
        goto exit;
    }

    frame = thread.GetFrameAtIndex(0);
    for (int i = 0; i < thread.GetNumFrames(); i++)
    {
        if (!frame.IsValid() || (cFrames > framesSize) || ((char *)currentContext > ((char *)frameContexts + frameContextsSize)))
        {
            break;
        }
        lldb::SBFrame framePrevious;
        lldb::SBFrame frameNext;

        currentFrame->InstructionOffset = frame.GetPC();
        currentFrame->StackOffset = frame.GetSP();

        currentFrame->FuncTableEntry = 0;
        currentFrame->Params[0] = 0;
        currentFrame->Params[1] = 0;
        currentFrame->Params[2] = 0;
        currentFrame->Params[3] = 0;
        currentFrame->Virtual = i == 0 ? TRUE : FALSE;
        currentFrame->FrameNumber = frame.GetFrameID();

        frameNext = thread.GetFrameAtIndex(i + 1);
        if (frameNext.IsValid())
        {
            currentFrame->ReturnOffset = frameNext.GetPC();
        }

        if (framePrevious.IsValid())
        {
            currentFrame->FrameOffset = framePrevious.GetSP();
        }
        else
        {
            currentFrame->FrameOffset = frame.GetSP();
        }

        GetContextFromFrame(frame, currentContext);

        framePrevious = frame;
        frame = frameNext;
        currentContext++;
        currentFrame++;
        cFrames++;
    }

exit:
    if (framesFilled != NULL)
    {
        *framesFilled = cFrames;
    }
    return hr;
}
    
//----------------------------------------------------------------------------
// IDebugDataSpaces
//----------------------------------------------------------------------------

HRESULT 
LLDBServices::ReadVirtual(
    ULONG64 offset,
    PVOID buffer,
    ULONG bufferSize,
    PULONG bytesRead)
{
    lldb::SBError error;
    size_t read = 0;

    // lldb doesn't expect sign-extended address
    offset = CONVERT_FROM_SIGN_EXTENDED(offset);

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
    return error.Success() || (read != 0) ? S_OK : E_FAIL;
}

HRESULT 
LLDBServices::WriteVirtual(
    ULONG64 offset,
    PVOID buffer,
    ULONG bufferSize,
    PULONG bytesWritten)
{
    lldb::SBError error;
    size_t written = 0;

    // lldb doesn't expect sign-extended address
    offset = CONVERT_FROM_SIGN_EXTENDED(offset);

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
    return error.Success() || (written != 0) ? S_OK : E_FAIL;
}

//----------------------------------------------------------------------------
// IDebugSymbols
//----------------------------------------------------------------------------

HRESULT 
LLDBServices::GetSymbolOptions(
    PULONG options)
{
    *options = SYMOPT_LOAD_LINES;
    return S_OK;
}

HRESULT 
LLDBServices::GetNameByOffset(
    ULONG64 offset,
    PSTR nameBuffer,
    ULONG nameBufferSize,
    PULONG nameSize,
    PULONG64 displacement)
{
    ULONG64 disp = DEBUG_INVALID_OFFSET;
    HRESULT hr = S_OK;

    lldb::SBTarget target;
    lldb::SBAddress address;
    lldb::SBModule module;
    lldb::SBFileSpec file;
    lldb::SBSymbol symbol;
    std::string str;

    // lldb doesn't expect sign-extended address
    offset = CONVERT_FROM_SIGN_EXTENDED(offset);

    target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        hr = E_FAIL;
        goto exit;
    }

    address = target.ResolveLoadAddress(offset);
    if (!address.IsValid())
    {
        hr = E_INVALIDARG;
        goto exit;
    }

    module = address.GetModule();
    if (!module.IsValid())
    {
        hr = E_FAIL;
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
LLDBServices::GetNumberModules(
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

HRESULT LLDBServices::GetModuleByIndex(
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
LLDBServices::GetModuleByModuleName(
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
LLDBServices::GetModuleByOffset(
    ULONG64 offset,
    ULONG startIndex,
    PULONG index,
    PULONG64 base)
{
    ULONG64 moduleBase = UINT64_MAX;
    ULONG moduleIndex = UINT32_MAX;

    lldb::SBTarget target;
    int numModules;

    // lldb doesn't expect sign-extended address
    offset = CONVERT_FROM_SIGN_EXTENDED(offset);

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
LLDBServices::GetModuleNames(
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
    lldb::SBTarget target;
    lldb::SBFileSpec fileSpec;
    HRESULT hr = S_OK;

    // lldb doesn't expect sign-extended address
    base = CONVERT_FROM_SIGN_EXTENDED(base);

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

HRESULT 
LLDBServices::GetLineByOffset(
    ULONG64 offset,
    PULONG fileLine,
    PSTR fileBuffer,
    ULONG fileBufferSize,
    PULONG fileSize,
    PULONG64 displacement)
{
    ULONG64 disp = DEBUG_INVALID_OFFSET;
    HRESULT hr = S_OK;
    ULONG line = 0;

    lldb::SBTarget target;
    lldb::SBAddress address;
    lldb::SBFileSpec file;
    lldb::SBLineEntry lineEntry;
    std::string str;

    // lldb doesn't expect sign-extended address
    offset = CONVERT_FROM_SIGN_EXTENDED(offset);

    target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        hr = E_FAIL;
        goto exit;
    }

    address = target.ResolveLoadAddress(offset);
    if (!address.IsValid())
    {
        hr = E_INVALIDARG;
        goto exit;
    }

    if (displacement)
    {
        lldb::SBSymbol symbol = address.GetSymbol();
        if (symbol.IsValid())
        {
            lldb::SBAddress startAddress = symbol.GetStartAddress();
            disp = address.GetOffset() - startAddress.GetOffset();
        }
    }

    lineEntry = address.GetLineEntry();
    if (!lineEntry.IsValid())
    {
        hr = E_FAIL;
        goto exit;
    }

    line = lineEntry.GetLine();
    file = lineEntry.GetFileSpec();
    if (file.IsValid())
    {
        str.append(file.GetDirectory());
        str.append(1, '/');
        str.append(file.GetFilename());
    }

    str.append(1, '\0');

exit:
    if (fileLine)
    {
        *fileLine = line;
    }
    if (fileSize)
    {
        *fileSize = str.length();
    }
    if (fileBuffer)
    {
        str.copy(fileBuffer, fileBufferSize);
    }
    if (displacement)
    {
        *displacement = disp;
    }
    return hr;
}
 
HRESULT 
LLDBServices::GetSourceFileLineOffsets(
    PCSTR file,
    PULONG64 buffer,
    ULONG bufferLines,
    PULONG fileLines)
{
    if (fileLines != NULL)
    {
        *fileLines = (ULONG)-1;
    }
    return E_NOTIMPL;
}

HRESULT 
LLDBServices::FindSourceFile(
    ULONG startElement,
    PCSTR file,
    ULONG flags,
    PULONG foundElement,
    PSTR buffer,
    ULONG bufferSize,
    PULONG foundSize)
{
    return E_NOTIMPL;
}

// Internal functions
PCSTR
LLDBServices::GetModuleDirectory(
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

ULONG64
LLDBServices::GetModuleBase(
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
LLDBServices::GetCurrentProcessId(
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
LLDBServices::GetCurrentThreadId(
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
LLDBServices::SetCurrentThreadId(
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
LLDBServices::GetCurrentThreadSystemId(
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
LLDBServices::GetThreadIdBySystemId(
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
LLDBServices::GetThreadContextById(
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
LLDBServices::GetContextFromFrame(
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
#elif DBG_TARGET_X86
    dtcontext->Eip = frame.GetPC();
    dtcontext->Esp = frame.GetSP();
    dtcontext->Ebp = frame.GetFP();
    dtcontext->EFlags = GetRegister(frame, "eflags");

    dtcontext->Edi = GetRegister(frame, "edi");
    dtcontext->Esi = GetRegister(frame, "esi");
    dtcontext->Ebx = GetRegister(frame, "ebx");
    dtcontext->Edx = GetRegister(frame, "edx");
    dtcontext->Ecx = GetRegister(frame, "ecx");
    dtcontext->Eax = GetRegister(frame, "eax");

    dtcontext->SegCs = GetRegister(frame, "cs");
    dtcontext->SegSs = GetRegister(frame, "ss");
    dtcontext->SegDs = GetRegister(frame, "ds");
    dtcontext->SegEs = GetRegister(frame, "es");
    dtcontext->SegFs = GetRegister(frame, "fs");
    dtcontext->SegGs = GetRegister(frame, "gs");
#endif
}

// Internal function
DWORD_PTR 
LLDBServices::GetRegister(
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
LLDBServices::GetValueByName(
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
LLDBServices::GetInstructionOffset(
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
LLDBServices::GetStackOffset(
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
LLDBServices::GetFrameOffset(
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
// Helper functions
//----------------------------------------------------------------------------

lldb::SBProcess
LLDBServices::GetCurrentProcess()
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
LLDBServices::GetCurrentThread()
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
LLDBServices::GetCurrentFrame()
{
    lldb::SBFrame frame;

    lldb::SBThread thread = GetCurrentThread();
    if (thread.IsValid())
    {
        frame = thread.GetSelectedFrame();
    }

    return frame;
}
