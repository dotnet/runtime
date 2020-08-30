The benchmarks in these sub-directories are based on

   The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/ 

See the adjoining LICENSE.TXT file for license terms.

Our intention with these tests is twofold:
 1 - To track .NET Core's performance on these benchmarks in the
     same benchmarking system used for other internal .NET Core
     performance benchmarks.
 2 - To make these available for daily JIT (and runtime) development,
     as a factor in assessing the performance impact of compiler
     (and runtime) changes.

The benchmarks have been modified to fit into the CoreCLR test and
performance test framework, as follows:

 - adding validity checks to ensure optimizers correctly optimize 
   and do not remove computation
 - adding return codes to main based on those checks
 - adding an xunit-performance entry point, and adjusting work so each
   xunit-performance iteration is approximately 1 second on modern x64
   hardware
 - reducing verbosity when run as a benchmark
 - calling different APIs in a few places to allow compiling against
   netstandard1.4
 - reformatting
 - in the case of pidigits, implementing on top of .NET's BigInteger
   type rather than p/invokes to the native GMP library

These benchmarks are just a subset of the benchmarks available in C# from
the Benchmarks Game site. The highest-scoring C# .NET Core variant of each
benchmark is included, and in the (common) case of benchmarks where the
best-scoring variant uses multiple threads, we've also selected variants
that do not rely on multiple threads, to ensure relative benchmark stability
across a variety of machines.
