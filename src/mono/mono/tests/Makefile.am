
TEST_PROG=../interpreter/mint
JITTEST_PROG=../jit/mono
RUNTIME_ARGS=--config tests-config

CSC=csc /unsafe

BENCHSRC=fib.cs random.cs nested-loops.cs ackermann.cs tight-loop.cs sieve.cs

TEST_CS_SRC=			\
	array-init.cs		\
	arraylist.cs		\
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
	struct.cs		\
	valuetype-gettype.cs	\
	static-constructor.cs	\
	pinvoke.cs		\
	pinvoke1.cs		\
	pinvoke2.cs		\
	pinvoke3.cs		\
	pinvoke4.cs		\
	pinvoke5.cs		\
	pinvoke6.cs		\
	pinvoke7.cs		\
	pinvoke8.cs		\
	pinvoke9.cs		\
	invoke.cs		\
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
	appdomain.cs		\
	appdomain-client.cs	\
	pointer.cs		\
	vararg.cs		\
	rounding.cs		\
	hashcode.cs		\
	delegate1.cs		\
	delegate2.cs		\
	delegate3.cs		\
	delegate4.cs		\
	delegate5.cs		\
	remoting1.cs		\
	remoting2.cs		\
	remoting3.cs		\
	nonvirt.cs		\
	largeexp.cs		\
	largeexp2.cs		\
	marshalbyref1.cs	\
	static-ctor.cs		\
	inctest.cs		\
	bound.cs		\
	array-invoke.cs		\
	decimal.cs		\
	marshal1.cs		\
	marshal2.cs

TEST_IL_SRC=			\
	cpblkTest.il		\
	jmpTest.il		\
	calliTest.il		\
	ckfiniteTest.il		\
	locallocTest.il		\
	initblkTest.il


TESTSI_CS=$(TEST_CS_SRC:.cs=.exe)
TESTSI_IL=$(TEST_IL_SRC:.il=.exe)
TESTBS=$(BENCHSRC:.cs=.exe)

EXTRA_DIST=test-driver $(TEST_CS_SRC) $(TEST_IL_SRC)

%.exe: %.il
	ilasm $< /OUTPUT=$@

%.exe: %.cs
	$(CSC) $<

test:	test_cs test_il

test_cs: $(TEST_PROG) $(TESTSI_CS) libtest.la
	@failed=0; \
	passed=0; \
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_CS); do	\
		if ./test-driver $(TEST_PROG) $$i $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo; echo ".cs: $${passed} test(s) passed. $${failed} test(s) failed."; echo

test_il: $(TEST_PROG) $(TESTSI_IL) libtest.la
	@failed=0; \
	passed=0; \
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_IL); do	\
		if ./test-driver $(TEST_PROG) $$i $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo; echo ".il: $${passed} test(s) passed. $${failed} test(s) failed."; echo

testb: $(TEST_PROG) $(TESTBS)
	for i in $(TESTBS); do	\
		./test-driver $(TEST_PROG) $$ii $(RUNTIME_ARGS);	\
	done

testjit: $(JITTEST_PROG) $(TESTSI_CS) $(TESTSI_IL) $(TESTBS)
	@failed=0; \
	passed=0; \
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_CS) $(TESTSI_IL) $(TESTBS); do	\
		if ./test-driver $(JITTEST_PROG) $$i $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) failed."

testjitspeed: $(JITTEST_PROG) $(TESTBS)
	for i in $(TESTBS); do	\
		echo $$i;	\
		time $(JITTEST_PROG) $$i;	\
	done

check:
	@echo no check yet

noinst_LTLIBRARIES = libtest.la

libtest_la_LDFLAGS = -rpath `pwd`
libtest_la_SOURCES = libtest.c
