MCS = mcs

all: Mono.Linker.Tests.dll

Mono.Cecil.dll:
	cp ../Mono.Cecil.dll .

monolinker.exe:
	cp ../monolinker.exe .

Mono.Linker.Tests.dll.sources:
	find Mono.Linker.Tests -name "*.cs" > Mono.Linker.Tests.dll.sources

Mono.Linker.Tests.dll: Mono.Cecil.dll monolinker.exe Mono.Linker.Tests.dll.sources
	$(MCS) /target:library /out:Mono.Linker.Tests.dll /r:nunit.framework.dll /r:Mono.Cecil.dll /r:monolinker.exe @Mono.Linker.Tests.dll.sources

clean:
	rm -rf Mono.Cecil.dll monolinker.exe Mono.Linker.Tests.dll.sources Mono.Linker.Tests.dll

run-test: all
	nunit-console Mono.Linker.Tests.dll
