NETDIR=c:\\winnt\\microsoft.net\\framework\\v1.0.2914
MCSCLASSDIR=..\\..\\mcs\\class
CSC=//c/winnt/microsoft.net/framework/v1.0.2914/csc.exe /nologo

all: ByMaintainer ByNamespace web

exec:
	$(CSC) /debug+ /debug:full /target:exe /out:compare.exe compare-assembly.cs

xml:
	./compare.exe $(NETDIR)\\mscorlib.dll $(MCSCLASSDIR)\\corlib\\corlib.dll > corlib.xml

push:
	perl mono-stats
	scp *.html *.xml www@www:/web/cvsmodules/mono/class-status

clean:
	rm -f compare.exe compare.pdb corlib.xml ByMaintainer.exe ByNamespace.exe commands *~ .*~
	rm -rf bn
	rm -rf bm
	rm -rf web

ByNamespace: ByNamespace.xsl
	$(CSC) ByNamespace.cs
	mkdir bn
	./ByNamespace

ByMaintainer: ByMaintainer.xsl
	$(CSC) ByMaintainer.cs
	mkdir bm
	./ByMaintainer

web:
	mkdir web
	./make_web.pl
	../doc/web/process.pl commands ../doc/web/template.html.in web
