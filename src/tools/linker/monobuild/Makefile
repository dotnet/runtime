MSBUILD = msbuild
CONFIGURATION = Debug

all: build check

build:
	$(MSBUILD) ../monolinker.sln /restore /p:Configuration=$(CONFIGURATION)

clean:
	$(MSBUILD) ../monolinker.sln /t:clean

check:
	$(MSBUILD) ../test/Mono.Linker.Tests/Mono.Linker.Tests.csproj /t:RunTestsOnMono
