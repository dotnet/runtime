MSBUILD = msbuild
CONFIGURATION = Debug

all: build check

build: prepare
	$(MSBUILD) ../linker.sln /p:Configuration=$(CONFIGURATION)

clean:
	$(MSBUILD) ../linker.sln /t:clean

prepare:
	nuget restore ../linker.sln

check: prepare
	mono ../packages/NUnit.ConsoleRunner.3.6.1/tools/nunit3-console.exe ../linker/Tests/bin/$(CONFIGURATION)/Mono.Linker.Tests.dll
