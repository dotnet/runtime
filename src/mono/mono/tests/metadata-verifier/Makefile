GLIB_CFLAGS=`pkg-config --cflags glib-2.0`
GLIB_LINK_FLAGS=`pkg-config --libs glib-2.0`

%.exe: %.il
	ilasm2 -out:$@ $<

assembly-with-resource.exe: assembly-with-resource.cs
	gmcs assembly-with-resource.cs -linkresource:Makefile -resource:simple-assembly.il

assembly-with-complex-type.exe: assembly-with-complex-type.cs
	gmcs assembly-with-complex-type.cs

assembly-with-methods.exe: assembly-with-methods.cs
	gmcs assembly-with-methods.cs

gen-md-tests: gen-md-tests.c
	gcc $(GLIB_CFLAGS) $(GLIB_LINK_FLAGS) -I../../../ gen-md-tests.c ../../mini/.libs/libmono.a -g -Wall -lpthread -lm -ldl -lrt -o gen-md-tests

header-tests.ok: gen-md-tests header-tests.md simple-assembly.exe
	./gen-md-tests header-tests.md
	touch header-tests.ok

section-table-tests.ok: gen-md-tests section-table-tests.md simple-assembly.exe
	./gen-md-tests section-table-tests.md
	touch section-table-tests.ok

data-directory-tests.ok: gen-md-tests data-directory-tests.md simple-assembly.exe
	./gen-md-tests data-directory-tests.md
	touch data-directory-tests.ok

resources-tests.ok: gen-md-tests resources-tests.md assembly-with-resource.exe
	./gen-md-tests resources-tests.md
	touch resources-tests.ok

cli-header-tests.ok: gen-md-tests cli-header-tests.md simple-assembly.exe
	./gen-md-tests cli-header-tests.md
	touch cli-header-tests.ok

cli-metadata-tests.ok: gen-md-tests cli-metadata-tests.md simple-assembly.exe
	./gen-md-tests cli-metadata-tests.md
	touch cli-metadata-tests.ok

cli-tables-tests.ok: gen-md-tests cli-tables-tests.md simple-assembly.exe assembly-with-complex-type.exe assembly-with-methods.exe assembly-with-global-method.exe
	./gen-md-tests cli-tables-tests.md
	touch cli-tables-tests.ok

generate-stamp: header-tests.ok section-table-tests.ok data-directory-tests.ok resources-tests.ok cli-header-tests.ok cli-metadata-tests.ok cli-tables-tests.ok
	touch generate-stamp

clean-stamps:
	rm -f *.ok
	rm -f generate-stamp

run-test: generate-stamp
	@for i in *.exe; do \
		RES=99; \
		FIRST=`echo $$i | cut -d '-' -f 1`; \
		if [ "$$FIRST" == "invalid" ];	then \
			RES=1; \
		fi; \
		if [ "$$FIRST" == "valid" ]; then \
			RES=0; \
		fi; \
		if [ $$RES != 99 ]; then \
			../../metadata/pedump --verify metadata $$i >/dev/null 2>/dev/null; \
			R=$$?; \
			if [ $$R != $$RES ]; then \
				echo "$$i failed expected $$RES but got $$R"; \
			fi; \
		fi; \
	done



check_with_peverify: generate-stamp
	@for i in *.exe; do \
		RES=99; \
		FIRST=`echo $$i | cut -d '-' -f 1`; \
		if [ "$$FIRST" == "invalid" ];	then \
			RES=1; \
		fi; \
		if [ "$$FIRST" == "valid" ]; then \
			RES=0; \
		fi; \
		if [ $$RES != 99 ]; then \
			peverify $$i >/dev/null 2>/dev/null; \
			R=$$?; \
			if [ $$R != $$RES ]; then \
				echo "$$i failed expected $$RES but got $$R"; \
			fi; \
		fi; \
	done
