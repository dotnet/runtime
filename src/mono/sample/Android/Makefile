MONO_CONFIG=Release
MONO_ARCH?=x64
DOTNET := ../../../../dotnet.sh
USE_LLVM=true
AOT=false
DEPLOY_AND_RUN?=true
#RUNTIME_COMPONENTS=diagnostics_tracing
#DIAGNOSTIC_PORTS=127.0.0.1:9000,nosuspend
#DIAGNOSTIC_PORTS=127.0.0.1:9000,suspend
#DIAGNOSTIC_PORTS=$(DOTNET_DiagnosticPorts)


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
	/p:RunActivity=false \
	'/p:RuntimeComponents="$(RUNTIME_COMPONENTS)"' \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"'

clean:
	rm -rf ../../../../artifacts/bin/AndroidSampleApp
