
%.exe: %.cil
	ilasm -out:$@ $<

compile-stamp: generate-stamp
	for i in *.il; do ilasm $$i; done
	touch compile-stamp

clean:
	rm -f *_generated.il *.exe *.dll generate-stamp compile-stamp

generate-stamp: make_tests.sh make_bin_test.sh make_exception_branch_test.sh make_obj_store_test.sh \
		make_stack_0_pop_test.sh make_stack_0_test.sh make_stack_1_pop_test.sh \
		make_stack_merge_test.sh make_store_test.sh make_unary_test.sh
	./make_tests.sh
	touch generate-stamp

test: compile-stamp
	@for i in *.exe; do \
		echo -n $$i; \
		mono $$i >/dev/null 2>/dev/null; \
		RES=$$?; \
		if [ $$RES = 1 ]; \
		then \
			echo " OK"; \
		fi; \
		if [ $$RES = 0 ]; \
		then \
			echo " NOT OK (silent success)"; \
		fi; \
		if [ $$RES '>' 1 ]; \
		then \
			echo " NOT OK (other failure)"; \
		fi; \
	done

