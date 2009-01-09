
%.exe: %.il
	ilasm2 -out:$@ $<

gen-md-tests: gen-md-tests.c
	gcc gen-md-tests.c -o gen-md-tests

header-tests.ok: header-tests.md simple-assembly.exe
	./gen-md-tests header-tests.md
	touch header_tests.ok

generate_stamp: header-tests.ok
	touch generate-stamp
