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
	-I$(top_srcdir) $(MINI_CFLAGS)

# hack for automake to have the same source file in a library and a bin
genmdesc_CFLAGS = $(AM_CFLAGS)

regtests=basic.exe arrays.exe basic-float.exe basic-long.exe objects.exe iltests.exe exceptions.exe bench.exe

bin_PROGRAMS = mini genmdesc

lib_LTLIBRARIES = libmini.la

mini_SOURCES = \
	main.c

mini_LDADD = \
	libmini.la

genmdesc_SOURCES = \
	mini.h		\
	genmdesc.c	\
	helpers.c

genmdesc_LDADD = \
	$(libs) -lgc -lm	\
	$(MINI_LIBS)

arch_sources = \
	mini-x86.c		\
	exceptions-x86.c	\
	tramp-x86.c

libmini_la_SOURCES = \
	mini.c		\
	dominators.c	\
	cfold.c		\
	regalloc.c	\
	inssel.c	\
	regset.c	\
	helpers.c	\
	liveness.c	\
	ssa.c		\
	driver.c	\
	debug.c		\
	debug-stabs.c	\
	debug-dwarf2.c	\
	debug-mini.c	\
	linear-scan.c	\
	aot.c		\
	graph.c		\
	$(arch_sources)

libmini_la_LIBADD = \
	$(libs) -lgc -lm	\
	$(MINI_LIBS)

%.exe: %.cs TestDriver.dll
	$(MCS) /unsafe $< /r:TestDriver.dll

%.exe: %.il
	ilasm /OUTPUT=$*.exe $<

TestDriver.dll: TestDriver.cs
	$(MCS) /out:TestDriver.dll /target:library TestDriver.cs

cpu-pentium.h: cpu-pentium.md genmdesc
	./genmdesc cpu-pentium.md cpu-pentium.h pentium

BURGSRC= inssel.brg inssel-x86.brg inssel-long32.brg inssel-float.brg

inssel.c inssel.h: $(BURGSRC)
	$(monodir)/mono/monoburg/monoburg -c 1 -p -e $(BURGSRC) -d inssel.h -s inssel.c

testi: mini test.exe
	./mini -v -v --ncompile 1 --compile Test:$(mtest) test.exe

# ensure the tests are actually correct
checktests: $(regtests)
	for i in $(regtests); do $(RUNTIME) $$i; done

check: mini $(regtests)
	./mini --verbose --regression $(regtests)

aotcheck: mini $(regtests)
	for i in $(regtests); do ./mini --aot $$i; done
	./mini --verbose --regression $(regtests)
	rm -f *.exe.so

bench: mini test.exe
	time ./mini --ncompile $(count) --compile Test:$(mtest) test.exe

mbench: test.exe
	time $(monodir)/mono/jit/mono --ncompile $(count) --compile Test:$(mtest) test.exe

stat1: mini bench.exe
	./mini --verbose --statfile stats.pl --regression bench.exe
	perl viewstat.pl stats.pl

stat2: mini basic.exe
	./mini --verbose --statfile stats.pl --regression basic.exe
	perl viewstat.pl -e stats.pl

stat3: mini bench.exe
	./mini --statfile stats.pl --ncompile 1000 --compile Tests:test_0_many_nested_loops bench.exe 
	perl viewstat.pl stats.pl

docu: mini.sgm
	docbook2txt mini.sgm

clean:
	rm -f mini a.out gmon.out *.o test.exe

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA= mini.pc
DISTCLEANFILES= mini.pc

BUILT_SOURCES= inssel.c inssel.h cpu-pentium.h
CLEANFILES= $(BUILT_SOURCES)
EXTRA_DIST = mini.pc.in

