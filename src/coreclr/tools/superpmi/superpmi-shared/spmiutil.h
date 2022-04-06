// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// SPMIUtil.h - General utility functions
//----------------------------------------------------------
#ifndef _SPMIUtil
#define _SPMIUtil

#include "methodcontext.h"

bool BreakOnDebugBreakorAV();
void SetBreakOnDebugBreakOrAV(bool value);

bool BreakOnException();
void SetBreakOnException(bool value);

void DebugBreakorAV(int val); // Global(ish) error handler

char* GetEnvironmentVariableWithDefaultA(const char* envVarName, const char* defaultValue = nullptr);

WCHAR* GetEnvironmentVariableWithDefaultW(const WCHAR* envVarName, const WCHAR* defaultValue = nullptr);

#ifdef TARGET_UNIX
LPSTR GetCommandLineA();
#endif // TARGET_UNIX

bool LoadRealJitLib(HMODULE& realJit, WCHAR* realJitPath);

WCHAR* GetResultFileName(const WCHAR* folderPath, const WCHAR* fileName, const WCHAR* extension);

// SuperPMI stores handles as unsigned 64-bit integers, no matter the platform the collection happens on
// (32 or 64 bit). Handles are defined as pointers. We need to be careful when converting from a handle
// to an int to ensure we don't sign extend the pointer, which is the behavior of some compilers.
// First cast the pointer to an unsigned integer the same size as the pointer, then, if the pointer is
// 32-bits, zero extend it to 64-bits.

template <typename T>
inline DWORDLONG CastHandle(T h)
{
    return (DWORDLONG)(uintptr_t)h;
}

// Basically the same thing, but variables/fields declared as for pointer types.
template <typename T>
inline DWORDLONG CastPointer(T* p)
{
    return (DWORDLONG)(uintptr_t)p;
}

enum SPMI_TARGET_ARCHITECTURE
{
    SPMI_TARGET_ARCHITECTURE_X86,
    SPMI_TARGET_ARCHITECTURE_AMD64,
    SPMI_TARGET_ARCHITECTURE_ARM64,
    SPMI_TARGET_ARCHITECTURE_ARM
};

SPMI_TARGET_ARCHITECTURE GetSpmiTargetArchitecture();
void SetSpmiTargetArchitecture(SPMI_TARGET_ARCHITECTURE spmiTargetArchitecture);

inline bool IsSpmiTarget64Bit()
{
    return (GetSpmiTargetArchitecture() == SPMI_TARGET_ARCHITECTURE_AMD64) || (GetSpmiTargetArchitecture() == SPMI_TARGET_ARCHITECTURE_ARM64);
}

inline size_t SpmiTargetPointerSize()
{
    return IsSpmiTarget64Bit() ? sizeof(INT64) : sizeof(INT32);
}

void PutArm64Rel28(UINT32* pCode, INT32 imm28);
void PutArm64Rel21(UINT32* pCode, INT32 imm21);
void PutArm64Rel12(UINT32* pCode, INT32 imm12);

void PutThumb2Mov32(UINT16* p, UINT32 imm32);
void PutThumb2BlRel24(UINT16* p, INT32 imm24);

template <typename T, int size>
inline constexpr unsigned ArrLen(T (&)[size])
{
    return size;
}

#endif // !_SPMIUtil
