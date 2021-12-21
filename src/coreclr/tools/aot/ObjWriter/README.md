# Native Object Writer library

This directory contains a native object file writer library based on LLVM. This library exposes C APIs to emit ELF/Mach-O/PE object files.

The implementation is based on LLVM's assembler APIs - these are APIs intended to be consumed by assembly language compilers and are therefore close to the underlying object file formats. When in doubt, look at how the assemblers (e.g. llvm-ml in the tools directory of LLVM) use these APIs.

The build script clones the appropriate version of the LLVM source tree, applies a patch that adds a couple of things we need, and builds the repo with this directory added as an out-of-tree tool.
