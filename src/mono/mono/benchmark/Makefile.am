TEST_PROG=../mini/mono
RUNTIME_ARGS="-O=all"

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
	inline5.cs		\
	inline6.cs		\
	muldiv.cs		\
	loops.cs		\
	initlocals.cs		\
	logic.cs		\
	switch.cs		\
	ctor-bench.cs		\
	readonly.cs		\
	bulkcpy.il		\
	math.cs			\
	boxtest.cs		\
	valuetype1.cs		\
	valuetype2.cs		\
	valuetype3.cs

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
		if ./test-driver $(TEST_PROG) $$i $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) failed."

check:
	@echo no check yet
