## Benchmarks
These benchmarks are written using [Google Benchmark](https://github.com/google/benchmark).

*Repetitions*

To increase the number of times each benchmark iteration is run use:

```
--benchmark_repetitions=20
```

*Filters*

To filter out which benchmarks are performed use:

```
--benchmark_filter="adler32*"
```

There are two different benchmarks, micro and macro.

### Benchmark benchmark_zlib
These are microbenchmarks intended to test lower level subfunctions of the library.

Benchmarks include implementations of:
    - Adler32
    - CRC
    - 256 byte comparisons
    - SIMD accelerated "slide hash" routine

By default these benchmarks report things on the nanosecond scale and are small enough
to measure very minute differences.

### Benchmark benchmark_zlib_apps
These benchmarks measure applications of zlib as a whole.  Currently the only examples
are PNG encoding and decoding. The PNG encode and decode tests leveraging procedurally
generated and highly compressible image data.

Additionally, a test called `png_decode_realistic` that will decode any RGB 8 BPP encoded
set of PNGs in the working directory under a directory named "test_pngs" with files named
{0..1}.png. If these images do not exist, they will error out and the benchmark will move
on to the next set of benchmarks.

*benchmark_zlib_apps_alt*

The user can compile a comparison benchmark application linking to any zlib-compatible
implementation of his or her choosing.
