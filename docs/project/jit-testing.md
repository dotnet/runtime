# JIT Testing

We would like to ensure that the CoreCLR contains sufficient test collateral
and tooling to enable high-quality contributions to RyuJit or LLILC's JIT.

JIT testing is somewhat specialized and can't rely solely on the general
framework tests or end to end application tests.

This document describes some of the work needed to bring JIT existing tests and
technology into the CoreCLR, and touches on some areas as that open for
innovation.

We expect to evolve this document into a road map for the overall JIT testing
effort, and to spawn a set of issues in the CoreCLR and LLILC repos for
implementing the needed capabilities.

## Requirements and Assumptions

1. It must be easy to add new tests.
2. Tests must execute with high throughput. We anticipate needing to run
thousands of tests to provide baseline level testing for JIT changes.
3. Tests should generally run on all supported/future chip architectures and
all OS platforms.
4. Tests must be partitionable so CI latency is tolerable (test latency goal
TBD).
5. Tests in CI can be run on private changes (currently tied to PRs; this may
be sufficient).
6. Test strategy harmonious with other .NET repo test strategies.
7. Test harness behaves reasonably on test failure. Easy to get at repro steps
for subsequent debugging.
8. Tests must allow fine-grained inspection of JIT outputs, for instance
comparing the generated code versus a baseline JIT.
9. Tests must support collection of various quantitative measurements, eg time
spent in the JIT, memory used by the JIT, etc.
10. For now, JIT test assets belong in the CoreCLR repo.
11. JIT tests use the same basic test xunit harness as existing CoreCLR tests.
12. JIT special-needs testing will rely on extensions/hooks. Examples below.

## Tasks

Below are some broad task areas that we should consider as part of this plan.
It seems sensible for Microsoft to focus on opening up the JIT self-host
(aka JITSH) tests first. A few other tasks are also Microsoft specific and are
marked with (MS) below.

Other than that the priority, task list, and possibly assignments are open to
discussion.

### (MS) Bring up equivalent of the JITSH tests

JITSH is a set of roughly 8000 tests that have been traditionally used by
Microsoft JIT developers as the frontline JIT test suite.

We'll need to subset these tests for various reasons:

1. Some have shallow desktop CLR dependence (e.g. missing cases in string
formatting).
2. Some have deep desktop CLR dependence (testing a desktop CLR feature that
is not present in CoreCLR).
3. Some require tools not yet available in CoreCLR (ilasm in particular).
4. Some test windows features and won't be relevant to other OS platforms.
5. Some tests may not be able to be freely redistributed.

We have done an internal inventory and identified roughly 1000 tests that
should be straightforward to port into CoreCLR, and have already started in on
moving these.

### Test script capabilities

We need to ensure that the CoreCLR repo contains a suitably
hookable test script. Core testing is driven by xunit but there's typically a
wrapper around this (run.cmd today) to facilitate test execution.

The proposal is to implement platform-neutral variant of run.cmd that
contains all the existing functionality plus some additional capabilities for
JIT testing. Initially this will mean:

1. Ability to execute tests with a JIT specified by the user (either as alt
JIT or as the only JIT)
2. Ability to pass options through to the JIT (eg for dumping assembly or IR)
or to the CoreCLR (eg to disable use of ngen images).

### Cache prebuilt test assets

In general we want JIT tests to be built from sources. But given the volume
of tests it can take a significant amount of time to compile those sources into
assemblies. This in turn slows down the ability to test the JIT.

Given the volume of tests, we might reach a point where the default CoreCLR
build does not build all the tests.

So it would be good if there was a regularly scheduled build of CoreCLR that
would prebuild a matching set of tests and make them available.

### Round out JITSH suite, filling in missing pieces

We need some way to run ILASM. Some suggestions here are to port the existing
ILASM or find some equivalent we could run instead. We could also prebuild
IL based tests and deploy as a package. Around 2400 JITSH tests are blocked by
this.

There are also some VB tests which presumably can be brought over now that VB
projects can build.

Native/interop tests may or may not require platform-specific adaption.

### (MS) Port the devBVT tests.

devBVT is a broader part of CLR SelfHost that is useful for second-tier testing.
Not yet clear what porting this entails.

### Leverage peer repo test suites.

We should be able to directly leverage tests provided in peer repo suites, once
they can run on top of CoreCLR. In particular CoreFx and Roslyn test cases
could be good initial targets.

Note LLILC is currently working through the remaining issues that prevent it
from being able to compile all of Roslyn. See the "needed for Roslyn" tags
on the open LLILC issues.

### Look for other CoreCLR hosted projects.

Similar to the above, as other projects are able to host on CoreCLR we can
potentially use their tests for JIT testing.

### Porting of existing suites/tools over to our repos.

Tools developed to test JVM Jits might be interesting to port over to .Net.
Suggestions for best practices or effective techniques are welcome.

### Bring up quantitative measurements.

For Jit testing we'll need various quantitative assessments of Jit behavior:

1. Time spent jitting
2. Speed of jitted code
3. Size of jitted code
4. Memory utilization by the jit (+ leak detection)
5. Debug info fidelity
6. Coverage ?

There will likely be work going on elsewhere to address some of these same
measurement capabilities, so we should make sure to keep it all in sync.

### Bring up alternate codegen capabilities.

For LLILC, implementing support for crossgen would provide the ability to drive
lots of IL through the JIT. There is enough similarity between the JIT and
crossgen paths that this would likely surface issues in both.

Alternatively one can imagine simple test drivers that load up assemblies and
use reflection to enumerate methods and asks for method bodies to force the JIT
to generate code for all the methods.

### Bring up stress testing

The value of existing test assets can be leveraged through various stress
testing modes. These modes use non-standard code generation or runtime
mechanisms to try an flush out bugs.

1. GC stress. Here the runtime will GC with much higher frequency in an attempt
to maximize the dependence on the GC info reported by the JIT.
2. Internal modes in the JIT to try and flush out bugs, eg randomized inlining,
register allocation stress, volatile stress, randomized block layout, etc.

### Bring up custom testing frameworks and tools.

We should invest in things like random program or IL generation tools.
