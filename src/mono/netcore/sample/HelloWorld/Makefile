TOP=../../../../../
DOTNET:=$(TOP)./dotnet.sh
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

MONO_CONFIG=Release
MONO_ARCH=x64

OS := $(shell uname -s)
ifeq ($(OS),Darwin)
	TARGET_OS=osx
else
	TARGET_OS=linux
endif

publish:
	$(DOTNET) publish -c $(MONO_CONFIG) -r $(TARGET_OS)-$(MONO_ARCH)

run: publish
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS="--llvm" \
	$(TOP)artifacts/bin/HelloWorld/$(MONO_ARCH)/$(MONO_CONFIG)/$(TARGET_OS)-$(MONO_ARCH)/publish/HelloWorld

clean:
	rm -rf $(TOP)artifacts/bin/HelloWorld/
