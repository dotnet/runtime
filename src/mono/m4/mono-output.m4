# Usage:
# AC_MONO_OUTPUT()
#
# Generates the output files used by Mono.
#
AC_DEFUN([AC_MONO_OUTPUT], [

	AC_CONFIG_FILES([po/mcs/Makefile.in])
	AC_CONFIG_FILES([acceptance-tests/microbench-perf.sh], [chmod +x acceptance-tests/microbench-perf.sh])
	AC_CONFIG_FILES([runtime/mono-wrapper],                [chmod +x runtime/mono-wrapper])
	AC_CONFIG_FILES([runtime/monodis-wrapper],             [chmod +x runtime/monodis-wrapper])
	AC_CONFIG_FILES([runtime/bin/mono-hang-watchdog],      [chmod +x runtime/bin/mono-hang-watchdog])

	AC_OUTPUT([
		Makefile
		llvm/Makefile
		mk/Makefile
		mono/Makefile
		mono/btls/Makefile
		mono/native/Makefile
		mono/utils/Makefile
		mono/metadata/Makefile
		mono/zlib/Makefile
		mono/dis/Makefile
		mono/cil/Makefile
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
		mono/native/platform-type.c
		mono/native/platform-type-compat.c
		mono/native/platform-type-unified.c
		mono/tests/Makefile
		mono/tests/assembly-load-reference/Makefile
		mono/tests/tests-config
		mono/tests/gc-descriptors/Makefile
		mono/tests/testing_gac/Makefile
		mono/tests/fullaot-mixed/Makefile
		mono/tests/llvmonly-mixed/Makefile
		mono/unit-tests/Makefile
		mono/benchmark/Makefile
		mono/mini/Makefile
		mono/profiler/Makefile
		mono/eglib/Makefile
		mono/eglib/eglib-config.h
		mono/eglib/test/Makefile
		m4/Makefile
		msvc/Makefile
		mono/utils/jemalloc/Makefile
		mono-uninstalled.pc
		acceptance-tests/Makefile
		scripts/mono-find-provides
		scripts/mono-find-requires
		ikvm-native/Makefile
		scripts/Makefile
		man/Makefile
		docs/Makefile
		data/Makefile
		data/net_2_0/Makefile
		data/net_4_0/Makefile
		data/net_4_5/Makefile
		data/net_2_0/Browsers/Makefile
		data/net_4_0/Browsers/Makefile
		data/net_4_5/Browsers/Makefile
		data/mint.pc
		data/mono-2.pc
		data/monosgen-2.pc
		data/mono.pc
		data/mono-cairo.pc
		data/mono-options.pc
		data/mono-lineeditor.pc
		data/monodoc.pc
		data/dotnet.pc
		data/dotnet35.pc
		data/wcf.pc
		data/cecil.pc
		data/system.web.extensions_1.0.pc
		data/system.web.extensions.design_1.0.pc
		data/system.web.mvc.pc
		data/system.web.mvc2.pc
		data/system.web.mvc3.pc
		data/aspnetwebstack.pc
		data/reactive.pc
		samples/Makefile
		support/Makefile
		data/config
		tools/Makefile
		tools/locale-builder/Makefile
		tools/sgen/Makefile
		tools/pedump/Makefile
		tools/mono-hang-watchdog/Makefile
		runtime/Makefile
		po/Makefile
		netcore/corerun/Makefile
	])
])
