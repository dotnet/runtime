
TEST_PROG=../interpreter/mint
CSC=csc

BENCHSRC=fib.cs random.cs nested-loops.cs 

TESTSRC=			\
	test-ops.cs		\
	obj.cs			\
	switch.cs		\
	outparm.cs		\
	exception.cs		\
	exception2.cs		\
	exception3.cs		\
	exception4.cs		\
	struct.cs		\
	static-constructor.cs		\
	pinvoke.cs		\
	array.cs		\
	enum.cs		\
	property.cs		\
	interface.cs		\
	virtual-method.cs		\
	intptrcast.cs	\
	stream.cs

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

