all: genproj.exe prepare.exe
	mono --debug genproj.exe

genproj.exe: genproj.cs ../../mcs/build/gensources.cs
	-rm $@
	csc -debug:portable genproj.cs ../../mcs/build/gensources.cs -main:Driver -r:System.dll -r:System.Core.dll -out:$@

prepare.exe: prepare.cs
	csc prepare.cs
