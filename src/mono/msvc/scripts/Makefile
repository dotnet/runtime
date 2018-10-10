all: genproj.exe prepare.exe
	mono --debug genproj.exe

genproj.exe: genproj.cs ../../mcs/tools/gensources/gensources.cs
	-rm $@
	csc -debug:portable $^ -main:Driver -r:System.dll -r:System.Core.dll -out:$@

prepare.exe: prepare.cs
	csc prepare.cs
