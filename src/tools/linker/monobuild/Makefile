MSBUILD = msbuild
CONFIGURATION = Debug

all: build check

build: prepare
	$(MSBUILD) ../monolinker.sln /p:Configuration=$(CONFIGURATION)

clean:
	$(MSBUILD) ../monolinker.sln /t:clean

prepare:
	nuget restore ../monolinker.sln
	# The nunit runner used below seems to expect the old package layout.
	# This could potentially be removed with an update to nunit.
	nuget restore -PackagesDirectory ../packages ../test/Mono.Linker.Tests/packages.config

check: prepare
	mono ../packages/NUnit.ConsoleRunner.3.6.1/tools/nunit3-console.exe --result="TestResults.xml;format=nunit2" ../test/Mono.Linker.Tests/bin/$(CONFIGURATION)/net471/Mono.Linker.Tests.dll
