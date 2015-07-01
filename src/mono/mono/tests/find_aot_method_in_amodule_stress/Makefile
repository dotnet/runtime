MONO_BIN_DIR=$(shell dirname `which mono`)
MONO_DIR=$(shell echo $(MONO_BIN_DIR)/../)

all: Template.py mscorlib.dll
	python3 Template.py $(MONO_DIR) | grep -v find_aot_method_in_amodule

run:
	MONO_PATH=. mono --full-aot Hello.exe	

mscorlib.dll:
	cp $(MONO_DIR)/lib/mono/4.5/mscorlib.dll ./mscorlib.dll

clean:
	- rm -f *.dll
	- rm -f *.dylib
	- rm -f *.exe
	- rm -rf *.dSYM
	- rm -f *TypeBucket.cs Hello.cs Foo.cs
