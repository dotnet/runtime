
%.exe: %.cil
	ilasm -out:$@ $<

BatchCompiler.exe: BatchCompiler.cs
	mcs -r:../../../mcs/class/lib/net_4_x/ilasm.exe BatchCompiler.cs

test_lib.dll: test_lib.cs
	mcs test_lib.cs -target:library

compile-stamp: generate-stamp BatchCompiler.exe test_lib.dll
	for i in *.cs; do \
		EXE="`echo $$i | cut -d. -f1`.exe"; \
		DLL="`echo $$i | cut -d. -f1`.dll"; \
		if ! [ -f $$EXE ] && ! [ -f $$DLL ]; then \
			mcs /unsafe $$i; \
		fi \
	done 
	#MONO_PATH=../../../mcs/class/lib/net_2_0/ mono BatchCompiler.exe
	for i in *.il; do ilasm $$i; done
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

run-test: compile-stamp
	@for i in *.exe; do \
		TEST=`echo $$i | cut -d '.' -f 1`; \
		RES=99; \
		FIRST=`echo $$i | cut -d '_' -f 1`; \
		if [ "$$FIRST" == "invalid" ]; \
		then \
			RES=3; \
		fi; \
		if [ "$$FIRST" == "unverifiable" ] || [ "$$FIRST" == "typeunverifiable" ]; \
		then \
			RES=2; \
		fi; \
		if [ "$$FIRST" == "badmd" ]; \
		then \
			RES=1; \
		fi; \
		if [ "$$FIRST" == "valid" ]; \
		then \
			RES=0; \
		fi; \
		if [ "$$FIRST" == "strict" ]; \
		then \
			echo "#in strict more it must fail under strict check and pass under non-strict check" >/dev/null; \
			../../metadata/pedump --verify partial-md,error,warn,cls,code $$TEST.exe >/dev/null 2>/dev/null; \
			R1=$$?; \
			../../metadata/pedump --verify partial-md,error,warn,cls,code,non-strict $$TEST.exe >/dev/null 2>/dev/null; \
			R2=$$?; \
			if [ $$R1 != 2 ] && [ $$R1 != 3 ]; then \
				echo "$$TEST is strict but did not fail under strict check, got $${R1} but expected 2 or 3"; \
			fi; \
			echo "#non-strict result" >/dev/null; \
			if [ $$R2 != 0 ]; then \
				echo "$$TEST is strict but did not pass under non-strict check, got $${R2} but expected 0"; \
			fi; \
		elif [ "$$FIRST" == "typeunverifiable" ]; then \
			echo "#in type unverifiable more it must fail under verifiable mode but it's ok under valid mode" >/dev/null; \
			../../metadata/pedump --verify partial-md,error,warn,cls,code $$TEST.exe  >/dev/null 2>/dev/null; \
			R1=$$?; \
			../../metadata/pedump --verify partial-md,error,warn,cls,code,valid-only $$TEST.exe  >/dev/null 2>/dev/null; \
			R2=$$?; \
			if [ $$R1 != 3 ]; then \
				echo "$$TEST is type unverifiable but did not fail under verifiable check, got $${R1} but expected 3"; \
			fi; \
			echo "#type unverifiable result" >/dev/null; \
			if [ $$R2 != 0 ]; then \
				echo "$$TEST is type unverifiable but did not pass under non-strict check, got $${R2} but expected 0"; \
			fi; \
		elif [ $$RES != 99 ]; then \
			../../metadata/pedump --verify partial-md,error,warn,cls,code $$TEST.exe >/dev/null 2>/dev/null; \
			R=$$?; \
			if [ $$R != $$RES ]; then \
				echo "$$TEST failed expected $$RES but got $$R"; \
			fi; \
		fi; \
	done

