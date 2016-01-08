The benchmarks in these sub-directories are based on

   The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/ 

See the adjoining LICENSE.TXT file for license terms.

Our intention with these tests is to provide interesting test cases
for jit developers to use in daily development practice -- not to
produce variants that give the maximum possible performance.

The benchmarks have been modified to fit into the CoreCLR test and
performance test framework, as follows:

 - adding validity checks to ensure optimizers correctly optimize 
   and do not remove computation
 - adding return codes to main based on those checks
 - adding an xunit-performance entry point, and adjusting work so each
   xunit-performance iteration is approximately 1 second on modern x64
   hardware
 - reducing verbosity when run as a benchmark
 - reformatting (via the codeformatter tool)

These benchmarks are just a subset of the benchmarks available in C# from
the Benchmarks Game site. We've selected variants that do not rely on
multiple threads to ensure relative benchmark stability across a
variety of machines. 

We've excluded two benchmarks that are inherently multitheaded:
chamenosredux and threadring. We may revisit this as we improve our
ability to harness threading tests in a stable way.

We've also excluded benchmarks that read in large input files:
knucleotide, regexdna, revcomp.



