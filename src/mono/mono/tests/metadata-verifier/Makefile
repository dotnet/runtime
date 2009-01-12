GLIB_CFLAGS=`pkg-config --cflags glib-2.0`
GLIB_LINK_FLAGS=`pkg-config --libs glib-2.0`

%.exe: %.il
	ilasm2 -out:$@ $<

gen-md-tests: gen-md-tests.c
	gcc $(GLIB_CFLAGS) $(GLIB_LINK_FLAGS) gen-md-tests.c -g -o gen-md-tests

header-tests.ok: header-tests.md simple-assembly.exe
	./gen-md-tests header-tests.md
	touch header_tests.ok

generate_stamp: header-tests.ok
	touch generate-stamp
