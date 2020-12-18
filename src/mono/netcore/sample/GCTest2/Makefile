TOP=../../../../../
DOTNET:=$(TOP)./dotnet.sh
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

MONO_CONFIG=Debug
MONO_ARCH=x64

OS := $(shell uname -s)
ifeq ($(OS),Darwin)
	TARGET_OS=osx
else
	TARGET_OS=linux
endif

# MONO_ENV_OPTIONS ?=--llvm

publish:
	$(DOTNET) publish -c $(MONO_CONFIG) -r $(TARGET_OS)-$(MONO_ARCH)

run: publish
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS="$(MONO_ENV_OPTIONS) --interpreter" \
	$(TOP)artifacts/bin/GCTest2/$(MONO_ARCH)/$(MONO_CONFIG)/$(TARGET_OS)-$(MONO_ARCH)/publish/GCTest2

debug: publish
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS="--interpreter" \
	lldb -- $(TOP)artifacts/bin/GCTest2/$(MONO_ARCH)/$(MONO_CONFIG)/$(TARGET_OS)-$(MONO_ARCH)/publish/GCTest2




clean:
	rm -rf $(TOP)artifacts/bin/HelloWorld/
