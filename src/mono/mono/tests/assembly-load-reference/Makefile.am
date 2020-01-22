MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

### buildtree stuff

RUNTIME_ARGS=--config tests-config --optimize=all --debug
TEST_RUNTIME_ARGS ?= $(RUNTIME_ARGS)
TEST_AOT_BUILD_FLAGS ?= $(AOT_BUILD_FLAGS)
TEST_AOT_RUN_FLAGS ?= $(AOT_RUN_FLAGS)

CLASS=$(mcs_topdir)/class/lib/$(DEFAULT_PROFILE)

with_mono_path = MONO_PATH=$(CLASS)

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper
TOOLS_RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/build $(top_builddir)/runtime/mono-wrapper --aot-path=$(mcs_topdir)/class/lib/build

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

TEST_RUNNER = ../test-runner.exe

if HOST_WIN32
if ENABLE_MSVC
	DEFAULT_WIN32_RUNTIME=$(shell cygpath -w -a $(top_builddir)/@mono_runtime@ | sed 's/\\/\\\\/g')
else
	DEFAULT_WIN32_RUNTIME=$(top_builddir)/runtime/mono-wrapper)
endif
TEST_RUNNER += --config tests-config --runtime "$(if $(MONO_EXECUTABLE),$(shell cygpath -w -a $(MONO_EXECUTABLE) | sed 's/\\/\\\\/g'),$(DEFAULT_WIN32_RUNTIME))"
else
TEST_RUNNER += --config tests-config --runtime "$(if $(MONO_EXECUTABLE),$(MONO_EXECUTABLE),$(top_builddir)/runtime/mono-wrapper)"
endif

TEST_RUNNER += $(if $(V), --verbose,)

if FULL_AOT_TESTS
TEST_RUNNER += --runtime-args "$(TEST_AOT_RUN_FLAGS)"
endif

if HYBRID_AOT_TESTS
TEST_RUNNER += --runtime-args "$(TEST_AOT_RUN_FLAGS)"
endif

TEST_RUNNER += --runtime-args "$(TEST_RUNTIME_FLAGS)"

TEST_RUNNER += --mono-path "$(CLASS)"


### tools

GACUTIL= $(mcs_topdir)/class/lib/build/gacutil.exe
SN= $(mcs_topdir)/class/lib/build/sn.exe

MCS= $(TOOLS_RUNTIME) $(CSC) -noconfig -nologo -debug:portable -target:library $(PROFILE_MCS_FLAGS)

### testcase stuff

EXTRA_DIST= README $(SIGNING_KEY) $(GACTESTLIB_SRCS) 

### Test cases

.PHONY: run-test test-local

TESTS_CS = \
	samedir/LoadFromMain.cs \
	samedir/LoadFileMain.cs \
	separatedir/LoadFromMain.cs \
	separatedir/LoadFileMain.cs \
	mainanddep/LoadFromMain.cs \
	mainanddep/LoadFileMain.cs

LIBRARIES_CS = src/Dep.cs \
	src/Mid.cs

EXTRA_DIST += $(TESTS_CS) $(LIBRARIES_CS)

TESTS_EXE = $(TESTS_CS:.cs=.exe)

TESTS_EXE_AOT = $(TESTS_EXE:.exe=.exe$(PLATFORM_AOT_SUFFIX))

REFERENCED_ASMS = \
	samedir/Mid.dll \
	samedir/Dep.dll \
	separatedir/middle/Mid.dll \
	separatedir/middle/Dep.dll \
	mainanddep/middle/Mid.dll \
	mainanddep/Dep.dll

REFERENCED_ASMS_AOT = $(REFERENCED_ASMS:.dll=.dll$(PLATFORM_AOT_SUFFIX))

if FULL_AOT_TESTS
TESTS_EXE_DEPEND=$(TESTS_EXE_AOT)
REFERENCED_ASMS_DEPEND=$(REFERENCED_ASMS_AOT)
else
if HYBRID_AOT_TESTS
TESTS_EXE_DEPEND=$(TESTS_EXE_AOT)
REFERENCED_ASMS_DEPEND=$(REFERENCED_ASMS_AOT)
else
TESTS_EXE_DEPEND=$(TESTS_EXE)
REFERENCED_ASMS_DEPEND=$(REFERENCED_ASMS)
endif
endif

DISABLED_TESTS=

# these don't work with full AOT
if FULL_AOT_TESTS
DISABLED_TESTS+= \
	mainanddep/LoadFromMain.exe \
	mainanddep/LoadFileMain.exe \
	separatedir/LoadFileMain.exe
endif

run-test:
	true
	$(MAKE) run-assembly-load-reference-tests


test-local: compile-samedir compile-separatedir compile-mainanddep

compile-samedir: samedir/LoadFromMain.exe samedir/LoadFileMain.exe samedir/Dep.dll samedir/Mid.dll

samedir/%.dll: src/%.dll
	mkdir -p $(@D)
	cp $< $@

compile-separatedir: separatedir/LoadFromMain.exe separatedir/LoadFileMain.exe separatedir/middle/Dep.dll separatedir/middle/Mid.dll

separatedir/middle/%.dll: src/%.dll
	mkdir -p $(@D)
	cp $< $@

compile-mainanddep: mainanddep/LoadFromMain.exe mainanddep/LoadFileMain.exe mainanddep/Dep.dll mainanddep/middle/Mid.dll

mainanddep/Dep.dll: src/Dep.dll
	mkdir -p $(@D)
	cp $< $@

mainanddep/middle/Mid.dll: src/Mid.dll
	mkdir -p $(@D)
	cp $< $@

src/Dep.dll: src/Dep.cs
	mkdir -p $(@D)
	$(MCS) -out:$@ $< -target:library

src/Mid.dll: src/Mid.cs src/Dep.dll
	$(MCS) -out:$@ $< -r:src/Dep.dll -target:library

%/LoadFromMain.exe: %/LoadFromMain.cs
	mkdir -p $(@D)
	$(MCS) -out:$@ $< -target:exe

%/LoadFileMain.exe: %/LoadFileMain.cs
	mkdir -p $(@D)
	$(MCS) -out:$@ $< -target:exe

%/LoadFromMain.exe$(PLATFORM_AOT_SUFFIX): %/LoadFromMain.exe
	$(RUNTIME) $(TEST_AOT_BUILD_FLAGS) $<

%/LoadFileMain.exe$(PLATFORM_AOT_SUFFIX): %/LoadFileMain.exe
	$(RUNTIME) $(TEST_AOT_BUILD_FLAGS) $<

%.dll$(PLATFORM_AOT_SUFFIX): %.dll
	$(RUNTIME) $(TEST_AOT_BUILD_FLAGS) $<

run-assembly-load-reference-tests: $(TESTS_EXE_DEPEND) $(REFERENCED_ASMS_DEPEND) 
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --disabled "$(DISABLED_TESTS)" $(TESTS_EXE)
