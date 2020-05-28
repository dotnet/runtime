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

assembly-with-params.exe: assembly-with-params.cs
	gmcs assembly-with-params.cs
	
assembly-with-constants.exe: assembly-with-constants.cs
	gmcs assembly-with-constants.cs

assembly-with-cattr.exe: assembly-with-cattr.cs
	gmcs assembly-with-cattr.cs

assembly-with-cattr-enc.exe: assembly-with-cattr-enc.cs
	gmcs assembly-with-cattr-enc.cs

assembly-with-cas.exe: assembly-with-cas.cs
	gmcs assembly-with-cas.cs

assembly-with-events.exe: assembly-with-events.cs
	gmcs assembly-with-events.cs

assembly-with-properties.exe: assembly-with-properties.cs
	gmcs assembly-with-properties.cs

assembly-with-fields.exe: assembly-with-fields.cs
	gmcs assembly-with-fields.cs

module.netmodule: module.il
	ilasm2 /dll /output:module.netmodule module.il

assembly-with-module.exe: assembly-with-module.cs module.netmodule
	gmcs assembly-with-module.cs -addmodule:module.netmodule

assembly-with-generics.exe: assembly-with-generics.cs
	gmcs assembly-with-generics.cs

assembly-with-signatures.exe: assembly-with-signatures.cs
	gmcs assembly-with-signatures.cs

TEST_BINARIES = assembly-with-resource.exe \
	assembly-with-complex-type.exe \
	assembly-with-methods.exe \
	assembly-with-params.exe \
	assembly-with-constants.exe \
	assembly-with-cattr.exe \
	assembly-with-cattr-enc.exe \
	assembly-with-cas.exe \
	assembly-with-events.exe \
	assembly-with-properties.exe \
	assembly-with-module.exe \
	assembly-with-resource.exe \
	assembly-with-generics.exe \
	assembly-with-custommod.exe \
	assembly-with-signatures.exe \
	assembly-with-locals.exe \
	assembly-with-types.exe \
	assembly-with-typespec.exe \
	assembly-with-global-method.exe \
	assembly-with-calli.exe	\
	assembly-with-fields.exe \
	simple-assembly.exe


gen-md-tests: gen-md-tests.c
	gcc $(GLIB_CFLAGS) $(GLIB_LINK_FLAGS) -I../../../ gen-md-tests.c ../../mini/.libs/libmono-2.0.a -g -Wall -lpthread -lm -ldl -lrt -o gen-md-tests

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

cli-tables-tests.ok: gen-md-tests cli-tables-tests.md $(TEST_BINARIES)
	./gen-md-tests cli-tables-tests.md
	touch cli-tables-tests.ok

cli-blob-tests.ok: gen-md-tests cli-blob-tests.md $(TEST_BINARIES)
	./gen-md-tests cli-blob-tests.md
	touch cli-blob-tests.ok

cli-cattr-tests.ok: gen-md-tests cli-cattr-tests.md $(TEST_BINARIES)
	./gen-md-tests cli-cattr-tests.md
	touch cli-cattr-tests.ok

cli-global-props-tests.ok: gen-md-tests cli-global-props-tests.md $(TEST_BINARIES)
	./gen-md-tests cli-global-props-tests.md
	touch cli-global-props-tests.ok

generate-stamp: header-tests.ok section-table-tests.ok data-directory-tests.ok resources-tests.ok cli-header-tests.ok cli-metadata-tests.ok cli-tables-tests.ok cli-blob-tests.ok  cli-cattr-tests.ok cli-global-props-tests.ok
	touch generate-stamp

clean-stamps:
	rm -f *.ok
	rm -f generate-stamp

run-test: generate-stamp
	@for i in *.exe; do \
		RES=99; \
		PEDUMP_ARG="metadata" \
		FIRST=`echo $$i | cut -d '-' -f 1`; \
		if [ "$$FIRST" == "invalid" ];	then \
			RES=1; \
		fi; \
		if [ "$$FIRST" == "badrt" ];	then \
			PEDUMP_ARG="metadata,code" \
			RES=1; \
		fi; \
		if [ "$$FIRST" == "valid" ]; then \
			RES=0; \
		fi; \
		if [ $$RES != 99 ]; then \
			../../metadata/pedump --verify $$PEDUMP_ARG $$i >/dev/null 2>/dev/null; \
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
		if [ "$$FIRST" == "badrt" ];	then \
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
