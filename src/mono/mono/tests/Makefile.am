
TEST_PROG=../interpreter/mint
CSC=csc

TESTSRC=			\
	test-ops.cs		\
	fib.cs			\
	obj.cs			\
	random.cs		\
	switch.cs		\
	outparm.cs		\
	nested-loops.cs		\
	exception.cs		\
	exception2.cs		\
	exception3.cs		\
	exception4.cs		\
	struct.cs		\
	static-constructor.cs		\
	pinvoke.cs		\
	array.cs		\
	stream.cs

TESTS=$(TESTSRC:.cs=.exe)

EXTRA_DIST=test-driver $(TESTSRC)

%.exe: %.cs
	$(CSC) $<

test: $(TEST_PROG) $(TESTS)
	for i in $(TESTS); do	\
		./test-driver $(TEST_PROG) $$i;	\
	done

