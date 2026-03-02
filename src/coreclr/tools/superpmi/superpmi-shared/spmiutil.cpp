// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// SPMIUtil.cpp - General utility functions
//----------------------------------------------------------

#include "standardpch.h"
#include "logging.h"
#include "spmiutil.h"

#include <sstream>
#include <iomanip>
#include <minipal/debugger.h>
#include <minipal/random.h>

static bool breakOnDebugBreakorAV = false;

bool BreakOnDebugBreakorAV()
{
    return breakOnDebugBreakorAV;
}

void SetBreakOnDebugBreakOrAV(bool value)
{
    breakOnDebugBreakorAV = value;
}

static bool breakOnException = false;

bool BreakOnException()
{
    return breakOnException;
}

void SetBreakOnException(bool value)
{
    breakOnException = value;
}

void DebugBreakorAV(int val)
{
    if (minipal_is_native_debugger_present())
    {
        if (val == 0)
            DEBUG_BREAK;
        if (BreakOnDebugBreakorAV())
            DEBUG_BREAK;
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
            dwRetVal  = (DWORD)u16_strlen(defaultValue) + 1; // add one for null terminator
            retString = new WCHAR[dwRetVal];
            memcpy_s(retString, dwRetVal * sizeof(WCHAR), defaultValue, dwRetVal * sizeof(WCHAR));
        }
    }

    return retString;
}

const char* GetEnvWithDefault(const char* envVarName, const char* defaultValue)
{
    // getenv isn't thread safe, but it's simple and sufficient since we are development-only tool
    char* env = getenv(envVarName);
    return env ? env : defaultValue;
}

std::string GetProcessCommandLine()
{
#ifdef TARGET_WINDOWS
    return ::GetCommandLineA();
#else
    FILE* fp = fopen("/proc/self/cmdline", "r");
    if (fp != NULL)
    {
        std::string result;
        char*       cmdLine = nullptr;
        size_t      size    = 0;

        while (getdelim(&cmdLine, &size, '\0', fp) != -1)
        {
            // /proc/self/cmdline uses \0 as delimiter, convert it to space
            if (!result.empty())
                result += ' ';

            result += cmdLine;

            free(cmdLine);
            cmdLine = nullptr;
            size    = 0;
        }

        fclose(fp);
        return result;
    }

    return "";
#endif
}

bool LoadRealJitLib(HMODULE& jitLib, const std::string& jitLibPath)
{
    // Load Library
    if (jitLib == NULL)
    {
        if (jitLibPath.empty())
        {
            LogError("LoadRealJitLib - No real jit path");
            return false;
        }
#ifdef TARGET_WINDOWS
        jitLib = ::LoadLibraryExA(jitLibPath.c_str(), NULL, 0);
        if (jitLib == NULL)
        {
            LogError("LoadRealJitLib - LoadLibrary failed to load '%s' (0x%08x)", jitLibPath.c_str(), ::GetLastError());
            return false;
        }
#else
        jitLib = ::dlopen(jitLibPath.c_str(), RTLD_LAZY);
        // The simulated DllMain of JIT doesn't do any meaningful initialization. Skip it.
        if (jitLib == NULL)
        {
            LogError("LoadRealJitLib - dlopen failed to load '%s' (%s)", jitLibPath.c_str(), ::dlerror());
            return false;
        }
#endif
    }
    return true;
}

void ReplaceIllegalCharacters(std::string& fileName)
{
    // Perform the following transforms:
    //  - Convert non-ASCII to ASCII for simplicity
    //  - Remove any illegal or annoying characters from the file name by
    // converting them to underscores.
    //  - Replace any quotes in the file name with spaces.

    for (char& quote : fileName)
    {
        char ch = quote;
        if ((ch <= 32) || (ch >= 127)) // Only allow textual ASCII characters
        {
            quote = '_';
        }
        else
        {
            switch (ch)
            {
                case '(': case ')': case '=': case '<':
                case '>': case ':': case '/': case '\\':
                case '|': case '?': case '!': case '*':
                case '.': case ',':
                    quote = '_';
                    break;
                case '"':
                    quote = ' ';
                    break;
                default:
                    break;
            }
        }
    }
}

std::string GetResultFileName(const std::string& folderPath,
                              const std::string& fileName,
                              const std::string& extension)
{
    // Append a random string to improve uniqueness.
    //
    uint32_t randomNumber = 0;
    minipal_get_non_cryptographically_secure_random_bytes((uint8_t*)&randomNumber, sizeof(randomNumber));
    std::stringstream ss;
    ss << std::hex << std::setw(8) << std::setfill('0') << randomNumber;
    std::string suffix = ss.str() + extension;

    // Limit the total file name length to MAX_PATH - 50
    int usableLength = MAX_PATH - 50 - (int)folderPath.size() - (int)suffix.size();
    if (usableLength < 0)
    {
        LogError("GetResultFileName - folder path '%s' length + minimal file name exceeds limit %d", folderPath.c_str(), MAX_PATH - 50);
        return "";
    }

    std::string copy = fileName;
    if ((int)copy.size() > usableLength)
    {
        copy = copy.substr(0, usableLength);
    }

    ReplaceIllegalCharacters(copy);
    return folderPath + DIRECTORY_SEPARATOR_CHAR_A + copy + suffix;
}

#ifdef TARGET_AMD64
static SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_AMD64;
#elif defined(TARGET_X86)
static SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_X86;
#elif defined(TARGET_ARM)
static SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_ARM;
#elif defined(TARGET_ARM64)
static SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_ARM64;
#elif defined(TARGET_LOONGARCH64)
static SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_LOONGARCH64;
#elif defined(TARGET_RISCV64)
static SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_RISCV64;
#elif defined(TARGET_WASM32)
static SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture = SPMI_TARGET_ARCHITECTURE_WASM32;
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

// GetArm64MovConstant / GetArm64MovkConstant: Decode arm64 mov / movk instructions, e.g.:
//    d29ff600 mov     x0, #65456
//    f2ab8640 movk    x0, #23602, lsl #16
//    f2c04bc0 movk    x0, #606, lsl #32
//
// This is used in the NearDiffer to determine if a sequence of mov/movk is actually an address.
//
// Return `true` if the instruction pointed to by `p` is a mov/movk, `false` otherwise.
// If true, fill out the target register in `*pReg`, constant in `*pCon`, and (for movk) shift value in `*pShift`.

bool GetArm64MovConstant(UINT32* p, unsigned* pReg, unsigned* pCon)
{
    UINT32 instr = *p;
    if ((instr & 0xffe00000) == 0xd2800000)
    {
        *pReg = instr & 0x1f;
        *pCon = (instr >> 5) & 0xffff;
        return true;
    }

    return false;
}

bool GetArm64MovkConstant(UINT32* p, unsigned* pReg, unsigned* pCon, unsigned* pShift)
{
    UINT32 instr = *p;
    if ((instr & 0xff800000) == 0xf2800000)
    {
        *pReg = instr & 0x1f;
        *pCon = (instr >> 5) & 0xffff;
        *pShift = ((instr >> 21) & 0x3) * 16;
        return true;
    }

    return false;
}

// PutArm64MovkConstant: set the constant field in an Arm64 `movk` instruction
void PutArm64MovkConstant(UINT32* p, unsigned con)
{
    *p = (*p & ~(0xffff << 5)) | ((con & 0xffff) << 5);
}

// GetArm32MovwConstant / GetArm32MovtConstant: Decode Arm32 movw / movt instructions, e.g.:
//    4b f2 33 40    movw    r0, #46131
//    c0 f2 79 30    movt    r0, #889
//
// Return `true` if the instruction pointed to by `p` is a movw/movt, `false` otherwise.
// If true, fill out the target register in `*pReg`, constant in `*pCon`.

bool GetArm32MovwConstant(UINT32* p, unsigned* pReg, unsigned* pCon)
{
    // This decodes the "MOV (immediate)" instruction, Encoding T3, from the ARM manual section A8.8.102.
    if (!Is32BitThumb2Instruction((UINT16*)p))
    {
        return false;
    }

    // A Thumb-2 instruction is one or two 16-bit words (in little-endian format).
    UINT16 instr1 = *(UINT16*)p;
    UINT16 instr2 = *((UINT16*)p + 1);
    UINT32 instr = (instr1 << 16) | instr2;
    if ((instr & 0xfbf08000) != 0xf2400000)
    {
        return false;
    }

    *pReg = (instr & 0xf00) >> 8;
    *pCon = ExtractArm32MovImm(instr);
    return true;
}

bool GetArm32MovtConstant(UINT32* p, unsigned* pReg, unsigned* pCon)
{
    // This decodes the "MOVT" instruction, Encoding T1, from the ARM manual section A8.8.106.
    if (!Is32BitThumb2Instruction((UINT16*)p))
    {
        return false;
    }

    // A Thumb-2 instruction is one or two 16-bit words (in little-endian format).
    UINT16 instr1 = *(UINT16*)p;
    UINT16 instr2 = *((UINT16*)p + 1);
    UINT32 instr = (instr1 << 16) | instr2;
    if ((instr & 0xfbf08000) != 0xf2C00000)
    {
        return false;
    }

    *pReg = (instr & 0xf00) >> 8;
    *pCon = ExtractArm32MovImm(instr);
    return true;
}

// Is the instruction we're pointing to an Arm32 (Thumb-2) 32-bit instruction?
bool Is32BitThumb2Instruction(UINT16* p)
{
    UINT16 instr1 = *p;
    if ((instr1 & 0xf800) < 0xe800)
    {
        // Not a 32-bit instruction
        return false;
    }
    return true;
}

// Extract the immediate value from a movw/movt instruction encoding.
UINT32 ExtractArm32MovImm(UINT32 instr)
{
    UINT32 imm4 = (instr >> 16) & 0xf;
    UINT32 i    = (instr >> 26) & 0x1;
    UINT32 imm3 = (instr >> 12) & 0x7;
    UINT32 imm8 = instr & 0xff;
    return (imm4 << 12) | (i << 11) | (imm3 << 8) | imm8;
}

// PutArm32MovtConstant: set the constant field in an Arm32 `movt` instruction.
// `*p` points to a `movt` instruction. `con` must be a 16-bit constant.
void PutArm32MovtConstant(UINT32* p, unsigned con)
{
    UINT32 imm4 = (con >> 12) & 0xf;
    UINT32 i    = (con >> 11) & 0x1;
    UINT32 imm3 = (con >> 8) & 0x7;
    UINT32 imm8 = con & 0xff;
    UINT16 instr1 = *(UINT16*)p;
    UINT16 instr2 = *((UINT16*)p + 1);
    UINT32 instr = (instr1 << 16) | instr2;
    instr = (instr & 0xfbf08f00) | (imm4 << 16) | (i << 26) | (imm3 << 12) | imm8;
    *(UINT16*)p       = (UINT16)(instr >> 16);
    *((UINT16*)p + 1) = (UINT16)instr;
}

//*****************************************************************************
//  Extract the PC-Relative offset from auipc + I-type or S-type adder (addi/load/store/jalr)
//*****************************************************************************
INT64 GetRiscV64AuipcCombo(UINT32 * pCode, bool isStype)
{
    enum
    {
        OpcodeAuipc = 0x17,
        OpcodeAddi = 0x13,
        OpcodeLoad = 0x03,
        OpcodeStore = 0x23,
        OpcodeLoadFp = 0x07,
        OpcodeStoreFp = 0x27,
        OpcodeJalr = 0x67,
        OpcodeMask = 0x7F,

        Funct3AddiJalr = 0x0000,
        Funct3Mask = 0x7000,
    };

    UINT32 auipc = pCode[0];
    _ASSERTE((auipc & OpcodeMask) == OpcodeAuipc);
    int auipcRegDest = (auipc >> 7) & 0x1F;
    _ASSERTE(auipcRegDest != 0);

    INT64 hi20 = (INT32(auipc) >> 12) << 12;

    UINT32 instr = pCode[1];
    UINT32 opcode = instr & OpcodeMask;
    UINT32 funct3 = instr & Funct3Mask;
    _ASSERTE(opcode == OpcodeLoad || opcode == OpcodeStore || opcode == OpcodeLoadFp || opcode == OpcodeStoreFp ||
        ((opcode == OpcodeAddi || opcode == OpcodeJalr) && funct3 == Funct3AddiJalr));
    _ASSERTE(isStype == (opcode == OpcodeStore || opcode == OpcodeStoreFp));
    int addrReg = (instr >> 15) & 0x1F;
    _ASSERTE(auipcRegDest == addrReg);

    INT64 lo12 = (INT32(instr) >> 25) << 5; // top 7 bits are in the same spot
    int bottomBitsPos = isStype ? 7 : 20;
    lo12 |= (instr >> bottomBitsPos) & 0x1F;

    return hi20 + lo12;
}


//*****************************************************************************
//  Deposit the PC-Relative offset into auipc + I-type or S-type adder (addi/load/store/jalr)
//*****************************************************************************
void PutRiscV64AuipcCombo(UINT32 * pCode, INT64 offset, bool isStype)
{
    INT32 lo12 = (offset << (64 - 12)) >> (64 - 12);
    INT32 hi20 = INT32(offset - lo12);
    _ASSERTE(INT64(lo12) + INT64(hi20) == offset);

    _ASSERTE(GetRiscV64AuipcCombo(pCode, isStype) == 0);
    pCode[0] |= hi20;
    int bottomBitsPos = isStype ? 7 : 20;
    pCode[1] |= (lo12 >> 5) << 25; // top 7 bits are in the same spot
    pCode[1] |= (lo12 & 0x1F) << bottomBitsPos;
    _ASSERTE(GetRiscV64AuipcCombo(pCode, isStype) == offset);
}

template<typename TPrint>
static std::string getFromPrinter(TPrint print)
{
    char buffer[256];

    size_t requiredBufferSize;
    print(buffer, sizeof(buffer), &requiredBufferSize);

    if (requiredBufferSize <= sizeof(buffer))
    {
        return std::string(buffer);
    }
    else
    {
        std::vector<char> vec(requiredBufferSize);
        size_t printed = print(vec.data(), requiredBufferSize, nullptr);
        assert(printed == requiredBufferSize - 1);
        return std::string(vec.data());
    }
}

std::string getMethodName(MethodContext* mc, CORINFO_METHOD_HANDLE methHnd)
{
    return getFromPrinter([&](char* buffer, size_t bufferSize, size_t* requiredBufferSize) {
        return mc->repPrintMethodName(methHnd, buffer, bufferSize, requiredBufferSize);
        });
}

std::string getClassName(MethodContext* mc, CORINFO_CLASS_HANDLE clsHnd)
{
    return getFromPrinter([&](char* buffer, size_t bufferSize, size_t* requiredBufferSize) {
        return mc->repPrintClassName(clsHnd, buffer, bufferSize, requiredBufferSize);
        });
}

std::string ConvertToUtf8(const WCHAR* str)
{
    unsigned len = WideCharToMultiByte(CP_UTF8, 0, str, -1, nullptr, 0, nullptr, nullptr);
    if (len == 0)
        return{};

    std::vector<char> buf(len + 1);
    WideCharToMultiByte(CP_UTF8, 0, str, -1, buf.data(), len + 1, nullptr, nullptr);
    return std::string{ buf.data() };
}
