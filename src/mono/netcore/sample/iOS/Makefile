MONO_CONFIG=Debug
MONO_ARCH=x64
DOTNET := ../../../../.././dotnet.sh
USE_LLVM=True

# usage example:
# 'make all MONO_ARCH=x64 MONO_CONFIG=Release' to build the app for simulator
all: bundle

program:
	$(DOTNET) build -c $(MONO_CONFIG) Program.csproj

bundle: clean program
	$(DOTNET) msbuild /t:BuildAppBundle /p:Configuration=$(MONO_CONFIG) /p:TargetArchitecture=$(MONO_ARCH) \
	/p:UseLLVM=$(USE_LLVM)

deploy-sim:
	$(DOTNET) msbuild /t:IosDeployToSimulator /p:Configuration=$(MONO_CONFIG) /p:TargetArchitecture=$(MONO_ARCH)

clean:
	rm -rf bin
