all: genproj.exe prepare.exe
	mono --debug genproj.exe

genproj.exe: genproj.cs ../../mcs/build/gensources.cs
	csc -debug:portable genproj.cs ../../mcs/build/gensources.cs -main:Driver -r:System.dll -r:System.Core.dll 

prepare.exe: prepare.cs
	csc prepare.cs
