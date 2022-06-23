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

   setMachState is guarnenteed to return 0 (so the return
   statement will never be executed), but the expression above
   insures insures that there is a 'quick' path to epilog
   of the function.  This insures that setMachState will only
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
#pragma optimize("gsy", on )        // optimize to insure that code generation does not have junk in it
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
        // Does the recusive function begin with push XXXX call <helper>
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
                    PTR_BYTE target = tmpIp + (__int32)*((PTR_TADDR)(PTR_TO_TADDR(tmpIp) - 4));
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
                ip += (signed __int8) ip[1] + 2;
                break;

            case 0xE9:              // jmp <disp32>
                ip += (__int32)*PTR_DWORD(PTR_TO_TADDR(ip) + 1) + 5;
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
                    PTR_BYTE tmpIp = ip + (TADDR)(signed __int8) ip[1] + 2;
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


/***************************************************************/
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif

/***************************************************************/
// A fundamental requirement of managed code is that we need to be able to enumerate all GC references on the
// stack at GC time. To do this we need to be able to 'crawl' the stack. We know how to do this in JIT
// compiled code (it generates additional information like the frame size etc), but we don't know how to do
// this for unmanaged code. For PINVOKE calls, we leave a pointer to the transition boundary between managed
// and unmanaged code and we simply ignore the lower part of the stack. However setting up this transition is
// a bit expensive (1-2 dozen instructions), and while that is acceptable for PINVOKE, it is not acceptable
// for high volume calls, like NEW, CAST, WriterBarrier, Stack field fetch and others.
//
// To get around this, for transitions into the runtime (which we call FCALLS), we DEFER setting up the
// boundary variables (what we call the transition frame), until we actually need it (we will do an operation
// that might cause a GC). This allow us to handle the common case (where we might find the thing in a cache,
// or be service the 'new' from a allocation quantum), and only pay the cost of setting up the transition
// frame when it will actually be used.
//
// The problem is that in order to set up a transition frame we need to be able to find ALL REGISTERS AT THE
// TIME THE TRANSITION TO UNMANAGED CODE WAS MADE (because we might need to update them if they have GC
// references). Because we have executed ordinary C++ code (which might spill the registers to the stack at
// any time), we have a problem. LazyMachState is our 'solution' to this problem. We take advantage of the
// fact that the C++ code MUST RESTORE the register before returning. Thus we simulate the execution from the
// current location to the return and 'watch' where the registers got restored from. This is what
// unwindLazyState does (determine what the registers would be IF you had never executed and unmanaged C++
// code).
//
// By design, this code does not handle all X86 instructions, but only those instructions needed in an
// epilog.  If you get a failure because of a missing instruction, it MAY simply be because the compiler
// changed and now emits a new instruction in the epilog, but it MAY also be because the unwinder is
// 'confused' and is trying to follow a code path that is NOT AN EPILOG, and in this case adding
// instructions to 'fix' it is inappropriate.
//
void LazyMachState::unwindLazyState(LazyMachState* baseState,
                                    MachState* lazyState,
                                    DWORD threadId,
                                    int funCallDepth /* = 1 */,
                                    HostCallPreference hostCallPreference /* = (HostCallPreference)(-1) */)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    lazyState->_edi = baseState->_edi;
    lazyState->_esi = baseState->_esi;
    lazyState->_ebx = baseState->_ebx;
    lazyState->_ebp = baseState->captureEbp;
#ifndef DACCESS_COMPILE
    lazyState->_pEdi = &baseState->_edi;
    lazyState->_pEsi = &baseState->_esi;
    lazyState->_pEbx = &baseState->_ebx;
    lazyState->_pEbp = &baseState->_ebp;
#endif

    // We have captured the state of the registers as they exist in 'captureState'
    // we need to simulate execution from the return address captured in 'captureState
    // until we return from the caller of captureState.

    PTR_BYTE ip = PTR_BYTE(baseState->captureEip);
    PTR_TADDR ESP = PTR_TADDR(baseState->captureEsp);
    ESP++;                                 // pop captureState's return address


    // VC now has small helper calls that it uses in epilogs.  We need to walk into these
    // helpers if we are to decode the stack properly.  After we walk the helper we need
    // to return and continue walking the epiliog.  This varaible remembers were to return to
    PTR_BYTE epilogCallRet = PTR_BYTE((TADDR)0);

    // The very first conditional jump that we are going to encounter is
    // the one testing for the return value of LazyMachStateCaptureState.
    // The non-zero path is the one directly leading to a return statement.
    // This variable keeps track of whether we are still looking for that
    // first conditional jump.
    BOOL bFirstCondJmp = TRUE;

    // The general strategy is that we always try to plough forward:
    // we follow a conditional jump if and only if it is a forward jump.
    // However, in fcall functions that set up a HELPER_METHOD_FRAME in
    // more than one place, gcc will have both of them share the same
    // epilog - and the second one may actually be a backward jump.
    // This can lead us to loop in a destructor code loop.  To protect
    // against this, we remember the ip of the last conditional jump
    // we followed, and if we encounter it again, we take the other branch.
    PTR_BYTE lastCondJmpIp = PTR_BYTE((TADDR)0);

    int datasize; // helper variable for decoding of address modes
    int mod;      // helper variable for decoding of mod r/m
    int rm;       // helper variable for decoding of mod r/m

#ifdef _DEBUG
    int count = 0;
    const DWORD cInstructions = 1000;
    PTR_BYTE *instructionBytes = (PTR_BYTE*)alloca(cInstructions * sizeof(PTR_BYTE));
    memset(instructionBytes, 0, cInstructions * sizeof(PTR_BYTE));
#endif
    bool bset16bit=false;
    bool b16bit=false;
    for(;;)
    {
        _ASSERTE(count++ < 1000);       // we should never walk more than 1000 instructions!
        b16bit=bset16bit;
        bset16bit=false;

#ifndef DACCESS_COMPILE
    again:
#endif
#ifdef _DEBUG
        instructionBytes[count-1] = ip;
#endif
        switch(*ip)
        {

            case 0x64:              // FS: prefix
                bset16bit=b16bit;   // In case we have just seen a 0x66 prefix
                goto incIp1;

            case 0x66:
                bset16bit=true;     // Remember that we saw the 0x66 prefix [16-bit datasize override]
                goto incIp1;

            case 0x50:              // push EAX
            case 0x51:              // push ECX
            case 0x52:              // push EDX
            case 0x53:              // push EBX
            case 0x55:              // push EBP
            case 0x56:              // push ESI
            case 0x57:              // push EDI
            case 0x9C:              // pushfd
                --ESP;
            case 0x40:              // inc EAX
            case 0x41:              // inc ECX
            case 0x42:              // inc EDX
            case 0x43:              // inc EBX
            case 0x46:              // inc ESI
            case 0x47:              // inc EDI
                goto incIp1;

            case 0x58:              // pop EAX
            case 0x59:              // pop ECX
            case 0x5A:              // pop EDX
            case 0x9D:              // popfd
                ESP++;
                // FALL THROUGH

            case 0x90:              // nop
        incIp1:
                ip++;
                break;

            case 0x5B:              // pop EBX
                lazyState->_pEbx = ESP;
                lazyState->_ebx  = *ESP++;
                goto incIp1;
            case 0x5D:              // pop EBP
                lazyState->_pEbp = ESP;
                lazyState->_ebp  = *ESP++;
                goto incIp1;
            case 0x5E:              // pop ESI
                lazyState->_pEsi = ESP;
                lazyState->_esi = *ESP++;
                goto incIp1;
            case 0x5F:              // pop EDI
                lazyState->_pEdi = ESP;
                lazyState->_edi = *ESP++;
                goto incIp1;

            case 0xEB:              // jmp <disp8>
                ip += (signed __int8) ip[1] + 2;
                break;

            case 0x72:              // jb <disp8> for gcc.
                {
                    PTR_BYTE tmpIp = ip + (int)(signed __int8)ip[1] + 2;
                    if (tmpIp > ip)
                        ip = tmpIp;
                    else
                        ip += 2;
                }
                break;

            case 0xE8:              // call <disp32>
                ip += 5;
                if (epilogCallRet == 0)
                {
                    PTR_BYTE target = ip + (__int32)*PTR_DWORD(PTR_TO_TADDR(ip) - 4);    // calculate target

                    if (shouldEnterCall(target))
                    {
                        epilogCallRet = ip;             // remember our return address
                        --ESP;                          // simulate pushing the return address
                        ip = target;
                    }
                }
                break;

            case 0xE9:              // jmp <disp32>
                {
                    PTR_BYTE tmpIp = ip
                        + ((__int32)*dac_cast<PTR_DWORD>(ip + 1) + 5);
                    ip = tmpIp;
                }
                break;

            case 0x0f:              // follow non-zero jumps:
              if (ip[1] >= 0x90 && ip[1] <= 0x9f) {
                  if ((ip[2] & 0xC0) != 0xC0)  // set<cc> reg
                      goto badOpcode;
                  ip += 3;
                  break;
              }
              else if ((ip[1] & 0xf0) == 0x40) { //cmov mod/rm
                  ++ip;
                  datasize = 0;
                  goto decodeRM;
              }
              else if (ip[1] >= 0x10 && ip[1] <= 0x17) { // movups, movlps, movhps, unpcklpd, unpckhpd
                  ++ip;
                  datasize = 0;
                  goto decodeRM;
              }
              else if (ip[1] == 0x1f) {     // nop (multi-byte)
                  ++ip;
                  datasize = 0;
                  goto decodeRM;
              }
              else if (ip[1] == 0x57) {     // xorps
                  ++ip;
                  datasize = 0;
                  goto decodeRM;
              }
              else if (ip[1] == 0xb6 || ip[1] == 0xb7) {     //movzx reg, r/m8
                  ++ip;
                  datasize = 0;
                  goto decodeRM;
              }
              else if (ip[1] == 0xbf) {     //movsx reg, r/m16
                  ++ip;
                  datasize = 0;
                  goto decodeRM;
              }
              else if (ip[1] == 0xd6 || ip[1] == 0x7e) {     // movq
                  ++ip;
                  datasize = 0;
                  goto decodeRM;
              }
              else if (bFirstCondJmp) {
                  bFirstCondJmp = FALSE;
                  if (ip[1] == 0x85)  // jne <disp32>
                      ip += (__int32)*dac_cast<PTR_DWORD>(ip + 2) + 6;
                  else if (ip[1] >= 0x80 && ip[1] <= 0x8F)  // jcc <disp32>
                      ip += 6;
                  else
                      goto badOpcode;
              }
              else {
                  if ((ip[1] >= 0x80) && (ip[1] <= 0x8F)) {
                      PTR_BYTE tmpIp = ip + (__int32)*dac_cast<PTR_DWORD>(ip + 2) + 6;

                      if ((tmpIp > ip) == (lastCondJmpIp != ip)) {
                          lastCondJmpIp = ip;
                          ip = tmpIp;
                      }
                      else {
                          lastCondJmpIp = ip;
                          ip += 6;
                      }
                  }
                  else
                      goto badOpcode;
              }
              break;

              // This is here because VC seems to not always optimize
              // away a test for a literal constant
            case 0x6A:              // push 0xXX
                ip += 2;
                --ESP;
                break;

            case 0x68:              // push 0xXXXXXXXX
                if ((ip[5] == 0xFF) && (ip[6] == 0x15)) {
                    ip += 11; //
                }
                else {
                    ip += 5;

                    // For office profiler.  They morph calls into push TARGET; call helper
                    // so if you see
                    //
                    // push XXXX
                    // call xxxx
                    //
                    // and we notice that mscorwks has been instrumented and
                    // xxxx starts with a JMP [] then do what you would do for call XXXX
                    if ((*ip & 0xFE) == 0xE8 && callsInstrumented()) {       // It is a call or a jump (E8 or E9)
                        PTR_BYTE tmpIp = ip + 5;
                        PTR_BYTE target = tmpIp + (__int32)*PTR_DWORD(PTR_TO_TADDR(tmpIp) - 4);
                        if (target[0] == 0xFF && target[1] == 0x25) {                // jmp [xxxx] (to external dll)
                            target = PTR_BYTE(*PTR_TADDR(PTR_TO_TADDR(ip) - 4));
                            if (*ip == 0xE9) {                                       // Do logic for jmp
                                ip = target;
                            }
                            else if (shouldEnterCall(target)) {                      // Do logic for calls
                                epilogCallRet = ip;             // remember our return address
                                --ESP;                          // simulate pushing the return address
                                ip = target;
                            }
                        }
                    }
                }
                break;

           case 0x74:              // jz <target>
                if (bFirstCondJmp) {
                    bFirstCondJmp = FALSE;
                    ip += 2;            // follow the non-zero path
                    break;
                }
                goto condJumpDisp8;

            case 0x75:              // jnz <target>
                // Except the first jump, we always follow forward jump to avoid possible looping.
                //
                if (bFirstCondJmp) {
                    bFirstCondJmp = FALSE;
                    ip += (signed __int8) ip[1] + 2;   // follow the non-zero path
                    break;
                }
                goto condJumpDisp8;

            case 0x77:              // ja <target>
            case 0x78:              // js <target>
            case 0x79:              // jns <target>
            case 0x7d:              // jge <target>
            case 0x7c:              // jl <target>
                goto condJumpDisp8;

        condJumpDisp8:
                {
                    PTR_BYTE tmpIp = ip + (TADDR)(signed __int8) ip[1] + 2;
                    if ((tmpIp > ip) == (lastCondJmpIp != ip)) {
                        lastCondJmpIp = ip;
                        ip = tmpIp;
                    }
                    else {
                        lastCondJmpIp = ip;
                        ip += 2;
                    }
                }
                break;

            case 0x84:
            case 0x85:
                mod = (ip[1] & 0xC0) >> 6;
                if (mod != 3)           // test reg1, reg2
                    goto badOpcode;
                ip += 2;
                break;

            case 0x34:                            // XOR AL, imm8
                ip += 2;
                break;

            case 0x31:
            case 0x32:
            case 0x33:
#ifdef __GNUC__
                //there are lots of special workarounds for XOR for msvc.  For GnuC
                //just do the normal Mod/rm stuff.
                datasize = 0;
                goto decodeRM;
#else
                mod = (ip[1] & 0xC0) >> 6;
                if (mod == 3)
                {
                    // XOR reg1, reg2

                    // VC generates this sequence in some code:
                    // xor reg, reg
                    // test reg reg
                    // je   <target>
                    // This is just an unconditional branch, so jump to it
                    if ((ip[1] & 7) == ((ip[1] >> 3) & 7)) {        // reg1 == reg2?
                        if (ip[2] == 0x85 && ip[3] == ip[1]) {      // TEST reg, reg
                            if (ip[4] == 0x74) {
                                ip += (signed __int8) ip[5] + 6;   // follow the non-zero path
                                break;
                            }
                            _ASSERTE(ip[4] != 0x0f || ((ip[5] & 0xF0)!=0x80)); // If this goes off, we need the big jumps
                        }
                        else
                        {
                            if (ip[2]==0x74)
                            {
                                ip += (signed __int8) ip[3] + 4;
                                break;
                            }
                            _ASSERTE(ip[2] != 0x0f || ((ip[3] & 0xF0)!=0x80));              // If this goes off, we need the big jumps
                        }
                    }
                    ip += 2;
                }
                else if (mod == 1)
                {
                    // XOR reg1, [reg+offs8]
                    // Used by the /GS flag for call to __security_check_cookie()
                    // Should only be XOR ECX,[EBP+4]
                    _ASSERTE((((ip[1] >> 3) & 0x7) == 0x1) && ((ip[1] & 0x7) == 0x5) && (ip[2] == 4));
                    ip += 3;
                }
                else if (mod == 2)
                {
                    // XOR reg1, [reg+offs32]
                    // Should not happen but may occur with __security_check_cookie()
                    _ASSERTE(!"Unexpected XOR reg1, [reg+offs32]");
                    ip += 6;
                }
                else // (mod == 0)
                {
                    // XOR reg1, [reg]
                    goto badOpcode;
                }
                break;
#endif

            case 0x05:
                // added to handle gcc 3.3 generated code
                // add %reg, constant
                ip += 5;
                break;

            case 0xFF:
                if ( (ip[1] & 0x38) == 0x30)
                {
                    // opcode generated by Vulcan/BBT instrumentation
                    // search for push dword ptr[esp]; push imm32; call disp32 and if found ignore it
                    if ((ip[1] == 0x34) && (ip[2] == 0x24) && // push dword ptr[esp]  (length 3 bytes)
                        (ip[3] == 0x68) &&                    // push imm32           (length 5 bytes)
                        (ip[8] == 0xe8))                      // call disp32          (length 5 bytes)
                    {
                        // found the magic seq emitted by Vulcan instrumentation
                        ip += 13;  // (3+5+5)
                        break;
                    }

                    --ESP;      // push r/m
                    datasize = 0;
                    goto decodeRM;
                }
                else if ( (ip[1] & 0x38) == 0x10)
                {
                    // added to handle gcc 3.3 generated code
                    // This is a call *(%eax) generated by gcc for destructor calls.
                    // We can safely skip over the call
                    datasize = 0;
                    goto decodeRM;
                }
                else if (ip[1] == 0xe0)
                {
                    goto badOpcode;
#if 0
                    // Handles jmp *%eax from gcc
                    datasize = 0;
                    goto decodeRM;
#endif
                }
                else if (ip[1] == 0x25 && epilogInstrumented())        // is it jmp [XXXX]
                {
                    // this is a office profiler epilog (this jmp is acting as a return instruction)
                    PTR_BYTE epilogHelper = PTR_BYTE(*PTR_TADDR(*PTR_TADDR(PTR_TO_TADDR(ip) + 2)));

                    ip = PTR_BYTE(*ESP);
                    lazyState->_pRetAddr = ESP++;

                    if (epilogHelper[0] != 0x6A)             // push <number of dwords to pop>
                        goto badOpcode;
                    unsigned disp = *PTR_BYTE(PTR_TO_TADDR(epilogHelper) + 1) * 4;
                    ESP = PTR_TADDR(PTR_TO_TADDR(ESP) + disp);         // pop args
                    goto ret_with_epilogHelperCheck;

                }
                else
                {
                    goto badOpcode;
                }
                break;

            case 0x39:                       // comp r/m, reg
            case 0x3B:                       // comp reg, r/m
                datasize = 0;
                goto decodeRM;

            case 0xA1:                          // MOV EAX, [XXXX]
                ip += 5;
                break;

            case 0x89:                          // MOV r/m, reg
                if (ip[1] == 0xEC)              // MOV ESP, EBP
                    goto mov_esp_ebp;
                if (ip[1] == 0xDC)              // MOV ESP, EBX
                    goto mov_esp_ebx;
                // FALL THROUGH

            case 0x18:                          // SBB r/m8, r8
            case 0x19:                          // SBB r/m[16|32], r[16|32]
            case 0x1A:                          // SBB r8, r/m8
            case 0x1B:                          // SBB r[16|32], r/m[16|32]

            case 0x88:                          // MOV reg, r/m (BYTE)
            case 0x8A:                          // MOV r/m, reg (BYTE)

        move:
                datasize = 0;

        decodeRM:
                // Note that we don't want to read from ip[]
                // after we do ANY incrementing of ip

                mod = (ip[1] & 0xC0) >> 6;
                if (mod != 3) {
                    rm  = (ip[1] & 0x07);
                    if (mod == 0) {             // (mod == 0)
                        if      (rm == 5)       //   has disp32?
                            ip += 4;            //     [disp32]
                        else if (rm == 4)       //   has SIB byte?
                            ip += 1;            //     [reg*K+reg]
                    }
                    else if (mod == 1) {        // (mod == 1)
                        if (rm == 4)            //   has SIB byte?
                            ip += 1;            //     [reg*K+reg+disp8]
                        ip += 1;                //   for disp8
                    }
                    else {                      // (mod == 2)
                        if (rm == 4)            //   has SIB byte?
                            ip += 1;            //     [reg*K+reg+disp32]
                        ip += 4;                //   for disp32
                    }
                }
                ip += 2;                        // opcode and Mod R/M byte
                ip += datasize;
                break;

            case 0x80:                           // OP r/m8, <imm8>
                datasize = 1;
                goto decodeRM;

            case 0x81:                           // OP r/m32, <imm32>
                if (!b16bit && ip[1] == 0xC4) {  // ADD ESP, <imm32>
                    ESP = dac_cast<PTR_TADDR>(dac_cast<TADDR>(ESP) +
                          (__int32)*dac_cast<PTR_DWORD>(ip + 2));
                    ip += 6;
                    break;
                } else if (!b16bit && ip[1] == 0xC5) { // ADD EBP, <imm32>
                    lazyState->_ebp += (__int32)*dac_cast<PTR_DWORD>(ip + 2);
                    ip += 6;
                    break;
                }

                datasize = b16bit?2:4;
                goto decodeRM;

            case 0x24:                           // AND AL, imm8
                ip += 2;
                break;

            case 0x01:                           // ADD mod/rm
            case 0x03:
            case 0x11:                           // ADC mod/rm
            case 0x13:
            case 0x29:                           // SUB mod/rm
            case 0x2B:
                datasize = 0;
                goto decodeRM;
            case 0x83:                           // OP r/m32, <imm8>
                if (ip[1] == 0xC4)  {            // ADD ESP, <imm8>
                    ESP = dac_cast<PTR_TADDR>(dac_cast<TADDR>(ESP) + (signed __int8)ip[2]);
                    ip += 3;
                    break;
                }
                if (ip[1] == 0xec) {            // SUB ESP, <imm8>
                    ESP = PTR_TADDR(PTR_TO_TADDR(ESP) - (signed __int8)ip[2]);
                    ip += 3;
                    break;
                }
                if (ip[1] == 0xe4) {            // AND ESP, <imm8>
                    ESP = PTR_TADDR(PTR_TO_TADDR(ESP) & (signed __int8)ip[2]);
                    ip += 3;
                    break;
                }
                if (ip[1] == 0xc5) {            // ADD EBP, <imm8>
                    lazyState->_ebp += (signed __int8)ip[2];
                    ip += 3;
                    break;
                }

                datasize = 1;
                goto decodeRM;

            case 0x8B:                          // MOV reg, r/m
                if (ip[1] == 0xE5) {            // MOV ESP, EBP
                mov_esp_ebp:
                    ESP = PTR_TADDR(lazyState->_ebp);
                    ip += 2;
                    break;
                }

                if (ip[1] == 0xE3) {           // MOV ESP, EBX
                mov_esp_ebx:
                    ESP = PTR_TADDR(lazyState->_ebx);
                    ip += 2;
                    break;
                }

                if ((ip[1] & 0xc7) == 0x4 && ip[2] == 0x24) // move reg, [esp]
                {
                    if ( ip[1] == 0x1C ) {  // MOV EBX, [ESP]
                      lazyState->_pEbx = ESP;
                      lazyState->_ebx =  *lazyState->_pEbx;
                    }
                    else if ( ip[1] == 0x34 ) {  // MOV ESI, [ESP]
                      lazyState->_pEsi = ESP;
                      lazyState->_esi =  *lazyState->_pEsi;
                    }
                    else if ( ip[1] == 0x3C ) {  // MOV EDI, [ESP]
                      lazyState->_pEdi = ESP;
                      lazyState->_edi =   *lazyState->_pEdi;
                    }
                    else if ( ip[1] == 0x24 /*ESP*/ || ip[1] == 0x2C /*EBP*/)
                      goto badOpcode;

                    ip += 3;
                    break;
                }

                if ((ip[1] & 0xc7) == 0x44 && ip[2] == 0x24) // move reg, [esp+imm8]
                {
                    if ( ip[1] == 0x5C ) {  // MOV EBX, [ESP+XX]
                      lazyState->_pEbx = PTR_TADDR(PTR_TO_TADDR(ESP) + (signed __int8)ip[3]);
                      lazyState->_ebx =  *lazyState->_pEbx ;
                    }
                    else if ( ip[1] == 0x74 ) {  // MOV ESI, [ESP+XX]
                      lazyState->_pEsi = PTR_TADDR(PTR_TO_TADDR(ESP) + (signed __int8)ip[3]);
                      lazyState->_esi =  *lazyState->_pEsi;
                    }
                    else if ( ip[1] == 0x7C ) {  // MOV EDI, [ESP+XX]
                      lazyState->_pEdi = PTR_TADDR(PTR_TO_TADDR(ESP) + (signed __int8)ip[3]);
                      lazyState->_edi =   *lazyState->_pEdi;
                    }
                    else if ( ip[1] == 0x64 /*ESP*/ || ip[1] == 0x6C /*EBP*/)
                      goto badOpcode;

                    ip += 4;
                    break;
                }

                if ((ip[1] & 0xC7) == 0x45) {   // MOV reg, [EBP + imm8]
                    // gcc sometimes restores callee-preserved registers
                    // via 'mov reg, [ebp-xx]' instead of 'pop reg'
                    if ( ip[1] == 0x5D ) {  // MOV EBX, [EBP+XX]
                      lazyState->_pEbx = PTR_TADDR(lazyState->_ebp + (signed __int8)ip[2]);
                      lazyState->_ebx =  *lazyState->_pEbx ;
                    }
                    else if ( ip[1] == 0x75 ) {  // MOV ESI, [EBP+XX]
                      lazyState->_pEsi = PTR_TADDR(lazyState->_ebp + (signed __int8)ip[2]);
                      lazyState->_esi =  *lazyState->_pEsi;
                    }
                    else if ( ip[1] == 0x7D ) {  // MOV EDI, [EBP+XX]
                      lazyState->_pEdi = PTR_TADDR(lazyState->_ebp + (signed __int8)ip[2]);
                      lazyState->_edi =   *lazyState->_pEdi;
                    }
                    else if ( ip[1] == 0x65 /*ESP*/ || ip[1] == 0x6D /*EBP*/)
                      goto badOpcode;

                    // We don't track the values of EAX,ECX,EDX

                    ip += 3;   // MOV reg, [reg + imm8]
                    break;
                }

                if ((ip[1] & 0xC7) == 0x85) {   // MOV reg, [EBP+imm32]
                    // gcc sometimes restores callee-preserved registers
                    // via 'mov reg, [ebp-xx]' instead of 'pop reg'
                    if ( ip[1] == 0xDD ) {  // MOV EBX, [EBP+XXXXXXXX]
                      lazyState->_pEbx = PTR_TADDR(lazyState->_ebp + (__int32)*dac_cast<PTR_DWORD>(ip + 2));
                      lazyState->_ebx =  *lazyState->_pEbx ;
                    }
                    else if ( ip[1] == 0xF5 ) {  // MOV ESI, [EBP+XXXXXXXX]
                      lazyState->_pEsi = PTR_TADDR(lazyState->_ebp + (__int32)*dac_cast<PTR_DWORD>(ip + 2));
                      lazyState->_esi =  *lazyState->_pEsi;
                    }
                    else if ( ip[1] == 0xFD ) {  // MOV EDI, [EBP+XXXXXXXX]
                      lazyState->_pEdi = PTR_TADDR(lazyState->_ebp + (__int32)*dac_cast<PTR_DWORD>(ip + 2));
                      lazyState->_edi =   *lazyState->_pEdi;
                    }
                    else if ( ip[1] == 0xE5 /*ESP*/ || ip[1] == 0xED /*EBP*/)
                      goto badOpcode;  // Add more registers

                    // We don't track the values of EAX,ECX,EDX

                    ip += 6;   // MOV reg, [reg + imm32]
                    break;
                }
                goto move;

            case 0x8D:                          // LEA
                if ((ip[1] & 0x38) == 0x20) {                       // Don't allow ESP to be updated
                    if (ip[1] == 0xA5)          // LEA ESP, [EBP+XXXX]
                        ESP = PTR_TADDR(lazyState->_ebp + (__int32)*dac_cast<PTR_DWORD>(ip + 2));
                    else if (ip[1] == 0x65)     // LEA ESP, [EBP+XX]
                        ESP = PTR_TADDR(lazyState->_ebp + (signed __int8) ip[2]);
                    else if (ip[1] == 0x24 && ip[2] == 0x24)    // LEA ESP, [ESP]
                        ;
                    else if (ip[1] == 0xa4 && ip[2] == 0x24 && *((DWORD *)(&ip[3])) == 0) // Another form of: LEA ESP, [ESP]
                        ;
                    else if (ip[1] == 0x64 && ip[2] == 0x24 && ip[3] == 0) // Yet another form of: LEA ESP, [ESP] (8 bit offset)
                        ;
                    else
                    {
                        goto badOpcode;
                    }
                }

                datasize = 0;
                goto decodeRM;

            case 0xB0:  // MOV AL, imm8
                ip += 2;
                break;
            case 0xB8:  // MOV EAX, imm32
            case 0xB9:  // MOV ECX, imm32
            case 0xBA:  // MOV EDX, imm32
            case 0xBB:  // MOV EBX, imm32
            case 0xBE:  // MOV ESI, imm32
            case 0xBF:  // MOV EDI, imm32
                if(b16bit)
                    ip += 3;
                else
                    ip += 5;
                break;

            case 0xC2:                  // ret N
                {
                unsigned __int16 disp = *dac_cast<PTR_WORD>(ip + 1);
                ip = PTR_BYTE(*ESP);
                lazyState->_pRetAddr = ESP++;
                _ASSERTE(disp < 64);    // sanity check (although strictly speaking not impossible)
                ESP = dac_cast<PTR_TADDR>(dac_cast<TADDR>(ESP) + disp);         // pop args
                goto ret;
                }
            case 0xC3:                  // ret
                ip = PTR_BYTE(*ESP);
                lazyState->_pRetAddr = ESP++;

            ret_with_epilogHelperCheck:
                if (epilogCallRet != 0) {       // we are returning from a special epilog helper
                    ip = epilogCallRet;
                    epilogCallRet = 0;
                    break;                      // this does not count toward funCallDepth
                }
            ret:
                if (funCallDepth > 0)
                {
                    --funCallDepth;
                    if (funCallDepth == 0)
                        goto done;
                }
                else
                {
                    // Determine  whether given IP resides in JITted code. (It returns nonzero in that case.)
                    // Use it now to see if we've unwound to managed code yet.
                    BOOL fFailedReaderLock = FALSE;
                    BOOL fIsManagedCode = ExecutionManager::IsManagedCode(*lazyState->pRetAddr(), hostCallPreference, &fFailedReaderLock);
                    if (fFailedReaderLock)
                    {
                        // We don't know if we would have been able to find a JIT
                        // manager, because we couldn't enter the reader lock without
                        // yielding (and our caller doesn't want us to yield).  So abort
                        // now.

                        // Invalidate the lazyState we're returning, so the caller knows
                        // we aborted before we could fully unwind
                        lazyState->_pRetAddr = NULL;
                        return;
                    }

                    if (fIsManagedCode)
                        goto done;
                }

                bFirstCondJmp = TRUE;
                break;

            case 0xC6:                  // MOV r/m8, imm8
                datasize = 1;
                goto decodeRM;

            case 0xC7:                  // MOV r/m32, imm32
                datasize = b16bit?2:4;
                goto decodeRM;

            case 0xC9:                  // leave
                ESP = PTR_TADDR(lazyState->_ebp);
                lazyState->_pEbp = ESP;
                lazyState->_ebp = *ESP++;
                ip++;
                break;

#ifndef DACCESS_COMPILE
            case 0xCC:
                if (IsDebuggerPresent())
                {
                    OutputDebugStringA("CLR: Invalid breakpoint in a helpermethod frame epilog\n");
                    DebugBreak();
                    goto again;
                }
#ifndef _PREFIX_
                *((volatile int*) 0) = 1; // If you get at this error, it is because yout
                                        // set a breakpoint in a helpermethod frame epilog
                                        // you can't do that unfortunately.  Just move it
                                        // into the interior of the method to fix it
#endif // !_PREFIX_
                goto done;
#endif //!DACCESS_COMPILE

            case 0xD0:  //  shl REG16, 1
            case 0xD1:  //  shl REG32, 1
                    if (0xE4 == ip[1] || 0xE5 == ip[1]) // shl, ESP, 1 or shl EBP, 1
                    goto badOpcode;       // Doesn't look like valid code
                ip += 2;
                break;

            case 0xC1:  //  shl REG32, imm8
                    if (0xE4 == ip[1] || 0xE5 == ip[1]) // shl, ESP, imm8 or shl EBP, imm8
                    goto badOpcode;       // Doesn't look like valid code
                ip += 3;
                break;

            case 0xD9:  // single prefix
                if (0xEE == ip[1])
                {
                    ip += 2;            // FLDZ
                    break;
                }
                //
                // INTENTIONAL FALL THRU
                //
            case 0xDD:  // double prefix
                if ((ip[1] & 0xC0) != 0xC0)
                {
                    datasize = 0;       // floatop r/m
                    goto decodeRM;
                }
                else
                {
                    goto badOpcode;
                }
                break;

            case 0xf2: // repne prefix
            case 0xF3: // rep prefix
                ip += 1;
                break;

            case 0xA4:  // MOVS byte
            case 0xA5:  // MOVS word/dword
                ip += 1;
                break;

            case 0xA8: //test AL, imm8
                ip += 2;
                break;
            case 0xA9: //test EAX, imm32
                ip += 5;
                break;
            case 0xF6:
                if ( (ip[1] & 0x38) == 0x00) // TEST r/m8, imm8
                {
                    datasize = 1;
                    goto decodeRM;
                }
                else
                {
                    goto badOpcode;
                }
                break;

            case 0xF7:
                if ( (ip[1] & 0x38) == 0x00) // TEST r/m32, imm32
                {
                    datasize = b16bit?2:4;
                    goto decodeRM;
                }
                else if ((ip[1] & 0xC8)  == 0xC8) //neg reg
                {
                    ip += 2;
                    break;
                }
                else if ((ip[1] & 0x30) == 0x30) //div eax by mod/rm
                {
                    datasize = 0;
                    goto decodeRM;
                }
                else
                {
                    goto badOpcode;
                }
                break;

#ifdef __GNUC__
            case 0x2e:
                // Group 2 instruction prefix.
                if (ip[1] == 0x0f && ip[2] == 0x1f)
                {
                    // Although not the recommended multi-byte sequence for 9-byte
                    // nops (the suggestion is to use 0x66 as the prefix), this shows
                    // up in GCC-optimized code.
                    ip += 2;
                    datasize = 0;
                    goto decodeRM;
                }
                else
                {
                    goto badOpcode;
                }
                break;
#endif // __GNUC__

            default:
            badOpcode:
                _ASSERTE(!"Bad opcode");
                // FIX what to do here?
#ifndef DACCESS_COMPILE
#ifndef _PREFIX_
                *((volatile PTR_BYTE*) 0) = ip;  // cause an access violation (Free Build assert)
#endif // !_PREFIX_
#else
                DacNotImpl();
#endif
                goto done;
        }
    }
done:
    _ASSERTE(epilogCallRet == 0);

    // At this point the fields in 'frame' coorespond exactly to the register
    // state when the helper returns to its caller.
    lazyState->_esp = dac_cast<TADDR>(ESP);
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif
#else  // !USE_EXTERNAL_UNWINDER

void LazyMachState::unwindLazyState(LazyMachState* baseState,
                                    MachState* lazyState,
                                    DWORD threadId,
                                    int funCallDepth /* = 1 */,
                                    HostCallPreference hostCallPreference /* = (HostCallPreference)(-1) */)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    CONTEXT                         ctx;
    KNONVOLATILE_CONTEXT_POINTERS   nonVolRegPtrs;

    ctx.ContextFlags = 0; // Read by PAL_VirtualUnwind.

    ctx.Eip = baseState->captureEip;
    ctx.Esp = baseState->captureEsp;
    ctx.Ebp = baseState->captureEbp;

    ctx.Edi = lazyState->_edi = baseState->_edi;
    ctx.Esi = lazyState->_esi = baseState->_esi;
    ctx.Ebx = lazyState->_ebx = baseState->_ebx;

    nonVolRegPtrs.Edi = &(baseState->_edi);
    nonVolRegPtrs.Esi = &(baseState->_esi);
    nonVolRegPtrs.Ebx = &(baseState->_ebx);
    nonVolRegPtrs.Ebp = &(baseState->_ebp);

    PCODE pvControlPc;

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    LazyMachState::unwindLazyState(ip:%p,bp:%p,sp:%p)\n", baseState->captureEip, baseState->captureEbp, baseState->captureEsp));

    do
    {
#ifdef DACCESS_COMPILE
        HRESULT hr = DacVirtualUnwind(threadId, &ctx, &nonVolRegPtrs);
        if (FAILED(hr))
        {
            DacError(hr);
        }
#else
        BOOL success = PAL_VirtualUnwind(&ctx, &nonVolRegPtrs);
        if (!success)
        {
            _ASSERTE(!"unwindLazyState: Unwinding failed");
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }
#endif // DACCESS_COMPILE

        pvControlPc = GetIP(&ctx);

        _ASSERTE(pvControlPc != 0);

        if (funCallDepth > 0)
        {
            --funCallDepth;
            if (funCallDepth == 0)
                break;
        }
        else
        {
            // Determine  whether given IP resides in JITted code. (It returns nonzero in that case.)
            // Use it now to see if we've unwound to managed code yet.
            BOOL fFailedReaderLock = FALSE;
            BOOL fIsManagedCode = ExecutionManager::IsManagedCode(pvControlPc, hostCallPreference, &fFailedReaderLock);
            if (fFailedReaderLock)
            {
                // We don't know if we would have been able to find a JIT
                // manager, because we couldn't enter the reader lock without
                // yielding (and our caller doesn't want us to yield).  So abort
                // now.

                // Invalidate the lazyState we're returning, so the caller knows
                // we aborted before we could fully unwind
                lazyState->_pRetAddr = NULL;
                return;
            }

            if (fIsManagedCode)
                break;
        }
    }
    while(TRUE);

    lazyState->_esp = ctx.Esp;
    lazyState->_pRetAddr = PTR_TADDR(lazyState->_esp - 4);

    lazyState->_edi = ctx.Edi;
    lazyState->_esi = ctx.Esi;
    lazyState->_ebx = ctx.Ebx;
    lazyState->_ebp = ctx.Ebp;

#ifdef DACCESS_COMPILE
    lazyState->_pEdi = NULL;
    lazyState->_pEsi = NULL;
    lazyState->_pEbx = NULL;
    lazyState->_pEbp = NULL;
#else  // DACCESS_COMPILE
    lazyState->_pEdi = nonVolRegPtrs.Edi;
    lazyState->_pEsi = nonVolRegPtrs.Esi;
    lazyState->_pEbx = nonVolRegPtrs.Ebx;
    lazyState->_pEbp = nonVolRegPtrs.Ebp;
#endif // DACCESS_COMPILE
}
#endif // !USE_EXTERNAL_UNWINDER
