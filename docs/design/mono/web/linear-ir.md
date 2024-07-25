# Linear IR

This document describes Mono's new JIT engine based on a rewrite to use a linear Intermediate Representation instead of the tree-based intermediate representation that was used up to Mono 2.0.

You might also want to check [Mono's Runtime Documentation](/docs/advanced/runtime/docs/).

Intermediate Representation (IR)
--------------------------------

The IR used by the JIT is standard three address code:

OP dreg \<- sreg1 sreg2

Here dreg, sreg1, sreg2 are virtual registers (vregs). OP is an opcode. For example:

    int_add R5 <- R6 R7

### Opcodes

The opcodes used by the JIT are defined in the [mini-ops.h](https://github.com/mono/mono/blob/main/mono/mini/mini-ops.h) file. Each opcode has a value which is a C constant, a name, and some metadata containing information about the opcode like the type of its arguments and its return value. An example:

    MINI_OP(OP_IADD, "int_add", IREG, IREG, IREG)

The opcodes conform to the following naming conventions:

-   CEE\_... opcodes are the original opcodes defined in the IL stream. The first pass of the JIT transforms these opcodes to the corresponding OP\_ opcodes so CEE\_ opcodes do not occur in the intermediate code. Correspondingly, they have no opcode metadata, and are not listed in mini-ops.h.
-   OP_\<XX\> opcodes are either size agnostic, like OP_THROW, or operate on the natural pointer size of the machine, ie. OP_ADD adds two pointer size integers.
-   OP_I\<XXX\> opcodes work on 32 bit integers, ie. vregs of type STACK_I4.
-   OP_L\<XXX\> opcodes work on 64 bit integers, ie. vregs of type STACK_I8.
-   OP_F\<XXX\> opcodes work on 64 bit floats, i.e. vregs of type STACK_R8.
-   OP_V\<XXX\> opcodes work on valuetypes.
-   OP_P\<XXX\> opcodes are macros which map to either OP_I\<XXX\> or OP_L\<XXX\> opcodes depending on whenever the architecture is 32 or 64 bits.

### High/low level IR

\<......\>

### Representation of IR instructions

Each IR instruction is represented by a MonoInst structure. The fields of the structure are used as follows:

-   ins-\>opcode contains the opcode of the instruction. It is always set.

-   ins-\>dreg, ins-\>sreg1, ins-\>sreg2 contain the the destination and source vregs of the instruction. If the instruction doesn't have a destination/and our source, the corresponding field is set to -1.

-   ins-\>backend is used for various purposes:
    -   for MonoInst's representing vtype variables, it indicates that the variable is in unmanaged format (used during marshalling)
    -   instructions which operate on a register pair use it for storing the third input register of the instruction.
    -   some opcodes, like X86_LEA use it for storing auxiliary information

-   ins-\>next and ins-\>prev are used for linking the instructions.

-   ins-\>ssa_op -\> not used anymore

-   ins-\>cil_code -\> Points to the IL instructions this ins belongs to. Used for generating native offset-\> IL offset maps for debugging support.

-   ins-\>flags is used for storing various flags

-   ins-\>type and ins-\>klass contain type information for the result of the instruction. These fields are only used during the method_to_ir () pass.

In addition to the fields above, each MonoInst structure contains two pointer sized fields which can be used by the instruction for storing arbitrary data. They can be accessed using a set of inst_\<XXX\> macros.

Some guidelines for their usage are as follows:

-   OP_\<XXX\>_IMM macros store their immediate argument in inst_imm.
-   OP_\<XXX\>_MEMBASE macros store the basereg in inst_basereg (sreg1), and the displacement in inst_offset.
-   OP_STORE\<XXX\>_MEMBASE macros store the basereg in inst_destbasereg (dreg), and the displacement in inst_offset. This has historical reasons since the dreg is not modified by the instruction.

Virtual Registers (Vregs)
-------------------------

All IR instructions work on vregs. A vreg is identified by an index. A vreg also has a type, which is one of the MonoStackType enumeration values. This type is implicit, i.e. it is not stored anywhere. Rather, the type can be deduced from the opcodes which work on the vreg, i.e. the arguments of the OP_IADD opcode are of type STACK_I4.

There are two types of vregs used inside the JIT: Local and Global. They have the following differences:

### Local Vregs (lvreg)

-   are local to a basic block
-   are lightweight: allocating an lvreg is equivalent to increasing a counter, and they don't consume any memory.
-   some optimization passes like local_deadce operate only on local vregs
-   local vregs are assigned to hard registers (hregs) by the local register allocator. They do not participate in liveness analysis, and in global register allocation.
-   they have no address, i.e. it is not possible to take their address
-   they cannot be volatile

### Global Vregs

-   are heavyweight: allocating them is slower, and they consume memory. Each global vreg has an entry in the cfg-\>varinfo and cfg-\>vars arrays.
-   global vregs are either allocated to hard registers during global register allocation, or are allocated to stack slots.
-   they have an address, so it is possible to apply the LDADDR operator to them.
-   The mapping between global vregs and their associated entry in the cfg-\>varinfo array is done by the cfg-\>vreg_to_inst array. There is a macro called get_vreg_to_inst () which indexes into this array. A vreg vr is global if get_vreg_to_inst (cfg, vr) returns non NULL.

### Motivation

The JIT allocates a large number of vregs. Most of these are created during the MSIL-\>IR phase, and represent the contents of the evaluation stack. By treating these vregs specially, we don't need to allocate memory for them, and don't need to include them in expensive optimization passes like liveness analysis. Also, lvregs enable the use of local versions of classic optimization passes, like copy/constant propagation and dead code elimination, which are much faster than their global counterparts, and thus can be included in the default optimization set of a JIT compiler.

### Transitioning between the two states

-   Most vregs start out being local. Others, like the ones representing the arguments and locals of a method, start out being global.
-   Some transformations done by the JIT can break the invariant that an lvreg is local to a basic block. There is a separate pass, mono_handle_global_vregs (), which verifies this invariant and transforms lvregs into global vregs if necessary. This pass also does the opposite transformation, by transforming global vregs used only in one bblock into an lvreg.
-   If an address of a vreg needs to be taken, the vreg is transformed into a global vreg.

JIT Passes
----------

### Method-to-IR

This is the first pass of the JIT, and also the largest. Its job is to convert the IL code of the method to our intermediate representation. Complex opcodes like isinst are decomposed immediately. It also performs verification in parallel. The code is in the function method_to_ir () in method-to-ir.c.

### Decompose-Long-Opts

This pass is responsible for decomposing instructions operating on longs on 32 bit platforms as described in the section 'Handling longs on 32 bit machines'. This pass changes the CFG of the method by introducing new bblocks. It resides in the mono_decompose_long_opts () function in decompose.c.

### Local Copy/Constant Propagation

This pass performs copy and constant propagation on single bblocks. It works by making a linear pass over the instructions inside a basic block, remembering the instruction where each vreg was defined, and using this knowledge to replace references to vregs by their definition if possible. It resides in the mono_local_cprop2 () function in local-propagation.c. This pass can run anytime. Currently, it is executed twice:

-   Just after the method-to-ir pass to clean up the many redundant copies generated during the initial conversion to IR.
-   After the spill-global-vars pass to optimize the loads/stores created by that pass.

### Branch Optimizations

This pass performs a variety of simple branch optimizations. It resides in the optimize_branches () function in mini.c.

This pass runs after local-cprop since it can use the transformations generated in that pass to eliminate conditional branches.

### Handle-Global-Vregs

This pass is responsible for promoting vregs used in more than one basic block into global vregs. It can also do the opposite transformation, i.e. it can denote global vregs used in only one basic block into local ones. It resides in the mono_handle_global_vregs () function in method-to-ir.c.

This pass must be run before passes that need to distinguish between global and local vregs, i.e. local-deadce.

### Local Dead Code Elimination

This pass performs dead code elimination on single basic blocks. The instructions inside a basic block are processed in reverse order, and instructions whose target is a local vreg which is not used later in the bblock are eliminated.

This pass mostly exists to get rid of the instructions made unnecessary by the local-cprop pass.

This pass must be run after the handle-global-vregs pass since it needs to distinguish between global and local vregs.

### Decompose VType Opts

This pass is responsible for decomposing valuetype operations into simpler operations, as described in the section 'Handling valuetypes'. It resides in the mono_decompose_vtype_opts () function in decompose.c.

This pass can be run anytime, but it should be run as late as possible to enable vtype opcodes to be optimized by the local and SSA optimizations.

### SSA Optimizations

These optimizations consists of:

-   transformation of the IR to SSA form
-   optimizations: deadce, copy/constant propagation
-   transformation out of SSA form

### Liveness Analysis

This pass is responsible for calculating the liveness intervals for all global vregs using a classical backward dataflow analysis. It is usually the most expensive pass of the JIT especially for large methods with lots of variables and basic blocks. It resides in the liveness.c file.

### Global Register Allocation

This pass is responsible for allocating some vregs to one of the hard registers available for global register allocation. It uses a linear scan algorithm. It resides in the linear-scan.c file.

### Allocate Vars

This arch-specific function is responsible for allocating all variables (or global vregs) to either a hard reg (as determined during global register allocation) or to a stack location. It depends on the mono_allocate_stack_slots () function to allocate stack slots using a linear scan algorithm.

### Spill Global Vars

This pass is responsible for processing global vregs in the IR. Vregs which are assigned to hard registers are replaced with the given registers. Vregs which are assigned to stack slots are replaced by local vregs and loads/stores are generated between the local vreg and the stack location. In addition, this pass also performs some optimizations to minimalize the number of loads/stores added, and to fold them into the instructions themselves on x86/amd64. It resides in the mono_spill_global_vars () function in method-to-ir.c.

This pass must be run after the allocate_vars () pass.

Handling longs on 32 bit machines
---------------------------------

On 32 bit platforms like x86, the JIT needs to decompose opcodes operating on longs into opcodes operating on ints. This is done as follows:

-   When a vreg of type 'long' is allocated, two consecutive vregs of type 'int' are allocated. These two vregs represent the most significant and less-significant word of the long value.
-   In the decompose-long-opts pass, all opcodes operating on longs are replaced with opcodes operating on the component vregs of the original long vregs. I.e.

<!-- -->

      R11 <- LOR R21 R31
     is replaced with:
      R12 <- IOR R22 R32
      R13 <- IOR R23 R33

-   Some opcodes, like OP_LCALL can't be decomposed so they are retained in the IR. This leads to some complexity since other parts of the JIT has to be prepared to deal with long vregs.

Handling valuetypes
-------------------

Valuetypes are first class citizens in the IR, i.e. there are opcodes operating on valuetypes, there are vtype vregs etc. This is done to allow the local and SSA optimizations to be able to work on valuetypes too, and to simplify other parts of the JIT. The decompose-vtype-opts pass is responsible for decomposing vtype opcodes into simpler ones. One of the most common operations on valuetypes is taking their address. Taking the address of a variable causes it to be ignored by most optimizations, so the JIT tries to avoid it if possible, for example using a VZERO opcode for initialization instead of LDADDR+INITOBJ etc. LDADDR opcodes are generated during the decompose-vtype-opts pass, but that pass is executed after all the other optimizations, so it is no longer a problem. Another complication is the fact the vregs have no type, which means that vtype opcodes have to have their ins-\>klass fields filled in to indicate the type which they operate on.

Porting an existing backend to the new IR
-----------------------------------------

-   Add the following new functions:
    -   mono_arch_emit_call (). Same as mono_arch_call_opcode (), but emits IR for pushing arguments to the stack. All the stuff in mono_arch_emit_this_vret_args () should be done in emit_call () too.
    -   mono_arch_emit_outarg_vt (). Emits IR to push a vtype to the stack
    -   mono_arch_emit_setret (). Emits IR to move its argument to the proper return register
    -   mono_arch_emit_inst_for_method (). Same as mono_arch_get_inst_for_method, but also emits the instructions.

-   Add new opcodes to cpu-\<ARCH\>.md and mono_arch_output_basic_block ():
    -   dummy_use, dummy_store, not_reached
    -   long_bCC and long_cCC opcodes
    -   cond_exc_iCC opcodes
    -   lcompare_imm == op_compare_imm
    -   int_neg == neg
    -   int_not == not
    -   int_convXX == conv.iXX
    -   op_jump_table
    -   long_add == cee_add (on 64 bit platforms)
    -   op_start_handler, op_endfinally, op_endfilter
-   In mono_arch_create_vars, when the result is a valuetype, it needs to create a new variable to represent the hidden argument holding the vtype return address and store this variable into cfg-\>vret_addr.
-   Also, in mono_arch_allocate_vars, when the result is a valuetype, it needs to setup cfg-\>vret_addr instead of cfg-\>ret.

For more info, compare the already converted backends like x86/amd64/ia64 with their original versions in HEAD. For example: [[1]](https://lists.dot.net/pipermail/mono-patches/2006-April/073170.html)

Benchmark results
-----------------

All the benchmarks were run on an amd64 machine in 64 bit mode.

-   pnetmark score:

<!-- -->

        current JIT: 19725
        linear IR: 24970 (25% faster)

-   mini/bench.exe:

<!-- -->

        current JIT: 2.183 secs
        linear IR: 1.972 secs (10% faster)

-   corlib 2.0 compile:

<!-- -->

        current JIT: 9.421 secs
        linear IR: 9.223 secs (2% faster)

-   ziptest.exe from [https://bugzilla.novell.com/show_bug.cgi?id=342190](https://bugzilla.novell.com/show_bug.cgi?id=342190) on the zerofile.bin input file:

<!-- -->

        current JIT: 18.648 secs
        linear IR: 9.934 secs (50% faster)

-   decimal arithmetic benchmark from [https://lists.dot.net/pipermail/mono-devel-list/2008-May/028061.html](https://lists.dot.net/pipermail/mono-devel-list/2008-May/028061.html):

<!-- -->

       current JIT:
         addition 3774.094 ms
         substraction 3644.657 ms
         multiplication 2959.355 ms
         division 61897.441 ms
       linear IR:
         addition 3096.526 ms
         substraction 3065.364 ms
         multiplication 2270.676 ms
         division 60514.169 ms

-   IronPython pystone.py 5000000 iterations:

<!-- -->

       current JIT: 69255.7 pystones/second
       linear IR: 83187.8 pystones/second (20% faster)

 All the code size tests were measured using `mono --stats --compile-all <ASSEMBLY NAME>`

-   corlib 1.0 native code size:

<!-- -->

        current JIT: 2100173 bytes
        linear IR: 1851966 bytes (12% smaller)

-   mcs.exe native code size:

<!-- -->

        current JIT: 1382372 bytes
        linear IR: 1233707 bytes (11% smaller)

-   all 1.0 assemblies combined:

<!-- -->

        current JIT: 15816800 bytes
        linear IR: 12774991 bytes (20% smaller)

Improvements compared to the Mono 1.x and Mono 2.0 JITs
-------------------------------------------------------

-   The old JIT used trees as its internal representation, and the only thing which was easy with trees was code generation, everything else is hard. With the linear IR, most things are easy, and only a few things are hard, like optimizations which transform multiple operations into one, like transforming a load+operation+store into an operation taking a memory operand on x86.

-   Since there is only one IR instead of two, the new JIT is (hopefully) easier to understand and modify.

-   There is an if-conversion pass which can convert simple if-then-else statements to predicated instructions on x86/64, eliminating branches.

-   Due to various changes, the ABCREM pass can eliminate about twice as many array bound checks in corlib as the current JIT. It was also extended to eliminate redundant null checks.

-   Handling of valuetypes is drastically improved, including:
    -   allowing most optimization passes like constant and copy propagation to work on valuetypes.
    -   elimination of redundant initialization code inserted because of the initlocals flag.
    -   elimination of many redundant copies when the result of a call is passed as an argument to another call.
    -   passing and returning small valuetypes in registers on x86/amd64.

-   Due to the elimination of the tree format, it is much easier to generate IR code for complex IL instructions. Some things, like branches, which are almost impossible to generate in the current JIT in the method_to_ir () pass, can be generated easily.

-   The handling of soft-float on ARM is done in a separate pass instead of in a miriad places, hopefully getting rid of bugs in this area.

-   In the old representation the tree to code transformations were easy only if the "expression" to transform was represented as a tree. If, for some reason, the operation was "linearized", using local variables as intermediate results instead of the tree nodes, then the optimization simply did not take place. Or the jit developer had to code twice: once for the tree case and once for the "linear" case.
