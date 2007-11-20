all: monoapi.tree

monoapi.tree: toc.xml 
	cp api-style.css deploy
	mdassembler --out monoapi --hb toc.xml

extract: convert.exe
	mkdir -p html
	perl exdoc -h . ../*/*.c

convert.exe: convert.cs AgilityPack.dll
	mcs convert.cs -r:AgilityPack.dll

install:
	cp monoapi.* `pkg-config monodoc --variable=sourcesdir`
