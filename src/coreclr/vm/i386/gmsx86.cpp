// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**************************************************************/
/*                       gmsx86.cpp                           */
/**************************************************************/

#include "common.h"
#include "gmscpu.h"

#ifdef TARGET_UNIX
#define USE_EXTERNAL_UNWINDER
#endif

#ifndef USE_EXTERNAL_UNWINDER
/***************************************************************/
/* setMachState figures out what the state of the CPU will be
   when the function that calls 'setMachState' returns.  It stores
   this information in 'frame'

   setMachState works by simulating the execution of the
   instructions starting at the instruction following the
   call to 'setMachState' and continuing until a return instruction
   is simulated.  To avoid having to process arbitrary code, the
   call to 'setMachState' should be called as follows

      if (machState.setMachState != 0) return;

   setMachState is guaranteed to return 0 (so the return
   statement will never be executed), but the expression above
   ensures that there is a 'quick' path to epilog
   of the function.  This ensures that setMachState will only
   have to parse a limited number of X86 instructions.   */


/***************************************************************/
#ifndef POISONC
#define POISONC ((sizeof(int *) == 4)?0xCCCCCCCCU:UI64(0xCCCCCCCCCCCCCCCC))
#endif

/***************************************************************/
/* the 'zeroFtn and 'recursiveFtn' are only here to determine
   if if mscorwks itself has been instrumented by a profiler
   that intercepts calls or epilogs of functions. (the
   callsInstrumented and epilogInstrumented functions).  */

#if !defined(DACCESS_COMPILE)

#ifdef _MSC_VER
#pragma optimize("gsy", on )        // optimize to ensure that code generation does not have junk in it
#endif // _MSC_VER
#pragma warning(disable:4717)

static int __stdcall zeroFtn() {
    return 0;
}

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winfinite-recursion"
#endif

static int __stdcall recursiveFtn() {
    return recursiveFtn()+1;
}

#ifdef __clang__
#pragma clang diagnostic pop
#endif

#ifdef _MSC_VER
#pragma optimize("", on )
#endif // _MSC_VER


/* Has mscorwks been instrumented so that calls are morphed into push XXXX call <helper> */
static bool callsInstrumented() {
        // Does the recursive function begin with push XXXX call <helper>
    PTR_BYTE ptr = PTR_BYTE(recursiveFtn);

    return (ptr[0] == 0x68 && ptr[5] == 0xe8);    // PUSH XXXX, call <helper>
}

/* Has mscorwks been instrumented so function prolog and epilogs are replaced with
   jmp [XXXX] */

static bool epilogInstrumented() {

    PTR_BYTE ptr = PTR_BYTE(zeroFtn);
    if (ptr[0] == 0xe8)                            // call <helper>     (prolog instrumentation)
        ptr += 5;
    if (ptr[0] == 0x33 && ptr[1] == 0xc0)        // xor eax eax
        ptr += 2;
    return (ptr[0] == 0xeb || ptr[0] == 0xe9);        // jmp <XXXX>
}

#else

    // Note that we have the callsInstrumeted and epilogInstrumented
    // functions so that the looser heuristics used for instrumented code
    // can't foul up an instrumented mscorwks.  For simplicity sake we
    // don't bother with this in the DAC, which means that the DAC could
    // be misled more frequently than mscorwks itself, but I still think
    // it will not be misled in any real scenario
static bool callsInstrumented() { LIMITED_METHOD_DAC_CONTRACT; return true; }
static bool epilogInstrumented() { LIMITED_METHOD_DAC_CONTRACT; return true; }

#endif // !defined(DACCESS_COMPILE)

/***************************************************************/
/* returns true if a call to 'ip' should be entered by the
   epilog walker.  Bascically we are looking for things that look
   like __SEH_epilog.  In particular we look for things that
   pops a register before doing a push.  If we see something
   that we don't recognise, we dont consider it a epilog helper
   and return false.
*/

static bool shouldEnterCall(PTR_BYTE ip) {
    SUPPORTS_DAC;

    int datasize; // helper variable for decoding of address modes
    int mod;      // helper variable for decoding of mod r/m
    int rm;       // helper variable for decoding of mod r/m

    int pushes = 0;

    // we should start unbalanced pops within 48 instrs. If not, it is not a special epilog function
    // the only reason we need as many instructions as we have below is because  coreclr
    // gets instrumented for profiling, code coverage, BBT etc, and we want these things to
    // just work.
    for (int i = 0; i < 48; i++) {
        switch(*ip) {
            case 0xF2:              // repne
            case 0xF3:              // repe
            case 0x90:              // nop
                ip++;
                break;

            case 0x68:              // push 0xXXXXXXXX
                ip += 5;

                // For office profiler.  They morph tail calls into push TARGET; jmp helper
                // so if you see
                //
                // push XXXX
                // jmp xxxx
                //
                // and we notice that coreclr has been instrumented and
                // xxxx starts with a JMP [] then do what you would do for jmp XXXX
                if (*ip == 0xE9 && callsInstrumented()) {        // jmp helper
                    PTR_BYTE tmpIp = ip + 5;
                    PTR_BYTE target = tmpIp + (int32_t)*((PTR_TADDR)(PTR_TO_TADDR(tmpIp) - 4));
                    if (target[0] == 0xFF && target[1] == 0x25) {                // jmp [xxxx] (to external dll)
                        ip = PTR_BYTE(*((PTR_TADDR)(PTR_TO_TADDR(ip) - 4)));
                    }
                }
                else {
                pushes++;
                }
                break;

            case 0x50:              // push EAX
            case 0x51:              // push ECX
            case 0x52:              // push EDX
            case 0x53:              // push EBX
            case 0x55:              // push EBP
            case 0x56:              // push ESI
            case 0x57:              // push EDI
                pushes++;
                ip++;
                break;

            case 0xE8:              // call <disp32>
                ip += 5;
                pushes = 0;         // This assumes that all of the previous pushes are arguments to this call
                break;

            case 0xFF:
                if (ip[1] != 0x15)  // call [XXXX] is OK (prolog of epilog helper is intrumented)
                    return false;   // but everything else is not OK.
                ip += 6;
                pushes = 0;         // This assumes that all of the previous pushes are arguments to this call
                break;

            case 0x9C:              // pushfd
            case 0x9D:              // popfd
                // a pushfd can never be an argument, so we model a pair of
                // these instruction as not changing the stack so that a call
                // that occurs between them does not consume the value of pushfd
                ip++;
                break;

            case 0x5D:              // pop EBP
            case 0x5E:              // pop ESI
            case 0x5F:              // pop EDI
            case 0x5B:              // pop EBX
            case 0x58:              // pop EAX
            case 0x59:              // pop ECX
            case 0x5A:              // pop EDX
                if (pushes <= 0) {
                    // We now have more pops than pushes.  This is our indication
                    // that we are in an EH_epilog function so we return true.
                    // This is the only way to exit this method with a retval of true.
                    return true;
                }
                --pushes;
                ip++;
                break;

            case 0xA1:              // MOV EAX, [XXXX]
                ip += 5;
                break;

            case 0xC6:              // MOV r/m8, imm8
                datasize = 1;
                goto decodeRM;

            case 0x89:              // MOV r/m, reg
                if (ip[1] == 0xE5)  // MOV EBP, ESP
                    return false;
                if (ip[1] == 0xEC)  // MOV ESP, EBP
                    return false;
                goto move;

            case 0x8B:              // MOV reg, r/m
                if (ip[1] == 0xE5)  // MOV ESP, EBP
                    return false;
                if (ip[1] == 0xEC)  // MOV EBP, ESP
                    return false;
                goto move;

            case 0x88:              // MOV reg, r/m (BYTE)
            case 0x8A:              // MOV r/m, reg (BYTE)

            case 0x31:              // XOR
            case 0x32:              // XOR
            case 0x33:              // XOR

        move:
                datasize = 0;

        decodeRM:
                // Note that we don't want to read from ip[] after
                // we do ANY incrementing of ip

                mod = (ip[1] & 0xC0) >> 6;
                if (mod != 3) {
                    rm  = (ip[1] & 0x07);
                    if (mod == 0) {         // (mod == 0)
                        if      (rm == 5)
                            ip += 4;            // disp32
                        else if (rm == 4)
                            ip += 1;            // [reg*K+reg]
                                                // otherwise [reg]

                    }
                    else if (mod == 1) {    // (mod == 1)
                        ip += 1;                // for disp8
                        if (rm == 4)
                            ip += 1;            // [reg*K+reg+disp8]
                                                // otherwise [reg+disp8]
                    }
                    else {                  // (mod == 2)
                        ip += 4;                // for disp32
                        if (rm == 4)
                            ip += 1;            // [reg*K+reg+disp32]
                                                // otherwise [reg+disp32]
                    }
                }

                ip += 2;
                ip += datasize;
                break;

            case 0x64:              // FS: prefix
                ip++;
                break;

            case 0xEB:              // jmp <disp8>
                ip += (int8_t) ip[1] + 2;
                break;

            case 0xE9:              // jmp <disp32>
                ip += (int32_t)*PTR_DWORD(PTR_TO_TADDR(ip) + 1) + 5;
                break;

            case 0xF7:               // test r/m32, imm32
                // Magellan code coverage build
                if ( (ip[1] & 0x38) == 0x00)
                {
                    datasize = 4;
                    goto decodeRM;
                }
                else
                {
                    return false;
                }
                break;

            case 0x75:              // jnz <target>
                // Magellan code coverage build
                // We always follow forward jump to avoid possible looping.
                {
                    PTR_BYTE tmpIp = ip + (TADDR)(int8_t) ip[1] + 2;
                    if (tmpIp > ip) {
                        ip = tmpIp;     // follow forwards jump
                    }
                    else {
                        return false;   // backwards jump implies not EH_epilog function
                    }
                }
                break;

            case 0xC2:                // ret
            case 0xC3:                // ret n
            default:
                return false;
        }
    }

    return false;
}

#endif // !USE_EXTERNAL_UNWINDER
