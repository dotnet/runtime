RUNTIME = mono --debug --security
CSCOMPILE = mcs -debug
PROFILE = net_1_1

all:	makepol.exe internet.xml intranet.xml \
	sandbox.exe stacktrace.exe whoami.exe 

aot:	makepol.exe.so sandbox.exe.so stacktrace.exe.so whoami.exe.so

FULLTRUST_TEST_FILES = sandbox

UNHANDLED_TEST_FILES = 

run: all
	@for i in $(FULLTRUST_TEST_FILES); do	\
		$(RUNTIME) $$i.exe x;	\
	done;
	@for i in $(INTERNET_TEST_FILES); do	\
		MONO_CAS_ZONE=Internet $(RUNTIME) $$i.exe;	\
	done;

test: all
	@failed=0; \
	passed=0; \
	for i in $(FULLTRUST_TEST_FILES); do	\
		$(RUNTIME) $$i.exe > /dev/null;	\
		if [ "$$?" = "0" ]; then	\
			echo -e "fulltrust-$$i\tpass";	\
			passed=`expr $${passed} + 1`; \
		else	\
			echo -e "fulltrust-$$i\tFAIL ($$?)";	\
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} fulltrust-$$i"; \
		fi;	\
	done;	\
	for i in $(UNHANDLED_TEST_FILES); do	\
		$(RUNTIME) $$i.exe > /dev/null;	\
		if [ "$$?" = "0" ]; then	\
			echo -e "unhandled-$$i\tFAIL ($$?)";	\
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} unhandled-$$i"; \
		else	\
			echo -e "unhandled-$$i\tpass";	\
			passed=`expr $${passed} + 1`; \
		fi;	\
	done;	\
	echo -e "\n$${passed} test(s) passed. $${failed} test(s) failed."; \
	if [ "$${failed}" != "0" ]; then	\
		echo -e "Failed tests are:";	\
		for i in $${failed_tests};	\
			do echo -e "\t$${i}";	\
		done;	\
		exit 1;	\
	fi

clean:
	rm -f *.exe* *.dll* *.snk

%.xml: makepol.exe
	$(RUNTIME) makepol.exe

%.exe: %.cs
	$(CSCOMPILE) $^ /out:$@

makepol.exe: makepol.cs
	$(CSCOMPILE) $^ /out:$@ -r:System.dll -r:System.Drawing.dll

%.exe.so: %.exe
	$(RUNTIME) --aot $^
