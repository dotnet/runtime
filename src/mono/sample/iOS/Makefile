MONO_CONFIG?=Debug
MONO_ARCH?=x64
DOTNET := ../../../../dotnet.sh
USE_LLVM=true
TARGET?=iossimulator
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

all: runtimepack run

TOOLS_DIR=../../../tasks
appbuilder:
	$(DOTNET) build -c Debug $(TOOLS_DIR)/AotCompilerTask/MonoAOTCompiler.csproj
	$(DOTNET) build -c Debug $(TOOLS_DIR)/MonoTargetsTasks/MonoTargetsTasks.csproj
	$(DOTNET) build -c Debug $(TOOLS_DIR)/AppleAppBuilder/AppleAppBuilder.csproj

runtimepack:
	../../../../build.sh mono+libs -os $(TARGET) -arch $(MONO_ARCH) -c $(MONO_CONFIG)

build-appbundle: clean appbuilder
	$(DOTNET) publish \
	-c $(MONO_CONFIG) \
	/p:TargetOS=$(TARGET) \
	/p:TargetArchitecture=$(MONO_ARCH) \
	/p:MonoEnableLLVM=$(USE_LLVM) \
	/p:StripDebugSymbols=$(STRIP_DEBUG_SYMBOLS) \
	/p:DeployAndRun=false \
	/p:HybridGlobalization=$(HYBRID_GLOBALIZATION) \
	/bl

run: clean appbuilder
	$(DOTNET) publish \
	-c $(MONO_CONFIG) \
	/p:TargetOS=ios \
	/p:TargetArchitecture=$(MONO_ARCH) \
	/p:MonoEnableLLVM=$(USE_LLVM) \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"' \
	/bl

run-sim: clean appbuilder
	$(DOTNET) publish \
	-c $(MONO_CONFIG) \
	/p:TargetOS=iossimulator \
	/p:TargetArchitecture=$(MONO_ARCH) \
	/p:MonoEnableLLVM=$(USE_LLVM) \
	/p:MonoForceInterpreter=false \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"' \
	/bl

run-sim-interp: clean appbuilder
	$(DOTNET) publish \
	-c $(MONO_CONFIG) \
	/p:TargetOS=iossimulator \
	/p:TargetArchitecture=$(MONO_ARCH) \
	/p:MonoEnableLLVM=$(USE_LLVM) \
	/p:MonoForceInterpreter=true \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	'/p:DiagnosticPorts="$(DIAGNOSTIC_PORTS)"' \
	/bl

run-catalyst: clean appbuilder
	$(DOTNET) publish \
	-c $(MONO_CONFIG) \
	/p:TargetOS=maccatalyst \
	/p:TargetArchitecture=$(MONO_ARCH) \
	/p:MonoEnableLLVM=false \
	/p:MonoForceInterpreter=false \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	/p:EnableAppSandbox=$(APP_SANDBOX) \
	/bl

run-catalyst-interp: clean appbuilder
	$(DOTNET) publish \
	-c $(MONO_CONFIG) \
	/p:TargetOS=maccatalyst \
	/p:TargetArchitecture=$(MONO_ARCH) \
	/p:MonoEnableLLVM=false \
	/p:MonoForceInterpreter=true \
	/p:DeployAndRun=$(DEPLOY_AND_RUN) \
	/p:EnableAppSandbox=$(APP_SANDBOX) \
	/bl

clean:
	rm -rf bin
