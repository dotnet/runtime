// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
#include "strike.h"
#include "util.h"
#include "disasm.h"
#include <dbghelp.h>

#include "../../../inc/corhdr.h"
#include "../../../inc/cor.h"
#include "../../../inc/dacprivate.h"


#if defined(SOS_TARGET_X86) && defined(SOS_TARGET_AMD64)
#error This file does not support SOS targeting both X86 and AMD64 debuggees
#endif

#if !defined(SOS_TARGET_X86) && !defined(SOS_TARGET_AMD64)
#error This file should be used to support SOS targeting either X86 or AMD64 debuggees
#endif


// These must be in the same order as they are used in the instruction
// encodings/same as the CONTEXT field order.
enum RegIndex
{
    EAX, ECX, EDX, EBX, ESP, EBP, ESI, EDI,

#ifdef _TARGET_AMD64_
    R8, R9, R10, R11, R12, R13, R14, R15,
#endif // _TARGET_AMD64_

    EIP, NONE
};

const int NumReg = NONE;
struct Register
{
    TADDR value;
    BOOL bValid;
    TADDR stack;
    BOOL bOnStack;
};

// Find the index for a register name
inline RegIndex FindReg (___in __in_z char *ptr, __out_opt int *plen = NULL, __out_opt int *psize = NULL)
{
    struct RegName
    {
        RegIndex index;
        PCSTR pszName;
        int cchName;
        int size;
    };

    static RegName rgRegNames[] = {

#define REG(index, reg, size) { index, #reg, sizeof(#reg)-1, size }
#define REG8(index, reg) REG(index, reg, 1)
#define REG16(index, reg) REG(index, reg, 2)
#define REG32(index, reg) REG(index, reg, 4)
#define REG64(index, reg) REG(index, reg, 8)

        REG8(EAX, al),
        REG8(EAX, ah),
        REG8(EBX, bl),
        REG8(EBX, bh),
        REG8(ECX, cl),
        REG8(ECX, ch),
        REG8(EDX, dl),
        REG8(EDX, dh),

        REG16(EAX, ax),
        REG16(EBX, bx),
        REG16(ECX, cx),
        REG16(EDX, dx),
        REG16(ESI, si),
        REG16(EDI, di),
        REG16(EBP, bp),
        REG16(ESP, sp),

        REG32(EAX, eax),
        REG32(EBX, ebx),
        REG32(ECX, ecx),
        REG32(EDX, edx),
        REG32(ESI, esi),
        REG32(EDI, edi),
        REG32(EBP, ebp),
        REG32(ESP, esp),

#ifdef _TARGET_AMD64_

        REG8(R8, r8b),
        REG8(R9, r9b),
        REG8(R10, r10b),
        REG8(R11, r11b),
        REG8(R12, r12b),
        REG8(R13, r13b),
        REG8(R14, r14b),
        REG8(R15, r15b),

        REG16(R8, r8w),
        REG16(R9, r9w),
        REG16(R10, r10w),
        REG16(R11, r11w),
        REG16(R12, r12w),
        REG16(R13, r13w),
        REG16(R14, r14w),
        REG16(R15, r15w),

        REG32(R8, r8d),
        REG32(R9, r9d),
        REG32(R10, r10d),
        REG32(R11, r11d),
        REG32(R12, r12d),
        REG32(R13, r13d),
        REG32(R14, r14d),
        REG32(R15, r15d),

        REG64(EAX, rax),
        REG64(EBX, rbx),
        REG64(ECX, rcx),
        REG64(EDX, rdx),
        REG64(ESI, rsi),
        REG64(EDI, rdi),
        REG64(EBP, rbp),
        REG64(ESP, rsp),
        REG64(R8, r8),
        REG64(R9, r9),
        REG64(R10, r10),
        REG64(R11, r11),
        REG64(R12, r12),
        REG64(R13, r13),
        REG64(R14, r14),
        REG64(R15, r15),

#endif // _TARGET_AMD64_

#undef REG
#undef REG8
#undef REG16
#undef REG32
#undef REG64

    };

    for (size_t i = 0; i < sizeof(rgRegNames)/sizeof(rgRegNames[0]); i++)
    {
        if (!strncmp(ptr, rgRegNames[i].pszName, rgRegNames[i].cchName))
        {
            if (psize)
                *psize = rgRegNames[i].size;

            if (plen)
                *plen = rgRegNames[i].cchName;

            return rgRegNames[i].index;
        }
    }

    return NONE;
}

// Find the value of an expression.
inline BOOL FindSrc (__in_z char *ptr, ___in Register *reg, INT_PTR &value, BOOL &bDigit)
{
    if (GetValueFromExpr (ptr, value))
    {
        bDigit = TRUE;
        return TRUE;
    }
    
    BOOL bValid = FALSE;
    BOOL bByRef = IsByRef (ptr);
    bDigit = FALSE;

    int regnamelen;
    RegIndex index = FindReg (ptr, &regnamelen);
    if (index != NONE)
    {
        if (reg[index].bValid)
        {
            value = reg[index].value;
            ptr += regnamelen;
            // TODO:  consider ecx+edi*4+0x4
            if ((IsTermSep (ptr[0]) && !bByRef)
                || (ptr[0] == ']' && bByRef))
            {
                bValid = TRUE;
                if (bByRef)
                    SafeReadMemory (TO_TADDR(value), &value, sizeof(void*), NULL);
            }
        }
    }
    return bValid;
}

enum ADDRESSMODE {REG, DATA, INDIRECT, NODATA, BAD};

struct RegState
{
    RegIndex reg;
    BOOL bFullReg;
    char scale;
    int namelen;
};

struct InstData
{
    ADDRESSMODE mode;
    RegState reg[2];
    INT_PTR value;
};

void FindMainReg (___in __in_z char *ptr, RegState &reg)
{
    int size = 0;

    reg.reg = FindReg(ptr, &reg.namelen, &size);

    reg.bFullReg = (reg.reg!=NONE && sizeof(void*)==size) ? TRUE : FALSE;
}

static void DecodeAddressIndirect (___in __in_z char *term, InstData& arg)
{
    arg.mode = BAD;
    arg.value = 0;
    arg.reg[0].scale = 0;
    arg.reg[1].scale = 0;
    
    if (!IsByRef (term))
    {
        return;
    }
    
    // first part must be a reg
    arg.reg[0].scale = 1;
    if (term[0] == '+')
        term ++;
    else if (term[0] == '-')
    {
        term ++;
        arg.reg[0].scale = -1;
    }
    if (isdigit(term[0]))
    {
        arg.reg[0].scale *= term[0]-'0';
        term ++;
    }

    FindMainReg (term, arg.reg[0]);
    if (arg.reg[0].reg == NONE)
        return;
    term += arg.reg[0].namelen;

    if (term[0] == ']')
    {
        // It is [reg]
        arg.mode = INDIRECT;
        arg.value = 0;
        return;
    }

    char sign = (char)((term[0] == '+')?1:-1);
    term ++;
    FindMainReg (term, arg.reg[1]);
    if (arg.reg[1].reg != NONE)
    {
        // It is either [reg+reg*c] or [reg+reg*c+c]

        term += arg.reg[1].namelen;

        if (term[0] == '*')
        {
            term ++;
            arg.reg[1].scale = sign*(term[0]-'0');
            term ++;
        }
        else
            arg.reg[1].scale = sign;
    
        if (term[0] == ']')
        {
            // It is [reg+reg*c]
            arg.mode = INDIRECT;
            arg.value = 0;
            return;
        }
        sign = (char)((term[0] == '+')?1:-1);
        term ++;
    }

    char *endptr;
    arg.value = strtoul(term, &endptr, 16);
    if (endptr[0] == ']')
    {
        // It is [reg+reg*c+c]
        arg.value *= sign;
        arg.mode = INDIRECT;
    }
}

void DecodeAddressTerm (___in __in_z char *term, InstData& arg)
{
    arg.mode = BAD;
    arg.reg[0].scale = 0;
    arg.reg[1].scale = 0;
    arg.value = 0;
    INT_PTR value;
    
    if (GetValueFromExpr (term, value))
    {
        arg.value = value;
        arg.mode = DATA;
    }
    else
    {
        FindMainReg (term, arg.reg[0]);
        if (arg.reg[0].reg != NONE)
        {
            arg.mode = REG;
        }
        else
        {
            DecodeAddressIndirect (term, arg);
        }
    }
}

// Return 0 for non-managed call.  Otherwise return MD address.
TADDR MDForCall (TADDR callee)
{
    // call managed code?
    JITTypes jitType;
    TADDR methodDesc;
    TADDR IP = callee;
    TADDR gcinfoAddr;

    if (!GetCalleeSite (callee, IP))
        return 0;

    IP2MethodDesc (IP, methodDesc, jitType, gcinfoAddr);
    if (methodDesc)
    {
        return methodDesc;
    }

    // jmp stub
    char line[256];
    DisasmAndClean (IP, line, 256);
    char *ptr = line;
    NextTerm (ptr);
    NextTerm (ptr);
    if (!strncmp (ptr, "jmp ", 4))
    {
        // jump thunk
        NextTerm (ptr);
        INT_PTR value;
        methodDesc = 0;
        if (GetValueFromExpr (ptr, value))
        {
            IP2MethodDesc (value, methodDesc, jitType, gcinfoAddr);
        }
        return methodDesc;
    }
    return 0;
}

// Handle a call instruction.
void HandleCall(TADDR callee, Register *reg)
{
    // call managed code?
    TADDR methodDesc = MDForCall (callee);
    if (methodDesc)
    {  
        DacpMethodDescData MethodDescData;
        if (MethodDescData.Request(g_sos, TO_CDADDR(methodDesc)) == S_OK)
        {
            NameForMD_s(methodDesc, g_mdName,mdNameLen);                    
            ExtOut(" (%S, mdToken: %p)", g_mdName, SOS_PTR(MethodDescData.MDToken));
            return;
        }
    }
    
#ifdef _TARGET_AMD64_
    // A jump thunk?

    CONTEXT ctx = {0};

    ctx.ContextFlags = (CONTEXT_AMD64 | CONTEXT_CONTROL | CONTEXT_INTEGER);

    for (unsigned ireg = 0; ireg < 16; ireg++)
    {
        if (reg[ireg].bValid)
        {
            *(&ctx.Rax + ireg) = reg[ireg].value;
        }
    }

    ctx.Rip = callee;

    CLRDATA_ADDRESS ip = 0, md = 0;
    if (S_OK == g_sos->GetJumpThunkTarget(&ctx, &ip, &md))
    {
        if (md)
        {
            DacpMethodDescData MethodDescData;
            if (MethodDescData.Request(g_sos, md) == S_OK)
            {
                NameForMD_s(md, g_mdName,mdNameLen);
                ExtOut(" (%S, mdToken: %p)", g_mdName, SOS_PTR(MethodDescData.MDToken));
                return;
            }
        }
        
        if (ip != callee)
        {
            return HandleCall(ip, reg);
        }
    }
#endif // _TARGET_AMD64_
    
    // A JitHelper?
    const char* name = HelperFuncName(callee);
    if (name) {
        ExtOut (" (JitHelp: %s)", name);
        return;
    }

    // call unmanaged code?
    char Symbol[1024];
    if (SUCCEEDED(g_ExtSymbols->GetNameByOffset(TO_CDADDR(callee), Symbol, 1024,
                                                NULL, NULL)))
    {
        if (Symbol[0] != '\0')
        {
            ExtOut (" (%s)", Symbol);
            return;
        }
    }
}

// Determine if a value is MT/MD/Obj
void HandleValue(TADDR value)
{
    // A MethodTable?
    if (IsMethodTable(value))
    {
        NameForMT_s (value, g_mdName,mdNameLen);
        ExtOut (" (MT: %S)", g_mdName);
        return;
    }
    
    // A Managed Object?
    TADDR dwMTAddr;
    move_xp (dwMTAddr, value);
    if (IsStringObject(value))
    {
        ExtOut (" (\"");
        StringObjectContent (value, TRUE);
        ExtOut ("\")");
        return;
    }
    else if (IsMethodTable(dwMTAddr))
    {
        NameForMT_s (dwMTAddr, g_mdName,mdNameLen);
        ExtOut (" (Object: %S)", g_mdName);
        return;
    }
    
    // A MethodDesc?
    if (IsMethodDesc(value))
    {        
        NameForMD_s (value, g_mdName,mdNameLen);
        ExtOut (" (MD: %S)", g_mdName);
        return;
    }

    // A JitHelper?
    const char* name = HelperFuncName(value);
    if (name) {
        ExtOut (" (JitHelp: %s)", name);
        return;
    }
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    Unassembly a managed code.  Translating managed object,           *  
*    call.                                                             *
*                                                                      *
\**********************************************************************/
void 
#ifdef _TARGET_X86_
    X86Machine::Unassembly 
#elif defined(_TARGET_AMD64_)
    AMD64Machine::Unassembly 
#endif
   (TADDR IPBegin, 
    TADDR IPEnd, 
    TADDR IPAskedFor, 
    TADDR GCStressCodeCopy, 
    GCEncodingInfo *pGCEncodingInfo, 
    SOSEHInfo *pEHInfo,
    BOOL bSuppressLines,
    BOOL bDisplayOffsets) const
{
    ULONG_PTR IP = IPBegin;
    char line[1024];
    Register reg [NumReg];
    ZeroMemory (reg, sizeof(reg));
    RegIndex dest;
    INT_PTR value;
    BOOL bDigit;
    char *ptr;

    ULONG curLine = -1;
    WCHAR filename[MAX_LONGPATH];
    ULONG linenum;

    while (IP < IPEnd)
    {
        if (IsInterrupt())
            return;

        // Print out line numbers if needed
        if (!bSuppressLines
            && SUCCEEDED(GetLineByOffset(TO_CDADDR(IP), &linenum, filename, MAX_LONGPATH)))
        {
            if (linenum != curLine)
            {
                curLine = linenum;
                ExtOut("\n%S @ %d:\n", filename, linenum);
            }
        }

        //
        // Print out any GC information corresponding to the current instruction offset.
        //

#ifndef FEATURE_PAL
        if (pGCEncodingInfo)
        {
            SIZE_T curOffset = (IP - IPBegin) + pGCEncodingInfo->hotSizeToAdd;
            while (   !pGCEncodingInfo->fDoneDecoding
                   && pGCEncodingInfo->ofs <= curOffset)
            {
                ExtOut(pGCEncodingInfo->buf);
                ExtOut("\n");
                SwitchToFiber(pGCEncodingInfo->pvGCTableFiber);
            }
        }
#endif // FEATURE_PAL        

        ULONG_PTR InstrAddr = IP;

        //
        // Print out any EH info corresponding to the current offset
        //
        if (pEHInfo)
        {
            pEHInfo->FormatForDisassembly(IP - IPBegin);
        }

        if (IP == IPAskedFor)
        {
            ExtOut (">>> ");
        }
        
        //
        // Print offsets, in addition to actual address.
        //
        if (bDisplayOffsets)
        {
            ExtOut("%04x ", IP - IPBegin);
        }

        DisasmAndClean (IP, line, _countof(line));

        // look at key word
        ptr = line;
        NextTerm (ptr);
        NextTerm (ptr);

        //
        // If there is gcstress info for this method, and this is a 'hlt'
        // instruction, then gcstress probably put the 'hlt' there.  Look
        // up the original instruction and print it instead.
        //        
        
        SSIZE_T cbIPOffset = 0;

        if (   GCStressCodeCopy
            && (   !strncmp (ptr, "hlt", 3)
                || !strncmp (ptr, "cli", 3)
                || !strncmp (ptr, "sti", 3)))
        {
            //
            // Compute address into saved copy of the code, and
            // disassemble the original instruction
            //
            
            ULONG_PTR OrigInstrAddr = GCStressCodeCopy + (InstrAddr - IPBegin);
            ULONG_PTR OrigIP = OrigInstrAddr;

            DisasmAndClean(OrigIP, line, _countof(line));

            //
            // Increment the real IP based on the size of the unmodifed
            // instruction
            //

            IP = InstrAddr + (OrigIP - OrigInstrAddr);

            cbIPOffset = IP - OrigIP;

            //
            // Print out real code address in place of the copy address
            //

#ifdef _WIN64
            ExtOut("%08x`%08x ", (ULONG)(InstrAddr >> 32), (ULONG)InstrAddr);
#else
            ExtOut("%08x ", (ULONG)InstrAddr);
#endif

            ptr = line;
            NextTerm (ptr);

            //
            // Print out everything after the code address, and skip the
            // instruction bytes
            //

            ExtOut(ptr);

            NextTerm (ptr);

            //
            // Add an indicator that this address has not executed yet
            //

            ExtOut(" (gcstress)");
        }
        else
        {
            ExtOut (line);
        }
    
        if (!strncmp (ptr, "mov ", 4))
        {
            NextTerm (ptr);

            dest = FindReg(ptr);
            if (dest != NONE)
            {
                NextTerm (ptr);

                if (FindSrc (ptr, reg, value, bDigit))
                {
                    reg[dest].bValid = TRUE;
                    reg[dest].value = value;
                    // Is it a managed obj
                    if (bDigit)
                        HandleValue (reg[dest].value);
                }
                else
                {
                    reg[dest].bValid = FALSE;
                }
            }
        }
        else if (!strncmp (ptr, "call ", 5))
        {
            NextTerm (ptr);
            if (FindSrc (ptr, reg, value, bDigit))
            {
                if (bDigit)
                    value += cbIPOffset;
                
                HandleCall (value, reg);
            }

            // trash EAX, ECX, EDX
            reg[EAX].bValid = FALSE;
            reg[ECX].bValid = FALSE;
            reg[EDX].bValid = FALSE;

#ifdef _TARGET_AMD64_
            reg[R8].bValid = FALSE;
            reg[R9].bValid = FALSE;
            reg[R10].bValid = FALSE;
            reg[R11].bValid = FALSE;
#endif // _TARGET_AMD64_
        }
        else if (!strncmp (ptr, "lea ", 4))
        {
            NextTerm (ptr);
            dest = FindReg(ptr);
            if (dest != NONE)
            {
                NextTerm (ptr);
                if (FindSrc (ptr, reg, value, bDigit))
                {
                    reg[dest].bValid = TRUE;
                    reg[dest].value = value;
                }
                else
                {
                    reg[dest].bValid = FALSE;
                }
            }
        }
        else if (!strncmp (ptr, "push ", 5))
        {
            // do not do anything
            NextTerm (ptr);
            if (FindSrc (ptr, reg, value, bDigit))
            {
                if (bDigit)
                {
                    HandleValue (value);
                }
            }
        }
        else
        {
            // assume this instruction will trash dest reg
            NextTerm (ptr);
            dest = FindReg(ptr);
            if (dest != NONE)
                reg[dest].bValid = FALSE;
        }
        ExtOut ("\n");
    }

    //
    // Print out any "end" EH info (where the end address is the byte immediately following the last instruction)
    //
    if (pEHInfo)
    {
        pEHInfo->FormatForDisassembly(IP - IPBegin);
    }
}

// Find the real callee site.  Handle JMP instruction.
// Return TRUE if we get the address, FALSE if not.
BOOL GetCalleeSite (TADDR IP, TADDR &IPCallee)
{
    while (TRUE) {
        unsigned char inst[2];
        if (g_ExtData->ReadVirtual(TO_CDADDR(IP), inst, sizeof(inst), NULL) != S_OK)
        {
            return FALSE;
        }
        if (inst[0] == 0xEB) {
            IP += 2+(char)inst[1];
        }
        else if (inst[0] == 0xE9) {
            int displace;
            if (g_ExtData->ReadVirtual(TO_CDADDR(IP+1), &displace, sizeof(displace), NULL) != S_OK)
            {
                return FALSE;
            }
            else
            {
                IP += 5+displace;
            }
        }
        else if (inst[0] == 0xFF && (inst[1] & 070) == 040) {
            if (inst[1] == 0x25) {
                DWORD displace;
                if (g_ExtData->ReadVirtual(TO_CDADDR(IP+2), &displace, sizeof(displace), NULL) != S_OK)
                {
                    return FALSE;
                }
                if (g_ExtData->ReadVirtual(TO_CDADDR(displace), &displace, sizeof(displace), NULL) != S_OK)
                {
                    return FALSE;
                }
                else
                {
                    IP = displace;
                }
            }
            else
                // Target for jmp is determined from register values.
                return FALSE;
        }
        else
        {
            IPCallee = IP;
            return TRUE;
        }
    }
}

// GetFinalTarget is based on HandleCall, but avoids printing anything to the output.
// This is currently only called on x64
eTargetType GetFinalTarget(TADDR callee, TADDR* finalMDorIP)
{
    // call managed code?
    TADDR methodDesc = MDForCall (callee);
    if (methodDesc)
    {  
        DacpMethodDescData MethodDescData;
        if (MethodDescData.Request(g_sos, TO_CDADDR(methodDesc)) == S_OK)
        {
            *finalMDorIP = methodDesc;
            return ettMD;
        }
    }
    
#ifdef _TARGET_AMD64_
    // A jump thunk?

    CONTEXT ctx = {0};
    ctx.ContextFlags = (CONTEXT_AMD64 | CONTEXT_CONTROL | CONTEXT_INTEGER);
    ctx.Rip = callee;

    CLRDATA_ADDRESS ip = 0, md = 0;
    if (S_OK == g_sos->GetJumpThunkTarget(&ctx, &ip, &md))
    {
        if (md)
        {
            DacpMethodDescData MethodDescData;
            if (MethodDescData.Request(g_sos, md) == S_OK)
            {
                *finalMDorIP = md;
                return ettStub;
            }
        }
        
        if (ip != callee)
        {
            return GetFinalTarget(ip, finalMDorIP);
        }
    }
#endif // _TARGET_AMD64_
    
    // A JitHelper?
    const char* name = HelperFuncName(callee);
    if (name) {
        *finalMDorIP = callee;
        return ettJitHelp;
    }

    // call unmanaged code?
    *finalMDorIP = callee;
    return ettNative;
}

#ifndef FEATURE_PAL

void ExpFuncStateInit (TADDR *IPRetAddr)
{
    ULONG64 offset;
    if (FAILED(g_ExtSymbols->GetOffsetByName("ntdll!KiUserExceptionDispatcher", &offset))) {
        return;
    }

    // test if we have a minidump for which the image is not cached anymore. this avoids
    // the having the while loop below spin forever (or a very long time)... 
    // (Watson backend hit this a few times, and they had to institute a timeout policy
    // to work around this)
    SIZE_T instrs;
    if (FAILED(g_ExtData->ReadVirtual(offset, &instrs, sizeof(instrs), NULL)) || instrs == 0) {
        return;
    }

    char line[256];
    int i = 0; 
    int cnt = 0;
#ifdef SOS_TARGET_X86
    // On x86 and x64 the last 3 "call" instructions in ntdll!KiUserExceptionDispatcher
    // are making calls to OS APIs that take as argument the context record (and some
    // of them the exception record as well)
    const int cCallInstrs = 3;
#elif defined(SOS_TARGET_AMD64)
    // On x64 the first "call" instruction should be considered, as well 
    const int cCallInstrs = 4;
#endif

    while (i < cCallInstrs) {
        g_ExtControl->Disassemble (offset, 0, line, 256, NULL, &offset);
        if (strstr (line, "call")) {
            IPRetAddr[i++] = (TADDR)offset;
        }
        // if we didn't find at least one "call" in the first 500 instructions give up...
        if (++cnt >= 500 && IPRetAddr[0] == 0)
            break;
    }
}

#endif // FEATURE_PAL


/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to fill in a cross platform context       *
*    struct by looking on the stack for return addresses into          *
*    KiUserExceptionDispatcher                                         *
*                                                                      *
\**********************************************************************/
BOOL
#ifdef SOS_TARGET_X86
    X86Machine::GetExceptionContext 
#elif defined(SOS_TARGET_AMD64)
    AMD64Machine::GetExceptionContext 
#endif
    (TADDR       stack, 
     TADDR       IP, 
     TADDR     * cxrAddr, 
     CROSS_PLATFORM_CONTEXT * pcxr,
     TADDR     * exrAddr, 
     PEXCEPTION_RECORD exr) const
{
#ifndef FEATURE_PAL
#ifdef SOS_TARGET_X86
    X86_CONTEXT * cxr = &pcxr->X86Context;
    size_t contextSize = offsetof(CONTEXT, ExtendedRegisters);
#elif defined(SOS_TARGET_AMD64)
    AMD64_CONTEXT * cxr = &pcxr->Amd64Context;
    size_t contextSize = offsetof(CONTEXT, FltSave);
#endif

    static TADDR IPRetAddr[4] = {0, 0, 0, 0};

    if (IPRetAddr[0] == 0) {
        ExpFuncStateInit (IPRetAddr);
    }
    *cxrAddr = 0;
    *exrAddr = 0;

#ifdef SOS_TARGET_X86

    if (IP == IPRetAddr[0]) {
        *exrAddr = stack + sizeof(TADDR);
        *cxrAddr = stack + 2*sizeof(TADDR);
    }
    else if (IP == IPRetAddr[1]) {
        *cxrAddr = stack + sizeof(TADDR);
    }
    else if (IP == IPRetAddr[2]) {
        *exrAddr = stack + sizeof(TADDR);
        *cxrAddr = stack + 2*sizeof(TADDR);
    }
    else
        return FALSE;

    if (FAILED (g_ExtData->ReadVirtual(TO_CDADDR(*cxrAddr), &stack, sizeof(stack), NULL)))
        return FALSE;
    *cxrAddr = stack;

    //if ((pContext->ContextFlags & CONTEXT_EXTENDED_REGISTERS) == CONTEXT_EXTENDED_REGISTERS)
    //    contextSize += sizeof(pContext->ExtendedRegisters);
    if (FAILED (g_ExtData->ReadVirtual(TO_CDADDR(stack), cxr, (ULONG)contextSize, NULL))) {
        return FALSE;
    }

    if (*exrAddr) {
        if (FAILED (g_ExtData->ReadVirtual(TO_CDADDR(*exrAddr), &stack, sizeof(stack), NULL)))
        {
            *exrAddr = 0;
            return TRUE;
        }
        *exrAddr = stack;
        size_t erSize = offsetof (EXCEPTION_RECORD, ExceptionInformation);
        if (FAILED (g_ExtData->ReadVirtual(TO_CDADDR(stack), exr, (ULONG)erSize, NULL))) {
            *exrAddr = 0;
            return TRUE;
        }
    }

#elif defined(SOS_TARGET_AMD64)

    if (IP == IPRetAddr[0] || IP == IPRetAddr[1] || IP == IPRetAddr[3]) {
        *exrAddr = stack + sizeof(TADDR) + 0x4F0;
        *cxrAddr = stack + sizeof(TADDR);
    } else if (IP == IPRetAddr[2]) {
        *cxrAddr = stack + sizeof(TADDR);
    }
    else {
        return FALSE;
    }

    if (FAILED (g_ExtData->ReadVirtual(TO_CDADDR(*cxrAddr), cxr, (ULONG)contextSize, NULL))) {
        return FALSE;
    }

    if (*exrAddr) {
        size_t erSize = offsetof (EXCEPTION_RECORD, ExceptionInformation);
        if (FAILED (g_ExtData->ReadVirtual(TO_CDADDR(*exrAddr), exr, (ULONG)erSize, NULL))) {
            *exrAddr = 0;
            return TRUE;
        }
    }

#endif
    return TRUE;
#else
    return FALSE;
#endif // FEATURE_PAL
}


/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to determine if a DWORD on the stack is   *
*    a return address.                                                 *
*    It does this by checking several bytes before the DWORD to see if *
*    there is a call instruction.                                      *
*                                                                      *
\**********************************************************************/

void 
#ifdef _TARGET_X86_
    X86Machine::IsReturnAddress
#elif defined(_TARGET_AMD64_)
    AMD64Machine::IsReturnAddress
#endif
        (TADDR retAddr, TADDR* whereCalled) const
{
    *whereCalled = 0;

    unsigned char spotend[6];
    move_xp (spotend, retAddr-6);
    unsigned char *spot = spotend+6;
    TADDR addr;
    
    // Note this is possible to be spoofed, but pretty unlikely
    // call XXXXXXXX
    if (spot[-5] == 0xE8) {
        DWORD offs = 0;
        move_xp (offs, retAddr-4);
        *whereCalled = retAddr + (ULONG64)(LONG)(offs);
        //*whereCalled = *((int*) (retAddr-4)) + retAddr;
        // on WOW64 the range valid for code is almost the whole 4GB address space
        if (g_ExtData->ReadVirtual(TO_CDADDR(*whereCalled), &addr, sizeof(addr), NULL) == S_OK)
        {
            TADDR callee;
            if (GetCalleeSite(*whereCalled, callee)) {
                *whereCalled = callee;
            }
            return;
        }
        else
            *whereCalled = 0;
    }

    // call [XXXXXXXX]
    if (spot[-6] == 0xFF && (spot[-5] == 025))  {
        DWORD offs = 0;
        move_xp (offs, retAddr-4);
#ifdef _TARGET_AMD64_
        // on x64 this 32-bit is an RIP offset
        addr = retAddr + (ULONG64)(LONG)(offs);
#elif defined (_TARGET_X86_)
        addr = offs;
#endif
        if (g_ExtData->ReadVirtual(TO_CDADDR(addr), whereCalled, sizeof(*whereCalled), NULL) == S_OK) {
            move_xp (*whereCalled, addr);
            //*whereCalled = **((unsigned**) (retAddr-4));
            // on WOW64 the range valid for code is almost the whole 4GB address space
            if (g_ExtData->ReadVirtual(TO_CDADDR(*whereCalled), &addr, sizeof(addr), NULL) == S_OK) 
            {
                TADDR callee;
                if (GetCalleeSite(*whereCalled,callee)) {
                    *whereCalled = callee;
                }
                return;
            }
            else
                *whereCalled = 0;
        }
        else
            *whereCalled = 0;
    }

    // call [REG+XX]
    if (spot[-3] == 0xFF && (spot[-2] & ~7) == 0120 && (spot[-2] & 7) != 4)
    {
        *whereCalled = 0xFFFFFFFF;
        return;
    }
    if (spot[-4] == 0xFF && spot[-3] == 0124)
    {
        *whereCalled = 0xFFFFFFFF;
        return;
    }

    // call [REG+XXXX]
    if (spot[-6] == 0xFF && (spot[-5] & ~7) == 0220 && (spot[-5] & 7) != 4)
    {
        *whereCalled = 0xFFFFFFFF;
        return;
    }
    if (spot[-7] == 0xFF && spot[-6] == 0224)
    {
        *whereCalled = 0xFFFFFFFF;
        return;
    }
    
    // call [REG]
    if (spot[-2] == 0xFF && (spot[-1] & ~7) == 0020 && (spot[-1] & 7) != 4 && (spot[-1] & 7) != 5)
    {
        *whereCalled = 0xFFFFFFFF;
        return;
    }
    
    // call REG
    if (spot[-2] == 0xFF && (spot[-1] & ~7) == 0320 && (spot[-1] & 7) != 4)
    {
        *whereCalled = 0xFFFFFFFF;
        return;
    }
    
    // There are other cases, but I don't believe they are used.
    return;
}


#ifdef _X86_

///
/// This is dead code, not called from anywhere, not linked in the final product.
///
static BOOL DecodeLine (___in __in_z char *line, ___in __in_z const char *const inst, InstData& arg1, InstData& arg2)
{
    char *ptr = line;
    if (inst[0] == '*' || !strncmp (ptr, inst, strlen (inst)))
    {
        arg1.mode = BAD;
        arg2.mode = BAD;
        NextTerm (ptr);
        if (*ptr == '\0')
        {
            arg1.mode = NODATA;
            return TRUE;
        }

        DecodeAddressTerm (ptr, arg1);
        NextTerm (ptr);
        if (*ptr == '\0')
        {
            return TRUE;
        }
        DecodeAddressTerm (ptr, arg2);
        return TRUE;
    }
    else
        return FALSE;
}

void PrintReg (Register *reg)
{
    ExtOut ("[EBX=%08x ESI=%08x EDI=%08x EBP=%08x ESP=%08x]\n",
             reg[EBX].value, reg[ESI].value, reg[EDI].value, reg[EBP].value,
             reg[ESP].value);
}


struct CallInfo
{
    DWORD_PTR stackPos;
    DWORD_PTR retAddr;
    DWORD_PTR whereCalled;
};

// Search for a Return address on stack.
BOOL GetNextRetAddr (DWORD_PTR stackBegin, DWORD_PTR stackEnd,
                     CallInfo &callInfo)
{
    for (callInfo.stackPos = stackBegin;
         callInfo.stackPos <= stackEnd;
         callInfo.stackPos += 4)
    {
        if (!SafeReadMemory (callInfo.stackPos, &callInfo.retAddr, 4, NULL))
            continue;

        g_targetMachine->IsReturnAddress(callInfo.retAddr, &callInfo.whereCalled);
        if (callInfo.whereCalled)
        {
            return TRUE;
        }
    }
    
    return FALSE;
}

struct FrameInfo
{
    DWORD_PTR IPStart;
    DWORD_PTR Prolog;
    DWORD_PTR FrameBase;   // The value of ESP at the entry.
    DWORD_PTR StackEnd;
    DWORD_PTR argCount;
    BOOL bEBPFrame;
};

// if a EBP frame, return TRUE if EBP has been setup
void GetFrameBaseHelper (DWORD_PTR IPBegin, DWORD_PTR IPEnd,
                         INT_PTR &StackChange)
{
    char line[256];
    char *ptr;
    InstData arg1;
    InstData arg2;
    DWORD_PTR IP = IPBegin;
    StackChange = 0;
    while (IP < IPEnd)
    {
        DisasmAndClean (IP, line, 256);
        ptr = line;
        NextTerm (ptr);
        NextTerm (ptr);
        if (DecodeLine (ptr, "push ", arg1, arg2))
        {
            StackChange += 4;
        }
        else if (DecodeLine (ptr, "pop ", arg1, arg2))
        {
            StackChange -= 4;
        }
        else if (DecodeLine (ptr, "sub ", arg1, arg2))
        {
            if (arg1.mode == REG && arg1.reg[0].reg == ESP)
            {
                if (arg2.mode == DATA)
                    StackChange -= arg2.value;
            }
        }
        else if (DecodeLine (ptr, "add ", arg1, arg2))
        {
            if (arg1.mode == REG && arg1.reg[0].reg == ESP)
            {
                if (arg2.mode == DATA)
                    StackChange += arg2.value;
            }
        }
        else if (!strncmp (ptr, "ret", 3)) {
            return;
        }
    }
}

enum IPSTATE {IPPROLOG1 /*Before EBP set*/, IPPROLOG2 /*After EBP set*/, IPCODE, IPEPILOG, IPEND};

IPSTATE GetIpState (DWORD_PTR IP, FrameInfo* pFrame)
{
    char line[256];
    char *ptr;
    
    if (IP >= pFrame->IPStart && IP < pFrame->IPStart + pFrame->Prolog)
    {
        if (pFrame->bEBPFrame) {
            DWORD_PTR pIP = pFrame->IPStart;
            while (pIP < IP) {
                DisasmAndClean (IP,line, 256);
                ptr = line;
                NextTerm (ptr);
                NextTerm (ptr);
                if (!strncmp (ptr, "mov ", 4)) {
                    NextTerm (ptr);
                    if (!strncmp (ptr, "ebp", 3)) {
                        NextTerm (ptr);
                        if (!strncmp (ptr, "esp", 3)) {
                            return IPPROLOG2;
                        }
                    }
                }
                else if (!strncmp (ptr, "call ", 5)) {
                    NextTerm (ptr);
                    if (strstr (ptr, "__EH_prolog")) {
                        return IPPROLOG2;
                    }
                }
            }
            pIP = IP;
            while (pIP < pFrame->IPStart + pFrame->Prolog) {
                DisasmAndClean (IP,line, 256);
                ptr = line;
                NextTerm (ptr);
                NextTerm (ptr);
                if (!strncmp (ptr, "mov ", 4)) {
                    NextTerm (ptr);
                    if (!strncmp (ptr, "ebp", 3)) {
                        NextTerm (ptr);
                        if (!strncmp (ptr, "esp", 3)) {
                            return IPPROLOG1;
                        }
                    }
                }
                else if (!strncmp (ptr, "call ", 5)) {
                    NextTerm (ptr);
                    if (strstr (ptr, "__EH_prolog")) {
                        return IPPROLOG1;
                    }
                }
            }

            ExtOut ("Fail to find where EBP is saved\n");
            return IPPROLOG2;
        }
        else
        {
            return IPPROLOG1;
        }
    }
    
    int nline = 0;
    while (1) {
        DisasmAndClean (IP,line, 256);
        nline ++;
        ptr = line;
        NextTerm (ptr);
        NextTerm (ptr);
        if (!strncmp (ptr, "ret", 3)) {
            return (nline==1)?IPEND:IPEPILOG;
        }
        else if (!strncmp (ptr, "leave", 5)) {
            return IPEPILOG;
        }
        else if (!strncmp (ptr, "call", 4)) {
            return IPCODE;
        }
        else if (ptr[0] == 'j') {
            return IPCODE;
        }
    }
}

// FrameBase is the ESP value at the entry of a function.
BOOL GetFrameBase (Register callee[], FrameInfo* pFrame)
{
    //char line[256];
    //char *ptr;
    INT_PTR dwpushed = 0;
    //DWORD_PTR IP;
    
    IPSTATE IpState = GetIpState (callee[EIP].value, pFrame);

    if (pFrame->bEBPFrame)
    {
        if (IpState == IPEND || IpState == IPPROLOG1) {
            pFrame->FrameBase = callee[ESP].value;
        }
        else
        {
            pFrame->FrameBase = callee[EBP].value+4;
        }
        return TRUE;
    }
    else
    {
        if (IpState == IPEND) {
            pFrame->FrameBase = callee[ESP].value;
            return TRUE;
        }

        DWORD_PTR IPBegin, IPEnd;
        if (IpState == IPEPILOG) {
            IPBegin = callee[EIP].value;
            IPEnd = ~0ul;
        }
        else if (IpState == IPPROLOG1) {
            IPBegin = pFrame->IPStart;
            IPEnd = callee[EIP].value;
        }
        else
        {
            IPBegin = pFrame->IPStart;
            IPEnd = IPBegin + pFrame->Prolog;
        }
        GetFrameBaseHelper (IPBegin, IPEnd, dwpushed);

        if (IpState == IPEPILOG) {
            ExtOut ("stack %d\n", dwpushed);
            pFrame->FrameBase = callee[ESP].value - dwpushed;
            return TRUE;
        }

        CallInfo callInfo;
        if (GetNextRetAddr (callee[ESP].value + dwpushed,
                            pFrame->StackEnd, callInfo))
        {
            pFrame->FrameBase = callInfo.stackPos;
            return TRUE;
        }

        return FALSE;
    }
}

// caller[ESP]: the ESP value when we return to caller.
void RestoreCallerRegister (Register callee[], Register caller[],
                            FrameInfo *pFrame)
{
    if (pFrame->bEBPFrame)
    {
        if (callee[ESP].value < pFrame->FrameBase)
        {
            SafeReadMemory (pFrame->FrameBase-4, &caller[EBP].value, 4, NULL);
        }
        else
            caller[EBP].value = callee[EBP].value;
    }
    else
        caller[EBP].value = callee[EBP].value;
    
    caller[EBP].bValid = TRUE;
    caller[ESP].value = pFrame->FrameBase + 4 + pFrame->argCount;
    callee[EBP].value = pFrame->FrameBase - sizeof(void*);
    SafeReadMemory (pFrame->FrameBase, &caller[EIP].value, 4, NULL);
}

BOOL GetFrameInfoHelper (Register callee[], Register caller[],
                         FrameInfo *pFrame)
{
    if (GetFrameBase (callee, pFrame))
    {
        RestoreCallerRegister (callee, caller, pFrame);
        return TRUE;
    }
    else
        return FALSE;
}

// Return TRUE if Frame Info is OK, otherwise FALSE.
BOOL GetUnmanagedFrameInfo (Register callee[], Register caller[],
                            DumpStackFlag &DSFlag, PFPO_DATA data)
{
    FrameInfo Frame;
    ULONG64 base;
    g_ExtSymbols->GetModuleByOffset (callee[EIP].value, 0, NULL, &base);
    Frame.IPStart = data->ulOffStart + (ULONG_PTR)base;
    Frame.Prolog = data->cbProlog;
    // Why do we have to do this to make it work?
    if (Frame.Prolog == 1) {
        Frame.Prolog = 0;
    }
    Frame.bEBPFrame = (data->cbFrame == FRAME_NONFPO);
    Frame.StackEnd = DSFlag.end;
    Frame.argCount = data->cdwParams*4;

    return GetFrameInfoHelper (callee, caller, &Frame);
}

// offsetEBP: offset of stack position where EBP is saved.
// If EBP is not saved, *offsetEBP = -1 (~0ul);
BOOL IPReachable (DWORD_PTR IPBegin, DWORD_PTR IP, DWORD *offsetEBP)
{
    *offsetEBP = ~0ul;
    return FALSE;
}
    
BOOL HandleEEStub (Register callee[], Register caller[], 
                   DumpStackFlag &DSFlag)
{
    // EEStub can only be called by IP directory.  Let's look for possible caller.
    CallInfo callInfo;
    DWORD_PTR stackPos = callee[ESP].value;
    while (stackPos < DSFlag.end) {
        if (GetNextRetAddr (stackPos,
                            DSFlag.end, callInfo))
        {
            if (callInfo.whereCalled != ~0ul) {
                DWORD offsetEBP;
                if (IPReachable (callInfo.whereCalled, callee[EIP].value, &offsetEBP)) {
                    caller[EIP].value = callInfo.retAddr;
                    // TODO: We may have saved EBP.
                    if (offsetEBP == ~0ul) {
                        caller[EBP].value = callee[EBP].value;
                    }
                    else
                    {
                        TADDR offs = TO_TADDR(callInfo.stackPos)-sizeof(PVOID)-offsetEBP;
                        SafeReadMemory (offs, &caller[EBP].value, sizeof(PVOID), NULL);
                    }
                    caller[ESP].value = callInfo.stackPos+sizeof(PVOID);
                    return TRUE;
                }
            }
            stackPos = callInfo.stackPos+sizeof(PVOID);
        }
        else
            return FALSE;
    }

    return FALSE;
}


BOOL HandleByEpilog (Register callee[], Register caller[], 
                    DumpStackFlag &DSFlag)
{
    return FALSE;
}

#ifndef FEATURE_PAL
void RestoreFrameUnmanaged (Register *reg, DWORD_PTR CurIP)
{
    char line[256];
    char *ptr;
    DWORD_PTR IP = CurIP;
    INT_PTR value;
    BOOL bDigit;
    BOOL bGoodESP = true;
    RegIndex dest;

    ULONG64 base;
    g_ExtSymbols->GetModuleByOffset (TO_CDADDR(CurIP), 0, NULL, &base);
    ULONG64 handle;
    g_ExtSystem->GetCurrentProcessHandle(&handle);
    PFPO_DATA data =
        (PFPO_DATA)SymFunctionTableAccess((HANDLE)handle, CurIP);
    DWORD_PTR IPBegin = data->ulOffStart + (ULONG_PTR)base;

    if (CurIP - IPBegin <= data->cbProlog)
    {
        // We are inside a prolog.
        // See where we save the callee saved register.
        // Also how many DWORD's we pushd
        IP = IPBegin;
        reg[ESP].stack = 0;
        reg[ESP].bOnStack = FALSE;
        reg[EBP].stack = 0;
        reg[EBP].bOnStack = FALSE;
        reg[ESI].stack = 0;
        reg[ESI].bOnStack = FALSE;
        reg[EDI].stack = 0;
        reg[EDI].bOnStack = FALSE;
        reg[EBX].stack = 0;
        reg[EBX].bOnStack = FALSE;

        while (IP < CurIP)
        {
            DisasmAndClean (IP, line, 256);
            ptr = line;
            NextTerm (ptr);
            NextTerm (ptr);
            if (!strncmp (ptr, "push ", 5))
            {
                reg[ESP].stack += 4;
                NextTerm (ptr);
                dest = FindReg(ptr);
                if (dest == EBP || dest == EBX || dest == ESI || dest == EDI)
                {
                    reg[dest].bOnStack = TRUE;
                    reg[dest].stack = reg[ESP].stack;
                }
            }
            else if (!strncmp (ptr, "sub ", 4))
            {
                NextTerm (ptr);
                dest = FindReg(ptr);
                if (dest == ESP)
                {
                    NextTerm (ptr);
                    char *endptr;
                    reg[ESP].stack += strtoul(ptr, &endptr, 16);;
                }
            }
        }
        
        DWORD_PTR baseESP = reg[ESP].value + reg[ESP].stack;
        if (reg[EBP].bOnStack)
        {
            move_xp (reg[EBP].value, baseESP-reg[EBP].stack);
        }
        if (reg[EBX].bOnStack)
        {
            move_xp (reg[EBX].value, baseESP-reg[EBX].stack);
        }
        if (reg[ESI].bOnStack)
        {
            move_xp (reg[ESI].value, baseESP-reg[ESI].stack);
        }
        if (reg[EDI].bOnStack)
        {
            move_xp (reg[EDI].value, baseESP-reg[EDI].stack);
        }
        move_xp (reg[EIP].value, baseESP);
        reg[ESP].value = baseESP + 4;
        return;
    }

    if (data->cbFrame == FRAME_NONFPO)
    {
        // EBP Frame
    }
    
    // Look for epilog
    while (1)
    {
        DisasmAndClean (IP, line, 256);
        ptr = line;
        NextTerm (ptr);
        NextTerm (ptr);
        if (!strncmp (ptr, "mov ", 4))
        {
            NextTerm (ptr);
            dest = FindReg(ptr);
            if (dest == ESP)
            {
                NextTerm (ptr);
                if (FindReg(ptr) == EBP)
                {
                    // We have a EBP frame
                    bGoodESP = true;
                    reg[ESP].value = reg[EBP].value;
                }
            }
        }
        else if (!strncmp (ptr, "ret", 3))
        {
            NextTerm (ptr);
            // check the value on stack is a return address.
            DWORD_PTR retAddr;
            DWORD_PTR whereCalled;
            move_xp (retAddr, reg[ESP].value);
            int ESPAdjustCount = 0;
            while (1)
            {
                g_targetMachine->IsReturnAddress(retAddr, &whereCalled);
                if (whereCalled)
                    break;
                ESPAdjustCount ++;
                reg[ESP].value += 4;
                move_xp (retAddr, reg[ESP].value);
            }
            reg[EIP].value = retAddr;
            if (ESPAdjustCount)
            {
                ESPAdjustCount *= 4;
            }
            if (reg[EBX].bOnStack)
            {
                reg[EBX].stack += ESPAdjustCount;
                move_xp (reg[EBX].value, reg[EBX].stack);
            }
            if (reg[ESI].bOnStack)
            {
                reg[ESI].stack += ESPAdjustCount;
                move_xp (reg[ESI].value, reg[EBX].stack);
            }
            if (reg[EDI].bOnStack)
            {
                reg[EDI].stack += ESPAdjustCount;
                move_xp (reg[EDI].value, reg[EBX].stack);
            }
            
            reg[ESP].value += 4;
            if (ptr[0] != '\0')
            {
                FindSrc (ptr, reg, value, bDigit);
                reg[ESP].value += value;
            }
            break;
        }
        else if (!strncmp (ptr, "pop ", 4))
        {
            NextTerm (ptr);
            dest = FindReg(ptr);
            if (dest == EBP || dest == EBX || dest == ESI || dest == EDI)
            {
                reg[dest].stack = reg[ESP].value;
                reg[dest].bOnStack = TRUE;
            }
            reg[ESP].value += 4;
        }
        else if (!strncmp (ptr, "add ", 4))
        {
            NextTerm (ptr);
            dest = FindReg(ptr);
            if (dest == ESP)
            {
                NextTerm (ptr);
                FindSrc (ptr, reg, value, bDigit);
                reg[ESP].value += value;
            }
        }
        else if (!strncmp (ptr, "call ", 5))
        {
            // assume we do not have a good value on ESP.
            // We could go into the call and find out number of pushed args.
            bGoodESP = FALSE;
        }
    }
    
    // Look for prolog
}
#endif // !FEATURE_PAL

#elif defined(_AMD64_)


#endif // !_X86_
