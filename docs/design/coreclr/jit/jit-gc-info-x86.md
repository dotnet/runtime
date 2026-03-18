GC Information Encoding for x86
============================

**This is an old document. It has been converted to Markdown, but has not been otherwise updated. It may
not match existing code and behavior. However, the x86 GC encoding almost never changes, so it is expected
it still closely matches the existing code, and might be useful.**

# GC Information Encoding

***Important Note***: the .NET runtime system *absolutely requires*
that a garbage collection can be performed whenever a method exits.
Thus there must be sufficient information available at *all* call sites to
guarantee that a garbage collection can occur.
**Rationale**: This allows code that does not allocate memory to
be compiled without directly testing for the need to garbage collect. This requires,
however, that the stack can be walked by the runtime system and that the information
needed to find all live GC pointers be available at the point of a call.

***Note***: The garbage collector also relies on information
provided in `vtable`s to locate pointers that are stored in
instance variables and arrays. Static variables (i.e. class variables) are also located
through a separate mechanism. None of this information is described here.

# Overview of the Method GC Information

The Method GC Information is divided into five separate regions, packed consecutively
in memory (there is no padding to provide alignment). Each of these regions is briefly
described here. The details of the encoding are provided in separate sections below. When
compiled with `VERIFY_GC_TABLES` many of these sections begin
with a 16 bit tag as shown in parentheses after the name. These tags are only for debugging
the GC encoder/decoder and are never used at runtime or even ordinary debugging builds.

## 1. Method Header Information (tag `0xFEEF`)

General information about the method (size of code, size and number of epilogs and
prologs, etc.)

## 2. Method Epilog Table (no tag)

Location of epilog (method exit) code.

## 3. Untracked Locals Table (tag `0xBEEF`)

This table specifies which slots in the fixed-length portion of this method's stack frame
contain pointers to be traced by the GC, as well as the particular kind of pointer, either
an object reference or [interior](#Dfn-interior-pointers) (i.e. a pointer that
may point to the interior of a garbage collectable object) stored at that location. A
`this` pointer is required to be tracked and thus cannot
appear in this table. Additionally each untracked local can be marked as
[pinned](#Dfn-pinned-pointer). (i.e. a pointer to a location that must be
considered pinned for the purposes of a garbage collection) Any frame slot mentioned here
is considered live for GC purposes during the *entire* lifetime of the frame so it must be
initialized in the method prolog unless the frame slot correponds to an incoming argument
and it should be explicitly cleared when it reaches the end of its lifetime (if the compiler
knows where the lifetime ends prior to the exit from the method).

**Rationale**: The term [untracked](#Dfn-untracked)
means that there is no lifetime information for these items, even though they are known to
contain GC pointers. The JITter currently has a hard-coded limit of 64 items whose lifetime
it can track and any additional pointers in the stack frame will be untracked. In addition
it is also useful to mark GC references for infrequently accessed incoming arguments that
reside in the [pushed argument area](#Dfn-pushed-argument-area) as untracked.

## 4. Stack Lifetime Table (tag `0xCAFE`)

This table provides liveness information, based on offset in the locals area and current
PC within the method code. The table specifies which slots in the fixed-length portion
of this method's stack frame contain pointers to be traced by the GC, their lifetimes (i,e.
their starting and ending offsets) as well as the particular kind of pointer, either an
object reference, a `this` pointer or an
[interior](#Dfn-interior-pointer) (i.e. a pointer that may point to the
interior of a garbage collectable object) stored at that location. Tracked locals cannot
be marked as [pinned](#Dfn-pinned-pointer). Items tracked here need not
be initialized or cleared at runtime (unlike items mentioned in the Untracked Locals
Table), and they may contain non-pointers during the portions of code when they are not
marked as live. As mentioned above, there is a tradeoff between compile-time/space and
execution-time/space that determines how many stack slots are tracked in this table vs.
listed in the Untracked Locals table.

## 5. GC liveness and stack depth tracking table (tag `0xBABE`)

This table provides, at specific code addresses within the method, the GC pointer
liveness of registers, and the GC pointer liveness of the local stack frame. In addition
for an [EBP-less method](#Dfn-EBP-less-Method) this table also provides
information on tracking changes to [ESP](#Dfn-ESP-Register).

# General Comments on Encoding

There are three encoding techniques that are used repeatedly. These are referred
to as **Unsigned**, **Signed**, and **UDelta** encodings. They work as follows:

**Unsigned**: A sequence of bytes where all but the last byte have
the 0x80 bit set. The bytes are stored most- to least-significant.
One byte can thus encode the numbers 0 to 127 (0x00 to 0x7F);
two bytes can encode 128 (0x81 0x00) to 16383 (0xFF 0x7F); and so forth.
Five bytes would be the maximum number of bytes needed to encode the largest 32-bit unsigned values.

**Signed**: A sequence of bytes where all but the last byte have
the 0x80 bit set. The first byte uses the 0x40 bit to encode the sign. The bytes are
stored most- to least-significant. After accumulating the unsigned values (6 bits from the
first byte and 7 bits from each succeeding byte) the result is two's complemented if the
sign bit was set. One byte can thus encode the numbers -63 (0x7f) to 63 (0x3F);
two bytes can encode -8191 (0xFF 0x7F) to 8191 (0xBF 0x7F); and so forth.
Five bytes would be the maximum number of bytes needed to encode the largest magnitude 32-bit signed values.

**UDelta**: A series of Unsigned encodings, where each is an
increment to the sum of all of the previous values. This is used to encode a stream of
ever increasing values (such as offsets in the code segment or a stack frame).

All other encodings are of a fixed size, as in "16 bit signed" or "32 bit unsigned".

# Stack Frame Format

The code manager supported by the encodings described here makes several assumptions
about the runtime stack. The following diagram shows the layout of the stack frame for
code generated by the x86 JITter.

```
               ESP frames                              EBP frames

       |                       |               |                       |
       |-----------------------|               |-----------------------|
       |       incoming        |               |       incoming        |
       |       arguments       |               |       arguments       |
       +=======================+               +=======================+
       |       Temps           |               |    incoming EBP       |
       |-----------------------|     EBP ----->|-----------------------|
       |       locspace        |               |   security object     |
       |-----------------------|               |-----------------------|
       |                       |               |       locspace        |
       |       Variables       |               |-----------------------|
       |                       |               |     ParamTypeArg      |
       |                       |               |-----------------------|
       |                       |               |  Last-executed-filter |
       |                       |               |-----------------------|
       |                       |               |                       |
       |                       |               |                       |
       |-----------------------|               ~      Shadow SPs       ~
       |Callee saved registers |               |                       |
       |-----------------------|               |                       |
       |   Arguments for the   |               |-----------------------|
       ~    next function      ~ <----- ESP    |                       |
       |                       |               |                       |
       |       |               |               ~      Variables        ~
       |       | Stack grows   |               |                       |
               | downward                      |                       |
               V                               |                       |
                                               ~-----------------------|
                                               |       Temps           |
                                               |-----------------------|
                                               |Callee saved registers |
                                               |-----------------------|
                                               |       localloc        |
                                               |-----------------------|
                                               |   Arguments for the   |
                                               |    next function      ~
                                               |                       |
                                               |       |               |
                                               |       | Stack grows   |
                                                       | downward
                                                       V
```

Here is a diagram of how the GC info relates to the various parts of the stack
frame: **missing**.

The stack consists of frames that are connected together either by explicitly stored
dynamic links (saved previous frame pointer) or by information in the method header
described here. The .NET runtime system requires the ability to walk back the stack at any
time that a fault may occur (i.e. at all times).

<a name="Dfn-Constructing-a-stack-frame"></a>

A stack frame is created in the following order (see diagram above):

In the callers code up to and including the call instruction:

- push the arguments to the method.
  This is called the [pushed argument area](#Dfn-pushed-argument-area).
  The size of this pushed argument area is recorded in the `argCount`
  field of the Method Header Information for the called method.
  **Note**: that the first two arguments are passed in registers and thus
  do not get pushed onto the stack and are thus not part of the pushed argument area.
- push the return address (This step executes automatically as part of the call instruction)

In the method prolog for the called method:

- push the [EBP](#Dfn-EBP-Register) register and setup a new EBP equal to the
  current value of ESP. This step only takes place if the current method uses the EBP
  register to point to the base of this frame. (i.e. `ebpFrame=1`)
- if `doubleAlign=1` is specified then the
  [local variable area](#Dfn-local-variable-area) requires quad word (8-byte) alignment,
  a pad word is pushed here if the stack is not currently quad word (8-byte) aligned.
- allocate the [local variable area](#Dfn-local-variable-area) for the current method; its
  size is available from the method header (i.e. `frameSize`).
  The allocation takes place by subtracting the size of the local area from [ESP](#Dfn-ESP-Register).
  This area will hold both the user visible local variables and any temporary locations needed
  by the compiler. The critical issue is that its size is known at compile time and that
  the space is allocated for the entire duration of the method activation.
- save any [callee saves](#Dfn-callee-saves) registers that will be modified
  by the method code; they are saved in a known order and which ones are saved by this
  method is specified in the method header. At this point the fixed portion of the stack
  frame (the part that is always present while in the main body of the method code) is complete.
- initialization of the security object (if present) and the initialization of any
  untracked locals to NULL; if there are a large numbers of untracked locals to be initalized,
  the JITter may choose to zero initialize the entire local frame instead of initializing
  each untracked local slot.
- if `localloc=1` is specified then the current value of ESP
  which is the initial default [ESP](#Dfn-ESP-Register) for the method is saved
  into the first local slot.

Offsets used at runtime into the [local variable area](#Dfn-local-variable-area)
and the [pushed argument area](#Dfn-pushed-argument-area) are handled
differently depending on whether the method uses [EBP](#Dfn-EBP-Register) as a
frame pointer and whether a frame has locals that require quad word (8-byte) alignment at
run time. For frames with an EBP but no double alignment constraint, offsets are relative
to the EBP, which always points at the saved value of the EBP; negative offsets refer to
local variables or temporary values while positive offsets refer to this method's arguments.
If the method does not use an EBP, all offsets will be from the method's initial default
[ESP](#Dfn-ESP-Register) and will necessarily be positive. If a frame requires
quad word (8-byte) alignment, it will use positive offsets from EBP (which is not
required to be quad word aligned) to reference the arguments and positive offsets from
ESP (which will be quad word aligned) to reference locals and temporaries.

**Implementation Note**: The runtime system and this GC encoding do
not currently support having GC pointers in the variable length portion of the stack frame.
This means that memory allocated using `_alloca` in C/C++
cannot contain GC pointers. This will be addressed in the future.

# Description of Method Header Information

The Method Header Information section of the Method GC Information encodes two things: the
number of bytes in the code for the method and the information in the C++ struct
`InfoHdr` as defined in inc/GCInfo.h (**warning**:
this is the format used internally by the .NET runtime system, it is *not</em> the file
format; that's described later):

```
#pragma pack(push, 1)

struct InfoHdr {
    unsigned char  prologSize;         // 0
    unsigned char  epilogSize;         // 1
    unsigned char  epilogCount    : 3; // 2 [0:2]
    unsigned char  epilogAtEnd    : 1; // 2 [3]
    unsigned char  ediSaved       : 1; // 2 [4]
    unsigned char  esiSaved       : 1; // 2 [5]
    unsigned char  ebxSaved       : 1; // 2 [6]
    unsigned char  ebpSaved       : 1; // 2 [7]
    unsigned char  ebpFrame       : 1; // 3 [0]
    unsigned char  interruptible  : 1; // 3 [1]
    unsigned char  doubleAlign    : 1; // 3 [2]
    unsigned char  security       : 1; // 3 [3]
    unsigned char  handlers       : 1; // 3 [4]
    unsigned char  localloc       : 1; // 3 [5]
    unsigned char  editNcontinue  : 1; // 3 [6]
    unsigned char  varargs        : 1; // 3 [7]
    unsigned short argCount;           // 4,5
    unsigned short frameSize;          // 6,7
    unsigned short untrackedCnt;       // 8,9
    unsigned short varPtrTableSize;    //10,11
                                      // 12 bytes total
    ...
};

#pragma pack(pop)
```

- **prologSize**: is the length, in bytes, of the method prolog (the entry code of the method up to the
  point where the contents of the fixed-size area (see introduction) is initialized and
  stable.
- **epilogSize**: is the length, in bytes, of the method epilog (the exit code of the method, during which
  the locals area is no longer stable).
- **epilogCount**: is the number of occurrences of the epilog code in the method.
    While a method has only one entry point, it may have multiple exits.
- **epilogAtEnd**: is a one-bit boolean. 1 indicates that there is a single epilog occurring at the end of
  the method code. 0 means that the Method Epilog Table specifies the location in the code
  of the epilogs.
- **ediSaved**: is a one-bit boolean specifying whether or not the EDI register is saved in the
  callee-saves area of the stack frame at entry to the method.
- **esiSaved**: is a one-bit boolean specifying whether or not the ESI register is saved in the
  callee-saves area of the stack frame at entry to the method.
- **ebxSaved**: is a one-bit boolean specifying whether or not the EBX register is saved in the
  callee-saves area of the stack frame at entry to the method.
- **ebpSaved**: is a one-bit boolean specifying whether or not the [EBP](#Dfn-EBP-Register)
  register is saved in the callee-saves area of the stack frame at entry to the method.
  If `ebpFrame=1` this bit is ignored and the EBP will
  always be saved immediately after the return address rather than in the callee-saves
  area of the stack frame.
- **ebpFrame**: is a one-bit boolean specifying whether or not, at runtime, the [EBP](#Dfn-EBP-Register)
  register will be dedicated for used as a frame pointer in the body of the method. When
  `ebpFrame=1` the method will be known as an
  [EBP-based method](#Dfn-EBP-Frame-or-EBP-based-Method). When
  `ebpFrame=0` the method will be known as an
  [EBP-less method](#Dfn-EBP-less-Method).
  The ramifications of this boolean are wide-ranging, since maintaining a reliable frame
  pointer requires additional execution code (larger prolog and epilog sequences and thus
  slower running time) but can reduce the amount of data in the Method GC Information tables
  needed to reliably track the active GC traceable pointers (less data space and faster
  garbage collection, stack unwinding, and exception handling).
- **interruptible**: is a one-bit boolean that is used to determine which of two encodings is used for
  information about the depth of the stack at runtime and the liveness of registers and
  values on the non-fixed length portion of the stack. The preferred
  representation is with `interruptible=0`
  ([non fully interruptible](#Dfn-Non-Fully-Interruptible-Method)) which
  indicates that the method is known to run for only a short interval (order of a few
  hundred instructions) before it either exits or makes a method call. The other
  representation, `interruptible=1`
  ([fully interruptible](#Dfn-Fully-Interruptible-Method)),
  is used when the method can't make this guarantee. In that case,
  additional information is made available by the compiler to allow the garbage collector to
  run while the method is active. ***Important Note***: the
  .NET runtime system *absolutely requires* that a garbage collection can be
  performed whenever a method exits. Thus there must be sufficient information
  available at *all* call sites to guarantee that a garbage collection can occur.
  Also, the .NET runtime system *absolutely requires* that the stack
  can be unwound even if a garbage collection cannot be allowed to occur.
- **doubleAlign**: is a one-bit boolean specifying whether the local variables require alignment on
  quad word (8-byte) boundaries. If this bit is set, the compiler is responsible for emitting a
  prolog that aligns the stack pointer at run time by conditionally inserting either 0 or 4
  bytes of padding between the stored EBP and the local variables. The JITter requires
  that any double aligned method use an [EBP frame](#Dfn-EBP-Frame-or-EBP-based-Method)
  so that locals can be referenced using the aligned [ESP](#Dfn-ESP-Register) while
  arguments are referenced using the unaligned [EBP](#Dfn-EBP-Register).
  The name `doubleAlign` is perhaphs a bit confusing since
  it actually requires a quad word or 8-byte alignment for ESP. Its purpose however is to
  allow the proper alignment for the floating point type `double`
  thus the name `doubleAlign`.
- **security**: is a one-bit boolean specifying whether the stack frame includes a security object.
  If present, the object will be located in the
  [local variable area](#Dfn-local-variable-area),
  immediately after the area actually used by the compiler. The security object is created and
  stored by the runtime system as needed; compiled code should never reference this location
  in any way (and the space for the security object is not included in the  frameSize,
  below) *except* that the method prolog must initialize the location to NULL (since
  it cannot know when it is live).
- **handlers**: is a one-bit boolean specifying whether the method has any exception
  handlers. When the method has exception handlers the stack frame includes hidden
  slots for shadow SPs.
- **localloc**: is a one-bit boolean specifying whether the method has a variable sized stack frame.
  For example any usage of the C/C++ `_alloca` routine
  in the method would require the method to allocate a variable sized stack frame.
  In such cases the stack frame will include a slot for the initial default
  [ESP](#Dfn-ESP-Register) for the method. **Note**:
  By convention the JITter and the Code Manager use the first local slot to save the
  value of the initial default ESP.
- **editNcontinue**: is a one-bit boolean specifying whether the method was compiled for EnC
  This also requires that the method have an
  [EBP frame](#Dfn-EBP-Frame-or-EBP-based-Method).
- **varargs**: is a one-bit boolean specifying whether the method is a vararg method.
  Reporting of GC arguments of such methods is specially handled.
- **argCount**: is the length (in units of 4 bytes) that is expected to be allocated and initialized by
  the caller of this method. (i.e. the size of the [pushed argument area](#Dfn-pushed-argument-area))
  **Note**: Since the first two arguments are normally passed in registers this value
  is normally either zero or two less than the actual number of arguments.
- **frameSize**: is the size (in units of 4 bytes) of the area labeled "Local Variables (incl.
  compile temps)" in the diagram above, *except* it does *not* include
  the 4 bytes of space used for the security object (if present).
- **untrackedCnt**: is the total number of entries in the Untracked Locals Table.
- **varPtrTableSize**: is the total number of entries in the Stack Lifetime Table.

# Encoding of the Method Header Information

The actual encoding of the Method Header Information is:

1. An Unsigned length (in bytes) of the total size of all the method code, including prolog and epilogs.
2. InfoHdr, encoded as a series of bytes all but the last of which have the 0x80 bit set.

Rather than store the entire 12 byte InfoHdr structure, it is encoded by using a table
(in the file `inc/GCDecoder.cpp`) of the 128 most commonly
encountered InfoHdr structures based on statistics from an existing code base.

The first byte of the encoding has as its low 7 bits an index into this table of common
structures. If the 0x80 bit is set, then this is followed by an arbitrary number
(terminated by a byte without the 0x80 bit set) of fixup bytes.

The bytes after the first are interpreted (after stripping the 0x80 bit) as follows (see `inc/GCInfo.h`):

| Value | Description |
| --- | --- |
| `0 - 7 (0x00-0x07)` | Set **frameSize** to this value |
| `8 -16 (0x08-0x10)` | Set **argCount** to `(value-8)` |
| `17-33 (0x11-0x21)` | Set **prologSize** to `(value-17)` |
| `34-44 (0x22-0x2C)` | Set **epilogSize** to `(value-34)` |
| `45-54 (0x2D-0x36)` | Set both **epilogCount** and **epilogAtEnd** epilogAtEnd is true if and only if `(value-45)` is odd epilogCount is the quotient of `(value-45) divided by 2` |
| `55-58 (0x37-0x3A)` | Set **untrackedCnt** to `(value-55)` |
| `59 (0x3B)` | Flip **ediSaved** (i.e. logical NOT) |
| `60 (0x3C)` | Flip **esiSaved** (i.e. logical NOT) |
| `61 (0x3D)` | Flip **ebxSaved** (i.e. logical NOT) |
| `62 (0x3E)` | Flip **ebpSaved** (i.e. logical NOT) |
| `63 (0x3F)` | Flip **ebpFrame** (i.e. logical NOT) |
| `64 (0x40)` | Flip **interruptible** (i.e. logical NOT) |
| `65 (0x41)` | Flip **doubleAlign** (i.e. logical NOT) |
| `66 (0x42)` | Flip b (i.e. logical NOT) |
| `67 (0x43)` | Flip all 16 bits in **varPtrTableSize**. **Note**: this allows it to be set to "0xFFFF" and back to "0x0000" |
| `68 (0x44)` | Set **untrackedCnt** to 0xFFFF (i.e. "too big") |
| `69-79 (0x45-0x4F)` | illegal encodings |
| `80-95 (0x50-0x5F)` | Multiply **frameSize** by 16 and add in the low 4 bits of value. |
| `96-111 (0x60-0x6F)` | Multiply **argCount** by 16 and add in the low 4 bits of value. |
| `112-119 (0x70-0x77)` | Multiply **prologSize** by 8 and add in the low 3 bits of value. |
| `120-127 (0x78-0x7F)` | Multiply **epilogSize** by 8 and add in the low 3 bits of value. |
| `128-255 (0x80-0xFF)` | Remember 0x80 just means there are more bytes to follow, so strip it off and look above! |

3. If **untrackedCnt** is now 0xFFFF, then there is an Unsigned specifying
   its correct value. As shown above, this is currently limited to 16 bits unsigned (64K).
4. If **varPtrTableSize** is now 0xFFFF, then there is an Unsigned
   specifying its correct value. As shown above, this is currently limited to 16 bits
   unsigned (64K).

***Implementation Restriction Note***:
This encoding requires that the code sequence at all exits from a method have the same
length and that there be no more than 4 such exits.

# Method Epilog Table

The Method Header Information value **epilogCount** specifies the number of
entries in this table. The entries are UDelta encodings of the byte offsets in the method
code at which epilog (method exit) code begins. The length of each of these is specified
by the **epilogSize** value in the Method Header Information.

This information is used to decide on GC safety (generally it isn't safe to GC while in an
epilog) and to unwind the stack (the epilog sequence is disassembled and instructions that
modify the ESP are simulated).

# Untracked Locals Table

The number of entries is specified in the Method Header Information as **untrackedCnt**.
Each entry is a single Signed. The values of these entries encode offsets within the
[pushed argument area](#Dfn-pushed-argument-area) or the
[local variable area](#Dfn-local-variable-area) of the stack.
Since locals are required to be a multiple of 4 bytes long, the bottom two bits of the value
are used to encode additional information about the data stored at the true offset (which is
the encoded offset with the bottom two bits cleared). The bottom two bits are interpreted as
follows:

- 00: a normal object reference
- 01: an interior pointer (i.e. a pointer that may point to the interior of a garbage collectible object)
- 02: a pinned object reference (i.e. the object cannot be moved during an garbage collection operation)
- 03: a pinned interior pointer (i.e. the combination of the two special cases)

# Stack Lifetime Table

The size of this table is specified in the Method Header Information by the value of
**varPtrTableSize**. This table is similar to the Untracked Locals Table
in that each entry encode offsets within the
[pushed argument area](#Dfn-pushed-argument-area) or the
[local variable area](#Dfn-local-variable-area) of the stack.
However additional lifetime information for each entry is provided,

Each entry consists of three parts:

## Part 1

An Unsigned specifying the offset within the argument or local variable area of the stack.
Since the offsets are required to be a multiple of 4 bytes long, the bottom two bits of the value are
used to encode additional information about the data stored at the true offset (which is the encoded
offset with the bottom two bits cleared). The bottom two bits are interpreted as follows:

- 00: a normal object reference
- 01: an interior pointer (i.e. a pointer that may point to the interior of a garbage collectible object)
- 02: the `this` pointer for the method
- 03: an interior pointer which also is the `this` pointer for the method

## Parts 2 and 3

A pair of UDeltas specifying the region of code for which this variable is
live (i.e. contains useful data). The first UDelta specifies the point where it becomes
live and the second specifies where it ceases to be live. The entire table is sorted by
increasing address in the code block. The first UDelta ("birth") is relative to
the birth address of the previous entry. The second UDelta ("death") is relative
to the birth specified in this entry.

For both the Untracked Local Table and the Stack Lifetime Table the offsets within the
[pushed argument area](#Dfn-pushed-argument-area) or the
[local variable area](#Dfn-local-variable-area) of the stack are interpreted
as follows:

- **In an EBP Frame**.
The negative of the offset from the runtime EBP (i.e. while the table has an unsigned
number it refers to a negative offset from the EBP - a a slot in the locals area).
Thus for an EBP frame is is not possible refer to the pushed arguments area.

- **In an EBP-less Frame or a double-aligned frame**.
Offset from the initial default ESP for the method (i.e, the lowest address of the
callee-saves area of the stack frame, or the value of ESP upon exiting the prolog)
For non double-aligned frames the pushed argument area can be specified by using
larger offsets. (i.e. Offsets larger than frameSize + the size of the callee-saves area)
For double aligned frames it is not possible to refer into the pushed argument area

An encoded value of 0x11 refers to the local at offset encoded by 0x10 (16 bytes)
and specifies that it is an [interior](#Dfn-interior-pointers) pointer
In an EBP frame this would be an offset of -16 bytes from the EBP. For an EBP-less frame
this would be an offset of +16 bytes from ESP.

# Stack depth and Register/Argument GC Table

This table is by far the most complicated structure in the Method GC Information. It is
used to encode the locations of the live GC pointers for particular regions of
the method code. It also may need to record every change made to the ESP register.
Two fundamentally different encodings are used based on whether the
Method Header Information indicates that the method is fully interruptible
(i.e. whether the `interruptible` bit is set).

To recap, this data structure is used to determine, at particular locations in the
method's code, how large the variable-length portion of the stack frame is and where in
that frame there are live pointers to be traced by the garbage collector. However,
this information need not be completely accurate for all addresses in the code:

1. [Fully interruptible](#Dfn-Fully-Interruptible-Method) methods must be
able to provide this information accurately for every point in the code except the
prolog and epilog.

2. [Non fully interruptible](#Dfn-Non-Fully-Interruptible-Method) methods
need only have accurate information available for points where the method calls another
method (through any calling mechanism).

3. A method that does not set up [EBP](#Dfn-EBP-Register) as a frame pointer
and instead references all it's locals and arguments using the [ESP](#Dfn-ESP-Register)
register is known as an [EBP-less method](#Dfn-EBP-less-Method)).
It must provide additional information about which locations in the code modify the
ESP register. This information is required to be accurate for every point in the code
expect the prolog and the epilog. This information is used in the unwind process to reconstruct
the initial default ESP for the method; that is the value of ESP upon exiting the prolog and
entering the main body of the method. All of the frame offsets in the Method GC Information
are provided relative to this initial default ESP.

***Rationale***: Most methods either run for a fairly short period of
time before returning a value or have only short periods of time between making method
calls. Thus, if it is safe to garbage collect at all call sites, the garbage collector
will not have long to wait if the return point of a method is hijacked by modifying the
return PC information on the stack to enter the garbage collector rather than the normal
running code. Methods for which this guarantee cannot be made (loops of unknown repetition
size with no calls) must have an alternate mechanism for allowing GC initiation. The JITter
enables this by making such methods fully interruptible.

***Note***: Methods that are marked as fully interruptible
require considerably more information in the Method GC Information table to express the
full liveness information at every point in the method. This will have a negative impact
on code size since the size of the Method GC Information can be quite large and really should
be considered as part of the managed code size for the method. Additionally the garbage
collector and stack walk will execute slower due to all of the extra information that it
must process.

There are three different encodings used:

1. Methods that are *not* fully interruptible and have dedicated the EBP register
as the frame pointer at runtime are called [EBP-based methods](#Dfn-EBP-Frame-or-EBP-based-Method).
The GC liveness information is only  available at call sites. This is the most space-efficient
GC encoding format, but it is not the fastest in terms of execution speed, since extra work must be
performed in the prolog and epilog code to setup and restore the EBP frame pointer.

2. Methods that are *not* fully interruptible and have *not* dedicated the EBP register
as a frame pointer at runtime are called [EBP-less methods](#Dfn-EBP-less-Method).
Like the EBP-based methods described above, the GC liveness information is only available at
call sites. However additional information for tracking changes to the [ESP](#Dfn-ESP-Register)
register must be maintained in the Method GC Information so that the return address
(stored on the stack) can be found and for translating the ESP-based offsets into the proper
addresses needed by the runtime. Methods of this type execute the fastest and are the preferred
output format for the JITter. The Method GC Information is necessarily slightly larger than
EBP-based methods to support the required ESP tracking.

3. Methods that are marked by the JITter as *fully interruptible* are called
[Fully Interruptible methods](#Dfn-Fully-Interruptible-Method).
Unlike the two alternatives described above, fully interruptible methods provide complete
GC liveness information for all points in the method. The must providing liveness register
information for all registers, including the the [scratch registers](#Dfn-caller-saves)
(EAX, ECX, EDX). Fully interruptible methods can either dedicate the EBP register as a frame
pointer or not. If they do *not* dedicate the EBP register as a frame pointer register
then the must also include additional information for tracking changes to the
[ESP](#Dfn-ESP-Register) register. Regardless of how the EBP register is used the
information that is required in the Method GC Information for a Fully Interruptible method can
be quite large, typically ten times larger than the other two encoding formats. Thus this is
the least efficient GC liveness encoding mechanism. It use is required, however, for certain
methods that may execute for an unbounded length of time due to a compute bound loop.

***Important Note***: While the actual values of the
[callee saves](#Dfn-callee-saves) registers are stored in a method's frame,
the information about whether these values are alive is available from the method that
*called* this method. In other words, this frame supplies a storage location
but it is the caller that knows what was in the register at the time of the call.
The Method Header Information specifies whether or not the register is saved in the frame,
but not whether it was a live GC pointer. Conversely, the calling method's Stack Depth
table states whether the register has a live GC pointer, but not whether it was stored in
the called method's stack frame.

## 1. EBP-Based Methods

This encoding tracks:

- liveness of 3 registers (EBX, ESI, and EDI)
- liveness of parameters that have been pushed on the stack in preparation for a
  subsequent call to a method (pending parameters), using offsets relative to the first
  pushed parameter
- liveness of parameters (as above) but where the value is an interior pointer rather than
  a pointer to the full object
- where the `this` pointer (pointer to the actual object instance) is stored

Since this is an EBP-based method, the EBP register always points to the current stack
frame and is not a live GC pointer.

The encoding is ordered by increasing code offset and uses delta coding
(i.e. `code delta` means offset from previously mentioned code offset).
The `pushed` `args`
`mask` is a single 32-bit quantity that encodes whether or not
each of the first 32 pushed parameters contains a live pointer (bit 0x1 corresponds to the
parameter at offset 0, bit 0x2 offset 4, bit 0x4 offset 8, etc.). When there are pointers
in parameters beyond the first 32, an alternate (larger) encoding is used.

**`this` pointer:** `0bsd0000`<br>
**b** indicates that register EBX holds `this`<br>
**s** indicates that register ESI holds `this`<br>
**b** indicates that register EDI holds `this`<br>
<br>
**tiny encoding:** `0bsdDDDD (DDDD != 0000)`<br>
requires code offset &lt; 16 (4-bits)<br>
requires no live pushed parameters<br>
**DDDD** is `code delta`<br>
**b** indicates that register EBX is a live pointer<br>
**s** indicates that register ESI is a live pointer<br>
**d** indicates that register EDI is a live pointer<br>
<br>
**small encoding:** `1DDDDDDD bsdAAAAA`<br>
requires code delta &lt; 121 (7-bits)<br>
requires pushed argmask &lt; 32 (5-bits)<br>
**DDDDDDD** is code delta (121-127 are used for larger encodings) <br>
**AAAAA** is the pushed args mask<br>
**b** indicates that register EBX is a live pointer<br>
**s** indicates that register ESI is a live pointer<br>
**d** indicates that register EDI is a live pointer<br>
<br>
**medium encoding:** `0xFD aaaaaaaa AAAAdddd bsdDDDDD`<br>
requires code delta &lt; 512 (9-bits)<br>
requires pushed argmask &lt; 4096 (12-bits)<br>
**DDDDD** is the upper 5-bits of the code delta<br>
**dddd** is the low 4-bits of the code delta<br>
**AAAA** is the upper 4-bits of the pushed arg mask<br>
**aaaaaaaa** is the low 8-bits of the pushed arg mask<br>
**b** indicates that register EBX is a live pointer<br>
**s** indicates that register ESI is a live pointer<br>
**d** indicates that register EDI is a live pointer<br>
<br>
**medium encoding with interior pointers:** `0xF9 DDDDDDDD bsdAAAAAA iiiIIIII`<br>
requires code delta &lt; 256 (8-bits)<br>
requires pushed argmask &lt; 32 (5-bits)<br>
**DDDDDDD** is the code delta<br>
**b** indicates that register EBX is a live pointer<br>
**s** indicates that register ESI is a live pointer<br>
**d** indicates that register EDI is a live pointer<br>
**AAAAA** is the pushed arg mask<br>
**iii** indicates that EBX, EDI, ESI are interior pointers<br>
**IIIII** indicates that bits is the arg mask are interior pointers<br>
<br>
**large encoding:**<br>
`0xFE [32-bit argMask] [24-bit code delta] [bsd] [5-bit high code delta]`<br>
requires code delta &lt; (29-bits)<br>
requires pushed argmask &lt; (32-bits)<br>
**b** indicates that register EBX is a live pointer<br>
**s** indicates that register ESI is a live pointer<br>
**d** indicates that register EDI is a live pointer<br>
<br>
**large encoding with interior pointers:**<br>
`0xFA [32-bit argMask][24-bit code delta][bsd][5-bit high code delta][24-bit iargMask][BSD][higher 5-bits of iargMask]`<br>
requires code delta &lt; (29-bits)<br>
requires pushed argmask &lt; 32-bits<br>
requires pushed iargmask &lt; 29-bits<br>
**b** indicates that register EBX is a live pointer<br>
**s** indicates that register ESI is a live pointer<br>
**d** indicates that register EDI is a live pointer<br>
**B** indicates that register EBX is an interior pointer<br>
**S** indicates that register ESI is an interior pointer<br>
**D** indicates that register EDI is an interior pointer<br>
<br>
**huge encoding:**<br>
This is the only encoding that supports more than 255 arguments<br>
`0xFB [0BSD0bsd][32-bit code delta][32-bit argTab count][32-bit argTab byte size][argOffs...]`<br>
The byte size of the table is provided to facilitate skipping over the encoded offsets.<br>
**B** indicates that register EBX is an interior pointer<br>
**S** indicates that register ESI is an interior pointer<br>
**D** indicates that register EDI is an interior pointer<br>
**b** indicates that register EBX is a live pointer<br>
**s** indicates that register ESI is a live pointer<br>
**d** indicates that register EDI is a live pointer<br>
<br>
**0xFF** indicates the end of the table.

***Implementation Note***: There is no encoding that supports more 32
arguments with interior pointers. These methods must be transformed either into
EBP-less methods or fully interruptible methods.

## 2. EBP-less Methods

For an [EBP-less method](#Dfn-EBP-less-Method) the encoding tracks:

- stack depth (so that initial default value of ESP can be reconstructed)
- liveness of 4 registers (EBX, ESI, EDI and EBP)
- liveness of parameters that have been pushed on the stack in preparation for a call to a
  method, using offsets relative to the first pushed parameter
- liveness of parameters (as above) but where the value is an interior pointer rather than
  a pointer to the full object

Liveness information is only provided at call sites. However the stack depth information
must be accurate at all code addresses where an exception can occur.

The encoding is basically a list (ordered by code offset) of information about what is
occurring regarding liveness and stack depth, with an associated code offset encoded as a
delta from the previously mentioned code offset. Items in brackets are always encoded as
Unsigned, allowing for a maximum of 32 bits.

<pre>
<strong>000DDDDD</strong> Push one item with 5-bit delta
<strong>00100000 [pushCount]</strong> Push "pushCount" items
<strong>0011xxxx</strong> Reserved
<strong>01000000</strong> <strong>[Delta]</strong>
         Skip Delta code bytes
<strong>0100DDDD</strong> Skip small (&lt; 16 bytes) code bytes
<strong>01CCDDDD</strong> Pop CC items (4 bytes each) with 4-bit delta
<strong>1PPPPPPP</strong> Call Pattern, P=[0..79], see below
<strong>1101pbsd</strong> <strong>DDCCCMMM</strong>
         Call RegMask=pbsd, ArgCnt=CCC,
              ArgMask=MMM Delta=callCommonDelta[DD]
<strong>1110pbsd[ArgCnt] [ArgMask]</strong>
         Call ArgCnt, RegMask=pbsd, ArgMask
<strong>11111000[PBSDpbsd][32-bit code delta][32-bit ArgCnt]</strong>
		<strong>[32-bit PndTabCnt][32-bit PndTab byte size][pndOffs...]</strong>
         Call ArgCnt, RegMask=PBSDpbsd,
	 Pending arguments table (used when 32 bit argMask wont do).
	 Table byte size facilitates skipping over the encoded offsets in table.
<strong>11110000 [IPtrMask]</strong>
<strong>         </strong>Interior Pointer Mask (see below)
<strong>11110100</strong> This pointer is in Register EDI
<strong>11110101</strong> This pointer is in Register ESI
<strong>11110110</strong> This pointer is in Register EBX
<strong>11110111</strong> This pointer is in Register EBP
<strong>11111111</strong> End of table
</pre>

The `call pattern` instruction uses 7 bits to encode a
choice of the 80 most common calling patterns. The expansion of these into the register
mask, code delta, argument mask and argument count is given in the file
`inc/GCDecoder.cpp`

The value of `callCommonDelta` is an index into the array of offsets (`codeCommonDelta[]`) given in the
file `inc/GCDecoder.cpp`

Bits in RegMask indicate the callee-saved register is a live pointer (p=EBP, b=EBX,
s=ESI, d=EDI). The capital letters indicate that the pointer in the register is an
interior pointer (P=EBP, B=EBX, S=ESI, D=EDI). The argument mask is a maximum of 32 bits
long and specifies which arguments have live GC pointers. If there are live GC
pointers beyond the 32nd argument, then the PendingArgsTable has to be used.

The `iptr` (interior pointer mask) encoding must
immediately precede a call encoding. It is used to indicate that a GC pointer specified in
the call is actually an interior pointer. The mask supplied to the iptr encoding is read
from the least significant bit to the most significant bit. (i.e the lowest bit is read
first); the low four bits represent the callee-saves registers and then subsequent bits
refer to parameters being passed. If an interior pointer must be passed beyond the 28th
argument, then the method must be made fully interruptible or EBP-based.

## 3. Fully Interruptible

This encoding tracks:

- liveness of 7 registers (EAX, EDX, ECX, EBX, ESI, EDI and EBP)
- liveness of parameters that have been pushed on the stack in preparation for a
  subsequent call to a method (pending parameters), using offsets relative to the first
  pushed parameter
- liveness of parameters (as above) but where the value is an interior pointer rather than
  a pointer to the full object
- Where the `this` pointer (pointer to the actual object
  instance) is stored
- and for ESP-based methods the stack depth (so that the initial default value of ESP can be reconstructed)

Accurate GC liveness information is provided at all locations in the method,
hence the term [Fully Interruptible](#Dfn-Fully-Interruptible-Method).
Addionally if the method header specifies an EBP-less frame then the stack depth information is
provided along with the liveness information.

The information is similar to the input to a state machine where one thing at a time can be changed.
However along with the one thing that is changing there is also an associated code offset encoded as a delta
from the previously mentioned code offset. If several things must be changed at the same code offset
then a sequence commands each changing one thing along with an associated code delta of zero is used.
The initial state is that none of the registers contain live GC pointers.

<pre>
<strong>00RRRDDD</strong> The specified register is not alive for GC [RRR != 100]
<strong>01RRRDDD</strong> The specified register is alive for GC [RRR != 100]
<strong>10110DDD</strong> Push a single 4 byte non-pointer on the stack
<strong>10SSSDDD</strong> Push a pointer onto the stack at offset SSS into the
         parameter area. Requires [SSS != 110] &amp;&amp; [SSS != 111]
<strong>11CCCDDD</strong> For EBP-less  frames, pop CCC items    off the stack.
	 For EBP-based frames, pop CCC pointers off the stack.
	 [CCC != 000] &amp;&amp; [CCC != 110] &amp;&amp; [CCC != 111]
<strong>11000DDD</strong> Skip DDD bytes in code space
<strong>11110BBB</strong> Skip 8*BBB bytes in code space
</pre>

Where:

<pre>
<strong>DDD</strong> code offset delta from previous entry (0-7)
<strong>BBB</strong> bigger delta 000=8, 001=16, 010=24, ..., 111=64
<strong>RRR</strong> register number (EAX=000, ECX=001, EDX=010, EBX=011,
    EBP=101, ESI=110, EDI=111), ESP=100 is reserved
<strong>SSS</strong> offset (in 4 byte units) in the parameter area where
    a pointer is to be stored
<strong>CCC</strong> number of 4 byte items being popped
</pre>

The following are the 'large' versions:

<pre>
<strong>10111000 [Delta]</strong>    Skip over Delta bytes in code space
<strong>11111000 [argPush] </strong> Push a pointer on the stack into offset
                    argPush (in 4 byte units) in the
                    variable sized area of the frame
<strong>11111001 [argPush] </strong> Push a non-pointer on the stack into offset
                    argPush (in 4 byte units) in the
                    variable sized area of the frame
<strong>11111100 [popCount]</strong> For EBP-less  frames, pop popCount items    off the stack.
		    For EBP-based frames, pop popCount pointers off the stack.
<strong>11111101 [killCnt]</strong>  Kill killCount pointers from the top of the stack.
		    Used at cdecl (caller-pop) call sites.
<strong>10111100 </strong>           The next encoding refers to a <strong>this</strong> pointer, not
                    just an ordinary GC object pointer
<strong>10111111</strong>            The next encoding refers to an interior pointer
                    or by-ref pointer, not an ordinary GC object.
<strong>11111111</strong>            End of Table
</pre>

***Implementation Note***: The JIT code manager requires that live GC
pointers be passed only in the first 32 parameters to a method.

For an EBP-less frame the encoding above is used to describe *every* push or pop
of the stack. The counts in both the long and the short form of the
`pop` encoding refer to the number of 4 byte items removed from
the stack.

For an EBP-based frame only pushes or pops of live GC pointers are recorded. The
counts in the `pop` instructions are not the number of items
removed from the stack, but rather the number of *pointers* removed from the
stack. The implementation of the code manager uses a bit mask and high water mark to
track the locations in the parameter area that contain pointers.

# Glossary

<a name="Dfn-by-ref-pointer">*by-ref pointer*</a>

A pointer to a location inside of a garbage collectible object, created in order
to pass a parameter *by reference* rather than *by value*. A by-ref
pointer refers to either an address on the stack (when passing the address of a local
variable), an address in the static area (when passing the address of a static or
class variable) or into an array (when passing the address of an array element).
(Also see <a name="Dfn-interior-pointer">*interior pointer*</a>)

<a name="Dfn-callee-saves">*callee saves*</a>

Registers that must be preserved by the called method so that the calling method
can assume that they are unchanged when control returns.
The callee saves registers are EBP, ESI, EDI, and EBX.
The values of `ediSaved`, `esiSaved`, `ebxSaved` and `ebpSaved`
in the Method Header Information indicates whether each of these registers should be saved in
the method prolog.

<a name="Dfn-caller-saves">*caller saves*</a>

Registers that are consider to be scratch registers at a call site. If the caller
wants the value in these registers to be saved it has to arrange to saved them into
the local frame. The caller saves registers are EAX, ECX and EDX.

<a name="Dfn-EBP-Frame-or-EBP-based-Method">*EBP-Frame or EBP-based Method*</a>

Methods that have been compiled to use the EBP pointer to locate the current stack
frame. The Method Header Information will have `ebpFrame=0`.
Since the EBP is a callee-saves registers, EBP-Frames always contain a slot
(pushed immediately after the return address) where the parent's EBP is stored. In
EBP-based methods, arguments are referenced as positive offsets from the EBP and locals
variables or compiler temporaries are typically referenced as negative offsets from the
EBP. There is an exception, however, for methods which require that some of their
variables be quad word (8-byte) aligned. In this case the arguments are referenced
with respect to the EBP, but the temporaries and locals are reference using the ESP so
that a padding word can be inserted at runtime to provide the required alignment of the
locals, temporaries and ESP without affecting the EBP.

<a name="Dfn-EBP-less-Method">*EBP-less Method*</a>

Methods that use ESP (the stack pointer) rather than EBP (the frame pointer) to
reference arguments, local variables, and compiler temporary values. The Method Header Informatiom
will have `ebpFrame=0`. In these frames, the caller's EBP is
saved only if the method's code uses the register for its own purposes. In this case
the old value is saved along with the other *callee-saves* registers rather than in
a special location in the frame.

<a name="Dfn-EBP-Register">*EBP Register*</a>

One of the 7 general purpose 32-bit registers used in the x86 architecture. It can either
be used as a general purpose registers which may contain a GC reference or in may be used
as a special purpose frame pointer registers. The value of the Method Header Information
`ebpFrame=1` when it is used as a special purpose frame pointer.

<a name="Dfn-ESP-Register">*ESP Register*</a>

The name given to the 32-bit register which is always used as the stack pointer on the x86 architecture.

<a name="Dfn-epilog">*epilog*</a>

Code generated to exit a method. Because the stack pointer and frame pointer are
adjusted by this code, the JIT code manager assumes that a garbage collection is not
allowed during this code, and stack unwinding has detailed knowledge of the precise code
sequences present in the epilog.

<a name="Dfn-Fully-Interruptible-Method">*Fully Interruptible Method*</a>

A method in which the information for garbage collection is available at all points
inside the main body of the method (excluding the prolog and epilog portions of
the method). The Method Header Information has `interruptible=1`.
Because of the size of the Method GC Information required to supply this information, the JITter
avoids creating fully interruptible methods where possible. However if a method has a compute
bound loop with no method calls the method is required to be fully interruptible.

<a name="Dfn-Non-Fully-Interruptible-Method">*Non Fully Interruptible Method*</a>

A method in which the information for garbage collection is only provided at method call sites.
The Method Header Information has `interruptible=0`.
The runtime system will [hijack](#Dfn-hijack) a method exit (i.e. replace the
actual return address on the stack with an address within the runtime system) in order to
accomplish a garbage collection. The JITter will prefer to make a method non fully interruptible
in order to keep the size of the Method GC Information to a minimum.

<a name="Dfn-garbage-collection">*garbage collection*</a>

The process of tracing through all pointers to actively used objects, transitively, to
locate all objects that might be potentially referenced, and then arranging to reuse any
heap memory that was not found during this trace. The .NET runtime garbage collector
also arranges to compact the memory that is in use to reduce the working space needed for
the heap.

<a name="Dfn-hijack">*hijack*</a>

This is the normal method used to initiate a garbage collection when the code is executing.
The .NET runtime system does this by stopping all threads and modifying their current
stack frame so that the return address from the current method points to .NET runtime
code that will initiate the garbage collection. This will place all of the threads that
were executing a [non fully interruptible](#Dfn-Non-Fully-Interruptible-Method)
method at a GC safe point. For a code that is still excuting inside a
[fully interruptible](#Dfn-Fully-Interruptible-Method) method, the code manager
provides other methods for initiating garbage collection in addition to hijacking.

<a name="Dfn-interior-pointers">*interior pointers*</a>

A pointer to a location inside of a garbage collectible object.
An interior pointer or by-ref pointer is typically created by a compiler for temporary use.
Three examples of interior pointers are:

- A pointer to an element within a garbage collectible array.
- A pointer to a data member or field of a garbage collectible object.
- An argument that has been identified as a [by-ref pointer](#Dfn-by-ref-pointer)

The garbage collector must update all interior pointers when it compacts the heap.
To operate correctly, there must be a live pointer to the whole object (somewhere visible
to the garbage collector) whenever there is an interior pointer to it.

<a name="Dfn-JIT-JITter">*JIT, JITter*</a>

Just In Time compiler, which converts the .NET intermediate language (IL) into native
machine code at runtime.

<a name="Dfn-lifetime">*lifetime*</a>

The addresses in the body of a method during which a storage location is actively in
use, beginning when the location is initialized and ending with the last reference to the
location. For example, an argument location on the stack begins its life immediately
after the method's prolog since it was provided by the caller at the time the method
begins execution. Other stack locations may be initialized only late in the
execution of the method. Some stack locations (or other storage locations) are not
referenced after a particular point in the methods body and they are considered
"dead" after that point. There is no need for the garbage collector to
trace through locations that are not live, and it is critical that the garbage collector
not trace through locations that have not been initialized or have out-of-date
information. *The garbage collector assumes that live locations are safe to
trace.*

<a name="Dfn-liveness">*liveness*</a>

A property of a storage location related to its lifetime. The location is
"alive" or "live" at all code addresses during its lifetime and is
"dead" outside of it.

<a name="Dfn-local-variable-area">*local variable area*</a>

Locations allocated on the stack by a method for its own storage purposes. This
includes, for example, local variables declared by the user as well as temporary
locations for storing intermediate values (eg., when the compiler runs out of registers).
In this specification, the *locals area* does *not*> include the linkage
area or callee-saves area, nor does it include the variable-sized region of the stack
frame that is used to store parameters for methods that are being called.

<a name="Dfn-Method-GC-Information">*Method GC Information*</a>

A data structure used to describe a method so that the .NET runtime system can perform
garbage collection, handle exceptions, and so forth. It can be stored in compacted
form in a `.EXE `file by a compiler and then mapped into
memory by the OS loader or created in the same compacted format directly in memory by a
JITter. The .NET runtime system is responsible for expanding the information as
necessary.

<a name="Dfn-parameter-area">*parameter area*</a>

The part of the method's stack frame that is used to store parameters being passed to
methods that it calls. This is the part of the stack frame that grows and shrinks as
the method executes. In the future it may also include memory allocated by
methods like `_alloca`.

<a name="Dfn-pinned-pointer">*pinned pointer*</a>

A pointer to a location that must be considered as pinned for the purposes of a
garbage collection. Only untracked local varaibles can be marked as pinned pointers.
Arguments and class static (i.e. class variables) can not be directly marked as pinned.
Instead a copy of the reference that they refer to must be placed in a local varible
and that local variable must be marked as pinned. It is also possible to have a pinned
by-ref or pinned interior pointer.
A pinned pointer is *only* considered to be pinned during the lifetime of the
stack frame associated with the local variable which contains the pinning mark.

<a name="Dfn-pending-arguments">*pending arguments*</a>

For nested calls, e.g. `foo(a, b, c, d, e, bar(), f, g, h)`, when the call
to the innner `bar()` is made, the JITed code for `foo()` typically
would have already pushed 3 arguments on the stack; the values of c, d and e. The first two
arguments are normally passed in registers and are handled specially in the JITter.
The values already pushed of the stack at the time of a nested call are known as pending
arguments. When `bar()` returns, `foo()` will continue pushing its
remaining arguments; the return value of `bar()` and the values of f, g and h.

<a name="Dfn-prolog">*prolog*</a>

The part of a method's code that is executed when the method starts and during which the
fixed part of the stack frame is not yet completely initialized.
The code manager does not permit a garbage collection to occur during a method prolog
and it must take extra care during stack unwinding for methods that are executing their prolog.
The prolog is resonsible for constructing the local stack frame, savinng the callee saved
registers and initializing the untracked locals. The process of how the prolog constructs
a stack frame is described [here](#Dfn-Constructing-a-stack-frame).

<a name="Dfn-pushed-argument-area">*pushed argument area*</a>

The portion of the current stack that holds the incoming arguments passed on the stack
for this method. The JITter assumes that this area occupies the portion of the stack frame
with the highest memory addresses (i.e. that part which is pushed first). It is created by
the calling method (in its outgoing *parameter area*) and after the call occurs, becomes
the base of the current stack frame.
**Note**: that the first two arguments are passed in registers and thus
do not get pushed onto the stack and are thus not part of the pushed argument area.

<a name="Dfn-pushed-args-mask">*pushed args mask*</a>

A bit mask used to indicate which locations in the outgoing parameter area (*not* the
incoming [pushed argument area](#Dfn-pushed-argument-area)) contain live GC pointers.
It sometimes also includes bits to indicate whether particular machine registers contain live
GC pointers.

<a name="Dfn-Security-Object">*Security Object*</a>

Some methods are compiled knowing that they will be calling system services that require
security checks. These methods allocate a location in their stack frame (currently,
effectively the last temporary variable) into which the system will store a security token
at runtime. The compiler is responsible *only* for initializing the location
when the method begins execution and for making sure that the Method GC INformation
indicates that the method has a security object.

<a name="Dfn-this">*this*</a>

A pointer to the object instance on whose behalf the current method is executing.
Not all methods have a **this** pointer (static
methods, for example, do not have a **this** pointer).

<a name="Dfn-untracked">*untracked*</a>

An argument, variable, or local which contains a GC pointer but whose lifetime
information is not available at runtime. Untracked locations are assumed by the
garbage collector to be live during the entire method body, so they must be initialized by
the prolog and must be either cleared at the end of their lifetime (if known) or when
their contents might become incorrect. It is an compilation time/space vs. execution
time vs. MIH space tradeoff to decide whether an item is tracked.

<a name="Dfn-variable-sized-stack-area">*variable-sized stack area*</a>

See [*parameter area*](#Dfn-parameter-area).
