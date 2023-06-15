TOP=../../../../
DOTNET:=$(TOP)dotnet.sh
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

MONO_CONFIG?=Debug
MONO_ARCH?=$(shell . $(TOP)eng/native/init-os-and-arch.sh && echo $${arch})
TARGET_OS?=$(shell . $(TOP)eng/native/init-os-and-arch.sh && echo $${os})
AOT?=false
StripILCode?=false
CompiledMethodsOutputDirectory?= #<path-to-a-writable-directory>

#NET_TRACE_PATH=<path-to-trace-of-sample>
#PGO_BINARY_PATH=<path-to-dotnet-pgo-executable>
#MIBC_PROFILE_PATH=<path-to-mibc-for-sample>

MONO_ENV_OPTIONS ?=

publish:
	$(DOTNET) publish \
	-c $(MONO_CONFIG) \
	-r $(TARGET_OS)-$(MONO_ARCH) \
	/p:RunAOTCompilation=$(AOT) \
	/p:StripILCode=$(StripILCode) \
	/p:CompiledMethodsOutputDirectory=$(CompiledMethodsOutputDirectory) \
	'/p:NetTracePath="$(NET_TRACE_PATH)"' \
	'/p:PgoBinaryPath="$(PGO_BINARY_PATH)"' \
	'/p:MibcProfilePath="$(MIBC_PROFILE_PATH)"'

run: publish
	DOTNET_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS="$(MONO_ENV_OPTIONS)" \
	$(TOP)artifacts/bin/HelloWorld/$(MONO_ARCH)/$(MONO_CONFIG)/$(TARGET_OS)-$(MONO_ARCH)/publish/HelloWorld

clean:
	rm -rf $(TOP)artifacts/bin/HelloWorld/
