Contents
--------

| Name             | Description                                                    |
|:-----------------|:---------------------------------------------------------------|
| arch/            | Architecture-specific code                                     |
| doc/             | Documentation for formats and algorithms                       |
| test/example.c   | Zlib usages examples for build testing                         |
| test/minigzip.c  | Minimal gzip-like functionality for build testing              |
| test/infcover.c  | Inflate code coverage for build testing                        |
| win32/           | Shared library version resources for Windows                   |
| CMakeLists.txt   | Cmake build script                                             |
| configure        | Bash configure/build script                                    |
| adler32.c        | Compute the Adler-32 checksum of a data stream                 |
| chunkset.*       | Inline functions to copy small data chunks                     |
| compress.c       | Compress a memory buffer                                       |
| deflate.*        | Compress data using the deflate algorithm                      |
| deflate_fast.c   | Compress data using the deflate algorithm with fast strategy   |
| deflate_medium.c | Compress data using the deflate algorithm with medium strategy |
| deflate_slow.c   | Compress data using the deflate algorithm with slow strategy   |
| functable.*      | Struct containing function pointers to optimized functions     |
| gzguts.h         | Internal definitions for gzip operations                       |
| gzlib.c          | Functions common to reading and writing gzip files             |
| gzread.c         | Read gzip files                                                |
| gzwrite.c        | Write gzip files                                               |
| infback.*        | Inflate using a callback interface                             |
| inflate.*        | Decompress data                                                |
| inffast.*        | Decompress data with speed optimizations                       |
| inffixed_tbl.h   | Table for decoding fixed codes                                 |
| inftrees.h       | Generate Huffman trees for efficient decoding                  |
| trees.*          | Output deflated data using Huffman coding                      |
| uncompr.c        | Decompress a memory buffer                                     |
| zconf.h.cmakein  | zconf.h template for cmake                                     |
| zendian.h        | BYTE_ORDER for endian tests                                    |
| zlib.map         | Linux symbol information                                       |
| zlib.pc.in       | Pkg-config template                                            |
