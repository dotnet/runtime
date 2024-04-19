MONO_CONFIG=Release
MONO_ARCH?=x64
DOTNET := ../../../../dotnet.sh
USE_LLVM=true
AOT=false
AOT_WITH_LIBRARY_FILES=false
INTERP=false
DEPLOY_AND_RUN?=true

#If DIAGNOSTIC_PORTS is enabled, @(RuntimeComponents) must also include 'diagnostics_tracing'.
#If @(RuntimeComponents) includes 'diagnostics_tracing', DIAGNOSTIC_PORTS is optional.
#If @(RuntimeComponents) includes 'diagnostics_tracing', and DIAGNOSTIC_PORTS is disabled, then use DOTNET_DiagnosticPorts when launching application to enable diagnostics.
#DIAGNOSTIC_PORTS=10.0.2.2:9000,nosuspend
#DIAGNOSTIC_PORTS=10.0.2.2:9000,suspend
#DIAGNOSTIC_PORTS=$(DOTNET_DiagnosticPorts)

all: runtimepack run

runtimepack:
	../../../../build.sh mono+libs -os android -arch $(MONO_ARCH) -c $(MONO_CONFIG)

run:
	$(DOTNET) publish \
	/p:TargetArchitecture=$(MONO_ARCH) \
	/p:TargetOS=android \
	/p:Configuration=$(MONO_CONFIG) \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	/p:RunAOTCompilation=$(AOT) \
	/p:ForceAOT=$(AOT) \
	/p:AOTWithLibraryFiles=$(AOT_WITH_LIBRARY_FILES) \
	/p:MonoForceInterpreter=$(INTERP) \
	/p:UseLLVM=$(USE_LLVM) \
	/p:RunActivity=false \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"'
clean:
	rm -rf ../../../../artifacts/bin/AndroidSampleApp
