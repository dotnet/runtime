MONO_CONFIG=Release
MONO_ARCH=arm64
DOTNET := ../../../../.././dotnet.sh

all: runtimepack run

appbuilder:
	$(DOTNET) build -c Release ../../../../../tools-local/tasks/mobile.tasks/AndroidAppBuilder/AndroidAppBuilder.csproj

runtimepack:
	../../../../.././build.sh Mono+Libs -os Android -arch $(MONO_ARCH) -c $(MONO_CONFIG)

run: clean appbuilder
	$(DOTNET) publish -c $(MONO_CONFIG) -r android-$(MONO_ARCH) \
	/p:Platform=$(MONO_ARCH) /p:DeployAndRun=true

clean:
	rm -rf bin
