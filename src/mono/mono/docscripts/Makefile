all: monoapi.tree

monoapi.tree: toc.xml 
	monodoc --assemble --out monoapi --hb toc.xml

extract: convert.exe
	perl exdoc -h . ../*/*.c

convert.exe: convert.cs AgilityPack.dll
	mcs convert.cs -r:AgilityPack.dll

install:
	cp monoapi.* `monodoc --get-sourcesdir`