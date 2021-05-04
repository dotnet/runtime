MONO_CONFIG=Release
MONO_ARCH?=x64
DOTNET := ../../../../dotnet.sh
USE_LLVM=true
AOT=false
DEPLOY_AND_RUN?=true

all: runtimepack run

runtimepack:
	../../../../build.sh Mono+Libs -os Android -arch $(MONO_ARCH) -c $(MONO_CONFIG)

run:
	$(DOTNET) publish \
	/p:TargetArchitecture=$(MONO_ARCH) \
	/p:TargetOS=Android \
	/p:Configuration=$(MONO_CONFIG) \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	/p:ForceAOT=$(AOT) \
	/p:UseLLVM=$(USE_LLVM) \
	/p:RunActivity=false

clean:
	rm -rf ../../../../artifacts/bin/AndroidSampleApp
