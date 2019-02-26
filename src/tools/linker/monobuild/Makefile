MSBUILD = msbuild
CONFIGURATION = Debug

all: build check

build: prepare
	$(MSBUILD) ../monolinker.sln /p:Configuration=$(CONFIGURATION)

clean:
	$(MSBUILD) ../monolinker.sln /t:clean

prepare:
	nuget restore ../monolinker.sln
	# A hack for project.assets.json issues
	rm -f ../external/cecil/obj/project.assets.json
	rm -f ../external/cecil/symbols/pdb/obj/project.assets.json
	rm -f ../external/cecil/symbols/mdb/obj/project.assets.json
	rm -f ../src/linker/obj/project.assets.json

check: prepare
	mono ../packages/NUnit.ConsoleRunner.3.6.1/tools/nunit3-console.exe --result="TestResults.xml;format=nunit2" ../test/Mono.Linker.Tests/bin/$(CONFIGURATION)/Mono.Linker.Tests.dll
