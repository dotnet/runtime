count=100000
mtest=for_loop
monodir=../mono/
libs=	\
	$(monodir)/mono/metadata/libmonoruntime.la	\
	$(monodir)/mono/metadata/libmetadata.la	\
	$(monodir)/mono/io-layer/libwapi.la	\
	$(monodir)/mono/utils/libmonoutils.la
debugger_libs=	\
	../debugger/wrapper/libmonodebuggerwrapper.a

MCS=mcs
RUNTIME=mono
CC=gcc
WARN=-Wall -Wunused -Wmissing-prototypes -Wmissing-declarations \
	-Wstrict-prototypes  -Wmissing-prototypes -Wnested-externs \
	-Wpointer-arith -Wno-cast-qual -Wcast-align -Wwrite-strings
CFLAGS=-g $(WARN) -fexceptions -DMONO_USE_EXC_TABLES
CPPFLAGS=-g $(WARN) -I$(monodir) `pkg-config --cflags glib-2.0 gmodule-2.0 gthread-2.0`
LDFLAGS=-g $(libs) -lgc -lm `pkg-config --libs glib-2.0 gmodule-2.0 gthread-2.0`
debugger_LDFLAGS=-g $(libs) $(debugger_libs) -lgc -lm `pkg-config --libs glib-2.0 gmodule-2.0 gthread-2.0`

arch_objects=mini-x86.o exceptions-x86.o tramp-x86.o
lib_objects=mini.o dominators.o cfold.o regalloc.o inssel.o regset.o helpers.o liveness.o ssa.o driver.o \
	debug.o debug-stabs.o debug-dwarf2.o debug-mini.o linear-scan.o aot.o graph.o $(arch_objects)
objects=main.o $(lib_objects)
debugger_objects=debugger-main.o $(lib_objects)
regtests=basic.exe arrays.exe basic-float.exe basic-long.exe objects.exe iltests.exe exceptions.exe bench.exe

%.o: %.c mini.h makefile regset.h mini-x86.h inssel.h cpu-pentium.h mini-ops.h
	$(CC) -c $< $(CFLAGS) $(CPPFLAGS)

%.exe: %.cs TestDriver.dll
	$(MCS) /unsafe $< /r:TestDriver.dll

%.exe: %.il
	ilasm /OUTPUT=$*.exe $<

all: mini genmdesc

TestDriver.dll: TestDriver.cs
	$(MCS) /out:TestDriver.dll /target:library TestDriver.cs

mini: mini.h $(objects) $(libs) makefile cpu-pentium.h jit-icalls.c
	libtool --mode=link gcc -o mini $(objects) $(LDFLAGS)

mono-debugger-mini-wrapper: mini.h $(debugger_objects) $(libs) makefile cpu-pentium.h jit-icalls.c
	libtool --mode=link gcc -o mono-debugger-mini-wrapper $(debugger_objects) $(debugger_LDFLAGS)

genmdesc: mini.h genmdesc.o helpers.o
	libtool --mode=link gcc -o genmdesc genmdesc.o helpers.o $(LDFLAGS)

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

