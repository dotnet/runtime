include $(top_srcdir)/mk/common.mk

## Set the C++ linker based on configure settings, see comment in configure.ac
## about why we need to do this.
CXXLD=$(MONO_CXXLD)
CCLD=$(MONO_CXXLD)

count=100000
mtest=for_loop
monodir=$(top_builddir)
mono=$(if $(MONO_EXECUTABLE),$(MONO_EXECUTABLE),mono)

if HOST_WIN32
PLATFORM_PATH_SEPARATOR=;
else
PLATFORM_PATH_SEPARATOR=:
endif

# This is needed for automake dependency generation
if SUPPORT_NULLGC
libgc_libs=
libgc_static_libs=
else
libgc_libs=$(monodir)/libgc/libmonogc.la
libgc_static_libs=$(monodir)/libgc/libmonogc-static.la
endif

glib_libs = $(monodir)/mono/eglib/libeglib.la

boehm_libs=	\
	$(monodir)/mono/metadata/libmonoruntime.la	\
	$(monodir)/mono/utils/libmonoutils.la \
	$(glib_libs) \
	$(libgc_libs)

sgen_libs = \
	$(monodir)/mono/metadata/libmonoruntimesgen.la	\
	$(monodir)/mono/sgen/libmonosgen.la	\
	$(monodir)/mono/utils/libmonoutils.la \
	$(glib_libs)

if ENABLE_LLVM

include $(mono_build_root)/llvm/llvm_config.mk
LLVM_CFLAGS=$(LLVM_CFLAGS_INTERNAL) $(LLVM_CFLAGS_EXTERNAL)
LLVM_CXXFLAGS=$(LLVM_CXXFLAGS_INTERNAL) $(LLVM_CXXFLAGS_EXTERNAL)
LLVM_LDFLAGS=$(LLVM_LDFLAGS_INTERNAL) $(LLVM_LDFLAGS_EXTERNAL)
LLVM_LIBS=$(LLVM_LIBS_INTERNAL) $(LLVM_LIBS_EXTERNAL)

$(mono_build_root)/llvm/llvm_config.mk:
	$(MAKE) -C $(top_builddir)/llvm $(mono_build_root)/llvm/llvm_config.mk

endif


if FULL_AOT_TESTS
# if the tests are going to run with framework assemblies compiled with
# -d:MOBILE, tell the runtime to remap framework assemblies using the mobile
# runtime info
MOBILE_RUNTIME_ARG=--runtime=mobile
else
MOBILE_RUNTIME_ARG=
endif

CLASS=$(mcs_topdir)/class/lib/$(DEFAULT_PROFILE)

RUNTIME_EXECUTABLE = $(if $(BOEHM),$(top_builddir)/mono/mini/mono-boehm,$(top_builddir)/runtime/mono-wrapper)

MINI_RUNTIME = MONO_PATH=$(CLASS) $(RUNTIME_EXECUTABLE) $(MOBILE_RUNTIME_ARG)
TOOLS_RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/build $(top_builddir)/runtime/mono-wrapper
INTERP_RUNTIME = $(MINI_RUNTIME) --interpreter
RUNTIME_AOTCHECK = MONO_PATH="$(CLASS)$(PLATFORM_PATH_SEPARATOR)." $(RUNTIME_EXECUTABLE)

MCS = CSC_SDK_PATH_DISABLED= $(TOOLS_RUNTIME) $(CSC) -langversion:7.2 -nostdlib -unsafe -nowarn:0162 -nologo -noconfig -r:$(CLASS)/mscorlib.dll -r:$(CLASS)/System.dll -r:$(CLASS)/System.Core.dll
ILASM = $(TOOLS_RUNTIME) $(mcs_topdir)/class/lib/build/ilasm.exe

AM_CFLAGS = \
	-I$(top_srcdir) 	\
	$(GLIB_CFLAGS)		\
	$(LLVM_CFLAGS)		\
	$(JEMALLOC_CFLAGS)	\
	$(PLATFORM_CFLAGS) $(ARCH_CFLAGS) $(SHARED_CFLAGS)

AM_CXXFLAGS = $(LLVM_CXXFLAGS) $(GLIB_CFLAGS)

if HOST_WIN32
export HOST_CC
# The mingw math.h has "extern inline" functions that dont appear in libs, so
# optimisation is required to actually inline them
PLATFORM_CFLAGS = -O
endif

monoldflags=$(export_ldflags)
monobinldflags=$(export_ldflags) $(extra_runtime_ldflags)

if HOST_WIN32
libmonoldflags=-no-undefined -avoid-version -Wl,--kill-at $(monoldflags)
else
if HOST_ANDROID
libmonoldflags= -avoid-version $(monoldflags)
else
libmonoldflags=$(monoldflags) -version-info 1:0:0
endif
endif

if SUPPORT_SGEN
sgen_binaries = mono-sgen
sgen_libraries = libmonosgen-2.0.la
sgen_static_libraries = libmini.la $(interp_libs_with_mini) $(dbg_libs_with_mini) $(sgen_libs)
endif

if SUPPORT_BOEHM
boehm_libraries = libmonoboehm-2.0.la
boehm_static_libraries = libmini.la $(interp_libs_with_mini) $(dbg_libs_with_mini) $(boehm_libs)
boehm_binaries  = mono-boehm
endif

if SUPPORT_SGEN
mono_bin_suffix = sgen
libmono_suffix = sgen
else
mono_bin_suffix = boehm
libmono_suffix = boehm
endif

if DISABLE_EXECUTABLES
else
mono: mono-$(mono_bin_suffix)
	ln -sf $< $@

mono.exe: mono-$(mono_bin_suffix).exe
	ln -sf $< $@

install-exec-hook:
	(cd $(DESTDIR)$(bindir) && ln -sf mono-$(mono_bin_suffix) mono)
	(cd $(DESTDIR)$(libdir); shopt -s nullglob 2>/dev/null; for i in libmono$(libmono_suffix)*; do ln -sf $$i `echo $$i | sed s/$(libmono_suffix)//` ; done)
endif

if DISABLE_EXECUTABLES
else
if HOST_WIN32
bin_PROGRAMS = $(boehm_binaries) $(sgen_binaries) monow
else
bin_PROGRAMS = $(boehm_binaries) $(sgen_binaries)
endif
endif

if DISABLE_EXECUTABLES
noinst_PROGRAMS =
else
noinst_PROGRAMS = mono
endif

if DISABLE_EXECUTABLES
shared_libraries = $(boehm_libraries) $(sgen_libraries)
else
if SHARED_MONO
shared_libraries = $(boehm_libraries) $(sgen_libraries)
endif
endif

lib_LTLIBRARIES = $(shared_libraries)

if DISABLE_INTERPRETER
lib_LTLIBRARIES += $(interp_libs)
endif

if SHARED_MONO
mini_common_lib = libmini.la
else
mini_common_lib = 
endif

noinst_LTLIBRARIES = $(mini_common_lib)

noinst_LIBRARIES = libmain.a

libmain_a_SOURCES = main-sgen.c
if SUPPORT_BOEHM
libmain_a_SOURCES += main.c
endif
libmain_a_CFLAGS = $(AM_CFLAGS) @CXX_ADD_CFLAGS@

if LOADED_LLVM
lib_LTLIBRARIES += libmono-llvm.la
libmono_llvm_la_SOURCES = mini-llvm.c mini-llvm-cpp.cpp llvm-jit.cpp
libmono_llvm_la_LIBADD = $(glib_libs) $(LLVM_LIBS) $(LLVM_LDFLAGS)

if HOST_DARWIN
libmono_llvm_la_LDFLAGS=-Wl,-undefined -Wl,suppress -Wl,-flat_namespace 
endif
endif

# we need this to prevent automake from generating a default mono_SOURCES = mono.c
mono_SOURCES =

mono_boehm_SOURCES =
mono_boehm_CFLAGS = $(AM_CFLAGS) @CXX_REMOVE_CFLAGS@

AM_CPPFLAGS = $(LIBGC_CPPFLAGS)

mono_sgen_SOURCES =
mono_sgen_CFLAGS = $(AM_CFLAGS) @CXX_REMOVE_CFLAGS@

# We build this after libmono was built so it contains the date when the final
# link was done
if SUPPORT_BOEHM
buildver-boehm.h: libmini.la $(monodir)/mono/metadata/libmonoruntime.la
	@echo "const char *build_date = \"`date`\";" > buildver-boehm.h
libmain_a-main.$(OBJEXT): buildver-boehm.h
endif

buildver-sgen.h: libmini.la $(monodir)/mono/metadata/libmonoruntimesgen.la $(monodir)/mono/sgen/libmonosgen.la
	@echo "const char *build_date = \"`date`\";" > buildver-sgen.h

libmain_a-main-sgen.$(OBJEXT): buildver-sgen.h

if DTRACE_G_REQUIRED
LIBMONO_DTRACE_OBJECT = .libs/mono-dtrace.$(OBJEXT)
if STATIC_MONO
MONO_DTRACE_OBJECT = mono-dtrace.$(OBJEXT)
else
MONO_DTRACE_OBJECT = 
endif
else
MONO_DTRACE_OBJECT = 
LIBMONO_DTRACE_OBJECT = 
endif

if STATIC_MONO
# Link libmono into mono statically
# This leads to higher performance, especially with TLS
MONO_LIB=$(boehm_static_libraries)
MONO_SGEN_LIB=$(sgen_static_libraries)
else 
MONO_LIB=libmonoboehm-2.0.la
MONO_SGEN_LIB=libmonosgen-2.0.la
endif

if LOADED_LLVM
LLVMMONOF=
else
LLVMMONOF=$(LLVM_LIBS) $(LLVM_LDFLAGS)
endif

mono_boehm_LDADD = \
	libmain_a-main.$(OBJEXT) \
	$(MONO_LIB)		\
	$(glib_libs)		\
	$(LLVMMONOF)		\
	-lm			\
	$(MONO_DTRACE_OBJECT)

mono_boehm_LDFLAGS = \
	$(static_flags) $(monobinldflags) $(monobin_platform_ldflags) 

mono_sgen_LDADD = \
	libmain_a-main-sgen.$(OBJEXT) \
	$(MONO_SGEN_LIB)	\
	$(glib_libs)		\
	$(LLVMMONOF)		\
	-lm			\
	$(MONO_DTRACE_OBJECT)


mono_sgen_LDFLAGS = $(static_flags) $(monobinldflags) $(monobin_platform_ldflags)

if ENABLE_STATIC_GCC_LIBS
mono_sgen_LDFLAGS += -all-static
endif

if BITCODE
libmonoldflags += -no-undefined
endif

# if SUPPORT_SGEN
# 
# mono_LDADD = $(mono_sgen_LDADD)
# mono_LDFLAGS = $(mono_sgen_LDFLAGS)
# 
# endif


if DTRACE_G_REQUIRED

mono-dtrace.$(OBJEXT): $(top_srcdir)/data/mono.d mini.lo $(monodir)/mono/metadata/libmonoruntime-static.la
	DTRACE="$(DTRACE)" DTRACEFLAGS="$(DTRACEFLAGS)" AR="$(AR)" $(SHELL) $(top_srcdir)/data/dtrace-prelink.sh \
	$@ $(top_srcdir)/data/mono.d $(monodir)/mono/metadata/libmonoruntime-static.la mini.lo

.libs/mono-dtrace.$(OBJEXT): $(top_srcdir)/data/mono.d mini.lo $(monodir)/mono/metadata/libmonoruntime.la
	DTRACE="$(DTRACE)" DTRACEFLAGS="$(DTRACEFLAGS)" AR="$(AR)" $(SHELL) $(top_srcdir)/data/dtrace-prelink.sh \
	--pic $@ $(top_srcdir)/data/mono.d $(monodir)/mono/metadata/libmonoruntime.la mini.lo

endif

# Create monow.exe, linked for the 'windows' subsystem
if HOST_WIN32
if SUPPORT_BOEHM
monow_LDADD = $(mono_boehm_LDADD)
monow_LDFLAGS = $(mono_boehm_LDFLAGS) -mwindows
monow_SOURCES = $(mono_boehm_SOURCES)
else
monow_LDADD = $(mono_sgen_LDADD)
monow_LDFLAGS = $(mono_sgen_LDFLAGS) -mwindows
monow_SOURCES = $(mono_sgen_SOURCES)
endif
endif

wasm_sources = \
	mini-wasm.c		\
	mini-wasm.h		\
	mini-wasm-debugger.c	\
	debugger-engine.c	\
	exceptions-wasm.c	\
	tramp-wasm.c

x86_sources = \
	mini-x86.c		\
	mini-x86.h		\
	exceptions-x86.c	\
	tramp-x86.c	\
	mini-x86-gsharedvt.c	\
	tramp-x86-gsharedvt.c

amd64_sources = \
	mini-amd64.c		\
	mini-amd64.h		\
	exceptions-amd64.c	\
	tramp-amd64.c	\
	mini-amd64-gsharedvt.c	\
	mini-amd64-gsharedvt.h	\
	tramp-amd64-gsharedvt.c

ppc_sources = \
	mini-ppc.c		\
	mini-ppc.h		\
	exceptions-ppc.c	\
	tramp-ppc.c

arm_sources = \
	mini-arm.c		\
	mini-arm.h		\
	exceptions-arm.c	\
	tramp-arm.c	\
	mini-arm-gsharedvt.c	\
	tramp-arm-gsharedvt.c

arm64_sources = \
	mini-arm64.c		\
	mini-arm64.h		\
	exceptions-arm64.c	\
	tramp-arm64.c	\
	mini-arm64-gsharedvt.c	\
	mini-arm64-gsharedvt.h	\
	tramp-arm64-gsharedvt.c

mips_sources = \
	mini-mips.c		\
	mini-mips.h		\
	exceptions-mips.c	\
	tramp-mips.c

sparc_sources = \
	mini-sparc.c		\
	mini-sparc.h		\
	exceptions-sparc.c	\
	tramp-sparc.c

s390x_sources = \
	mini-s390x.c		\
	mini-s390x.h		\
	support-s390x.h		\
	exceptions-s390x.c	\
	tramp-s390x.c

riscv_sources = \
	mini-riscv.c \
	mini-riscv.h \
	exceptions-riscv.c \
	tramp-riscv.c

darwin_sources = \
	mini-darwin.c

windows_sources = \
	mini-windows.c \
	mini-windows.h \
	mini-windows-dllmain.c \
	mini-windows-dlldac.c

posix_sources = \
	mini-posix.c

if ENABLE_LLVM
if LOADED_LLVM
llvm_sources = \
	mini-llvm-loaded.c
else
llvm_sources = \
	mini-llvm.c		\
	mini-llvm-loaded.c \
	mini-llvm-cpp.cpp \
	llvm-jit.cpp
endif
endif

interp_sources =	\
	interp/interp.h	\
	interp/interp-internals.h	\
	interp/interp.c	\
	interp/mintops.h	\
	interp/mintops.def	\
	interp/mintops.c	\
	interp/transform.c

interp_libs = libmono-ee-interp.la

if !DISABLE_INTERPRETER
interp_libs_with_mini = $(interp_libs)
endif

if ENABLE_LLVM
llvm_runtime_sources = \
	llvm-runtime.cpp
else
if ENABLE_LLVM_RUNTIME
llvm_runtime_sources = \
	llvm-runtime.cpp
endif
endif

if ENABLE_NETCORE
netcore_sources = \
	main-core.c
endif

common_sources = \
	mini.c			\
	mini-runtime.c	\
	seq-points.c	\
	seq-points.h	\
	ir-emit.h		\
	method-to-ir.c		\
	cfgdump.h		\
	cfgdump.c		\
	decompose.c		\
	mini.h			\
	version.h		\
	optflags-def.h		\
	jit-icalls.h 		\
	jit-icalls.c 		\
	trace.c			\
	trace.h			\
	patch-info.h		\
	mini-ops.h		\
	mini-arch.h		\
	dominators.c		\
	cfold.c			\
	regalloc.h		\
	helpers.c		\
	liveness.c		\
	ssa.c			\
	abcremoval.c		\
	abcremoval.h		\
	local-propagation.c	\
	driver.c		\
	debug-mini.c		\
	linear-scan.c		\
	aot-compiler.h		\
	aot-compiler.c		\
	aot-runtime.c		\
	aot-runtime-wasm.c	\
	wasm_m2n_invoke.g.h	\
	graph.c			\
	mini-codegen.c		\
	mini-exceptions.c	\
	mini-trampolines.c  	\
	branch-opts.c		\
	mini-generic-sharing.c	\
	simd-methods.h		\
	tasklets.c		\
	tasklets.h		\
	simd-intrinsics.c	\
	mini-native-types.c \
	mini-unwind.h		\
	unwind.c		\
	image-writer.h		\
	image-writer.c		\
	dwarfwriter.h		\
	dwarfwriter.c		\
	mini-gc.h		\
	mini-gc.c		\
	debugger-agent.h 	\
	debugger-engine.h	\
	debugger-agent-stubs.c	\
	debugger-state-machine.h	\
	xdebug.c			\
	mini-llvm.h			\
	mini-llvm-cpp.h	\
	llvm-jit.h		\
	alias-analysis.c	\
	mini-cross-helpers.c \
	arch-stubs.c		\
	llvm-runtime.h	\
	type-checking.c \
	calls.c			\
	lldb.h			\
	lldb.c	\
	memory-access.c	\
	intrinsics.c \
	mini-profiler.c	\
	interp-stubs.c \
	llvmonly-runtime.h \
	llvmonly-runtime.c \
	aot-runtime.h	\
	ee.h \
	mini-runtime.h

# keep in sync with mcs/class/Mono.Runtime.Tests
test_sources = 			\
	basic-calls.cs 		\
	basic-long.cs 		\
	bench.cs 		\
	builtin-types.cs 	\
	objects.cs 		\
	arrays.cs		\
	basic-float.cs		\
	basic-math.cs		\
	basic.cs		\
	exceptions.cs		\
	devirtualization.cs	\
	iltests.il		\
	test.cs			\
	generics.cs		\
	generics-variant-types.il\
	basic-simd.cs \
	basic-vectors.cs \
	aot-tests.cs \
	gc-test.cs \
	gshared.cs \
	unaligned.cs	\
	MemoryIntrinsics.il	\
	mixed.cs	\
	ratests.cs

regtests_UNIVERSAL = \
	aot-tests.exe \
	basic.exe \
	basic-float.exe \
	basic-long.exe \
	basic-calls.exe \
	builtin-types.exe \
	gshared.exe \
	objects.exe \
	arrays.exe \
	basic-math.exe \
	exceptions.exe \
	iltests.exe \
	devirtualization.exe \
	generics.exe \
	basic-simd.exe \
	unaligned.exe	\
	basic-vectors.exe	\
	ratests.exe

regtests_DISABLED = 

if FULL_AOT_TESTS
regtests_DISABLED += builtin-types.exe
endif

regtests = $(filter-out $(regtests_DISABLED),$(regtests_UNIVERSAL))

if WASM
arch_sources = $(wasm_sources)
arch_built=cpu-wasm.h
arch_define=__wasm__
target_define=TARGET_WASM
endif

if X86
arch_sources = $(x86_sources)
arch_built=cpu-x86.h
arch_define=__i386__
target_define=TARGET_X86
endif

if AMD64
arch_sources = $(amd64_sources)
arch_built=cpu-amd64.h
arch_define=__x86_64__
target_define=TARGET_AMD64
ARCH_FULLAOT_EXCLUDE=
endif

if POWERPC
arch_sources = $(ppc_sources)
arch_built=cpu-ppc.h
arch_define=__ppc__
target_define=TARGET_POWERPC
endif

if POWERPC64
arch_sources = $(ppc_sources)
arch_built=cpu-ppc64.h
arch_define=__ppc64__
target_define=TARGET_POWERPC
endif

if MIPS
arch_sources = $(mips_sources)
arch_built=cpu-mips.h
arch_define=__mips__
target_define=TARGET_MIPS
endif

if ARM
# pick up arm_dpimacros.h
ARCH_CFLAGS = -I../arch/arm
arch_sources = $(arm_sources)
arch_built=cpu-arm.h
arch_define=__arm__
target_define=TARGET_ARM
endif

if ARM64
arch_sources = $(arm64_sources)
arch_built=cpu-arm64.h
arch_define=__aarch64__
target_define=TARGET_ARM64
endif

if SPARC
arch_sources = $(sparc_sources)
arch_built=cpu-sparc.h
arch_define=__sparc__
target_define=TARGET_SPARC
endif

if SPARC64
arch_sources = $(sparc_sources)
arch_built=cpu-sparc.h
arch_define=__sparc__
target_define=TARGET_SPARC
endif

if S390X
arch_sources = $(s390x_sources)
arch_built=cpu-s390x.h
arch_define=__s390__
target_define=TARGET_S390X
endif

if RISCV32
arch_sources = $(riscv_sources)
arch_built = cpu-riscv32.h
arch_define = __riscv
target_define = TARGET_RISCV32
endif

if RISCV64
arch_sources = $(riscv_sources)
arch_built = cpu-riscv64.h
arch_define = __riscv
target_define = TARGET_RISCV64
endif

if HOST_WIN32
os_sources = $(windows_sources)
monobin_platform_ldflags=
endif

if HOST_SIGPOSIX
os_sources = $(posix_sources)
monobin_platform_ldflags=
endif

if HOST_DARWIN
os_sources = $(darwin_sources) $(posix_sources)
#monobin_platform_ldflags=-sectcreate __TEXT __info_plist $(top_srcdir)/mono/mini/Info.plist -framework CoreFoundation -framework Foundation
monobin_platform_ldflags=-framework CoreFoundation -framework Foundation
endif

if MONO_JEMALLOC_ENABLED
libmonoldflags += $(JEMALLOC_LDFLAGS)
endif

libmono_ee_interp_la_SOURCES = $(interp_sources)
libmono_ee_interp_la_CFLAGS = $(AM_CFLAGS) @CXX_ADD_CFLAGS@
if BITCODE
libmono_ee_interp_la_LDFLAGS = $(libmonoldflags)
if DISABLE_INTERPRETER
libmono_ee_interp_la_LIBADD = libmonosgen-2.0.la
endif
endif

if !WASM
extra_libmono_dbg_source = debugger-engine.c
endif

libmono_dbg_la_SOURCES = debugger-agent.c debugger-state-machine.c $(extra_libmono_dbg_source)
libmono_dbg_la_CFLAGS = $(AM_CFLAGS) @CXX_ADD_CFLAGS@
if BITCODE
if DISABLE_DEBUGGER_AGENT
libmono_dbg_la_LIBADD = libmonosgen-2.0.la
endif
endif

dbg_libs = libmono-dbg.la

if DISABLE_DEBUGGER_AGENT
lib_LTLIBRARIES += $(dbg_libs)
endif

if !DISABLE_DEBUGGER_AGENT
dbg_libs_with_mini = $(dbg_libs)
noinst_LTLIBRARIES += $(dbg_libs)
endif

#
# This library is shared between mono and mono-sgen, since the code in mini/ doesn't contain
# compile time dependencies on boehm/sgen.
#
libmini_la_SOURCES = $(common_sources) $(llvm_sources) $(llvm_runtime_sources) $(arch_sources) $(os_sources) $(netcore_sources)
libmini_la_CFLAGS = $(AM_CFLAGS) @CXX_ADD_CFLAGS@

libmonoboehm_2_0_la_SOURCES =
libmonoboehm_2_0_la_CFLAGS = $(mono_boehm_CFLAGS) @CXX_ADD_CFLAGS@

libmonoboehm_2_0_la_LIBADD = libmini.la $(interp_libs_with_mini) $(dbg_libs_with_mini) $(boehm_libs) $(LIBMONO_DTRACE_OBJECT) $(LLVMMONOF)
libmonoboehm_2_0_la_LDFLAGS = $(libmonoldflags) $(monobin_platform_ldflags) 

libmonosgen_2_0_la_SOURCES =
libmonosgen_2_0_la_CFLAGS = $(mono_sgen_CFLAGS) @CXX_ADD_CFLAGS@

libmonosgen_2_0_la_LIBADD = libmini.la $(interp_libs_with_mini) $(dbg_libs_with_mini) $(sgen_libs) $(LIBMONO_DTRACE_OBJECT) $(LLVMMONOF)
libmonosgen_2_0_la_LDFLAGS = $(libmonoldflags) $(monobin_platform_ldflags) 

libmonoincludedir = $(includedir)/mono-$(API_VER)/mono/jit

# These are public headers.
# They should not use glib.h, G_BEGIN_DECLS, guint, etc.
# They should be wrapped in MONO_BEGIN_DECLS / MONO_END_DECLS.
libmonoinclude_HEADERS = jit.h

CSFLAGS = -unsafe -nowarn:0219,0169,0414,0649,0618

basic-simd.exe: basic-simd.cs TestDriver.dll
	$(MCS) -out:$@ $(CSFLAGS) $< -r:TestDriver.dll -r:$(CLASS)/Mono.Simd.dll

basic-vectors.exe: basic-vectors.cs TestDriver.dll
	$(MCS) -out:$@ $(CSFLAGS) $< -r:TestDriver.dll -r:$(CLASS)/System.Numerics.dll -r:$(CLASS)/System.Numerics.Vectors.dll

builtin-types.exe: builtin-types.cs TestDriver.dll
	$(MCS) -out:$@ $(CSFLAGS) -define:ARCH_$(shell echo $$((8 * $(SIZEOF_VOID_P)))) $< -r:TestDriver.dll

nacl.exe: nacl.cs TestDriver.dll
	$(MCS) -out:$@ $(CSFLAGS) $< -r:TestDriver.dll -r:$(CLASS)/Mono.Simd.dll

generics.exe: generics.cs TestDriver.dll generics-variant-types.dll
	$(MCS) -out:$@ $(CSFLAGS) $< -r:TestDriver.dll -r:generics-variant-types.dll -r:$(CLASS)/System.Core.dll

unaligned.exe: unaligned.cs TestDriver.dll MemoryIntrinsics.dll
	$(MCS) -out:$@ $(CSFLAGS) $< -r:TestDriver.dll -r:MemoryIntrinsics.dll

%.exe: %.cs TestDriver.dll
	$(MCS) -out:$@ $(CSFLAGS) $< -r:TestDriver.dll

%.exe: %.il
	$(ILASM) -output=$@ $<

TestDriver.dll: $(srcdir)/TestDriver.cs $(srcdir)/TestHelpers.cs
	$(MCS) -out:$@ -target:library $^

generics-variant-types.dll: generics-variant-types.il
	$(ILASM) -dll -output=$@ $<

MemoryIntrinsics.dll: MemoryIntrinsics.il
	$(ILASM) -dll -output=$@ $<

GENMDESC_OPTS=

GENMDESC_PRG=python $(srcdir)/genmdesc.py $(target_define) $(srcdir)

cpu-wasm.h: mini-ops.h cpu-wasm.md $(srcdir)/genmdesc.py
	$(GENMDESC_PRG) cpu-wasm.h wasm_desc $(srcdir)/cpu-wasm.md

cpu-x86.h: mini-ops.h cpu-x86.md $(srcdir)/genmdesc.py
	$(GENMDESC_PRG) cpu-x86.h x86_desc $(srcdir)/cpu-x86.md

cpu-amd64.h: mini-ops.h cpu-amd64.md $(srcdir)/genmdesc.py
	$(GENMDESC_PRG) cpu-amd64.h amd64_desc $(srcdir)/cpu-amd64.md

cpu-ppc.h: mini-ops.h cpu-ppc.md $(srcdir)/genmdesc.py
	$(GENMDESC_PRG) cpu-ppc.h ppcg4 $(srcdir)/cpu-ppc.md

cpu-ppc64.h: mini-ops.h cpu-ppc64.md $(srcdir)/genmdesc.py
	$(GENMDESC_PRG) cpu-ppc64.h ppc64_cpu_desc $(srcdir)/cpu-ppc64.md

cpu-arm.h: mini-ops.h cpu-arm.md $(srcdir)/genmdesc.py
	$(GENMDESC_PRG) cpu-arm.h arm_cpu_desc $(srcdir)/cpu-arm.md

cpu-arm64.h: mini-ops.h cpu-arm64.md $(srcdir)/genmdesc.py
	$(GENMDESC_PRG) cpu-arm64.h arm64_cpu_desc $(srcdir)/cpu-arm64.md

cpu-sparc.h: mini-ops.h cpu-sparc.md $(srcdir)/genmdesc.py
	$(GENMDESC_PRG) cpu-sparc.h sparc_desc $(srcdir)/cpu-sparc.md

cpu-s390x.h: mini-ops.h cpu-s390x.md $(srcdir)/genmdesc.py
	$(GENMDESC_PRG) cpu-s390x.h s390x_cpu_desc $(srcdir)/cpu-s390x.md

cpu-mips.h: mini-ops.h cpu-mips.md $(srcdir)/genmdesc.py
	$(GENMDESC_PRG) cpu-mips.h mips_desc $(srcdir)/cpu-mips.md

cpu-riscv32.h: cpu-riscv32.md $(srcdir)/genmdesc.py
	$(GENMDESC_PRG) cpu-riscv32.h riscv32_cpu_desc $(srcdir)/cpu-riscv32.md

cpu-riscv64.h: cpu-riscv64.md $(srcdir)/genmdesc.py
	$(GENMDESC_PRG) cpu-riscv64.h riscv64_cpu_desc $(srcdir)/cpu-riscv64.md

testi: mono test.exe
	$(MINI_RUNTIME) -v -v --ncompile 1 --compile Test:$(mtest) test.exe

test-local: $(regtests)

test-bundle-local:
	mkdir -p $(TEST_BUNDLE_PATH)/tests/mini
	cp -L $(regtests) TestDriver.dll MemoryIntrinsics.dll generics-variant-types.dll $(TEST_BUNDLE_PATH)/tests/mini/
	cp -L mono-sgen $(TEST_BUNDLE_PATH)/
	chmod +x $(TEST_BUNDLE_PATH)/mono-sgen
if SUPPORT_BOEHM
	cp -L mono-boehm $(TEST_BUNDLE_PATH)/
	chmod +x $(TEST_BUNDLE_PATH)/mono-boehm
endif

# ensure the tests are actually correct
checktests: $(regtests)
	for i in $(regtests); do $(MINI_RUNTIME) $$i; done

rcheck-nunit: mono $(regtests)
	$(MINI_RUNTIME) --regression $(regtests) > regressiontests.out 2>&1; cat regressiontests.out; \
	if grep -q "100% pass" regressiontests.out; then successbool=True; failurescount=0; else successbool=False; failurescount=1; fi; \
	echo "<?xml version='1.0' encoding='utf-8'?>\
		<test-results failures='$$failurescount' total='1' not-run='0' name='regression-tests.dummy' date='$$(date +%F)' time='$$(date +%T)'>\
			<test-suite name='regression-tests.dummy' success='$$successbool' time='0'>\
				<results><test-case name='MonoTests.regressions.100percentsuccess' executed='True' success='$$successbool' time='0'>" > TestResult-regression.xml; \
					if [ "$$successbool" = "False" ]; then echo "<failure><message><![CDATA[$$(cat regressiontests.out)]]></message><stack-trace></stack-trace></failure>" >> TestResult-regression.xml; fi; \
				echo "</test-case></results>\
			</test-suite>\
		</test-results>" >> TestResult-regression.xml; exit $$failurescount

rcheck: mono $(regtests)
	$(MINI_RUNTIME) --regression $(regtests)

richeck: mono $(regtests)
	$(INTERP_RUNTIME) --regression $(regtests)

mixedcheck: mono mixed.exe
	$(MINI_RUNTIME) --interp=jit=JitClass mixed.exe

if ARM
check-seq-points:
else
check-seq-points: mono $(regtests)
	rm -f TestResult-op_il_seq_point.xml
	for i in $(regtests); do $(srcdir)/test_op_il_seq_point.sh $(DEFAULT_PROFILE) $$i || ($(srcdir)/test_op_il_seq_point_headerfooter.sh; exit 1) || exit 1; done
	for i in $(regtests); do $(srcdir)/test_op_il_seq_point.sh $(DEFAULT_PROFILE) $$i --aot || ($(srcdir)/test_op_il_seq_point_headerfooter.sh; exit 1) || exit 1; done
	$(srcdir)/test_op_il_seq_point_headerfooter.sh
endif

gctest: mono gc-test.exe
	MONO_DEBUG_OPTIONS=clear-nursery-at-gc $(MINI_RUNTIME) --regression gc-test.exe

LLVM_AOT_RUNTIME_OPTS=$(if $(LLVM),--llvm,)
GSHAREDVT_RUNTIME_OPTS=$(if $(GSHAREDVT),-O=gsharedvt,)

aotcheck: mono $(regtests)
	rm -rf *.exe.so *.exe.dylib *.exe.dylib.dSYM
	$(MINI_RUNTIME) $(LLVM_AOT_RUNTIME_OPTS) --aot $(regtests) || exit 1
	for i in $(regtests); do $(RUNTIME_AOTCHECK) --regression $$i || exit 1; done
	rm -rf *.exe.so *.exe.dylib *.exe.dylib.dSYM

llvmaotcheck:
	$(MAKE) aotcheck LLVM=1

gsharedvtcheck:
	$(MAKE) fullaotcheck GSHAREDVT=1

hybridcheck:
	$(MAKE) fullaotcheck HYBRID=1

# FIXME: force preemptive suspend while interpreter doesn't support coop/hybird suspend
fullaotmixedcheck:
	$(MAKE) fullaotcheck MIXED=1 MONO_THREADS_SUSPEND=preemptive

fullaot_regtests = $(regtests)
fullaot_testing_deps = generics-variant-types.dll TestDriver.dll MemoryIntrinsics.dll
fullaot_testing_deps_commas = generics-variant-types.dll,TestDriver.dll,MemoryIntrinsics.dll

FULLAOT_LIBS_UNIVERSAL = \
	mscorlib.dll \
	System.Core.dll \
	System.dll \
	System.Xml.dll \
	System.Security.dll \
	Mono.Simd.dll \
	Mono.Security.dll \
	System.Numerics.dll \
	System.Numerics.Vectors.dll \
	Mono.Posix.dll \
	System.Configuration.dll

FULLAOT_LIBS_DISABLED =

# Skip aoting the tests that aren't compiled 
# on the full aot profiles because they're skipped
# on mobile profiles
if FULL_AOT_TESTS
FULLAOT_LIBS_DISABLED += \
	Mono.Posix.dll \
	System.Configuration.dll
else
if FULL_AOT_INTERP_TESTS
FULLAOT_LIBS_DISABLED += \
	Mono.Posix.dll \
	System.Configuration.dll
endif
endif

FULLAOT_LIBS = $(filter-out $(FULLAOT_LIBS_DISABLED),$(FULLAOT_LIBS_UNIVERSAL))

FULLAOT_TMP_DIR=$(top_builddir)/mono/mini/fullaot-tmp

FULLAOT_AOT_ARGS=$(if $(HYBRID),hybrid,full,interp),$(MONO_FULLAOT_ADDITIONAL_ARGS)$(INVARIANT_AOT_OPTIONS)
FULLAOT_ARGS=$(if $(HYBRID),--hybrid-aot,--full-aot)

# This currently only works on amd64/arm
fullaotcheck: $(mono) $(fullaot_regtests) $(fullaot_testing_deps)
	rm -rf $(FULLAOT_TMP_DIR)
	mkdir $(FULLAOT_TMP_DIR)
	$(MAKE) fullaot-libs AOT_FLAGS="$(FULLAOT_AOT_ARGS)"
	cp $(fullaot_regtests) $(fullaot_testing_deps) $(FULLAOT_TMP_DIR)/
	MONO_PATH=$(FULLAOT_TMP_DIR) $(top_builddir)/runtime/mono-wrapper $(MOBILE_RUNTIME_ARG) $(LLVM_AOT_RUNTIME_OPTS) $(GSHAREDVT_RUNTIME_OPTS) --aot="$(FULLAOT_AOT_ARGS)" $(FULLAOT_TMP_DIR)/{$(fullaot_testing_deps_commas),*.exe} || exit 1
	ln -s $(if $(MONO_EXECUTABLE),$(MONO_EXECUTABLE),$$PWD/mono) $(FULLAOT_TMP_DIR)/
	for i in $(fullaot_regtests); do echo $$i; MONO_PATH=$(FULLAOT_TMP_DIR) $(top_builddir)/runtime/mono-wrapper $(MOBILE_RUNTIME_ARG) $(FULLAOT_ARGS) $(FULLAOT_TMP_DIR)/$$i --exclude '!FULLAOT' $(ARCH_FULLAOT_EXCLUDE) || exit 1; done
	if test x$(MIXED) == x1; then failed=0;i=0; while test $$i -lt 900; do i=`expr $$i + 1`; bash -c "echo -n '.'"; MONO_PATH=$(FULLAOT_TMP_DIR) MONO_DEBUG=aot-skip=$$i $(top_builddir)/runtime/mono-wrapper --full-aot-interp $(FULLAOT_TMP_DIR)/basic.exe > $(FULLAOT_TMP_DIR)/mixed.log || failed=1; if test $$failed -eq 1; then echo "Failed at $$i"; exit $$failed; fi; done; fi

# This can run in parallel
fullaot-libs: $(patsubst %,fullaot-tmp/%.dylib,$(FULLAOT_LIBS))

fullaot-tmp/%.dylib: $(CLASS)/%
	cp $(CLASS)/$* fullaot-tmp/
	mkdir fullaot-tmp/$*-tmp
	MONO_PATH="fullaot-tmp/$(PLATFORM_PATH_SEPARATOR)$(CLASS)" $(top_builddir)/runtime/mono-wrapper $(MOBILE_RUNTIME_ARG) $(LLVM_AOT_RUNTIME_OPTS) $(GSHAREDVT_RUNTIME_OPTS) --aot=$(AOT_FLAGS),temp-path=fullaot-tmp/$*-tmp fullaot-tmp/$*
	rm -rf fullaot-tmp/$*-tmp

llvmfullaotcheck:
	$(MAKE) fullaotcheck LLVM=1 GSHAREDVT=1

llvmonly_regtests = $(fullaot_regtests) gshared.exe

llvmonlycheck: mono $(llvmonly_regtests)
	rm -rf fullaot-tmp
	mkdir fullaot-tmp
	$(MAKE) fullaot-libs AOT_FLAGS="llvmonly,clangxx=clang++-6.0,$(MONO_FULLAOT_ADDITIONAL_ARGS)$(INVARIANT_AOT_OPTIONS)"
	cp $(llvmonly_regtests) $(fullaot_testing_deps) fullaot-tmp/
	MONO_PATH=fullaot-tmp $(top_builddir)/runtime/mono-wrapper  $(MOBILE_RUNTIME_ARG) --aot=llvmonly,clangxx=clang++-6.0 fullaot-tmp/{$(fullaot_testing_deps_commas),*.exe} || exit 1
	ln -s $$PWD/mono fullaot-tmp/
	for i in $(llvmonly_regtests); do echo $$i; MONO_PATH=fullaot-tmp $(top_builddir)/runtime/mono-wrapper $(MOBILE_RUNTIME_ARG) --llvmonly fullaot-tmp/$$i --exclude '!BITCODE' || exit 1; done

gccheck: gc-test.exe
	MONO_GC_PARAMS=stack-mark=precise MONO_GC_DEBUG=clear-at-gc ./mono-sgen gc-test.exe	

bench: mono test.exe
	time env $(MINI_RUNTIME) --ncompile $(count) --compile Test:$(mtest) test.exe

mbench: test.exe
	time $(monodir)/mono/jit/mono --ncompile $(count) --compile Test:$(mtest) test.exe

stat1: mono bench.exe
	$(MINI_RUNTIME) --verbose --statfile stats.pl --regression bench.exe
	perl viewstat.pl stats.pl

stat2: mono basic.exe
	$(MINI_RUNTIME) --verbose --statfile stats.pl --regression basic.exe
	perl viewstat.pl -e stats.pl

stat3: mono bench.exe
	$(MINI_RUNTIME) --statfile stats.pl --ncompile 1000 --compile Tests:test_0_many_nested_loops bench.exe 
	perl viewstat.pl stats.pl

docu: mini.sgm
	docbook2txt mini.sgm

# We need these because automake can't process normal make conditionals
check_local_targets = $(if $(EMIT_NUNIT), rcheck-nunit, rcheck)

check-local: $(check_local_targets)

clean-local:
	rm -f mono a.out gmon.out *.o buildver-boehm.h buildver-sgen.h test.exe regressionexitcode.out TestResult-op_il_seq_point.xml*

pkgconfigdir = $(libdir)/pkgconfig

BUILT_SOURCES = version.h $(arch_built)

CLEANFILES= $(BUILT_SOURCES) *.exe *.dll
EXTRA_DIST = TestDriver.cs \
	TestHelpers.cs \
	genmdesc.py				\
	$(test_sources) 			\
	$(wasm_sources) cpu-wasm.md 		\
	$(x86_sources) cpu-x86.md 		\
	$(amd64_sources) cpu-amd64.md 		\
	$(ppc_sources) cpu-ppc.md cpu-ppc64.md 	\
	$(arm_sources) cpu-arm.md 		\
	$(arm64_sources) cpu-arm64.md 		\
	$(mips_sources) cpu-mips.md 		\
	$(sparc_sources) cpu-sparc.md 		\
	$(s390x_sources) cpu-s390x.md 		\
	$(riscv_sources) cpu-riscv32.md cpu-riscv64.md \
	$(windows_sources)			\
	$(darwin_sources) Info.plist		\
	$(posix_sources)					\
	test_op_il_seq_point.sh			\
	test_op_il_seq_point_headerfooter.sh	\
	Makefile.am.in

version.h: Makefile
	if test -e $(top_srcdir)/.git; then \
		(cd $(top_srcdir); \
			LANG=C; export LANG; \
			if test -z "$$ghprbPullId"; then \
				branch=`git branch | grep '^\*' | sed 's/.*detached .*/explicit/' | cut -d ' ' -f 2`; \
			else \
				branch="pull-request-$$ghprbPullId"; \
			fi; \
			version=`git log --no-color --first-parent -n1 --pretty=format:%h`; \
			echo "#define FULL_VERSION \"$$branch/$$version\""; \
		); \
	else \
		if test -z "$$MONO_BRANCH" -o -z "$$MONO_BUILD_REVISION"; then \
			echo "#define FULL_VERSION \"tarball\""; \
		else \
			echo "#define FULL_VERSION \"$$MONO_BRANCH/$$MONO_BUILD_REVISION\""; \
		fi \
	fi > version.h

# Utility target for patching libtool to speed up linking
patch-libtool:
	sed -e 's,if (for obj in $$oldobjs,if (for obj in "",g' < ../../libtool > 2; mv 2 ../../libtool
	chmod a+x ../../libtool

# Utility target for patching libtool to get rid of the 'ranlib: file <file> has no symbols" warnings
patch-osx-libtool:
	sed -e 's/old_archive_cmds=.*/old_archive_cmds="libtool -no_warning_for_no_symbols -static -o \\$$oldlib \\$$oldobjs"/g' < ../../libtool > 2; mv 2 ../../libtool
	chmod a+x ../../libtool

# Utility target to patch automake to generate the same format silent output as the old mono silent build did
patch-automake:
	src="@echo \"  '. \$$name . ' ' x (8 - length (\$$name)) . '\""; dst="@echo \"'. \$$name . ' ' x (7 - length (\$$name)) .'\""; sed -e "s/$$src/$$dst/g" < $$EXE > 2 && cp 2 $$EXE && rm -f 2

tags:
	etags -o TAGS `find .. -name "*.h" -o -name "*.c"`

if ENABLE_LLVM
llvm_makefile_dep = $(mono_build_root)/llvm/llvm_config.mk
endif

if HAS_EXTENSION_MODULE
else
Makefile.am: Makefile.am.in
	echo "##################### Generated from Makefile.am.in, do not modify ##########################" > $@
	cat $< >> $@

endif

# Per-library because linking doesn't like -xc++, it treats libraries as C++.
CFLAGS := $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@)
