NETDIR=c:\\winnt\\microsoft.net\\framework\\v1.0.2914
MCSCLASSDIR=..\\..\\mcs\\class
CSC=//c/winnt/microsoft.net/framework/v1.0.2914/csc.exe

all: exec xml

exec:
	$(CSC) /debug+ /debug:full /target:exe /out:compare.exe compare-assembly.cs

xml:
	#./compare.exe $(NETDIR)\\mscorlib.dll $(MCSCLASSDIR)\\corlib\\corlib.dll > corlib.xml
	./compare.exe $(MCSCLASSDIR)\\corlib\\corlib.dll

push:
	perl mono-stats
	scp *.html *.xml www@www:/web/cvsmodules/mono/class-status
