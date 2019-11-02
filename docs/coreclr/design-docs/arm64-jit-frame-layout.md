# ARM64 JIT frame layout

NOTE: This document was written before the code was written, and hasn't been
verified to match existing code. It refers to some documents that might not be
open source.

This document describes the frame layout constraints and options for the ARM64
JIT compiler.

These frame layouts were taken from the "Windows ARM64 Exception Data"
specification, and expanded for use by the JIT.

We will generate chained frames in most case (where we save the frame pointer on
the stack, and point the frame pointer (x29) at the saved frame pointer),
including all non-leaf frames, to support ETW stack walks. This is recommended
by the "Windows ARM64 ABI" document. See `ETW_EBP_FRAMED` in the JIT code. (We
currently don’t set `ETW_EBP_FRAMED` for ARM64.)

For frames with alloca (dynamic stack allocation), we must use a frame pointer
that is fixed after the prolog (and before any alloca), so the stack pointer can
vary. The frame pointer will be used to access locals, parameters, etc., in the
fixed part of the frame.

For non-alloca frames, the stack pointer is set and not changed at the end of
the prolog. In this case, the stack pointer can be used for all frame member
access. If a frame pointer is also created, the frame pointer can optionally be
used to access frame members if it gives an encoding advantage.

We require a frame pointer for several cases: (1) functions with exception
handling establish a frame pointer so handler funclets can use the frame pointer
to access parent function locals, (2) for functions with P/Invoke, (3) for
certain GC encoding limitations or requirements, (4) for varargs functions, (5)
for Edit & Continue functions, (6) for debuggable code, and (7) for MinOpts.
This list might not be exhaustive.

On ARM64, the stack pointer must remain 16 byte aligned at all times.

The immediate offset addressing modes for various instructions have different
offset ranges. We want the frames to be designed to efficiently use the
available instruction encodings. Some important offset ranges for immediate
offset addressing include:

* ldrb /ldrsb / strb, unsigned offset: 0 to 4095
* ldrh /ldrsh / strh, unsigned offset: 0 to 8190, multiples of 2 (aligned halfwords)
* ldr / str (32-bit variant) / ldrsw, unsigned offset: 0 to 16380, multiple of 4 (aligned words)
* ldr / str (64-bit variant), unsigned offset: 0 to 32760, multiple of 8 (aligned doublewords)
* ldp / stp (32-bit variant), pre-indexed, post-indexed, and signed offset: -256 to 252, multiple of 4
* ldp / stp (64-bit variant), pre-indexed, post-indexed, and signed offset: -512 to 504, multiple of 8
* ldurb / ldursb / ldurh / ldursb / ldur (32-bit and 64-bit variants) / ldursw / sturb / sturh / stur (32-bit and 64-bit variants): -256 to 255
* ldr / ldrh / ldrb / ldrsw / ldrsh / ldrsb / str / strh / strb pre-indexed/post-indexed: -256 to 255 (unscaled)
* add / sub (immediate): 0 to 4095, or with 12 bit left shift: 4096 to 16777215 (multiples of 4096).
  * Thus, to construct a frame larger than 4095 using `sub`, we could use one "small" sub, or one "large" / shifted sub followed by a single "small" / unshifted sub. The reverse applies for tearing down the frame.
  * Note that we need to probe the stack for stack overflow when allocating large frames.

Most of the offset modes (that aren't pre-indexed or post-indexed) are unsigned.
Thus, we want the frame pointer, if it exists, to be at a lower address than the
objects on the frame (with the small caveat that we could use the limited
negative offset addressing capability of the `ldu*` / `stu*` unscaled modes).
The stack pointer will point to the first slot of the outgoing stack argument
area, if any, even for alloca functions (thus, the alloca operation needs to
"move" the outgoing stack argument space down), so filling the outgoing stack
argument space will always use SP.

For extremely large frames (e.g., frames larger than 32760, certainly, but
probably for any frame larger than 4095), we need to globally reserve and use an
additional register to construct an offset, and then use a register offset mode
(see `compRsvdRegCheck()`). It is unlikely we could accurately allocate a
register for this purpose at all points where it will be actually necessary.

In general, we want to put objects close to the stack or frame pointer, to take
advantage of the limited addressing offsets described above, especially if we
use the ldp/stp instructions. If we do end up using ldp/stp, we will want to
consider pointing the frame pointer somewhere in the middle of the locals (or
other objects) in the frame, to maximize the limited, but signed, offset range.
For example, saved callee-saved registers should be far from the frame/stack
pointer, since they are going to be saved once and loaded once, whereas
locals/temps are expected to be used more frequently.

For variadic (varargs) functions, and possibly for functions with incoming
struct register arguments, it is easier to put the arguments on the stack in the
prolog such that the entire argument list is contiguous in memory, including
both the register and stack arguments. On ARM32, we used the "prespill" concept,
where we used a register mask "push" instruction for the "prespilled" registers.
Note that on ARM32, structs could be split between incoming argument registers
and the stack. On ARM64, this is not true. A struct <=16 bytes is passed in one
or two consecutive registers, or entirely on the stack. Structs >16 bytes are
passed by reference (the caller allocates space for the struct in its frame,
copies the output struct value there, and passes a pointer to that space). On
ARM64, instead of prespill we can instead just allocate the appropriate stack
space, and use `str` or `stp` to save the incoming register arguments to the
reserved space.

To support GC "return address hijacking", we need to, for all functions, save
the return address to the stack in the prolog, and load it from the stack in the
epilog before returning. We must do this so the VM can change the return address
stored on the stack to cause the function to return to a special location to
support suspension.

Below are some sample frame layouts. In these examples, `#localsz` is the byte
size of the locals/temps area (everything except callee-saved registers and the
outgoing argument space, but including space to save FP and SP), `#outsz` is the
outgoing stack parameter size, and `#framesz` is the size of the entire stack
(meaning `#localsz` + `#outsz` + callee-saved register size, but not including
any alloca size).

Note that in these frame layouts, the saved `<fp,lr>` pair is not contiguous
with the rest of the callee-saved registers. This is because for chained
functions, the frame pointer must point at the saved frame pointer. Also, if we
are to use the positive immediate offset addressing modes, we need the frame
pointer to be lowest on the stack. In addition, we want the callee-saved
registers to be "far away", especially for large frames where an immediate
offset addressing mode won’t be able to reach them, as we want locals to be
closer than the callee-saved registers.

To maintain 16 byte stack alignment, we may need to add alignment padding bytes.
Ideally we design the frame such that we only need at most 15 alignment bytes.
Since our frame objects are minimally 4 bytes (or maybe even 8 bytes?) in size,
we should only need maximally 12 (or 8?) alignment bytes. Note that every time
the stack pointer is changed, it needs to be by 16 bytes, so every time we
adjust the stack might require alignment. (Technically, it might be the case
that you can change the stack pointer by values not a multiple of 16, but you
certainly can’t load or store from non-16-byte-aligned SP values. Also, the
ARM64 unwind code `alloc_s` is 8 byte scaled, so it can only handle multiple of
8 byte changes to SP.) Note that ldp/stp can be given an 8-byte aligned address
when reading/writing 8-byte register pairs, even though the total data transfer
for the instruction is 16 bytes.

## 1. chained, `#framesz <= 512`, `#outsz = 0`

```
stp fp,lr,[sp,-#framesz]!       // pre-indexed, save <fp,lr> at bottom of frame
mov fp,sp                       // fp points to bottom of stack
stp r19,r20,[sp,#framesz - 96]  // save INT pair
stp d8,d9,[sp,#framesz - 80]    // save FP pair
stp r0,r1,[sp,#framesz - 64]    // home params (optional)
stp r2,r3,[sp,#framesz - 48]
stp r4,r5,[sp,#framesz - 32]
stp r6,r7,[sp,#framesz - 16]
```

8 instructions (for this set of registers saves, used in most examples given
here). There is a single SP adjustment, that is folded into the `<fp,lr>`
register pair store. Works with alloca. Frame access is via SP or FP.

We will use this for most frames with no outgoing stack arguments (which is
likely to be the 99% case, since we have 8 integer register arguments and 8
floating-point register arguments).

Here is a similar example, but with an odd number of saved registers:

```
stp fp,lr,[sp,-#framesz]!       // pre-indexed, save <fp,lr> at bottom of frame
mov fp,sp                       // fp points to bottom of stack
stp r19,r20,[sp,#framesz - 24]  // save INT pair
str r21,[sp,#framesz - 8]       // save INT reg
```

Note that the saved registers are "packed" against the "caller SP" value (that
is, they are at the "top" of the downward-growing stack). Any alignment is lower
than the callee-saved registers.

For leaf functions, we don't need to save the callee-save registers, so we will
have, for chained function (such as functions with alloca):

```
stp fp,lr,[sp,-#framesz]!       // pre-indexed, save <fp,lr> at bottom of frame
mov fp,sp                       // fp points to bottom of stack
```

## 2. chained, `#framesz - 16 <= 512`, `#outsz != 0`

```
sub sp,sp,#framesz
stp fp,lr,[sp,#outsz]           // pre-indexed, save <fp,lr>
add fp,sp,#outsz                // fp points to bottom of local area
stp r19,r20,[sp,#framez - 96]   // save INT pair
stp d8,d9,[sp,#framesz - 80]    // save FP pair
stp r0,r1,[sp,#framesz - 64]    // home params (optional)
stp r2,r3,[sp,#framesz - 48]
stp r4,r5,[sp,#framesz - 32]
stp r6,r7,[sp,#framesz - 16]
```

9 instructions. There is a single SP adjustment. It isn’t folded into the
`<fp,lr>` register pair store because the SP adjustment points the new SP at the
outgoing argument space, and the `<fp,lr>` pair needs to be stored above that.
Works with alloca. Frame access is via SP or FP.

We will use this for most non-leaf frames with outgoing argument stack space.

As for #1, if there is an odd number of callee-save registers, they can easily
be put adjacent to the caller SP (at the "top" of the stack), so any alignment
bytes will be in the locals area.

## 3. chained, `(#framesz - #outsz) <= 512`, `#outsz != 0`.

Different from #2, as `#framesz` is too big. Might be useful for `#framesz >
512` but `(#framesz - #outsz) <= 512`.

```
stp fp,lr,[sp,-(#localsz + 96)]!    // pre-indexed, save <fp,lr> above outgoing argument space
mov fp,sp                           // fp points to bottom of stack
stp r19,r20,[sp,#localsz + 80]      // save INT pair
stp d8,d9,[sp,#localsz + 64]        // save FP pair
stp r0,r1,[sp,#localsz + 48]        // home params (optional)
stp r2,r3,[sp,#localsz + 32]
stp r4,r5,[sp,#localsz + 16]
stp r6,r7,[sp,#localsz]
sub sp,sp,#outsz
```

9 instructions. There are 2 SP adjustments. Works with alloca. Frame access is
via SP or FP.

We will not use this.

## 4. chained, `#localsz <= 512`

```
stp r19,r20,[sp,#-96]!      // pre-indexed, save incoming 1st FP/INT pair
stp d8,d9,[sp,#16]          // save incoming floating-point regs (optional)
stp r0,r1,[sp,#32]          // home params (optional)
stp r2,r3,[sp,#48]
stp r4,r5,[sp,#64]
stp r6,r7,[sp,#80]
stp fp,lr,[sp,-#localsz]!   // save <fp,lr> at bottom of local area
mov fp,sp                   // fp points to bottom of local area
sub sp,sp,#outsz            // if #outsz != 0
```

9 instructions. There are 3 SP adjustments: to set SP for saving callee-saved
registers, for allocating the local space (and storing `<fp,lr>`), and for
allocating the outgoing argument space. Works with alloca. Frame access is via
SP or FP.

We likely will not use this. Instead, we will use #2 or #5/#6.

## 5. chained, `#localsz > 512`, `#outsz <= 512`.

Another case with an unlikely mix of sizes.

```
stp r19,r20,[sp,#-96]!      // pre-indexed, save incoming 1st FP/INT pair
stp d8,d9,[sp,#16]          // save in FP regs (optional)
stp r0,r1,[sp,#32]          // home params (optional)
stp r2,r3,[sp,#48]
stp r4,r5,[sp,#64]
stp r6,r7,[sp,#80]
sub sp,sp,#localsz+#outsz   // allocate remaining frame
stp fp,lr,[sp,#outsz]       // save <fp,lr> at bottom of local area
add fp,sp,#outsz            // fp points to the bottom of local area
```

9 instructions. There are 2 SP adjustments. Works with alloca. Frame access is
via SP or FP.

We will use this.

To handle an odd number of callee-saved registers with this layout, we would
need to insert alignment bytes higher in the stack. E.g.:

```
str r19,[sp,#-16]!          // pre-indexed, save incoming 1st INT reg
sub sp,sp,#localsz + #outsz // allocate remaining frame
stp fp,lr,[sp,#outsz]       // save <fp,lr> at bottom of local area
add fp,sp,#outsz            // fp points to the bottom of local area
```

This is not ideal, since if `#localsz + #outsz` is not 16 byte aligned, it would
need to be padded, and we would end up with two different paddings that might
not be necessary. An alternative would be:

```
sub sp,sp,#16
str r19,[sp,#8]                 // Save register at the top
sub sp,sp,#localsz + #outsz     // allocate remaining frame. Note that there are 8 bytes of padding from the first "sub sp" that can be subtracted from "#localsz + #outsz" before padding them up to 16.
stp fp,lr,[sp,#outsz]           // save <fp,lr> at bottom of local area
add fp,sp,#outsz                // fp points to the bottom of local area
```

## 6. chained, `#localsz > 512`, `#outsz > 512`

The most general case. It is a simple generalization of #5. `sub sp` (or a pair
of `sub sp` for really large sizes) is used for both sizes that might overflow
the pre-indexed addressing mode offset limit.

```
stp r19,r20,[sp,#-96]!      // pre-indexed, save incoming 1st FP/INT pair
stp d8,d9,[sp,#16]          // save in FP regs (optional)
stp r0,r1,[sp,#32]          // home params (optional)
stp r2,r3,[sp,#48]
stp r4,r5,[sp,#64]
stp r6,r7,[sp,#80]
sub sp,sp,#localsz          // allocate locals space
stp fp,lr,[sp]              // save <fp,lr> at bottom of local area
mov fp,sp                   // fp points to the bottom of local area
sub sp,sp,#outsz            // allocate outgoing argument space
```

10 instructions. There are 3 SP adjustments. Works with alloca. Frame access is
via SP or FP.

We will use this.

## 7. chained, any size frame, but no alloca.

```
stp fp,lr,[sp,#-112]!       // pre-indexed, save <fp,lr>
mov fp,sp                   // fp points to top of local area
stp r19,r20,[sp,#16]        // save INT pair
stp d8,d9,[sp,#32]          // save FP pair
stp r0,r1,[sp,#48]          // home params (optional)
stp r2,r3,[sp,#64]
stp r4,r5,[sp,#80]
stp r6,r7,[sp,#96]
sub sp,sp,#framesz - 112    // allocate the remaining local area
```

9 instructions. There are 2 SP adjustments. The frame pointer FP points to the
top of the local area, which means this is not suitable for frames with alloca.
All frame access will be SP-relative. #1 and #2 are better for small frames, or
with alloca.

## 8. Unchained. No alloca.

```
stp r19,r20,[sp,#-80]!      // pre-indexed, save incoming 1st FP/INT pair
stp r21,r22,[sp,#16]        // ...
stp r23,lr,[sp,#32]         // save last Int reg and lr
stp d8,d9,[sp,#48]          // save FP pair (optional)
stp d10,d11,[sp,#64]        // ...
sub sp,sp,#framesz-80       // allocate the remaining local area

Or, with even number saved Int registers. Note that here we leave 8 bytes of
padding at the highest address in the frame. We might choose to use a different
format, to put the padding in the locals area, where it might be absorbed by the
locals.

stp r19,r20,[sp,-80]!       // pre-indexed, save in 1st FP/INT reg-pair
stp r21,r22,[sp,16]         // ...
str lr,[sp, 32]             // save lr
stp d8,d9,[sp, 40]          // save FP reg-pair (optional)
stp d10,d11,[sp,56]         // ...
sub sp,#framesz - 80        // allocate the remaining local area
```

All locals are accessed based on SP. FP points to the previous frame.

For optimization purpose, FP can be put at any position in locals area to
provide a better coverage for "reg-pair" and pre-/post-indexed offset addressing
mode. Locals below frame pointers can be accessed based on SP.

## 9. The minimal leaf frame

```
str lr,[sp,#-16]!           // pre-indexed, save lr, align stack to 16
... function body ...
ldr lr,[sp],#16             // epilog: reverse prolog, load return address
ret lr
```

Note that in this case, there is 8 bytes of alignment above the save of LR.
