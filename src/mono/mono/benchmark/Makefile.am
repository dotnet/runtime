TEST_PROG=../jit/mono

CSC=mcs


TESTSRC=			\
	fib.cs 			\
	life.cs 		\
	castclass.cs		\
	isinst.cs		\
	inline1.cs		\
	inline2.cs		\
	inline3.cs		\
	inline4.cs		\
	muldiv.cs		\
	loops.cs		\
	initlocals.cs		\
	logic.cs		\
	switch.cs		\
	bulkcpy.il		\
	math.cs

TESTSI_TMP=$(TESTSRC:.cs=.exe)
TESTSI=$(TESTSI_TMP:.il=.exe)

EXTRA_DIST=test-driver $(TESTSRC)

%.exe: %.il
	ilasm $< /OUTPUT=$@

%.exe: %.cs
	$(CSC) $<

test: $(TEST_PROG) $(TESTSI)
	@failed=0; \
	passed=0; \
	for i in $(TESTSI); do	\
		if ./test-driver $(TEST_PROG) $$i; \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) failed."

check:
	@echo no check yet
