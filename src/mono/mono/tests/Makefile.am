check-local: test

TEST_PROG = ../interpreter/mint
JITTEST_PROG = ../mini/mono

TEST_PROG_RUN = MONO_PATH=$(mcs_topdir)/class/lib/default $(LIBTOOL) --mode=execute $(TEST_PROG)
JITTEST_PROG_RUN = MONO_PATH=$(mcs_topdir)/class/lib/default $(LIBTOOL) --mode=execute $(JITTEST_PROG)

RUNTIME_ARGS=--config tests-config --optimize=all

RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/default $(top_builddir)/runtime/mono-wrapper

CSC = $(RUNTIME) $(mcs_topdir)/class/lib/default/mcs.exe --unsafe -nowarn:0162
ILASM = $(RUNTIME) $(mcs_topdir)/ilasm/ilasm.exe

BENCHSRC=fib.cs random.cs nested-loops.cs ackermann.cs tight-loop.cs sieve.cs

TEST_CS_SRC=			\
	array-init.cs		\
	arraylist.cs		\
	checked.cs		\
	char-isnumber.cs	\
	create-instance.cs	\
	field-layout.cs		\
	pack-layout.cs		\
	hash-table.cs		\
	test-ops.cs		\
	obj.cs			\
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
	struct.cs		\
	valuetype-gettype.cs	\
	typeof-ptr.cs		\
	static-constructor.cs	\
	pinvoke.cs		\
	pinvoke1.cs		\
	pinvoke2.cs		\
	pinvoke3.cs		\
	pinvoke10.cs		\
	pinvoke11.cs		\
	pinvoke13.cs		\
	pinvoke15.cs		\
	pinvoke17.cs		\
	pinvoke18.cs		\
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
	appdomain3.cs		\
	appdomain-client.cs	\
	appdomain-unload.cs \
	loader.cs       \
	pointer.cs		\
	rounding.cs		\
	hashcode.cs		\
	delegate1.cs		\
	delegate2.cs		\
	delegate3.cs		\
	delegate4.cs		\
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
	cross-domain.cs

if X86
PLATFORM_DISABLED_TESTS=
endif

DISABLED_TESTS=			\
	$(PLATFORM_DISABLED_TESTS)

# These only compile with MS CSC
TEST_CSC_SRC=			\
	vararg.cs

TEST_IL_SRC=			\
	cpblkTest.il		\
	jmpTest.il			\
	calliTest.il		\
	ckfiniteTest.il		\
	locallocTest.il		\
	initblkTest.il		\
	qt-instance.il		\
	vararg.il			\
	bug-29859.il		\
	even-odd.il


TESTSI_CS=$(TEST_CS_SRC:.cs=.exe)
TESTSI_IL=$(TEST_IL_SRC:.il=.exe)
TESTBS=$(BENCHSRC:.cs=.exe)

EXTRA_DIST=test-driver $(TEST_CS_SRC) $(TEST_IL_SRC) $(BENCHSRC)

%.exe: %.il
	$(ILASM) -out:$@ $<

%.exe: %.cs TestDriver.dll
	$(CSC) -r:TestDriver.dll -out:$@ $<

test:	testjit

TestDriver.dll:
	$(CSC) -target:library -o $@ $(srcdir)/../mini/TestDriver.cs

test_cs: $(TEST_PROG) $(TESTSI_CS) libtest.la
	@failed=0; \
	passed=0; \
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_CS); do	\
		if $(srcdir)/test-driver '$(TEST_PROG_RUN)' $$i '$(DISABLED_TESTS)' $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo; echo ".cs: $${passed} test(s) passed. $${failed} test(s) failed."; echo

test_il: $(TEST_PROG) $(TESTSI_IL) libtest.la
	@failed=0; \
	passed=0; \
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_IL); do	\
		if $(srcdir)/test-driver '$(TEST_PROG_RUN)' $$i "$(DISABLED_TESTS)" $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo; echo ".il: $${passed} test(s) passed. $${failed} test(s) failed."; echo

testb: $(TEST_PROG) $(TESTBS)
	for i in $(TESTBS); do	\
		$(srcdir)/test-driver '$(TEST_PROG_RUN)' $$i '$(DISABLED_TESTS)' $(RUNTIME_ARGS);	\
	done

testjit: $(TESTSI_CS) $(TESTSI_IL) $(TESTBS) libtest.la
	@failed=0; \
	passed=0; \
	failed_tests="";\
	for i in $(TESTSI_CS) $(TESTBS) $(TESTSI_IL); do	\
		rm -f $${i}.so; \
		if $(srcdir)/test-driver '$(JITTEST_PROG_RUN)' $$i '$(DISABLED_TESTS)' $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) failed."; \
	if [ $${failed} != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

testaot: $(TESTSI_CS) $(TESTBS) libtest.la
	@failed=0; \
	passed=0; \
	failed_tests="";\
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_CS) $(TESTBS); do	\
		rm -f $${i}.so; \
		$(JITTEST_PROG) --aot $${i} > /dev/null; \
		if $(srcdir)/test-driver '$(JITTEST_PROG_RUN)' $$i "$(DISABLED_TESTS)" $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) failed."; \
	if [ $$failed != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

testinterp: $(TESTSI_CS) $(TESTSI_IL) libtest.la
	@failed=0; \
	passed=0; \
	failed_tests="";\
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_CS) $(TESTSI_IL); do \
		if $(srcdir)/test-driver '$(TEST_PROG_RUN)' $$i "$(DISABLED_TESTS)" $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) failed."; \
	if [ $$failed != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

testjitspeed: $(JITTEST_PROG) $(TESTBS)
	for i in $(TESTBS); do	\
		echo $$i;	\
		time $(JITTEST_PROG) $$i;	\
	done

noinst_LTLIBRARIES = libtest.la

INCLUDES = $(GLIB_CFLAGS)

libtest_la_LDFLAGS = -rpath `pwd`
libtest_la_SOURCES = libtest.c
libtest_la_LIBADD = $(GLIB_LIBS)

CLEANFILES = $(TESTSI_CS) $(TESTSI_IL) *.dll *.stdout *.exe stest.dat
