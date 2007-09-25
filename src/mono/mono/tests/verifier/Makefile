
%.exe: %.cil
	ilasm -out:$@ $<

compile-stamp: generate-stamp
	for i in *.il; do ilasm2 $$i; done
	for i in *.cs; do gmcs $$i; done
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
		#echo $$i; \
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
		../../metadata/pedump --verify error,warn,cls,code $$TEST.exe >/dev/null 2>/dev/null;\
		R=$$?; \
		if [ $$R != $$RES ]; \
		then \
			echo "$$TEST failed expected $$RES but got $$R"; \
		fi; \
	done

