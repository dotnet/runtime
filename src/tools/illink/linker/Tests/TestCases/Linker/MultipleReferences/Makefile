include ../../config.make

all:
	MCS /t:library NotLinkedAttribute.cs
	MCS /t:library Bar.cs
	MCS /t:library /r:Bar.dll /r:NotLinkedAttribute.dll Foo.cs
	MCS /t:library /r:Foo.dll /r:Bar.dll /r:NotLinkedAttribute.dll Baz.cs
