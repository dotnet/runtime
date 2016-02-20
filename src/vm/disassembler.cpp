// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"

#include "disassembler.h"
#include "dllimport.h"

#if USE_DISASSEMBLER

// TODO: Which contracts should be used where? Currently, everything is using LIMITED_METHOD_CONTRACT.

#if USE_COREDISTOOLS_DISASSEMBLER
HMODULE Disassembler::s_libraryHandle = nullptr;

Disassembler::CorDisasm *(*Disassembler::External_InitDisasm)(enum TargetArch Target) = nullptr;
SIZE_T(*Disassembler::External_DisasmInstruction)(const Disassembler::CorDisasm *Disasm, size_t Address,
                                  const uint8_t *Bytes, size_t Maxlength,
                                  bool PrintAssembly) = nullptr;
void(*Disassembler::External_FinishDisasm)(const Disassembler::CorDisasm *Disasm) = nullptr;
#endif // USE_COREDISTOOLS_DISASSEMBLER

Disassembler::ExternalDisassembler *Disassembler::s_availableExternalDisassembler = nullptr;

#if defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
// static
bool Disassembler::IsRexPrefix(UINT8 potentialRexByte)
{
    LIMITED_METHOD_CONTRACT;

#ifdef _TARGET_AMD64_
    return (potentialRexByte & 0xf0) == REX_PREFIX_BASE;
#else // !_TARGET_AMD64_
    return false;
#endif // _TARGET_AMD64_
}

// static
UINT8 Disassembler::DecodeModFromModRm(UINT8 modRm)
{
    LIMITED_METHOD_CONTRACT;
    return modRm >> 6;
}

// static
UINT8 Disassembler::DecodeRegOrOpCodeFromModRm(UINT8 modRm)
{
    LIMITED_METHOD_CONTRACT;
    return (modRm >> 3) & 0x7;
}

// static
UINT8 Disassembler::DecodeRmFromModRm(UINT8 modRm)
{
    LIMITED_METHOD_CONTRACT;
    return modRm & 0x7;
}
#endif // defined(_TARGET_AMD64_) || defined(_TARGET_X86_)

// static
bool Disassembler::IsAvailable()
{
    LIMITED_METHOD_CONTRACT;

#if USE_COREDISTOOLS_DISASSEMBLER
    return s_libraryHandle != nullptr;
#else // !USE_COREDISTOOLS_DISASSEMBLER
    return true;
#endif // USE_COREDISTOOLS_DISASSEMBLER
}

void Disassembler::StaticInitialize()
{
    LIMITED_METHOD_CONTRACT;

#if USE_COREDISTOOLS_DISASSEMBLER
    _ASSERTE(!IsAvailable());

    HMODULE libraryHandle = nullptr;
    PathString libPath;
    DWORD result = WszGetModuleFileName(nullptr, libPath);
    if (result == 0) {
#ifdef _DEBUG
        wprintf(
            W("GetModuleFileName failed, function 'DisasmInstruction': error %u\n"),
            GetLastError());
#endif // _DEBUG
        return;
    }

#if defined(FEATURE_PAL)
    WCHAR delim = W('/');
#else
    WCHAR delim = W('\\');
#endif
    LPCWSTR libFileName = MAKEDLLNAME(W("coredistools"));
    PathString::Iterator iter = libPath.End();
    if (libPath.FindBack(iter, delim)) {
        libPath.Truncate(++iter);
        libPath.Append(libFileName);
    }
    else {
        _ASSERTE(!"unreachable");
    }

    LPCWSTR libraryName = libPath.GetUnicode();
    libraryHandle = CLRLoadLibrary(libraryName);
    do
    {
        if (libraryHandle == nullptr)
        {
        #ifdef _DEBUG
            wprintf(W("LoadLibrary failed for '%s': error %u\n"), libraryName, GetLastError());
        #endif // _DEBUG
            break;
        }

        External_InitDisasm =
            reinterpret_cast<decltype(External_InitDisasm)>(GetProcAddress(libraryHandle, "InitDisasm"));
        if (External_InitDisasm == nullptr)
        {
        #ifdef _DEBUG
            wprintf(
                W("GetProcAddress failed for library '%s', function 'InitDisasm': error %u\n"),
                libraryName,
                GetLastError());
        #endif // _DEBUG
            break;
        }

        External_DisasmInstruction =
            reinterpret_cast<decltype(External_DisasmInstruction)>(GetProcAddress(libraryHandle, "DisasmInstruction"));
        if (External_DisasmInstruction == nullptr)
        {
        #ifdef _DEBUG
            wprintf(
                W("GetProcAddress failed for library '%s', function 'DisasmInstruction': error %u\n"),
                libraryName,
                GetLastError());
        #endif // _DEBUG
            break;
        }

        External_FinishDisasm =
            reinterpret_cast<decltype(External_FinishDisasm)>(GetProcAddress(libraryHandle, "FinishDisasm"));
        if (External_FinishDisasm == nullptr)
        {
        #ifdef _DEBUG
            wprintf(
                W("GetProcAddress failed for library '%s', function 'FinishDisasm': error %u\n"),
                libraryName,
                GetLastError());
        #endif // _DEBUG
            break;
        }

        // Set this last to indicate successful load of the library and all exports
        s_libraryHandle = libraryHandle;
        _ASSERTE(IsAvailable());
        return;
    } while (false);

    _ASSERTE(!IsAvailable());
    
#endif // USE_COREDISTOOLS_DISASSEMBLER
}

// static
void Disassembler::StaticClose()
{
    LIMITED_METHOD_CONTRACT;

    if (!IsAvailable())
    {
        return;
    }

#if USE_COREDISTOOLS_DISASSEMBLER
    CLRFreeLibrary(s_libraryHandle);
    s_libraryHandle = nullptr;
#endif
}

Disassembler::Disassembler()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(IsAvailable());

    // TODO: Is it ok to save and reuse an instance of the LLVM-based disassembler? It may later be used from a different
    // thread, and it may be deleted from a different thread than the one from which it was created.

    // Try to get an external disassembler that is already available for use before creating one
    ExternalDisassembler *externalDisassembler =
        FastInterlockExchangePointer(&s_availableExternalDisassembler, static_cast<ExternalDisassembler *>(nullptr));
    if (externalDisassembler == nullptr)
    {
    #if USE_COREDISTOOLS_DISASSEMBLER
        // First parameter:
        // - Empty string for the current architecture
        // - A string of the form "x86_64-pc-win32"
        externalDisassembler = External_InitDisasm(Target_Host);
    #elif USE_MSVC_DISASSEMBLER
    #ifdef _TARGET_X86_
        externalDisassembler = ExternalDisassembler::PdisNew(ExternalDisassembler::distX86);
    #elif defined(_TARGET_AMD64_)
        externalDisassembler = ExternalDisassembler::PdisNew(ExternalDisassembler::distX8664);
    #endif // defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
    #endif // USE_COREDISTOOLS_DISASSEMBLER || USE_MSVC_DISASSEMBLER
    }

    _ASSERTE(externalDisassembler != nullptr);
    m_externalDisassembler = externalDisassembler;
}

Disassembler::~Disassembler()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(IsAvailable());

    // Save the external disassembler for future use. We only save one instance, so delete a previously saved one.
    ExternalDisassembler *externalDisassemblerToDelete =
        FastInterlockExchangePointer(&s_availableExternalDisassembler, m_externalDisassembler);
    if (externalDisassemblerToDelete == nullptr)
    {
        return;
    }

#if USE_COREDISTOOLS_DISASSEMBLER
    External_FinishDisasm(externalDisassemblerToDelete);
#elif USE_MSVC_DISASSEMBLER
    delete externalDisassemblerToDelete;
#endif // USE_COREDISTOOLS_DISASSEMBLER || USE_MSVC_DISASSEMBLER
}

SIZE_T Disassembler::DisassembleInstruction(const UINT8 *code, SIZE_T codeLength, InstructionType *instructionTypeRef) const
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(IsAvailable());

#if USE_COREDISTOOLS_DISASSEMBLER
    SIZE_T instructionLength = External_DisasmInstruction(m_externalDisassembler, reinterpret_cast<SIZE_T>(code), code, codeLength, false /* PrintAssembly */);
#elif USE_MSVC_DISASSEMBLER
    SIZE_T instructionLength =
        m_externalDisassembler->CbDisassemble(reinterpret_cast<ExternalDisassembler::ADDR>(code), code, codeLength);
#endif // USE_COREDISTOOLS_DISASSEMBLER || USE_MSVC_DISASSEMBLER
    _ASSERTE(instructionLength <= codeLength);

    if (instructionTypeRef != nullptr)
    {
        if (instructionLength == 0)
        {
            *instructionTypeRef = InstructionType::Unknown;
        }
        else
        {
        #if USE_COREDISTOOLS_DISASSEMBLER
            *instructionTypeRef = DetermineInstructionType(code, instructionLength);
        #elif USE_MSVC_DISASSEMBLER
            *instructionTypeRef = DetermineInstructionType(m_externalDisassembler->Trmt());
        #endif // USE_COREDISTOOLS_DISASSEMBLER || USE_MSVC_DISASSEMBLER
        }
    }

    return instructionLength;
}

// static
InstructionType Disassembler::DetermineInstructionType(
#if USE_COREDISTOOLS_DISASSEMBLER
    const UINT8 *instructionCode, SIZE_T instructionCodeLength
#elif USE_MSVC_DISASSEMBLER
    ExternalDisassembler::TRMT terminationType
#endif // USE_COREDISTOOLS_DISASSEMBLER || USE_MSVC_DISASSEMBLER
    )
{
    LIMITED_METHOD_CONTRACT;

#if USE_COREDISTOOLS_DISASSEMBLER
    _ASSERTE(instructionCodeLength != 0);

    SIZE_T i = 0;
    if (Disassembler::IsRexPrefix(instructionCode[i]))
    {
        ++i;
    }

    switch (instructionCode[i])
    {
        case 0xe8: // call near rel
        #ifdef _TARGET_X86_
        case 0x9a: // call far ptr
        #endif // _TARGET_X86_
            return InstructionType::Call_DirectUnconditional;

        case 0xff:
            ++i;
            if (i >= instructionCodeLength)
            {
                break;
            }

            switch (Disassembler::DecodeRegOrOpCodeFromModRm(instructionCode[i]))
            {
                case 2: // call near r/m
                case 3: // call far m
                    return InstructionType::Call_IndirectUnconditional;

                case 4: // jmp near r/m
                case 5: // jmp far m
                    return InstructionType::Branch_IndirectUnconditional;
            }
            break;
    }
#elif USE_MSVC_DISASSEMBLER
    switch (terminationType)
    {
        case ExternalDisassembler::trmtCall:
            return InstructionType::Call_DirectUnconditional;

        case ExternalDisassembler::trmtCallInd:
            return InstructionType::Call_IndirectUnconditional;

        case ExternalDisassembler::trmtBraInd:
            return InstructionType::Branch_IndirectUnconditional;
    }
#endif // USE_COREDISTOOLS_DISASSEMBLER || USE_MSVC_DISASSEMBLER

    return InstructionType::Unknown;
}

#endif // USE_DISASSEMBLER
