Morphing of call nodes in RyuJIT
=========================

Overview
--------

In C# and IL, and unlike C/C++, the evaluation order of the arguments for calls is strictly defined.
Basically this is left to right in C# or the IL instruction ordering for IL.


One issue that must be addressed is the problem of nested calls.  Consider `Foo(x[i], Bar(y))`.
We first must evaluate `x[i]` and possibly set up the first argument for `Foo()`.  But immediately
after that, we must set up `y` as first argument of `Bar()`.  Thus, when we evaluate `x[i]` we will
need to hold that value someplace while we set up and call `Bar().`  Arguments that contain an
assignment are another issue that we need to address.  Most cases of this are rare except for
post/pre increment, perhaps like this: `Foo(j, a[j++])`.  Here `j` is updated via assignment
when the second arg is evaluated, so the earlier uses of `j` would need to be evaluated and
saved in a new LclVar.

One simple approach would be to create new single definition, single use LclVars for every argument
that is passed.  This would preserve the evaluation order.  However, it would potentially create
hundreds of LclVar for moderately sized methods and that would overflow the limited number of
tracked local variables in the JIT.  One observation is that many arguments to methods are
either constants or LclVars and can be set up anytime we want. They usually will not need a
new LclVar to preserve the order of evaluation rule.

Each argument is an arbitrary expression tree.  The JIT tracks a summary of observable side-effects
using a set of five bit flags in every GenTree node: `GTF_ASG`, `GTF_CALL`, `GTF_EXCEPT`, `GTF_GLOB_REF`,
and `GTF_ORDER_SIDEEFF`.  These flags are propagated up the tree so that the top node has a particular
flag set if any of its child nodes has the flag set.  Decisions about whether to evaluate arguments
into temp LclVars are made by examining these flags on each of the arguments.


*Our design goal for call sites is to create a few temp LclVars as possible, while preserving the
order of evaluation rules of IL and C#.*


Data Structures
------------

The most important data structure is the `GenTreeCall` node which represents a single
call site and is created by the Importer when it sees a call in the IL.  It is also
used for internal calls that the JIT needs such as helper calls.  Every `GT_CALL` node
should have a `GTF_CALL` flag set on it.  Nodes that may be implemented using a function
call also should have the `GTF_CALL` flag set on them. The arguments for a single call
site are encapsulated in the `CallArgs` class. Every call has an instance of this class
in `GenTreeCall::gtArgs`. `CallArgs` contains two linked list of arguments: the "normal"
linked list, which can be enumerated via `CallArgs::Args`, and the "late" linked list,
enumerated via `CallArgs::LateArgs`.

The normal linked list is a linked list of `CallArg` structures, in normal argument
order. When the `GenTreeCall` is first created the late args list is empty and is
set up later when we call `fgMorphArgs()` during the global Morph of all nodes. The short
explanation of why we need two lists is that we may need to force the correct evaluation
order of arguments and also architecture-specific ways of passing some arguments. See
below and the documentation of `fgMorphArgs` and `AddFinalArgsAndDetermineABIInfo` for
more information about late args.

In addition to containing IR nodes, each `CallArg` entry also contains information about
how it was evaluated and ABI information describing how to pass it.

`FEATURE_FIXED_OUT_ARGS`
-----------------

All architectures support passing a limited number of arguments in registers and the
additional arguments must be passed on the stack. There are two distinctly different
approaches for passing arguments of the stack.  For the x86 architecture a push
instruction is typically used to pass a stack based argument.  For all other architectures
that we support we allocate the `lvaOutgoingArgSpaceVar`, which is a variable-sized
stack based LclVar.  Its size is determined by the call site that has the largest
requirement for stack based arguments.  The define `FEATURE_FIXED_OUT_ARGS` is 1 for
architectures that use the outgoing arg space to pass their stack based arguments.
There is only one outgoing argument space and any values stored there are considered
to be killed by the very next call even if the next call doesn't take any stack based
arguments. For x86, we can push some arguments on the stack for one call and leave
them there while pushing some new arguments for a nested call.  Thus we allow nested
calls for x86 but do not allow them for the other architectures.


Rules for when Arguments must be evaluated into temp LclVars
-----------------

During the first Morph phase known as global Morph we call `CallArgs::ArgsComplete()`
after we have completed determining ABI information for each arg. This method applies
the following rules:

1. When an argument is marked as containing an assignment using `GTF_ASG`, then we
force all previous non-constant arguments to be evaluated into temps.  This is very
conservative, but at this phase of the JIT it is rare to have an assignment subtree
as part of an argument.
2. When an argument is marked as containing a call using the `GTF_CALL` flag, then
we force that argument and any previous argument that is marked with any of the
`GTF_ALL_EFFECT` flags into temps.
	* Additionally, for `FEATURE_FIXED_OUT_ARGS`, any previous stack based args that
    we haven't marked as needing a temp but still need to store in the outgoing args
    area is marked as needing a placeholder temp using `needPlace`.
3. We force any arguments that use `localloc` to be evaluated into temps.
4. We mark any address taken locals with the `GTF_GLOB_REF` flag. For two special
cases we call `SetNeedsTemp()` and set up the temp in `fgMorphArgs`. `SetNeedsTemp`
records the tmpNum used and sets `isTmp` so that we handle it like the other temps.
The special cases are for `GT_MKREFANY` and for a `TYP_STRUCT` argument passed by
value when we can't optimize away the extra copy.


Rules use to determine the order of argument evaluation
-----------------

After calling `ArgsComplete()` the `SortArgs()` method is called to determine the
optimal way to evaluate the arguments.  This sorting controls the order that we place
the nodes in the late argument list.

1. We iterate over the arguments and move any constant arguments to be evaluated
last and remove them from further consideration by marking them as processed.
2. We iterate over the arguments and move any arguments that contain calls to be evaluated first and remove them from further consideration by marking them as processed.
3. We iterate over the arguments and move arguments that must be evaluated into
temp LclVars to be after the ones that contain calls.
4. We iterate over the arguments and move arguments that are simple LclVar or
LclFlds and put them before the constant args.
5. If there are any remaining arguments, we evaluate them from the most complex
to the least complex.


Evaluating Args into new LclVar temps and the creation of the LateArgs
-----------------

After calling `SortArgs()`, the `EvalArgsToTemps()` method is called to create
the temp assignments and to populate the LateArgs list.

For arguments that are marked as needing a temp:
-----------------

1. We create an assignment using `gtNewTempAssign`. This assignment replaces
the original argument in the early argument list.  After we create the assignment
the argument is marked with `m_isTmp = true`.  The new assignment is marked with the
`GTF_LATE_ARG` flag.
2. Arguments that are already marked with `m_isTmp` are treated similarly as
above except we don't create an assignment for them.
3. A `TYP_STRUCT` argument passed by value will have `m_isTmp` set to true
and will use a `GT_COPYBLK` or a `GT_COPYOBJ` to perform the assignment of the temp.
4. The assignment node or the CopyBlock node is referred to as `arg1 SETUP` in the JitDump.


For arguments that are marked as not needing a temp:
-----------------

1. If this is an argument that is passed in a register, then the existing
node is moved to the late argument list and a new `GT_ARGPLACE` (placeholder)
node replaces it in the early argument list.
2. Additionally, if `m_needPlace` is true (only for `FEATURE_FIXED_OUT_ARGS`)
then the existing node is moved to the late argument list and a new
`GT_ARGPLACE` (placeholder) node replaces it in the `early argument list.
3. Otherwise the argument is left in the early argument and it will be
evaluated directly into the outgoing arg area or pushed on the stack.

After the Call node is fully morphed the LateArgs list will contain the arguments
passed in registers as well as additional ones for `m_needPlace` marked
arguments whenever we have a nested call for a stack based argument.
When `m_needTmp` is true the LateArg will be a LclVar that was created
to evaluate the arg (single-def/single-use).  When `m_needTmp` is false
the LateArg can be an arbitrary expression tree.
