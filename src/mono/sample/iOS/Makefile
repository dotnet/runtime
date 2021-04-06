MONO_CONFIG=Debug
MONO_ARCH=x64
DOTNET := ../../../../dotnet.sh
USE_LLVM=true
AOT=false

all: runtimepack run

TOOLS_DIR=../../../tasks
appbuilder:
	$(DOTNET) build -c Debug $(TOOLS_DIR)/AotCompilerTask/MonoAOTCompiler.csproj
	$(DOTNET) build -c Debug $(TOOLS_DIR)/AppleAppBuilder/AppleAppBuilder.csproj

runtimepack:
	../../../../build.sh Mono+Libs -os iOSSimulator -arch $(MONO_ARCH) -c $(MONO_CONFIG)

run: clean appbuilder
	$(DOTNET) publish -c $(MONO_CONFIG) /p:TargetArchitecture=$(MONO_ARCH) \
	/p:UseLLVM=$(USE_LLVM) /p:ForceAOT=$(AOT)

run-sim: clean appbuilder
	$(DOTNET) publish -c $(MONO_CONFIG) /p:TargetOS=iOSSimulator /p:TargetArchitecture=$(MONO_ARCH) \
	/p:UseLLVM=$(USE_LLVM) /p:ForceAOT=$(AOT)

run-catalyst:
	$(DOTNET) publish -c $(MONO_CONFIG) /p:TargetOS=MacCatalyst /p:TargetArchitecture=$(MONO_ARCH) \
	/p:UseLLVM=False /p:ForceAOT=False

run-sim-interp: clean appbuilder
	$(DOTNET) publish -c $(MONO_CONFIG) /p:TargetOS=iOSSimulator /p:TargetArchitecture=$(MONO_ARCH) \
	/p:UseLLVM=$(USE_LLVM) /p:ForceAOT=$(AOT) /p:MonoForceInterpreter=true

run-catalyst-interp:
	$(DOTNET) publish -c $(MONO_CONFIG) /p:TargetOS=MacCatalyst /p:TargetArchitecture=$(MONO_ARCH) \
	/p:UseLLVM=False /p:ForceAOT=False /p:MonoForceInterpreter=true

clean:
	rm -rf bin
