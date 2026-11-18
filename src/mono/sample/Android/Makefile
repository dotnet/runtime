BUILD_CONFIG=Release
TARGET_ARCH?=x64
DOTNET := ../../../../dotnet.sh
DEPLOY_AND_RUN?=true
R2R?=false
R2R_COMPOSITE?=false
STATIC_LINKING?=false

#DIAGNOSTIC_PORTS=10.0.2.2:9000,nosuspend
#DIAGNOSTIC_PORTS=10.0.2.2:9000,suspend
#DIAGNOSTIC_PORTS=$(DOTNET_DiagnosticPorts)

TOP=../../../../
REPO_DIR=$(realpath $(TOP))
TASKS_DIR=$(REPO_DIR)/src/tasks
DOTNET=$(REPO_DIR)/dotnet.sh
BUILD_SCRIPT=$(REPO_DIR)/build.sh

all: appbuilder runtimepack run

appbuilder:
	$(DOTNET) build -c $(BUILD_CONFIG) $(TASKS_DIR)/AndroidAppBuilder/AndroidAppBuilder.csproj

runtimepack:
	$(BUILD_SCRIPT) clr.runtime+clr.alljits+clr.corelib+clr.nativecorelib+clr.tools+clr.packages+libs -os android -arch $(TARGET_ARCH) -c $(BUILD_CONFIG) -bl

run: appbuilder
	$(DOTNET) publish \
	/p:TargetArchitecture=$(TARGET_ARCH) \
	/p:TargetOS=android \
	/p:Configuration=$(BUILD_CONFIG) \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	/p:AOTWithLibraryFiles=$(AOT_WITH_LIBRARY_FILES) \
	/p:RunActivity=false \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"' \
	/p:PublishReadyToRun=$(R2R) \
	/p:PublishReadyToRunComposite=$(R2R_COMPOSITE) \
	/p:StaticLinkedRuntime=$(STATIC_LINKING) \
	/bl
clean:
	rm -rf $(REPO_DIR)/artifacts/bin/AndroidSampleApp
	rm -rf $(REPO_DIR)/artifacts/obj/mono/AndroidSampleApp
