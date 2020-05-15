MONO_CONFIG=Debug
MONO_ARCH=arm64
DOTNET := ../../../../.././dotnet.sh

#export ANDROID_NDK_ROOT=/path/to/android/ndk
#export ANDROID_SDK_ROOT=/path/to/android/sdk

all: bundle

bundle: clean
	$(DOTNET) build -c $(MONO_CONFIG) Program.csproj
	$(DOTNET) msbuild /t:BuildAppBundle /p:Configuration=$(MONO_CONFIG) /p:TargetArchitecture=$(MONO_ARCH)

deploy-launch: bundle
	$(DOTNET) msbuild /t:ReinstallAndLaunch

clean:
	rm -rf bin
