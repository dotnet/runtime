with_mono_path = MONO_PATH=$(mcs_topdir)/class/lib/net_2_0

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper --debug

CAS_RUNTIME = $(RUNTIME) --security

CSCOMPILE = $(RUNTIME) $(mcs_topdir)/class/lib/basic/mcs.exe -debug
PROFILE = net_2_0

EXTRA_DIST = thread1.cs thread2.cs thread3.cs \
	tpool1.cs tpool2.cs \
	timer1.cs timer2.cs swf-timer3.cs swf-timer4.cs \
	delegate1.cs delegate2.cs \
	swf-control1.cs

tests:	thread1.exe thread2.exe thread3.exe \
	tpool1.exe tpool2.exe delegate1.exe delegate2.exe \
	timer1.exe timer2.exe
#	swf-timer3.exe swf-timer4.exe swf-control1.exe

aot:	thread1.exe.so thread2.exe.so thread3.exe.so \
	tpool1.exe.so tpool2.exe.so \
	timer1.exe.so timer2.exe.so swf-timer3.exe.so swf-timer4.exe.so \
	delegate1.exe.so delegate2.exe.so \
	swf-control1.exe.so

FULLTRUST_TEST_FILES = thread1 thread2 thread3 \
	tpool1 tpool2 \
	timer1 timer2 \
	delegate1 delegate2

UNHANDLED_TEST_FILES = 

run: tests
	@for i in $(FULLTRUST_TEST_FILES); do	\
		$(CAS_RUNTIME) $$i.exe x;	\
	done;
	@for i in $(INTERNET_TEST_FILES); do	\
		MONO_CAS_ZONE=Internet $(CAS_RUNTIME) $$i.exe;	\
	done;

test: tests
	@failed=0; \
	passed=0; \
	for i in $(FULLTRUST_TEST_FILES); do	\
		$(CAS_RUNTIME) $$i.exe > /dev/null;	\
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
		$(CAS_RUNTIME) $$i.exe > /dev/null;	\
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

swf-%.exe: swf-%.cs
	$(CSCOMPILE) $^ /out:$@ /r:System.Windows.Forms.dll /r:System.Drawing.dll

%.exe: %.cs
	$(CSCOMPILE) $^ /out:$@

%.exe.so: %.exe
	$(RUNTIME) --aot $^
