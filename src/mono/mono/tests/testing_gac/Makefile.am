MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

### buildtree stuff

CLASS=$(mcs_topdir)/class/lib/$(DEFAULT_PROFILE)

if FULL_AOT_TESTS
PROFILE_MCS_FLAGS = -d:MOBILE,MOBILE_LEGACY,FULL_AOT_DESKTOP 
endif

if HYBRID_AOT_TESTS
PROFILE_MCS_FLAGS = -d:MOBILE,MOBILE_LEGACY 
endif

### test runner stuff

if HOST_WIN32
PLATFORM_PATH_SEPARATOR=;
else
PLATFORM_PATH_SEPARATOR=:
endif

RUNTIME= $(top_builddir)/runtime/mono-wrapper
TEST_RUNTIME = MONO_PATH=$(CLASS) $(RUNTIME)
TOOLS_RUNTIME= MONO_PATH=$(mcs_topdir)/class/lib/build $(RUNTIME)

# These tests are testing strict assembly strong-name resolution
TEST_RUNNER = ../test-runner.exe --runtime-args "--assembly-loader=strict"

if HOST_WIN32
if ENABLE_MSVC
DEFAULT_WIN32_RUNTIME=$(shell cygpath -w -a $(top_builddir)/@mono_runtime@ | sed 's/\\/\\\\/g')
else
DEFAULT_WIN32_RUNTIME=$(top_builddir)/runtime/mono-wrapper)
endif
TEST_RUNNER_ARGS=--runtime "$(if $(MONO_EXECUTABLE),$(shell cygpath -w -a $(MONO_EXECUTABLE) | sed 's/\\/\\\\/g'),$(DEFAULT_WIN32_RUNTIME))"
else
TEST_RUNNER_ARGS=--runtime $(if $(MONO_EXECUTABLE),$(MONO_EXECUTABLE),mono)
endif

TEST_RUNNER_ARGS += $(if $(V), --verbose,)

if FULL_AOT_TESTS
TEST_RUNNER += --runtime-args "$(AOT_RUN_FLAGS)"
endif

if HYBRID_AOT_TESTS
TEST_RUNNER += --runtime-args "$(AOT_RUN_FLAGS)"
endif

### tools

GACUTIL= $(mcs_topdir)/class/lib/build/gacutil.exe
SN= $(mcs_topdir)/class/lib/build/sn.exe

MCS= $(TOOLS_RUNTIME) $(CSC) -noconfig -nologo -debug:portable -target:library $(PROFILE_MCS_FLAGS)

### testcase stuff

BASE_MONO_PATH=$(CLASS)

V1_SRC= v1/gactestlib.cs
V2_SRC= v2/gactestlib.cs
APP_V1_SRC = v1/app.cs v1/app-refl-load.cs
APP_BOTH_SRC = app-both.cs

APP_SIGNED_V1_EXE = app-v1.exe app-refl-load-v1.exe

APP_BOTH_EXE = app-both.exe

UNSIGNED_V1_DLL= unsigned_v1/gactestlib.dll
UNSIGNED_V2_DLL= unsigned_v2/gactestlib.dll

SIGNED_V1_DLL= signed_v1/gactestlib.dll
SIGNED_V2_DLL= signed_v2/gactestlib.dll

GACTESTLIB_SRCS= $(V1_SRC) $(V2_SRC)
GACTESTLIB_DLLS= $(SIGNED_V1_DLL) $(SIGNED_V2_DLL) $(UNSIGNED_V1_DLL) $(UNSIGNED_V2_DLL)

#if FULL_AOT_TESTS
#GACTESTLIB_DLLS_AOT=$(GACTESTLIB_DLLS:.dll=.dll$(PLATFORM_AOT_SUFFIX))
#APP_SIGNED_V1_AOT=$(APP_SIGNED_V1_EXE:.exe=.exe$(PLATFORM_AOT_SUFFIX))
#endif

#if HYBRID_AOT_TESTS
#GACTESTLIB_DLLS_AOT=$(GACTESTLIB_DLLS:.dll=.dll$(PLATFORM_AOT_SUFFIX))
#APP_SIGNED_V1_AOT=$(APP_SIGNED_V1_EXE:.exe=.exe$(PLATFORM_AOT_SUFFIX))
#endif


SIGNING_KEY= $(srcdir)/testkey.snk

# prefix for the testing gac.  gacutil will add lib/mono/gac directories
GACDIR=.

SIGN= -delaysign -keyfile:$(SIGNING_KEY)


### autotools stuff

EXTRA_DIST= README $(SIGNING_KEY) $(GACTESTLIB_SRCS) $(APP_V1_SRC) $(APP_BOTH_SRC)

### Test cases

.PHONY: run-test test-local prereqs

run-test:
	true
if !FULL_AOT_TESTS
if !HYBRID_AOT_TESTS
	$(MAKE) test-app-both
	$(MAKE) test-signed-v1-app-mp-unsigned-v1
	$(MAKE) test-signed-v1-app-mp-signed-v1
	$(MAKE) test-signed-v1-app-mp-unsigned-v2-signed-v1
	$(MAKE) test-signed-v1-app-mp-signed-v2-signed-v1
	$(MAKE) test-signed-v1-app-gac
endif
endif

test-local: prereqs $(APP_SIGNED_V1_EXE) $(APP_BOTH_EXE) $(APP_SIGNED_V1_AOT)

prereqs: $(GACTESTLIB_DLLS) $(GACTESTLIB_DLLS_AOT)
	$(MAKE) gacinstall

$(UNSIGNED_V1_DLL): $(V1_SRC)
	-mkdir -p $(@D)
	$(MCS) -out:$@ $<

$(UNSIGNED_V2_DLL): $(V2_SRC)
	-mkdir -p $(@D)
	$(MCS) -out:$@ $<

$(SIGNED_V1_DLL): $(V1_SRC) $(SIGNING_KEY)
	-mkdir -p $(@D)
	$(MCS) -out:$@ $(SIGN) $< && $(TOOLS_RUNTIME) $(SN) -R $@ $(SIGNING_KEY)

$(SIGNED_V2_DLL): $(V2_SRC) $(SIGNING_KEY)
	-mkdir -p $(@D)
	$(MCS) -out:$@ $(SIGN) $< && $(TOOLS_RUNTIME) $(SN) -R $@ $(SIGNING_KEY)

app-both.exe: app-both.cs $(SIGNED_V1_DLL) $(SIGNED_V2_DLL)
	$(MCS) -target:exe -out:$@ -r:V1=$(SIGNED_V1_DLL) /r:V2=$(SIGNED_V2_DLL) $<

%-v1.exe: v1/%.cs $(SIGNED_V1_DLL)
	$(MCS) -target:exe -out:$@ -r:$(SIGNED_V1_DLL) $<

%.exe$(PLATFORM_AOT_SUFFIX): %.exe 
	$(TEST_RUNTIME) $(AOT_BUILD_FLAGS) $<

%.dll$(PLATFORM_AOT_SUFFIX): %.dll 
	$(TEST_RUNTIME) $(AOT_BUILD_FLAGS) $<

# MONO_PATH tests

test-signed-v1-app-mp-unsigned-v1: $(APP_SIGNED_V1_EXE) prereqs
	$(TOOLS_RUNTIME) $(TEST_RUNNER) $(TEST_RUNNER_ARGS) --testsuite-name "testing_gac_$@" --mono-path "unsigned_v1$(PLATFORM_PATH_SEPARATOR)$(BASE_MONO_PATH)" --expected-exit-code 1 $(APP_SIGNED_V1_EXE)

test-signed-v1-app-mp-signed-v1: $(APP_SIGNED_V1_EXE) prereqs
	$(TOOLS_RUNTIME) $(TEST_RUNNER) $(TEST_RUNNER_ARGS) --testsuite-name "testing_gac_$@" --mono-path "signed_v1$(PLATFORM_PATH_SEPARATOR)$(BASE_MONO_PATH)"  $(APP_SIGNED_V1_EXE)

test-signed-v1-app-mp-unsigned-v2-signed-v1: $(APP_SIGNED_V1_EXE) prereqs
	$(TOOLS_RUNTIME) $(TEST_RUNNER) $(TEST_RUNNER_ARGS) --testsuite-name "testing_gac_$@" --mono-path "unsigned_v2$(PLATFORM_PATH_SEPARATOR)signed_v1$(PLATFORM_PATH_SEPARATOR)$(BASE_MONO_PATH)" $(APP_SIGNED_V1_EXE)

test-signed-v1-app-mp-signed-v2-signed-v1: $(APP_SIGNED_V1_EXE) prereqs
	$(TOOLS_RUNTIME) $(TEST_RUNNER) $(TEST_RUNNER_ARGS) --testsuite-name "testing_gac_$@" --mono-path "signed_v2$(PLATFORM_PATH_SEPARATOR)signed_v1$(PLATFORM_PATH_SEPARATOR)$(BASE_MONO_PATH)" $(APP_SIGNED_V1_EXE)

# N.B. the path order is important - the test needs to load v1 successfully and then try to load v2 from the v1 directory.
test-app-both: $(APP_BOTH_EXE) prereqs
	$(TOOLS_RUNTIME) $(TEST_RUNNER) $(TEST_RUNNER_ARGS) --testsuite-name "testing_gac_$@" --mono-path "signed_v1$(PLATFORM_PATH_SEPARATOR)signed_v2$(PLATFORM_PATH_SEPARATOR)$(BASE_MONO_PATH)" app-both.exe

# MONO_GAC_PREFIX tests

test-signed-v1-app-gac: $(APP_SIGNED_V1_EXE) gacinstall prereqs
	$(TOOLS_RUNTIME) $(TEST_RUNNER) $(TEST_RUNNER_ARGS) --testsuite-name "testing_gac_$@" --mono-gac-prefix $(GACDIR) $(APP_SIGNED_V1_EXE)

.PHONY: gacinstall

gacinstall: $(SIGNED_V1_DLL) $(GACTESTLIB_DLLS_AOT)
	$(TOOLS_RUNTIME) $(GACUTIL) -i $< -gacdir $(GACDIR) 

mostlyclean-local: clean-gac-subdir clean-gactestlib-dll
	-rm -f TestResult-*.xml

.PHONY: clean-gac-subdir clean-gactestlib-dll

clean-gac-subdir:
	-rm -rf $(GACDIR)/lib/mono/gac/

clean-gactestlib-dll:
	-rm -f $(GACTESTLIB_DLLS)
	-rm -f $(GACTESTLIB_DLLS_AOT)
	-rm -f $(APP_SIGNED_V1_EXE) $(APP_SIGNED_V1_AOT)
