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
	$(monodir)/mono/metadata/libmetadata.la	\
	$(monodir)/mono/io-layer/libwapi.la	\
	$(monodir)/mono/utils/libmonoutils.la \
	$(GMODULE_LIBS)	\
	$(GLIB_LIBS)	\
	$(libgc_libs)	\
	$(ICU_LIBS)

static_libs=	\
	$(monodir)/mono/metadata/libmonoruntime-static.la	\
	$(monodir)/mono/metadata/libmetadata-static.la	\
	$(monodir)/mono/io-layer/libwapi.la	\
	$(monodir)/mono/utils/libmonoutils.la \
	$(GMODULE_LIBS)	\
	$(GLIB_LIBS)	\
	$(libgc_static_libs) \
	$(ICU_LIBS)

RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/default ./mono --config $(top_builddir)/data/config

MCS = $(RUNTIME) $(mcs_topdir)/class/lib/default/mcs.exe --unsafe -nowarn:0162
ILASM = $(RUNTIME) $(mcs_topdir)/ilasm/ilasm.exe

INCLUDES = \
	-I$(top_srcdir) 	\
	$(LIBGC_CFLAGS)		\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)

if PLATFORM_WIN32
export HOST_CC
# The mingw math.h has "extern inline" functions that dont appear in libs, so
# optimisation is required to actually inline them
AM_CFLAGS = -O
endif

# hack for automake to have the same source file in a library and a bin
genmdesc_CFLAGS = $(AM_CFLAGS)

if NO_VERSION_SCRIPT
monoldflags=
else
monoldflags=-Wl,-version-script=$(srcdir)/ldscript
endif

if PLATFORM_WIN32
libmono_la_LDFLAGS=-no-undefined -version-info 1:0:0 $(monoldflags)
else
libmono_la_LDFLAGS=$(monoldflags)
endif

if JIT_SUPPORTED
bin_PROGRAMS = mono

noinst_PROGRAMS = genmdesc

lib_LTLIBRARIES = libmono.la
noinst_LTLIBRARIES = libmono-static.la
endif

mono_SOURCES = \
	main.c

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
	$(GMODULE_LIBS) -lm	\
	$(ICU_LIBS)

mono_LDFLAGS = \
	$(static_flags) -export-dynamic $(monoldflags)

genmdesc_SOURCES = \
	mini.h		\
	genmdesc.c	\
	helpers.c

# Don't link this against libmetadata to speed up rebuilds
genmdesc_LDADD = \
	$(monodir)/mono/utils/libmonoutils.la $(monodir)/mono/metadata/opcodes.lo -lm	\
	$(GLIB_LIBS)		\
	$(GMODULE_LIBS)

x86_sources = \
    mini-exceptions.c	\
	mini-x86.c		\
	mini-x86.h		\
	exceptions-x86.c	\
	tramp-x86.c

amd64_sources = \
	mini-exceptions.c	\
	mini-amd64.c		\
	mini-amd64.h		\
	exceptions-amd64.c	\
	tramp-amd64.c

ppc_sources = \
	mini-ppc.c		\
	mini-ppc.h		\
	exceptions-ppc.c	\
	tramp-ppc.c

sparc_sources = \
	mini-sparc.c		\
	mini-sparc.h		\
    mini-exceptions.c	\
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

common_sources = \
	mini.c		\
	mini.h		\
	trace.c		\
	trace.h		\
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
	driver.c	\
	debug-mini.c	\
	linear-scan.c	\
	aot.c		\
	graph.c		\
	declsec.c	\
	declsec.h

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
	iltests.il		\
	test.cs

regtests=basic.exe arrays.exe basic-float.exe basic-math.exe basic-long.exe objects.exe basic-calls.exe iltests.exe exceptions.exe bench.exe

common_BURGSRC= $(srcdir)/inssel.brg $(srcdir)/inssel-float.brg

if X86
libmono_la_SOURCES = \
	$(common_sources) $(x86_sources)

arch_BURGSRC= $(srcdir)/inssel-long32.brg $(srcdir)/inssel-x86.brg
arch_built=cpu-pentium.h
endif

if AMD64
libmono_la_SOURCES = \
	$(common_sources) $(amd64_sources)
arch_BURGSRC= $(srcdir)/inssel-long.brg $(srcdir)/inssel-amd64.brg
arch_built=cpu-amd64.h
endif

if POWERPC
libmono_la_SOURCES = \
	$(common_sources) $(ppc_sources)

arch_BURGSRC= $(srcdir)/inssel-long32.brg $(srcdir)/inssel-ppc.brg
arch_built=cpu-g4.h
endif

if SPARC
libmono_la_SOURCES = \
	$(common_sources) $(sparc_sources)

arch_BURGSRC= $(srcdir)/inssel-long32.brg $(srcdir)/inssel-sparc.brg
arch_built=cpu-sparc.h
endif

if SPARC64
libmono_la_SOURCES = \
	$(common_sources) $(sparc_sources)

arch_BURGSRC= $(srcdir)/inssel-long.brg $(srcdir)/inssel-sparc.brg
arch_built=cpu-sparc.h
endif

if S390
libmono_la_SOURCES = \
	$(common_sources) $(s390_sources)

arch_BURGSRC= $(srcdir)/inssel-long32.brg $(srcdir)/inssel-s390.brg
arch_built=cpu-s390.h
endif

if S390x
libmono_la_SOURCES = \
	$(common_sources) $(s390x_sources)

arch_BURGSRC= $(srcdir)/inssel-long.brg $(srcdir)/inssel-s390x.brg
arch_built=cpu-s390x.h
endif

nodist_libmono_la_SOURCES = inssel.c inssel.h

libmono_static_la_SOURCES = $(libmono_la_SOURCES)
nodist_libmono_static_la_SOURCES = $(nodist_libmono_la_SOURCES)
libmono_static_la_LDFLAGS = -static
libmono_static_la_LIBADD = $(static_libs)

BURGSRC= $(common_BURGSRC) $(arch_BURGSRC)

libmonoincludedir = $(includedir)/mono/jit

libmonoinclude_HEADERS = jit.h

libmono_la_LIBADD = \
	$(libs)

%.exe: %.cs TestDriver.dll
	$(MCS) /out:$*.exe /unsafe $< /r:TestDriver.dll

%.exe: %.il
	$(ILASM) /output=$*.exe $<

TestDriver.dll: $(srcdir)/TestDriver.cs
	$(MCS) /out:TestDriver.dll /target:library $(srcdir)/TestDriver.cs

cpu-pentium.h: cpu-pentium.md genmdesc$(EXEEXT)
	./genmdesc $(srcdir)/cpu-pentium.md cpu-pentium.h pentium_desc

cpu-amd64.h: cpu-amd64.md genmdesc$(EXEEXT)
	./genmdesc $(srcdir)/cpu-amd64.md cpu-amd64.h amd64_desc

cpu-g4.h: cpu-g4.md genmdesc$(EXEEXT)
	./genmdesc $(srcdir)/cpu-g4.md cpu-g4.h ppcg4

cpu-sparc.h: cpu-sparc.md genmdesc$(EXEEXT)
	./genmdesc $(srcdir)/cpu-sparc.md cpu-sparc.h sparc_desc

cpu-s390.h: cpu-s390.md genmdesc$(EXEEXT)
	./genmdesc $(srcdir)/cpu-s390.md cpu-s390.h s390

cpu-s390x.h: cpu-s390x.md genmdesc$(EXEEXT)
	./genmdesc $(srcdir)/cpu-s390x.md cpu-s390x.h s390x

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
	for i in $(regtests); do $(RUNTIME) --aot $$i; done
	$(RUNTIME) --verbose --regression $(regtests)
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
BUILT_SOURCES= inssel.c inssel.h $(arch_built)
endif

CLEANFILES= $(BUILT_SOURCES)
EXTRA_DIST = $(common_BURGSRC) jit-icalls.c cprop.c TestDriver.cs ldscript $(test_sources) \
	inssel-long.brg inssel-long32.brg \
	$(x86_sources) inssel-x86.brg cpu-pentium.md \
	$(amd64_sources) inssel-amd64.brg cpu-amd64.md \
	$(ppc_sources) inssel-ppc.brg cpu-g4.md \
	$(sparc_sources) inssel-sparc.brg cpu-sparc.md \
	$(s390_sources) inssel-s390.brg cpu-s390.md \
	$(s390x_sources) inssel-s390x.brg cpu-s390x.md
