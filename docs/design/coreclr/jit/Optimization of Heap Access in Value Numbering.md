# Optimization of Heap Access in Value Numbering

CLR Jit Team, Jan 2013

**Note:** this is a document written a number of years ago outlining a plan for Jit development. There is no guarantee that what is described here has been implemented, or was implemented as described.

## Introduction
The heap memory modeling currently embodied by the JIT's value numbering has at least the potential of being quite precise.  In fact, it currently throws away none of the available information, maintaining, along each path, the complete set of heap updates performed.  Unfortunately, this raises a performance problem.  Since each heap update forms a new heap value, in the form of a `VNF_MapStore` VN function applied to the previous heap value, searching through a heap value for relevant updates has the potential of being time-consuming.  This note gives a design for solving this problem.

## Background on the problem
Recall that an update `o.f = v`, where `f` is a field of some class `C`, is modeled as:
```
   H’ = H0[ C$f := H0[C$f][o := v] ]
```
Here `H0` is the heap value before the assignment, `H’` the heap value after.  The notation `m[ind := val]` denotes the “functional update” of a map `m` creating a new map that is just like `m`, except at index value `ind`, where it has value `val`.  In the VN framework, this will appear as `VNF_MapStore(m, ind, val)`.  The notation `m[ind]` is just the value of `m` at `ind`; this will appear in the VN framework as `VNF_MapSelect(m, ind)`.   Read `C$f` as a unique identifier for the field – in our system, the `CORINFO_FIELD_HANDLE` suffices.
When we create a `VNF_MapSelect` value, we try to simplify it, using the “select-of-store” rules:
```
   M[ind := v][ind] = v

   ind1 != ind2 ==> M[ind1 := v][ind2] = M[ind2]
   ```
This latter rule will be troublesome: if `M` is itself a big composition of `VNF_MapStore`’s, then we could keep applying this rule, going backwards through the history of stores to the heap, trying to find the last one for the field we’re currently interested in.

Going back to the translation of `o.f = v`, note that the value we substitute at `C$f` itself is a `MapSelect` of `H0`.  Consider a class with a large number `n` of fields, and a constructor that initializes each of the `n` fields to some value.   When we get to the `k`th field `f_k`, we will have a `k`-deep nest of `VNF_MapStore`s, representing all the stores since the start of the method.  We’ll want to construct the previous value of the `T$f_k` field map, so we’ll create a `VNF_MapSelect`.  This construction will use the second select-of-store rule to search backwards through all the previous `k` stores, trying to find a store into `f_k`.  There isn’t one, so all of these searches will end up indexing the initial heap value with `T$f_k`.  I’ve obviously just described a quadratic process for such a method.  We’d like to avoid this pathology.

Before we go on, I’ll note that we always have an “out”: if we give this search a “budget” parameter, we can always give up when the budget is exhausted, given the `VNF_MapSelect` term we’re trying to simplify a new, unique value number – which is always correct, just imprecise.  The solution I’ll describe here tries to solve the performance problem without sacrificing precision.

Let me also anticipate one possible thought: why do we have a single “heap” variable?  Couldn’t I avoid this problem by having a separate state variable for each distinct field map?  Indeed I could, but:
* We’d have to have SSA variables for each state of each such field map, complicating the SSA/tracked variable story.
* Calls are very common – and, in our current model, completely trash the heap.  With a single heap variable, from which field maps are acquired by indexing, we can lose all information about the heap at a call simply by giving the heap a new, unique value number about which nothing is known.  The same is true for other situations where we need to lose all heap information: stores through unknown pointers, volatile variable accesses, etc.  If we had per-field maps, we’d need to give all of them that are mentioned in the method a new, unique value at each such point.

## Solution

The heap here has a special property: it is indexed only by constants, where that we can always decide whether or not two indices are equal.  In some sense, this is the root of the problem, since it keeps us going backwards when the indices are distinct.  (In contrast, consider the representation of a field map `T$f_k`: this is indexed by object reference values, where we usually can’t tell whether two such values are equal or distinct, causing the search performed in creating a `VNF_MapSelect` term whose argument is a field map that’s been updated several times to stop quickly.)
The indexed-only-by-constants property allows us to consider a hash-table representation: as we’re doing value numbering within a block, we care about the current value of the heap.  If we update the heap at a particular `T$f_k` more than once, we only care about the most recent value that we stored for this index.  Thus, the idea is:
* We support a flattened representation of a heap state, and alternative to the standard “term” representation.  The flattened form would have a base state, which may either be a flattened or term form, and an updates hash table mapping heap indices (in full generality, these are field handles for static and instance fields, and representations of array types) to the corresponding values (direct values for statics, field maps for instance fields, and “array maps” for arrays) for the indices where the heap state has been updated relative to the base state.
* When value-numbering a block, we would always have the current heap state both in term form and in flattened form.  When we process an operation that modifies the heap, we would form the new term representation for the heap, as we do today, but also update the current heap state.  This is “update” in the strong sense: if a given block updates the heap at a field f several times, we change the hash table mapping to the most recent value to which f has been updated.
* Since we have the current heap state in this flattened form, we can use it to speed the evaluate of `VNF_MapSelect`’s of the current heap state: we’ll be able to find, in expected constant time, the current value, if the heap has been updated at the selected index, or else that it has not, in which case we have to continue with the base heap state.
* The point above indicates that there’s still a search.  To make this faster, we’d like the base heap state to also be flattened.  We can achieve this easily.  We allocate a new flattened representation at the beginning of each basic block we process.  When we complete the block, we save the final heap state with the block.  For a block `B1`, if it has a successor block `B2` such that `B1` is `B2`’s sole successor, then `B1`’s final heap state will be the “base” state for the current heap state used in value-numbering `B2`.  If `B2` has multiple predecessors (and some predecessor modifies the heap) then it will have a phi definition for the heap state.  In this case, `B1`’s final heap state will be the value of the phi function argument corresponding to `B1`.  In this way, the backwards search done in evaluating a `VNF_MapSelect` of a current heap state will be at most proportional to the number of basic blocks on the path, rather than the number of heap updates performed.
* We could take this idea one step further.  Assume that `B2` has `B1` as its sole predecessor, and that we’ve value-numbered `B1` and stored its final heap state.  As I’ve described it, we would make `B1`’s post-state the base state of the current heap state we use in value-numbering `B2`.  We could, instead, copy `B1`’s final state, and use that as the current heap state in `B2`.   If `H0` is the base heap state of `B1`’s post-state, then this will also be the base state we use in `B2`.  Thus, when we finish `B2`, and save the flattened heap state as `B2`’s post-state, we will have collapsed the search chain.  A `VNF_MapSelect` query on `B2`’s post-state will check that hash table, but if the indexed key isn’t found, it won’t look at `B1`’s hash table, since that was copied into `B2`’s initial state.

This is a time/space tradeoff.  It might be an attractive one: if the total set of fields that are mentioned in the method is relatively small, then there might be considerable commonality between fields modified in `B1` and those modified in `B2`.  In that case, the total final size of the hash tables would be roughly the same.  The bad case is if the sets of fields modified by `B1` and `B2` are disjoint; in that case, the fields modified by `B1` are represented twice, in the flattened post-states of each block.

If we adopt this collapsing as the default strategy, then this means within an extended basic block with root block `B0`, with base heap state `H0`, all the blocks in the EBB will immediately skip back to `H0` on a failing `VNF_MapSelect` query, skipping any interior blocks in the EBB.

## Heap phi definitions

There’s a second, perhaps worse, performance problem embedded in the current code.  If we do a `VNF_MapSelect(H, ind)` query on a heap state `H`, and `H` is a phi definition at the beginning of block `B`, and all of the predecessors of `B` have already been value-numbered, we don’t just “give up.”  Rather, we note that if `H = phi(H0, …, Hk)`, we can form `VNF_MapSelect(H0, ind), …, VNF_MapSelect(Hk, ind)` – if these all simplify to the same value, that is the value of `VNF_MapSelect(H, ind)`.
To motivate this strategy, consider the following source fragment:
```
   … o1.f …
   if (P) {
       o2.g = 17;
   } else {
       o3.h = 102;
   }
   … o1.f …
```
To make CSE work well, we want the initial and final occurrences of `o1.f` to get the same value number.  If `H0` is the heap value before the code fragment, this VN should represent `H0[T1$f][o1]`.  Both arms of the conditional update the heap – but at indices `T2$g` and `T3$h` distinct from `T1$f`.  But to determine this, and get the right value number, we have to look at both phi arguments at the heap phi definition that occurs at the merge point of the conditional, and query them with `T1$f`.

One might fear that is a disastrous exponential process: if we had a series of `N` such conditionals, aren’t there `2^N` paths through this conditional cascade, and wouldn’t we explore all of them?  There are exponentially many paths, but we could easily make sure not explore them all.  In fact, we only need to do linear work.  Assume we’re value numbering `o1.f` after the end of cascade.  We work our way back to the first conditional in the sequence, and evaluate the `VNF_MapSelect` on its phi arguments.  These both yield `H0[T1$f]`.  Recall that the “ValueNumberStore” type maintains a number of hash tables mapping function applications to their VN results.  When we attempt to evaluate a function application, we always first check to see whether there is a previously-evaluated result.   If not, we record the final result.  When we’re using the select-of-store axioms to simplify applications of `VNF_MapSelect`, we do not usually record the “intermediate results”: if the outer query were
```
    VNF_MapSelect(VNF_MapStore(VNF_MapStore(H0, T3$h, v3), T2$g, v2), T1$f)
```
we would decide first that `T1$f` and `T2$g` were distinct, and thus recursively evaluate
```
   VNF_MapSelect(VNF_MapStore(H0, T3$h, v3), T1$f)
```
This, again would reduce, and we’d get `VNF_MapSelect(H0, T1$f)` as the final result.  We’d record this VN as the result for the original function application, but not for the intermediate one above.

However, there’s no reason we couldn’t do this, as long as we were selective, to avoid excess space overhead.  The phi function case, because of the danger of exponential explosion, is an excellent candidate.  In my example, if having determined that doing the `VNF_MapSelect` on all the arguments to the phi function after the merge of the first conditional in the sequence yields the same result, `H0[T1$f][o1]`, we would record this result for the select application of the phi function in the “global” hash table that memoizes VN function applications.  We got to the first conditional by exploring first phi arguments for all the post-conditional merge blocks in the conditional cascade.  There are many more paths to get to the first conditional, but none of them will explore it further, because of the memoization.  In the same way, the second conditional’s heap phi definition will get recorded, and on up the chain.  We’ll only do linear work on this.

## Final Pathological case

We’ve handled two possible performance pathologies (or at least sketched ways for doing so.)  What bad cases remain?  If we’ve largely solved cases with large number of distinct fields, by flattening, are there perhaps problems at the next level – perhaps updates to a small number of distinct fields, but at a large number of (potentially) different object reference values?  Generally, this isn’t a problem: the quadratic search problem for the heap arose because of the constant-index property, which allowed us to keep determining that store fields were distinct from the select field, and continue searching backwards.  For object reference values, we don’t have that property – while we can determine that distinct object reference variables have the same value, we don’t really have many mechanisms for determining that they have distinct values.  We might add one, however: we could add a reasoning mechanism that allows us to note that a reference to a newly allocated object is distinct any reference to a previously-allocated object; the result is fresh.  In we add such a mechanism, a potential pathology arises:
```
    class T { public int f; }
    void M(T t0) {
        … t0.f …
        T t1 = new T(); t1.f = 1;
        … t0.f …
        T t2 = new T(); t2.f = 2;
        … t0.f …
        …
        T tk = new T(); tk.f = k;
        … t0.f …
    }
```
If we can tell that each `t1, t2, …, tk` is distinct from `t0` (and, in fact, pairwise distinct from one another, though that’s not relevant to this example), then each of the `t0.f` queries will search through all of the stores that have been done so far – and the total work will be quadratic in `k`.
Flattening is not an option here (or at least not an easy one), because normally we won’t know that object references are distinct.
I think the right response to this (and to other possible performance pathologies that I haven’t figured out yet) is to do what I mentioned at the start: have a maximum “budget” for any particular heap query, and give up and return a new, unique value number if the budget is exhausted.  For example, the budget could be formulated in terms of maximum number of recursive `VNF_MapSelect` terms evaluated as part of evaluation of an outermost such term.

## Structs

I’ll note in passing that we represent also represent struct values as maps: from struct field handles to the values of the field in the struct.  This is quite similar to the heap treatment; in fact, you could look at the static variable portion of the heap as a big struct containing all the statics.  Would flattening help here?  The pathological case to imagine here is a long sequence of stores to fields of the same struct variable `vs`, except for field `f` – followed by a lookup of `vs.f`, which has to traverse backwards through all the `VNF_MapStore` terms for the struct to get back to the original value.

First, note that his case is not quite as pathological as the original “constructor stores to `k` distinct fields” example.  For instance fields, each store implicitly involved a lookup of the previous value of the field map.  This is not true for the struct case; we just overwrite the previous value of the field, without looking it.  (For the same reason, we don’t get the quadratic pathology for a class constructor that stores to `k` distinct static fields: a subsequent query of another static not in the set stored to can take `O(k)`, but the stores themselves are each linear.)

Unfortunately, the flattening idea that helped the heap field case is not really applicable to the struct case – or at least not easily.  In the heap case, we don’t usually store intermediate states of the heap (though we may in some cases, if they’re possible inputs to a heap phi in a handler block – and we might want to consider whether we want to make copies of the current flattened heap state in that such situations.)  In the struct case, however, we do: every modification of a field of a struct variable leads to a new SSA name for the outer variable, and its value is a `VNF_MapStore` into the previous value.  We store these values with the SSA definition.  Thus, we might have many hash table copies, which would be undesirable.  A somewhat vague idea here is that if we knew the “height” of a store nest, we could choose to create a flattened representation when creating a new store whose height would exceed some maximum.  This flattened representation would have a base value, and summarize, in a hash table, the stores performed since that base value.  For example, if the maximum height were 10, when we were going to create a store term whose height were 10, we’d search through the store sequence, creating the hash table, and the base value would be the map argument of the 10th store.  We could even make this hierarchical, ensuring that even a query on an arbitrary store nest only had logarithmic cost.

## Summary, other directions

We have a system a value numbering system that saves enough information to allow sophisticated modeling of values of the program, including those in the heap.  By saving so much information, we open the possibility of making queries expensive.  I’ve sketched here a few strategies for dealing with some of the most obvious pathological examples.  I’m pretty sure we will need to address at least the flattening, phi query memoization, and budget upper bound issues, to avoid a future bug trail of “the compiler takes forever on my weird mechanically generated 100MB method.”

One other thing: I thought a little bit about merging heap states at merge points.  If we had a control flow diamond, with heap state `H0` beforehand, where each arm of the conditional assigns to `o.f`, then, especially given the flattened form, we could determine that input heap states to the heap phi definition at the merge both have `H0` as “common ancestor” state.  Then you could look at what fields were modified where.  If they were modified at the same fields and object references, then you’d look at the values.  If they agree, you could summarize them via a store to H0 of that value at that index.  If they disagree, you store a new, unique value: losing the information that “it was one of these two values.”  If only one arm stores, you also lose the information.  Complications occur when the arms store to the same field at different object references.  Then we have to analyze not just the set of field handle indices into the heap, but also the object reference handles into the field maps.  The complexities of this caused me to shy away from a full proposal of this idea, but perhaps someone else can make it work.
