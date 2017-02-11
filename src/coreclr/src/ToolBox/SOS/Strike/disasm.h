// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
#ifndef __disasm_h__
#define __disasm_h__

#include "sos_stacktrace.h"

struct InfoHdr;
class GCDump;


struct DumpStackFlag
{
    BOOL fEEonly;
    BOOL fSuppressSrcInfo;
    DWORD_PTR top;
    DWORD_PTR end;
};

struct GCEncodingInfo
{
    LPVOID pvMainFiber;
    LPVOID pvGCTableFiber;

    BYTE *table;
    unsigned int methodSize;

    char buf[1000];
    int cch;

    SIZE_T ofs;
    
    // When decoding a cold region, set this to the size of the hot region to keep offset
    // calculations working.
    SIZE_T hotSizeToAdd;    
    bool fDoneDecoding;
};

// Returns:
//   NULL if the EHInfo passed in does not refer to a Typed clause
//   "..." if pEHInfo->isCatchAllHandler is TRUE
//   "TypeName" if pEHInfo is a DACEHInfo* that references type "TypeName".
// Note:
//   The return is a pointer to a global buffer, therefore this value must
//   be consumed as soon as possible after a call to this function.
LPCWSTR EHTypedClauseTypeName(const DACEHInfo* pEHInfo);

struct SOSEHInfo
{
    DACEHInfo  *m_pInfos;
    UINT        EHCount;
    CLRDATA_ADDRESS methodStart;

    SOSEHInfo()   { ZeroMemory(this, sizeof(SOSEHInfo)); }
    ~SOSEHInfo()  { if (m_pInfos) { delete [] m_pInfos; } }    

    void FormatForDisassembly(CLRDATA_ADDRESS offSet);
};

BOOL IsClonedFinally(DACEHInfo *pEHInfo);

void DumpStackWorker (DumpStackFlag &DSFlag);

void UnassemblyUnmanaged (DWORD_PTR IP, BOOL bSuppressLines);

HRESULT CheckEEDll ();

BOOL GetCalleeSite (DWORD_PTR IP, DWORD_PTR &IPCallee);

void DisasmAndClean (DWORD_PTR &IP, __out_ecount_opt(length) char *line, ULONG length);

INT_PTR GetValueFromExpr(___in __in_z char *ptr, INT_PTR &value);

void NextTerm (__deref_inout_z char *& ptr);

BOOL IsByRef (__deref_inout_z char *& ptr);

BOOL IsTermSep (char ch);

const char * HelperFuncName (size_t IP);

enum eTargetType { ettUnk = 0, ettNative = 1, ettJitHelp = 2, ettStub = 3, ettMD = 4 };

// GetFinalTarget is based on HandleCall, but avoids printing anything to the output.
// This is currently only called on x64
eTargetType GetFinalTarget(DWORD_PTR callee, DWORD_PTR* finalMDorIP);

#ifdef _MSC_VER
// SOS is essentially single-threaded. ignore "construction of local static object is not thread-safe"
#pragma warning(push)
#pragma warning(disable:4640)
#endif // _MSC_VER

//-----------------------------------------------------------------------------------------
//
//  Implementations for the supported target platforms
//
//-----------------------------------------------------------------------------------------

#ifndef THUMB_CODE
#define THUMB_CODE 1
#endif
#define STACKWALK_CONTROLPC_ADJUST_OFFSET 2

#ifdef SOS_TARGET_X86

/// X86 Machine specific code
class X86Machine : public IMachine
{
public:
    typedef X86_CONTEXT TGT_CTXT;

    static IMachine* GetInstance()
    { static X86Machine s_X86MachineInstance; return &s_X86MachineInstance; }

    ULONG GetPlatform()             const { return IMAGE_FILE_MACHINE_I386; }
    ULONG GetContextSize()          const { return sizeof(X86_CONTEXT); }
    virtual void Unassembly(
                TADDR IPBegin, 
                TADDR IPEnd, 
                TADDR IPAskedFor, 
                TADDR GCStressCodeCopy, 
                GCEncodingInfo * pGCEncodingInfo, 
                SOSEHInfo *pEHInfo,
                BOOL bSuppressLines,
                BOOL bDisplayOffsets) const;
    virtual void IsReturnAddress(
                TADDR retAddr, 
                TADDR* whereCalled) const;
    virtual BOOL GetExceptionContext (
                TADDR stack, 
                TADDR PC, 
                TADDR *cxrAddr, 
                CROSS_PLATFORM_CONTEXT * cxr,
                TADDR * exrAddr, 
                PEXCEPTION_RECORD exr) const;

    // retrieve stack pointer, frame pointer, and instruction pointer from the target context
    virtual TADDR GetSP(const CROSS_PLATFORM_CONTEXT & ctx) const  { return ctx.X86Context.Esp; }
    virtual TADDR GetBP(const CROSS_PLATFORM_CONTEXT & ctx) const  { return ctx.X86Context.Ebp; }
    virtual TADDR GetIP(const CROSS_PLATFORM_CONTEXT & ctx) const  { return ctx.X86Context.Eip; }
    
    virtual void  FillSimpleContext(StackTrace_SimpleContext * dest, LPVOID srcCtx) const;
    virtual void  FillTargetContext(LPVOID destCtx, LPVOID srcCtx, int idx = 0) const;

    virtual LPCSTR GetDumpStackHeading() const          { return s_DumpStackHeading; }
    virtual LPCSTR GetDumpStackObjectsHeading() const   { return s_DSOHeading; }
    virtual LPCSTR GetSPName() const                    { return s_SPName; }
    virtual void GetGCRegisters(LPCSTR** regNames, unsigned int* cntRegs) const
    { _ASSERTE(cntRegs != NULL); *regNames = s_GCRegs; *cntRegs = _countof(s_GCRegs); }

    virtual void DumpGCInfo(GCInfoToken gcInfoToken, unsigned methodSize, printfFtn gcPrintf, bool encBytes, bool bPrintHeader) const;

private:
    X86Machine()  {}
    ~X86Machine() {}
    X86Machine(const X86Machine& machine);      // undefined
    X86Machine & operator=(const X86Machine&);  // undefined

private:
    static LPCSTR     s_DumpStackHeading;
    static LPCSTR     s_DSOHeading;
    static LPCSTR     s_GCRegs[7];
    static LPCSTR     s_SPName;
}; // class X86Machine

#endif // SOS_TARGET_X86


#ifdef SOS_TARGET_ARM

/// ARM Machine specific code
class ARMMachine : public IMachine
{
public:
    typedef ARM_CONTEXT TGT_CTXT;
    
    static IMachine* GetInstance()
    { return &s_ARMMachineInstance; }

    ULONG GetPlatform()             const { return IMAGE_FILE_MACHINE_ARMNT; }
    ULONG GetContextSize()          const { return sizeof(ARM_CONTEXT); }
    virtual void Unassembly(
                TADDR IPBegin, 
                TADDR IPEnd, 
                TADDR IPAskedFor, 
                TADDR GCStressCodeCopy, 
                GCEncodingInfo *pGCEncodingInfo, 
                SOSEHInfo *pEHInfo,
                BOOL bSuppressLines,
                BOOL bDisplayOffsets) const;
    virtual void IsReturnAddress(
                TADDR retAddr, 
                TADDR* whereCalled) const;
    virtual BOOL GetExceptionContext (
                TADDR stack, 
                TADDR PC, 
                TADDR *cxrAddr, 
                CROSS_PLATFORM_CONTEXT * cxr,
                TADDR *exrAddr, 
                PEXCEPTION_RECORD exr) const;

    // retrieve stack pointer, frame pointer, and instruction pointer from the target context
    virtual TADDR GetSP(const CROSS_PLATFORM_CONTEXT & ctx) const { return ctx.ArmContext.Sp; }
    // @ARMTODO: frame pointer
    virtual TADDR GetBP(const CROSS_PLATFORM_CONTEXT & ctx) const { return 0; }
    virtual TADDR GetIP(const CROSS_PLATFORM_CONTEXT & ctx) const { return ctx.ArmContext.Pc; }
    
    virtual void  FillSimpleContext(StackTrace_SimpleContext * dest, LPVOID srcCtx) const;
    virtual void  FillTargetContext(LPVOID destCtx, LPVOID srcCtx, int idx = 0) const;

    virtual LPCSTR GetDumpStackHeading() const          { return s_DumpStackHeading; }
    virtual LPCSTR GetDumpStackObjectsHeading() const   { return s_DSOHeading; }
    virtual LPCSTR GetSPName() const                    { return s_SPName; }
    virtual void GetGCRegisters(LPCSTR** regNames, unsigned int* cntRegs) const
    { _ASSERTE(cntRegs != NULL); *regNames = s_GCRegs; *cntRegs = _countof(s_GCRegs); }

    virtual void DumpGCInfo(GCInfoToken gcInfoToken, unsigned methodSize, printfFtn gcPrintf, bool encBytes, bool bPrintHeader) const;

private:
    ARMMachine()  {}
    ~ARMMachine() {}
    ARMMachine(const ARMMachine& machine);      // undefined
    ARMMachine & operator=(const ARMMachine&);  // undefined

private:
    static LPCSTR     s_DumpStackHeading;
    static LPCSTR     s_DSOHeading;
    static LPCSTR     s_GCRegs[14];
    static LPCSTR     s_SPName;
    static ARMMachine s_ARMMachineInstance;
}; // class ARMMachine

#endif // SOS_TARGET_ARM

#ifdef SOS_TARGET_AMD64

/// AMD64 Machine specific code
class AMD64Machine : public IMachine
{
public:
    typedef AMD64_CONTEXT TGT_CTXT;
    
    static IMachine* GetInstance()
    { static AMD64Machine s_AMD64MachineInstance; return &s_AMD64MachineInstance; }

    ULONG GetPlatform()             const { return IMAGE_FILE_MACHINE_AMD64; }
    ULONG GetContextSize()          const { return sizeof(AMD64_CONTEXT); }

    virtual void Unassembly(
                TADDR IPBegin, 
                TADDR IPEnd, 
                TADDR IPAskedFor, 
                TADDR GCStressCodeCopy, 
                GCEncodingInfo *pGCEncodingInfo, 
                SOSEHInfo *pEHInfo,
                BOOL bSuppressLines,
                BOOL bDisplayOffsets) const;

    virtual void IsReturnAddress(
                TADDR retAddr, 
                TADDR* whereCalled) const;

    virtual BOOL GetExceptionContext (
                TADDR stack, 
                TADDR PC, 
                TADDR *cxrAddr, 
                CROSS_PLATFORM_CONTEXT * cxr,
                TADDR *exrAddr, 
                PEXCEPTION_RECORD exr) const;

    // retrieve stack pointer, frame pointer, and instruction pointer from the target context
    virtual TADDR GetSP(const CROSS_PLATFORM_CONTEXT & ctx) const  { return ctx.Amd64Context.Rsp; }
    virtual TADDR GetBP(const CROSS_PLATFORM_CONTEXT & ctx) const  { return ctx.Amd64Context.Rbp; }
    virtual TADDR GetIP(const CROSS_PLATFORM_CONTEXT & ctx) const  { return ctx.Amd64Context.Rip; }
    
    virtual void  FillSimpleContext(StackTrace_SimpleContext * dest, LPVOID srcCtx) const;
    virtual void  FillTargetContext(LPVOID destCtx, LPVOID srcCtx, int idx = 0) const;

    virtual LPCSTR GetDumpStackHeading() const          { return s_DumpStackHeading; }
    virtual LPCSTR GetDumpStackObjectsHeading() const   { return s_DSOHeading; }
    virtual LPCSTR GetSPName() const                    { return s_SPName; }
    virtual void GetGCRegisters(LPCSTR** regNames, unsigned int* cntRegs) const
    { _ASSERTE(cntRegs != NULL); *regNames = s_GCRegs; *cntRegs = _countof(s_GCRegs); }

    virtual void DumpGCInfo(GCInfoToken gcInfoToken, unsigned methodSize, printfFtn gcPrintf, bool encBytes, bool bPrintHeader) const;

private:
    AMD64Machine()  {}
    ~AMD64Machine() {}
    AMD64Machine(const AMD64Machine& machine);      // undefined
    AMD64Machine & operator=(const AMD64Machine&);  // undefined

private:
    static LPCSTR       s_DumpStackHeading;
    static LPCSTR       s_DSOHeading;
    static LPCSTR       s_GCRegs[15];
    static LPCSTR       s_SPName;
}; // class AMD64Machine

#endif // SOS_TARGET_AMD64

#ifdef SOS_TARGET_ARM64

/// ARM64 Machine specific code
class ARM64Machine : public IMachine
{
public:
    typedef ARM64_CONTEXT TGT_CTXT;
    
    static IMachine* GetInstance()
    { static ARM64Machine s_ARM64MachineInstance; return &s_ARM64MachineInstance; }

    ULONG GetPlatform()             const { return IMAGE_FILE_MACHINE_ARM64; }
    ULONG GetContextSize()          const { return sizeof(ARM64_CONTEXT); }
    virtual void Unassembly(
                TADDR IPBegin, 
                TADDR IPEnd, 
                TADDR IPAskedFor, 
                TADDR GCStressCodeCopy, 
                GCEncodingInfo *pGCEncodingInfo, 
                SOSEHInfo *pEHInfo,
                BOOL bSuppressLines,
                BOOL bDisplayOffsets) const;
    virtual void IsReturnAddress(
                TADDR retAddr, 
                TADDR* whereCalled) const;
    virtual BOOL GetExceptionContext (
                TADDR stack, 
                TADDR PC, 
                TADDR *cxrAddr, 
                CROSS_PLATFORM_CONTEXT * cxr,
                TADDR *exrAddr, 
                PEXCEPTION_RECORD exr) const;

    // retrieve stack pointer, frame pointer, and instruction pointer from the target context
    virtual TADDR GetSP(const CROSS_PLATFORM_CONTEXT & ctx) const  { return ctx.Arm64Context.Sp; }
    virtual TADDR GetBP(const CROSS_PLATFORM_CONTEXT & ctx) const  { return ctx.Arm64Context.Fp; }
    virtual TADDR GetIP(const CROSS_PLATFORM_CONTEXT & ctx) const  { return ctx.Arm64Context.Pc; }
    
    virtual void  FillSimpleContext(StackTrace_SimpleContext * dest, LPVOID srcCtx) const;
    virtual void  FillTargetContext(LPVOID destCtx, LPVOID srcCtx, int idx = 0) const;
    
    virtual LPCSTR GetDumpStackHeading() const          { return s_DumpStackHeading; }
    virtual LPCSTR GetDumpStackObjectsHeading() const   { return s_DSOHeading; }
    virtual LPCSTR GetSPName() const                    { return s_SPName; }
    virtual void GetGCRegisters(LPCSTR** regNames, unsigned int* cntRegs) const
    { _ASSERTE(cntRegs != NULL); *regNames = s_GCRegs; *cntRegs = _countof(s_GCRegs);}

    virtual void DumpGCInfo(GCInfoToken gcInfoToken, unsigned methodSize, printfFtn gcPrintf, bool encBytes, bool bPrintHeader) const;

private:
    ARM64Machine()  {}
    ~ARM64Machine() {}
    ARM64Machine(const ARM64Machine& machine);      // undefined
    ARM64Machine & operator=(const ARM64Machine&);  // undefined

    static LPCSTR     s_DumpStackHeading;
    static LPCSTR     s_DSOHeading;
    static LPCSTR     s_GCRegs[28];
    static LPCSTR     s_SPName;

}; // class ARM64Machine

#endif // SOS_TARGET_ARM64
#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER


//
// Inline methods
//


#ifdef SOS_TARGET_X86
inline void X86Machine::FillSimpleContext(StackTrace_SimpleContext * dest, LPVOID srcCtx) const
{
    TGT_CTXT& src = *(TGT_CTXT*) srcCtx;
    dest->StackOffset = src.Esp;
    dest->FrameOffset = src.Ebp;
    dest->InstructionOffset = src.Eip;
}

inline void X86Machine::FillTargetContext(LPVOID destCtx, LPVOID srcCtx, int idx /*= 0*/) const
{
    TGT_CTXT* dest = (TGT_CTXT*)destCtx + idx;
    *dest = *(TGT_CTXT*)srcCtx;
}
#endif // SOS_TARGET_X86


#ifdef SOS_TARGET_ARM
inline void ARMMachine::FillSimpleContext(StackTrace_SimpleContext * dest, LPVOID srcCtx) const
{
    TGT_CTXT& src = *(TGT_CTXT*) srcCtx;
    dest->StackOffset = src.Sp;
    // @ARMTODO: frame pointer - keep in sync with ARMMachine::GetBP
    dest->FrameOffset = 0;
    dest->InstructionOffset = src.Pc;
}

inline void ARMMachine::FillTargetContext(LPVOID destCtx, LPVOID srcCtx, int idx /*= 0*/) const
{
    TGT_CTXT* dest = (TGT_CTXT*)destCtx + idx;
    *dest = *(TGT_CTXT*)srcCtx;
}
#endif // SOS_TARGET_ARM


#ifdef SOS_TARGET_AMD64
inline void AMD64Machine::FillSimpleContext(StackTrace_SimpleContext * dest, LPVOID srcCtx) const
{
    TGT_CTXT& src = *(TGT_CTXT*) srcCtx;
    dest->StackOffset = src.Rsp;
    dest->FrameOffset = src.Rbp;
    dest->InstructionOffset = src.Rip;
}

inline void AMD64Machine::FillTargetContext(LPVOID destCtx, LPVOID srcCtx, int idx /*= 0*/) const
{
    TGT_CTXT* dest = (TGT_CTXT*)destCtx + idx;
    *dest = *(TGT_CTXT*)srcCtx;
}
#endif // SOS_TARGET_AMD64

#ifdef SOS_TARGET_ARM64
inline void ARM64Machine::FillSimpleContext(StackTrace_SimpleContext * dest, LPVOID srcCtx) const
{
    TGT_CTXT& src = *(TGT_CTXT*) srcCtx;
    dest->StackOffset = src.Sp;
    dest->FrameOffset = src.Fp;
    dest->InstructionOffset = src.Pc;
}

inline void ARM64Machine::FillTargetContext(LPVOID destCtx, LPVOID srcCtx, int idx /*= 0*/) const
{
    TGT_CTXT* dest = (TGT_CTXT*)destCtx + idx;
    *dest = *(TGT_CTXT*)srcCtx;
}
#endif // SOS_TARGET_ARM64

#endif // __disasm_h__
