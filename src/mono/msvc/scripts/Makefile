all: genproj.exe prepare.exe
	mono --debug genproj.exe

genproj.exe: genproj.cs
	mcs -debug:portable -g genproj.cs -r:System.Xml.Linq

prepare.exe: prepare.cs
	mcs prepare.cs
