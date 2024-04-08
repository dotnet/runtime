TOP=../../../../
DOTNET:=$(TOP)dotnet.sh
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

MONO_CONFIG?=Debug
MONO_ARCH?=$(shell . $(TOP)eng/common/native/init-os-and-arch.sh && echo $${arch})
TARGET_OS?=$(shell . $(TOP)eng/common/native/init-os-and-arch.sh && echo $${os})
AOT?=false
USE_LLVM?=false
StripILCode?=false
TrimmingEligibleMethodsOutputDirectory?= #<path-to-a-writable-directory>

#MIBC_PROFILE_PATH=<path-to-mibc-for-sample>

MONO_ENV_OPTIONS ?=

publish:
	$(DOTNET) publish \
	-c $(MONO_CONFIG) \
	-r $(TARGET_OS)-$(MONO_ARCH) \
	/p:RunAOTCompilation=$(AOT) \
	/p:MonoEnableLLVM=$(USE_LLVM) \
	/p:StripILCode=$(StripILCode) \
	/p:TrimmingEligibleMethodsOutputDirectory=$(TrimmingEligibleMethodsOutputDirectory) \
	'/p:MibcProfilePath="$(MIBC_PROFILE_PATH)"' \
	/bl

run: publish
	DOTNET_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS="$(MONO_ENV_OPTIONS)" \
	$(TOP)artifacts/bin/HelloWorld/$(MONO_ARCH)/$(MONO_CONFIG)/$(TARGET_OS)-$(MONO_ARCH)/publish/HelloWorld

clean:
	rm -rf $(TOP)artifacts/bin/HelloWorld/
