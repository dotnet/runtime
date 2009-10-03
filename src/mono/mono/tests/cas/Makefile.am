with_mono_path = MONO_PATH=$(mcs_topdir)/class/lib/net_2_0

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper --debug --security

CSCOMPILE = gmcs -debug
PROFILE = net_2_0

SUBDIRS = assembly linkdemand inheritance demand threads

# Uncomment this for these tests to be for for 'make check'
#check-local: test

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
