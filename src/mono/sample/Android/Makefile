MONO_CONFIG=Release
MONO_ARCH?=x64
DOTNET := ../../../../dotnet.sh
USE_LLVM=true
AOT=false
AOT_WITH_LIBRARY_FILES=false
INTERP=false
DEPLOY_AND_RUN?=true

#If DIAGNOSTIC_PORTS is enabled, RUNTIME_COMPONENTS must also be enabled.
#If RUNTIME_COMPONENTS is enabled, DIAGNOSTIC_PORTS is optional.
#If RUNTIME_COMPONENTS is enabled, DIAGNOSTIC_PORTS is disabled, use DOTNET_DiagnosticPorts when launching application to enable diagnostics.
#RUNTIME_COMPONENTS=diagnostics_tracing
#DIAGNOSTIC_PORTS=10.0.2.2:9000,nosuspend
#DIAGNOSTIC_PORTS=10.0.2.2:9000,suspend
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
	/p:RunAOTCompilation=$(AOT) \
	/p:ForceAOT=$(AOT) \
	/p:AOTWithLibraryFiles=$(AOT_WITH_LIBRARY_FILES) \
	/p:MonoForceInterpreter=$(INTERP) \
	/p:UseLLVM=$(USE_LLVM) \
	/p:RunActivity=false \
	'/p:RuntimeComponents="$(RUNTIME_COMPONENTS)"' \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"'

clean:
	rm -rf ../../../../artifacts/bin/AndroidSampleApp
