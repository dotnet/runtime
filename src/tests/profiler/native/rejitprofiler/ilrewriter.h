// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <assert.h>
#include <cstdio>
#include "cor.h"
#include "corprof.h"
#include "corhlpr.h"

#ifdef WIN32
#define FASTCALL __fastcall
#else // WIN32
#define FASTCALL
#endif // WIN32

// ILRewriter::Export intentionally does a comparison by casting a variable (delta) down
// to an INT8, with data loss being expected and handled. This pragma is required because
// this is compiled with RTC on, and without the pragma, the above cast will generate a
// run-time check on whether we lose data, and cause an unhandled exception (look up
// RTC_Check_4_to_1).  In theory, I should be able to just bracket the Export function
// with the #pragma, but that didn't work.  (Perhaps because all the functions are
// defined inline in the class definition?)
#if defined(WIN32) || defined(WIN64)
#pragma runtime_checks("", off)
#endif

struct COR_ILMETHOD_SECT_EH;
struct ILInstr
{
    ILInstr *       m_pNext;
    ILInstr *       m_pPrev;

    unsigned        m_opcode;
    unsigned        m_offset;

    union
    {
        ILInstr *   m_pTarget;
        INT8        m_Arg8;
        INT16       m_Arg16;
        INT32       m_Arg32;
        INT64       m_Arg64;
    };
};

struct EHClause
{
    CorExceptionFlag            m_Flags;
    ILInstr *                   m_pTryBegin;
    ILInstr *                   m_pTryEnd;
    ILInstr *                   m_pHandlerBegin;    // First instruction inside the handler
    ILInstr *                   m_pHandlerEnd;      // Last instruction inside the handler
    union
    {
        DWORD                   m_ClassToken;   // use for type-based exception handlers
        ILInstr *               m_pFilter;      // use for filter-based exception handlers (COR_ILEXCEPTION_CLAUSE_FILTER is set)
    };
};

typedef enum
{
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) c,
#include "opcode.def"
#undef OPDEF
    CEE_COUNT,
    CEE_SWITCH_ARG, // special internal instructions
} OPCODE;

#define dimensionof(a) 		(sizeof(a)/sizeof(*(a)))

#define OPCODEFLAGS_SizeMask        0x0F
#define OPCODEFLAGS_BranchTarget    0x10
#define OPCODEFLAGS_Switch          0x20

static const BYTE s_OpCodeFlags[] =
{
#define InlineNone           0
#define ShortInlineVar       1
#define InlineVar            2
#define ShortInlineI         1
#define InlineI              4
#define InlineI8             8
#define ShortInlineR         4
#define InlineR              8
#define ShortInlineBrTarget  1 | OPCODEFLAGS_BranchTarget
#define InlineBrTarget       4 | OPCODEFLAGS_BranchTarget
#define InlineMethod         4
#define InlineField          4
#define InlineType           4
#define InlineString         4
#define InlineSig            4
#define InlineRVA            4
#define InlineTok            4
#define InlineSwitch         0 | OPCODEFLAGS_Switch

#define OPDEF(c,s,pop,push,args,type,l,s1,s2,flow) args,
#include "opcode.def"
#undef OPDEF

#undef InlineNone
#undef ShortInlineVar
#undef InlineVar
#undef ShortInlineI
#undef InlineI
#undef InlineI8
#undef ShortInlineR
#undef InlineR
#undef ShortInlineBrTarget
#undef InlineBrTarget
#undef InlineMethod
#undef InlineField
#undef InlineType
#undef InlineString
#undef InlineSig
#undef InlineRVA
#undef InlineTok
#undef InlineSwitch
    0,                              // CEE_COUNT
    4 | OPCODEFLAGS_BranchTarget,   // CEE_SWITCH_ARG
};

static int k_rgnStackPushes[] = {

#if defined(WIN32) || defined(WIN64)
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) \
    { push },
#else
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) \
    push,
#endif

#define Push0    0
#define Push1    1
#define PushI    1
#define PushI4   1
#define PushR4   1
#define PushI8   1
#define PushR8   1
#define PushRef  1
#define VarPush  1          // Test code doesn't call vararg fcns, so this should not be used

#include "opcode.def"

#undef Push0
#undef Push1
#undef PushI
#undef PushI4
#undef PushR4
#undef PushI8
#undef PushR8
#undef PushRef
#undef VarPush
#undef OPDEF
    0,  // CEE_COUNT
    0   // CEE_SWITCH_ARG
};


class ILRewriter
{
private:
    ICorProfilerInfo * m_pICorProfilerInfo;
    ICorProfilerFunctionControl * m_pICorProfilerFunctionControl;

    //FunctionID  m_functionId;
    //ClassID     m_classId;
    ModuleID    m_moduleId;
    mdToken     m_tkMethod;

    mdToken     m_tkLocalVarSig;
    unsigned    m_maxStack;
    unsigned    m_flags;
    bool        m_fGenerateTinyHeader;

    ILInstr m_IL; // Double linked list of all il instructions

    unsigned    m_nEH;
    EHClause *  m_pEH;

    // Helper table for importing.  Sparse array that maps BYTE offset of beginning of an
    // instruction to that instruction's ILInstr*.  BYTE offsets that don't correspond
    // to the beginning of an instruction are mapped to NULL.
    ILInstr **  m_pOffsetToInstr;
    unsigned    m_CodeSize;

    unsigned    m_nInstrs;

    BYTE *      m_pOutputBuffer;

    IMethodMalloc * m_pIMethodMalloc;

    IMetaDataImport * m_pMetaDataImport;
    IMetaDataEmit * m_pMetaDataEmit;

public:
    ILRewriter(ICorProfilerInfo * pICorProfilerInfo, ICorProfilerFunctionControl * pICorProfilerFunctionControl, ModuleID moduleID, mdToken tkMethod);
    ~ILRewriter();
    HRESULT Initialize();
    void InitializeTiny();

    /////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // I M P O R T
    //
    ////////////////////////////////////////////////////////////////////////////////////////////////
    HRESULT Import();
    HRESULT ImportIL(LPCBYTE pIL);
    HRESULT ImportEH(const COR_ILMETHOD_SECT_EH* pILEH, unsigned nEH);
    ILInstr* NewILInstr();
    ILInstr* GetInstrFromOffset(unsigned offset);
    void InsertBefore(ILInstr * pWhere, ILInstr * pWhat);
    void InsertAfter(ILInstr * pWhere, ILInstr * pWhat);
    void AdjustState(ILInstr * pNewInstr);
    ILInstr * GetILList();

    /////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // E X P O R T
    //
    ////////////////////////////////////////////////////////////////////////////////////////////////
    HRESULT Export();
    HRESULT SetILFunctionBody(unsigned size, LPBYTE pBody);
    LPBYTE AllocateILMemory(unsigned size);
    void DeallocateILMemory(LPBYTE pBody);

    /////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // R E W R I T E
    //
    ////////////////////////////////////////////////////////////////////////////////////////////////
#ifdef WIN32
    // Probe_XXX are the callbacks to be called from the JITed code
    static void FASTCALL Probe_LDSFLD(WCHAR * pFieldName)
    {
        printf("LDSFLD: %S\n", pFieldName);
    }

    static void FASTCALL Probe_SDSFLD(WCHAR * pFieldName)
    {
        printf("STSFLD: %S\n", pFieldName);
    }
#endif // WIN32
    UINT AddNewInt32Local();
    WCHAR* GetNameFromToken(mdToken tk);
    ILInstr * NewLDC(LPVOID p);
};
