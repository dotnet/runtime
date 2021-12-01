CONFIG=Debug
MONO_ARCH=x64
DOTNET := ../../../../../dotnet.sh

run-sim:
	$(DOTNET) publish -c $(CONFIG) /p:TargetOS=iOSSimulator /p:TargetArchitecture=$(MONO_ARCH) \
	/p:UseLLVM=False /p:ForceAOT=False /p:MonoForceInterpreter=true

run-catalyst:
	$(DOTNET) publish -c $(CONFIG) /p:TargetOS=MacCatalyst /p:TargetArchitecture=$(MONO_ARCH) \
	/p:UseLLVM=False /p:ForceAOT=False /p:MonoForceInterpreter=true

clean:
	rm -rf bin
