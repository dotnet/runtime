MONO_CONFIG=Release
MONO_ARCH=x64
DOTNET := ../../../../.././dotnet.sh

all: run

run:
	$(DOTNET) publish \
	/p:TargetArchitecture=$(MONO_ARCH) \
	/p:TargetOS=Android \
	/p:Configuration=$(MONO_CONFIG) \
	/p:DeployAndRun=true \
	/p:RunActivity=false

clean:
	rm -rf ../../../../../artifacts/bin/AndroidSampleApp
