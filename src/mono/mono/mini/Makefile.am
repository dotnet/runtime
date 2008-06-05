count=100000
mtest=for_loop
monodir=$(top_builddir)

# This is needed for automake dependency generation
if INCLUDED_LIBGC
libgc_libs=$(monodir)/libgc/libmonogc.la
libgc_static_libs=$(monodir)/libgc/libmonogc-static.la
else
libgc_libs=$(LIBGC_LIBS)
libgc_static_libs=$(LIBGC_STATIC_LIBS)
endif

libs=	\
	$(monodir)/mono/metadata/libmonoruntime.la	\
	$(monodir)/mono/io-layer/libwapi.la	\
	$(monodir)/mono/utils/libmonoutils.la \
	$(GLIB_LIBS)	\
	$(libgc_libs)

static_libs=	\
	$(monodir)/mono/metadata/libmonoruntime-static.la	\
	$(monodir)/mono/io-layer/libwapi.la	\
	$(monodir)/mono/utils/libmonoutils.la \
	$(GLIB_LIBS)	\
	$(libgc_static_libs)

CLASS=$(mcs_topdir)/class/lib/net_2_0

RUNTIME = MONO_PATH=$(CLASS) $(top_builddir)/runtime/mono-wrapper
RUNTIME_AOTCHECK = MONO_PATH=$(CLASS):. $(top_builddir)/runtime/mono-wrapper

MCS = $(RUNTIME) $(CLASS)/gmcs.exe -unsafe -nowarn:0162
ILASM = $(RUNTIME) $(CLASS)/ilasm.exe

AM_CFLAGS = \
	-I$(top_srcdir) 	\
	$(LIBGC_CFLAGS)		\
	$(GLIB_CFLAGS)
	$(PLATFORM_CFLAGS) $(ARCH_CFLAGS)

if PLATFORM_WIN32
export HOST_CC
# The mingw math.h has "extern inline" functions that dont appear in libs, so
# optimisation is required to actually inline them
PLATFORM_CFLAGS = -O
endif

# hack for automake to have the same source file in a library and a bin
genmdesc_CFLAGS = $(AM_CFLAGS)

if NO_VERSION_SCRIPT
monoldflags=$(export_ldflags)
monobinldflags=$(export_ldflags)
else
monoldflags=-Wl,-version-script=$(srcdir)/ldscript $(export_ldflags)
monobinldflags=-Wl,-version-script=$(srcdir)/ldscript.mono $(export_ldflags)
endif

if PLATFORM_WIN32
libmono_la_LDFLAGS=-no-undefined -avoid-version -Wl,--kill-at $(monoldflags)
else
libmono_la_LDFLAGS=$(monoldflags)
endif

if JIT_SUPPORTED

if PLATFORM_WIN32
bin_PROGRAMS = mono monow
else
bin_PROGRAMS = mono
endif

noinst_PROGRAMS = genmdesc

lib_LTLIBRARIES = libmono.la
noinst_LTLIBRARIES = libmono-static.la
endif

mono_SOURCES = \
	main.c

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
MONO_LIB=libmono-static.la
else 
MONO_LIB=libmono.la
endif

mono_LDADD = \
	$(MONO_LIB)			\
	$(GLIB_LIBS)		\
	-lm	\
	$(MONO_DTRACE_OBJECT)

mono_LDFLAGS = \
	$(static_flags) -export-dynamic $(monobinldflags)

if DTRACE_G_REQUIRED

mono-dtrace.$(OBJEXT): $(top_srcdir)/data/mono.d mini.lo $(monodir)/mono/metadata/libmonoruntime-static.la
	DTRACE="$(DTRACE)" DTRACEFLAGS="$(DTRACEFLAGS)" AR="$(AR)" $(SHELL) $(top_srcdir)/dtrace-prelink.sh \
	$@ $(top_srcdir)/data/mono.d $(monodir)/mono/metadata/libmonoruntime-static.la mini.lo

.libs/mono-dtrace.$(OBJEXT): $(top_srcdir)/data/mono.d mini.lo $(monodir)/mono/metadata/libmonoruntime.la
	DTRACE="$(DTRACE)" DTRACEFLAGS="$(DTRACEFLAGS)" AR="$(AR)" $(SHELL) $(top_srcdir)/dtrace-prelink.sh \
	--pic $@ $(top_srcdir)/data/mono.d $(monodir)/mono/metadata/libmonoruntime.la mini.lo

endif

# Create monow.exe, linked for the 'windows' subsystem
if PLATFORM_WIN32
monow_LDADD = $(mono_LDADD)
monow_LDFLAGS = $(mono_LDFLAGS) -mwindows
monow_SOURCES = $(mono_SOURCES)
endif

genmdesc_SOURCES = \
	mini.h		\
	genmdesc.c	\
	helpers.c

# Don't link this against libmonoruntime to speed up rebuilds
genmdesc_LDADD = \
	$(monodir)/mono/utils/libmonoutils.la $(monodir)/mono/metadata/opcodes.lo -lm	\
	$(GLIB_LIBS)

x86_sources = \
	mini-x86.c		\
	mini-x86.h		\
	exceptions-x86.c	\
	tramp-x86.c

amd64_sources = \
	mini-amd64.c		\
	mini-amd64.h		\
	exceptions-amd64.c	\
	tramp-amd64.c

ppc_sources = \
	mini-ppc.c		\
	mini-ppc.h		\
	exceptions-ppc.c	\
	tramp-ppc.c

arm_sources = \
	mini-arm.c		\
	mini-arm.h		\
	exceptions-arm.c	\
	tramp-arm.c

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

s390_sources = \
	mini-s390.c		\
	mini-s390.h		\
	exceptions-s390.c	\
	tramp-s390.c

s390x_sources = \
	mini-s390x.c		\
	mini-s390x.h		\
	exceptions-s390x.c	\
	tramp-s390x.c

ia64_sources = \
	mini-ia64.c			\
	mini-ia64.h			\
	exceptions-ia64.c	\
	tramp-ia64.c

alpha_sources = \
	mini-alpha.c	\
	mini-alpha.h	\
	exceptions-alpha.c	\
	tramp-alpha.c

hppa_sources = \
	mini-hppa.c	\
	mini-hppa.h	\
	exceptions-hppa.c	\
	tramp-hppa.c

common_sources = \
	mini.c		\
	mini.h		\
	version.h	\
	optflags-def.h		\
	jit-icalls.h \
	jit-icalls.c \
	trace.c		\
	trace.h		\
	patch-info.h	\
	mini-ops.h	\
	mini-arch.h	\
	dominators.c	\
	cfold.c		\
	regalloc.c	\
	regalloc.h	\
	helpers.c	\
	liveness.c	\
	ssa.c		\
	abcremoval.c	\
	abcremoval.h	\
	ssapre.c	\
	ssapre.h	\
	aliasing.c	\
	aliasing.h	\
	local-propagation.c	\
	simple-cee-ops.h \
	simple-mini-ops.h \
	driver.c	\
	debug-mini.c	\
	debug-mini.h	\
	linear-scan.c	\
	aot-compiler.c		\
	aot-runtime.c		\
	graph.c		\
	mini-exceptions.c	\
	mini-codegen.c		\
	mini-trampolines.c  \
	declsec.c	\
	declsec.h	\
	wapihandles.c	\
	branch-opts.c	\
	generic-sharing.c

test_sources = 		\
	basic-calls.cs 	\
	basic-long.cs 	\
	bench.cs 		\
	objects.cs 		\
	arrays.cs		\
	basic-float.cs	\
	basic-math.cs	\
	basic.cs		\
	exceptions.cs	\
	devirtualization.cs	\
	iltests.il.in		\
	test.cs			\
	generics.cs	\
	generics-variant-types.il

if MONO_DEBUGGER_SUPPORTED
if AMD64
mono_debugger_arch_sources = mdb-debug-info64.s
else
if X86
mono_debugger_arch_sources = mdb-debug-info32.s
endif
endif
mono_debugger_sources = debug-debugger.c debug-debugger.h $(mono_debugger_arch_sources)
else
mono_debugger_sources =
endif

regtests=basic.exe basic-float.exe basic-long.exe basic-calls.exe objects.exe arrays.exe basic-math.exe exceptions.exe iltests.exe devirtualization.exe generics.exe bench.exe

common_BURGSRC= $(srcdir)/inssel.brg $(srcdir)/inssel-float.brg

if X86
arch_sources = $(x86_sources) $(mono_debugger_sources)
arch_BURGSRC= $(srcdir)/inssel-long32.brg $(srcdir)/inssel-x86.brg
arch_built=cpu-x86.h
arch_define=__i386__
endif

if AMD64
arch_sources = $(amd64_sources) $(mono_debugger_sources)
arch_BURGSRC= $(srcdir)/inssel-long.brg $(srcdir)/inssel-amd64.brg
arch_built=cpu-amd64.h
arch_define=__x86_64__
endif

if POWERPC
arch_sources = $(ppc_sources)
arch_BURGSRC= $(srcdir)/inssel-long32.brg $(srcdir)/inssel-ppc.brg
arch_built=cpu-ppc.h
arch_define=__ppc__
endif

if MIPS
arch_sources = $(mips_sources)
arch_BURGSRC= $(srcdir)/inssel-long32-mips.brg $(srcdir)/inssel-mips.brg
arch_built=cpu-mips.h
arch_define=__mips__
endif

if ARM
# pick up arm_dpimacros.h and arm_fpamacros.h
ARCH_CFLAGS = -I../arch/arm
arch_sources = $(arm_sources)
arch_BURGSRC= $(srcdir)/inssel-long32.brg $(srcdir)/inssel-arm.brg $(srcdir)/inssel-softfloat.brg
arch_built=cpu-arm.h
arch_define=__arm__
endif

if SPARC
arch_sources = $(sparc_sources)
arch_BURGSRC= $(srcdir)/inssel-long32.brg $(srcdir)/inssel-sparc.brg
arch_built=cpu-sparc.h
arch_define=__sparc__
endif

if SPARC64
arch_sources = $(sparc_sources)
arch_BURGSRC= $(srcdir)/inssel-long.brg $(srcdir)/inssel-sparc.brg
arch_built=cpu-sparc.h
arch_define=__sparc__
endif

if S390
arch_sources = $(s390_sources)
arch_BURGSRC= $(srcdir)/inssel-long32.brg $(srcdir)/inssel-s390.brg
arch_built=cpu-s390.h
arch_define=__s390__
endif

if S390x
arch_sources = $(s390x_sources)
arch_BURGSRC= $(srcdir)/inssel-long.brg $(srcdir)/inssel-s390x.brg
arch_built=cpu-s390x.h
arch_define=__s390__
endif

if IA64
arch_sources = $(ia64_sources)
arch_BURGSRC = $(srcdir)/inssel-long.brg $(srcdir)/inssel-ia64.brg
arch_built = cpu-ia64.h
arch_define=__ia64__
endif

if ALPHA
arch_sources = $(alpha_sources) $(mono_debugger_sources)
arch_BURGSRC = $(srcdir)/inssel-long.brg $(srcdir)/inssel-alpha.brg
arch_built = cpu-alpha.h
arch_define=__alpha__
endif

if HPPA
# Only support 32-bit targets for now
arch_sources = $(hppa_sources)
arch_BURGSRC = $(srcdir)/inssel-long32.brg $(srcdir)/inssel-hppa.brg
arch_built = cpu-hppa.h
arch_define=__hppa__
endif

libmono_la_SOURCES = $(common_sources) $(arch_sources)

nodist_libmono_la_SOURCES = inssel.c inssel.h

libmono_static_la_SOURCES = $(libmono_la_SOURCES)
nodist_libmono_static_la_SOURCES = $(nodist_libmono_la_SOURCES)
libmono_static_la_LDFLAGS = -static
libmono_static_la_LIBADD = $(static_libs) $(MONO_DTRACE_OBJECT)

BURGSRC= $(common_BURGSRC) $(arch_BURGSRC)

libmonoincludedir = $(includedir)/mono-$(API_VER)/mono/jit

libmonoinclude_HEADERS = jit.h

libmono_la_LIBADD = \
	$(libs) $(LIBMONO_DTRACE_OBJECT)

generics.exe: generics.cs TestDriver.dll generics-variant-types.dll
	$(MCS) -out:$@ $< -r:TestDriver.dll -r:generics-variant-types.dll

%.exe: %.cs TestDriver.dll
	$(MCS) -out:$@ -unsafe $< -r:TestDriver.dll

iltests.il: iltests.il.in Makefile.am
	echo '// DO NOT EDIT: This file has been generated from iltests.il.in' > iltests.il
	cpp -Darch=$(arch_define) < $(srcdir)/iltests.il.in | sed 's/^#.*//' >> iltests.il

%.exe: %.il
	$(ILASM) -output=$@ $<

TestDriver.dll: $(srcdir)/TestDriver.cs
	$(MCS) -out:$@ -target:library $<

generics-variant-types.dll: generics-variant-types.il
	$(ILASM) -dll -output=$@ $<

# we don't always use the perl impl because it's an additional
# build dependency for the poor windows users
# $(arch_define) is the preprocessor symbol that enables all the opcodes
# for the specific platform in mini-ops.h
if CROSS_COMPILING
GENMDESC_PRG=perl genmdesc.pl $(arch_define) $(srcdir)
else !CROSS_COMPILING
GENMDESC_PRG=./genmdesc
endif !CROSS_COMPILING

cpu-x86.h: cpu-x86.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) $(srcdir)/cpu-x86.md cpu-x86.h x86_desc

cpu-amd64.h: cpu-amd64.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) $(srcdir)/cpu-amd64.md cpu-amd64.h amd64_desc

cpu-ppc.h: cpu-ppc.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) $(srcdir)/cpu-ppc.md cpu-ppc.h ppcg4

cpu-arm.h: cpu-arm.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) $(srcdir)/cpu-arm.md cpu-arm.h arm_cpu_desc

cpu-sparc.h: cpu-sparc.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) $(srcdir)/cpu-sparc.md cpu-sparc.h sparc_desc

cpu-s390.h: cpu-s390.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) $(srcdir)/cpu-s390.md cpu-s390.h s390_cpu_desc

cpu-s390x.h: cpu-s390x.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) $(srcdir)/cpu-s390x.md cpu-s390x.h s390x_cpu_desc

cpu-ia64.h: cpu-ia64.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) $(srcdir)/cpu-ia64.md cpu-ia64.h ia64_desc

cpu-alpha.h: cpu-alpha.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) $(srcdir)/cpu-alpha.md cpu-alpha.h alpha_desc

cpu-hppa.h: cpu-hppa.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) $(srcdir)/cpu-hppa.md cpu-hppa.h hppa_desc

cpu-mips.h: cpu-mips.md genmdesc$(EXEEXT)
	$(GENMDESC_PRG) $(srcdir)/cpu-mips.md cpu-mips.h mips_desc

inssel.c inssel.h: $(BURGSRC)
	$(monodir)/mono/monoburg/monoburg -c 1 -p -e $(BURGSRC) -d inssel.h -s inssel.c

testi: mono test.exe
	$(RUNTIME) -v -v --ncompile 1 --compile Test:$(mtest) test.exe

# ensure the tests are actually correct
checktests: $(regtests)
	for i in $(regtests); do $(RUNTIME) $$i; done

rcheck: mono $(regtests)
	$(RUNTIME) --regression $(regtests)

aotcheck: mono $(regtests)
	for i in $(regtests); do $(RUNTIME) --aot $$i || exit 1; done
	for i in $(regtests); do $(RUNTIME_AOTCHECK) --regression $$i || exit 1; done
	rm -f *.exe.so

bench: mono test.exe
	time env $(RUNTIME) --ncompile $(count) --compile Test:$(mtest) test.exe

mbench: test.exe
	time $(monodir)/mono/jit/mono --ncompile $(count) --compile Test:$(mtest) test.exe

stat1: mono bench.exe
	$(RUNTIME) --verbose --statfile stats.pl --regression bench.exe
	perl viewstat.pl stats.pl

stat2: mono basic.exe
	$(RUNTIME) --verbose --statfile stats.pl --regression basic.exe
	perl viewstat.pl -e stats.pl

stat3: mono bench.exe
	$(RUNTIME) --statfile stats.pl --ncompile 1000 --compile Tests:test_0_many_nested_loops bench.exe 
	perl viewstat.pl stats.pl

docu: mini.sgm
	docbook2txt mini.sgm

check-local: rcheck

clean-local:
	rm -f mono a.out gmon.out *.o test.exe

pkgconfigdir = $(libdir)/pkgconfig

if JIT_SUPPORTED
BUILT_SOURCES = version.h inssel.c inssel.h $(arch_built)
else
BUILT_SOURCES = version.h
endif

CLEANFILES= $(BUILT_SOURCES) *.exe *.dll
EXTRA_DIST = $(common_BURGSRC) cprop.c TestDriver.cs ldscript ldscript.mono \
	genmdesc.pl	\
	$(test_sources) \
	inssel-long.brg inssel-long32.brg \
	inssel-softfloat.brg	\
	$(x86_sources) inssel-x86.brg cpu-x86.md \
	$(amd64_sources) inssel-amd64.brg cpu-amd64.md \
	$(ppc_sources) inssel-ppc.brg cpu-ppc.md \
	$(arm_sources) inssel-arm.brg cpu-arm.md \
	$(mips_sources) inssel-mips.brg inssel-long32-mips.brg cpu-mips.md \
	$(sparc_sources) inssel-sparc.brg cpu-sparc.md \
	$(s390_sources) inssel-s390.brg cpu-s390.md \
	$(s390x_sources) inssel-s390x.brg cpu-s390x.md \
	$(ia64_sources) inssel-ia64.brg cpu-ia64.md \
	$(alpha_sources) inssel-alpha.brg cpu-alpha.md \
	$(hppa_sources) inssel-hppa.brg cpu-hppa.md

version.h: Makefile
	if test -d $(top_srcdir)/.git/svn; then svn_info='git svn info'; fi; \
	if test -d $(srcdir)/.svn; then svn_info='svn info'; fi; \
	if test -n "$$svn_info"; then \
		(cd $(top_srcdir); \
			LANG=C; export LANG; \
			branch=`$$svn_info | grep URL | sed -e 's/.*source//' -e 's,mono$$,,'`; \
			version=`$$svn_info | grep Revision | sed 's/.*: //'`; \
			echo "#define FULL_VERSION \"$$branch r$$version\""; \
		); \
	else \
		echo "#define FULL_VERSION \"tarball\""; \
	fi > version.h
