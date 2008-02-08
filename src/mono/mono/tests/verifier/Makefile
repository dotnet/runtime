
%.exe: %.cil
	ilasm -out:$@ $<

compile-stamp: generate-stamp
	for i in *.il; do ilasm2 $$i; done
	for i in *.cs; do gmcs /unsafe $$i; done
	touch compile-stamp

clean:
	for i in *generated*; do rm $$i; done
	for i in *.exe; do rm $$i; done
	rm generate-stamp compile-stamp

generate-stamp: make_tests.sh make_bin_test.sh make_exception_branch_test.sh make_obj_store_test.sh \
		make_stack_0_pop_test.sh make_stack_0_test.sh make_stack_1_pop_test.sh \
		make_stack_merge_test.sh make_store_test.sh make_unary_test.sh 
	./make_tests.sh
	touch generate-stamp

test: compile-stamp run-test

run-test:
	@for i in *.exe; do \
		TEST=`echo $$i | cut -d '.' -f 1`; \
		RES=99; \
		FIRST=`echo $$i | cut -d '_' -f 1`; \
		if [ "$$FIRST" == "invalid" ]; \
		then \
			RES=3; \
		fi; \
		if [ "$$FIRST" == "unverifiable" ]; \
		then \
			RES=2; \
		fi; \
		if [ "$$FIRST" == "valid" ]; \
		then \
			RES=0; \
		fi; \
		if [ "$$FIRST" == "strict" ]; \
		then \
			#in strict more it must fail under strict check and pass under non-strict check \
			../../metadata/pedump --verify error,warn,cls,code $$TEST.exe >/dev/null 2>/dev/null; \
			R1=$$?; \
			../../metadata/pedump --verify error,warn,cls,code,non-strict $$TEST.exe >/dev/null 2>/dev/null; \
			R2=$$?; \
			if [ $$R1 != 2 ] && [ $$R1 != 3 ]; then \
				echo "$$TEST is strict but did not fail under strict check, got $${R1} but expected 2 or 3"; \
			fi \
			#non-strict result \
			if [ $$R2 != 0 ]; then \
				echo "$$TEST is strict but did not pass under non-strict check, got $${R2} but expected 0"; \
			fi \
		else \
			../../metadata/pedump --verify error,warn,cls,code $$TEST.exe >/dev/null 2>/dev/null; \
			R=$$?; \
			if [ $$R != $$RES ]; then \
				echo "$$TEST failed expected $$RES but got $$R"; \
			fi \
		fi; \
	done

