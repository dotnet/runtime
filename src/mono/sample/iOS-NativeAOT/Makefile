.DEFAULT_GOAL := all

TOP=../../../../

BUILD_CONFIG?=Debug
TARGET_ARCH?=$(shell . $(TOP)eng/common/native/init-os-and-arch.sh && echo $${arch})
TARGET_OS?=iossimulator
DEPLOY_AND_RUN?=false
STRIP_DEBUG_SYMBOLS?=false
USE_RUNTIME_PACKS?=false
STATS?=false
HYBRID_GLOBALIZATION?=true

REPO_DIR=$(realpath $(TOP))
TASKS_DIR=$(REPO_DIR)/src/tasks
DOTNET=$(REPO_DIR)/dotnet.sh
BUILD_SCRIPT=$(REPO_DIR)/build.sh
ifeq ($(USE_RUNTIME_PACKS),false)
LOCAL_NUGET_RESTORE_DIR=
else
LOCAL_NUGET_RESTORE_DIR=$(realpath .)/packages
endif

world: build-deps all

# build all dependencies: runtime, nativeAot and all the libs
ifeq ($(USE_RUNTIME_PACKS),false)
build-deps: build-runtime-libs-ilc
else
build-deps: build-runtime-ilc-packs build-libs-all-packs
endif

# building for host
build-runtime-libs-ilc:
	$(BUILD_SCRIPT) --cross -s clr.alljits+clr.tools+clr.nativeaotruntime+clr.nativeaotlibs+libs -c $(BUILD_CONFIG) -os $(TARGET_OS) -arch $(TARGET_ARCH)

build-runtime-ilc-packs:
	$(BUILD_SCRIPT) clr+clr.aot+libs+packs -c $(BUILD_CONFIG)

build-libs-all-packs:
	$(BUILD_SCRIPT) clr.nativeaotruntime+clr.nativeaotlibs+libs+packs -c $(BUILD_CONFIG) -os $(TARGET_OS) -arch $(TARGET_ARCH) /p:BuildNativeAOTRuntimePack=true

build-libs-nativeaot:
	$(BUILD_SCRIPT) clr.nativeaotruntime+clr.nativeaotlibs -c $(BUILD_CONFIG) -os $(TARGET_OS) -arch $(TARGET_ARCH)

all: appbuilder hello-app

appbuilder:
	$(DOTNET) build -c $(BUILD_CONFIG) $(TASKS_DIR)/AppleAppBuilder/AppleAppBuilder.csproj

ifeq ($(USE_RUNTIME_PACKS),true)
hello-app: export NUGET_PACKAGES = $(LOCAL_NUGET_RESTORE_DIR)
endif

hello-app: clean
	$(DOTNET) \
	publish -c $(BUILD_CONFIG) \
	-p:TargetOS=$(TARGET_OS) \
	-p:TargetArchitecture=$(TARGET_ARCH) \
	-p:DeployAndRun=$(DEPLOY_AND_RUN) \
	-p:StripDebugSymbols=$(STRIP_DEBUG_SYMBOLS) \
	-p:PublishAotUsingRuntimePack=$(USE_RUNTIME_PACKS) \
	-p:GenerateStats=$(STATS) \
	-p:HybridGlobalization=$(HYBRID_GLOBALIZATION) \
	-bl

clean:
	rm -rf obj bin *.binlog $(LOCAL_NUGET_RESTORE_DIR)
