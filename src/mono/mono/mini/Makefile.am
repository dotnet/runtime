count=100000
mtest=for_loop
monodir=$(top_builddir)
libs=	\
	$(monodir)/mono/metadata/libmonoruntime.la	\
	$(monodir)/mono/metadata/libmetadata.la	\
	$(monodir)/mono/io-layer/libwapi.la	\
	$(monodir)/mono/utils/libmonoutils.la

MCS=mcs
RUNTIME=mono
WARN=-Wall -Wunused -Wmissing-prototypes -Wmissing-declarations \
	-Wstrict-prototypes  -Wmissing-prototypes -Wnested-externs \
	-Wpointer-arith -Wno-cast-qual -Wcast-align -Wwrite-strings

INCLUDES = \
	$(WARN) -fexceptions -DMONO_USE_EXC_TABLES	\
	-I$(top_srcdir) 	\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)

if PLATFORM_WIN32
export HOST_CC
endif

# hack for automake to have the same source file in a library and a bin
genmdesc_CFLAGS = $(AM_CFLAGS)

regtests=basic.exe arrays.exe basic-float.exe basic-long.exe objects.exe basic-calls.exe iltests.exe exceptions.exe bench.exe

libmono_la_LDFLAGS=-Wl,-version-script=$(srcdir)/ldscript

if JIT_SUPPORTED
bin_PROGRAMS = mono

noinst_PROGRAMS = genmdesc

lib_LTLIBRARIES = libmono.la
endif

mono_SOURCES = \
	main.c

mono_LDADD = \
	libmono.la		\
	$(GLIB_LIBS)		\
	$(GMODULE_LIBS) -lm

mono_LDFLAGS = \
	-export-dynamic

genmdesc_SOURCES = \
	mini.h		\
	genmdesc.c	\
	helpers.c

genmdesc_LDADD = \
	$(libs) -lm	\
	$(GLIB_LIBS)		\
	$(GMODULE_LIBS)

x86_sources = \
	mini-x86.c		\
	mini-x86.h		\
	exceptions-x86.c	\
	tramp-x86.c

ppc_sources = \
	mini-ppc.c		\
	mini-ppc.h		\
	exceptions-ppc.c	\
	tramp-ppc.c

common_sources = \
	mini.c		\
	mini.h		\
	mini-ops.h	\
	mini-arch.h	\
	dominators.c	\
	cfold.c		\
	regalloc.c	\
	regalloc.h	\
	inssel.c	\
	helpers.c	\
	liveness.c	\
	ssa.c		\
	driver.c	\
	debug-mini.c	\
	linear-scan.c	\
	aot.c		\
	graph.c	

common_BURGSRC= $(srcdir)/inssel.brg $(srcdir)/inssel-long32.brg $(srcdir)/inssel-float.brg

if X86
libmono_la_SOURCES = \
	$(common_sources) $(x86_sources)

BURGSRC= $(common_BURGSRC) $(srcdir)/inssel-x86.brg
endif

if POWERPC
libmono_la_SOURCES = \
	$(common_sources) $(ppc_sources)

BURGSRC= $(common_BURGSRC) $(srcdir)/inssel-ppc.brg
endif

libmono_la_LIBADD = \
	$(libs)

%.exe: %.cs TestDriver.dll
	$(MCS) /unsafe $< /r:TestDriver.dll

%.exe: %.il
	ilasm /OUTPUT=$*.exe $<

TestDriver.dll: TestDriver.cs
	$(MCS) /out:TestDriver.dll /target:library TestDriver.cs

cpu-pentium.h: cpu-pentium.md genmdesc$(EXEEXT)
	./genmdesc $(srcdir)/cpu-pentium.md cpu-pentium.h pentium

cpu-g4.h: cpu-g4.md genmdesc$(EXEEXT)
	./genmdesc $(srcdir)/cpu-g4.md cpu-g4.h ppcg4

inssel.c inssel.h: $(BURGSRC)
	$(monodir)/mono/monoburg/monoburg -c 1 -p -e $(BURGSRC) -d inssel.h -s inssel.c

testi: mono test.exe
	./mono -v -v --ncompile 1 --compile Test:$(mtest) test.exe

# ensure the tests are actually correct
checktests: $(regtests)
	for i in $(regtests); do $(RUNTIME) $$i; done

rcheck: mono $(regtests)
	./mono --verbose --regression $(regtests)

aotcheck: mono $(regtests)
	for i in $(regtests); do ./mono --aot $$i; done
	./mono --verbose --regression $(regtests)
	rm -f *.exe.so

bench: mono test.exe
	time ./mono --ncompile $(count) --compile Test:$(mtest) test.exe

mbench: test.exe
	time $(monodir)/mono/jit/mono --ncompile $(count) --compile Test:$(mtest) test.exe

stat1: mono bench.exe
	./mono --verbose --statfile stats.pl --regression bench.exe
	perl viewstat.pl stats.pl

stat2: mono basic.exe
	./mono --verbose --statfile stats.pl --regression basic.exe
	perl viewstat.pl -e stats.pl

stat3: mono bench.exe
	./mono --statfile stats.pl --ncompile 1000 --compile Tests:test_0_many_nested_loops bench.exe 
	perl viewstat.pl stats.pl

docu: mini.sgm
	docbook2txt mini.sgm

clean:
	rm -f mono a.out gmon.out *.o test.exe

pkgconfigdir = $(libdir)/pkgconfig

BUILT_SOURCES= inssel.c inssel.h cpu-pentium.h cpu-g4.h
CLEANFILES= $(BUILT_SOURCES)
EXTRA_DIST = $(common_BURGSRC) inssel-x86.brg \
	jit-icalls.c cprop.c \
	cpu-pentium.md cpu-g4.md ldscript


