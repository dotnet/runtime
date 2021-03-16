JIT Optimizer Planning Guide
============================

The goal of this document is to capture some thinking about the process used to
prioritize and validate optimizer investments.  The overriding goal of such
investments is to help ensure that the dotnet platform satisfies developers'
performance needs.


Benchmarking
------------

There are a number of public benchmarks which evaluate different platforms'
relative performance, so naturally dotnet's scores on such benchmarks give
some indication of how well it satisfies developers' performance needs.  The JIT
team has used some of these benchmarks, particularly [TechEmpower](https://www.techempower.com/benchmarks/)
and [Benchmarks Game](http://benchmarksgame.alioth.debian.org/), for scouting
out optimization opportunities and prioritizing optimization improvements.
While it is important to track scores on such benchmarks to validate performance
changes in the dotnet platform as a whole, when it comes to planning and
prioritizing JIT optimization improvements specifically, they aren't sufficient,
due to a few well-known issues:

 - For macro-benchmarks, such as TechEmpower, compiler optimization is often not
   the dominant factor in performance.  The effects of individual optimizer
   changes are most often in the sub-percent range, well below the noise level
   of the measurements, which will usually be at least 3% or so even for the
   most well-behaved macro-benchmarks.
 - Source-level changes can be made much more rapidly than compiler optimization
   changes.  This means that for anything we're trying to track where the whole
   team is effecting changes in source, runtime, etc., any particular code
   sequence we may target with optimization improvements may well be targeted
   with source changes in the interim, nullifying the measured benefit of the
   optimization change when it is eventually merged.  Source/library/runtime
   changes are in play for TechEmpower and Benchmarks Game both.

Compiler micro-benchmarks (like those in our [test tree](https://github.com/dotnet/runtime/tree/main/src/tests/JIT/Performance/CodeQuality))
don't share these issues, and adding them as optimizations are implemented is
critical for validation and regression prevention; however, micro-benchmarks
often aren't as representative of real-world code, and therefore not as
reflective of developers' performance needs, so aren't well suited for scouting
out and prioritizing opportunities.


Benefits of JIT Optimization
----------------------------

While source changes can more rapidly and dramatically effect changes to
targeted hot code sequences in macro-benchmarks, compiler changes have the
advantage that they apply broadly to all compiled code.  One of the best reasons
to invest in compiler optimization improvements is to capitalize on this.  A few
specific benefits:

 - Optimizer changes can effect "peanut-butter" improvements; by making an
   improvement which is small in any particular instance to a code sequence that
   is repeated thousands of times across a codebase, they can produce substantial
   cumulative wins.  These should accrue toward the standard metrics (benchmark
   scores and code size), but identifying the most profitable "peanut-butter"
   opportunities is difficult.  Improving our methodology for identifying such
   opportunities would be helpful; some ideas are below.
 - Optimizer changes can unblock coding patterns that performance-sensitive
   developers want to employ but consider prohibitively expensive.  They may
   have inelegant works-around in their code, such as gotos for loop-exiting
   returns to work around poor block layout, manually scalarized structs to work
   around poor struct promotion, manually unrolled loops to work around lack of
   loop unrolling, limited use of lambdas to work around inefficient access to
   heap-allocated closures, etc.  The more the optimizer can improve such
   situations, the better, as it both increases developer productivity and
   increases the usefulness of abstractions provided by the language and
   libraries.  Finding a measurable metric to track this type of improvement
   poses a challenge, but would be a big help toward prioritizing and validating
   optimization improvements; again, some ideas are below.


Brainstorm
----------

Listed here are several ideas for undertakings we might pursue to improve our
ability to identify opportunities and validate/track improvements that mesh
with the benefits discussed above.  Thinking here is in the early stages, but
the hope is that with some thought/discussion some of these will surface as
worth investing in.

 - Is there telemetry we can implement/analyze to identify "peanut-butter"
   opportunities, or target "coding pattern"s?  Probably easier to use this
   to evaluate/prioritize patterns we're considering targeting than to identify
   the patterns in the first place.
 - Can we construct some sort of "peanut-butter profiler"?  The idea would
   roughly be to aggregate samples/counters under particular input constructs
   rather than aggregate them under callstack.  Might it be interesting to
   group by MSIL opcode, or opcode pair, or opcode triplet... ?
 - It might behoove us to build up some SPMI traces that could be data-mined
   for any of these experiments.
 - We should make it easy to view machine code emitted by the jit, and to
   collect profiles and correlate them with that machine code.  This could
   benefit any developers doing performance analysis of their own code.
   The JIT team has discussed this, options include building something on top of
   the profiler APIs, enabling COMPlus_JitDisasm in release builds, and shipping
   with or making easily available an alt jit that supports JitDisasm.
 - Hardware companies maintain optimization/performance guides for their ISAs.
   Should we maintain one for MSIL and/or C# (and/or F#)?  If we hosted such a
   thing somewhere publicly votable, we could track which anti-patterns people
   find most frustrating to avoid, and subsequent removal of them.  Does such
   a guide already exist somewhere, that we could use as a starting point?
   Should we collate GitHub issues or Stack Overflow issues to create such a thing?
 - Maybe we should expand our labels on GitHub so that there are sub-areas
   within "optimization"?  It could help prioritize by letting us compare the
   relative sizes of those buckets.
 - Can we more effectively leverage the legacy JIT codebases for comparative
   analysis?  We've compared micro-benchmark performance against Jit64 and
   manually compared disassembly of hot code, what else can we do?  One concrete
   idea:  run over some large corpus of code (SPMI?), and do a path-length
   comparison e.g. by looking at each sequence of k MSIL instructions (for some
   small k), and for each combination of k opcodes collect statistics on the
   size of generated machine code (maybe using debug line number info to do the
   correlation?), then look for common sequences which are much longer with
   RyuJIT.
 - Maybe hook RyuJIT up to some sort of superoptimizer to identify opportunities?
 - Microsoft Research has done some experimenting that involved converting RyuJIT
   IR to LLVM IR; perhaps we could use this to identify common expressions that
   could be much better optimized.
 - What's a practical way to establish a metric of "unblocked coding patterns"?
 - How developers give feedback about patterns/performance could use some thought;
   the GitHub issue list is open, but does it need to be publicized somehow?  We
   perhaps should have some regular process where we pull issues over from other
   places where people report/discuss dotnet performance issues, like
   [Stack Overflow](https://stackoverflow.com/questions/tagged/performance+.net).
