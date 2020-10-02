MONO_CONFIG=Release
MONO_ARCH=x64
DOTNET := ../../../../.././dotnet.sh

all: runtimepack run

runtimepack:
	../../../../.././build.sh Mono+Libs -os Android -arch $(MONO_ARCH) -c $(MONO_CONFIG)

run:
	$(DOTNET) build \
	/p:TargetArchitecture=$(MONO_ARCH) \
	/p:TargetOS=Android \
	/p:Configuration=$(MONO_CONFIG) \
	/p:DeployAndRun=true

clean:
	rm -rf bin
