TOP=../../../../../
DOTNET:=$(TOP)./dotnet.sh
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

# How to build the project.  For hot reload this must be Debug
CONFIG ?=Debug
# How was dotnet/runtime built? should be the same as build.sh -c option
BUILT_RUNTIME_CONFIG ?= Release
MONO_ARCH=x64

OS := $(shell uname -s)
ifeq ($(OS),Darwin)
	TARGET_OS=osx
else
	TARGET_OS=linux
endif

MONO_ENV_OPTIONS = --interp

publish:
	$(DOTNET) publish -c $(CONFIG) -r $(TARGET_OS)-$(MONO_ARCH) -p:BuiltRuntimeConfiguration=$(BUILT_RUNTIME_CONFIG)

run: publish
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS="$(MONO_ENV_OPTIONS)" \
	DOTNET_MODIFIABLE_ASSEMBLIES=debug \
	$(TOP)artifacts/bin/ConsoleDelta/$(MONO_ARCH)/$(CONFIG)/$(TARGET_OS)-$(MONO_ARCH)/publish/ConsoleDelta

clean:
	rm -rf $(TOP)artifacts/bin/HelloWorld/
