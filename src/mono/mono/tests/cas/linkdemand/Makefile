RUNTIME = mono --debug --security
CSCOMPILE = mcs --debug
PROFILE = net_1_1

all:	icall1a.exe icall1b.exe icall2a.exe icall2b.exe \
	aptc1a.exe aptc1b.exe aptc2a.exe aptc2b.exe aptclib.dll \
	pinvoke1.exe pinvoke2.exe cas1.exe 

aot:	icall1a.exe.so icall1b.exe.so icall2a.exe.so icall2b.exe.so \
	aptc1a.exe.so aptc1b.exe.so aptc2a.exe.so aptc2b.exe.so aptclib.dll.so \
	pinvoke1.exe.so pinvoke2.exe.so cas1.exe.so

FULLTRUST_TEST_FILES = icall1a icall1b icall2a icall2b \
	aptc1a aptc1b aptc2a aptc2b \
	pinvoke1 pinvoke2 cas1 

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
