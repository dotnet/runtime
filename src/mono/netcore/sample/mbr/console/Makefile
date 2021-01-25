TOP=../../../../../../
DOTNET:=$(TOP)./dotnet.sh
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

CONFIG ?=Release
MONO_ARCH=x64

OS := $(shell uname -s)
ifeq ($(OS),Darwin)
	TARGET_OS=osx
else
	TARGET_OS=linux
endif

MONO_ENV_OPTIONS ?=--interp=-inline

publish:
	$(DOTNET) publish -c $(CONFIG) -r $(TARGET_OS)-$(MONO_ARCH)

run: publish
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS="$(MONO_ENV_OPTIONS)" \
	$(TOP)artifacts/bin/ConsoleDelta/$(MONO_ARCH)/$(CONFIG)/$(TARGET_OS)-$(MONO_ARCH)/publish/ConsoleDelta

clean:
	rm -rf $(TOP)artifacts/bin/HelloWorld/
