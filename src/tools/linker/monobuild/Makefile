MSBUILD = msbuild
CONFIGURATION = Debug

all: build check

build:
	$(MSBUILD) ../monolinker.sln /restore /p:Configuration=$(CONFIGURATION) /p:MonoBuild=true

clean:
	$(MSBUILD) ../monolinker.sln /p:MonoBuild=true /t:clean

check:
	$(MSBUILD) ../test/Mono.Linker.Tests/Mono.Linker.Tests.csproj /p:MonoBuild=true /t:RunTestsOnMono
