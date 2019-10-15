# RyuJIT: Porting to different platforms

## What is a Platform?
* Target instruction set and pointer size
* Target calling convention
* Runtime data structures (not really covered here)
* GC encoding
  * So far only JIT32_GCENCODER and everything else
* Debug info (so far mostly the same for all targets?)
* EH info (not really covered here)

One advantage of the CLR is that the VM (mostly) hides the (non-ABI) OS differences

## The Very High Level View
* 32 vs. 64 bits
  * This work is not yet complete in the backend, but should be sharable
* Instruction set architecture:
  * instrsXXX.h, emitXXX.cpp and targetXXX.cpp
  * lowerXXX.cpp
  * codeGenXXX.cpp and simdcodegenXXX.cpp
  * unwindXXX.cpp
* Calling Convention: all over the place

## Front-end changes
* Calling Convention
  * Struct args and returns seem to be the most complex differences 
    * Importer and morph are highly aware of these
      * E.g. fgMorphArgs(), fgFixupStructReturn(), fgMorphCall(), fgPromoteStructs() and the various struct assignment morphing methods
  * HFAs on ARM 
* Tail calls are target-dependent, but probably should be less so
* Intrinsics: each platform recognizes different methods as intrinsics (e.g. Sin only for x86, Round everywhere BUT amd64)
* Target-specific morphs such as for mul, mod and div

## Backend Changes
* Lowering: fully expose control flow and register requirements
* Code Generation: traverse blocks in layout order, generating code (InstrDescs) based on register assignments on nodes
  * Then, generate prolog & epilog, as well as GC, EH and scope tables
* ABI changes:
  * Calling convention register requirements
    * Lowering of calls and returns
    * Code sequences for prologs & epilogs
  * Allocation & layout of frame

## Target ISA "Configuration"
* Conditional compilation (set in jit.h, based on incoming define, e.g. #ifdef X86)
```C++
_TARGET_64_BIT_ (32 bit target is just ! _TARGET_64BIT_)
_TARGET_XARCH_, _TARGET_ARMARCH_
_TARGET_AMD64_, _TARGET_X86_, _TARGET_ARM64_, _TARGET_ARM_
```
* Target.h
* InstrsXXX.h

## Instruction Encoding
* The instrDesc is the data structure used for encoding
  * It is initialized with the opcode bits, and has fields for immediates and register numbers.
  * instrDescs are collected into groups
  * A label may only occur at the beginning of a group
* The emitter is called to:
  * Create new instructions (instrDescs), during CodeGen
  * Emit the bits from the instrDescs after CodeGen is complete
  * Update Gcinfo (live GC vars & safe points)

## Adding Encodings
* The instruction encodings are captured in instrsXXX.h. These are the opcode bits for each instruction
* The structure of each instruction's encoding is target-dependent
* An "instruction" is just the representation of the opcode
* An instance of "instrDesc" represents the instruction to be emitted
* For each "type" of instruction, emit methods need to be implemented. These follow a pattern but a target may have unique ones, e.g.
```C++
emitter::emitInsMov(instruction ins, emitAttr attr, GenTree* node)
emitter::emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t     val)
emitter::emitInsTernary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src1, GenTree* src2) (currently Arm64 only)
```

## Lowering
* Lowering ensures that all register requirements are exposed for the register allocator
  * Use count, def count, "internal" reg count, and any special register requirements
  * Does half the work of code generation, since all computation is made explicit
    * But it is NOT necessarily a 1:1 mapping from lowered tree nodes to target instructions
  * Its first pass does a tree walk, transforming the instructions. Some of this is target-independent. Notable exceptions:
    * Calls and arguments
    * Switch lowering
    * LEA transformation
  * Its second pass walks the nodes in execution order
    * Sets register requirements
      * sometimes changes the register requirements children (which have already been traversed)
    * Sets the block order and node locations for LSRA
      * LinearScan:: startBlockSequence() and LinearScan::moveToNextBlock()

## Register Allocation
* Register allocation is largely target-independent
  * The second phase of Lowering does nearly all the target-dependent work
* Register candidates are determined in the front-end
  * Local variables or temps, or fields of local variables or temps
  * Not address-taken, plus a few other restrictions
  * Sorted by lvaSortByRefCount(), and marked "lvTracked"

## Addressing Modes
* The code to find and capture addressing modes is particularly poorly abstracted
* genCreateAddrMode(), in CodeGenCommon.cpp traverses the tree looking for an addressing mode, then captures its constituent elements (base, index, scale & offset) in "out parameters"
  * It optionally generates code
  * For RyuJIT, it NEVER generates code, and is only used by gtSetEvalOrder, and by lowering

## Code Generation
* For the most part, the code generation method structure is the same for all architectures
  * Most code generation methods start with "gen"
* Theoretically, CodeGenCommon.cpp contains code "mostly" common to all targets (this factoring is imperfect)
  * Method prolog, epilog, 
* genCodeForBBList
  * walks the trees in execution order, calling genCodeForTreeNode, which needs to handle all nodes that are not "contained"
  * generates control flow code (branches, EH) for the block
