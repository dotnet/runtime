.DEFAULT_GOAL := all

TOP=../../../../

BUILD_CONFIG?=Debug
TARGET_ARCH?=$(shell . $(TOP)eng/native/init-os-and-arch.sh && echo $${arch})
TARGET_OS?=iossimulator
DEPLOY_AND_RUN?=false
STRIP_DEBUG_SYMBOLS?=false

REPO_DIR=$(realpath $(TOP))
TASKS_DIR=$(REPO_DIR)/src/tasks
DOTNET=$(REPO_DIR)/dotnet.sh
BUILD_SCRIPT=$(REPO_DIR)/build.sh

world: build-deps all

# build all dependencies: runtime, nativeAot and all the libs
build-deps: build-runtime-ilc build-libs-all

# building for host
build-runtime-ilc:
	$(BUILD_SCRIPT) clr+clr.aot -c $(BUILD_CONFIG)

build-ilc:
	$(BUILD_SCRIPT) clr.aot -c $(BUILD_CONFIG)

# building for target platform
build-libs-all:
	$(BUILD_SCRIPT) clr.nativeaotruntime+clr.nativeaotlibs+libs -c $(BUILD_CONFIG) -os $(TARGET_OS) -arch $(TARGET_ARCH)

build-libs-nativeaot:
	$(BUILD_SCRIPT) clr.nativeaotruntime+clr.nativeaotlibs -c $(BUILD_CONFIG) -os $(TARGET_OS) -arch $(TARGET_ARCH)

all: appbuilder hello-app

appbuilder:
	$(DOTNET) build -c $(BUILD_CONFIG) $(TASKS_DIR)/AppleAppBuilder/AppleAppBuilder.csproj

hello-app: clean
	$(DOTNET) \
	build -c $(BUILD_CONFIG) \
	-p:TargetOS=$(TARGET_OS) \
	-p:TargetArchitecture=$(TARGET_ARCH) \
	-p:DeployAndRun=$(DEPLOY_AND_RUN) \
	-p:StripDebugSymbols=$(STRIP_DEBUG_SYMBOLS) \
	-bl

clean:
	rm -rf obj bin