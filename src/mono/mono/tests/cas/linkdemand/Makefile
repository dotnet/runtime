RUNTIME = mono --debug --security
CSCOMPILE = mcs --debug
PROFILE = net_1_1

all:	icall1.exe icall2.exe icall3.exe icall4.exe icall5.exe icall6.exe \
	aptc1a.exe aptc1b.exe aptc2a.exe aptc2b.exe aptclib.dll \
	pinvoke1.exe pinvoke2.exe pinvoke3.exe pinvoke4.exe \
	cas1.exe cas2.exe cas3.exe cas4.exe cas5.exe

aot:	icall1.exe.so icall2.exe.so icall3.exe.so icall4.exe.so icall5.exe.so icall6.exe.so \
	aptc1a.exe.so aptc1b.exe.so aptc2a.exe.so aptc2b.exe.so aptclib.dll.so \
	pinvoke1.exe.so pinvoke2.exe.so pinvoke3.exe.so pinvoke4.exe.so \
	cas1.exe.so cas2.exe.so cas3.exe.so cas4.exe.so cas5.exe.so

FULLTRUST_TEST_FILES = icall1 icall2 icall3 icall4 icall5 icall6 \
	aptc1a aptc1b aptc2a aptc2b \
	pinvoke1 pinvoke2 pinvoke4 \
	cas1 cas3 cas4 cas5

UNHANDLED_TEST_FILES = cas2 pinvoke3

INTERNET_TEST_FILES = pinvoke1 pinvoke2 cas1

run: all
	@for i in $(FULLTRUST_TEST_FILES); do	\
		$(RUNTIME) $$i.exe;	\
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
#	for i in $(INTERNET_TEST_FILES); do	\
#		MONO_CAS_ZONE=Internet $(RUNTIME) $$i.exe > /dev/null;	\
#		if [ "$$?" = "0" ]; then	\
#			echo -e "internet-$$i\tpass";	\
#			passed=`expr $${passed} + 1`; \
#		else	\
#			echo -e "internet-$$i\tFAIL ($$?)";	\
#			failed=`expr $${failed} + 1`; \
#			failed_tests="$${failed_tests} internet-$$i"; \
#		fi;	\
#	done;	\
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

aptc.snk:
	sn -k aptc.snk

%a.exe: %.cs
	$(CSCOMPILE) $^ /out:$@

%b.exe: %.cs
	$(CSCOMPILE) $^ /out:$@ /keyfile:ecma.pub /delaysign+

aptc1b.exe: aptc1.cs aptc.snk
	$(CSCOMPILE) aptc1.cs /out:$@ /keyfile:aptc.snk

aptc2a.exe: aptc2.cs aptclib.dll
	$(CSCOMPILE) aptc2.cs /out:$@ /r:aptclib.dll

aptc2b.exe: aptc2.cs aptclib.dll
	$(CSCOMPILE) aptc2.cs /out:$@ /keyfile:aptc.snk /r:aptclib.dll

aptclib.dll: aptclib.cs aptc.snk
	$(CSCOMPILE) aptclib.cs /out:$@ /target:library /keyfile:aptc.snk

linkdemand_%.exe: linkdemand_%.cs
	$(CSCOMPILE) $^ /out:$@ /r:Mono.Permissions.dll

%.exe: %.cs
	$(CSCOMPILE) $^ /out:$@

%.exe.so: %.exe
	$(RUNTIME) --aot $^
