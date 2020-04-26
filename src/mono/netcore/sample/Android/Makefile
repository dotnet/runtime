MONO_CONFIG=Debug
MONO_ARCH=x64
DOTNET := ../../../../.././dotnet.sh

all: runtimepack bundle

bundle: clean
	$(DOTNET) build -c $(MONO_CONFIG) Program.csproj
	$(DOTNET) msbuild /t:BuildAppBundle /p:Configuration=$(MONO_CONFIG) /p:TargetArchitecture=$(MONO_ARCH)

deploy-launch:
	$(DOTNET) msbuild /t:ReinstallAndLaunch

runtimepack:
	../../../../.././build.sh -c $(MONO_CONFIG) -os Android -arch $(MONO_ARCH) -subset Mono+Libs /p:DisableCrossgen=true

clean:
	rm -rf bin
