## OSR x64 Epilog Redesign

### Problem

The current x64 OSR epilog generation creates "non-canonical"
epilogs. While the code sequences are correct, the windows x64
unwinder depends on code generators to produce canonical epilogs, so
that the unwinder can reliably detect when an IP is within an epilog.

The windows x64 unwind info has no data whatsoever on epilogs, so this
sort of implicit epilog detection is necessary. The unwinder
disassembles the code at starting at the IP to deduce if the IP is
within an epilog. Only very specific sequences of instructions are
expected, and anything unexpected causes the unwinder to deduce
that the IP is not in an epilog.

The canonical epilog is a single RSP adjust followed by some number of
non-volatile integer register POPs, and then a RET or JMP. Non-volatile float
registers are restored outside the epilog via MOVs.

OSR methods currently generate the following kind of epilog. It is
non-canonical because of the second RSP adjustment, whose purpose is
to remove the Tier0 frame from the stack.

```asm
       add      rsp, 120   ;; pop OSR contribution to frame
       pop      rbx        ;; restore non-volatile regs (callee-saves)
       pop      rsi
       pop      rdi
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       add      rsp, 472   ;; pop Tier0 contribution to frame
       pop      rbp        ;; second RBP restore (see below)
       ret
```

These non-canonical OSR epilogs break the x64 unwinder's "in epilog"
detection and also break epilog unwind. This leads to assertions and
bugs during thread suspension, when suspended threads are in the
middle of OSR epilogs, and to broken stack traces when walking the
stack for diagnostic purposes (debugging or sampling).

The CLR (mostly?) tries to avoid suspending threads in epilog, but it
does this by suspending the thread and then calling into the os
unwinder to determine if a thread is in an epilog. The non-canonical
OSR epilogs break thread suspension.

So it is imperative that the x64 OSR epilog sequence be one that the
OS unwinder can reliably recognize as an epilog. It is also beneficial
(though perhaps not mandatory) to be able to unwind from such epilogs; this
improves diagnostic stackwalking accuracy and allows hijacking to
work normally during epilogs, if needed.

Arm64 unwind codes are more flexible and the OSR epilogs we generate
today do not cause any known problems.

### Solution

If the OSR method is required to have a canonical epilog, a single
RSP adjust must remove both the OSR and Tier0 frames. This implies any
and all nonvolatile integer register saves must be stored at the root of the
Tier0 frame so that they can be properly restored by the OSR epilog
via POPs after the single RSP adjustment.

Generally speaking, the Tier0 and OSR methods will not save the same
set of non-volatile registers, and there is no way for the Tier0
method to know which registers the OSR methods might want to save.

Thus we will require that any Tier0 method with patchpoints must
reserve the maximum sized area for integer registers (8 regs * 8 bytes
on Windows, 64 bytes).  The Tier0 method will only use the part it
needs. The rest will be unused unless we end up creating an OSR
method. OSR methods will save any additional nonvolatile registers
they use in this area in their prologs.

OSR method epilogs will then adjust the SP to remove both the OSR and
Tier0 frames, setting RSP to the appropriate offset into the save
area, so that the epilog can pop all the saved nonvolatile registers and
return.  This gives OSR methods a canonical epilog.

That fixes the epilogs. But we must now also ensure that all this can
be handled properly in the OSR prolog, so that in-prolog and in-body
unwind are still viable.

A typical prolog would PUSH the non-volatiles it wants to save, but
on entry, the OSR method's RSP is pointing below the Tier0 frame,
and so is located well below the save area. So PUSHing is not possible.

Instead, the OSR method will use MOVs to save nonvolatile
registers. Luckily, the x64 unwind format has support describing saves
done via MOVs instead of PUSHes via `UWOP_SAVE_NONVOL` (added for supporting
shrink-wrapping). We will use these codes to describe the callee save actions
in the OSR prolog.

This new unwind code uses the established frame pointer (for x64 OSR this
is always RSP) and so integer callee saves must be saved only after any
RSP adjustments are made. This means in an OSR frame prolog the SP adjustment
happens first, then the (additional) callee saves are saved. We need
to take some care to ensure no callee save is trashed during the SP
adjustment (which may be more than just an add, say if stack probing is needed).

### Work Needed

* Update the Tier0 method to allocate a maximally sized integer save area.

* OSR method prolog and unwind fixes
  * To express the fact that some callee saves were saved by the Tier0
method, the OSR method will first issue a phantom (unwind only, offset 0)
series of pushes for those callee saves.
  * Next the OSR method will do a phantom SP adjust to account for the
remainder of the Tier0 frame and any SP adjustment done by the patchpoint
transition code.
  * Since the Tier0 method is always an RBP frame and always saves RBP at the
    top of the register save area, the OSR method does not need to save RBP, and
    RBP can be restored from the Tier0 save. But (for RBP OSR frames) the x64
    OSR prolog must still set up a proper frame chain. So it will load from RBP
    (into a scratch register) and push the result to establish a proper value
    for RBP-based frame chaining. The OSR method is invoked with the Tier0 RBP,
    so this load/push fetches the Tier0 caller RBP and stores it in a slot on
    the OSR frame. This sets up a redundant copy of the saved RBP that does not
    need to undone on method exit.
  * Next the OSR prolog will establish its final RSP.
  * Finally the OSR method will save any remaining callee saves, using MOV
    instructions and `UWOP_NONVOL_SAVE` unwind records.
  * Nonvolatile float (xmm) registers continue to be stored via MOVs
    done after the int callee saves and RSP adjust -- their save area can be
    disjoint from the integer save area. Thus XMM registers can be saved to and
    restored from space on the OSR frame (otherwise the Tier0 frame would
    need to reserve another 160 bytes (windows) to hold possible OSR XMM
    saves). We do not yet take advantage of the fact that Tier0 methods
    may have also saved XMMs so that the OSR method may only need to save
    a subset.

### Example

Here is an example contrasting the new and old approaches on a test case.

#### Old Approach
```asm
;; Tier0 prolog

       55                   push     rbp
       56                   push     rsi
       4883EC38             sub      rsp, 56
       488D6C2440           lea      rbp, [rsp+40H]

;; Tier0 epilog

       4883C438             add      rsp, 56
       5E                   pop      rsi
       5D                   pop      rbp
       C3                   ret

;; Tier0 unwind

    CodeOffset: 0x06 UnwindOp: UWOP_ALLOC_SMALL (2)     OpInfo: 6 * 8 + 8 = 56 = 0x38
    CodeOffset: 0x02 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rsi (6)
    CodeOffset: 0x01 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rbp (5)

;; OSR prolog

       57                   push     rdi
       56                   push     rsi           // redundant
       4883EC28             sub      rsp, 40

;; OSR epilog (non-standard)

       4883C428             add      rsp, 40
       5E                   pop      rsi
       5F                   pop      rdi
       4883C448             add      rsp, 72
       5D                   pop      rbp
       C3                   ret

;; OSR unwind

    CodeOffset: 0x06 UnwindOp: UWOP_ALLOC_SMALL (2)     OpInfo: 4 * 8 + 8 = 40 = 0x28
    CodeOffset: 0x02 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rsi (6)
    CodeOffset: 0x01 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rdi (7)

    ;; "phantom unwind" records at offset 0 (Tier0 actions)

    CodeOffset: 0x00 UnwindOp: UWOP_ALLOC_SMALL (2)     OpInfo: 8 * 8 + 8 = 72 = 0x48
    CodeOffset: 0x00 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rbp (5)
```

#### New Approach

Note how the OSR method only saves RDI in its prolog, as RSI was already saved.
And this save happens *after* RSP is updated in the OSR frame.
Restore of RDI in unwind uses `UWOP_SAVE_NONVOL`.
```asm
;; Tier0 prolog

       55                   push     rbp
       56                   push     rsi
       4883EC68             sub      rsp, 104           // leave room for OSR
       488D6C2470           lea      rbp, [rsp+70H]

;; Tier0 epilog

       4883C468             add      rsp, 104
       5E                   pop      rsi
       5D                   pop      rbp
       C3                   ret

;; Tier0 unwind

    CodeOffset: 0x06 UnwindOp: UWOP_ALLOC_SMALL (2)     OpInfo: 12 * 8 + 8 = 104 = 0x68
    CodeOffset: 0x02 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rsi (6)
    CodeOffset: 0x01 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rbp (5)

;; OSR prolog

       4883EC38             sub      rsp, 56
       4889BC24A0000000     mov      qword ptr [rsp+A0H], rdi

;; OSR epilog (standard)

       4881C4A0000000       add      rsp, 160
       5F                   pop      rdi
       5E                   pop      rsi
       5D                   pop      rbp
       C3                   ret

;; OSR unwind

    CodeOffset: 0x0C UnwindOp: UWOP_SAVE_NONVOL (4)     OpInfo: rdi (7)
      Scaled Small Offset: 20 * 8 = 160 = 0x000A0
    CodeOffset: 0x04 UnwindOp: UWOP_ALLOC_SMALL (2)     OpInfo: 6 * 8 + 8 = 56 = 0x38

    ;; "phantom unwind" records at offset 0 (Tier0 actions)

    CodeOffset: 0x00 UnwindOp: UWOP_ALLOC_SMALL (2)     OpInfo: 13 * 8 + 8 = 112 = 0x70
    CodeOffset: 0x00 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rsi (6)
    CodeOffset: 0x00 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rbp (5)
```

### Notes

* We are not changing arm64 OSR at this time, it still uses the "old plan". Non-standard epilogs are handled on arm64 via epilog unwind codes.

* The OSR frame still reserves space for callee saves on its frame, despite
not saving them there.