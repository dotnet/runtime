
TEST_PROG=../interpreter/mono-int
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
	struct.cs		\
	pinvoke.cs		\
	array.cs		\
	stream.cs

TESTS=				\
	test-ops.exe		\
	fib.exe			\
	obj.exe			\
	random.exe		\
	switch.exe		\
	outparm.exe		\
	nested-loops.exe	\
	exception.exe		\
	struct.exe		\
	pinvoke.exe		\
	array.exe		\
	stream.exe

EXTRA_DIST=test-driver $(TESTSRC)

%.exe: %.cs
	$(CSC) $<

test: $(TEST_PROG) $(TESTS)
	for i in $(TESTS); do	\
		./test-driver $(TEST_PROG) $$i;	\
	done

