// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// SPMIUtil.cpp - General utility functions
//----------------------------------------------------------

#include "standardpch.h"
#include "logging.h"
#include "spmiutil.h"

static bool breakOnDebugBreakorAV = false;

bool BreakOnDebugBreakorAV()
{
    return breakOnDebugBreakorAV;
}

void SetBreakOnDebugBreakOrAV(bool value)
{
    breakOnDebugBreakorAV = value;
}

void DebugBreakorAV(int val)
{
    if (IsDebuggerPresent())
    {
        if (val == 0)
            __debugbreak();
        if (BreakOnDebugBreakorAV())
            __debugbreak();
    }

    int exception_code = EXCEPTIONCODE_DebugBreakorAV + val;
    // assert((EXCEPTIONCODE_DebugBreakorAV <= exception_code) && (exception_code < EXCEPTIONCODE_DebugBreakorAV_MAX))
    LogException(exception_code, "DebugBreak or AV Exception %d", val);
}

char* GetEnvironmentVariableWithDefaultA(const char* envVarName, const char* defaultValue)
{
    char* retString = nullptr;

    // Figure out how much space we need to allocate
    DWORD dwRetVal = ::GetEnvironmentVariableA(envVarName, nullptr, 0);
    if (dwRetVal != 0)
    {
        retString = new char[dwRetVal];
        dwRetVal  = ::GetEnvironmentVariableA(envVarName, retString, dwRetVal);
    }
    else
    {
        if (defaultValue != nullptr)
        {
            dwRetVal  = (DWORD)strlen(defaultValue) + 1; // add one for null terminator
            retString = new char[dwRetVal];
            memcpy_s(retString, dwRetVal, defaultValue, dwRetVal);
        }
    }

    return retString;
}

WCHAR* GetEnvironmentVariableWithDefaultW(const WCHAR* envVarName, const WCHAR* defaultValue)
{
    WCHAR* retString = nullptr;

    // Figure out how much space we need to allocate
    DWORD dwRetVal = ::GetEnvironmentVariableW(envVarName, nullptr, 0);
    if (dwRetVal != 0)
    {
        retString = new WCHAR[dwRetVal];
        dwRetVal  = ::GetEnvironmentVariableW(envVarName, retString, dwRetVal);
    }
    else
    {
        if (defaultValue != nullptr)
        {
            dwRetVal  = (DWORD)wcslen(defaultValue) + 1; // add one for null terminator
            retString = new WCHAR[dwRetVal];
            memcpy_s(retString, dwRetVal * sizeof(WCHAR), defaultValue, dwRetVal * sizeof(WCHAR));
        }
    }

    return retString;
}

#ifdef TARGET_UNIX
// For some reason, the PAL doesn't have GetCommandLineA(). So write it.
LPSTR GetCommandLineA()
{
    LPSTR  pCmdLine  = nullptr;
    LPWSTR pwCmdLine = GetCommandLineW();

    if (pwCmdLine != nullptr)
    {
        // Convert to ASCII

        int n = WideCharToMultiByte(CP_ACP, 0, pwCmdLine, -1, nullptr, 0, nullptr, nullptr);
        if (n == 0)
        {
            LogError("MultiByteToWideChar failed %d", GetLastError());
            return nullptr;
        }

        pCmdLine = new char[n];

        int n2 = WideCharToMultiByte(CP_ACP, 0, pwCmdLine, -1, pCmdLine, n, nullptr, nullptr);
        if ((n2 == 0) || (n2 != n))
        {
            LogError("MultiByteToWideChar failed %d", GetLastError());
            return nullptr;
        }
    }

    return pCmdLine;
}
#endif // TARGET_UNIX

bool LoadRealJitLib(HMODULE& jitLib, WCHAR* jitLibPath)
{
    // Load Library
    if (jitLib == NULL)
    {
        if (jitLibPath == nullptr)
        {
            LogError("LoadRealJitLib - No real jit path");
            return false;
        }
        jitLib = ::LoadLibraryW(jitLibPath);
        if (jitLib == NULL)
        {
            LogError("LoadRealJitLib - LoadLibrary failed to load '%ws' (0x%08x)", jitLibPath, ::GetLastError());
            return false;
        }
    }
    return true;
}

void ReplaceIllegalCharacters(WCHAR* fileName)
{
    WCHAR* quote = nullptr;

    // If there are any quotes in the file name convert them to spaces.
    while ((quote = wcsstr(fileName, W("\""))) != nullptr)
    {
        *quote = W(' ');
    }

    // Remove any illegal or annoying characters from the file name by converting them to underscores.
    while ((quote = wcspbrk(fileName, W("=<>:\"/\\|?! *.,"))) != nullptr)
    {
        *quote = W('_');
    }
}

// All lengths in this function exclude the terminal NULL.
WCHAR* GetResultFileName(const WCHAR* folderPath, const WCHAR* fileName, const WCHAR* extension)
{
    const size_t folderPathLength   = wcslen(folderPath);
    const size_t fileNameLength     = wcslen(fileName);
    const size_t extensionLength    = wcslen(extension);
    const size_t maxPathLength      = MAX_PATH - 50; // subtract 50 because excel doesn't like paths longer then 230.
    const size_t randomStringLength = 8;

    size_t fullPathLength   = folderPathLength + 1 + extensionLength;
    bool appendRandomString = false;

    if (fileNameLength > 0)
    {
        fullPathLength += fileNameLength;
    }
    else
    {
        fullPathLength += randomStringLength;
        appendRandomString = true;
    }

    size_t charsToDelete = 0;

    if (fullPathLength > maxPathLength)
    {
        // The path name is too long; creating the file will fail. This can happen because we use the command line,
        // which for ngen includes lots of environment variables, for example.
        // Shorten the file name and add a random string to the end to avoid collisions.

        charsToDelete = fullPathLength - maxPathLength + randomStringLength;

        if (fileNameLength >= charsToDelete)
        {
            appendRandomString = true;
            fullPathLength = maxPathLength;
        }
        else
        {
            LogError("GetResultFileName - path to the output file is too long '%ws\\%ws.%ws(%d)'", folderPath, fileName, extension, fullPathLength);
            return nullptr;
        }
    }

    WCHAR* fullPath = new WCHAR[fullPathLength + 1];
    fullPath[0] = W('\0');
    wcsncat_s(fullPath, fullPathLength + 1, folderPath, folderPathLength);
    wcsncat_s(fullPath, fullPathLength + 1, DIRECTORY_SEPARATOR_STR_W, 1);

    if (fileNameLength > charsToDelete)
    {
        wcsncat_s(fullPath, fullPathLength + 1, fileName, fileNameLength - charsToDelete);
        ReplaceIllegalCharacters(fullPath + folderPathLength + 1);
    }

    if (appendRandomString)
    {
       unsigned randomNumber = 0;

#ifdef TARGET_UNIX
       PAL_Random(&randomNumber, sizeof(randomNumber));
#else  // !TARGET_UNIX
       rand_s(&randomNumber);
#endif // !TARGET_UNIX

       WCHAR randomString[randomStringLength + 1];
       swprintf_s(randomString, randomStringLength + 1, W("%08X"), randomNumber);
       wcsncat_s(fullPath, fullPathLength + 1, randomString, randomStringLength);
    }

    wcsncat_s(fullPath, fullPathLength + 1, extension, extensionLength);

    return fullPath;
}

#ifdef TARGET_AMD64
static SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_AMD64;
#elif defined(TARGET_X86)
static SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_X86;
#elif defined(TARGET_ARM)
static SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_ARM;
#elif defined(TARGET_ARM64)
static SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_ARM64;
#else
#error Unsupported architecture
#endif

SPMI_TARGET_ARCHITECTURE GetSpmiTargetArchitecture()
{
    return SpmiTargetArchitecture;
}

void SetSpmiTargetArchitecture(SPMI_TARGET_ARCHITECTURE spmiTargetArchitecture)
{
    SpmiTargetArchitecture = spmiTargetArchitecture;
}

// The following functions are used for arm64/arm32 relocation processing.
// They are copies of the code in src\coreclr\utilcode\util.cpp.
// We decided to copy them instead of linking with utilcode library
// to avoid introducing additional runtime dependencies.

void PutArm64Rel28(UINT32* pCode, INT32 imm28)
{
    UINT32 branchInstr = *pCode;
    branchInstr &= 0xFC000000;
    branchInstr |= ((imm28 >> 2) & 0x03FFFFFF);
    *pCode = branchInstr;
}

void PutArm64Rel21(UINT32* pCode, INT32 imm21)
{
    UINT32 adrpInstr = *pCode;
    adrpInstr &= 0x9F00001F;
    INT32 immlo = imm21 & 0x03;
    INT32 immhi = (imm21 & 0x1FFFFC) >> 2;
    adrpInstr |= ((immlo << 29) | (immhi << 5));
    *pCode = adrpInstr;
}

void PutArm64Rel12(UINT32* pCode, INT32 imm12)
{
    UINT32 addInstr = *pCode;
    addInstr &= 0xFFC003FF;
    addInstr |= (imm12 << 10);
    *pCode = addInstr;
}

void PutThumb2Imm16(UINT16* p, UINT16 imm16)
{
    USHORT Opcode0 = p[0];
    USHORT Opcode1 = p[1];
    Opcode0 &= ~((0xf000 >> 12) | (0x0800 >> 1));
    Opcode1 &= ~((0x0700 << 4) | (0x00ff << 0));
    Opcode0 |= (imm16 & 0xf000) >> 12;
    Opcode0 |= (imm16 & 0x0800) >> 1;
    Opcode1 |= (imm16 & 0x0700) << 4;
    Opcode1 |= (imm16 & 0x00ff) << 0;
    p[0] = Opcode0;
    p[1] = Opcode1;
}

void PutThumb2Mov32(UINT16* p, UINT32 imm32)
{
    PutThumb2Imm16(p, (UINT16)imm32);
    PutThumb2Imm16(p + 2, (UINT16)(imm32 >> 16));
}

void PutThumb2BlRel24(UINT16* p, INT32 imm24)
{
    USHORT Opcode0 = p[0];
    USHORT Opcode1 = p[1];
    Opcode0 &= 0xF800;
    Opcode1 &= 0xD000;

    UINT32 S = (imm24 & 0x1000000) >> 24;
    UINT32 J1 = ((imm24 & 0x0800000) >> 23) ^ S ^ 1;
    UINT32 J2 = ((imm24 & 0x0400000) >> 22) ^ S ^ 1;

    Opcode0 |= ((imm24 & 0x03FF000) >> 12) | (S << 10);
    Opcode1 |= ((imm24 & 0x0000FFE) >> 1) | (J1 << 13) | (J2 << 11);

    p[0] = Opcode0;
    p[1] = Opcode1;
}
