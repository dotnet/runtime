// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: walker.h
//

//
// Debugger code stream analysis routines
//
//*****************************************************************************

#ifndef WALKER_H_
#define WALKER_H_


/* ========================================================================= */

/* ------------------------------------------------------------------------- *
 * Constants
 * ------------------------------------------------------------------------- */

enum WALK_TYPE
{
  WALK_NEXT,
  WALK_BRANCH,
  WALK_COND_BRANCH,
  WALK_CALL,
  WALK_RETURN,
  WALK_BREAK,
  WALK_THROW,
  WALK_META,
  WALK_UNKNOWN
};

// struct holding information for the instruction being skipped over
struct InstructionAttribute
{
    bool m_fIsCall;         // is this a call instruction?
    bool m_fIsCond;         // is this a conditional jump?
    bool m_fIsAbsBranch;    // is this an absolute branch (either a call or a jump)?
    bool m_fIsRelBranch;    // is this a relative branch (either a call or a jump)?
    bool m_fIsWrite;        // does the instruction write to an address?


    DWORD m_cbInstr;        // the size of the instruction
    DWORD m_cbDisp;         // the size of the displacement
    DWORD m_dwOffsetToDisp; // the offset from the beginning of the instruction
                            // to the beginning of the displacement
    BYTE m_cOperandSize;    // the size of the operand

    void Reset()
    {
        m_fIsCall = false;
        m_fIsCond = false;
        m_fIsAbsBranch = false;
        m_fIsRelBranch = false;
        m_fIsWrite = false;
        m_cbInstr = 0;
        m_cbDisp  = 0;
        m_dwOffsetToDisp = 0;
        m_cOperandSize = 0;
    }
};

/* ------------------------------------------------------------------------- *
 * Classes
 * ------------------------------------------------------------------------- */

class Walker
{
protected:
    Walker()
      : m_type(WALK_UNKNOWN), m_registers(NULL), m_ip(0), m_skipIP(0), m_nextIP(0), m_isAbsoluteBranch(false)
      {LIMITED_METHOD_CONTRACT; }

public:

    virtual void Init(const BYTE *ip, REGDISPLAY *pregisters)
    {
        PREFIX_ASSUME(pregisters != NULL);
        _ASSERTE(GetControlPC(pregisters) == (PCODE)ip);

        m_registers = pregisters;
        SetIP(ip);
    }

    const BYTE *GetIP()
      { return m_ip; }

    WALK_TYPE GetOpcodeWalkType()
      { return m_type; }

    const BYTE *GetSkipIP()
      { return m_skipIP; }

    bool IsAbsoluteBranch()
      { return m_isAbsoluteBranch; }

    const BYTE *GetNextIP()
      { return m_nextIP; }

    // We don't currently keep the registers up to date
    // <TODO> Check if it really works on IA64. </TODO>
    virtual void Next() { m_registers = NULL; SetIP(m_nextIP); }
    virtual void Skip() { m_registers = NULL; LOG((LF_CORDB, LL_INFO10000, "skipping over to %p \n", m_skipIP)); SetIP(m_skipIP); }

    // Decode the instruction
    virtual void Decode() = 0;

private:
    void SetIP(const BYTE *ip)
      { m_ip = ip; Decode(); }

protected:
    WALK_TYPE           m_type;             // Type of instructions
    REGDISPLAY         *m_registers;        // Registers
    const BYTE         *m_ip;               // Current IP
    const BYTE         *m_skipIP;           // IP if we skip the instruction
    const BYTE         *m_nextIP;           // IP if the instruction is taken
    bool                m_isAbsoluteBranch; // Is it an obsolute branch or not
};

#ifdef TARGET_X86

class NativeWalker : public Walker
{
public:
    void Init(const BYTE *ip, REGDISPLAY *pregisters)
    {
        m_opcode = 0;
        Walker::Init(ip, pregisters);
    }

    DWORD GetOpcode()
      { return m_opcode; }
/*
    void SetRegDisplay(REGDISPLAY *registers)
      { m_registers = registers; }
*/
    REGDISPLAY *GetRegDisplay()
      { return m_registers; }

    void Decode();
    void DecodeModRM(BYTE mod, BYTE reg, BYTE rm, const BYTE *ip);
    static void DecodeInstructionForPatchSkip(const BYTE *address, InstructionAttribute * pInstrAttrib);

private:
    DWORD GetRegisterValue(int registerNumber);

    DWORD m_opcode;           // Current instruction or opcode
};

#elif defined (TARGET_ARM)

class NativeWalker : public Walker
{
public:
    void Init(const BYTE *ip, REGDISPLAY *pregisters)
    {
        Walker::Init(ip, pregisters);
    }

    void Decode();

private:
    bool ConditionHolds(DWORD cond);
    DWORD GetReg(DWORD reg);
};

#elif defined(TARGET_AMD64)

class NativeWalker : public Walker
{
public:
    void Init(const BYTE *ip, REGDISPLAY *pregisters)
    {
        m_opcode = 0;
        Walker::Init(ip, pregisters);
    }

    DWORD GetOpcode()
      { return m_opcode; }
/*
    void SetRegDisplay(REGDISPLAY *registers)
      { m_registers = registers; }
*/
    REGDISPLAY *GetRegDisplay()
      { return m_registers; }

    void Decode();
    void DecodeModRM(BYTE mod, BYTE reg, BYTE rm, const BYTE *ip);
    static void DecodeInstructionForPatchSkip(const BYTE *address, InstructionAttribute * pInstrAttrib);

private:
    UINT64 GetRegisterValue(int registerNumber);

    DWORD m_opcode;           // Current instruction or opcode
};
#elif defined (TARGET_ARM64)
#include "controller.h"
class NativeWalker : public Walker
{
public:
    void Init(const BYTE *ip, REGDISPLAY *pregisters)
    {
        Walker::Init(ip, pregisters);
    }
    void Decode();
    static void DecodeInstructionForPatchSkip(const BYTE *address, InstructionAttribute * pInstrAttrib)
    {
        pInstrAttrib->Reset();
    }
    static BOOL  DecodePCRelativeBranchInst(PT_CONTEXT context,const PRD_TYPE& opcode, PCODE& offset, WALK_TYPE& walk);
    static BOOL  DecodeCallInst(const PRD_TYPE& opcode, int& RegNum, WALK_TYPE& walk);
    static BYTE* SetupOrSimulateInstructionForPatchSkip(T_CONTEXT * context, SharedPatchBypassBuffer * m_pSharedPatchBypassBuffer, const BYTE *address, PRD_TYPE opcode);

};
#elif defined (TARGET_LOONGARCH64)
#include "controller.h"
class NativeWalker : public Walker
{
public:
    void Init(const BYTE *ip, REGDISPLAY *pregisters)
    {
        Walker::Init(ip, pregisters);
    }
    void Decode();
    static void DecodeInstructionForPatchSkip(const BYTE *address, InstructionAttribute * pInstrAttrib)
    {
        pInstrAttrib->Reset();
    }
    static BOOL  DecodePCRelativeBranchInst(PT_CONTEXT context,const PRD_TYPE opcode, PCODE& offset, WALK_TYPE& walk);
    static BOOL  DecodeJumpInst(const PRD_TYPE opcode, int& RegNum, PCODE& offset, WALK_TYPE& walk);
};
#else
PORTABILITY_WARNING("NativeWalker not implemented on this platform");
class NativeWalker : public Walker
{
public:
    void Init(const BYTE *ip, REGDISPLAY *pregisters)
    {
        m_opcode = 0;
        Walker::Init(ip, pregisters);
    }
    DWORD GetOpcode()
      { return m_opcode; }
    void Next()
      { Walker::Next(); }
    void Skip()
      { Walker::Skip(); }

    void Decode()
    {
    PORTABILITY_ASSERT("NativeWalker not implemented on this platform");
        m_type = WALK_UNKNOWN;
        m_skipIP = m_ip++;
        m_nextIP = m_ip++;
    }

    static void DecodeInstructionForPatchSkip(const BYTE *address, InstructionAttribute * pInstrAttrib)
    {
    PORTABILITY_ASSERT("NativeWalker not implemented on this platform");

    }

private:
    DWORD m_opcode;           // Current instruction or opcode
};
#endif

#endif // WALKER_H_
