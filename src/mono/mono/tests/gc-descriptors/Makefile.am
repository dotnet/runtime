MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

CLASS=$(mcs_topdir)/class/lib/$(DEFAULT_PROFILE)

TOOLS_RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/build $(top_builddir)/runtime/mono-wrapper
MCS = $(TOOLS_RUNTIME) $(CSC) -lib:$(CLASS) -noconfig -unsafe -nowarn:0162 -nowarn:0168 -nowarn:0219 -debug:portable

if INSTALL_TESTING_AOT_FULL

check-local:

else

check-local: test

test-local : descriptor-tests.exe

descriptor-tests.exe : descriptor-tests.cs
	$(MCS) -Warn:0 descriptor-tests.cs

descriptor-tests.cs : descriptor-tests-driver.cs descriptor-tests-prefix.cs gen-descriptor-tests.py
	if [ "$(srcdir)" != "$(builddir)" ]; then cp $^ .; fi
	$(srcdir)/gen-descriptor-tests.py >descriptor-tests.cs

endif

EXTRA_DIST = descriptor-tests-driver.cs descriptor-tests-prefix.cs gen-descriptor-tests.py

.DELETE_ON_ERROR:

