
MCS = mcs
KEY_FILE = ../../class/mono.snk
MONO.CECIL.DLL = Mono.Cecil.dll
MCS_FLAGS = -debug -keyfile:$(KEY_FILE) -r:$(MONO.CECIL.DLL) -resource:Descriptors/mscorlib.xml -resource:Descriptors/System.xml -resource:Descriptors/System.Web.xml -resource:Descriptors/Mono.Posix.xml -resource:Descriptors/System.Drawing.xml
LINKER = monolinker.exe

all: config.make monolinker.exe monolinker

monolinker: monolinker.in Makefile
	sed "s,@prefix@,$(prefix)," < monolinker.in > monolinker
	chmod +x monolinker

monolinker.exe: Mono.Cecil.dll
	$(MCS) $(MCS_FLAGS) @$(LINKER).sources /out:$(LINKER)

Mono.Cecil.dll:
	if pkg-config --atleast-version=0.5 mono-cecil; then \
		cp `pkg-config --variable=Libraries mono-cecil` .; \
	else \
		echo You must install Mono.Cecil first; \
		exit 1; \
	fi

clean:
	rm -f $(LINKER) $(MONO.CECIL.DLL) monolinker

install: all
	mkdir -p $(prefix)/bin
	mkdir -p $(prefix)/lib/monolinker
	cp $(LINKER) $(MONO.CECIL.DLL) $(prefix)/lib/monolinker
	cp monolinker $(prefix)/bin
	cp man/monolinker.1 $(prefix)/share/man/man1

config.make:
	echo You must run configure first
	exit 1

include config.make

run-test: all
	cd ./Tests; \
	make clean run-test; \
	cd ..;
