all: genproj.exe
	mono genproj.exe

genproj.exe: genproj.cs
	mcs genproj.cs -pkg:dotnet -r:System.Xml.Linq