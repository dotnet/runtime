TOP=../../../../
DOTNET:=$(TOP)./dotnet.sh
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

MONO_CONFIG ?=Debug
MONO_ARCH=x64
AOT?=false

#NET_TRACE_PATH=<path-to-trace-of-sample>
#PGO_BINARY_PATH=<path-to-dotnet-pgo-executable>
#MIBC_PROFILE_PATH=<path-to-mibc-for-sample>

OS := $(shell uname -s)
ifeq ($(OS),Darwin)
	TARGET_OS=osx
else
	TARGET_OS=linux
endif

MONO_ENV_OPTIONS ?=

publish:
	$(DOTNET) publish \
	-c $(MONO_CONFIG) \
	-r $(TARGET_OS)-$(MONO_ARCH) \
	/p:RunAOTCompilation=$(AOT) \
	'/p:NetTracePath="$(NET_TRACE_PATH)"' \
	'/p:PgoBinaryPath="$(PGO_BINARY_PATH)"' \
	'/p:MibcProfilePath="$(MIBC_PROFILE_PATH)"'

run: publish
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS="$(MONO_ENV_OPTIONS)" \
	$(TOP)artifacts/bin/HelloWorld/$(MONO_ARCH)/$(MONO_CONFIG)/$(TARGET_OS)-$(MONO_ARCH)/publish/HelloWorld

clean:
	rm -rf $(TOP)artifacts/bin/HelloWorld/
