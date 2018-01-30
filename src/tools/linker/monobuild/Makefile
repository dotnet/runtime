MSBUILD = msbuild
CONFIGURATION = Debug

all: build check

build: prepare
	$(MSBUILD) ../linker.sln /p:Configuration=$(CONFIGURATION)

clean:
	$(MSBUILD) ../linker.sln /t:clean

prepare:
	nuget restore ../linker.sln
	# A hack for project.assets.json issues
	rm -f ../cecil/obj/project.assets.json
	rm -f ../cecil/symbols/pdb/obj/project.assets.json
	rm -f ../cecil/symbols/mdb/obj/project.assets.json
	rm -f ../linker/obj/project.assets.json

check: prepare
	mono ../packages/NUnit.ConsoleRunner.3.6.1/tools/nunit3-console.exe --result="TestResults.xml;format=nunit2" ../linker/Tests/bin/$(CONFIGURATION)/Mono.Linker.Tests.dll
