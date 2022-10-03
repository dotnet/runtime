Passing and Returning Structs
=============================
Problem Statement
-----------------
The current implementation of ABI (Application Binary Interface, aka calling
convention) support in RyuJIT is problematic in a number of areas, especially
when it comes to the handling of structs (aka value types).

- RyuJIT currently supports 4 target architectures: x86, x64 (aka x86-64), ARM
  and ARM64, with two different ABIs for x64 (Windows and Linux).
  These each have unique requirements, yet these requirements are expressed in
  the code programmatically, with #ifdefs, and yet even where the requirements
  are shared, they are often handled in different code paths.

- When passing or returning structs, the code generator sometimes requires
  that the struct must be copied to or from memory. The morpher (`fgMorphArgs()`)
  attempts to discern these cases, and create copies when necessary, but sometimes it
  makes copies when they aren't needed.

- Even in cases where the code generator currently requires the struct to be
  in memory, it could be enhanced to handle the in-register case:
  - Currently, when we have a register-passed struct that fits in a register,
    but that doesn't have a single field of a matching type,
    `fgMorphArgs()` generates a `GT_LCL_FLD` of the appropriate scalar type
    to reference the value. This forces the struct to be marked `lvDoNotEnregister`.
    However, the backend has support for performing the necessary move in
    some cases (e.g. when a struct with a single field of `TYP_DOUBLE` is passed
    in an integer register as `TYP_LONG`), by generating a `GT_BITCAST` to move
    the value to the appropriate register.
  - In other cases (e.g. a struct with two `TYP_INT` fields in registers), the
    backend should be able to generate the necessary code to place the fields
    in the necessary register(s).

- Even when the requirements are similar, the IL representation, as well as the
  transformations performed by `fgMorphArgs()`, are not the same.

- Much of the information about each argument is contained in the `fgArgInfo`
  on the `GT_CALL` node. It in turn contains an `argTable` with an entry for
  each argument. However, this information is not complete, especially on
  x64/Linux where repeated calls are made to the VM to obtain the struct
  descriptor.

- The functionality of `fgMorphArgs()` combines the determination of the ABI
  requirements, which sets up the `fgArgInfo` and `argTable`, with the IR
  transformations required to ensure that the arguments of the `GT_CALL` are
  in the appropriate form.

- When `fgCanFastTailCall()` is called, it doesn't yet have the `fgArgInfo`,
  so it must duplicate some of the analysis that is done in `fgMorphArgs()`

High-Level Proposed Design
--------------------------
Note that much of the below work has already been carried out and further refactoring has replaced the side `fgArgInfo` table with `CallArgs`.
The plan here is intended to provide some historical context and may not completely reflect JIT sources.

First, the `fgArgInfo` is extended to contain all the information needed to determine
how an argument is passed. Ideally, most of the `#ifdef`s relating to ABI differences
can be eliminated by querying the `fgArgInfo`. Most of the information will be queried
via properties, such that when a target doesn't support a particular struct passing
mechanism (e.g. passing structs by reference), the property will unconditionally return false, and the associated code paths will be eliminated.

The initial determination of the number of arguments and how they
are passed is extracted from `fgMorphArgs()` into a separate method: `gtInitArgInfo()`. It is idempotent - that is, it can be re-invoked and will simply return if it
has already been called. It can be called by `fgCanFastTailCall()` so that it can query
the `argTable` to get the information it requires.

This method is responsible for the first part of what is currently `fgMorphArgs()`, plus setting up the `argTable`:
- Count the number of args.
  - Create any non-standard args (e.g. indirection cells or cookie parameters) that
    are needed, but don't yet create copies
- Create the `argTable` for the given number of args
- Initialize the `fgArgInfo` for each arg, with all the information about how
  the arg is passed, and whether it requires a temp, but don't yet create any
  temps.
  - On x64/ux, this is the only method that should need to consult the struct
    descriptor for outgoing arguments.
  - The `isProcessed` flag remains false until `fgMorphArgs()` has handled
    the arg.
  - The `fgArgInfo` contains an array of register numbers (sized according to the
    maximum number of registers used for a single argument). If the first register
    in `REG_STK`, the argument is passed entirely on the stack. For most targets,
    if the first register is a register, the argument is passed entirely in
    registers. When arguments can be split (`_TARGET_ARM_`), this will be indicated
    with an `isSplit` property of `true`.
    - Note that the `isSplit` property would evaluate to false on targets where
      it is not supported, reducing the need for `ifdef`s (we can rely on the compiler
      to eliminate those dead paths).
- Validate that each struct argument is either a `GT_LCL_VAR`, a `GT_OBJ`,
  or a `GT_MKREFANY`.

During the initial `fgMorph` phase, `fgMorphArgs()` does the following:

- Calls `gtInitArgInfo()` to ensure that the `argTable` is set up properly.

- Creates a copy of each argument as necessary.
  - This should only be done if one or more of the following conditions hold:
    - A copy is required to preserve possible ordering dependencies, in which
      case the `needsTmp` field of the `fgArgInfo` was set to true by
      `fgInitArgInfo()`.
    - A struct arg has been promoted, it is passed in register(s) (or split),
      and has not yet been marked `lvDoNotEnregister`.

- Sets up the actual argument for any non-standard args.

- Transforms struct arg nodes from `GT_LCL_VAR`, `GT_OBJ` or `GT_MKREFANY` into:
  - `GT_FIELD_LIST` (i.e. a list of fields) if the lclVar is promoted and
    either 1) passed on the stack, or 2) each register used to pass the struct
    corresponds to exactly one field of the struct. The type of the register
    in which a field is passed need not match the type of the field.
    - The case of a single `GT_FIELD_LIST` node subsumes the current
      `GT_LCL_FLD` representation for a matching single-field struct,
      and does not require a lclVar to be marked `lvDoNotEnregister`.
      Any register type mismatch (e.g. a float field passed in an integer
      register) will be handled by `Lowering` (see below).
    - In future, this should include *any* case of a promoted struct, and the
      backend (`Lowering` and/or `CodeGen`) should be enhanced to correctly
      perform the needed re-assembling of fields into registers.
  - `GT_LCL_VAR` if the argument is a non-promoted struct that is either
    marked `lvDoNotEnregister` or fully enregistered, such as a SIMD type lclVar
    or (in future) a struct that fits entirely into a register.
  - `GT_OBJ` otherwise. In this case, if it is a partial reference to a lclVar, it must be
    marked `lvDoNotEnregister`. (If it is a full reference to a lclVar, it falls into
    the `GT_LCL_VAR` case above.) This representation will be used even for structs
    that are passed as a primitive type (i.e. that currently use the `GT_LCL_FLD`
    representation).

During `Lowering`, any mismatches between the type of an actual register argument (i.e. the
`GT_OBJ` or the `GT_FIELD_LIST` element) and the type of the register, will cause a
`GT_BITCAST` node to be inserted. The purpose of this node is simply to instruct the
register allocator to move the value between the register files, without requiring the
value to necessarily be spilled to memory.'

Future
------
There are additional improvements for struct parameters for future consideration:

- Support passing promoted structs in registers (as suggested above), where `Lowering`
  would insert the necessary IR to assemble the fields into registers.
- Instead of generating `GT_FIELD_LIST`, we should consider modeling the passing of a
  promoted struct as separate arguments. This would probably be best implemented by
  modifying the `argTable` during `fgMorphArgs()` such that it reflects the "as-if"
  signature with the exploded struct fields.
  - How this would impact the handling of fields that must be packed into a single
    register remains to be determined (i.e. does `fgMorphArgs()` generate the IR
    to assemble the fields into a single register-sized value, or is that somehow
    deferred?)
- Support vector calling conventions. This should be somewhat simplified by the
  extraction of the ABI code.
