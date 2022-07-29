Finally Optimizations
=====================

In MSIL, a try-finally is a construct where a block of code
(the finally) is guaranteed to be executed after control leaves a
protected region of code (the try) either normally or via an
exception.

In RyuJit a try-finally is currently implemented by transforming the
finally into a local function that is invoked via jitted code at normal
exits from the try block and is invoked via the runtime for exceptional
exits from the try block.

For x86 the local function is simply a part of the method and shares
the same stack frame with the method. For other architectures the
local function is promoted to a potentially separable "funclet"
which is almost like a regular function with a prolog and epilog. A
custom calling convention gives the funclet access to the parent stack
frame.

In this proposal we outline three optimizations for finallys: removing
empty trys, removing empty finallys and finally cloning.

Empty Finally Removal
---------------------

An empty finally is one that has no observable effect. These often
arise from `foreach` or `using` constructs (which induce a
try-finally) where the cleanup method called in the finally does
nothing. Often, after inlining, the empty finally is readily apparent.

For example, this snippet of C# code
```C#
static int Sum(List<int> x) {
    int sum = 0;
    foreach(int i in x) {
        sum += i;
    }
    return sum;
}
```
produces the following jitted code:
```asm
; Successfully inlined Enumerator[Int32][System.Int32]:Dispose():this
;    (1 IL bytes) (depth 1) [below ALWAYS_INLINE size]
G_M60484_IG01:
       55                   push     rbp
       57                   push     rdi
       56                   push     rsi
       4883EC50             sub      rsp, 80
       488D6C2460           lea      rbp, [rsp+60H]
       488BF1               mov      rsi, rcx
       488D7DD0             lea      rdi, [rbp-30H]
       B906000000           mov      ecx, 6
       33C0                 xor      rax, rax
       F3AB                 rep stosd
       488BCE               mov      rcx, rsi
       488965C0             mov      qword ptr [rbp-40H], rsp

G_M60484_IG02:
       33C0                 xor      eax, eax
       8945EC               mov      dword ptr [rbp-14H], eax
       8B01                 mov      eax, dword ptr [rcx]
       8B411C               mov      eax, dword ptr [rcx+28]
       33D2                 xor      edx, edx
       48894DD0             mov      gword ptr [rbp-30H], rcx
       8955D8               mov      dword ptr [rbp-28H], edx
       8945DC               mov      dword ptr [rbp-24H], eax
       8955E0               mov      dword ptr [rbp-20H], edx

G_M60484_IG03:
       488D4DD0             lea      rcx, bword ptr [rbp-30H]
       E89B35665B           call     Enumerator[Int32][System.Int32]:MoveNext():bool:this
       85C0                 test     eax, eax
       7418                 je       SHORT G_M60484_IG05

; Body of foreach loop

G_M60484_IG04:
       8B4DE0               mov      ecx, dword ptr [rbp-20H]
       8B45EC               mov      eax, dword ptr [rbp-14H]
       03C1                 add      eax, ecx
       8945EC               mov      dword ptr [rbp-14H], eax
       488D4DD0             lea      rcx, bword ptr [rbp-30H]
       E88335665B           call     Enumerator[Int32][System.Int32]:MoveNext():bool:this
       85C0                 test     eax, eax
       75E8                 jne      SHORT G_M60484_IG04

; Normal exit from the implicit try region created by `foreach`
; Calls the finally to dispose of the iterator

G_M60484_IG05:
       488BCC               mov      rcx, rsp
       E80C000000           call     G_M60484_IG09      // call to finally

G_M60484_IG06:
       90                   nop

G_M60484_IG07:
       8B45EC               mov      eax, dword ptr [rbp-14H]

G_M60484_IG08:
       488D65F0             lea      rsp, [rbp-10H]
       5E                   pop      rsi
       5F                   pop      rdi
       5D                   pop      rbp
       C3                   ret

; Finally funclet. Note it simply sets up and then tears down a stack
; frame. The dispose method was inlined and is empty.

G_M60484_IG09:
       55                   push     rbp
       57                   push     rdi
       56                   push     rsi
       4883EC30             sub      rsp, 48
       488B6920             mov      rbp, qword ptr [rcx+32]
       48896C2420           mov      qword ptr [rsp+20H], rbp
       488D6D60             lea      rbp, [rbp+60H]

G_M60484_IG10:
       4883C430             add      rsp, 48
       5E                   pop      rsi
       5F                   pop      rdi
       5D                   pop      rbp
       C3                   ret
```

In such cases the try-finally can be removed, leading to code like the following:
```asm
G_M60484_IG01:
       57                   push     rdi
       56                   push     rsi
       4883EC38             sub      rsp, 56
       488BF1               mov      rsi, rcx
       488D7C2420           lea      rdi, [rsp+20H]
       B906000000           mov      ecx, 6
       33C0                 xor      rax, rax
       F3AB                 rep stosd
       488BCE               mov      rcx, rsi

G_M60484_IG02:
       33F6                 xor      esi, esi
       8B01                 mov      eax, dword ptr [rcx]
       8B411C               mov      eax, dword ptr [rcx+28]
       48894C2420           mov      gword ptr [rsp+20H], rcx
       89742428             mov      dword ptr [rsp+28H], esi
       8944242C             mov      dword ptr [rsp+2CH], eax
       89742430             mov      dword ptr [rsp+30H], esi

G_M60484_IG03:
       488D4C2420           lea      rcx, bword ptr [rsp+20H]
       E8A435685B           call     Enumerator[Int32][System.Int32]:MoveNext():bool:this
       85C0                 test     eax, eax
       7414                 je       SHORT G_M60484_IG05

G_M60484_IG04:
       8B4C2430             mov      ecx, dword ptr [rsp+30H]
       03F1                 add      esi, ecx
       488D4C2420           lea      rcx, bword ptr [rsp+20H]
       E89035685B           call     Enumerator[Int32][System.Int32]:MoveNext():bool:this
       85C0                 test     eax, eax
       75EC                 jne      SHORT G_M60484_IG04

G_M60484_IG05:
       8BC6                 mov      eax, esi

G_M60484_IG06:
       4883C438             add      rsp, 56
       5E                   pop      rsi
       5F                   pop      rdi
       C3                   ret
```

Empty finally removal is unconditionally profitable: it should always
reduce code size and improve code speed.

Empty Try Removal
---------------------

If the try region of a try-finally is empty, and the jitted code will
execute on a runtime that does not protect finally execution from
thread abort, then the try-finally can be replaced with just the
content of the finally.

Empty trys with non-empty finallys often exist in code that must run
under both thread-abort aware and non-thread-abort aware runtimes. In
the former case the placement of cleanup code in the finally ensures
that the cleanup code will execute fully. But if thread abort is not
possible, the extra protection offered by the finally is not needed.

Empty try removal looks for try-finallys where the try region does
nothing except invoke the finally. There are currently two different
EH implementation models, so the try screening has two cases:

* callfinally thunks (x64/arm64): the try must be a single empty
basic block that always jumps to a callfinally that is the first
half of a callfinally/always pair;
* non-callfinally thunks (x86/arm32): the try must be a
callfinally/always pair where the first block is an empty callfinally.

The screening then verifies that the callfinally identified above is
the only callfinally for the try. No other callfinallys are expected
because this try cannot have multiple leaves and its handler cannot be
reached by nested exit paths.

When the empty try is identified, the jit modifies the
callfinally/always pair to branch to the handler, modifies the
handler's return to branch directly to the continuation (the
branch target of the second half of the callfinally/always pair),
updates various status flags on the blocks, and then removes the
try-finally region.

Finally Cloning
---------------

Finally cloning is an optimization where the jit duplicates the code
in the finally for one or more of the normal exit paths from the try,
and has those exit points branch to the duplicated code directly,
rather than calling the finally.  This transformation allows for
improved performance and optimization of the common case where the try
completes without an exception.

Finally cloning also allows hot/cold splitting of finally bodies: the
cloned finally code covers the normal try exit paths (the hot cases)
and can be placed in the main method region, and the original finally,
now used largely or exclusively for exceptional cases (the cold cases)
spilt off into the cold code region. Without cloning, RyuJit
would always treat the finally as cold code.

Finally cloning will increase code size, though often the size
increase is mitigated somewhat by more compact code generation in the
try body and streamlined invocation of the cloned finallys.

Try-finally regions may have multiple normal exit points. For example
the following `try` has two: one at the `return 3` and one at the try
region end:

```C#
try {
   if (p) return 3;
   ...
}
finally {
   ...
}
return 4;
```

Here the finally must be executed no matter how the try exits. So
there are to two normal exit paths from the try, both of which pass
through the finally but which then diverge. The fact that some try
regions can have multiple exits opens the potential for substantial
code growth from finally cloning, and so leads to a choice point in
the implementation:

* Share the clone along all exit paths
* Share the clone along some exit paths
* Clone along all exit paths
* Clone along some exit paths
* Only clone along one exit path
* Only clone when there is one exit path

The shared clone option must essentially recreate or simulate the
local call mechanism for the finally, though likely somewhat more
efficiently. Each exit point must designate where control should
resume once the shared finally has finished.  For instance the jit
could introduce a new local per try-finally to determine where the
cloned finally should resume, and enumerate the possibilities using a
small integer. The end of the cloned finally would then use a switch
to determine what code to execute next. This has the downside of
introducing unrealizable paths into the control flow graph.

Cloning along all exit paths can potentially lead to large amounts of
code growth.

Cloning along some paths or only one path implies that some normal
exit paths won't be as well optimized. Nonetheless cloning along one
path was the choice made by JIT64 and the one we recommend for
implementation. In particular we suggest only cloning along the end of
try region exit path, so that any early exit will continue to invoke
the funclet for finally cleanup (unless that exit happens to have the
same post-finally continuation as the end try region exit, in which
case it can simply jump to the cloned finally).

One can imagine adaptive strategies. The size of the finally can
be roughly estimated and the number of clones needed for full cloning
readily computed. Selective cloning can be based on profile
feedback or other similar mechanisms for choosing the profitable
cases.

The current implementation will clone the finally and retarget the
last (largest IL offset) leave in the try region to the clone. Any
other leave that ultimately transfers control to the same post-finally
offset will also be modified to jump to the clone.

Empirical studies have shown that most finallys are small. Thus to
avoid excessive code growth, a crude size estimate is formed by
counting the number of statements in the blocks that make up the
finally. Any finally larger that 15 statements is not cloned. In our
study this disqualified about 0.5% of all finallys from cloning.

### EH Nesting Considerations

Finally cloning is also more complicated when the finally encloses
other EH regions, since the clone will introduce copies of all these
regions. While it is possible to implement cloning in such cases we
propose to defer for now.

Finally cloning is also a bit more complicated if the finally is
enclosed by another finally region, so we likewise propose deferring
support for this.  (Seems like a rare enough thing but maybe not too
hard to handle -- though possibly not worth it if we're not going to
support the enclosing case).

### Control-Flow and Other Considerations

If the try never exits normally, then the finally can only be invoked
in exceptional cases. There is no benefit to cloning since the cloned
finally would be unreachable. We can detect a subset of such cases
because there will be no call finally blocks.

JIT64 does not clone finallys that contained switch. We propose to
do likewise. (Initially I did not include this restriction but
hit a failing test case where the finally contained a switch. Might be
worth a deeper look, though such cases are presumably rare.)

If the finally never exits normally, then we presume it is cold code,
and so will not clone.

If the finally is marked as run rarely, we will not clone.

Implementation Proposal
-----------------------

We propose that empty finally removal and finally cloning be run back
to back, spliced into the phase list just after fgInline and
fgAddInternal, and just before implicit by-ref and struct
promotion. We want to run these early before a lot of structural
invariants regarding EH are put in place, and before most
other optimization, but run them after inlining
(so empty finallys can be more readily identified) and after the
addition of implicit try-finallys created by the jit.  Empty finallys
may arise later because of optimization, but this seems relatively
uncommon.

We will remove empty finallys first, then clone.

Neither optimization will run when the jit is generating debuggable
code or operating in min opts mode.

### Empty Finally Removal (Sketch)

Skip over methods that have no EH, are compiled with min opts, or
where the jit is generating debuggable code.

Walk the handler table, looking for try-finally (we could also look
for and remove try-faults with empty faults, but those are presumably
rare).

If the finally is a single block and contains only a `retfilter`
statement, then:

* Retarget the callfinally(s) to jump always to the continuation blocks.
* Remove the paired jump always block(s) (note we expect all finally
calls to be paired since the empty finally returns).
* For funclet EH models with finally target bits, clear the finally
target from the continuations.
* For non-funclet EH models only, clear out the GT_END_LFIN statement
in the finally continuations.
* Remove the handler block.
* Reparent all directly contained try blocks to the enclosing try region
or to the method region if there is no enclosing try.
* Remove the try-finally from the EH table via `fgRemoveEHTableEntry`.

After the walk, if any empty finallys were removed, revalidate the
integrity of the handler table.

### Finally Cloning (Sketch)

Skip over all methods, if the runtime supports thread abort. More on
this below.

Skip over methods that have no EH, are compiled with min opts, or
where the jit is generating debuggable code.

Walk the handler table, looking for try-finally. If the finally is
enclosed in a handler or encloses another handler, skip.

Walk the finally body blocks. If any is BBJ_SWITCH, or if none
is BBJ_EHFINALLYRET, skip cloning. If all blocks are RunRarely
skip cloning. If the finally has more that 15 statements, skip
cloning.

Walk the try region from back to front (from largest to smallest IL
offset). Find the last block in the try that invokes the finally. That
will be the path that will invoke the clone.

If the EH model requires callfinally thunks, and there are multiple
thunks that invoke the finally, and the callfinally thunk along the
clone path is not the first, move it to the front (this helps avoid
extra jumps).

Set the insertion point to just after the callfinally in the path (for
thunk models) or the end of the try (for non-thunk models).  Set up a
block map. Clone the finally body using `fgNewBBinRegion` and
`fgNewBBafter` to make the first and subsequent blocks, and
`CloneBlockState` to fill in the block contents. Clear the handler
region on the cloned blocks. Bail out if cloning fails. Mark the first
and last cloned blocks with appropriate BBF flags. Patch up inter-clone
branches and convert the returns into jumps to the continuation.

Walk the callfinallys, retargeting the ones that return to the
continuation so that they invoke the clone. Remove the paired always
blocks. Clear the finally target bit and any GT_END_LFIN from the
continuation.

If all call finallys are converted, modify the region to be try/fault
(internally EH_HANDLER_FAULT_WAS_FINALLY, so we can distinguish it
later from "organic" try/faults).  Otherwise leave it as a
try/finally.

Clear the catch type on the clone entry.

### Thread Abort

For runtimes that support thread abort (desktop), more work is
required:

* The cloned finally must be reported to the runtime. Likely this
can trigger off of the BBF_CLONED_FINALLY_BEGIN/END flags.
* The jit must maintain the integrity of the clone by not losing
track of the blocks involved, and not allowing code to move in our
out of the cloned region

Code Size Impact
----------------

Code size impact from finally cloning was measured for CoreCLR on
Windows x64.

```
Total bytes of diff: 16158 (0.12 % of base)
    diff is a regression.
Total byte diff includes 0 bytes from reconciling methods
        Base had    0 unique methods,        0 unique bytes
        Diff had    0 unique methods,        0 unique bytes
Top file regressions by size (bytes):
        3518 : Microsoft.CodeAnalysis.CSharp.dasm (0.16 % of base)
        1895 : System.Linq.Expressions.dasm (0.32 % of base)
        1626 : Microsoft.CodeAnalysis.VisualBasic.dasm (0.07 % of base)
        1428 : System.Threading.Tasks.Parallel.dasm (4.66 % of base)
        1248 : System.Linq.Parallel.dasm (0.20 % of base)
Top file improvements by size (bytes):
       -4529 : System.Private.CoreLib.dasm (-0.14 % of base)
        -975 : System.Reflection.Metadata.dasm (-0.28 % of base)
        -239 : System.Private.Uri.dasm (-0.27 % of base)
        -104 : System.Runtime.InteropServices.RuntimeInformation.dasm (-3.36 % of base)
         -99 : System.Security.Cryptography.Encoding.dasm (-0.61 % of base)
57 total files with size differences.
Top method regessions by size (bytes):
         645 : System.Diagnostics.Process.dasm - System.Diagnostics.Process:StartCore(ref):bool:this
         454 : Microsoft.CSharp.dasm - Microsoft.CSharp.RuntimeBinder.Semantics.ExpressionBinder:AdjustCallArgumentsForParams(ref,ref,ref,ref,ref,byref):this
         447 : System.Threading.Tasks.Dataflow.dasm - System.Threading.Tasks.Dataflow.Internal.SpscTargetCore`1[__Canon][System.__Canon]:ProcessMessagesLoopCore():this
         421 : Microsoft.CodeAnalysis.VisualBasic.dasm - Microsoft.CodeAnalysis.VisualBasic.Symbols.ImplementsHelper:FindExplicitlyImplementedMember(ref,ref,ref,ref,ref,ref,byref):ref
         358 : System.Private.CoreLib.dasm - System.Threading.TimerQueueTimer:Change(int,int):bool:this
Top method improvements by size (bytes):
       -2512 : System.Private.CoreLib.dasm - DomainNeutralILStubClass:IL_STUB_CLRtoWinRT():ref:this (68 methods)
        -824 : Microsoft.CodeAnalysis.dasm - Microsoft.Cci.PeWriter:WriteHeaders(ref,ref,ref,ref,byref):this
        -663 : System.Private.CoreLib.dasm - DomainNeutralILStubClass:IL_STUB_CLRtoWinRT(ref):int:this (17 methods)
        -627 : System.Private.CoreLib.dasm - System.Diagnostics.Tracing.ManifestBuilder:CreateManifestString():ref:this
        -546 : System.Private.CoreLib.dasm - DomainNeutralILStubClass:IL_STUB_WinRTtoCLR(long):int:this (67 methods)
3014 total methods with size differences.
```

The largest growth is seen in `Process:StartCore`, which has 4
try-finally constructs.

Diffs generally show improved codegen in the try bodies with cloned
finallys. However some of this improvement comes from more aggressive
use of callee save registers, and this causes size inflation in the
funclets (note finally cloning does not alter the number of
funclets). So if funclet save/restore could be contained to registers
used in the funclet, the size impact would be slightly smaller.

There are also some instances where cloning relatively small finallys
leads to large code size increases. xxx is one example.
