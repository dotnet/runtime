# Modeling Longs on 32-bit Architectures

The challenge here is to model long operations in a way that reflects register lifetimes for the
lo and hi halves of a 64-bit integer accurately enough that we can achieve good register
allocation.

## Background

The current liveness model supports:
* Internal registers (interfere with all sources, but no defs)
* Sources that are not last use (interfere with all others)
* Sources that are last use and NOT DelayFree (interfere with all sources, but no defs)
* Sources that are last use AND DelayFree (interfere with all sources and defs)
* Kills (interfere with all uses but no defs)

Previously (eons ago) when we were working on arm32, the model had all but the last def
interfering with the uses and internal registers (i.e. they went live during the first
location for the node).
* This supports the ability to reuse inputs, e.g. for long adds, subs or addresses, after the first def register goes live.
* This doesn’t work for supporting multi-reg call nodes:
  * All defs need to occur AFTER the kills, and they do not interfere with any sources.

Considerations for an expanded model:
* Must be “pay for play” (not make LSRA more expensive for non-long code generation)
* It is not practical to precisely model nodes that generate multiple instructions:
  * In the case of longs, we may have one source operand that could be overwritten by
    the first result, but other operands that need to remain live.
  * However, on x86 it will be really important not to waste registers.

## Decomposition

This is the approach currently being taken. In the initial implementation, the reordering
of nodes to support the required "valid tree traversal" property of the IR was causing
the code to be incorrect because for instructions like `add` the carry bit needs to be
immediately consumed by the computation ofthe hi bits.

## Decomposition with temps

In order to preserve the current decomposition approach and not change the fundamental
tree ordering properties, it is necessary to evaluate sub-expressions into temps, to keep
the lo and hi computations together.

There are concerns about this, because it requires generating a number of extra temps
in the case of nested expressions. However, mikedn has done some experimentation
[here](https://github.com/mikedn/coreclr/blob/decompose/src/jit/lower.cpp#L424)
that indicates that this approach may not be as problematic as we feared.

This basic idea is that whenever we need to decompose hi/lo operations but keep them
together (i.e. can’t preserve the tree traversal/linear order invariants), we create a temp.

## Richer Liveness Model (No Decomposition)

The idea here would be to retain `TYP_LONG` nodes in the IR, and to find a way to extend
the liveness model used by Lowering, LSRA and CodeGen to ensure good register allocation.
o

In the table below, there are several operations that have different register lifetime
characteristics.
Each is modeled with a sequence of "Locations" for which the changes in register lifetimes
can be viewed as happening at the same time.
All lifetimes participating in a given Location are considered to interfere.
Note that the simplest model is that all the uses happen at one location, and then the
def(s) happen at the next Location (i.e. the def does not interfere with the uses).
The model becomes more complicated when we have constraints such as RMW (read-modify-write)
operands, and even more so when we are actually modeling multiple target instructions
with a single IR node.

To avoid even more complication than is already inherent in this issue, we will assume
that the evaluation order is fixed during Lowering (and in these examples we are assuming
that the lo operand is evaluated first).

In future we may want to consider the implications of relaxing that constraint, though
note that the Decomposition approach also requires a predetermined evaluation order.

| Operation     | Location 1 | Location 2 | Location 3 |
| --------------|:----------:| ----------:| ----------:|
| x = y         | use y.lo   | def x.lo   |            |
|               | use y.hi   | def x.hi   |            |
|               |            |            |            |
| x = y + z     | use y.lo   | def x.lo   | def x.hi   |
|               | use z.lo   | use y.hi   |            |
|               | y.hi live  | use z.hi   |            |
|               | z.hi live  | use z.hi   |            |
|               |            |            |            |
| x = *p        | use p      | def x.hi   |            |
| (non-ldp)     | def x.lo   |            |            |
|               |            |            |            |
| x = *p        | use p      | def x.lo   |            |
| (ldp)         |            | def x.hi   |            |
|               |            |            |            |
| x = \*(p+i*8) | use p      | def x.hi   |            |
| (non-ldp)     | use i      |            |            |
|               | def x.lo   |            |            |
|               |            |            |            |
| x = call      | use args   | def x.lo   |            |
|               | kill (end) | def x.hi   |            |


Both the non-ldp  (load pair - available on Arm but not x86) cases take
advantage of the fact that all the sources must remain live
with the first def. The same can be done in the non-ldp case of `x = *p`.
However, that approach doesn't reduce the Location requirement for the `x = y + z` case,
where, if we assume that all inputs must be live for the first def, then we keep the lo
registers live longer than necessary, and therefore can't reuse them for the x.lo (or
other) results.

TO DO:
* Extend the above to include more operations, and to show the actual instructions
  generated, to determine how well we can approximate the "optimal" liveness model.

## True Linear IR

It would be really nice to break the “valid tree traversal” restriction, and allow
arbitrary (or at least more flexible) reordering of nodes when in linear form.
* Gets rid of embedded statements!!
* Affects (at least) rationalizer and lsra, which implicitly depend on this property in their use of stacks.
* Probably has many unknown other impacts
