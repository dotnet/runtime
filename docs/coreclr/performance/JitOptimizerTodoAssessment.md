Optimizer Codebase Status/Investments
=====================================

There are a number of areas in the optimizer that we know we would invest in
improving if resources were unlimited.  This document lists them and some
thoughts about their current state and prioritization, in an effort to capture
the thinking about them that comes up in planning discussions.


Big-Ticket Items
----------------

### Improved Struct Handling

Most of the work required to improve struct handling in RyuJIT is captered in the [first-class structs](https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/first-class-structs.md)
document, though that document needs to be updated to reflect the work that has already been done.

Recent improvements include the struct promotion improvements that went in for `Span<T>`.

Work to improve struct handling is expected to continue and can happen incrementally.

Possible next steps:

 - Code quality improvements:
   - Promotion of more structs. Relaxing limits, such as on field count, should generally
     help performance-sensitive code where structs are increasingly used to avoid
     heap allocations.
   - Implicit byref parameter promotion w/o shadow copy.
   - Improve support for value numbering optimizations on structs, especially for copy propagation
     and assertion propagation of zero-initialization.
     - This impacts the pi-digits benchmark.
   - Unify the handling of struct arguments - in particular remove some of the Unix-specific code
     that is responsible for some of the extra copies that are inserted around calls.
     - This improves a number of methods in the Devirtualization benchmarks.
   - Eliminate additional cases where structs are unnecessarily forced to memory when passed to or
     returned from calls, even though there is a one-to-one match between fields and registers.
     - This impacts the binarytrees benchmark.
   - Enable non-memory assignments of same-sized structs with different field composition.
     This is effectively what is required to allow passing of structs in registers (on targets that
     support it) when the fields don't match the registers.

 - Test and reliability improvements:
   - Struct promotion stress mode (test mode to improve robustness/reliability) - i.e. eliminate
     or greatly increase current limits on the size or number of fields.


We don't have specific benchmarks that we know would jump in response to all of
the code quality improvements, though most have issues associated with them.
We may well be able to find some additional benchmarks or real-world-code with some looking,
though it may be the case that current performance-sensitive code avoids structs.

There's also work going on in corefx to use `Span<T>` more broadly.  We should
make sure we are expanding our span benchmarks appropriately to track and
respond to any particular issues that come out of that work.


### Exception handling

This is increasingly important as C# language constructs like async/await and
certain `foreach` incantations are implemented with EH constructs, making them
difficult to avoid at source level.  The recent work on finally cloning, empty
finally removal, and empty try removal targeted this.  [Writethrough](https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/eh-writethru.md)
is another key optimization enabler here, and we are actively pursuing it.  Other
things we've discussed include inlining methods with EH and computing funclet
callee-save register usage independently of main function callee-save register
usage, but I don't think we have any particular data pointing to either as a
high priority.


### Loop Optimizations

We haven't been targeting benchmarks that spend a lot of time doing computations
in an inner loop.  Pursuing loop optimizations for the peanut butter effect
would seem odd.  So this simply hasn't bubbled up in priority yet, though it's
bound to eventually.  Obvious candidates include [IV widening](https://github.com/dotnet/coreclr/issues/9179),
[unrolling](https://github.com/dotnet/coreclr/issues/11606), load/store motion,
and strength reduction.


### High Tier Optimization

We don't have that many knobs we can "crank up" (though we do have the tracked
assertion count and could switch inliner policies), nor do we have any sort of
benchmarking story set up to validate whether tiering changes are helping or
hurting.  We should get that benchmarking story sorted out and at least hook
up those two knobs.

Some of this may depend on register allocation work, as the RA currently has
some issues, particularly around spill placement, that could be exacerbated by
very aggressive upstream optimizations.


Mid-Scale Items
---------------

### More Expression Optimizations

We again don't have particular benchmarks pointing to key missing cases, and
balancing the CQ vs TP will be delicate here, so it would really help to have
an appropriate benchmark suite to evaluate this work against.


### Forward Substitution

This too needs an appropriate benchmark suite that I don't think we have at
this time.  The tradeoffs against register pressure increase and throughput
need to be evaluated.  This also might make more sense to do if/when we can
handle SSA renames.


### Async

We've made note of the prevalence of async/await in modern code (and particularly
in web server code such as TechEmpower), and have some opportunities listed in
[#7914](https://github.com/dotnet/coreclr/issues/7914).  Some sort of study of
async peanut butter to find more opportunities is probably in order, but what
would that look like?


### If-Conversion (cmov formation)

This hits big in microbenchmarks where it hits.  There's some work in flight
on this (see [#7447](https://github.com/dotnet/coreclr/issues/7447) and
[#10861](https://github.com/dotnet/coreclr/pull/10861)).


### Address Mode Building

One opportunity that's frequently visible in asm dumps is that more address
expressions could be folded into memory operands' address expressions.  This
would likely give a measurable codesize win.  Needs some thought about where
to run in phase list and how aggressive to be about e.g. analyzing across
statements.


### Low Tier Back-Off

We have some changes we know we want to make here: morph does more than it needs
to in minopts, and tier 0 should be doing throughput-improving inlines, as
opposed to minopts which does no inlining.  It would be nice to have the
benchmarking story set up to measure the effect of such changes when they go in,
we should do that.


### Helper Call Register Kill Set Improvements

We have some facility to allocate caller-save registers across calls to runtime
helpers that are known not to trash them, but the information about which
helpers trash which registers is spread across a few places in the codebase,
and has some puzzling quirks like separate "GC" and "NoGC" kill sets for the
same helper.  Unifying the information sources and then refining the recorded
kill sets would help avoid more stack traffic.  See [#12940](https://github.com/dotnet/coreclr/issues/12940).

Low-Hanging Fruit
-----------------

### Switch Lowering

The MSIL `switch` instruction is actually encoded as a jump table, so (for
better or worse) intelligent optimization of source-level switch statements
largely falls to the MSIL generator (e.g. Roslyn), since encoding sparse
switches as jump tables in MSIL would be impractical.  That said, when the MSIL
has a switch of just a few cases (as in [#12868](https://github.com/dotnet/coreclr/issues/12868)),
or just a few distinct cases that can be efficiently checked (as in [#12477](https://github.com/dotnet/coreclr/issues/12477)),
the JIT needn't blindly emit these as jump tables in the native code.  Work is
underway to address the latter case in [#12552](https://github.com/dotnet/coreclr/pull/12552).


### Write Barriers

A number of suggestions have been made for having the JIT recognize certain
patterns and emit specialized write barriers that avoid various overheads --
see [#13006](https://github.com/dotnet/coreclr/issues/13006) and [#12812](https://github.com/dotnet/coreclr/issues/12812).


### Byref-Exposed Store/Load Value Propagation

There are a few tweaks to our value-numbering for byref-exposed loads and stores
to share some of the machinery we use for heap loads and stores that would
allow better propagation through byref-exposed locals and out parameters --
see [#13457](https://github.com/dotnet/coreclr/issues/13457) and
[#13458](https://github.com/dotnet/coreclr/issues/13458).

Miscellaneous
-------------

### Value Number Conservativism

We have some frustrating phase-ordering issues resulting from this, but the
opt-repeat experiment indicated that they're not prevalent enough to merit
pursuing changing this right now.  Also, using SSA def as the proxy for value
number would require handling SSA renaming, so there's a big dependency chained
to this.
Maybe it's worth reconsidering the priority based on throughput?


### Mulshift

RyuJIT has an implementation that handles the valuable cases (see [analysis](https://gist.github.com/JosephTremoulet/c1246b17ea2803e93e203b9969ee5a25#file-mulshift-md)
and [follow-up](https://github.com/dotnet/coreclr/pull/13128) for details).
The current implementation is split across Morph and CodeGen; ideally it would
be moved to Lower, which is tracked by [#13150](https://github.com/dotnet/coreclr/issues/13150).
