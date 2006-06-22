RUNTIME = mono --debug --security
CSCOMPILE = mcs -debug
PROFILE = net_1_1

SUBDIRS = assembly linkdemand inheritance demand threads

all:

clean:

test:
	@failed=0;	\
	passed=0;	\
	for i in $(SUBDIRS); do	\
		cd $$i;	\
		$(MAKE) test;	\
		if [ "$$?" != "0" ]; then	\
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi;	\
		cd ..;	\
	done;	\
	if [ "$${failed}" == "0" ]; then	\
		echo -e "Perfect score :-)";	\
	else	\
		echo -e "Failure(s) in category:";	\
		for i in $${failed_tests};	\
			do echo -e "\t$${i}";	\
		done;	\
		exit 1;	\
	fi
