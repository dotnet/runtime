NETDIR=c:\\winnt\\microsoft.net\\framework\\v1.0.2914
MCSCLASSDIR=..\\..\\mcs\\class
CSC=csc.exe /nologo

all: ByMaintainer ByNamespace webfiles

exec:
	$(CSC) /debug+ /debug:full /target:exe /out:compare.exe compare-assembly.cs

xml:
	./compare.exe $(NETDIR)\\mscorlib.dll $(MCSCLASSDIR)\\corlib\\corlib.dll > corlib.xml

push:
	scp -P 2200 *.xml web/*html www@www.ximian.com:/web/cvsmodules/mono/class-status

clean:
	-rm -f compare.exe compare.pdb corlib.xml ByMaintainer.exe ByNamespace.exe commands *~ .*~
	-rm -rf src
	-rm -rf bm
	-rm -rf web

ByNamespace: ByNamespace.xsl class.xml
	$(CSC) ByNamespace.cs
	-mkdir src
	./ByNamespace

ByMaintainer: ByMaintainer.xsl class.xml
	$(CSC) ByMaintainer.cs
	-mkdir src
	./ByMaintainer

webfiles:
	-mkdir web
	./make_web.pl
	cp *.src src
	../doc/web/process.pl commands ../doc/web/template.html.in web
