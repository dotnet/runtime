# Exception Handling Write Through Optimization.

Write through is an optimization done on local variables that live across
exception handling flow like a handler, filter, or finally so that they can be
enregistered - treated as a register candidate - throughout a method.  For each
variable live across one of these constructs, the minimum requirement is that a
store to the variables location on the stack is placed between a reaching
definition and any point of control flow leading to the handler, as well as a
load between any return from a filter or finally and an upward exposed use.
Conceptually this maintains the value of the variable on the stack across the
exceptional flow which would kill any live registers.  This transformation splits
a local variable into an enregisterable compiler temporary backed by
the local variable on the stack. For local vars that additionally have
appearances within an eh construct, a load from the stack local is inserted to
a temp that will be enregistered within the handler.

## Motivation

Historically the JIT has not done this transformation because exception
handling was rare and thus the transformation was not worth the compile time.
Additionally it was easy to make the recomendation to users to remove EH from
performance critical methods since they had control of where the EH appeared.
Neither of these points remain true as we increase our focus on cloud
workloads.  The use of non-blocking async calls are common in performance
critical paths for these workloads and async injects exception handling
constructs to implement the feature.  This in combination with the long
standing use of EH in 'foreach' and 'using' statements means that we are seeing
EH constructs that are difficult for the user to manage or remove high in the
profile (Techempower on Kestrel is a good example).  It's also good to consider
that in MSIL, basic operations can raise semantically meaningful exceptions
(unlike say C++, where an explicit throw is required to raise an exception) so
injected handlers can end up pessimizing a number of local variables in the
method. Given this combination of issues in cloud workloads doing the
transformation should be a clear benefit.

## Design

The goal of the design is to preserve the constraints listed above - i.e.
preserve a correct value on the stack for any local var that crosses an EH edge
in the flow graph. To ensure that the broad set of global optimizations can act
on the IR shape produced by this transformation and that phase ordering issues
do not block enregistration opportunities the write through phase will be
staged just prior to SSA build after morph and it will do a full walk of the
IR rewriting appearances to proxies as well as inserting reloads at the
appropriate blocks in the flow graph as indicated by EH control flow semantics.
To preserve the needed values on the stack a store will also be inserted after
every definition to copy the new value in the proxy back to the stack location.
This will leave non optimal number of stores (too many) but with the strategy
that the more expensive analysis to eliminate/better place stores will be
staged as a global optimization in a higher compilation tier.

There are a number of wrinkles informing this design based on how the JIT models EH:
- The jit does not explicitly model the exception flow, so a given block and
  even a given statement within a block may have multiple exception-raising sites.
- For statements within protected regions, and for all variables live into any
  reachable handler, the jit assumes all definitions within the region can
  potentially reach uses in the handlers, since the exact interleaving of
  definition points and exception points is not known. Hence every definition
  is a reaching definition, even both values back from to back stores with no
  read of the variable in between.
- The jit does not model which handlers are reachable from a given protected region,
  so considers a variable live into a handler if it is live into any handler in the method.

It is possible to do better than the "store every definition" approach outlined
in the design, but the expectation is that this would require possibly
modifying the model in the JIT and staging more throughput intensive analyses.
With these considerations this design was selected and further improvements
left to future optimization.

### Throughput

To identify EH crossing local vars global liveness is necessary.  This comes at
the significant cost of the liveness analysis.  To mitigate this the write
through phase is staged immediately before SSA build for the global optimizer.
Since the typical case is that there is no EH, the liveness analysis in write
through can be reused directly by SSA build.  For the case where EH local vars
are present liveness today must be rebuilt for SSA since new local vars have
been added, but incremental update to the RyuJIT liveness analysis can be
implemented (worklist based live analysis) to improve the throughput.
Additionally the write through transformation does a full IR walk - also
expensive - to replace EH local var appearances with proxies and insert
transfers to and from the stack for EH flow, given this initial implementations
may need to be staged as part of AOT (crossgen) compiles until tiering can move
the more expensive analysis out of the startup path.

### Algorithm

On the IR directly before SSA build:
- Run global liveness to identify local vars that cross EH boundaries (as a
  byproduct of this these local vars are marked "do not enregister")
- Foreach EH local var create a new local var "proxy" that can be enregistered.
- Iterate each block in the flow graph doing the following:
  * Foreach tree in block do a post order traversal and
    - Replace all appearances of EH local vars with the defined proxy
    - Insert a copy of proxy definition back to the EH local var (on the stack)
  * If EH handler entry block insert reloads from EH local var to proxy at
    block head
  * If finally or filter exit, insert reloads from EH local var to proxy at
    successor block heads
- For method entry block, insert reloads from parameter EH local vars to
  proxies

At end no proxy should be live across EH flow and all value updates will be
written back to the stack location.

### Alternate Algorithm: In LSRA

* Add a flag to identify Intervals as "WriteThru". This would be set on all lclVars
  considered by liveness to be exceptVars.
* Additionally, add a flag to identify RefPositions as "WriteThru". The motivation
  for having both, is that in the exception var case, we want to create all defs as
  write-thru, but for other purposes we may want to make some defs write-thru
  (i.e. they spill but the target register remains live), but not all defs for a given lclVar.
* During liveness, mark exception vars as `lvLiveInOutOfHndlr`, but not `lvDoNotEnregister`.
* During interval creation, if a lclVar is marked `lvLiveInOutOfHndlr`, set `isWriteThru` on the interval.
* Set handler entry blocks as having no predecessor for register-mapping purposes.
  - Leave the inVarToRegMaps empty (all incoming vars on stack)
* Set the outVarToRegMap to empty for EH exit blocks.
* During allocation, treat isWriteThru interval defs and uses differently:
  - A def is always marked writeThru if it is assigned a register. If it doesn't get a register
    at all, it is marked spillAfter as per usual.
  - A use is never marked spillAfter (as the stack location is always valid at a use).
* During resolution/writeback:
  - Mark all isWriteThru defs with `GTF_SPILL`, as for `spillAfter`, but keep the reg assignment,
    and the interval stays active.
  - Assert that uses of isWriteThru intervals are never marked spillAfter
* During `genFnProlog()`, ensure that incoming reg parameters that have register assoginments also
  get stored to stack if they are marked lvLiveInOutOfHndlr.

#### Challenges/Issues with the LSRA approach above:

* Liveness currently adds all exceptVars to the live-in for blocks where `ehBlockHasExnFlowDsc` returns true.
  This results in more "artificial" liveness than strictly entry to and exit from EH regions.
* In some cases, write-thru may be worse, performance-wise, than always using memory, if the EH local is
  infrequently referenced in non-EH code. This is a slightly different issue than known spill placement
  and allocation issues, but is related (i.e. when to choose not to keep the register live, and simply
  create the value in memory if that doesn't require a register).

## Next steps

The initial prototype that produced the example bellow is currently being
improved to make it production ready.  At the same time a more extensive suite
of example tests are being developed.

- [X] Proof of concept prototype.
- [ ] Production implementation of WriteThru phase.
- [ ] Suite of optimization examples/regression tests.
- [ ] Testing
   * [ ] Full CI test pass.
   * [ ] JIT benchmark diffs.
   * [ ] Kestrel techempower numbers.

## Example

The following is a simple example that shows enregistration for a local var
live, and modified, through a catch.

#### Source code snippet

```
class Enreg01
{
    int val;
    double dist;

    public Enreg01(int x) {
        val = x;
        dist = (double)x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int foo(ref double d) { return (int)d; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Run()
    {
        int sum = val;

        try {
            TryValue(97);
        }
        catch (ValueException e)
        {
            Console.WriteLine("Catching {0}", Convert.ToString(e.x));
            sum += val + e.x;
            foo(ref dist);
            sum += val;
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int TryValue(int y)
    {
        if (y == 97)
        {
            Console.WriteLine("Throwing 97");
            throw new ValueException(97);
        }
        else
        {
            return y;
        }
    }
}
```
#### Post WriteThru GenTree nodes for Run() method

The Run() contains the catch and is the only method the EH WriteThru modifies.

```
Creating enregisterable proxies:
lvaGrabTemp returning 8 (V08 tmp5) (a long lifetime temp) called for  Add proxy for EH Write Thru..
Creating proxy V08 for local var V00

lvaGrabTemp returning 9 (V09 tmp6) (a long lifetime temp) called for  Add proxy for EH Write Thru..
Creating proxy V09 for local var V01

Trees after EH Write Thru

---------------------------------------------------------------------------------------------------------------------------
BBnum         descAddr ref try hnd preds           weight   [IL range]      [jump]      [EH region]         [flags]
---------------------------------------------------------------------------------------------------------------------------
BB01 [00000263A1C161B8]  1                              1   [000..007)                                     i label target
BB02 [00000263A1C162D0]  1  0    BB01                   1   [007..012)                 T0      try { }     keep i try label gcsafe
BB03 [00000263A1C16500]  2       BB02,BB04              1   [050..052)        (return)                     i label target gcsafe
++++ funclets follow
BB04 [00000263A1C163E8]  0     0                        0   [012..050)-> BB03 ( cret )    H0 F catch { }   keep i rare label target gcsafe flet
-------------------------------------------------------------------------------------------------------------------------------------

------------ BB01 [000..007), preds={} succs={BB02}

***** BB01, stmt 1
     (  3,  3) [000123] ------------             *  stmtExpr  void  (IL   ???...  ???)
N001 (  3,  2) [000120] ------------             |  /--*  lclVar    ref    V00 this
N003 (  3,  3) [000122] -A------R---             \--*  =         ref
N002 (  1,  1) [000121] D------N----                \--*  lclVar    ref    V08 tmp5

***** BB01, stmt 2
     ( 17, 13) [000005] ------------             *  stmtExpr  void  (IL 0x000...0x006)
N007 (  3,  2) [000097] ------------             |     /--*  lclVar    int    V09 tmp6
N009 (  7,  5) [000098] -A------R---             |  /--*  =         int
N008 (  3,  2) [000096] D------N----             |  |  \--*  lclVar    int    V01 loc0
N010 ( 17, 13) [000099] -A-XG-------             \--*  comma     void
N004 (  6,  5) [000002] ---XG-------                |  /--*  indir     int
N002 (  1,  1) [000059] ------------                |  |  |  /--*  const     long   16 field offset Fseq[val]
N003 (  4,  3) [000060] -------N----                |  |  \--*  +         byref
N001 (  3,  2) [000001] ------------                |  |     \--*  lclVar    ref    V08 tmp5
N006 ( 10,  8) [000004] -A-XG---R---                \--*  =         int
N005 (  3,  2) [000003] D------N----                   \--*  lclVar    int    V09 tmp6

------------ BB02 [007..012), preds={BB01} succs={BB03}

***** BB02, stmt 3
     ( 16, 10) [000013] ------------             *  stmtExpr  void  (IL 0x007...0x00F)
N008 ( 16, 10) [000011] --C-G-------             \--*  call      int    Enreg01.TryIncrement
N004 (  1,  1) [000009] ------------ this in rcx    +--*  lclVar    ref    V08 tmp5
N005 (  1,  1) [000010] ------------ arg1 in rdx    \--*  const     int    97

------------ BB03 [050..052) (return), preds={BB02,BB04} succs={}

***** BB03, stmt 4
     (  3,  3) [000119] ------------             *  stmtExpr  void  (IL   ???...  ???)
N001 (  3,  2) [000116] ------------             |  /--*  lclVar    int    V01 loc0
N003 (  3,  3) [000118] -A------R---             \--*  =         int
N002 (  1,  1) [000117] D------N----                \--*  lclVar    int    V09 tmp6

***** BB03, stmt 5
     (  4,  3) [000017] ------------             *  stmtExpr  void  (IL 0x050...0x051)
N002 (  4,  3) [000016] ------------             \--*  return    int
N001 (  3,  2) [000015] ------------                \--*  lclVar    int    V09 tmp6

------------ BB04 [012..050) -> BB03 (cret), preds={} succs={BB03}

***** BB04, stmt 6
     (  5,  4) [000021] ------------             *  stmtExpr  void  (IL 0x012...0x012)
N001 (  1,  1) [000007] -----O------             |  /--*  catchArg  ref
N003 (  5,  4) [000020] -A---O--R---             \--*  =         ref
N002 (  3,  2) [000019] D------N----                \--*  lclVar    ref    V03 tmp0

***** BB04, stmt 7
     (  3,  3) [000111] ------------             *  stmtExpr  void  (IL   ???...  ???)
N001 (  3,  2) [000108] ------------             |  /--*  lclVar    ref    V00 this
N003 (  3,  3) [000110] -A------R---             \--*  =         ref
N002 (  1,  1) [000109] D------N----                \--*  lclVar    ref    V08 tmp5

***** BB04, stmt 8
     (  3,  3) [000115] ------------             *  stmtExpr  void  (IL   ???...  ???)
N001 (  3,  2) [000112] ------------             |  /--*  lclVar    int    V01 loc0
N003 (  3,  3) [000114] -A------R---             \--*  =         int
N002 (  1,  1) [000113] D------N----                \--*  lclVar    int    V09 tmp6

***** BB04, stmt 9
     ( 59, 43) [000034] ------------             *  stmtExpr  void  (IL 0x013...0x037)
N021 ( 59, 43) [000031] --CXG-------             \--*  call      void   System.Console.WriteLine
N002 (  5, 12) [000066] ----G-------                |  /--*  indir     ref
N001 (  3, 10) [000065] ------------                |  |  \--*  const(h)  long   0xB3963070 "Catching {0}"
N004 (  9, 15) [000076] -A--G---R-L- arg0 SETUP     +--*  =         ref
N003 (  3,  2) [000075] D------N----                |  \--*  lclVar    ref    V05 tmp2
N012 ( 20, 14) [000029] --CXG-------                |  /--*  call      ref    System.Convert.ToString
N010 (  6,  8) [000028] ---XG------- arg0 in rcx    |  |  \--*  indir     int
N008 (  1,  4) [000067] ------------                |  |     |  /--*  const     long   140 field offset Fseq[x]
N009 (  4,  6) [000068] -------N----                |  |     \--*  +         byref
N007 (  3,  2) [000027] ------------                |  |        \--*  lclVar    ref    V03 tmp0
N014 ( 24, 17) [000072] -ACXG---R-L- arg1 SETUP     +--*  =         ref
N013 (  3,  2) [000071] D------N----                |  \--*  lclVar    ref    V04 tmp1
N017 (  3,  2) [000073] ------------ arg1 in rdx    +--*  lclVar    ref    V04 tmp1          (last use)
N018 (  3,  2) [000077] ------------ arg0 in rcx    \--*  lclVar    ref    V05 tmp2          (last use)

***** BB04, stmt 10
     ( 18, 19) [000044] ------------             *  stmtExpr  void  (IL 0x028...  ???)
N014 (  1,  1) [000101] ------------             |     /--*  lclVar    int    V09 tmp6
N016 (  5,  4) [000102] -A------R---             |  /--*  =         int
N015 (  3,  2) [000100] D------N----             |  |  \--*  lclVar    int    V01 loc0
N017 ( 18, 19) [000103] -A-XG-------             \--*  comma     void
N010 (  6,  8) [000039] ---XG-------                |     /--*  indir     int
N008 (  1,  4) [000081] ------------                |     |  |  /--*  const     long   140 field offset Fseq[x]
N009 (  4,  6) [000082] -------N----                |     |  \--*  +         byref
N007 (  3,  2) [000038] ------------                |     |     \--*  lclVar    ref    V03 tmp0          (last use)
N011 ( 13, 15) [000041] ---XG-------                |  /--*  +         int
N005 (  4,  4) [000037] ---XG-------                |  |  |  /--*  indir     int
N003 (  1,  1) [000079] ------------                |  |  |  |  |  /--*  const     long   16 field offset Fseq[val]
N004 (  2,  2) [000080] -------N----                |  |  |  |  \--*  +         byref
N002 (  1,  1) [000036] ------------                |  |  |  |     \--*  lclVar    ref    V08 tmp5
N006 (  6,  6) [000040] ---XG-------                |  |  \--*  +         int
N001 (  1,  1) [000035] ------------                |  |     \--*  lclVar    int    V09 tmp6
N013 ( 13, 15) [000043] -A-XG---R---                \--*  =         int
N012 (  1,  1) [000042] D------N----                   \--*  lclVar    int    V09 tmp6

***** BB04, stmt 11
     ( 20, 14) [000051] ------------             *  stmtExpr  void  (IL 0x038...0x044)
N013 ( 20, 14) [000049] --CXGO------             \--*  call      int    Enreg01.foo
N007 (  1,  1) [000086] ------------                |     /--*  const     long   8 field offset Fseq[dist]
N008 (  3,  3) [000087] ------------                |  /--*  +         byref
N006 (  1,  1) [000085] ------------                |  |  \--*  lclVar    ref    V08 tmp5
N009 (  5,  5) [000088] ---XGO-N---- arg1 in rdx    +--*  comma     byref
N005 (  2,  2) [000084] ---X-O-N----                |  \--*  nullcheck byte
N004 (  1,  1) [000083] ------------                |     \--*  lclVar    ref    V08 tmp5
N010 (  1,  1) [000045] ------------ this in rcx    \--*  lclVar    ref    V08 tmp5

***** BB04, stmt 12
     ( 11, 10) [000058] ------------             *  stmtExpr  void  (IL 0x045...0x04D)
N009 (  1,  1) [000105] ------------             |     /--*  lclVar    int    V09 tmp6
N011 (  5,  4) [000106] -A------R---             |  /--*  =         int
N010 (  3,  2) [000104] D------N----             |  |  \--*  lclVar    int    V01 loc0
N012 ( 11, 10) [000107] -A-XG-------             \--*  comma     void
N005 (  4,  4) [000054] ---XG-------                |     /--*  indir     int
N003 (  1,  1) [000094] ------------                |     |  |  /--*  const     long   16 field offset Fseq[val]
N004 (  2,  2) [000095] -------N----                |     |  \--*  +         byref
N002 (  1,  1) [000053] ------------                |     |     \--*  lclVar    ref    V08 tmp5
N006 (  6,  6) [000055] ---XG-------                |  /--*  +         int
N001 (  1,  1) [000052] ------------                |  |  \--*  lclVar    int    V09 tmp6
N008 (  6,  6) [000057] -A-XG---R---                \--*  =         int
N007 (  1,  1) [000056] D------N----                   \--*  lclVar    int    V09 tmp6

```

#### Post register allocation and code generation code

```diff
--- base.asmdmp	2017-03-28 20:40:36.000000000 -0700
+++ wt.asmdmp	2017-03-28 20:41:11.000000000 -0700
@@ -1,78 +1,85 @@
 *************** After end code gen, before unwindEmit()
-G_M16307_IG01:        ; func=00, offs=000000H, size=0014H, gcVars=0000000000000000 {}, gcrefRegs=00000000 {}, byrefRegs=00000000 {}, gcvars, byref, nogc <-- Prolog IG
+G_M16307_IG01:        ; func=00, offs=000000H, size=0017H, gcVars=0000000000000000 {}, gcrefRegs=00000000 {}, byrefRegs=00000000 {}, gcvars, byref, nogc <-- Prolog IG

 push     rbp
+push     r14
 push     rdi
 push     rsi
+push     rbx
 sub      rsp, 48
-lea      rbp, [rsp+40H]
-mov      qword ptr [V07 rbp-20H], rsp
+lea      rbp, [rsp+50H]
+mov      qword ptr [V07 rbp-30H], rsp
 mov      gword ptr [V00 rbp+10H], rcx

-G_M16307_IG02:        ; offs=000014H, size=000AH, gcVars=0000000000000001 {V00}, gcrefRegs=00000000 {}, byrefRegs=00000000 {}, gcvars, byref
+G_M16307_IG02:        ; offs=000017H, size=000AH, gcVars=0000000000000001 {V00}, gcrefRegs=00000000 {}, byrefRegs=00000000 {}, gcvars, byref

-mov      rcx, gword ptr [V00 rbp+10H]
-mov      ecx, dword ptr [rcx+16]
-mov      dword ptr [V01 rbp-14H], ecx
+mov      rsi, gword ptr [V00 rbp+10H]
+mov      edi, dword ptr [rsi+16]
+mov      dword ptr [V01 rbp-24H], edi

-G_M16307_IG03:        ; offs=00001EH, size=000FH, gcrefRegs=00000000 {}, byrefRegs=00000000 {}, byref
+G_M16307_IG03:        ; offs=000021H, size=000EH, gcrefRegs=00000040 {rsi}, byrefRegs=00000000 {}, byref

-mov      rcx, gword ptr [V00 rbp+10H]
+mov      rcx, rsi     ; Elided reload in try region
 mov      edx, 97
 call     Enreg01:TryIncrement(int):int:this
 nop

-G_M16307_IG04:        ; offs=00002DH, size=0003H, gcVars=0000000000000000 {}, gcrefRegs=00000000 {}, byrefRegs=00000000 {}, gcvars, byref
+G_M16307_IG04:        ; offs=00002FH, size=0005H, gcVars=0000000000000000 {}, gcrefRegs=00000000 {}, byrefRegs=00000000 {}, gcvars, byref

-mov      eax, dword ptr [V01 rbp-14H]
+mov      edi, dword ptr [V01 rbp-24H]
+mov      eax, edi

-G_M16307_IG05:        ; offs=000030H, size=0008H, epilog, nogc, emitadd
+G_M16307_IG05:        ; offs=000034H, size=000BH, epilog, nogc, emitadd

-lea      rsp, [rbp-10H]
+lea      rsp, [rbp-20H]
+pop      rbx
 pop      rsi
 pop      rdi
+pop      r14
 pop      rbp
 ret

-G_M16307_IG06:        ; func=01, offs=000038H, size=0014H, gcrefRegs=00000004 {rdx}, byrefRegs=00000000 {}, byref, funclet prolog, nogc
+G_M16307_IG06:        ; func=01, offs=00003FH, size=0017H, gcrefRegs=00000004 {rdx}, byrefRegs=00000000 {}, byref, funclet prolog, nogc

 push     rbp
+push     r14
 push     rdi
 push     rsi
+push     rbx
 sub      rsp, 48
 mov      rbp, qword ptr [rcx+32]
 mov      qword ptr [rsp+20H], rbp
-lea      rbp, [rbp+40H]
+lea      rbp, [rbp+50H]

-G_M16307_IG07:        ; offs=00004CH, size=005EH, gcVars=0000000000000001 {V00}, gcrefRegs=00000004 {rdx}, byrefRegs=00000000 {}, gcvars, byref, isz
+G_M16307_IG07:        ; offs=000056H, size=0054H, gcVars=0000000000000001 {V00}, gcrefRegs=00000004 {rdx}, byrefRegs=00000000 {}, gcvars, byref, isz

 mov      rsi, rdx
-mov      rcx, 0x18A3C473070
-mov      rdi, gword ptr [rcx]
+mov      rcx, gword ptr [V00 rbp+10H]        ; Reload of proxy register
+mov      rdi, rcx                            ; Missed peep
+mov      ecx, dword ptr [V01 rbp-24H]        ; Reload of proxy register
+mov      ebx, ecx                            ; Missed peep
+mov      rcx, 0x263B3963070
+mov      r14, gword ptr [rcx]                ; Missed addressing mode
 mov      ecx, dword ptr [rsi+140]
 call     System.Convert:ToString(int):ref
 mov      rdx, rax
-mov      rcx, rdi
+mov      rcx, r14
 call     System.Console:WriteLine(ref,ref)
-mov      edx, dword ptr [V01 rbp-14H]        ; Elided stack access
-mov      rcx, gword ptr [V00 rbp+10H]        ; Elided stack access
-add      edx, dword ptr [rcx+16]
-add      edx, dword ptr [rsi+140]
-mov      dword ptr [V01 rbp-14H], edx        ; Elided stack access
-mov      rdx, gword ptr [V00 rbp+10H]        ; Elided stack access
-add      rdx, 8
-mov      rcx, gword ptr [V00 rbp+10H]        ; Elided stack access
+add      ebx, dword ptr [rdi+16]
+add      ebx, dword ptr [rsi+140]
+lea      rdx, bword ptr [rdi+8]
+mov      rcx, rdi
 call     Enreg01:foo(byref):int:this
-mov      eax, dword ptr [V01 rbp-14H]        ; Elided stack access
-mov      rdx, gword ptr [V00 rbp+10H]        ; Elided stack access
-add      eax, dword ptr [rdx+16]
-mov      dword ptr [V01 rbp-14H], eax        ; Elided stack access
+add      ebx, dword ptr [rdi+16]
+mov      dword ptr [V01 rbp-24H], ebx        ; Store of proxy register
 lea      rax, G_M16307_IG04

-G_M16307_IG08:        ; offs=0000AAH, size=0008H, funclet epilog, nogc, emitadd
+G_M16307_IG08:        ; offs=0000AAH, size=000BH, funclet epilog, nogc, emitadd

 add      rsp, 48
+pop      rbx
 pop      rsi
 pop      rdi
+pop      r14
 pop      rbp
 ret

```

Summary of diff:
replaced 6 loads and 2 stores with 2 loads, 1 store, 2 push, 2 pop.



