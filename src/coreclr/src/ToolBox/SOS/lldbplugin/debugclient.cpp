//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include "sosplugin.h"
#include <string.h>

DebugClient::DebugClient(lldb::SBDebugger &debugger, lldb::SBCommandReturnObject &returnObject) :
    m_debugger(debugger),
    m_returnObject(returnObject)
{
}

DebugClient::~DebugClient()
{
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
        char *str_ptr = NULL;
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
    *bytesRead = 0;

    lldb::SBTarget target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        return E_FAIL;
    }
    lldb::SBProcess process = target.GetProcess();
    if (!process.IsValid())
    {
        return E_FAIL;
    }
    lldb::SBError error;
    *bytesRead = process.ReadMemory(offset, buffer, bufferSize, error);

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
DebugClient::GetNumberModules(
PULONG loaded,
PULONG unloaded)
{
    if (loaded)
    {
        *loaded = 0;
    }
    if (unloaded)
    {
        *unloaded = 0;
    }
    lldb::SBTarget target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        return E_FAIL;
    }
    if (loaded)
    {
        *loaded = target.GetNumModules();
    }
    return S_OK;
}

HRESULT 
DebugClient::GetModuleByModuleName(
    PCSTR name,
    ULONG startIndex,
    PULONG index,
    PULONG64 base)
{
    if (index)
    {
        *index = UINT32_MAX;
    }
    if (base)
    {
        *base = UINT64_MAX;
    }
    lldb::SBTarget target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        return E_FAIL;
    }
    lldb::SBFileSpec fileSpec;
    fileSpec.SetFilename(name);

    lldb::SBModule module = target.FindModule(fileSpec);
    if (!module.IsValid())
    {
        return E_FAIL;
    }
    if (index)
    {
        int numModules = target.GetNumModules();
        for (int moduleIndex = startIndex; moduleIndex < numModules; moduleIndex++)
        {
            lldb::SBModule mod = target.GetModuleAtIndex(moduleIndex);
            if (module == mod)
            {
                *index = moduleIndex;
                break;
            }
        }
    }
    if (base)
    {
        // Find the first section with an valid base address
        int numSections = module.GetNumSections();
        for (int sectionIndex = 0; sectionIndex < numSections; sectionIndex++)
        {
            lldb::SBSection section = module.GetSectionAtIndex(sectionIndex);
            if (section.IsValid())
            {
                lldb::addr_t baseAddress = section.GetLoadAddress(target);
                if (baseAddress != LLDB_INVALID_ADDRESS)
                {
                    *base = baseAddress - section.GetFileOffset();
                    break;
                }
            }
        }
        if (*base == UINT64_MAX)
        {
            return E_FAIL;
        }
    }
    return S_OK;
}

HRESULT 
DebugClient::GetModuleByOffset(
    ULONG64 offset,
    ULONG startIndex,
    PULONG index,
    PULONG64 base)
{
    if (index)
    {
        *index = UINT32_MAX;
    }
    if (base)
    {
        *base = UINT64_MAX;
    }
    lldb::SBTarget target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        return E_FAIL;
    }
    int numModules = target.GetNumModules();
    for (int moduleIndex = startIndex; moduleIndex < numModules; moduleIndex++)
    {
        lldb::SBModule module = target.GetModuleAtIndex(moduleIndex);

        int numSections = module.GetNumSections();
        for (int sectionIndex = 0; sectionIndex < numSections; sectionIndex++)
        {
            lldb::SBSection section = module.GetSectionAtIndex(sectionIndex);
            if (section.IsValid())
            {
                lldb::addr_t baseAddress = section.GetLoadAddress(target);
                if (baseAddress != LLDB_INVALID_ADDRESS)
                {
                    if (offset > baseAddress)
                    {
                        if ((offset - baseAddress) < section.GetByteSize())
                        {
                            if (index)
                            {
                                *index = moduleIndex;
                            }
                            if (base)
                            {
                                *base = baseAddress - section.GetFileOffset();
                            }
                            return S_OK;
                        }
                    }
                }
            }
        }
    }

    return E_FAIL;
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
    lldb::SBTarget target = m_debugger.GetSelectedTarget();
    if (!target.IsValid())
    {
        return E_FAIL;
    }
    lldb::SBModule module;
    if (index != DEBUG_ANY_ID)
    {
        module = target.GetModuleAtIndex(index);
    }
    else
    {
        int numModules = target.GetNumModules();
        for (int moduleIndex = 0; moduleIndex < numModules; moduleIndex++)
        {
            lldb::SBModule mod = target.GetModuleAtIndex(moduleIndex);

            int numSections = mod.GetNumSections();
            for (int sectionIndex = 0; sectionIndex < numSections; sectionIndex++)
            {
                lldb::SBSection section = mod.GetSectionAtIndex(sectionIndex);
                if (section.IsValid())
                {
                    lldb::addr_t baseAddress = section.GetLoadAddress(target);
                    if (baseAddress != LLDB_INVALID_ADDRESS)
                    {
                        if (base == (baseAddress - section.GetFileOffset()))
                        {
                            module = mod;
                            break;
                        }
                    }
                }
            }
            if (module.IsValid())
            {
                break;
            }
        }
    }
    if (!module.IsValid())
    {
        return E_FAIL;
    }
    lldb::SBFileSpec fileSpec = module.GetFileSpec();
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
        stpncpy(moduleNameBuffer, fileSpec.GetFilename(), moduleNameBufferSize);
        if (moduleNameSize)
        {
            *moduleNameSize = strlen(moduleNameBuffer);
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
    return S_OK;
}