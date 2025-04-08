BUILD_CONFIG=Release
TARGET_ARCH?=x64
DOTNET := ../../../../dotnet.sh
USE_LLVM=true
AOT=false
AOT_WITH_LIBRARY_FILES=false
INTERP=false
DEPLOY_AND_RUN?=true
RUNTIME_FLAVOR?=mono
R2R?=false
R2R_COMPOSITE?=false

#If DIAGNOSTIC_PORTS is enabled, @(RuntimeComponents) must also include 'diagnostics_tracing'.
#If @(RuntimeComponents) includes 'diagnostics_tracing', DIAGNOSTIC_PORTS is optional.
#If @(RuntimeComponents) includes 'diagnostics_tracing', and DIAGNOSTIC_PORTS is disabled, then use DOTNET_DiagnosticPorts when launching application to enable diagnostics.
#DIAGNOSTIC_PORTS=10.0.2.2:9000,nosuspend
#DIAGNOSTIC_PORTS=10.0.2.2:9000,suspend
#DIAGNOSTIC_PORTS=$(DOTNET_DiagnosticPorts)

TOP=../../../../
REPO_DIR=$(realpath $(TOP))
TASKS_DIR=$(REPO_DIR)/src/tasks
DOTNET=$(REPO_DIR)/dotnet.sh
BUILD_SCRIPT=$(REPO_DIR)/build.sh
RUNTIME_FLAVOR_LOWER := $(shell echo $(RUNTIME_FLAVOR) | tr '[:upper:]' '[:lower:]')

all: appbuilder runtimepack run

appbuilder:
	$(DOTNET) build -c $(BUILD_CONFIG) $(TASKS_DIR)/AndroidAppBuilder/AndroidAppBuilder.csproj

ifeq ($(RUNTIME_FLAVOR_LOWER),mono)
runtimepack:
	$(BUILD_SCRIPT) mono+libs -os android -arch $(TARGET_ARCH) -c $(BUILD_CONFIG) -bl
else ifeq ($(RUNTIME_FLAVOR_LOWER),coreclr)
runtimepack:
	$(BUILD_SCRIPT) clr.runtime+clr.alljits+clr.corelib+clr.nativecorelib+clr.tools+clr.packages+libs -os android -arch $(TARGET_ARCH) -c $(BUILD_CONFIG) -bl
else
runtimepack:
	@echo "Invalid RUNTIME_FLAVOR: $(RUNTIME_FLAVOR)"
	@exit 1
endif

run: appbuilder
	$(DOTNET) publish \
	/p:TargetArchitecture=$(TARGET_ARCH) \
	/p:TargetOS=android \
	/p:Configuration=$(BUILD_CONFIG) \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	/p:RunAOTCompilation=$(AOT) \
	/p:ForceAOT=$(AOT) \
	/p:AOTWithLibraryFiles=$(AOT_WITH_LIBRARY_FILES) \
	/p:MonoForceInterpreter=$(INTERP) \
	/p:MonoEnableLLVM=$(USE_LLVM) \
	/p:RunActivity=false \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"' \
	/p:RuntimeFlavor=$(RUNTIME_FLAVOR) \
	/p:PublishReadyToRun=$(R2R) \
	/p:PublishReadyToRunComposite=$(R2R_COMPOSITE) \
	/bl
clean:
	rm -rf $(REPO_DIR)/artifacts/bin/AndroidSampleApp
	rm -rf $(REPO_DIR)/artifacts/obj/mono/AndroidSampleApp