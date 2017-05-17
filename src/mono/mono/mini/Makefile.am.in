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
libgc_libs=$(monodir)/libgc/libmonogc.la
libgc_static_libs=$(monodir)/libgc/libmonogc-static.la

boehm_libs=	\
	$(monodir)/mono/metadata/libmonoruntime.la	\
	$(monodir)/mono/utils/libmonoutils.la \
	$(GLIB_LIBS) $(LIBICONV) \
	$(libgc_libs)

sgen_libs = \
	$(monodir)/mono/metadata/libmonoruntimesgen.la	\
	$(monodir)/mono/sgen/libmonosgen.la	\
	$(monodir)/mono/utils/libmonoutils.la \
	$(GLIB_LIBS) $(LIBICONV)

boehm_static_libs=	\
	$(monodir)/mono/metadata/libmonoruntime-static.la	\
	$(monodir)/mono/utils/libmonoutils.la \
	$(GLIB_LIBS) $(LIBICONV) \
	$(libgc_static_libs)

sgen_static_libs = \
	$(monodir)/mono/metadata/libmonoruntimesgen-static.la	\
	$(monodir)/mono/sgen/libmonosgen-static.la	\
	$(monodir)/mono/utils/libmonoutils.la \
	$(GLIB_LIBS) $(LIBICONV)

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

MCS = CSC_SDK_PATH_DISABLED= $(TOOLS_RUNTIME) $(CSC) -unsafe -nowarn:0162 -nologo -noconfig -r:$(CLASS)/mscorlib.dll -r:$(CLASS)/System.dll -r:$(CLASS)/System.Core.dll
ILASM = $(TOOLS_RUNTIME) $(mcs_topdir)/class/lib/build/ilasm.exe

AM_CFLAGS = \
	-I$(top_srcdir) 	\
	$(GLIB_CFLAGS)		\
	$(LLVM_CFLAGS)		\
	$(PLATFORM_CFLAGS) $(ARCH_CFLAGS) $(SHARED_CFLAGS)

AM_CXXFLAGS = $(LLVM_CXXFLAGS) $(GLIB_CFLAGS)

if HOST_WIN32
export HOST_CC
# The mingw math.h has "extern inline" functions that dont appear in libs, so
# optimisation is required to actually inline them
PLATFORM_CFLAGS = -O
endif

# hack for automake to have the same source file in a library and a bin
genmdesc_CFLAGS = $(AM_CFLAGS)

monoldflags=$(export_ldflags)
monobinldflags=$(export_ldflags) $(extra_runtime_ldflags)

if HOST_WIN32
libmonoldflags=-no-undefined -avoid-version -Wl,--kill-at $(monoldflags)
else
if PLATFORM_ANDROID
libmonoldflags= -avoid-version $(monoldflags)
else
libmonoldflags=$(monoldflags) -version-info 1:0:0
endif
endif

if SUPPORT_SGEN
sgen_binaries = mono-sgen
sgen_libraries = libmonosgen-2.0.la
sgen_static_libraries = libmini-static.la $(sgen_static_libs)
endif

if SUPPORT_BOEHM
boehm_libraries = libmonoboehm-2.0.la
boehm_static_libraries = libmini-static.la $(boehm_static_libs)
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
noinst_PROGRAMS = genmdesc
else
noinst_PROGRAMS = genmdesc mono
endif

if DISABLE_EXECUTABLES
shared_libraries = $(boehm_libraries) $(sgen_libraries)
else
if SHARED_MONO
shared_libraries = $(boehm_libraries) $(sgen_libraries)
endif
endif

lib_LTLIBRARIES = $(shared_libraries)

if SHARED_MONO
mini_common_lib = libmini.la
else
mini_common_lib = 
endif

if DISABLE_EXECUTABLES
noinst_LTLIBRARIES = $(mini_common_lib)
else
noinst_LTLIBRARIES = $(mini_common_lib) libmini-static.la
endif

if LOADED_LLVM
lib_LTLIBRARIES += libmono-llvm.la
libmono_llvm_la_SOURCES = mini-llvm.c mini-llvm-cpp.cpp llvm-jit.cpp
libmono_llvm_la_LIBADD = $(GLIB_LIBS) $(LLVM_LIBS) $(LLVM_LDFLAGS)
if PLATFORM_DARWIN
libmono_llvm_la_LDFLAGS=-Wl,-undefined -Wl,suppress -Wl,-flat_namespace
else
libmono_llvm_la_LIBADD += $(top_builddir)/mono/mini/libmonoboehm-$(API_VER).la $(boehm_libs)
endif
endif

mono_boehm_SOURCES = \
	main.c

mono_CFLAGS = $(AM_CFLAGS)

mono_boehm_CFLAGS = $(AM_CFLAGS)

AM_CPPFLAGS = $(LIBGC_CPPFLAGS)

mono_sgen_SOURCES = \
	main-sgen.c

mono_SOURCES = \
	main-sgen.c

mono_sgen_CFLAGS = $(AM_CFLAGS)

# We build this after libmono was built so it contains the date when the final
# link was done
if SUPPORT_BOEHM
if DISABLE_EXECUTABLES
buildver-boehm.h: libmini-static.la $(monodir)/mono/metadata/libmonoruntime.la
else
buildver-boehm.h: libmini-static.la $(monodir)/mono/metadata/libmonoruntime-static.la
endif
	@echo "const char *build_date = \"`date`\";" > buildver-boehm.h
mono_boehm-main.$(OBJEXT): buildver-boehm.h
endif

if DISABLE_EXECUTABLES
buildver-sgen.h: libmini-static.la $(monodir)/mono/metadata/libmonoruntimesgen.la $(monodir)/mono/sgen/libmonosgen.la
else
buildver-sgen.h: libmini-static.la $(monodir)/mono/metadata/libmonoruntimesgen-static.la $(monodir)/mono/sgen/libmonosgen-static.la
endif
	@echo "const char *build_date = \"`date`\";" > buildver-sgen.h
mono_sgen-main-sgen.$(OBJEXT): buildver-sgen.h
main-sgen.$(OBJEXT): buildver-sgen.h

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
	$(MONO_LIB)		\
	$(GLIB_LIBS)		\
	$(LLVMMONOF)		\
	$(LIBICONV)		\
	-lm			\
	$(MONO_DTRACE_OBJECT)

mono_boehm_LDFLAGS = \
	$(static_flags) -export-dynamic $(monobinldflags) $(monobin_platform_ldflags)

mono_sgen_LDADD = \
	$(MONO_SGEN_LIB)	\
	$(GLIB_LIBS)		\
	$(LLVMMONOF)		\
	$(LIBICONV)		\
	-lm			\
	$(MONO_DTRACE_OBJECT)

mono_sgen_LDFLAGS = $(static_flags) -export-dynamic $(monobinldflags) $(monobin_platform_ldflags)

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

genmdesc_SOURCES = \
	mini.h		\
	seq-points.h	\
	genmdesc.c	\
	helpers.c	\
	../metadata/opcodes.c

# Don't link this against libmonoruntime to speed up rebuilds
genmdesc_LDADD = \
	$(monodir)/mono/utils/libmonoutils.la -lm	\
	$(GLIB_LIBS)					\
	$(LIBICONV)

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

if ENABLE_INTERPRETER
interp_sources =	\
	interp/hacks.h		\
	interp/interp.h	\
	interp/interp-internals.h	\
	interp/interp.c	\
	interp/mintops.h	\
	interp/mintops.def	\
	interp/mintops.c	\
	interp/transform.c
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
	debugger-agent.c	\
	xdebug.c			\
	mini-llvm.h			\
	mini-llvm-cpp.h	\
	llvm-jit.h		\
	alias-analysis.c	\
	mini-cross-helpers.c \
	arch-stubs.c		\
	llvm-runtime.h	\
	type-checking.c \
	lldb.h			\
	lldb.c	\
	memory-access.c

test_sources = 			\
	basic-calls.cs 		\
	basic-long.cs 		\
	bench.cs 		\
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
	mixed.cs

if NACL_CODEGEN
test_sources += nacl.cs
endif

regtests_UNIVERSAL = \
	basic.exe \
	basic-float.exe \
	basic-long.exe \
	basic-calls.exe \
	objects.exe \
	arrays.exe \
	basic-math.exe \
	exceptions.exe \
	iltests.exe \
	devirtualization.exe \
	generics.exe \
	basic-simd.exe \
	basic-vectors.exe

if NACL_CODEGEN
regtests_UNIVERSAL += nacl.exe
endif

regtests_DISABLED = 

if FULL_AOT_TESTS
regtests_DISABLED += 
endif

regtests = $(filter-out $(regtests_DISABLED),$(regtests_UNIVERSAL))

if X86
arch_sources = $(x86_sources)
arch_built=cpu-x86.h
arch_define=__i386__
endif

if AMD64
arch_sources = $(amd64_sources)
arch_built=cpu-amd64.h
arch_define=__x86_64__
ARCH_FULLAOT_EXCLUDE=
endif

if POWERPC
arch_sources = $(ppc_sources)
arch_built=cpu-ppc.h
arch_define=__ppc__
endif

if POWERPC64
arch_sources = $(ppc_sources)
arch_built=cpu-ppc64.h
arch_define=__ppc64__
endif

if MIPS
arch_sources = $(mips_sources)
arch_built=cpu-mips.h
arch_define=__mips__
endif

if ARM
# pick up arm_dpimacros.h
ARCH_CFLAGS = -I../arch/arm
arch_sources = $(arm_sources)
arch_built=cpu-arm.h
arch_define=__arm__
endif

if ARM64
arch_sources = $(arm64_sources)
arch_built=cpu-arm64.h
arch_define=__aarch64__
endif

if SPARC
arch_sources = $(sparc_sources)
arch_built=cpu-sparc.h
arch_define=__sparc__
endif

if SPARC64
arch_sources = $(sparc_sources)
arch_built=cpu-sparc.h
arch_define=__sparc__
endif

if S390X
arch_sources = $(s390x_sources)
arch_built=cpu-s390x.h
arch_define=__s390__
endif

if HOST_WIN32
os_sources = $(windows_sources)
monobin_platform_ldflags=
endif

if PLATFORM_SIGPOSIX
os_sources = $(posix_sources)
monobin_platform_ldflags=
endif

if PLATFORM_DARWIN
os_sources = $(darwin_sources) $(posix_sources)
#monobin_platform_ldflags=-sectcreate __TEXT __info_plist $(top_srcdir)/mono/mini/Info.plist -framework CoreFoundation -framework Foundation
monobin_platform_ldflags=-framework CoreFoundation -framework Foundation
endif

libmini_la_SOURCES = $(common_sources) $(llvm_sources) $(llvm_runtime_sources) $(interp_sources) $(arch_sources) $(os_sources)
libmini_la_CFLAGS = $(mono_CFLAGS)

libmonoboehm_2_0_la_SOURCES =
libmonoboehm_2_0_la_CFLAGS = $(mono_boehm_CFLAGS)
libmonoboehm_2_0_la_LIBADD = libmini.la $(boehm_libs) $(LIBMONO_DTRACE_OBJECT) $(LLVMMONOF)
libmonoboehm_2_0_la_LDFLAGS = $(libmonoldflags) $(monobin_platform_ldflags)

libmonosgen_2_0_la_SOURCES =
libmonosgen_2_0_la_CFLAGS = $(mono_sgen_CFLAGS)
libmonosgen_2_0_la_LIBADD = libmini.la $(sgen_libs) $(LIBMONO_DTRACE_OBJECT) $(LLVMMONOF)
libmonosgen_2_0_la_LDFLAGS = $(libmonoldflags) $(monobin_platform_ldflags)

#
# This library is shared between mono and mono-sgen, since the code in mini/ doesn't contain
# compile time dependencies on boehm/sgen.
#
libmini_static_la_SOURCES = $(libmini_la_SOURCES)
libmini_static_la_CFLAGS = $(AM_CFLAGS)
libmini_static_la_LDFLAGS = -static
libmini_static_la_LIBADD = $(MONO_DTRACE_OBJECT)

libmonoincludedir = $(includedir)/mono-$(API_VER)/mono/jit

libmonoinclude_HEADERS = jit.h

CSFLAGS = -unsafe -nowarn:0219,0169,0414,0649,0618

basic-simd.exe: basic-simd.cs TestDriver.dll
	$(MCS) -out:$@ $(CSFLAGS) $< -r:TestDriver.dll -r:$(CLASS)/Mono.Simd.dll

basic-vectors.exe: basic-vectors.cs TestDriver.dll
	$(MCS) -out:$@ $(CSFLAGS) $< -r:TestDriver.dll -r:$(CLASS)/System.Numerics.dll -r:$(CLASS)/System.Numerics.Vectors.dll

nacl.exe: nacl.cs TestDriver.dll
	$(MCS) -out:$@ $(CSFLAGS) $< -r:TestDriver.dll -r:$(CLASS)/Mono.Simd.dll

generics.exe: generics.cs TestDriver.dll generics-variant-types.dll
	$(MCS) -out:$@ $(CSFLAGS) $< -r:TestDriver.dll -r:generics-variant-types.dll -r:$(CLASS)/System.Core.dll

%.exe: %.cs TestDriver.dll
	$(MCS) -out:$@ $(CSFLAGS) $< -r:TestDriver.dll

%.exe: %.il
	$(ILASM) -output=$@ $<

TestDriver.dll: $(srcdir)/TestDriver.cs $(srcdir)/TestHelpers.cs
	$(MCS) -out:$@ -target:library $^

generics-variant-types.dll: generics-variant-types.il
	$(ILASM) -dll -output=$@ $<

if NACL_CODEGEN
GENMDESC_OPTS=--nacl
else !NACL_CODEGEN
GENMDESC_OPTS=
endif !NACL_CODEGEN

# we don't always use the perl impl because it's an additional
# build dependency for the poor windows users
# $(arch_define) is the preprocessor symbol that enables all the opcodes
# for the specific platform in mini-ops.h
if CROSS_COMPILING
GENMDESC_PRG=perl $(srcdir)/genmdesc.pl $(arch_define) $(srcdir) $(GENMDESC_OPTS)
else !CROSS_COMPILING
if NACL_CODEGEN
GENMDESC_PRG=perl $(srcdir)/genmdesc.pl $(arch_define) $(srcdir) $(GENMDESC_OPTS)
else
GENMDESC_PRG=./genmdesc $(GENMDESC_OPTS)
endif
endif !CROSS_COMPILING

cpu-x86.h: cpu-x86.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) cpu-x86.h x86_desc $(srcdir)/cpu-x86.md

cpu-amd64.h: cpu-amd64.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) cpu-amd64.h amd64_desc $(srcdir)/cpu-amd64.md

cpu-ppc.h: cpu-ppc.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) cpu-ppc.h ppcg4 $(srcdir)/cpu-ppc.md

cpu-ppc64.h: cpu-ppc64.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) cpu-ppc64.h ppc64_cpu_desc $(srcdir)/cpu-ppc64.md

cpu-arm.h: cpu-arm.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) cpu-arm.h arm_cpu_desc $(srcdir)/cpu-arm.md

cpu-arm64.h: cpu-arm64.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) cpu-arm64.h arm64_cpu_desc $(srcdir)/cpu-arm64.md

cpu-sparc.h: cpu-sparc.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) cpu-sparc.h sparc_desc $(srcdir)/cpu-sparc.md

cpu-s390x.h: cpu-s390x.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) cpu-s390x.h s390x_cpu_desc $(srcdir)/cpu-s390x.md

cpu-mips.h: cpu-mips.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) cpu-mips.h mips_desc $(srcdir)/cpu-mips.md

testi: mono test.exe
	$(MINI_RUNTIME) -v -v --ncompile 1 --compile Test:$(mtest) test.exe

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
	for i in $(regtests); do $(srcdir)/test_op_il_seq_point.sh $$i || ($(srcdir)/test_op_il_seq_point_headerfooter.sh; exit 1) || exit 1; done
	for i in $(regtests); do $(srcdir)/test_op_il_seq_point.sh $$i --aot || ($(srcdir)/test_op_il_seq_point_headerfooter.sh; exit 1) || exit 1; done
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

fullaot_regtests = $(regtests) aot-tests.exe $(if $(GSHAREDVT),gshared.exe)

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

if FULL_AOT_TESTS
# Skip aoting the tests that aren't compiled 
# on the full aot profiles because they're skipped
# on mobile profiles
FULLAOT_LIBS_DISABLED += \
	Mono.Posix.dll \
	System.Configuration.dll
endif

FULLAOT_LIBS = $(filter-out $(FULLAOT_LIBS_DISABLED),$(FULLAOT_LIBS_UNIVERSAL))

FULLAOT_TMP_DIR=$(top_builddir)/mono/mini/fullaot-tmp

# This currently only works on amd64/arm
fullaotcheck: $(mono) $(fullaot_regtests)
	rm -rf $(FULLAOT_TMP_DIR)
	mkdir $(FULLAOT_TMP_DIR)
	$(MAKE) fullaot-libs AOT_FLAGS="full,$(MONO_FULLAOT_ADDITIONAL_ARGS)$(INVARIANT_AOT_OPTIONS)" GSHAREDVT=$(GSHAREDVT)
	cp $(regtests) $(fullaot_regtests) generics-variant-types.dll TestDriver.dll $(FULLAOT_TMP_DIR)/
	MONO_PATH=$(FULLAOT_TMP_DIR) $(top_builddir)/runtime/mono-wrapper $(MOBILE_RUNTIME_ARG) $(LLVM_AOT_RUNTIME_OPTS) $(GSHAREDVT_RUNTIME_OPTS) --aot="full,$(MONO_FULLAOT_ADDITIONAL_ARGS)$(INVARIANT_AOT_OPTIONS)" $(FULLAOT_TMP_DIR)/{generics-variant-types.dll,TestDriver.dll,*.exe} || exit 1
	ln -s $(if $(MONO_EXECUTABLE),$(MONO_EXECUTABLE),$$PWD/mono) $(FULLAOT_TMP_DIR)/
	for i in $(fullaot_regtests); do echo $$i; MONO_PATH=$(FULLAOT_TMP_DIR) $(top_builddir)/runtime/mono-wrapper $(MOBILE_RUNTIME_ARG) --full-aot $(FULLAOT_TMP_DIR)/$$i --exclude '!FULLAOT' $(ARCH_FULLAOT_EXCLUDE) || exit 1; done

# This can run in parallel
fullaot-libs: $(patsubst %,fullaot-tmp/%.dylib,$(FULLAOT_LIBS))

fullaot-tmp/%.dylib: $(CLASS)/%
	cp $(CLASS)/$* fullaot-tmp/
	mkdir fullaot-tmp/$*-tmp
	MONO_PATH="fullaot-tmp/$(PLATFORM_PATH_SEPARATOR)$(CLASS)" $(top_builddir)/runtime/mono-wrapper $(MOBILE_RUNTIME_ARG) $(if $(GSHAREDVT),-O=gsharedvt) --aot=$(AOT_FLAGS),temp-path=fullaot-tmp/$*-tmp fullaot-tmp/$*
	rm -rf fullaot-tmp/$*-tmp

llvmfullaotcheck:
	$(MAKE) fullaotcheck LLVM=1

llvmonly_regtests = $(fullaot_regtests) gshared.exe

llvmonlycheck: mono $(llvmonly_regtests)
	rm -rf fullaot-tmp
	mkdir fullaot-tmp
	$(MAKE) fullaot-libs AOT_FLAGS="llvmonly,$(MONO_FULLAOT_ADDITIONAL_ARGS)$(INVARIANT_AOT_OPTIONS)"
	cp $(llvmonly_regtests) generics-variant-types.dll TestDriver.dll fullaot-tmp/
	MONO_PATH=fullaot-tmp $(top_builddir)/runtime/mono-wrapper  $(MOBILE_RUNTIME_ARG) --aot=llvmonly fullaot-tmp/{generics-variant-types.dll,TestDriver.dll,*.exe} || exit 1
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
	genmdesc.pl				\
	$(test_sources) 			\
	$(x86_sources) cpu-x86.md 		\
	$(amd64_sources) cpu-amd64.md 		\
	$(ppc_sources) cpu-ppc.md cpu-ppc64.md 	\
	$(arm_sources) cpu-arm.md 		\
	$(arm64_sources) cpu-arm64.md 		\
	$(mips_sources) cpu-mips.md 		\
	$(sparc_sources) cpu-sparc.md 		\
	$(s390x_sources) cpu-s390x.md 		\
	$(windows_sources)			\
	$(darwin_sources) Info.plist		\
	$(posix_sources)					\
	test_op_il_seq_point.sh			\
	test_op_il_seq_point_headerfooter.sh	\
	Makefile.am.in

version.h: Makefile
	if test -d $(top_srcdir)/.git; then \
		(cd $(top_srcdir); \
			LANG=C; export LANG; \
			if test -z "$$ghprbPullId"; then \
				branch=`git branch | grep '^\*' | sed 's/(detached from .*/explicit/' | cut -d ' ' -f 2`; \
			else \
				branch="pull-request-$$ghprbPullId"; \
			fi; \
			version=`git log --no-color --first-parent -n1 --pretty=format:%h`; \
			echo "#define FULL_VERSION \"$$branch/$$version\""; \
		); \
	else \
		echo "#define FULL_VERSION \"tarball\""; \
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

if HAS_EXTENSION_MODULE
else
Makefile.am: Makefile.am.in
	cp $< $@
endif
