# Introduction

This directory contains SystemZ deflate hardware acceleration support.
It can be enabled using the following build commands:

    $ ./configure --with-dfltcc-deflate --with-dfltcc-inflate
    $ make

or

    $ cmake -DWITH_DFLTCC_DEFLATE=1 -DWITH_DFLTCC_INFLATE=1 .
    $ make

When built like this, zlib-ng would compress using hardware on level 1,
and using software on all other levels. Decompression will always happen
in hardware. In order to enable hardware compression for levels 1-6
(i.e. to make it used by default) one could add
`-DDFLTCC_LEVEL_MASK=0x7e` to CFLAGS when building zlib-ng.

SystemZ deflate hardware acceleration is available on [IBM z15](
https://www.ibm.com/products/z15) and newer machines under the name [
"Integrated Accelerator for zEnterprise Data Compression"](
https://www.ibm.com/support/z-content-solutions/compression/). The
programming interface to it is a machine instruction called DEFLATE
CONVERSION CALL (DFLTCC). It is documented in Chapter 26 of [Principles
of Operation](https://publibfp.dhe.ibm.com/epubs/pdf/a227832c.pdf). Both
the code and the rest of this document refer to this feature simply as
"DFLTCC".

# Performance

Performance figures are published [here](
https://github.com/iii-i/zlib-ng/wiki/Performance-with-dfltcc-patch-applied-and-dfltcc-support-built-on-dfltcc-enabled-machine
). The compression speed-up can be as high as 110x and the decompression
speed-up can be as high as 15x.

# Limitations

Two DFLTCC compression calls with identical inputs are not guaranteed to
produce identical outputs. Therefore care should be taken when using
hardware compression when reproducible results are desired. In
particular, zlib-ng-specific `zng_deflateSetParams` call allows setting
`Z_DEFLATE_REPRODUCIBLE` parameter, which disables DFLTCC support for a
particular stream.

DFLTCC does not support every single zlib-ng feature, in particular:

* `inflate(Z_BLOCK)` and `inflate(Z_TREES)`
* `inflateMark()`
* `inflatePrime()`
* `inflateSyncPoint()`

When used, these functions will either switch to software, or, in case
this is not possible, gracefully fail.

# Code structure

All SystemZ-specific code lives in `arch/s390` directory and is
integrated with the rest of zlib-ng using hook macros.

## Hook macros

DFLTCC takes as arguments a parameter block, an input buffer, an output
buffer and a window. `ZALLOC_DEFLATE_STATE()`, `ZALLOC_INFLATE_STATE()`,
`ZFREE_STATE()`, `ZCOPY_DEFLATE_STATE()`, `ZCOPY_INFLATE_STATE()`,
`ZALLOC_WINDOW()`, `ZCOPY_WINDOW()` and `TRY_FREE_WINDOW()` macros encapsulate
allocation  details for the parameter block (which is allocated alongside
zlib-ng state) and the window (which must be page-aligned and large enough).

Software and hardware window formats do not match, therefore,
`deflateSetDictionary()`, `deflateGetDictionary()`, `inflateSetDictionary()`
and `inflateGetDictionary()` need special handling, which is triggered using
`DEFLATE_SET_DICTIONARY_HOOK()`, `DEFLATE_GET_DICTIONARY_HOOK()`,
`INFLATE_SET_DICTIONARY_HOOK()` and `INFLATE_GET_DICTIONARY_HOOK()` macros.

`deflateResetKeep()` and `inflateResetKeep()` update the DFLTCC
parameter block using `DEFLATE_RESET_KEEP_HOOK()` and
`INFLATE_RESET_KEEP_HOOK()` macros.

`INFLATE_PRIME_HOOK()`, `INFLATE_MARK_HOOK()` and
`INFLATE_SYNC_POINT_HOOK()` macros make the respective unsupported
calls gracefully fail.

`DEFLATE_PARAMS_HOOK()` implements switching between hardware and
software compression mid-stream using `deflateParams()`. Switching
normally entails flushing the current block, which might not be possible
in low memory situations. `deflateParams()` uses `DEFLATE_DONE()` hook
in order to detect and gracefully handle such situations.

The algorithm implemented in hardware has different compression ratio
than the one implemented in software. `DEFLATE_BOUND_ADJUST_COMPLEN()`
and `DEFLATE_NEED_CONSERVATIVE_BOUND()` macros make `deflateBound()`
return the correct results for the hardware implementation.

Actual compression and decompression are handled by `DEFLATE_HOOK()` and
`INFLATE_TYPEDO_HOOK()` macros. Since inflation with DFLTCC manages the
window on its own, calling `updatewindow()` is suppressed using
`INFLATE_NEED_UPDATEWINDOW()` macro.

In addition to compression, DFLTCC computes CRC-32 and Adler-32
checksums, therefore, whenever it's used, software checksumming is
suppressed using `DEFLATE_NEED_CHECKSUM()` and `INFLATE_NEED_CHECKSUM()`
macros.

While software always produces reproducible compression results, this
is not the case for DFLTCC. Therefore, zlib-ng users are given the
ability to specify whether or not reproducible compression results
are required. While it is always possible to specify this setting
before the compression begins, it is not always possible to do so in
the middle of a deflate stream - the exact conditions for that are
determined by `DEFLATE_CAN_SET_REPRODUCIBLE()` macro.

## SystemZ-specific code

When zlib-ng is built with DFLTCC, the hooks described above are
converted to calls to functions, which are implemented in
`arch/s390/dfltcc_*` files. The functions can be grouped in three broad
categories:

* Base DFLTCC support, e.g. wrapping the machine instruction -
  `dfltcc()` and allocating aligned memory - `dfltcc_alloc_state()`.
* Translating between software and hardware data formats, e.g.
  `dfltcc_deflate_set_dictionary()`.
* Translating between software and hardware state machines, e.g.
  `dfltcc_deflate()` and `dfltcc_inflate()`.

The functions from the first two categories are fairly simple, however,
various quirks in both software and hardware state machines make the
functions from the third category quite complicated.

### `dfltcc_deflate()` function

This function is called by `deflate()` and has the following
responsibilities:

* Checking whether DFLTCC can be used with the current stream. If this
  is not the case, then it returns `0`, making `deflate()` use some
  other function in order to compress in software. Otherwise it returns
  `1`.
* Block management and Huffman table generation. DFLTCC ends blocks only
  when explicitly instructed to do so by the software. Furthermore,
  whether to use fixed or dynamic Huffman tables must also be determined
  by the software. Since looking at data in order to gather statistics
  would negate performance benefits, the following approach is used: the
  first `DFLTCC_FIRST_FHT_BLOCK_SIZE` bytes are placed into a fixed
  block, and every next `DFLTCC_BLOCK_SIZE` bytes are placed into
  dynamic blocks.
* Writing EOBS. Block Closing Control bit in the parameter block
  instructs DFLTCC to write EOBS, however, certain conditions need to be
  met: input data length must be non-zero or Continuation Flag must be
  set. To put this in simpler terms, DFLTCC will silently refuse to
  write EOBS if this is the only thing that it is asked to do. Since the
  code has to be able to emit EOBS in software anyway, in order to avoid
  tricky corner cases Block Closing Control is never used. Whether to
  write EOBS is instead controlled by `soft_bcc` variable.
* Triggering block post-processing. Depending on flush mode, `deflate()`
  must perform various additional actions when a block or a stream ends.
  `dfltcc_deflate()` informs `deflate()` about this using
  `block_state *result` parameter.
* Converting software state fields into hardware parameter block fields,
  and vice versa. For example, `wrap` and Check Value Type or `bi_valid`
  and Sub-Byte Boundary. Certain fields cannot be translated and must
  persist untouched in the parameter block between calls, for example,
  Continuation Flag or Continuation State Buffer.
* Handling flush modes and low-memory situations. These aspects are
  quite intertwined and pervasive. The general idea here is that the
  code must not do anything in software - whether explicitly by e.g.
  calling `send_eobs()`, or implicitly - by returning to `deflate()`
  with certain return and `*result` values, when Continuation Flag is
  set.
* Ending streams. When a new block is started and flush mode is
  `Z_FINISH`, Block Header Final parameter block bit is used to mark
  this block as final. However, sometimes an empty final block is
  needed, and, unfortunately, just like with EOBS, DFLTCC will silently
  refuse to do this. The general idea of DFLTCC implementation is to
  rely as much as possible on the existing code. Here in order to do
  this, the code pretends that it does not support DFLTCC, which makes
  `deflate()` call a software compression function, which writes an
  empty final block. Whether this is required is controlled by
  `need_empty_block` variable.
* Error handling. This is simply converting
  Operation-Ending-Supplemental Code to string. Errors can only happen
  due to things like memory corruption, and therefore they don't affect
  the `deflate()` return code.

### `dfltcc_inflate()` function

This function is called by `inflate()` from the `TYPEDO` state (that is,
when all the metadata is parsed and the stream is positioned at the type
bits of deflate block header) and it's responsible for the following:

* Falling back to software when flush mode is `Z_BLOCK` or `Z_TREES`.
  Unfortunately, there is no way to ask DFLTCC to stop decompressing on
  block or tree boundary.
* `inflate()` decompression loop management. This is controlled using
  the return value, which can be either `DFLTCC_INFLATE_BREAK` or
  `DFLTCC_INFLATE_CONTINUE`.
* Converting software state fields into hardware parameter block fields,
  and vice versa. For example, `whave` and History Length or `wnext` and
  History Offset.
* Ending streams. This instructs `inflate()` to return `Z_STREAM_END`
  and is controlled by `last` state field.
* Error handling. Like deflate, error handling comprises
  Operation-Ending-Supplemental Code to string conversion. Unlike
  deflate, errors may happen due to bad inputs, therefore they are
  propagated to `inflate()` by setting `mode` field to `MEM` or `BAD`.

# Testing

Given complexity of DFLTCC machine instruction, it is not clear whether
QEMU TCG will ever support it. At the time of writing, one has to have
access to an IBM z15+ VM or LPAR in order to test DFLTCC support. Since
DFLTCC is a non-privileged instruction, neither special VM/LPAR
configuration nor root are required.

zlib-ng CI uses an IBM-provided z15 self-hosted builder for the DFLTCC
testing. There are no IBM Z builds of GitHub Actions runner, and
stable qemu-user has problems with .NET apps, so the builder runs the
x86_64 runner version with qemu-user built from the master branch.

## Configuring the builder.

### Install prerequisites.

```
$ sudo dnf install docker
```

### Add services.

```
$ sudo cp self-hosted-builder/*.service /etc/systemd/system/
$ sudo systemctl daemon-reload
```

### Create a config file.

```
$ sudo tee /etc/actions-runner
repo=<owner>/<name>
access_token=<ghp_***>
```

Access token should have the repo scope, consult
https://docs.github.com/en/rest/reference/actions#create-a-registration-token-for-a-repository
for details.

### Autostart the x86_64 emulation support.

```
$ sudo systemctl enable --now qemu-user-static
```

### Autostart the runner.

```
$ sudo systemctl enable --now actions-runner
```

## Rebuilding the image

In order to update the `iiilinuxibmcom/actions-runner` image, e.g. to get the
latest OS security fixes, use the following commands:

```
$ sudo docker build \
      --pull \
      -f self-hosted-builder/actions-runner.Dockerfile \
      -t iiilinuxibmcom/actions-runner
$ sudo systemctl restart actions-runner
```

## Removing persistent data

The `actions-runner` service stores various temporary data, such as runner
registration information, work directories and logs, in the `actions-runner`
volume. In order to remove it and start from scratch, e.g. when switching the
runner to a different repository, use the following commands:

```
$ sudo systemctl stop actions-runner
$ sudo docker rm -f actions-runner
$ sudo docker volume rm actions-runner
```
