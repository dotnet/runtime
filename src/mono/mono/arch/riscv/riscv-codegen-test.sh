#!/usr/bin/env bash

# This can be used to test instruction encodings when cross-compiling.

if grep "#define TARGET_RISCV64 1" ../../../config.h > /dev/null; then
	bits=64
else
	bits=32
fi

gcc -I../../.. -I../../eglib riscv-codegen-test.c -o riscv-codegen-test $@ || exit 1
./riscv-codegen-test > riscv-codegen.s || exit 1
riscv64-unknown-linux-gnu-as riscv-codegen.s -o riscv-codegen.elf || exit 1
riscv64-unknown-linux-gnu-objdump -D -M numeric,no-aliases riscv-codegen.elf > riscv-codegen.res || exit 1
diff -u riscv-codegen.exp${bits} riscv-codegen.res || exit 1
