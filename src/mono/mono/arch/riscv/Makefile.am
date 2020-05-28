AM_CPPFLAGS = $(GLIB_CFLAGS)

EXTRA_DIST = riscv-codegen.h

bin_PROGRAMS = riscv-codegen-test

riscv_codegen_test_SOURCES = riscv-codegen-test.c

if RISCV64
bits = 64
else
bits = 32
endif

check-local: riscv-codegen-test
	./riscv-codegen-test > riscv-codegen.s
	$(AS) riscv-codegen.s -o riscv-codegen.elf
	$(OBJDUMP) -D -M numeric,no-aliases riscv-codegen.elf > riscv-codegen.res
	diff -u riscv-codegen.exp$(bits) riscv-codegen.res
