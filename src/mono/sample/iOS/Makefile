.DEFAULT_GOAL := all

TOP=../../../../

BUILD_CONFIG?=Debug
TARGET_ARCH?=$(shell . $(TOP)eng/common/native/init-os-and-arch.sh && echo $${arch})
TARGET_OS?=iossimulator
USE_LLVM=true
DEPLOY_AND_RUN?=true
APP_SANDBOX?=false
STRIP_DEBUG_SYMBOLS?=false # only used when measuring SOD via build-appbundle make target
HYBRID_GLOBALIZATION?=true

#If DIAGNOSTIC_PORTS is enabled, @(RuntimeComponents) must also include 'diagnostics_tracing'.
#If @(RuntimeComponents) includes 'diagnostics_tracing', DIAGNOSTIC_PORTS is optional.
#If @(RuntimeComponents) includes 'diagnostics_tracing', and DIAGNOSTIC_PORTS is disabled, then use DOTNET_DiagnosticPorts when launching application to enable diagnostics.
#DIAGNOSTIC_PORTS=127.0.0.1:9000,nosuspend
#DIAGNOSTIC_PORTS=127.0.0.1:9000,suspend
#DIAGNOSTIC_PORTS=$(DOTNET_DiagnosticPorts)

REPO_DIR=$(realpath $(TOP))
TASKS_DIR=$(REPO_DIR)/src/tasks
DOTNET=$(REPO_DIR)/dotnet.sh
BUILD_SCRIPT=$(REPO_DIR)/build.sh

all: runtimepack run

appbuilder:
	$(DOTNET) build -c $(BUILD_CONFIG) $(TASKS_DIR)/AotCompilerTask/MonoAOTCompiler.csproj
	$(DOTNET) build -c $(BUILD_CONFIG) $(TASKS_DIR)/MonoTargetsTasks/MonoTargetsTasks.csproj
	$(DOTNET) build -c $(BUILD_CONFIG) $(TASKS_DIR)/AppleAppBuilder/AppleAppBuilder.csproj

runtimepack:
	$(BUILD_SCRIPT) mono+libs -os $(TARGET_OS) -arch $(TARGET_ARCH) -c $(BUILD_CONFIG)

build-appbundle: clean appbuilder
	$(DOTNET) publish \
	-c $(BUILD_CONFIG) \
	/p:TargetOS=$(TARGET_OS) \
	/p:TargetArchitecture=$(TARGET_ARCH) \
	/p:MonoEnableLLVM=$(USE_LLVM) \
	/p:StripDebugSymbols=$(STRIP_DEBUG_SYMBOLS) \
	/p:DeployAndRun=false \
	/p:HybridGlobalization=$(HYBRID_GLOBALIZATION) \
	/bl

run: clean appbuilder
	$(DOTNET) publish \
	-c $(BUILD_CONFIG) \
	/p:TargetOS=ios \
	/p:TargetArchitecture=$(TARGET_ARCH) \
	/p:MonoEnableLLVM=$(USE_LLVM) \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"' \
	/bl

run-sim: clean appbuilder
	$(DOTNET) publish \
	-c $(BUILD_CONFIG) \
	/p:TargetOS=iossimulator \
	/p:TargetArchitecture=$(TARGET_ARCH) \
	/p:MonoEnableLLVM=$(USE_LLVM) \
	/p:MonoForceInterpreter=false \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"' \
	/bl

run-sim-interp: clean appbuilder
	$(DOTNET) publish \
	-c $(BUILD_CONFIG) \
	/p:TargetOS=iossimulator \
	/p:TargetArchitecture=$(TARGET_ARCH) \
	/p:MonoEnableLLVM=$(USE_LLVM) \
	/p:MonoForceInterpreter=true \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"' \
	/bl

run-catalyst: clean appbuilder
	$(DOTNET) publish \
	-c $(BUILD_CONFIG) \
	/p:TargetOS=maccatalyst \
	/p:TargetArchitecture=$(TARGET_ARCH) \
	/p:MonoEnableLLVM=false \
	/p:MonoForceInterpreter=false \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	/p:EnableAppSandbox=$(APP_SANDBOX) \
	/bl

run-catalyst-interp: clean appbuilder
	$(DOTNET) publish \
	-c $(BUILD_CONFIG) \
	/p:TargetOS=maccatalyst \
	/p:TargetArchitecture=$(TARGET_ARCH) \
	/p:MonoEnableLLVM=false \
	/p:MonoForceInterpreter=true \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	/p:EnableAppSandbox=$(APP_SANDBOX) \
	/bl

clean:
	rm -rf obj bin *.binlog
