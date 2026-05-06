# Proposed Plans for Inlining

This document outlines a plan for improving the inlining capabilities
of RyuJit as well as adding inlining capabilities to LLILC.

## Background

Inlining is a key optimization, directly reducing call overhead and
indirectly exposing a wider scope for powerful intraprocedural
optimizations.

From an implementation standpoint, an inliner has the following aspects:

* Machinery: transformations on the compiler's IR that physically
incorporate the code for the callee into the caller.

* Legality: a set of rules that describe when inlining is legal (that
is, preserves program semantics). Here, the legality rules are largely
dictated to us by the CoreCLR runtime.

* Ability: a set of rules that describe whether the machinery is able
to perform a legal inline and whether the result can be handled by
downstream phases.

* Profitability: a set of heuristic rules that describe which legal
and able inlines are desirable.

The general consensus is that the inlining in RyuJit can be improved,
while carefully maintaining RyuJit's current lean compilation
overhead. It is anticipated that this work will encompass Machinery,
Ability, and Profitability.

LLILC does no inlining today. Since we aspire to have LLILC be a
high-performance .NET code generator, we need to enable inlining in
LLILC. LLILC can likely leverage much of LLVM's built-in inlining
Machinery and Ability, but will need new code for Legality and
Profitability.

We envision various scenarios for .NET Code generation that impact
inlining: a first-tier JIT compiler, a higher-tier JIT compiler, a
fast AOT compiler, and an optimizing AOT compiler. Each scenario calls
for inlining, but the tradeoffs are different. For a given scenario,
we may choose to deploy RyuJit or LLILC or both depending on the
scenario requirements and code generator capabilities.

There are also aspects of inlining that are runtime and target machine
specific. Each runtime and target architecture will require some
specialized tuning to accommodate the different costs of operations at
runtime.

Taking this all into account, the proposed scope of the work
encompasses two different compiler code bases, a number of codegen
scenarios, and a number of target architectures and runtime
conventions. The goal is to come up with one conceptual framework that
covers all these cases and avoids relying too much on time-intensive
manual investigation and tuning.

We will measure the quality of inlining along three axes: the time
spent generating code (aka *throughput* -- TP) abbreviate as TP), the
time spent executing the code (aka *code quality* -- CQ), and the size
of the generated code (CS).

We also desire that the resulting decision-making machinery (in particular
the Profitability heuristic) is expressed in terms that are sensible to
compiler writers.

## The Proposal

### Machinery

RyuJit's machinery may or may not require updates depending on how many
key inlines are rejected because of lack of ability. This is something
that requires further investigation. We may also decide to pursue
enhancements here to ensure that in cascaded inline cases the deeper
callsites are provided up-to-date information.

LLILC should be able to leverage LLVM's existing inline machinery. In
JIT-like scenarios, LLILC's reader will need some updates to properly
handle cases where a method is being "read" as an inline candidate
rather than a root method to compiled. For AOT scenarios we ultimately
will want to do something more elaborate where all of the methods
being compiled are represented as LLVM IR.

### Legality

As mentioned above, the legality constraints are generally imposed by
the CoreCLR runtime.

These constraints are already captured in the RyuJit source
code. However, we generally prefer to at least logically perform all
the legality checks before any of ability or profitability checks, so
that we can reason more simply about why a particular inline didn't
happen. This may not be entirely practical, since some of the
profitability or ability early outs may be helping TP by keeping
RyuJit from doing a lot of exploratory work before ultimately
rejecting an inline candidate.

LLILC lacks legality checking, and this needs to be implemented.

### Ability

RyuJit currently bails out of some legal inlining cases because of
internal restrictions. For instance, if a callee modifies one of its
parameters, RyuJit won't inline it because of limitations in how the
IR is incorporated. We expect to work on reducing or removing key
ability limiters in RyuJit.

Assuming the LLVM inlining framework is robust, LLILC shouldn't have
too many ability constraints.

### Profitability

Given the wide range of scenarios, targets, and the two distinct code
bases, we propose to heavily rely on machine learning techniques for
building and tuning the Profitability components of the inliners.

The general approach we suggest is based on past experiences we've had
developing inliners for various compilers and is inspired by the CGO
paper [Automatic Construction of Inlining Heuristics using Machine
Learning](http://dl.acm.org/citation.cfm?id=2495914) by Kulkarni,
Cavazos, Wimmer, and Simon.

Kulkarni et. al. treat profitability as an unsupervised learning
problem, and create a well-performing heuristic black box (neural
network) using evolutionary programming techniques. They then turn
around and use this black box as an oracle to label inline instances,
and from this guide a supervised machine learning algorithm to produce
a decision tree that expresses the profitability heuristics in terms
sensible to the compiler writer.

The inputs to the heuristic black box are various facts and estimates
about an inline candidate, based on observations of the caller, callee,
and call site (collectively, *features*). These features may be booleans,
enumerates (categories), integers, or floating-point values. For example:

* (boolean) `CalleeIsLeaf`, `CallerSiteInTry`
* (enumerate) `CalleeReturnCorType`
* (int) `CalleeILSize`,`CallSiteProfileWeight`
* (float) `CalleeLoadStoreFrequency`

The output is a yes/no inlining decision for a particular candidate.

The evolutionary process is governed by a fitness metric that expresses the
appropriate balance of TP, CQ, and CS for the scenario. For instance, in a JIT
scenario we likely will want to keep TP and CS close to the current
baseline values. For AOT we may want to allow higher TP and larger CS if we
can demonstrate CQ improvements.

## Implications

For this approach to work, we will need to make frequent and robust
measurements of the key fitness metrics across a range of
representative benchmarks. So this presumes:

* we can identify and capture these key benchmarks
* we can develop methodology for doing appropriate measurements
* we can do these measurements at appropriate scale

### The Benchmarks

Clearly in a machine learning approach the actual observations made
are crucial. We need a solid, representative benchmark set. But this
is a general problem with a heuristic based optimization and not an
inherent limitation of this approach -- one can only measure what one
can measure.

We plan on capturing the results in standard schema and hope that with
this and the regularization of the metric data (see next section) that
interested 3rd parties can perhaps supply their own benchmark results.

### The Metrics

CS is typically very easy to measure and the measurements are "noise
free". So the measurement only needs to be done once.

If TP and CQ are both measured in terms of time (or cycles) then the
measurements will be inherently noisy. To reduce the variance caused
by noise some repeated runs may be necessary, and it might also be
necessary to run on dedicated, matched hardware. This severely limits
scalability.

We instead propose to measure both TP and CQ in terms of instructions
retired. While this may not be entirely noise-free, we expect the
measurements to be low noise and fairly insensitive to other
computations on the machine. Thus this should give us decent scale
(for instance we can run scenarios in the cloud on VMs with varying
underlying hardware, etc).

For TP, instructions retired should be a reasonable proxy for time,
since we expect the CPI for the compiler proper to be relatively
static. For CQ, we realize there may not be as strong a correlation
with time, but typically the inliner operates early in the compilation
process and inlining is mainly focused on reducing instruction count
and less about reducing cycle stalls.

We'll do some experimentation early on to see how well these proxy
metrics hold up.

### Analysis

To help prioritize work on profitability and ability (and perhaps find
areas where we might discuss trying to remove legality constraints),
we need some mechanism to correlate inlining outcomes with success and
failure reasons.

We propose to develop a textual, machine-readable inline log schema
that describes a set of inlining decisions. This schema can be
produced by a compiler as it makes decisions, and contains sufficient
correlating data so that we can then match it up with runtime
observations. For example, we might end up with the following sort of
per-method output (though more likely it will be embedded in some kind of
xml or json markup):

```
  Inlining for 9811 Lookup
     [o] 22825 Lookup@Lattice
        [o] 22827 ??0?$interior_ptr@PAVCell
        [x] 22826 Lookup@Lattice@@@Z (reason: SizeLimit)
     [o] 21728 ?get_CellList

```

where `[o]` is a successful inline, `[x]` a failed inline, and
indentation shows the inlining tree. For .NET compilation we'll need
some kind of persistent ID for methods, which may not be all that easy
to come by.

This inline log can also be read back by the code generator to enable
*inline replay* and force the inliner to perform a particular pattern
of inlines. This is useful for things like performance investigations,
bug isolation, and for trying to have one compiler emulate the
inlining behavior of another. Since the logs are textual they can also
be edited by hand if necessary to perform experiments.

### Obtaining Runtime Data

If we can obtain runtime data about the frequency of call
instructions, we can run this through the log to accumulate failure
reasons and create a failure reason table like the following
(categories and data here totally made up):

```
  FailedReason       | Count    | [%]   |  Sites |   [%]
---------------------|----------|-------|--------|------
SpeculativeNonGround | 79347955 | 37.8% |    417 | 14.1%
NonGroundShared      | 28756634 | 13.7% |    217 | 07.4%
SizeLimit            | 22066033 | 10.5% |    760 | 25.8%
ExternFunction       | 21923402 | 10.5% |    300 | 10.2%
... etc ...
```

This can be used to prioritize inlining work. For instance, if
`SpeculativeNonGround` was an ability issue, and we wanted to improve
CQ, we'd give it high priority to fix, since it impacts inlining most
frequently.

### Robustness

Since we'll likely be altering inlining decisions, we may uncover bugs
or performance blemishes in the compilers.

It may make sense to build up a random inlining methodology (RyuJit
evidently has something like this) to try and stress test the compiler
in the face of inlining changes. Log replay can be used with automatic
bisection tools to try and isolate the particular inline that exposes
a bug.

Performance issues are harder to spot automatically, and will likely
require some amount of manual analysis. However, we may be able to study
inline instances that are not well-predicted by the inlining models -- for
instance, cases where the CS is much greater than predicted, or CQ is lower --
and see if the modelling shortfall is an indicator of problems within the
compiler itself.

## Development Stages

Given the above, here's a rough draft outline for the how the
functionality could be built.

### Preliminaries -- Measurement

1. Make some benchmarks available for measurement.

2. Demonstrate we can reliably measure the metrics of interest: CS,
CQ, and TP (as bytes, instructions, and instructions) when jitting and
pre-jitting the benchmarks. Get some idea of the noise levels for CQ
and TP. Hopefully they are small.

### Preliminaries -- RyuJit

1. Refactor RyuJit code to clearly separate out legality, ability,
and profitability aspects of the inliner. Hopefully this can be done
with no CQ changes and no TP impact.

2. Develop the inline log format.

3. Produce inline logs from RyuJit.

4. Develop methodology for runtime correlation.

5. Produce failure reason analysis for the benchmark set.  Decide
which if any abilities need improving.

6. Implement inline log replay for RyuJit.

7. Modify logs by hand or via a tool and replay them to assess the
landscape of TP/CQ/CS tradeoffs from inliner changes.

### Preliminaries -- LLILC

1. Add code to capture legality constraints when reading in a method.

2. Generalize LLILC's MSIL reader to read in an inlinee in
the context of the caller.

3. Write a simple LLVM pass (or specialize an existing pass) to
perform inlining. Initially just implement a very simple profitability
heuristic. Verify it all works correctly.

4. Implement inline log generation by LLILC.

5. Implement inline log replay by LLILC.

6. Modify logs by hand or via a tool and replay them to assess the
landscape of TP/CQ/CS tradeoffs from inliner changes.

### Develop the Heuristics (JIT case)

1. Start by trying to produce code size estimates for a given inline
candidate. Measure actual code size impacts for individual inlining
decisions.

2. Determine features to surface and produce a labelled data set of
(feature*, size impact). Use this to build a machine-learning model
estimator for code size.

3. We probably can't just plug the new size estimate into the existing
heuristic framework and expect good results (generally, better
information in does not always result in better decisions out).  So
instead build up a parallel (off by default) heuristic path where we
use the new size estimator.

4. Surface features for the CQ and TP impacts. Build a pluggable model
for the black box heuristic in the compiler. Likely this means that
the model code is written by a tool from some textual description.

5. Run evolutionary experiments to produce an optimized black box
heuristic. This involves: (a) randomly or otherwise generating an
initial population of models; (b) running each model on the
benchmarks, gathering metrics; (c) combining metrics across benchmarks
to produce fitness; (d) using fitness to create a new set of models
using various genetic approaches; (e) iterating until satisfied with
the result.

All of this setup should be automated and available in the CI system.

6. Use the winning model to produce a labelled data set of (feature*,
inlining decision) for all the cases in the benchmarks. Build a
decision tree from this via standard techniques. Decide if any pruning
is appropriate.

7. Back validate the results via measurement to show (a) final heuristic
works as expected and metrics match expectations on training examples;
cross validate on untrained benchmarks to show results are not over-fitted.
Flight this to various 3rd parties for independent confirmation.

8. Enable new heuristics as defaults.

9. Repeat the above process for each code base, scenario,
architecture, etc.

### AOT Considerations

In the AOT case (particularly the optimized AOT) we will likely need
to develop a different set of heuristics.

The current frameworks for AOT (NGEN and Ready2Run) give the code
generator the opportunity to preview the methods to be compiled. We'll
take advantage of that to build a call graph and orchestrate
compilation of methods in a "bottom-up" fashion (callees before
callers). Because of that, when inlining, the caller typically has the
opportunity to look at a richer set of data about the callee -- for
instance, detailed usage summary for parameters and similar.

The inliner also has the opportunity to plan inlining over a wider
scope, perhaps adjusting heuristics for the relative importance of
callers or the shape of the call graph or similar.

## Vetting and Revalidation

With an evaluation framework in place, we can now run carefully
controlled experiments to see if further enhancements to the inliner
are warranted. For example, the order in which candidates are
considered may have an impact -- ideally the inliner would greedily
look at the best candidates first. Other feature data may prove
important, for instance actual or synthetic profile data.

The rest of the compiler also changes over time. Periodically we
should rerun the heuristic development to see if the current set of
heuristics are still good, especially if major new capabilities are
added to the compiler.

We should also continually strive to improve the benchmark set.

## Risks

The plan calls for the inliner's Profitability logic to be constructed
via machine learning. This exposes us to a number of risks:

* We may not understand how to successfully apply ML techniques.
* We may run into one of the common ML pitfalls, like having too many
features, or strongly correlated features.
* We may not be using the right features.
* Some of the features may themselves be estimates (eg code size).
* The benchmark set may not be sufficiently representative.
* We may overfit the benchmark set. The plan here is to cross-validate and
not use all our benchmarks as training examples.
* The training process may be too slow to be practical.
* There may be too much measurement noise.
* The resulting models may not be stable -- meaning that small changes in
the input programs might trigger large changes in the inlining decisions.
* The supervised learning output may still be black-box like in practice.
* The downstream phases of the compiler might not adapt well to inlining
changes or may offer an uneven performance surface.
* We've found it tricky in the past to produce a good combined fitness metric
from a large set of benchmarks and metrics.
* It's generally not possible to build a new heuristic which is better in
every way than the old one -- some metrics on some benchmarks will inevitably
regress.

However, it's worth pointing out that a manually constructed heuristic faces
many of these same risks.

## Notes

See this [early
draft](https://github.com/AndyAyersMS/coreclr/commit/035054402a345f643d9dee0ec31dbdf5fadbb17c),
now orphaned by a squash, for some interesting comments.
