TOP=../../../../../
CONFIG?=Debug
MONO_ARCH?=$(shell . $(TOP)eng/common/native/init-os-and-arch.sh && echo $${arch})
DOTNET:=$(TOP)dotnet.sh

run-sim:
	$(DOTNET) publish -c $(CONFIG) /p:TargetOS=iossimulator /p:TargetArchitecture=$(MONO_ARCH) \
	/p:UseLLVM=False /p:ForceAOT=False /p:MonoForceInterpreter=true

run-catalyst:
	$(DOTNET) publish -c $(CONFIG) /p:TargetOS=maccatalyst /p:TargetArchitecture=$(MONO_ARCH) \
	/p:UseLLVM=False /p:ForceAOT=False /p:MonoForceInterpreter=true

clean:
	rm -rf bin
