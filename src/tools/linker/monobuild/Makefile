MSBUILD = msbuild
CONFIGURATION = Debug

all: build check

build: prepare nuget-hack
	$(MSBUILD) ../linker.sln /p:Configuration=$(CONFIGURATION)

# A hack for project.assets.json issues
nuget-hack:
	rm -f ../cecil/obj/project.assets.json
	rm -f ../cecil/symbols/pdb/obj/project.assets.json
	rm -f ../linker/obj/project.assets.json

clean:
	$(MSBUILD) ../linker.sln /t:clean

prepare:
	nuget restore ../linker.sln

check: prepare
	mono ../packages/NUnit.ConsoleRunner.3.6.1/tools/nunit3-console.exe --result="TestResults.xml;format=nunit2" ../linker/Tests/bin/$(CONFIGURATION)/Mono.Linker.Tests.dll
