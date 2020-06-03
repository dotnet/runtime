MONO_CONFIG=Debug
MONO_ARCH=x64
DOTNET := ../../../../.././dotnet.sh
USE_LLVM=True

all: runtimepack run

TOOLS_DIR=../../../../../tools-local/tasks/mobile.tasks
appbuilder:
	$(DOTNET) build -c Release $(TOOLS_DIR)/AotCompilerTask/MonoAOTCompiler.csproj
	$(DOTNET) build -c Release $(TOOLS_DIR)/AppleAppBuilder/AppleAppBuilder.csproj

runtimepack:
	../../../../.././build.sh Mono+Libs -os iOS -arch $(MONO_ARCH) -c $(MONO_CONFIG)

run: clean appbuilder
	$(DOTNET) publish -c $(MONO_CONFIG) /p:TargetArchitecture=$(MONO_ARCH) \
	/p:UseLLVM=$(USE_LLVM) /p:UseAotForSimulator=true

clean:
	rm -rf bin
