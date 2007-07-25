SUBDIRS = cas

check-local: test

TEST_PROG = ../interpreter/mint
JITTEST_PROG = ../mini/mono

TEST_PROG_RUN = MONO_SHARED_DIR=$(mono_build_root)/runtime MONO_CFG_DIR=$(mono_build_root)/runtime/etc $(LIBTOOL) --mode=execute $(TEST_PROG)
JITTEST_PROG_RUN = MONO_SHARED_DIR=$(mono_build_root)/runtime MONO_CFG_DIR=$(mono_build_root)/runtime/etc $(LIBTOOL) --mode=execute $(JITTEST_PROG)

RUNTIME_ARGS=--config tests-config --optimize=all

with_mono_path = MONO_PATH=$(mcs_topdir)/class/lib/default
with_mono_path2 = MONO_PATH=$(mcs_topdir)/class/lib/net_2_0

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper
RUNTIME2 = $(with_mono_path2) $(top_builddir)/runtime/mono-wrapper

MKBUNDLE = \
	PKG_CONFIG_PATH=$(top_builddir):$(PKG_CONFIG_PATH) \
	$(RUNTIME) $(mcs_topdir)/class/lib/default/mkbundle.exe

MCS = $(RUNTIME) $(mcs_topdir)/class/lib/default/mcs.exe -unsafe -nowarn:0162 -nowarn:0168 -nowarn:0219
GMCS = $(RUNTIME2) $(mcs_topdir)/class/lib/net_2_0/gmcs.exe -unsafe -nowarn:0162 -nowarn:0168 -nowarn:0219
ILASM = $(RUNTIME) $(mcs_topdir)/class/lib/default/ilasm.exe
ILASM2 = $(RUNTIME2) $(mcs_topdir)/class/lib/net_2_0/ilasm.exe

BENCHSRC=fib.cs random.cs nested-loops.cs ackermann.cs tight-loop.cs sieve.cs

STRESS_TESTS_SRC=	\
	abort-stress-1.cs	\
	abort-stress-2.cs	\
	abort-stress-3.cs	\
	domain-stress.cs	\
	gchandle-stress.cs	\
	monitor-stress.cs	\
	thread-stress.cs	\
	gc-stress.cs

TEST_CS_SRC=			\
	array-init.cs		\
	arraylist.cs		\
	checked.cs		\
	char-isnumber.cs	\
	create-instance.cs	\
	field-layout.cs		\
	pack-layout.cs		\
	pack-bug.cs		\
	hash-table.cs		\
	test-ops.cs		\
	obj.cs			\
	test-dup-mp.cs		\
	string.cs		\
	stringbuilder.cs	\
	switch.cs		\
	outparm.cs		\
	delegate.cs		\
	bitconverter.cs		\
	exception.cs		\
	exception2.cs		\
	exception3.cs		\
	exception4.cs		\
	exception5.cs		\
	exception6.cs		\
	exception7.cs		\
	exception8.cs		\
	exception10.cs		\
	exception11.cs		\
	exception12.cs		\
	exception13.cs		\
	exception14.cs		\
	exception15.cs		\
	exception16.cs		\
	typeload-unaligned.cs	\
	struct.cs		\
	valuetype-gettype.cs	\
	typeof-ptr.cs		\
	static-constructor.cs	\
	pinvoke.cs		\
	pinvoke2.cs		\
	pinvoke3.cs		\
	pinvoke11.cs		\
	pinvoke13.cs		\
	pinvoke17.cs		\
	invoke.cs		\
	invoke2.cs		\
	runtime-invoke.cs		\
	invoke-string-ctors.cs		\
	reinit.cs		\
	box.cs			\
	array.cs		\
	enum.cs			\
	enum2.cs		\
	property.cs		\
	enumcast.cs		\
	assignable-tests.cs	\
	array-cast.cs		\
	array-subtype-attr.cs	\
	cattr-compile.cs	\
	cattr-field.cs		\
	cattr-object.cs		\
	custom-attr.cs		\
	double-cast.cs		\
	newobj-valuetype.cs	\
	arraylist-clone.cs	\
	setenv.cs		\
	vtype.cs		\
	isvaluetype.cs		\
	iface6.cs		\
	iface7.cs		\
	ipaddress.cs		\
	array-vt.cs		\
	interface1.cs		\
	reflection-enum.cs	\
	reflection-prop.cs	\
	reflection4.cs		\
	reflection5.cs		\
	reflection-const-field.cs \
	many-locals.cs		\
	string-compare.cs	\
	test-prime.cs		\
	params.cs		\
	reflection.cs		\
	interface.cs		\
	iface.cs		\
	iface2.cs		\
	iface3.cs		\
	iface4.cs		\
	iface-large.cs		\
	virtual-method.cs	\
	intptrcast.cs		\
	indexer.cs		\
	stream.cs		\
	console.cs		\
	shift.cs		\
	jit-int.cs		\
	jit-uint.cs		\
	jit-long.cs		\
	long.cs			\
	jit-ulong.cs		\
	jit-float.cs		\
	pop.cs			\
	time.cs			\
	appdomain.cs        \
	appdomain1.cs		\
	appdomain2.cs		\
	appdomain-client.cs	\
	appdomain-unload.cs \
	loader.cs       \
	pointer.cs		\
	hashcode.cs		\
	delegate1.cs		\
	delegate2.cs		\
	delegate3.cs		\
	delegate5.cs		\
	delegate6.cs		\
	delegate7.cs		\
	delegate8.cs		\
	remoting1.cs		\
	remoting2.cs		\
	remoting3.cs		\
	remoting4.cs		\
	remoting5.cs		\
	nonvirt.cs		\
	largeexp.cs		\
	largeexp2.cs		\
	marshalbyref1.cs	\
	static-ctor.cs		\
	inctest.cs		\
	bound.cs		\
	array-invoke.cs		\
	test-arr.cs		\
	decimal.cs		\
	decimal-array.cs	\
	marshal.cs		\
	marshal1.cs		\
	marshal2.cs		\
	marshal3.cs		\
	marshal5.cs		\
	marshal6.cs		\
	marshal7.cs		\
	marshal8.cs		\
	marshal9.cs		\
	test-byval-in-struct.cs	\
	thread.cs		\
	thread5.cs		\
	thread6.cs		\
	thread-static.cs	\
	thread-static-init.cs	\
	context-static.cs	\
	float-pop.cs		\
	interfacecast.cs	\
	array3.cs		\
	classinit.cs		\
	classinit2.cs		\
	synchronized.cs		\
	async_read.cs		\
	threadpool.cs		\
	threadpool1.cs		\
	base-definition.cs	\
	bug-27420.cs		\
	bug-47295.cs		\
	bug-46781.cs		\
	bug-48015.cs		\
	bug-42136.cs		\
	bug-59286.cs		\
	bug-70561.cs		\
	bug-78311.cs		\
	bug-78653.cs		\
	bug-78656.cs		\
	interlocked.cs		\
	cross-domain.cs		\
	appdomain-exit.cs	\
	delegate-async-exit.cs	\
	delegate-delegate-exit.cs	\
	delegate-exit.cs	\
	finalizer-abort.cs	\
	finalizer-exception.cs	\
	finalizer-exit.cs	\
	main-exit.cs	\
	main-returns-abort-resetabort.cs	\
	main-returns-background-abort-resetabort.cs	\
	main-returns-background-resetabort.cs	\
	main-returns-background.cs	\
	main-returns-background-change.cs	\
	main-returns.cs		\
	subthread-exit.cs	\
	desweak.cs		\
	cominterop.cs		\
	exists.cs		\
	handleref.cs	\
	transparentproxy.cs

if X86
# bug #71274
PLATFORM_DISABLED_TESTS=finalizer-abort.exe finalizer-exception.exe finalizer-exit.exe
endif

if POWERPC
# bug #71274
PLATFORM_DISABLED_TESTS=finalizer-abort.exe finalizer-exception.exe finalizer-exit.exe
endif

DISABLED_TESTS=			\
	delegate-async-exception.exe	\
	delegate-delegate-exception.exe	\
	appdomain-unload.exe		\
	$(PLATFORM_DISABLED_TESTS)

# These only compile with MS CSC
TEST_CSC_SRC=			\
	vararg.cs

TEST_IL_SRC=			\
	cpblkTest.il		\
	vbinterface.il		\
	jmpTest.il		\
	calliTest.il		\
	ckfiniteTest.il		\
	fault-handler.il		\
	locallocTest.il		\
	long-shift-regalloc.il		\
	initblkTest.il		\
	qt-instance.il		\
	vararg.il		\
	bug-29859.il		\
	bug-78549.il		\
	static-fields-nonconst.il	\
	reload-at-bb-end.il	\
	test-enum-indstoreil.il	\
	filter-bug.il		\
	even-odd.il		\
	bug-82022.il

TEST_CS2_SRC = \
	interlocked-2.2.cs	\
	pinvoke-2.2.cs 		\
	bug-78431.2.cs 		\
	bug-79684.2.cs 		\
	catch-generics.2.cs	\
	event-get.2.cs		\
	safehandle.2.cs		\
	stackframes-async.2.cs		\
	module-cctor-loader.2.cs	\
	generics-invoke-byref.2.cs	\
	bug-80392.2.cs

TEST_IL2_SRC = find-method.2.il	\
	bug-79215.2.il	\
	bug-79956.2.il

# pre-requisite test sources: files that are not test themselves
# but that need to be compiled
PREREQ_IL_SRC=event-il.il module-cctor.il
PREREQ_CS_SRC=

PREREQSI_IL=$(PREREQ_IL_SRC:.il=.exe)
PREREQSI_CS=$(PREREQ_CS_SRC:.cs=.exe)
TESTSI_CS=$(TEST_CS_SRC:.cs=.exe)
TESTSI_IL=$(TEST_IL_SRC:.il=.exe)
TESTSI_CS2=$(TEST_CS2_SRC:.2.cs=.exe)
TESTSI_IL2=$(TEST_IL2_SRC:.2.il=.exe)
TESTBS=$(BENCHSRC:.cs=.exe)
STRESS_TESTS=$(STRESS_TESTS_SRC:.cs=.exe)

EXTRA_DIST=test-driver $(TEST_CS_SRC) $(TEST_CS2_SRC) $(TEST_IL_SRC) $(TEST_IL2_SRC) \
	$(BENCHSRC) $(STRESS_TESTS_SRC) stress-runner.pl $(PREREQ_IL_SRC) $(PREREQ_CS_SRC)

%.exe: %.2.il
	$(ILASM2) -out:$@ $<

%.exe: %.il
	$(ILASM) -out:$@ $<

%.exe: %.2.cs TestDriver.dll
	$(GMCS) -r:TestDriver.dll -out:$@ $<

%.exe: %.cs TestDriver.dll
	$(MCS) -r:TestDriver.dll -out:$@ $<

# mkbundle works on ppc, but the pkg-config POC doesn't when run with make test
if POWERPC
test:	testjit test-type-load test-inline-call-stack test-bug-80307 test-bug-81673 test-bug-81691
else
# Can't use mkbundle on win32 since there is no static build there
if PLATFORM_WIN32
test:   testjit test-type-load test-inline-call-stack test-bug-80307 test-bug-81673 test-bug-81691
else
test:	testjit testbundle test-type-load test-inline-call-stack test-iomap-regression test-bug-80307 test-bug-81673 test-bug-81691
endif
endif

TestDriver.dll:
	$(MCS) -target:library -out:$@ $(srcdir)/../mini/TestDriver.cs

test_cs: $(TEST_PROG) $(TESTSI_CS) libtest.la
	@failed=0; \
	passed=0; \
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_CS); do	\
		if $(srcdir)/test-driver '$(with_mono_path) $(TEST_PROG_RUN)' $$i '$(DISABLED_TESTS)' $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo; echo ".cs: $${passed} test(s) passed. $${failed} test(s) did not pass."; echo

test_il: $(TEST_PROG) $(TESTSI_IL) libtest.la
	@failed=0; \
	passed=0; \
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_IL); do	\
		if $(srcdir)/test-driver '$(with_mono_path) $(TEST_PROG_RUN)' $$i "$(DISABLED_TESTS)" $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo; echo ".il: $${passed} test(s) passed. $${failed} test(s) did not pass."; echo

testb: $(TEST_PROG) $(TESTBS)
	for i in $(TESTBS); do	\
		$(srcdir)/test-driver '$(with_mono_path) $(TEST_PROG_RUN)' $$i '$(DISABLED_TESTS)' $(RUNTIME_ARGS);	\
	done

testjit: testjit-default testjit-net_2_0

testjit-default: $(TESTSI_CS) $(TESTSI_IL) $(TESTBS) libtest.la $(PREREQSI_IL) $(PREREQSI_CS)
	@failed=0; \
	passed=0; \
	failed_tests="";\
	for i in $(TESTSI_CS) $(TESTBS) $(TESTSI_IL); do	\
		rm -f $${i}.so; \
		if $(srcdir)/test-driver '$(with_mono_path) $(JITTEST_PROG_RUN)' $$i '$(DISABLED_TESTS)' $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) did not pass."; \
	if [ $${failed} != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

testjit-net_2_0: $(TESTSI_CS2) $(TESTSI_IL2) libtest.la
	@failed=0; \
	passed=0; \
	failed_tests="";\
	for i in $(TESTSI_CS2) $(TESTSI_IL2); do	\
		rm -f $${i}.so; \
		if $(srcdir)/test-driver '$(with_mono_path2) $(JITTEST_PROG_RUN)' $$i '$(DISABLED_TESTS)' $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) did not pass."; \
	if [ $${failed} != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

testaot: $(TESTSI_CS) $(TESTBS) libtest.la
	@failed=0; \
	passed=0; \
	failed_tests="";\
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_CS) $(TESTBS); do	\
		rm -f $${i}.so; \
		$(JITTEST_PROG_RUN) --aot $${i} > /dev/null; \
		if $(srcdir)/test-driver '$(with_mono_path) $(JITTEST_PROG_RUN)' $$i "$(DISABLED_TESTS)" $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) did not pass."; \
	if [ $$failed != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

testtrace:
	$(MAKE) RUNTIME_ARGS="$${RUNTIME_ARGS} --trace" testjit

testinterp: $(TESTSI_CS) $(TESTSI_IL) libtest.la
	@failed=0; \
	passed=0; \
	failed_tests="";\
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_CS) $(TESTSI_IL); do \
		if $(srcdir)/test-driver '$(with_mono_path) $(TEST_PROG_RUN)' $$i "$(DISABLED_TESTS)" $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) did not pass."; \
	if [ $$failed != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

testjitspeed: $(JITTEST_PROG) $(TESTBS)
	for i in $(TESTBS); do	\
		echo $$i;	\
		time $(JITTEST_PROG) $$i;	\
	done

test-iomap-regression: exists.cs
	$(MCS) $(srcdir)/exists.cs -out:exists.exe
	MONO_IOMAP=all $(RUNTIME) exists.exe

stresstest: $(STRESS_TESTS)
	@failed=0; \
	passed=0; \
	failed_tests="";\
	for i in $(STRESS_TESTS); do	\
		if $(srcdir)/stress-runner.pl $$i ../mini/mono $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) did not pass."; \
	if [ $${failed} != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

testbundle: console.exe
	$(MKBUNDLE) --static console.exe
	$(with_mono_path) MONO_CFG_DIR=$(mono_cfg_dir) MONO_SHARED_DIR=$(mono_build_root)/runtime ./a.out
	- rm -rf a.out

EXTRA_DIST += load-missing.il t-missing.cs load-exceptions.cs
test-type-load: TestDriver.dll
	$(ILASM) /dll /output:load-missing.dll $(srcdir)/load-missing.il
	$(MCS) -t:library -out:t.dll -d:FOUND $(srcdir)/t-missing.cs
	$(MCS) -r:TestDriver.dll -r:load-missing.dll -r:t.dll -out:load-exceptions.exe $(srcdir)/load-exceptions.cs
	$(MCS) -t:library -out:t.dll $(srcdir)/t-missing.cs
	$(RUNTIME) load-exceptions.exe

EXTRA_DIST += test-inline-call-stack-library.cs test-inline-call-stack.cs
test-inline-call-stack-library.dll: TestDriver.dll $(srcdir)/test-inline-call-stack-library.cs
	$(MCS) -t:library -out:test-inline-call-stack-library.dll $(srcdir)/test-inline-call-stack-library.cs
test-inline-call-stack.exe: TestDriver.dll test-inline-call-stack-library.dll $(srcdir)/test-inline-call-stack.cs
	$(MCS) -r:TestDriver.dll -r:test-inline-call-stack-library.dll -out:test-inline-call-stack.exe $(srcdir)/test-inline-call-stack.cs
test-inline-call-stack: TestDriver.dll test-inline-call-stack-library.dll test-inline-call-stack.exe
	$(RUNTIME) test-inline-call-stack.exe

EXTRA_DIST += bug-80307.cs
bug-80307.exe: $(srcdir)/bug-80307.cs
	$(MCS) -r:System.Web -out:$@ $(srcdir)/bug-80307.cs

# We use 'test-support-files' to handle an ordering issue between the 'mono/' and 'runtime/' directories
test-bug-80307: bug-80307.exe
	cd $(top_builddir)/runtime && $(MAKE) test-support-files
	$(RUNTIME) bug-80307.exe

EXTRA_DIST += bug-81673.cs bug-81673-interface.cs
bug-81673.exe bug-81673-interface.dll: $(srcdir)/bug-81673.cs $(srcdir)/bug-81673-interface.cs
	$(MCS) -target:library -out:bug-81673-interface.dll $(srcdir)/bug-81673-interface.cs
	$(MCS) -out:bug-81673.exe -r:bug-81673-interface.dll $(srcdir)/bug-81673.cs
	$(MCS) -define:WITH_STOP -target:library -out:bug-81673-interface.dll $(srcdir)/bug-81673-interface.cs

test-bug-81673 : bug-81673.exe bug-81673-interface.dll
	$(RUNTIME) bug-81673.exe

EXTRA_DIST += bug-81691.cs bug-81691-a.cs bug-81691-b.cs
bug-81691.exe bug-81691-b.dll: $(srcdir)/bug-81691.cs $(srcdir)/bug-81691-a.cs $(srcdir)/bug-81691-b.cs
	$(GMCS) -target:library -out:bug-81691-a.dll $(srcdir)/bug-81691-a.cs
	$(GMCS) -target:library -out:bug-81691-b.dll -r:bug-81691-a.dll $(srcdir)/bug-81691-b.cs
	$(GMCS) -r:bug-81691-b.dll -out:bug-81691.exe $(srcdir)/bug-81691.cs
	rm -f bug-81691-a.dll

test-bug-81691 : bug-81691.exe bug-81691-b.dll
	$(RUNTIME) bug-81691.exe

# Useful if mono is compiled with --enable-shared=no
patch-libtool:
	cp "../../libtool" .
	sed -e 's,build_libtool_libs=no,build_libtool_libs=yes,g' libtool > 2; mv 2 libtool
	sed -e 's,LIBTOOL =,LIBTOOL2 =,g' Makefile > 2 && echo "LIBTOOL = bash ./libtool" > 1 && cat 1 2 > Makefile
	touch libtest.c

noinst_LTLIBRARIES = libtest.la

INCLUDES = $(GLIB_CFLAGS)

if PLATFORM_WIN32
# gcc-3.4.4 emits incorrect code when making indirect calls to stdcall functions using a tail call
# This shows up when compiling mono_test_marshal_delegate ()
libtest_la_CFLAGS=-fno-optimize-sibling-calls
# the exported names created by gcc for stdcall functions are missing the leading _, so MS.NET
# can't find them. So we use --kill-at to remove the @ suffix as well.
libtest_la_LDFLAGS=-no-undefined -rpath `pwd` -Wl,--kill-at
else
libtest_la_LDFLAGS = -rpath `pwd`
endif
libtest_la_SOURCES = libtest.c
libtest_la_LIBADD = $(GLIB_LIBS)

CLEANFILES = $(TESTSI_CS) $(TESTSI_IL) $(STRESS_TESTS) *.dll *.stdout *.exe stest.dat
