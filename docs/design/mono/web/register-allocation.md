# Register allocation in the Mono JIT

### Global Register Allocation

\<TODO\>

### Local Register Allocation

This section describes the cross-platform local register allocator which is in the file mini-codegen.c.

The input to the allocator is a basic block which contains linear IL, ie. instructions of the form:

      DEST <- SRC1 OP SRC2

where DEST, SRC1, and SRC2 are virtual registers (vregs). The job of the allocator is to assign hard or physical registers (hregs) to each virtual registers so the vreg references in the instructions can be replaced with their assigned hreg, allowing machine code to be generated later.

The allocator needs information about the number and types of arguments of instructions. It takes this information from the machine description files. It also needs arch specific information, like the number and type of the hard registers. It gets this information from arch-specific macros.

Currently, the vregs and hregs are partitioned into two classes: integer and floating point.

The allocator consists of two phases: In the first phase, a forward pass is made over the instructions, collecting liveness information for vregs. In the second phase, a backward pass is made over the instructions, assigning registers. This backward mode of operation makes the allocator somewhat difficult to understand, but leads to better code in most cases.

#### Allocator state

The state of the allocator is stored in two arrays: iassign and isymbolic. iassign maps vregs to hregs, while isymbolic is the opposite. For a vreg, iassign [vreg] can contain the following values:

       -1                      vreg has no assigned hreg

       hreg index (>= 0)            vreg is assigned to the given hreg. This means later instructions (which we have already processed due to the backward direction) expect the value of vreg to be found in hreg.

       spill slot index (< -1)  vreg is spilled to the given spill slot. This means later instructions expect the value of vreg to be found on the stack in the given spill slot. When this vreg is used as a dreg of an instruction, a spill store needs to be generated after the instruction saving its value to the given spill slot.

Also, the allocator keeps track of which hregs are free and which are used. This information is stored in a bitmask called ifree_mask.

There is a similar set of data structures for floating point registers.

#### Spilling

When an allocator needs a free hreg, but all of them are assigned, it needs to free up one of them. It does this by spilling the contents of the vreg which is currently assigned to the selected hreg. Since later instructions expect the vreg to be found in the selected hreg, the allocator emits a spill-load instruction to load the value from the spill slot into the hreg after the currently processed instruction. When the vreg which is spilled is a destination in an instruction, the allocator will emit a spill-store to store the value into the spill slot.

#### Fixed registers

Some architectures, notably x86/amd64 require that the arguments/results of some instructions be assigned to specific hregs. An example is the shift opcodes on x86, where the second argument must be in ECX. The allocator has support for this. It tries to allocate the vreg to the required hreg. If thats not possible, then it will emit compensation code which moves values to the correct registers before/after the instruction.

Fixed registers are mainly used on x86, but they are useful on more regular architectures on well, for example to model that after a call instruction, the return of the call is in a specific register.

A special case of fixed registers is two address architectures, like the x86, where the instructions place their results into their first argument. This is modelled in the allocator by allocating SRC1 and DEST to the same hreg.

#### Global registers

Some variables might already be allocated to hardware registers during the global allocation phase. In this case, SRC1, SRC2 and DEST might already be a hardware register. The allocator needs to do nothing in this case, except when the architecture uses fixed registers, in which case it needs to emit compensation code.

#### Register pairs

64 bit arithmetic on 32 bit machines requires instructions whose arguments are not registers, but register pairs. The allocator has support for this, both for freely allocatable register pairs, and for register pairs which are constrained to specific hregs (EDX:EAX on x86).

#### Floating point stack

The x86 architecture uses a floating point register stack instead of a set of fp registers. The allocator supports this by a post-processing pass which keeps track of the height of the fp stack, and spills/loads values from the stack as necessary.

#### Calls

Calls need special handling for two reasons: first, they will clobber all caller-save registers, meaning their contents will need to be spilled. Also, some architectures pass arguments in registers. The registers used for passing arguments are usually the same as the ones used for local allocation, so the allocator needs to handle them specially. This is done as follows: the MonoInst for the call instruction contains a map mapping vregs which contain the argument values to hregs where the argument needs to be placed,like this (on amd64):

    R33 -> RDI
    R34 -> RSI
    ...

When the allocator processes the call instruction, it allocates the vregs in the map to their associated hregs. So the call instruction is processed as if having a variable number of arguments which fixed register assignments.

An example:

      R33 <- 1
      R34 <- 2
      call

When the call instruction is processed, R33 is assigned to RDI, and R34 is assigned to RSI. Later, when the two assignment instructions are processed, R33 and R34 are already assigned to a hreg, so they are replaced with the associated hreg leading to the following final code:

      RDI <- 1
      RSI <- 1
      call

#### Machine description files

A typical entry in the machine description files looks like this:

shl: dest:i src1:i src2:s clob:1 len:2

The allocator is only interested in the dest,src1,src2 and clob fields. It understands the following values for the dest, src1, src2 fields:

-   i - integer register
-   f - fp register
-   b - base register (same as i, but the instruction does not modify the reg)
-   m - fp register, even if an fp stack is used (no fp stack tracking)

It understands the following values for the clob field:

-   1 - sreg1 needs to be the same as dreg
-   c - instruction clobbers the caller-save registers

Beside these values, an architecture can define additional values (like the 's' in the example). The allocator depends on a set of arch-specific macros to convert these values to information it needs during allocation.

#### Arch specific macros

These macros usually receive a value from the machine description file (like the 's' in the example). The examples below are for x86.

    /*
     * A bitmask selecting the caller-save registers (these are used for local
     * allocation).
     */
    #define MONO_ARCH_CALLEE_REGS X86_CALLEE_REGS

    /*
     * A bitmask selecting the callee-saved registers (these are usually used for
     * global allocation).
     */
    #define MONO_ARCH_CALLEE_SAVED_REGS X86_CALLER_REGS

    /* Same for the floating point registers */
    #define MONO_ARCH_CALLEE_FREGS 0
    #define MONO_ARCH_CALLEE_SAVED_FREGS 0

    /* Whenever the target uses a floating point stack */
    #define MONO_ARCH_USE_FPSTACK TRUE

    /* The size of the floating point stack */
    #define MONO_ARCH_FPSTACK_SIZE 6

    /*
     * Given a descriptor value from the machine description file, return the fixed
     * hard reg corresponding to that value.
     */
    #define MONO_ARCH_INST_FIXED_REG(desc) ((desc == 's') ? X86_ECX : ((desc == 'a') ? X86_EAX : ((desc == 'd') ? X86_EDX : ((desc == 'y') ? X86_EAX : ((desc == 'l') ? X86_EAX : -1)))))

    /*
     * A bitmask selecting the hregs which can be used for allocating sreg2 for
     * a given instruction.
     */
    #define MONO_ARCH_INST_SREG2_MASK(ins) (((ins [MONO_INST_CLOB] == 'a') || (ins [MONO_INST_CLOB] == 'd')) ? (1 << X86_EDX) : 0)

    /*
     * Given a descriptor value, return whenever it denotes a register pair.
     */
    #define MONO_ARCH_INST_IS_REGPAIR(desc) (desc == 'l' || desc == 'L')

    /*
     * Given a descriptor value, and the first register of a regpair, return a
     * bitmask selecting the hregs which can be used for allocating the second
     * register of the regpair.
     */
    #define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (desc == 'l' ? X86_EDX : -1)

[Original version of this document in git.](https://github.com/mono/mono/blob/4b2982c3096e3b17156bf00a062777ed364e3674/docs/jit-regalloc)
