.DEFAULT_GOAL := all

TOP=../../../../

BUILD_CONFIG?=Debug
TARGET_ARCH?=$(shell . $(TOP)eng/common/native/init-os-and-arch.sh && echo $${arch})
TARGET_OS?=iossimulator
DEPLOY_AND_RUN?=true
APP_SANDBOX?=false
STRIP_DEBUG_SYMBOLS?=false # only used when measuring SOD via build-appbundle make target
AOT?=true
R2R?=false

#DIAGNOSTIC_PORTS=127.0.0.1:9000,nosuspend
#DIAGNOSTIC_PORTS=127.0.0.1:9000,suspend
#DIAGNOSTIC_PORTS=$(DOTNET_DiagnosticPorts)

REPO_DIR=$(realpath $(TOP))
TASKS_DIR=$(REPO_DIR)/src/tasks
DOTNET=$(REPO_DIR)/dotnet.sh
BUILD_SCRIPT=$(REPO_DIR)/build.sh

all: runtimepack run

appbuilder:
	$(DOTNET) build -c $(BUILD_CONFIG) $(TASKS_DIR)/AppleAppBuilder/AppleAppBuilder.csproj

runtimepack:
	$(BUILD_SCRIPT) clr+libs -os $(TARGET_OS) -arch $(TARGET_ARCH) -c $(BUILD_CONFIG)

build-appbundle: clean appbuilder
	$(DOTNET) publish \
	-c $(BUILD_CONFIG) \
	/p:TargetOS=$(TARGET_OS) \
	/p:TargetArchitecture=$(TARGET_ARCH) \
	/p:StripDebugSymbols=$(STRIP_DEBUG_SYMBOLS) \
	/p:DeployAndRun=false \
	/p:PublishReadyToRun=$(R2R) \
	/bl

run: clean appbuilder
	$(DOTNET) publish \
	-c $(BUILD_CONFIG) \
	/p:TargetOS=ios \
	/p:TargetArchitecture=$(TARGET_ARCH) \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"' \
	/bl

run-sim: clean appbuilder
	$(DOTNET) publish \
	-c $(BUILD_CONFIG) \
	/p:TargetOS=iossimulator \
	/p:TargetArchitecture=$(TARGET_ARCH) \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"' \
	/bl

run-catalyst: clean appbuilder
	$(DOTNET) publish \
	-c $(BUILD_CONFIG) \
	/p:TargetOS=maccatalyst \
	/p:TargetArchitecture=$(TARGET_ARCH) \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	/p:EnableAppSandbox=$(APP_SANDBOX) \
	/bl

clean:
	rm -rf obj bin *.binlog
