
TEST_PROG=../interpreter/mint
JITTEST_PROG=../jit/testjit

CSC=csc

BENCHSRC=fib.cs random.cs nested-loops.cs 

TESTSRC=			\
	array-init.cs		\
	arraylist.cs		\
	field-layout.cs		\
	hash-table.cs		\
	test-ops.cs		\
	obj.cs			\
	string.cs		\
	stringbuilder.cs	\
	switch.cs		\
	outparm.cs		\
	delegate.cs		\
	exception.cs		\
	exception2.cs		\
	exception3.cs		\
	exception4.cs		\
	struct.cs		\
	static-constructor.cs	\
	pinvoke.cs		\
	box.cs			\
	array.cs		\
	enum.cs			\
	property.cs		\
	interface.cs		\
	iface.cs		\
	iface2.cs		\
	iface3.cs		\
	iface4.cs		\
	virtual-method.cs	\
	intptrcast.cs		\
	indexer.cs		\
	stream.cs		\
	stream-writer.cs	\
	console.cs		\
	jit-int.cs		\
	jit-uint.cs		\
	jit-long.cs		\
	jit-ulong.cs		\
	jit-float.cs		\
	pop.cs


TESTS=$(TESTSRC:.cs=.exe)
TESTBS=$(BENCHSRC:.cs=.exe)

EXTRA_DIST=test-driver $(TESTSRC)

%.exe: %.cs
	$(CSC) $<

test: $(TEST_PROG) $(TESTS)
	for i in $(TESTS); do	\
		./test-driver $(TEST_PROG) $$i;	\
	done

testb: $(TEST_PROG) $(TESTBS)
	for i in $(TESTBS); do	\
		./test-driver $(TEST_PROG) $$i;	\
	done

testjit: $(JITTEST_PROG) $(TESTS)
	for i in $(TESTS); do	\
		./test-driver $(JITTEST_PROG) $$i;	\
	done
