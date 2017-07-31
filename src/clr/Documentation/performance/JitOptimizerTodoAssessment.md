Optimizer Codebase Status/Investments
=====================================

There are a number of areas in the optimizer that we know we would invest in
improving if resources were unlimited.  This document lists them and some
thoughts about their current state and prioritization, in an effort to capture
the thinking about them that comes up in planning discussions.


Improved Struct Handling
------------------------

This is an area that has received recent attention, with the [first-class structs](https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/first-class-structs.md)
work and the struct promotion improvements that went in for `Span<T>`.  Work here
is expected to continue and can happen incrementally.  Possible next steps:

 - Struct promotion stress mode (test mode to improve robustness/reliability)
 - Promotion of more structs; relax limits on e.g. field count (should generally
   help performance-sensitive code where structs are increasingly used to avoid
   heap allocations)
 - Improve handling of System V struct passing (I think we currently insert
   some unnecessary round-trips through memory at call boundaries due to
   internal representation issues)
 - Implicit byref parameter promotion w/o shadow copy

We don't have specific benchmarks that we know would jump in response to any of
these.  May well be able to find some with some looking, though this may be an
area where current performance-sensitive code avoids structs.


Exception handling
------------------

This is increasingly important as C# language constructs like async/await and
certain `foreach` incantations are implemented with EH constructs, making them
difficult to avoid at source level.  The recent work on finally cloning, empty
finally removal, and empty try removal targeted this.  [Writethrough](https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/eh-writethru.md)
is another key optimization enabler here, and we are actively pursuing it.  Other
things we've discussed include inlining methods with EH and computing funclet
callee-save register usage independently of main function callee-save register
usage, but I don't think we have any particular data pointing to either as a
high priority.


Loop Optimizations
------------------

We haven't been targeting benchmarks that spend a lot of time doing compuations
in an inner loop.  Pursuing loop optimizations for the peanut butter effect
would seem odd.  So this simply hasn't bubbled up in priority yet, though it's
bound to eventually.


More Expression Optimizations
-----------------------------

We again don't have particular benchmarks pointing to key missing cases, and
balancing the CQ vs TP will be delicate here, so it would really help to have
an appropriate benchmark suite to evaluate this work against.


Forward Substitution
--------------------

This too needs an appropriate benchmark suite that I don't think we have at
this time.  The tradeoffs against register pressure increase and throughput
need to be evaluated.  This also might make more sense to do if/when we can
handle SSA renames.


Value Number Conservativism
---------------------------

We have some frustrating phase-ordering issues resulting from this, but the
opt-repeat experiment indicated that they're not prevalent enough to merit
pursuing changing this right now.  Also, using SSA def as the proxy for value
number would require handling SSA renaming, so there's a big dependency chained
to this.
Maybe it's worth reconsidering the priority based on throughput?


High Tier Optimizations
-----------------------

We don't have that many knobs we can "crank up" (though we do have the tracked
assertion count and could switch inliner policies), nor do we have any sort of
benchmarking story set up to validate whether tiering changes are helping or
hurting.  We should get that benchmarking story sorted out and at least hook
up those two knobs.


Low Tier Back-Off
-----------------

We have some changes we know we want to make here: morph does more than it needs
to in minopts, and tier 0 should be doing throughput-improving inlines, as
opposed to minopts which does no inlining.  It would be nice to have the
benchmarking story set up to measure the effect of such changes when they go in,
we should do that.


Async
-----

We've made note of the prevalence of async/await in modern code (and particularly
in web server code such as TechEmpower), and have some opportunities listed in
[#7914](https://github.com/dotnet/coreclr/issues/7914).  Some sort of study of
async peanut butter to find more opportunities is probably in order, but what
would that look like?


Address Mode Building
---------------------

One opportunity that's frequently visible in asm dumps is that more address
expressions could be folded into memory operands' address expressions.  This
would likely give a measurable codesize win.  Needs some thought about where
to run in phase list and how aggressive to be about e.g. analyzing across
statements.


If-Conversion (cmov formation)
------------------------------

This hits big in microbenchmarks where it hits.  There's some work in flight
on this (see #7447 and #10861).


Mulshift
--------

Replacing multiplication by constants with shift/add/lea sequences is a
classic optimization that keeps coming up in planning.  An [analysis](https://gist.github.com/JosephTremoulet/c1246b17ea2803e93e203b9969ee5a25#file-mulshift-md)
indicates that RyuJIT is already capitalizing on most of the opportunity here.
