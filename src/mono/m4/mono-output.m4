# Usage:
# AC_MONO_OUTPUT()
#
# Generates the output files used by Mono.
#
AC_DEFUN([AC_MONO_OUTPUT], [

	AC_OUTPUT([
		Makefile
		llvm/Makefile
		mono/Makefile
		mono/utils/Makefile
		mono/metadata/Makefile
		mono/zlib/Makefile
		mono/eventpipe/Makefile
		mono/eventpipe/test/Makefile
		mono/arch/Makefile
		mono/arch/x86/Makefile
		mono/arch/amd64/Makefile
		mono/arch/ppc/Makefile
		mono/arch/sparc/Makefile
		mono/arch/s390x/Makefile
		mono/arch/arm/Makefile
		mono/arch/arm64/Makefile
		mono/arch/mips/Makefile
		mono/arch/riscv/Makefile
		mono/sgen/Makefile
		mono/mini/Makefile
		mono/profiler/Makefile
		mono/eglib/Makefile
		mono/eglib/eglib-config.h
		mono/eglib/test/Makefile
	])
])
