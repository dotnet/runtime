all: genproj.exe prepare.exe
	mono genproj.exe

genproj.exe: genproj.cs
	mcs genproj.cs -r:System.Xml.Linq

prepare.exe: prepare.cs
	mcs prepare.cs
